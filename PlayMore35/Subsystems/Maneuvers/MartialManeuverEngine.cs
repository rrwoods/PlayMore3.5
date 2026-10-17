using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Rules;

namespace PlayMore35.Subsystems.Maneuvers {
    public class MartialManeuverEngine : ISubsystemEngine {
        public const string SubsystemId = "martial_maneuvers";
        public const string ACTIVE_STANCES = "active stances";
        public static readonly string[] StandardSources = new[] { "Warblade", "Crusader", "Swordsage", "Feats" };

        public static string InitiatorLevelStat(string source) {
            if (string.IsNullOrWhiteSpace(source)) {
                throw new ArgumentException("Source cannot be null or whitespace.", nameof(source));
            }
            return $"initiator level:{source.Trim().ToLowerInvariant()}";
        }

        private static readonly Dictionary<int, int> MaxManeuverLevelLookupTable = new Dictionary<int, int> {
            { 0, 0 },
            { 1, 1 },
            { 3, 2 },
            { 5, 3 },
            { 7, 4 },
            { 9, 5 },
            { 11, 6 },
            { 13, 7 },
            { 15, 8 },
            { 17, 9 }
        };

        private IRulesCatalog catalog;
        private ICharacterContext context;
        private MartialManeuverState state;
        private readonly Dictionary<string, SubsystemSlotGroup<KnownManeuverSlot>> knownGroups =
            new Dictionary<string, SubsystemSlotGroup<KnownManeuverSlot>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SubsystemSlotGroup<StanceSlot>> stanceGroups =
            new Dictionary<string, SubsystemSlotGroup<StanceSlot>>(StringComparer.OrdinalIgnoreCase);

        public string Id => SubsystemId;

        public MartialManeuverState State => state;

        public MartialManeuverEngine(IRulesCatalog catalog = null, MartialManeuverState state = null) {
            this.catalog = catalog ?? new GameRulesCatalog();
            this.state = state ?? new MartialManeuverState();
        }

        public bool IsActive {
            get {
                if (context == null) return false;

                var allSources = GetAllSources();
                foreach (var source in allSources) {
                    if (context.GetStatValue(InitiatorLevelStat(source), 0) > 0) {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Initialize(ICharacterContext context) {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            if (context.Catalog != null) {
                this.catalog = context.Catalog;
            }
            if (context.Character != null) {
                if (context.Character.Subsystems.TryGetValue(SubsystemId, out var existingState) && existingState is MartialManeuverState mState) {
                    this.state = mState;
                } else {
                    context.Character.Subsystems[SubsystemId] = this.state;
                }
            }
            knownGroups.Clear();
            stanceGroups.Clear();
        }

        public IEnumerable<string> GetAllSources() {
            var sources = new HashSet<string>(StandardSources, StringComparer.OrdinalIgnoreCase);
            foreach (var k in state.Known.Keys) sources.Add(k);
            foreach (var k in state.Stances.Keys) sources.Add(k);
            return sources;
        }

        public IEnumerable<string> GetKnownSources() {
            return StandardSources.Union(state.Known.Keys, StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> GetStanceSources() {
            return StandardSources.Union(state.Stances.Keys, StringComparer.OrdinalIgnoreCase);
        }

        public SubsystemSlotGroup<KnownManeuverSlot> GetKnownSlotGroup(string source) {
            if (string.IsNullOrWhiteSpace(source)) {
                throw new ArgumentException("Source name cannot be null or empty.", nameof(source));
            }
            if (!knownGroups.TryGetValue(source, out var group)) {
                string lower = source.ToLowerInvariant();
                var storage = state.GetOrCreateKnown(source);
                group = new SubsystemSlotGroup<KnownManeuverSlot>(
                    () => context?.GetStatValue($"maneuvers known:{lower}", 0) ?? 0,
                    () => context?.GetStatValue($"max maneuver level:{lower}", 0) ?? int.MaxValue,
                    slot => {
                        if (slot == null || string.IsNullOrEmpty(slot.ManeuverName)) return 0;
                        return catalog.GetManeuver(slot.ManeuverName)?.Level ?? 0;
                    },
                    storage
                );
                knownGroups[source] = group;
            }
            return group;
        }

        public SubsystemSlotGroup<StanceSlot> GetStanceSlotGroup(string source) {
            if (string.IsNullOrWhiteSpace(source)) {
                throw new ArgumentException("Source name cannot be null or empty.", nameof(source));
            }
            if (!stanceGroups.TryGetValue(source, out var group)) {
                string lower = source.ToLowerInvariant();
                var storage = state.GetOrCreateStances(source);
                group = new SubsystemSlotGroup<StanceSlot>(
                    () => context?.GetStatValue($"stances known:{lower}", 0) ?? 0,
                    () => context?.GetStatValue($"max maneuver level:{lower}", 0) ?? int.MaxValue,
                    slot => {
                        if (slot == null || string.IsNullOrEmpty(slot.StanceName)) return 0;
                        return catalog.GetManeuver(slot.StanceName)?.Level ?? 0;
                    },
                    storage
                );
                stanceGroups[source] = group;
            }
            return group;
        }


        public KnownManeuverSlot GetKnownSlot(string source, int slotIndex) {
            if (slotIndex < 0) return null;
            if (state.Known.TryGetValue(source, out var list) && slotIndex < list.Count) {
                return list[slotIndex];
            }
            return null;
        }

        public void SetKnownSlot(string source, int slotIndex, KnownManeuverSlot slot) {
            if (slotIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Slot index {slotIndex} must be non-negative.");
            }
            GetKnownSlotGroup(source)[slotIndex] = slot;
        }

        public void SetKnownSlot(string source, int slotIndex, string maneuverName) {
            var existing = GetKnownSlot(source, slotIndex);
            var slotState = existing?.State ?? ManeuverState.Unreadied;
            SetKnownSlot(source, slotIndex, new KnownManeuverSlot(maneuverName, slotState));
        }

        public void ClearKnownSlot(string source, int slotIndex) {
            if (slotIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Slot index {slotIndex} must be non-negative.");
            }
            GetKnownSlotGroup(source).ClearSlot(slotIndex);
        }

        public StanceSlot GetStanceSlot(string source, int slotIndex) {
            if (slotIndex < 0) return null;
            if (state.Stances.TryGetValue(source, out var list) && slotIndex < list.Count) {
                return list[slotIndex];
            }
            return null;
        }

        public void SetStanceSlot(string source, int slotIndex, StanceSlot slot) {
            if (slotIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Slot index {slotIndex} must be non-negative.");
            }
            GetStanceSlotGroup(source)[slotIndex] = slot;
        }

        public void SetStanceSlot(string source, int slotIndex, string stanceName) {
            var existing = GetStanceSlot(source, slotIndex);
            var isActive = existing?.IsActive ?? false;
            SetStanceSlot(source, slotIndex, new StanceSlot(stanceName, isActive));
        }

        public void ClearStanceSlot(string source, int slotIndex) {
            if (slotIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Slot index {slotIndex} must be non-negative.");
            }
            GetStanceSlotGroup(source).ClearSlot(slotIndex);
        }

        public IReadOnlyList<KnownManeuverSlot> GetKnownSlots(string source) {
            return GetKnownSlotGroup(source).Slots;
        }

        public IReadOnlyList<StanceSlot> GetStanceSlots(string source) {
            return GetStanceSlotGroup(source).Slots;
        }

        public IEnumerable<KnownManeuverSlot> GetReadiedManeuvers(string source) {
            if (state.Known.TryGetValue(source, out var list) && list != null) {
                return list.Where(s => s != null && s.IsReadied);
            }
            return Enumerable.Empty<KnownManeuverSlot>();
        }

        public int GetReadiedCount(string source) {
            if (state.Known.TryGetValue(source, out var list) && list != null) {
                return list.Count(s => s != null && s.IsReadied);
            }
            return 0;
        }

        public IEnumerable<StanceSlot> GetActiveStances(string source) {
            if (state.Stances.TryGetValue(source, out var list) && list != null) {
                return list.Where(s => s != null && s.IsActive);
            }
            return Enumerable.Empty<StanceSlot>();
        }

        public IEnumerable<StanceSlot> GetAllActiveStances() {
            return state.Stances.Values
                .Where(list => list != null)
                .SelectMany(list => list)
                .Where(s => s != null && s.IsActive);
        }

        public IEnumerable<string> GetKnownManeuverNames(string source = null) {
            if (!string.IsNullOrEmpty(source)) {
                if (state.Known.TryGetValue(source, out var list) && list != null) {
                    return list.Where(s => s != null && !string.IsNullOrWhiteSpace(s.ManeuverName))
                               .Select(s => s.ManeuverName);
                }
                return Enumerable.Empty<string>();
            }
            return state.Known.Values
                .Where(list => list != null)
                .SelectMany(list => list)
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.ManeuverName))
                .Select(s => s.ManeuverName);
        }

        public IEnumerable<string> GetStanceNames(string source = null) {
            if (!string.IsNullOrEmpty(source)) {
                if (state.Stances.TryGetValue(source, out var list) && list != null) {
                    return list.Where(s => s != null && !string.IsNullOrWhiteSpace(s.StanceName))
                               .Select(s => s.StanceName);
                }
                return Enumerable.Empty<string>();
            }
            return state.Stances.Values
                .Where(list => list != null)
                .SelectMany(list => list)
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.StanceName))
                .Select(s => s.StanceName);
        }

        public bool HasManeuver(string maneuverName, string source = null) {
            if (string.IsNullOrWhiteSpace(maneuverName)) return false;
            return GetKnownManeuverNames(source).Any(name => string.Equals(name, maneuverName, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasStance(string stanceName, string source = null) {
            if (string.IsNullOrWhiteSpace(stanceName)) return false;
            return GetStanceNames(source).Any(name => string.Equals(name, stanceName, StringComparison.OrdinalIgnoreCase));
        }

        public List<string> ValidateManeuver(string maneuverName, string source) {
            var warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(maneuverName)) {
                return warnings;
            }

            var maneuver = catalog.GetManeuver(maneuverName);
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
            var allSources = GetAllSources();
            foreach (var otherSource in allSources) {
                if (!string.Equals(otherSource, source, StringComparison.OrdinalIgnoreCase)) {
                    if (HasManeuver(maneuverName, otherSource) || HasStance(maneuverName, otherSource)) {
                        warnings.Add($"Maneuver '{maneuverName}' is already learned under source '{otherSource}'.");
                    }
                }
            }

            // 4. Readied capacity check
            if (!string.IsNullOrEmpty(source)) {
                int readiedCount = GetReadiedCount(source);
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
            int totalActiveStances = GetAllActiveStances().Count();
            int maxActiveStances = context?.GetStatValue(ACTIVE_STANCES, 0) ?? 0;
            if (totalActiveStances > maxActiveStances) {
                warnings.Add($"Active stances count ({totalActiveStances}) exceeds active stances capacity ({maxActiveStances}).");
            }
            return warnings;
        }

        private int CountKnownInDiscipline(string discipline, string excludingManeuverName) {
            var distinctKnown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in GetKnownManeuverNames()) {
                if (!string.Equals(name, excludingManeuverName, StringComparison.OrdinalIgnoreCase)) {
                    var m = catalog.GetManeuver(name);
                    if (m != null && string.Equals(m.Discipline, discipline, StringComparison.OrdinalIgnoreCase)) {
                        distinctKnown.Add(name);
                    }
                }
            }

            foreach (var name in GetStanceNames()) {
                if (!string.Equals(name, excludingManeuverName, StringComparison.OrdinalIgnoreCase)) {
                    var m = catalog.GetManeuver(name);
                    if (m != null && string.Equals(m.Discipline, discipline, StringComparison.OrdinalIgnoreCase)) {
                        distinctKnown.Add(name);
                    }
                }
            }

            return distinctKnown.Count;
        }

        public void ContributeEffects(EffectPool pool, ICharacterContext context) {
            if (pool == null) return;
            var ctx = context ?? this.context;

            if (ctx != null) {
                foreach (var source in GetAllSources()) {
                    if (ctx.GetStatValue(InitiatorLevelStat(source), 0) > 0) {
                        string lower = source.Trim().ToLowerInvariant();
                        string statName = $"max maneuver level:{lower}";
                        pool.AddStat(new Statistic(statName));
                        var maxLevelEffect = new Effect(statName);
                        var lookupAmount = new LookupAmount(InitiatorLevelStat(source), MaxManeuverLevelLookupTable, -1, 1);
                        maxLevelEffect.AddModifier(statName, new Modifier(lookupAmount, ModifierTypes.INTERNAL));
                        pool.AddEffect(maxLevelEffect);
                    }
                }
            }

            foreach (var stance in GetAllActiveStances()) {
                if (string.IsNullOrEmpty(stance.StanceName)) continue;
                var maneuver = catalog.GetManeuver(stance.StanceName);
                if (maneuver?.Effects != null) {
                    foreach (var effect in maneuver.Effects) {
                        pool.AddEffect(effect);
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
                var maneuver = catalog.GetManeuver(action.ManeuverName);
                if (maneuver?.Effects != null) {
                    foreach (var effect in maneuver.Effects) {
                        pool.AddEffect(effect);
                    }
                }
            }
        }

        // Source: Tome of Battle, pp. 5, 23, 38, 40, 43
        public void AdoptStance(string source, int slotIndex, string replaceSource = null, int? replaceSlotIndex = null) {
            var slot = GetStanceSlot(source, slotIndex);
            if (slot == null) return;

            int maxActive = context?.GetStatValue(ACTIVE_STANCES, 1) ?? 1;

            if (maxActive <= 1) {
                ExitAllStances();
            } else {
                var activeStances = new List<(StanceSlot Slot, string SourceKey, int SlotIndex)>();
                var allSources = GetStanceSources();
                foreach (var sourceKey in allSources) {
                    if (!state.Stances.TryGetValue(sourceKey, out var slots)) continue;
                    for (int i = 0; i < slots.Count; i++) {
                        var s = slots[i];
                        if (s != null && s.IsActive && s != slot) {
                            activeStances.Add((s, sourceKey, i));
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
                        activeStances.Remove(target);
                    }
                }

                while (activeStances.Count >= maxActive) {
                    var oldest = activeStances[0];
                    oldest.Slot.IsActive = false;
                    activeStances.RemoveAt(0);
                }
            }

            slot.IsActive = true;
        }

        // Source: Tome of Battle, p. 23
        public void AdoptStances(string source1, int slotIndex1, string source2, int slotIndex2) {
            var slot1 = GetStanceSlot(source1, slotIndex1);
            var slot2 = GetStanceSlot(source2, slotIndex2);

            if (slot1 == null && slot2 == null) return;

            ExitAllStances();

            if (slot1 != null) {
                slot1.IsActive = true;
            }
            if (slot2 != null && slot2 != slot1) {
                slot2.IsActive = true;
            }
        }

        // Source: Tome of Battle, pp. 5, 38, 43
        public void ExitStance(string source, int slotIndex) {
            var slot = GetStanceSlot(source, slotIndex);
            if (slot != null && slot.IsActive) {
                slot.IsActive = false;
            }
        }

        // Source: Tome of Battle, pp. 5, 38, 43
        public void ExitAllStances() {
            foreach (var slots in state.Stances.Values) {
                if (slots != null) {
                    foreach (var slot in slots) {
                        if (slot != null && slot.IsActive) {
                            slot.IsActive = false;
                        }
                    }
                }
            }
        }

        // Source: Tome of Battle, p. 38
        public void ReadyManeuver(string source, int slotIndex) {
            var slot = GetKnownSlot(source, slotIndex);
            if (slot != null && slot.State == ManeuverState.Unreadied) {
                if (string.Equals(source, "Crusader", StringComparison.OrdinalIgnoreCase)) {
                    slot.State = ManeuverState.Withheld;
                } else {
                    slot.State = ManeuverState.Available;
                }
            }
        }

        // Source: Tome of Battle, p. 38
        public void UnreadyManeuver(string source, int slotIndex) {
            var slot = GetKnownSlot(source, slotIndex);
            if (slot != null && slot.State != ManeuverState.Unreadied) {
                slot.State = ManeuverState.Unreadied;
            }
        }

        // Source: Tome of Battle, pp. 38, 42-45
        public bool InitiateManeuver(string source, int slotIndex, bool throwOnInvalid = false) {
            var slot = GetKnownSlot(source, slotIndex);
            if (slot == null || slot.State != ManeuverState.Available) {
                if (throwOnInvalid) {
                    throw new InvalidOperationException($"Maneuver slot {slotIndex} in source '{source}' is not Available.");
                }
                return false;
            }

            slot.State = ManeuverState.Expended;

            var maneuver = !string.IsNullOrEmpty(slot.ManeuverName) ? catalog.GetManeuver(slot.ManeuverName) : null;
            if (maneuver != null && (maneuver.Type == ManeuverType.Boost || maneuver.Type == ManeuverType.Counter)) {
                if (context?.Timeline != null) {
                    context.Timeline.Schedule(new ManeuverSheetAction(maneuver.Name, source, "1 round"), "1 round", context);
                }
            }

            return true;
        }

        // Source: Tome of Battle, p. 40
        public void RecoverWarblade(string source = "Warblade") {
            if (state.Known.TryGetValue(source, out var slots) && slots != null) {
                foreach (var slot in slots) {
                    if (slot != null && slot.State == ManeuverState.Expended) {
                        slot.State = ManeuverState.Available;
                    }
                }
            }
        }

        // Source: Tome of Battle, p. 40
        public void RecoverSwordsage(string source, int slotIndex) {
            var slot = GetKnownSlot(source, slotIndex);
            if (slot != null && slot.State == ManeuverState.Expended) {
                slot.State = ManeuverState.Available;
            }
        }

        // Source: Tome of Battle, p. 40
        public void RecoverSwordsage(int slotIndex, string source = "Swordsage") {
            RecoverSwordsage(source, slotIndex);
        }

        // Source: Tome of Battle, pp. 38, 40
        public void DealCrusader(string source = "Crusader", Random rng = null) {
            var readied = GetReadiedManeuvers(source).ToList();
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
        }

        // Source: Tome of Battle, pp. 38, 40
        public void DealCrusader(Random rng) => DealCrusader("Crusader", rng);

        // Source: Tome of Battle, p. 38
        public void AdvanceCrusaderTurn(string source = "Crusader", Random rng = null) {
            rng = rng ?? new Random();
            var withheldSlots = GetKnownSlots(source).Where(s => s != null && s.State == ManeuverState.Withheld).ToList();

            if (withheldSlots.Count > 0) {
                int pickIndex = rng.Next(withheldSlots.Count);
                withheldSlots[pickIndex].State = ManeuverState.Available;
            } else {
                DealCrusader(source, rng);
            }
        }

        // Source: Tome of Battle, p. 38
        public void AdvanceCrusaderTurn(Random rng) => AdvanceCrusaderTurn("Crusader", rng);

        // Source: Tome of Battle, pp. 38, 40
        public void ResetEncounter(Random rng = null) {
            var allSources = GetKnownSources();
            foreach (var sourceKey in allSources) {
                if (string.Equals(sourceKey, "Crusader", StringComparison.OrdinalIgnoreCase)) {
                    DealCrusader(sourceKey, rng);
                } else {
                    if (state.Known.TryGetValue(sourceKey, out var slots) && slots != null) {
                        foreach (var slot in slots) {
                            if (slot != null && slot.IsReadied) {
                                slot.State = ManeuverState.Available;
                            }
                        }
                    }
                }
            }
        }
    }
}
