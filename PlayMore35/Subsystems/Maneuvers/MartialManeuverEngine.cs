using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.Maneuvers {
    public class MartialManeuverEngine : ISubsystemEngine {
        public const string SubsystemId = "martial_maneuvers";
        public const string ACTIVE_STANCES = "active stances";
        public static readonly string[] StandardSources = new[] { "Warblade", "Crusader", "Swordsage", "Feats" };

        public static string InitiatorLevelStat(string source = null) {
            if (string.IsNullOrWhiteSpace(source)) {
                return "initiator level";
            }
            return $"initiator level:{source.Trim().ToLowerInvariant()}";
        }

        private readonly ISubsystemContentProvider contentProvider;
        private ICharacterContext context;
        private MartialManeuverState state;

        public string Id => SubsystemId;

        public MartialManeuverState State => state;

        public MartialManeuverEngine(ISubsystemContentProvider contentProvider = null, MartialManeuverState state = null) {
            this.contentProvider = contentProvider ?? new ResourceSubsystemContentProvider();
            this.state = state ?? new MartialManeuverState();
        }

        public bool IsActive {
            get {
                if (context == null) return false;
                if (context.GetStatValue(InitiatorLevelStat(), 0) > 0 ||
                    context.GetStatValue("maneuvers known", 0) > 0 ||
                    context.GetStatValue("stances known", 0) > 0 ||
                    context.GetStatValue(ACTIVE_STANCES, 0) > 0) {
                    return true;
                }

                var allSources = StandardSources.Union(state.Sources.Keys, StringComparer.OrdinalIgnoreCase);
                foreach (var source in allSources) {
                    string lower = source.ToLowerInvariant();
                    if (context.GetStatValue(InitiatorLevelStat(source), 0) > 0 ||
                        context.GetStatValue($"maneuvers known:{lower}", 0) > 0 ||
                        context.GetStatValue($"stances known:{lower}", 0) > 0) {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Initialize(ICharacterContext context) {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            if (context.Character != null) {
                if (context.Character.Subsystems.TryGetValue(SubsystemId, out var existingState) && existingState is MartialManeuverState mState) {
                    this.state = mState;
                } else {
                    context.Character.Subsystems[SubsystemId] = this.state;
                }
            }
            WireAllSources();
        }

        private void WireAllSources() {
            var allSources = StandardSources.Union(state.Sources.Keys, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var source in allSources) {
                WireSource(source);
            }
        }

        public ManeuverSourceState GetSource(string source) {
            var src = state.GetOrCreateSource(source);
            if (context != null) {
                WireSource(source, src);
            }
            src.EnsureLoaded();
            return src;
        }

        private void WireSource(string sourceKey, ManeuverSourceState src = null) {
            src = src ?? state.GetOrCreateSource(sourceKey);
            string lower = sourceKey.ToLowerInvariant();
            src.Wire(
                () => context?.GetStatValue($"maneuvers known:{lower}", 0) ?? 0,
                () => context?.GetStatValue($"max maneuver level:{lower}", 0) ?? int.MaxValue,
                slot => {
                    if (slot == null || string.IsNullOrEmpty(slot.ManeuverName)) return 0;
                    return contentProvider.GetManeuver(slot.ManeuverName)?.Level ?? 0;
                },
                () => context?.GetStatValue($"stances known:{lower}", 0) ?? 0,
                () => context?.GetStatValue($"max maneuver level:{lower}", 0) ?? int.MaxValue,
                slot => {
                    if (slot == null || string.IsNullOrEmpty(slot.StanceName)) return 0;
                    return contentProvider.GetManeuver(slot.StanceName)?.Level ?? 0;
                }
            );
        }

        public List<string> ValidateManeuver(string maneuverName, string source) {
            var warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(maneuverName)) {
                return warnings;
            }

            var maneuver = contentProvider.GetManeuver(maneuverName);
            if (maneuver == null) {
                warnings.Add($"Maneuver '{maneuverName}' is not recognized in the rules catalog.");
                return warnings;
            }

            string lowerSource = (source ?? "").ToLowerInvariant();

            // 1. Level cap check
            int maxLevel = context?.GetStatValue($"max maneuver level:{lowerSource}", 0) ?? int.MaxValue;
            if (maneuver.Level > maxLevel) {
                warnings.Add($"Maneuver level {maneuver.Level} exceeds maximum maneuver level {maxLevel} for {source}.");
            }

            // 2. Discipline prerequisites check
            if (maneuver.PrerequisiteCount > 0) {
                int knownDisciplineCount = CountKnownInDiscipline(maneuver.Discipline, maneuverName);
                if (knownDisciplineCount < maneuver.PrerequisiteCount) {
                    string plural = maneuver.PrerequisiteCount == 1 ? "maneuver" : "maneuvers";
                    warnings.Add($"Requires {maneuver.PrerequisiteCount} {maneuver.Discipline} {plural}, but only {knownDisciplineCount} known.");
                }
            }

            // 3. Cross-class uniqueness check
            foreach (var kvp in state.Sources) {
                if (!string.Equals(kvp.Key, source, StringComparison.OrdinalIgnoreCase)) {
                    var otherSource = kvp.Value;
                    if (otherSource.HasManeuver(maneuverName) || otherSource.HasStance(maneuverName)) {
                        warnings.Add($"Maneuver '{maneuverName}' is already learned under source '{kvp.Key}'.");
                    }
                }
            }

            // 4. Readied capacity check
            if (!string.IsNullOrEmpty(source) && state.Sources.TryGetValue(source, out var srcState)) {
                int readiedCount = srcState.ReadiedCount;
                int maxReadied = context?.GetStatValue($"maneuvers readied:{lowerSource}", 0) ?? 0;
                if (readiedCount > maxReadied) {
                    warnings.Add($"Readied maneuvers count ({readiedCount}) exceeds readied capacity ({maxReadied}) for {source}.");
                }
                if (readiedCount < maxReadied) {
                    warnings.Add($"Readied maneuvers count ({readiedCount}) is below readied capacity ({maxReadied}) for {source}.");
                }
            }

            // 5. Active stances capacity check
            if (maneuver != null && maneuver.Type == ManeuverType.Stance) {
                warnings.AddRange(ValidateStances());
            }

            return warnings;
        }

        public List<string> ValidateStances() {
            var warnings = new List<string>();
            int totalActiveStances = state.Sources.Values.Sum(s => s.ActiveStances.Count());
            int maxActiveStances = context?.GetStatValue(ACTIVE_STANCES, 0) ?? 0;
            if (totalActiveStances > maxActiveStances) {
                warnings.Add($"Active stances count ({totalActiveStances}) exceeds active stances capacity ({maxActiveStances}).");
            }
            return warnings;
        }

        private int CountKnownInDiscipline(string discipline, string excludingManeuverName) {
            var distinctKnown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var src in state.Sources.Values) {
                foreach (var name in src.AllKnownManeuverNames()) {
                    if (!string.Equals(name, excludingManeuverName, StringComparison.OrdinalIgnoreCase)) {
                        var m = contentProvider.GetManeuver(name);
                        if (m != null && string.Equals(m.Discipline, discipline, StringComparison.OrdinalIgnoreCase)) {
                            distinctKnown.Add(name);
                        }
                    }
                }

                foreach (var name in src.AllStanceNames()) {
                    if (!string.Equals(name, excludingManeuverName, StringComparison.OrdinalIgnoreCase)) {
                        var m = contentProvider.GetManeuver(name);
                        if (m != null && string.Equals(m.Discipline, discipline, StringComparison.OrdinalIgnoreCase)) {
                            distinctKnown.Add(name);
                        }
                    }
                }
            }

            return distinctKnown.Count;
        }

        public void ContributeEffects(EffectPool pool, ICharacterContext context) {
            if (pool == null) return;
            var ctx = context ?? this.context;
            foreach (var source in state.Sources.Values) {
                foreach (var stance in source.ActiveStances) {
                    if (string.IsNullOrEmpty(stance.StanceName)) continue;
                    var maneuver = contentProvider.GetManeuver(stance.StanceName);
                    if (maneuver?.Effects != null) {
                        foreach (var effect in maneuver.Effects) {
                            pool.AddEffect(effect);
                        }
                    }
                }
            }

            if (ctx?.Timeline?.ScheduledActions != null) {
                foreach (var actionList in ctx.Timeline.ScheduledActions.Values) {
                    ContributeManeuverActions(pool, actionList);
                }
            }

            if (ctx?.Timeline?.EncounterActions != null) {
                ContributeManeuverActions(pool, ctx.Timeline.EncounterActions);
            }
        }

        private void ContributeManeuverActions(EffectPool pool, IEnumerable<SheetAction> actions) {
            if (actions == null) return;
            foreach (var action in actions.OfType<ManeuverSheetAction>()) {
                if (string.IsNullOrEmpty(action.ManeuverName)) continue;
                var maneuver = contentProvider.GetManeuver(action.ManeuverName);
                if (maneuver?.Effects != null) {
                    foreach (var effect in maneuver.Effects) {
                        pool.AddEffect(effect);
                    }
                }
            }
        }

        // Source: Tome of Battle, pp. 5, 23, 38, 40, 43
        public void AdoptStance(string source, int slotIndex, string replaceSource = null, int? replaceSlotIndex = null) {
            var src = GetSource(source);
            var slot = src?.GetStanceSlot(slotIndex);
            if (slot == null) return;

            int maxActive = context?.GetStatValue(ACTIVE_STANCES, 1) ?? 1;

            if (maxActive <= 1) {
                ExitAllStances();
            } else {
                var activeStances = new List<(ManeuverSourceState Source, StanceSlot Slot, string SourceKey, int SlotIndex)>();
                var allSources = StandardSources.Union(state.Sources.Keys, StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var sourceKey in allSources) {
                    if (!state.Sources.ContainsKey(sourceKey)) continue;
                    var sState = state.Sources[sourceKey];
                    if (sState.Stances != null) {
                        for (int i = 0; i < sState.Stances.Slots.Count; i++) {
                            var s = sState.Stances.Slots[i];
                            if (s != null && s.IsActive && s != slot) {
                                activeStances.Add((sState, s, sourceKey, i));
                            }
                        }
                    }
                }

                // If caller specified an explicit replacement target, deactivate it
                if (!string.IsNullOrEmpty(replaceSource) && replaceSlotIndex.HasValue) {
                    var target = activeStances.FirstOrDefault(a =>
                        string.Equals(a.SourceKey, replaceSource, StringComparison.OrdinalIgnoreCase) &&
                        a.SlotIndex == replaceSlotIndex.Value);
                    if (target.Slot != null) {
                        target.Slot.IsActive = false;
                        target.Source.SyncToSaved();
                        activeStances.Remove(target);
                    }
                }

                while (activeStances.Count >= maxActive) {
                    var oldest = activeStances[0];
                    oldest.Slot.IsActive = false;
                    oldest.Source.SyncToSaved();
                    activeStances.RemoveAt(0);
                }
            }

            slot.IsActive = true;
            src.SyncToSaved();
        }

        // Source: Tome of Battle, p. 23
        public void AdoptStances(string source1, int slotIndex1, string source2, int slotIndex2) {
            var src1 = GetSource(source1);
            var slot1 = src1?.GetStanceSlot(slotIndex1);
            var src2 = GetSource(source2);
            var slot2 = src2?.GetStanceSlot(slotIndex2);

            if (slot1 == null && slot2 == null) return;

            ExitAllStances();

            if (slot1 != null) {
                slot1.IsActive = true;
                src1.SyncToSaved();
            }
            if (slot2 != null && slot2 != slot1) {
                slot2.IsActive = true;
                src2.SyncToSaved();
            }
        }

        // Source: Tome of Battle, pp. 5, 38, 43
        public void ExitStance(string source, int slotIndex) {
            var src = GetSource(source);
            var slot = src?.GetStanceSlot(slotIndex);
            if (slot != null && slot.IsActive) {
                slot.IsActive = false;
                src.SyncToSaved();
            }
        }

        // Source: Tome of Battle, pp. 5, 38, 43
        public void ExitAllStances() {
            var allSources = StandardSources.Union(state.Sources.Keys, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var sourceKey in allSources) {
                if (!state.Sources.ContainsKey(sourceKey)) continue;
                var src = state.Sources[sourceKey];
                if (src.Stances != null) {
                    bool changed = false;
                    foreach (var slot in src.Stances.Slots) {
                        if (slot != null && slot.IsActive) {
                            slot.IsActive = false;
                            changed = true;
                        }
                    }
                    if (changed) {
                        src.SyncToSaved();
                    }
                }
            }
        }

        // Source: Tome of Battle, p. 38
        public void ReadyManeuver(string source, int slotIndex) {
            var src = GetSource(source);
            var slot = src?.GetKnownSlot(slotIndex);
            if (slot != null && slot.State == ManeuverState.Unreadied) {
                if (string.Equals(source, "Crusader", StringComparison.OrdinalIgnoreCase)) {
                    slot.State = ManeuverState.Withheld;
                } else {
                    slot.State = ManeuverState.Available;
                }
                src.SyncToSaved();
            }
        }

        // Source: Tome of Battle, p. 38
        public void UnreadyManeuver(string source, int slotIndex) {
            var src = GetSource(source);
            var slot = src?.GetKnownSlot(slotIndex);
            if (slot != null && slot.State != ManeuverState.Unreadied) {
                slot.State = ManeuverState.Unreadied;
                src.SyncToSaved();
            }
        }

        // Source: Tome of Battle, pp. 38, 42-45
        public bool InitiateManeuver(string source, int slotIndex, bool throwOnInvalid = false) {
            var src = GetSource(source);
            var slot = src?.GetKnownSlot(slotIndex);
            if (slot == null || slot.State != ManeuverState.Available) {
                if (throwOnInvalid) {
                    throw new InvalidOperationException($"Maneuver slot {slotIndex} in source '{source}' is not Available.");
                }
                return false;
            }

            slot.State = ManeuverState.Expended;
            src.SyncToSaved();

            var maneuver = !string.IsNullOrEmpty(slot.ManeuverName) ? contentProvider.GetManeuver(slot.ManeuverName) : null;
            if (maneuver != null && (maneuver.Type == ManeuverType.Boost || maneuver.Type == ManeuverType.Counter)) {
                if (context?.Timeline != null) {
                    context.Timeline.Schedule(new ManeuverSheetAction(maneuver.Name, source, "1 round"), "1 round", context);
                }
            }

            return true;
        }

        // Source: Tome of Battle, p. 40
        public void RecoverWarblade(string source = "Warblade") {
            var src = GetSource(source);
            if (src?.Known == null) return;
            foreach (var slot in src.Known.Slots) {
                if (slot != null && slot.State == ManeuverState.Expended) {
                    slot.State = ManeuverState.Available;
                }
            }
            src.SyncToSaved();
        }

        // Source: Tome of Battle, p. 40
        public void RecoverSwordsage(string source, int slotIndex) {
            var src = GetSource(source);
            var slot = src?.GetKnownSlot(slotIndex);
            if (slot != null && slot.State == ManeuverState.Expended) {
                slot.State = ManeuverState.Available;
                src.SyncToSaved();
            }
        }

        // Source: Tome of Battle, p. 40
        public void RecoverSwordsage(int slotIndex, string source = "Swordsage") {
            RecoverSwordsage(source, slotIndex);
        }

        // Source: Tome of Battle, pp. 38, 40
        public void DealCrusader(string source = "Crusader", Random rng = null) {
            var src = GetSource(source);
            if (src == null) return;

            var readied = src.ReadiedManeuvers.ToList();
            if (readied.Count == 0) return;

            string lower = (source ?? "").ToLowerInvariant();
            int k = context?.GetStatValue($"maneuvers granted:{lower}", 2) ?? 2;
            if (k <= 0) k = 2;

            foreach (var slot in readied) {
                slot.State = ManeuverState.Withheld;
            }

            rng = rng ?? new Random();
            var shuffled = readied.ToList();
            for (int i = shuffled.Count - 1; i > 0; i--) {
                int j = rng.Next(i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            int grantCount = Math.Min(k, shuffled.Count);
            for (int i = 0; i < grantCount; i++) {
                shuffled[i].State = ManeuverState.Available;
            }

            src.SyncToSaved();
        }

        // Source: Tome of Battle, pp. 38, 40
        public void DealCrusader(Random rng) => DealCrusader("Crusader", rng);

        // Source: Tome of Battle, p. 38
        public void AdvanceCrusaderTurn(string source = "Crusader", Random rng = null) {
            var src = GetSource(source);
            if (src == null) return;

            rng = rng ?? new Random();
            var withheldSlots = src.Known?.Slots?.Where(s => s != null && s.State == ManeuverState.Withheld).ToList()
                                ?? src.ReadiedManeuvers.Where(s => s.State == ManeuverState.Withheld).ToList();

            if (withheldSlots.Count > 0) {
                int pickIndex = rng.Next(withheldSlots.Count);
                withheldSlots[pickIndex].State = ManeuverState.Available;
                src.SyncToSaved();
            } else {
                DealCrusader(source, rng);
            }
        }

        // Source: Tome of Battle, p. 38
        public void AdvanceCrusaderTurn(Random rng) => AdvanceCrusaderTurn("Crusader", rng);

        // Source: Tome of Battle, pp. 38, 40
        public void ResetEncounter(Random rng = null) {
            var allSources = StandardSources.Union(state.Sources.Keys, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var sourceKey in allSources) {
                if (!state.Sources.ContainsKey(sourceKey)) continue;
                var src = state.Sources[sourceKey];
                if (string.Equals(sourceKey, "Crusader", StringComparison.OrdinalIgnoreCase)) {
                    DealCrusader(sourceKey, rng);
                } else {
                    if (src.Known != null) {
                        foreach (var slot in src.Known.Slots) {
                            if (slot != null && slot.IsReadied) {
                                slot.State = ManeuverState.Available;
                            }
                        }
                        src.SyncToSaved();
                    }
                }
            }
        }
    }
}
