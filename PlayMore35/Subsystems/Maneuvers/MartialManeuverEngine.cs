using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.CharacterChoices;
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
            ProjectFeatSlots();
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
            ProjectFeatSlots();
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
            ProjectFeatSlots();
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
            ProjectFeatSlots();
            if (state.Known.TryGetValue(source, out var list) && slotIndex < list.Count) {
                return list[slotIndex];
            }
            return null;
        }

        public void SetKnownSlot(string source, int slotIndex, KnownManeuverSlot slot) {
            if (slotIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Slot index {slotIndex} must be non-negative.");
            }
            var existing = GetKnownSlot(source, slotIndex);
            if (existing != null && existing.IsFeatBound) {
                throw new InvalidOperationException($"Cannot retrain or replace feat-bound maneuver slot '{existing.ManeuverName}'.");
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
            var existing = GetKnownSlot(source, slotIndex);
            if (existing != null && existing.IsFeatBound) {
                throw new InvalidOperationException($"Cannot clear feat-bound maneuver slot '{existing.ManeuverName}'.");
            }
            GetKnownSlotGroup(source).ClearSlot(slotIndex);
        }

        public StanceSlot GetStanceSlot(string source, int slotIndex) {
            if (slotIndex < 0) return null;
            ProjectFeatSlots();
            if (state.Stances.TryGetValue(source, out var list) && slotIndex < list.Count) {
                return list[slotIndex];
            }
            return null;
        }

        public void SetStanceSlot(string source, int slotIndex, StanceSlot slot) {
            if (slotIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), $"Slot index {slotIndex} must be non-negative.");
            }
            var existing = GetStanceSlot(source, slotIndex);
            if (existing != null && existing.IsFeatBound) {
                throw new InvalidOperationException($"Cannot retrain or replace feat-bound stance slot '{existing.StanceName}'.");
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
            var existing = GetStanceSlot(source, slotIndex);
            if (existing != null && existing.IsFeatBound) {
                throw new InvalidOperationException($"Cannot clear feat-bound stance slot '{existing.StanceName}'.");
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
            ProjectFeatSlots();
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
            ProjectFeatSlots();
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

        public int GetInitiatorLevelAtLevel(string source, int level) {
            if (string.IsNullOrWhiteSpace(source)) return 0;
            var character = context?.Character;
            if (character == null) return 0;
            int cappedLevel = Math.Min(level, character.classLevels.Count);
            if (cappedLevel <= 0) return 0;
            var classesAtLevel = character.classLevels.Take(cappedLevel).ToList();

            if (string.Equals(source, "Feats", StringComparison.OrdinalIgnoreCase)) {
                return Math.Max(1, classesAtLevel.Count / 2);
            }

            int classLevels = classesAtLevel.Count(c => string.Equals(c, source, StringComparison.OrdinalIgnoreCase));
            if (classLevels == 0) return 0;
            int otherLevels = classesAtLevel.Count - classLevels;
            return classLevels + (otherLevels / 2);
        }

        public int GetMaxManeuverLevelAtLevel(string source, int level) {
            int il = GetInitiatorLevelAtLevel(source, level);
            if (il <= 0) return 0;
            return Math.Min(9, (il + 1) / 2);
        }

        public int CountKnownInDisciplineAtLevel(string discipline, int level, string currentFeatSlot = null, string excludingManeuverName = null, IDictionary<string, string> currentChoices = null) {
            if (string.IsNullOrWhiteSpace(discipline)) return 0;
            var character = context?.Character;
            if (character == null) return 0;

            var distinctKnown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int cappedLevel = Math.Min(level, character.classLevels.Count);
            var classesAtLevel = character.classLevels.Take(cappedLevel).ToList();

            // 1. Known from class progression
            foreach (var kvp in state.Known) {
                string sourceClass = kvp.Key;
                if (classesAtLevel.Any(c => string.Equals(c, sourceClass, StringComparison.OrdinalIgnoreCase))) {
                    foreach (var slot in kvp.Value) {
                        if (slot != null && !slot.IsFeatBound && !string.IsNullOrWhiteSpace(slot.ManeuverName)) {
                            distinctKnown.Add(slot.ManeuverName);
                        }
                    }
                }
            }
            foreach (var kvp in state.Stances) {
                string sourceClass = kvp.Key;
                if (classesAtLevel.Any(c => string.Equals(c, sourceClass, StringComparison.OrdinalIgnoreCase))) {
                    foreach (var slot in kvp.Value) {
                        if (slot != null && !slot.IsFeatBound && !string.IsNullOrWhiteSpace(slot.StanceName)) {
                            distinctKnown.Add(slot.StanceName);
                        }
                    }
                }
            }

            // 2. Known from feats at or before this level
            var session = context as ICharacterSession;
            if (character.feats != null) {
                foreach (var featKvp in character.feats) {
                    string slot = featKvp.Key;
                    if (string.Equals(slot, currentFeatSlot, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                    if (currentChoices != null && (currentChoices.ContainsKey($"{slot}:maneuver") || currentChoices.ContainsKey($"{slot}:stance"))) {
                        continue;
                    }
                    int slotLevel = session?.GetFeatSlotLevel(slot) ?? 0;
                    if (slotLevel > 0 && slotLevel <= level) {
                        if (character.featChoices != null) {
                            if (character.featChoices.TryGetValue($"{slot}:maneuver", out var mName) && !string.IsNullOrWhiteSpace(mName)) {
                                distinctKnown.Add(mName);
                            } else if (character.featChoices.TryGetValue($"{slot}:stance", out var sName) && !string.IsNullOrWhiteSpace(sName)) {
                                distinctKnown.Add(sName);
                            }
                        }
                    }
                }
            }

            if (currentChoices != null) {
                foreach (var kvp in currentChoices) {
                    string slot = null;
                    if (kvp.Key.EndsWith(":maneuver", StringComparison.OrdinalIgnoreCase)) {
                        slot = kvp.Key.Substring(0, kvp.Key.Length - ":maneuver".Length);
                    } else if (kvp.Key.EndsWith(":stance", StringComparison.OrdinalIgnoreCase)) {
                        slot = kvp.Key.Substring(0, kvp.Key.Length - ":stance".Length);
                    }
                    if (slot != null && !string.Equals(slot, currentFeatSlot, StringComparison.OrdinalIgnoreCase)) {
                        int slotLevel = session?.GetFeatSlotLevel(slot) ?? 0;
                        if (slotLevel > 0 && slotLevel <= level && !string.IsNullOrWhiteSpace(kvp.Value)) {
                            distinctKnown.Add(kvp.Value);
                        }
                    }
                }
            }

            int count = 0;
            foreach (var name in distinctKnown) {
                if (!string.Equals(name, excludingManeuverName, StringComparison.OrdinalIgnoreCase)) {
                    var m = catalog.GetManeuver(name);
                    if (m != null && string.Equals(m.Discipline, discipline, StringComparison.OrdinalIgnoreCase)) {
                        count++;
                    }
                }
            }
            return count;
        }

        public List<string> ValidateFeatManeuver(string featSlot, string maneuverName, string targetSource = null, IDictionary<string, string> currentChoices = null) {
            var warnings = new List<string>();
            if (string.IsNullOrWhiteSpace(maneuverName)) {
                return warnings;
            }

            var maneuver = catalog.GetManeuver(maneuverName);
            if (maneuver == null) {
                warnings.Add($"Maneuver '{maneuverName}' is not recognized in the rules catalog.");
                return warnings;
            }

            var session = context as ICharacterSession;
            int level = session?.GetFeatSlotLevel(featSlot) ?? 0;
            if (level <= 0) {
                level = context?.Character?.classLevels?.Count ?? 0;
            }

            string source = targetSource;
            if (string.IsNullOrWhiteSpace(source)) {
                source = session?.ResolveFeatTargetSource(featSlot) ?? "Feats";
            }

            // 1. Level cap check at that acquisition level
            int maxLevel = GetMaxManeuverLevelAtLevel(source, level);
            if (maneuver.Level > maxLevel) {
                warnings.Add($"Maneuver level {maneuver.Level} exceeds maximum maneuver level {maxLevel} for {source}.");
            }

            // 2. Discipline prerequisites check at that acquisition level
            int requiredDisciplineCount = maneuver.PrerequisiteCount;
            if (maneuver.Type == ManeuverType.Stance) {
                requiredDisciplineCount = Math.Max(1, requiredDisciplineCount);
            }
            if (requiredDisciplineCount > 0) {
                int knownDisciplineCount = CountKnownInDisciplineAtLevel(maneuver.Discipline, level, featSlot, maneuverName, currentChoices);
                if (knownDisciplineCount < requiredDisciplineCount) {
                    string plural = requiredDisciplineCount == 1 ? "maneuver" : "maneuvers";
                    warnings.Add($"Requires {requiredDisciplineCount} {maneuver.Discipline} {plural}, but only {knownDisciplineCount} known.");
                }
            }

            return warnings;
        }

        public IReadOnlyList<string> GetAccessibleElements(string filter, string targetSource, int level) {
            if (catalog?.Maneuvers == null) return new List<string>().AsReadOnly();
            bool isStance = string.Equals(filter, "stance", StringComparison.OrdinalIgnoreCase);
            bool isManeuver = string.Equals(filter, "maneuver", StringComparison.OrdinalIgnoreCase);
            if (!isStance && !isManeuver && !string.IsNullOrEmpty(filter)) {
                return new List<string>().AsReadOnly();
            }

            int slotLevel = level;
            if (slotLevel <= 0) {
                slotLevel = context?.Character?.classLevels?.Count ?? 0;
            }

            string source = targetSource;
            if (string.IsNullOrWhiteSpace(source)) {
                var session = context as ICharacterSession;
                source = session?.ResolveFeatTargetSource(null) ?? "Feats";
            }

            int maxLevel = GetMaxManeuverLevelAtLevel(source, slotLevel);

            return catalog.Maneuvers.Values
                .Where(m => isStance ? m.Type == ManeuverType.Stance : m.Type != ManeuverType.Stance)
                .Where(m => m.Level <= maxLevel)
                .OrderBy(m => m.Discipline)
                .ThenBy(m => m.Name)
                .Select(m => $"[{m.Discipline}] {m.Name}")
                .ToList()
                .AsReadOnly();
        }

        public List<string> ValidateChoice(string featSlot, ChoiceDescriptor descriptor, string choiceValue, string targetSource, IDictionary<string, string> currentChoices = null) {
            if (descriptor == null || string.IsNullOrWhiteSpace(choiceValue)) {
                return new List<string>();
            }
            if (string.Equals(descriptor.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase)) {
                if (string.Equals(descriptor.Filter, "maneuver", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(descriptor.Filter, "stance", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrEmpty(descriptor.Filter)) {
                    return ValidateFeatManeuver(featSlot, choiceValue, targetSource, currentChoices);
                }
            }
            return new List<string>();
        }

        public void ContributeEffects(EffectPool pool, ICharacterContext context) {
            if (pool == null) return;
            var ctx = context ?? this.context;
            if (ctx != null && this.context != ctx) {
                this.context = ctx;
            }
            ProjectFeatSlots();

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

        public void ProjectFeatSlots() {
            if (context?.Character == null) return;

            var featChoices = context.Character.featChoices;
            if (featChoices == null) return;

            var activeFeatManeuvers = new Dictionary<string, (string FeatSlot, string ManeuverName)>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in featChoices) {
                if (kvp.Key.EndsWith(":maneuver", StringComparison.OrdinalIgnoreCase)) {
                    string featSlot = kvp.Key.Substring(0, kvp.Key.Length - ":maneuver".Length);
                    if (!string.IsNullOrWhiteSpace(kvp.Value)) {
                        activeFeatManeuvers[featSlot] = (featSlot, kvp.Value);
                    }
                }
            }

            // Remove any slots in state.Known that are feat-bound to a featSlot no longer active
            foreach (var kvp in state.Known) {
                for (int i = kvp.Value.Count - 1; i >= 0; i--) {
                    var slot = kvp.Value[i];
                    if (slot != null && slot.IsFeatBound && !string.IsNullOrEmpty(slot.FeatSlot)) {
                        if (!activeFeatManeuvers.ContainsKey(slot.FeatSlot)) {
                            kvp.Value.RemoveAt(i);
                        }
                    }
                }
            }

            // Project each active feat maneuver
            foreach (var item in activeFeatManeuvers.Values) {
                string featSlot = item.FeatSlot;
                string maneuverName = item.ManeuverName;
                string targetSource = ResolveTargetSource(featSlot);

                var targetList = state.GetOrCreateKnown(targetSource);

                KnownManeuverSlot existingSlot = null;
                string existingSource = null;
                int existingIndex = -1;

                foreach (var kvp in state.Known) {
                    for (int i = 0; i < kvp.Value.Count; i++) {
                        var slot = kvp.Value[i];
                        if (slot != null && slot.IsFeatBound && string.Equals(slot.FeatSlot, featSlot, StringComparison.OrdinalIgnoreCase)) {
                            existingSlot = slot;
                            existingSource = kvp.Key;
                            existingIndex = i;
                            break;
                        }
                    }
                    if (existingSlot != null) break;
                }

                int totalCapacity = context?.GetStatValue($"maneuvers known:{targetSource.ToLowerInvariant()}", 0) ?? 0;
                int totalFeatsForSource = activeFeatManeuvers.Values.Count(f => string.Equals(ResolveTargetSource(f.FeatSlot), targetSource, StringComparison.OrdinalIgnoreCase));
                int baseClassCapacity = Math.Max(0, totalCapacity - totalFeatsForSource);

                if (existingSlot != null) {
                    if (string.Equals(existingSource, targetSource, StringComparison.OrdinalIgnoreCase)) {
                        existingSlot.ManeuverName = maneuverName;
                        existingSlot.IsFeatBound = true;
                        existingSlot.FeatSlot = featSlot;

                        if (existingIndex < baseClassCapacity && targetList.All(s => s == null || s.IsFeatBound)) {
                            targetList.RemoveAt(existingIndex);
                            while (targetList.Count < baseClassCapacity) {
                                targetList.Add(null);
                            }
                            targetList.Add(existingSlot);
                        }
                    } else {
                        state.Known[existingSource].RemoveAt(existingIndex);
                        existingSlot.ManeuverName = maneuverName;
                        existingSlot.IsFeatBound = true;
                        existingSlot.FeatSlot = featSlot;
                        InsertOrAppendFeatSlot(targetSource, targetList, existingSlot, baseClassCapacity);
                    }
                } else {
                    var newSlot = new KnownManeuverSlot(maneuverName, ManeuverState.Unreadied, isFeatBound: true, featSlot: featSlot);
                    InsertOrAppendFeatSlot(targetSource, targetList, newSlot, baseClassCapacity);
                }
            }

            var activeFeatStances = new Dictionary<string, (string FeatSlot, string StanceName)>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in featChoices) {
                if (kvp.Key.EndsWith(":stance", StringComparison.OrdinalIgnoreCase)) {
                    string featSlot = kvp.Key.Substring(0, kvp.Key.Length - ":stance".Length);
                    if (!string.IsNullOrWhiteSpace(kvp.Value)) {
                        activeFeatStances[featSlot] = (featSlot, kvp.Value);
                    }
                }
            }

            // Remove any slots in state.Stances that are feat-bound to a featSlot no longer active
            foreach (var kvp in state.Stances) {
                for (int i = kvp.Value.Count - 1; i >= 0; i--) {
                    var slot = kvp.Value[i];
                    if (slot != null && slot.IsFeatBound && !string.IsNullOrEmpty(slot.FeatSlot)) {
                        if (!activeFeatStances.ContainsKey(slot.FeatSlot)) {
                            kvp.Value.RemoveAt(i);
                        }
                    }
                }
            }

            // Project each active feat stance
            foreach (var item in activeFeatStances.Values) {
                string featSlot = item.FeatSlot;
                string stanceName = item.StanceName;
                string targetSource = ResolveTargetSource(featSlot);

                var targetList = state.GetOrCreateStances(targetSource);

                StanceSlot existingSlot = null;
                string existingSource = null;
                int existingIndex = -1;

                foreach (var kvp in state.Stances) {
                    for (int i = 0; i < kvp.Value.Count; i++) {
                        var slot = kvp.Value[i];
                        if (slot != null && slot.IsFeatBound && string.Equals(slot.FeatSlot, featSlot, StringComparison.OrdinalIgnoreCase)) {
                            existingSlot = slot;
                            existingSource = kvp.Key;
                            existingIndex = i;
                            break;
                        }
                    }
                    if (existingSlot != null) break;
                }

                int totalCapacity = context?.GetStatValue($"stances known:{targetSource.ToLowerInvariant()}", 0) ?? 0;
                int totalFeatsForSource = activeFeatStances.Values.Count(f => string.Equals(ResolveTargetSource(f.FeatSlot), targetSource, StringComparison.OrdinalIgnoreCase));
                int baseClassCapacity = Math.Max(0, totalCapacity - totalFeatsForSource);

                if (existingSlot != null) {
                    if (string.Equals(existingSource, targetSource, StringComparison.OrdinalIgnoreCase)) {
                        existingSlot.StanceName = stanceName;
                        existingSlot.IsFeatBound = true;
                        existingSlot.FeatSlot = featSlot;

                        if (existingIndex < baseClassCapacity && targetList.All(s => s == null || s.IsFeatBound)) {
                            targetList.RemoveAt(existingIndex);
                            while (targetList.Count < baseClassCapacity) {
                                targetList.Add(null);
                            }
                            targetList.Add(existingSlot);
                        }
                    } else {
                        state.Stances[existingSource].RemoveAt(existingIndex);
                        existingSlot.StanceName = stanceName;
                        existingSlot.IsFeatBound = true;
                        existingSlot.FeatSlot = featSlot;
                        InsertOrAppendFeatStanceSlot(targetSource, targetList, existingSlot, baseClassCapacity);
                    }
                } else {
                    var newSlot = new StanceSlot(stanceName, isActive: false, isFeatBound: true, featSlot: featSlot);
                    InsertOrAppendFeatStanceSlot(targetSource, targetList, newSlot, baseClassCapacity);
                }
            }
        }

        private void InsertOrAppendFeatSlot(string targetSource, List<KnownManeuverSlot> targetList, KnownManeuverSlot featSlot, int baseClassCapacity) {
            while (targetList.Count < baseClassCapacity) {
                targetList.Add(null);
            }
            targetList.Add(featSlot);
        }

        private void InsertOrAppendFeatStanceSlot(string targetSource, List<StanceSlot> targetList, StanceSlot featSlot, int baseClassCapacity) {
            while (targetList.Count < baseClassCapacity) {
                targetList.Add(null);
            }
            targetList.Add(featSlot);
        }

        private string ResolveTargetSource(string featSlot) {
            if (context is ICharacterSession session) {
                return session.ResolveFeatTargetSource(featSlot);
            }

            if (context?.Character?.featChoices != null &&
                context.Character.featChoices.TryGetValue($"{featSlot}:class", out var explicitClass) &&
                !string.IsNullOrWhiteSpace(explicitClass)) {
                return explicitClass;
            }

            if (context?.Character?.classLevels != null && catalog != null) {
                var adept = context.Character.classLevels.FirstOrDefault(c => {
                    var classDef = catalog.GetClass(c);
                    return classDef != null && classDef.Subsystems.ContainsKey(SubsystemId);
                });
                if (!string.IsNullOrEmpty(adept)) return adept;
            }

            return "Feats";
        }
    }
}
