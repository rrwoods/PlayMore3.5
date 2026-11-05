using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Subsystems {
    public class SubsystemEngine {
        private readonly Dictionary<string, SubsystemSlotGroup<SubsystemSlot>> slotGroups = new Dictionary<string, SubsystemSlotGroup<SubsystemSlot>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> poolValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly List<SubsystemSlot> activeSlotOrder = new List<SubsystemSlot>();
        private readonly ICharacterContext context;

        public SubsystemDefinition Definition { get; }
        public string Id => Definition.Id;
        public string Name => Definition.Name;
        public bool IsActive {
            get {
                if (slotGroups.Values.Any(g => g.Capacity > 0)) return true;
                if (Definition.SlotGroups.Keys.Any(k => GetSlotCapacity(k) > 0)) return true;
                if (slotGroups.Values.Any(g => g.RawSlots.Any(s => !string.IsNullOrEmpty(s.Element)))) return true;
                if (Definition.Pools.Keys.Any(k => GetPoolCapacity(k) > 0)) return true;
                if (GetActiveSources().Count > 0) return true;
                return false;
            }
        }
        public Random Rng { get; private set; }

        public SubsystemEngine(SubsystemDefinition definition, ICharacterContext context, Random? rng = null) {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            Rng = rng ?? new Random();
            InitializeStorage();

            if (this.context.Character != null) {
                if (this.context.Character.Subsystems.TryGetValue(Id, out var state) && state != null) {
                    LoadState(state);
                }
            }
            foreach (var kvp in Definition.Pools) {
                if (!poolValues.ContainsKey(kvp.Key)) {
                    int cap = GetPoolCapacity(kvp.Key);
                    poolValues[kvp.Key] = cap;
                }
            }
            if (this.context.Character != null && (IsActive || this.context.Character.Subsystems.ContainsKey(Id))) {
                SyncState();
            }
        }

        public SubsystemEngine(SubsystemDefinition definition, ICharacterContext context)
            : this(definition, context, new Random()) { }

        public void SetRandom(Random rng) {
            Rng = rng ?? new Random();
        }

        private void InitializeStorage() {
            slotGroups.Clear();
            activeSlotOrder.Clear();
            foreach (var kvp in Definition.SlotGroups) {
                var groupDef = kvp.Value;
                string groupKey = kvp.Key;
                var group = new SubsystemSlotGroup<SubsystemSlot>(
                    capacityProvider: () => ResolveCapacity(groupDef, null),
                    slotFactory: () => new SubsystemSlot("", "", "")
                );
                slotGroups[groupKey] = group;
            }

            poolValues.Clear();
            foreach (var kvp in Definition.Pools) {
                var poolDef = kvp.Value;
                int cap = poolDef.DefaultCapacity;
                poolValues[kvp.Key] = cap;
            }
        }

        public List<string> GetActiveSources(EffectPool? pool = null) {
            var sources = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddSource(string? src) {
                if (string.IsNullOrWhiteSpace(src)) return;
                string trimmed = src!.Trim();
                if (seen.Add(trimmed)) {
                    sources.Add(trimmed);
                }
            }

            if (context.Catalog != null) {
                var candidateClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (context.Character != null) {
                    foreach (var c in context.Character.classLevels) candidateClasses.Add(c);
                }
                var effPool = pool ?? context.Effects;
                if (effPool != null) {
                    foreach (var cls in context.Catalog.Classes.Values) {
                        if (effPool.GetStatValue($"{cls.Name} level", null, 0) > 0) {
                            candidateClasses.Add(cls.Name);
                        }
                    }
                }

                foreach (var clsName in candidateClasses) {
                    if (context.Catalog.TryGetClass(clsName, out var cls) && cls.Subsystems != null && cls.Subsystems.ContainsKey(Id)) {
                        AddSource(cls.Name);
                    }
                }

                if (context.Character != null) {
                    foreach (var featKvp in context.Character.feats) {
                        string featSlot = featKvp.Key;
                        string featName = featKvp.Value;
                        if (context.Catalog.TryGetFeat(featName, out var feat) && feat.Subsystems != null && feat.Subsystems.Contains(Id)) {
                            string src = ResolveFeatTargetSource(featSlot);
                            AddSource(src);
                        }
                    }
                }
            }

            return sources;
        }

        private int ResolveCapacity(SlotGroupDefinition? def, string? source = null) {
            if (def == null) return 0;
            if (!string.IsNullOrEmpty(def.CapacityStat)) {
                if (def.CapacityStat.Contains("{source}")) {
                    if (!string.IsNullOrEmpty(source)) {
                        string statName = def.CapacityStat.Replace("{source}", source!.Trim().ToLowerInvariant());
                        return ResolveStatOrParam(statName, def.FixedCapacity);
                    }
                    var sources = GetActiveSources(null);
                    if (sources.Count > 0) {
                        return sources.Sum(s => ResolveStatOrParam(def.CapacityStat.Replace("{source}", s.Trim().ToLowerInvariant()), 0));
                    }
                    if (context?.Effects != null) {
                        int prefixIdx = def.CapacityStat.IndexOf("{source}", StringComparison.OrdinalIgnoreCase);
                        if (prefixIdx > 0) {
                            string prefix = def.CapacityStat.Substring(0, prefixIdx);
                            return context.Effects.stats.Keys
                                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                .Sum(k => ResolveStatOrParam(k, 0));
                        }
                    }
                    return def.FixedCapacity;
                }
                return ResolveStatOrParam(def.CapacityStat, def.FixedCapacity);
            }
            return def.FixedCapacity;
        }

        private int ResolveMaxLevel(SlotGroupDefinition? def, string? source = null) {
            if (def?.MaxLevel == null || string.IsNullOrEmpty(def.MaxLevel.Stat)) {
                return def?.MaxLevel != null ? def.MaxLevel.Base : int.MaxValue;
            }

            if (def.MaxLevel.Stat.Contains("{source}")) {
                if (!string.IsNullOrEmpty(source)) {
                    string statName = def.MaxLevel.Stat.Replace("{source}", source!.Trim().ToLowerInvariant());
                    return EvaluateMaxLevelStat(def, statName, source!);
                }
                var sources = GetActiveSources(null);
                if (sources.Count == 0) return def.MaxLevel.Base;
                return sources.Max(s => ResolveMaxLevel(def, s));
            }

            return EvaluateMaxLevelStat(def, def.MaxLevel.Stat, null);
        }

        private int EvaluateMaxLevelStat(SlotGroupDefinition def, string statName, string? source) {
            bool hasDirectStat = context.Effects != null && context.Effects.stats.ContainsKey(statName);
            if (hasDirectStat) {
                return ResolveStatOrParam(statName, def.MaxLevel.Base);
            }

            var allEffects = new List<Effect>();
            foreach (var g in Definition.SlotGroups.Values) {
                allEffects.AddRange(g.GetEffectsForSource(source));
            }
            if (context.Catalog != null) {
                foreach (var featDef in context.Catalog.Feats.Values) {
                    if (featDef.Subsystems != null && featDef.Subsystems.Contains(Id) && featDef.Benefit != null) {
                        allEffects.Add(featDef.Benefit.ApplyChoice(source ?? "Feats"));
                    }
                }
            }

            if (allEffects.Count > 0) {
                var tempPool = new EffectPool(context.Character ?? new Character());
                tempPool.AddStat(new Statistic(statName));
                tempPool.AddEffect(new Effect($"{statName} Base")
                    .AddModifier(statName, new Modifier(def.MaxLevel.Base, ModifierTypes.BASE)));

                var allStats = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { statName };
                foreach (var eff in allEffects) {
                    var json = eff.ToJson();
                    foreach (var token in json.SelectTokens("$..['stat value']")) {
                        string s = (string)token!;
                        if (!string.IsNullOrEmpty(s)) allStats.Add(s);
                    }
                    foreach (var token in json.SelectTokens("$..affects")) {
                        string s = (string)token!;
                        if (!string.IsNullOrEmpty(s)) allStats.Add(s);
                    }
                }

                foreach (var s in allStats) {
                    if (!tempPool.stats.ContainsKey(s)) {
                        int refVal = ResolveStatOrParam(s, 0);
                        if (s.Equals("character level", StringComparison.OrdinalIgnoreCase) && refVal == 0) {
                            refVal = context.Character?.classLevels?.Count ?? 0;
                        }
                        tempPool.AddStat(new Statistic(s));
                        if (refVal != 0) {
                            tempPool.AddEffect(new Effect($"{s} Base")
                                .AddModifier(s, new Modifier(refVal, ModifierTypes.BASE)));
                        }
                    }
                }

                foreach (var eff in allEffects) {
                    tempPool.AddEffect(eff);
                }
                return tempPool.GetStatistic(statName, context.EquippedItems)?.Value ?? def.MaxLevel.Base;
            }

            return ResolveStatOrParam(statName, def.MaxLevel.Base);
        }

        public int GetMaxLevel(string groupName, string? source = null) {
            if (string.IsNullOrEmpty(groupName) || !Definition.SlotGroups.TryGetValue(groupName, out var def)) return int.MaxValue;
            return ResolveMaxLevel(def, source);
        }

        private int ResolveStatOrParam(string? name, int defaultValue) {
            if (string.IsNullOrEmpty(name)) return defaultValue;
            if (context.Effects != null && context.Effects.stats.ContainsKey(name)) {
                return context.GetStatValue(name, defaultValue);
            }
            if (context.Effects != null && context.Effects.controllableParameters.TryGetValue(name, out var p)) {
                return p.Value;
            }
            if (context.Character != null && context.Character.paramValues.TryGetValue(name, out var pv)) {
                return pv;
            }
            return context.GetStatValue(name, defaultValue);
        }

        public SubsystemElement? GetElement(string elementName) {
            if (string.IsNullOrEmpty(elementName)) return null;
            if (Definition.Elements.TryGetValue(elementName, out var elem)) {
                return elem;
            }
            if (context.Catalog is Rules.IRulesCatalog cat) {
                if (cat.TryGetSubsystem(Id, out var subDef) && subDef.Elements.TryGetValue(elementName, out var catalogElem)) {
                    return catalogElem;
                }
            }
            return null;
        }

        public void LoadState(SubsystemState state) {
            if (state == null) return;
            activeSlotOrder.Clear();

            foreach (var kvp in state.Slots) {
                var group = GetSlotGroup(kvp.Key);
                if (group != null) {
                    group.ClearAll();
                    for (int i = 0; i < kvp.Value.Count; i++) {
                        var s = kvp.Value[i];
                        if (s != null) {
                            var copy = new SubsystemSlot(s.Element, s.State, s.Source, s.FeatSlot);
                            group[i] = copy;
                            if (string.Equals(copy.State, "active", StringComparison.OrdinalIgnoreCase)) {
                                activeSlotOrder.Add(copy);
                            }
                        }
                    }
                }
            }

            foreach (var kvp in state.Pools) {
                poolValues[kvp.Key] = kvp.Value;
            }
        }

        public SubsystemState ExportState() {
            var state = new SubsystemState();
            foreach (var kvp in slotGroups) {
                var groupKey = kvp.Key;
                var group = kvp.Value;
                var slots = group.RawSlots;
                int lastIndex = -1;
                for (int i = slots.Count - 1; i >= 0; i--) {
                    var s = slots[i];
                    if (!string.IsNullOrEmpty(s.Element) || !string.IsNullOrEmpty(s.State) || !string.IsNullOrEmpty(s.Source)) {
                        lastIndex = i;
                        break;
                    }
                }
                var list = new List<SubsystemSlot>();
                for (int i = 0; i <= lastIndex; i++) {
                    var s = slots[i];
                    list.Add(new SubsystemSlot(s.Element, s.State, s.Source, s.FeatSlot));
                }
                state.Slots[groupKey] = list;
            }

            foreach (var kvp in Definition.Pools) {
                state.Pools[kvp.Key] = GetPoolValue(kvp.Key);
            }
            foreach (var kvp in poolValues) {
                if (!state.Pools.ContainsKey(kvp.Key)) {
                    state.Pools[kvp.Key] = kvp.Value;
                }
            }

            return state;
        }

        public void SyncState() {
            if (context?.Character != null) {
                if (IsActive || context.Character.Subsystems.ContainsKey(Id)) {
                    context.Character.Subsystems[Id] = ExportState();
                }
            }
        }

        public void TrimSlotsToCapacity() {
            foreach (var kvp in slotGroups) {
                string groupKey = kvp.Key;
                var group = kvp.Value;
                var sourceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int unpartitionedCount = 0;
                int totalCap = GetSlotCapacity(groupKey, null);
                var slotsToKeep = new List<SubsystemSlot>();

                foreach (var slot in group.RawSlots) {
                    if (slot.IsFeatBound) {
                        slotsToKeep.Add(slot);
                    } else if (!string.IsNullOrEmpty(slot.Source)) {
                        int cap = GetSlotCapacity(groupKey, slot.Source);
                        sourceCounts.TryGetValue(slot.Source, out int count);
                        if (count < cap) {
                            slotsToKeep.Add(slot);
                            sourceCounts[slot.Source] = count + 1;
                        }
                    } else {
                        if (unpartitionedCount < totalCap) {
                            slotsToKeep.Add(slot);
                            unpartitionedCount++;
                        }
                    }
                }

                group.ClearAll();
                for (int i = 0; i < slotsToKeep.Count; i++) {
                    group[i] = slotsToKeep[i];
                }
            }
            SyncState();
        }

        private string GetDefaultState(string groupName) {
            if (!string.IsNullOrEmpty(groupName) && Definition.SlotGroups.TryGetValue(groupName, out var def) && !string.IsNullOrEmpty(def.DefaultState)) {
                return def.DefaultState;
            }
            return "available";
        }

        private List<int> EnsureSourceSlots(string groupName, string source, int minCount = 0) {
            var group = GetSlotGroup(groupName);
            if (group == null || string.IsNullOrEmpty(source)) return new List<int>();

            var indices = new List<int>();
            var currentSlots = group.RawSlots;
            for (int i = 0; i < currentSlots.Count; i++) {
                var s = currentSlots[i];
                if (string.Equals(s.Source, source, StringComparison.OrdinalIgnoreCase)) {
                    indices.Add(i);
                }
            }

            int targetCount = Math.Max(minCount, GetSlotCapacity(groupName, source));
            string defaultState = GetDefaultState(groupName);

            while (indices.Count < targetCount) {
                int flatIdx = -1;
                var slots = group.RawSlots;
                for (int i = 0; i < slots.Count; i++) {
                    if (string.IsNullOrEmpty(slots[i].Element) && string.IsNullOrEmpty(slots[i].Source)) {
                        flatIdx = i;
                        break;
                    }
                }
                if (flatIdx == -1) {
                    flatIdx = slots.Count;
                }
                group[flatIdx] = new SubsystemSlot("", defaultState, source);
                indices.Add(flatIdx);
            }

            return indices;
        }

        private bool IsSourcePartitionedGroup(string groupName) {
            return !string.IsNullOrEmpty(groupName) &&
                   Definition.SlotGroups.TryGetValue(groupName, out var def) &&
                   !string.IsNullOrEmpty(def.CapacityStat) &&
                   def.CapacityStat.Contains("{source}");
        }

        private int ResolveFlatSlotIndex(string groupName, int index, string? source, bool createIfMissing) {
            if (index < 0) return -1;
            var group = GetSlotGroup(groupName);
            if (group == null) return -1;
            if (string.IsNullOrEmpty(source) || !IsSourcePartitionedGroup(groupName)) {
                return index;
            }
            var indices = EnsureSourceSlots(groupName, source, createIfMissing ? index + 1 : 0);
            if (index < indices.Count) {
                return indices[index];
            }
            // Fallback if slots were assigned without source tag in single-source setup
            if (indices.Count == 0 && index < group.Slots.Count) {
                var s = group.Slots[index];
                if (string.IsNullOrEmpty(s.Source)) {
                    return index;
                }
            }
            return -1;
        }

        public void SetSlotState(string groupName, int index, string state, string? source = null) {
            var slot = GetSlot(groupName, index, source);
            if (slot != null) {
                string priorState = slot.State;
                slot.State = state ?? "";
                if (string.Equals(slot.State, "empty", StringComparison.OrdinalIgnoreCase)) {
                    ResetElementParameters(slot.Element);
                    slot.Element = "";
                } else if (!string.Equals(priorState, slot.State, StringComparison.OrdinalIgnoreCase)) {
                    ResetDeactivatedParameters(slot.Element, groupName, priorState, slot.State);
                }
                SyncState();
            }
        }

        public SubsystemSlotGroup<SubsystemSlot>? GetSlotGroup(string groupName) {
            if (string.IsNullOrEmpty(groupName)) return null;
            return slotGroups.TryGetValue(groupName, out var group) ? group : null;
        }

        public IReadOnlyList<SubsystemSlot> GetSlots(string groupName, string? source = null) {
            var group = GetSlotGroup(groupName);
            if (group == null) return new List<SubsystemSlot>();
            if (!string.IsNullOrEmpty(source)) {
                var indices = EnsureSourceSlots(groupName, source);
                return indices.Select(i => group[i]).ToList();
            }
            if (Definition.SlotGroups.TryGetValue(groupName, out var def) && !string.IsNullOrEmpty(def.CapacityStat) && def.CapacityStat.Contains("{source}")) {
                foreach (var src in GetActiveSources(null)) {
                    EnsureSourceSlots(groupName, src);
                }
            }
            return group.Slots;
        }

        public SubsystemSlot? GetSlot(string groupName, int index, string? source = null) {
            var group = GetSlotGroup(groupName);
            if (group == null || index < 0) return null;
            int flatIndex = ResolveFlatSlotIndex(groupName, index, source, createIfMissing: false);
            if (flatIndex < 0 || flatIndex >= group.Slots.Count) return null;
            return group[flatIndex];
        }

        public int GetSlotCapacity(string groupName, string? source = null) {
            if (string.IsNullOrEmpty(groupName) || !Definition.SlotGroups.TryGetValue(groupName, out var def)) return 0;
            if (string.IsNullOrEmpty(source) && (string.IsNullOrEmpty(def.CapacityStat) || !def.CapacityStat.Contains("{source}"))) {
                return GetSlotGroup(groupName)?.Capacity ?? 0;
            }
            return Math.Max(0, ResolveCapacity(def, source));
        }

        public string? GetSlotElement(string groupName, int index, string? source = null) {
            var slot = GetSlot(groupName, index, source);
            return string.IsNullOrEmpty(slot?.Element) ? null : slot.Element;
        }

        public string GetSlotState(string groupName, int index, string? source = null) {
            var slot = GetSlot(groupName, index, source);
            return string.IsNullOrEmpty(slot?.State) ? "empty" : slot.State;
        }

        public bool IsSlotLevelValid(string groupName, int slotIndex, string? source = null) {
            var slot = GetSlot(groupName, slotIndex, source);
            if (slot == null || string.IsNullOrEmpty(slot.Element)) return true;

            if (Definition.LevelGating == null || string.IsNullOrEmpty(Definition.LevelGating.ElementStatistic)) return true;
            if (!Definition.SlotGroups.TryGetValue(groupName, out var groupDef)) return true;
            if (groupDef.MaxLevel == null || string.IsNullOrEmpty(groupDef.MaxLevel.Stat)) return true;

            var elem = GetElement(slot.Element);
            if (elem == null) return true;

            string effectiveSource = !string.IsNullOrEmpty(source) ? source : slot.Source;
            int elementLevel = elem.GetStatistic(Definition.LevelGating.ElementStatistic, context).Value;
            int maxLevel = ResolveMaxLevel(groupDef, effectiveSource);
            return elementLevel <= maxLevel;
        }

        public void ResetElementParameters(string elementName) {
            if (string.IsNullOrEmpty(elementName)) return;
            var elem = GetElement(elementName);
            if (elem == null) return;
            foreach (var paramKey in Definition.GetElementParameterNames(elem)) {
                if (context?.Character != null) {
                    context.Character.paramValues[paramKey] = 0;
                }
                if (context?.Effects?.controllableParameters != null &&
                    context.Effects.controllableParameters.TryGetValue(paramKey, out var param)) {
                    param.Value = 0;
                }
            }
        }

        public void ResetCategoryParameters(string elementName, string category) {
            if (string.IsNullOrEmpty(elementName) || string.IsNullOrEmpty(category)) return;
            var elem = GetElement(elementName);
            if (elem == null) return;
            foreach (var paramKey in Definition.GetCategoryParameterNames(elem, category)) {
                if (context?.Character != null) {
                    context.Character.paramValues[paramKey] = 0;
                }
                if (context?.Effects?.controllableParameters != null &&
                    context.Effects.controllableParameters.TryGetValue(paramKey, out var param)) {
                    param.Value = 0;
                }
            }
        }

        private void ResetDeactivatedParameters(string elementName, string slotGroup, string priorState, string newState) {
            if (string.IsNullOrEmpty(elementName)) return;
            var priorCategories = Definition.GetProjectedCategories(slotGroup, priorState);
            var newCategories = Definition.GetProjectedCategories(slotGroup, newState);
            foreach (var cat in priorCategories) {
                if (!newCategories.Contains(cat)) {
                    ResetCategoryParameters(elementName, cat);
                }
            }
        }

        public void AssignSlot(string groupName, int index, string element, string? state = null, string? source = null, string? featSlot = null) {
            var group = GetSlotGroup(groupName);
            if (group == null) throw new ArgumentException($"Slot group '{groupName}' not found in subsystem '{Id}'.");
            string targetState = !string.IsNullOrEmpty(state) ? state : GetDefaultState(groupName);

            int flatIndex = ResolveFlatSlotIndex(groupName, index, source, createIfMissing: true);
            var existing = (flatIndex >= 0 && flatIndex < group.Slots.Count) ? group[flatIndex] : null;
            if (existing != null && existing.IsFeatBound && string.IsNullOrEmpty(featSlot)) {
                throw new InvalidOperationException($"Cannot overwrite feat-bound slot at index {index} (bound to feat '{existing.FeatSlot}').");
            }
            if (existing != null && !string.IsNullOrEmpty(existing.Element)) {
                ResetElementParameters(existing.Element);
                activeSlotOrder.Remove(existing);
            }
            ResetElementParameters(element);
            var newSlot = new SubsystemSlot(element, targetState, source ?? existing?.Source, featSlot);
            group[flatIndex] = newSlot;
            if (string.Equals(targetState, "active", StringComparison.OrdinalIgnoreCase)) {
                activeSlotOrder.Add(newSlot);
            }
            SyncState();
        }

        public void ClearSlot(string groupName, int index, string? source = null) {
            var group = GetSlotGroup(groupName);
            if (group == null || index < 0) return;
            int flatIndex = ResolveFlatSlotIndex(groupName, index, source, createIfMissing: false);
            if (flatIndex >= 0 && flatIndex < group.Slots.Count) {
                var existing = group[flatIndex];
                if (existing != null) {
                    if (existing.IsFeatBound) {
                        throw new InvalidOperationException($"Cannot clear feat-bound slot at index {index} (bound to feat '{existing.FeatSlot}').");
                    }
                    if (!string.IsNullOrEmpty(existing.Element)) {
                        ResetElementParameters(existing.Element);
                    }
                    activeSlotOrder.Remove(existing);
                    if (!string.IsNullOrEmpty(existing.Source) && IsSourcePartitionedGroup(groupName)) {
                        group[flatIndex] = new SubsystemSlot("", GetDefaultState(groupName), existing.Source);
                        SyncState();
                        return;
                    }
                }
                group.ClearSlot(flatIndex);
            }
            SyncState();
        }

        public int GetPoolCapacity(string poolId) {
            if (string.IsNullOrEmpty(poolId) || !Definition.Pools.TryGetValue(poolId, out var def)) return 0;
            if (context != null && !string.IsNullOrEmpty(def.CapacityStat)) {
                return ResolveStatOrParam(def.CapacityStat, def.DefaultCapacity);
            }
            return def.DefaultCapacity;
        }

        public int GetPoolValue(string poolId) {
            if (string.IsNullOrEmpty(poolId)) return 0;
            int cap = GetPoolCapacity(poolId);
            int current = poolValues.TryGetValue(poolId, out var val) ? val : cap;
            return Math.Min(current, cap);
        }

        public void SetPoolValue(string poolId, int value) {
            if (string.IsNullOrEmpty(poolId)) return;
            int cap = GetPoolCapacity(poolId);
            poolValues[poolId] = Math.Max(0, Math.Min(value, cap));
            SyncState();
        }

        public void ConsumePool(string poolId, int amount) {
            if (string.IsNullOrEmpty(poolId) || amount <= 0) return;
            int current = GetPoolValue(poolId);
            SetPoolValue(poolId, current - amount);
        }

        public void RechargePool(string poolId, int amount) {
            if (string.IsNullOrEmpty(poolId) || amount <= 0) return;
            int current = GetPoolValue(poolId);
            SetPoolValue(poolId, current + amount);
        }

        public TimelineRefreshRequirement ExecutePoolOperation(SubsystemPoolOperation? operation) {
            if (operation == null || string.IsNullOrEmpty(operation.Pool)) return TimelineRefreshRequirement.None;
            string op = operation.Op?.ToLowerInvariant() ?? "consume";
            switch (op) {
                case "consume":
                    ConsumePool(operation.Pool, operation.Amount);
                    break;
                case "recharge":
                    RechargePool(operation.Pool, operation.Amount);
                    break;
                case "set":
                    SetPoolValue(operation.Pool, operation.Amount);
                    break;
            }
            return TimelineRefreshRequirement.RecomputeDerived;
        }

        private static bool MatchesState(string slotState, string fromState) {
            if (string.IsNullOrEmpty(fromState) || fromState == "*") return true;
            if (fromState.StartsWith("!")) {
                string excluded = fromState.Substring(1);
                return !string.Equals(slotState, excluded, StringComparison.OrdinalIgnoreCase);
            }
            return string.Equals(slotState, fromState, StringComparison.OrdinalIgnoreCase);
        }

        private List<SubsystemSlot> GetMatchingSlots(SubsystemSlotGroup<SubsystemSlot> group, string fromState, string? targetSource) {
            var result = new List<SubsystemSlot>();
            for (int i = 0; i < group.Slots.Count; i++) {
                var slot = group.Slots[i];
                if (string.IsNullOrEmpty(slot.Element)) continue;
                if (!string.IsNullOrEmpty(targetSource) && !string.Equals(slot.Source, targetSource, StringComparison.OrdinalIgnoreCase)) continue;
                if (MatchesState(slot.State, fromState)) {
                    result.Add(slot);
                }
            }
            return result;
        }

        public TimelineRefreshRequirement ExecuteTransition(SubsystemTransition transition, SubsystemTarget target) {
            return ExecuteTransition(transition, target?.SlotIndex, target?.Element, target?.Source);
        }

        public TimelineRefreshRequirement ExecuteTransition(SubsystemTransition transition, int? targetSlotIndex = null, string? targetElement = null, string? targetSource = null) {
            if (transition == null || string.IsNullOrEmpty(transition.SlotGroup)) return TimelineRefreshRequirement.None;
            var group = GetSlotGroup(transition.SlotGroup);
            if (group == null) return TimelineRefreshRequirement.None;

            string selector = transition.Selector?.ToLowerInvariant() ?? "invoked";
            string from = transition.FromState ?? "";
            string to = transition.ToState ?? "";
            Definition.SlotGroups.TryGetValue(transition.SlotGroup, out var groupDef);
            bool anyTransitioned = false;

            bool MatchesFrom(SubsystemSlot slot) {
                if (string.IsNullOrEmpty(slot.Element)) return false;
                return MatchesState(slot.State, from);
            }

            int rawIndex = transition.SlotIndex ?? targetSlotIndex ?? -1;

            void TransitionSlot(SubsystemSlot slot) {
                string priorState = slot.State;
                string elemName = slot.Element;

                var activeCap = groupDef?.StateCapacities.FirstOrDefault(c => c.States.Any(s => string.Equals(s, "active", StringComparison.OrdinalIgnoreCase)));
                if (string.Equals(to, "active", StringComparison.OrdinalIgnoreCase) && activeCap != null) {
                    if (string.Equals(priorState, "active", StringComparison.OrdinalIgnoreCase)) {
                        anyTransitioned = true;
                        return;
                    }
                    string statName = activeCap.Stat.Contains("{source}") && !string.IsNullOrEmpty(slot.Source)
                        ? activeCap.Stat.Replace("{source}", slot.Source.Trim().ToLowerInvariant())
                        : activeCap.Stat;
                    int maxActive = Math.Max(1, ResolveStatOrParam(statName, 1));
                    var currentlyActive = group.Slots
                        .Where(s => !string.IsNullOrEmpty(s.Element) && s != slot && string.Equals(s.State, "active", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    string inactiveState = !string.IsNullOrEmpty(groupDef.DefaultState) ? groupDef.DefaultState : "inactive";
                    while (currentlyActive.Count >= maxActive) {
                        SubsystemSlot? toDeactivate = null;
                        if (rawIndex >= 0 && transition.SelectedIndices != null && transition.SelectedIndices.Count > 0) {
                            int repFlatIdx = ResolveFlatSlotIndex(transition.SlotGroup, transition.SelectedIndices[0], targetSource, false);
                            if (repFlatIdx >= 0 && repFlatIdx < group.Slots.Count) {
                                var candidate = group.Slots[repFlatIdx];
                                if (currentlyActive.Contains(candidate)) {
                                    toDeactivate = candidate;
                                }
                            }
                        }
                        if (toDeactivate == null) {
                            toDeactivate = activeSlotOrder.FirstOrDefault(s => currentlyActive.Contains(s)) ?? currentlyActive[0];
                        }
                        toDeactivate.State = inactiveState;
                        currentlyActive.Remove(toDeactivate);
                        activeSlotOrder.Remove(toDeactivate);
                    }
                    activeSlotOrder.Remove(slot);
                    activeSlotOrder.Add(slot);
                } else if (string.Equals(priorState, "active", StringComparison.OrdinalIgnoreCase) && !string.Equals(to, "active", StringComparison.OrdinalIgnoreCase)) {
                    activeSlotOrder.Remove(slot);
                }

                slot.State = to;
                anyTransitioned = true;
                if (string.Equals(to, "empty", StringComparison.OrdinalIgnoreCase)) {
                    ResetElementParameters(elemName);
                    slot.Element = "";
                } else if (!string.Equals(priorState, to, StringComparison.OrdinalIgnoreCase)) {
                    ResetDeactivatedParameters(elemName, transition.SlotGroup, priorState, to);
                }
            }

            switch (selector) {
                case "invoked":
                    if (rawIndex < 0 && string.IsNullOrEmpty(targetElement) && transition.SelectedIndices != null && transition.SelectedIndices.Count > 0) {
                        foreach (var idx in transition.SelectedIndices) {
                            int flatIdx = ResolveFlatSlotIndex(transition.SlotGroup, idx, targetSource, false);
                            if (flatIdx >= 0 && flatIdx < group.Slots.Count) {
                                var slot = group.Slots[flatIdx];
                                if (MatchesFrom(slot)) {
                                    TransitionSlot(slot);
                                }
                            }
                        }
                        break;
                    }
                    SubsystemSlot? targetSlot = null;
                    if (rawIndex >= 0) {
                        int flatIdx = ResolveFlatSlotIndex(transition.SlotGroup, rawIndex, targetSource, false);
                        if (flatIdx >= 0 && flatIdx < group.Slots.Count) {
                            targetSlot = group[flatIdx];
                        }
                    }
                    if (targetSlot == null && !string.IsNullOrEmpty(targetElement)) {
                        for (int i = 0; i < group.Slots.Count; i++) {
                            var s = group.Slots[i];
                            if (string.Equals(s.Element, targetElement, StringComparison.OrdinalIgnoreCase)) {
                                if (string.IsNullOrEmpty(targetSource) || string.Equals(s.Source, targetSource, StringComparison.OrdinalIgnoreCase)) {
                                    targetSlot = s;
                                    break;
                                }
                            }
                        }
                    }
                    if (targetSlot != null && MatchesFrom(targetSlot)) {
                        TransitionSlot(targetSlot);
                    }
                    break;

                case "all":
                    foreach (var slot in GetMatchingSlots(group, from, targetSource)) {
                        TransitionSlot(slot);
                    }
                    break;

                case "select":
                    var selected = transition.SelectedIndices;
                    if (selected != null && selected.Count > 0) {
                        foreach (var idx in selected) {
                            int flatIdx = ResolveFlatSlotIndex(transition.SlotGroup, idx, targetSource, false);
                            if (flatIdx >= 0 && flatIdx < group.Slots.Count) {
                                var slot = group.Slots[flatIdx];
                                if (MatchesFrom(slot)) {
                                    TransitionSlot(slot);
                                }
                            }
                        }
                    } else {
                        int selIndex = transition.SlotIndex ?? targetSlotIndex ?? -1;
                        if (selIndex >= 0) {
                            int flatIdx = ResolveFlatSlotIndex(transition.SlotGroup, selIndex, targetSource, false);
                            if (flatIdx >= 0 && flatIdx < group.Slots.Count) {
                                var slot = group.Slots[flatIdx];
                                if (MatchesFrom(slot)) {
                                    TransitionSlot(slot);
                                }
                            }
                        } else if (!string.IsNullOrEmpty(targetElement)) {
                            var slot = GetMatchingSlots(group, from, targetSource)
                                .FirstOrDefault(s => string.Equals(s.Element, targetElement, StringComparison.OrdinalIgnoreCase));
                            if (slot != null) {
                                TransitionSlot(slot);
                            }
                        }
                    }
                    break;

                case "random":
                    var candidates = GetMatchingSlots(group, from, targetSource);
                    if (candidates.Count == 0 && !string.IsNullOrEmpty(transition.OnEmptyVerb)) {
                        return ExecuteVerb(transition.OnEmptyVerb, transition.SlotGroup, targetSlotIndex, targetElement, targetSource);
                    }
                    int count = ResolveCount(transition.CountExpression, targetSource);
                    if (candidates.Count <= count) {
                        foreach (var c in candidates) {
                            TransitionSlot(c);
                        }
                    } else {
                        var poolList = new List<SubsystemSlot>(candidates);
                        for (int i = 0; i < count && poolList.Count > 0; i++) {
                            int pick = Rng.Next(poolList.Count);
                            TransitionSlot(poolList[pick]);
                            poolList.RemoveAt(pick);
                        }
                    }
                    break;
            }

            if (!anyTransitioned) {
                return TimelineRefreshRequirement.None;
            }

            SyncState();
            return TimelineRefreshRequirement.FullReload;
        }

        private int ResolveCount(string? countExpr, string? source) {
            if (string.IsNullOrEmpty(countExpr)) return 1;
            if (int.TryParse(countExpr, out int direct)) return Math.Max(0, direct);

            string statName = countExpr;
            if (!string.IsNullOrEmpty(source) && statName.Contains("{source}")) {
                statName = statName.Replace("{source}", source.Trim().ToLowerInvariant());
            }

            if (context != null) {
                return Math.Max(0, context.GetStatValue(statName, 1));
            }
            return 1;
        }

        public TimelineRefreshRequirement InvokeElementVerb(string verbName, SubsystemSlot slot) {
            if (string.IsNullOrEmpty(verbName) || slot == null || string.IsNullOrEmpty(slot.Element)) {
                return TimelineRefreshRequirement.None;
            }

            var elem = GetElement(slot.Element);
            List<SubsystemStep>? steps = null;

            if (elem != null && elem.Verbs.TryGetValue(verbName, out var elemSteps)) {
                steps = elemSteps;
            } else if (Definition.DefaultElementVerbs.TryGetValue(verbName, out var defaultSteps)) {
                steps = defaultSteps;
            }

            if (steps == null || steps.Count == 0) {
                return TimelineRefreshRequirement.None;
            }

            TimelineRefreshRequirement requirement = TimelineRefreshRequirement.None;
            foreach (var step in steps) {
                var req = ExecuteStep(step, slotGroup: null, targetSlotIndex: null, targetElement: slot.Element, targetSource: slot.Source, invokedSlot: slot);
                if (req > requirement) requirement = req;
            }
            return requirement;
        }

        private List<SubsystemStep>? ResolveVerbSteps(string verbName, string? source) {
            if (!string.IsNullOrEmpty(source) && context?.Catalog != null) {
                if (context.Catalog.TryGetClass(source!, out var cls) && cls.Subsystems != null && cls.Subsystems.TryGetValue(Id, out var subFeature)) {
                    if (subFeature.Verbs != null && subFeature.Verbs.TryGetValue(verbName, out var classSteps)) {
                        return classSteps;
                    }
                }
            }
            if (Definition.Verbs.TryGetValue(verbName, out var defSteps)) {
                return defSteps;
            }
            return null;
        }

        public TimelineRefreshRequirement InvokeVerb(string verbName, string? slotGroup = null, int? slotIndex = null, string? elementName = null, string? source = null, List<int>? selectedIndices = null) {
            return ExecuteVerb(verbName, slotGroup, slotIndex, elementName, source, selectedIndices);
        }

        public TimelineRefreshRequirement ExecuteVerb(string verbName, SubsystemTarget? target = null) {
            return ExecuteVerb(verbName, target?.SlotGroup, target?.SlotIndex, target?.Element, target?.Source, target?.SelectedIndices);
        }

        public TimelineRefreshRequirement ExecuteVerb(string verbName, string? slotGroup = null, int? slotIndex = null, string? elementName = null, string? source = null, List<int>? selectedIndices = null) {
            if (string.IsNullOrEmpty(verbName)) {
                return TimelineRefreshRequirement.None;
            }

            // When invoked across all sources (e.g. reset_encounter with source == null and no specific slot targeted),
            // dispatch per source if multiple sources or source-specific verb overrides exist.
            if (string.IsNullOrEmpty(source) && !slotIndex.HasValue && string.IsNullOrEmpty(elementName)) {
                var slotSources = slotGroups.Values
                    .SelectMany(g => g.Slots)
                    .Where(s => !string.IsNullOrEmpty(s.Source))
                    .Select(s => s.Source!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                bool anySourceOverride = slotSources.Any(src =>
                    context?.Catalog != null &&
                    context.Catalog.TryGetClass(src, out var cls) &&
                    cls.Subsystems != null &&
                    cls.Subsystems.TryGetValue(Id, out var sf) &&
                    sf.Verbs.ContainsKey(verbName));
                if (anySourceOverride && slotSources.Count > 0) {
                    TimelineRefreshRequirement totalReq = TimelineRefreshRequirement.None;
                    foreach (var src in slotSources) {
                        var r = ExecuteVerb(verbName, slotGroup, null, null, src, selectedIndices);
                        if (r > totalReq) totalReq = r;
                    }
                    return totalReq;
                }
            }

            SubsystemSlot? invokedSlot = null;
            if (!string.IsNullOrEmpty(slotGroup)) {
                var group = GetSlotGroup(slotGroup);
                if (group != null) {
                    if (slotIndex.HasValue && slotIndex.Value >= 0) {
                        int flatIdx = ResolveFlatSlotIndex(slotGroup, slotIndex.Value, source, false);
                        if (flatIdx >= 0 && flatIdx < group.Slots.Count) {
                            invokedSlot = group[flatIdx];
                        }
                    } else if (!string.IsNullOrEmpty(elementName)) {
                        invokedSlot = group.Slots.FirstOrDefault(s =>
                            string.Equals(s.Element, elementName, StringComparison.OrdinalIgnoreCase) &&
                            (string.IsNullOrEmpty(source) || string.Equals(s.Source, source, StringComparison.OrdinalIgnoreCase)));
                    }
                }
            } else {
                foreach (var kvp in slotGroups) {
                    var grp = kvp.Value;
                    if (slotIndex.HasValue && slotIndex.Value >= 0) {
                        int flatIdx = ResolveFlatSlotIndex(kvp.Key, slotIndex.Value, source, false);
                        if (flatIdx >= 0 && flatIdx < grp.Slots.Count && !string.IsNullOrEmpty(grp[flatIdx].Element)) {
                            slotGroup = kvp.Key;
                            invokedSlot = grp[flatIdx];
                            break;
                        }
                    } else if (!string.IsNullOrEmpty(elementName)) {
                        var found = grp.Slots.FirstOrDefault(s =>
                            string.Equals(s.Element, elementName, StringComparison.OrdinalIgnoreCase) &&
                            (string.IsNullOrEmpty(source) || string.Equals(s.Source, source, StringComparison.OrdinalIgnoreCase)));
                        if (found != null) {
                            slotGroup = kvp.Key;
                            invokedSlot = found;
                            break;
                        }
                    }
                }
            }

            string? effectiveSource = !string.IsNullOrEmpty(source) ? source : invokedSlot?.Source;
            var steps = ResolveVerbSteps(verbName, effectiveSource);
            if (steps == null || steps.Count == 0) {
                return TimelineRefreshRequirement.None;
            }

            TimelineRefreshRequirement requirement = TimelineRefreshRequirement.None;
            foreach (var step in steps) {
                var req = ExecuteStep(step, slotGroup, slotIndex, elementName, effectiveSource, invokedSlot, selectedIndices);
                if (string.Equals(step.Type, "transition", StringComparison.OrdinalIgnoreCase) && req == TimelineRefreshRequirement.None) {
                    break;
                }
                if (req > requirement) requirement = req;
            }

            return requirement;
        }

        private TimelineRefreshRequirement ExecuteStep(SubsystemStep step, string? slotGroup, int? targetSlotIndex, string? targetElement, string? targetSource, SubsystemSlot? invokedSlot, List<int>? selectedIndices = null) {
            if (step == null) return TimelineRefreshRequirement.None;
            string type = step.Type?.ToLowerInvariant() ?? "";

            switch (type) {
                case "assign":
                    string? groupName = !string.IsNullOrEmpty(step.SlotGroup) ? step.SlotGroup : slotGroup;
                    string targetState = !string.IsNullOrEmpty(step.ToState) ? step.ToState : "assigned";
                    int selIndex = targetSlotIndex ?? -1;
                    if (selIndex == -1 && !string.IsNullOrEmpty(groupName)) {
                        var slots = GetSlots(groupName, targetSource);
                        for (int i = 0; i < slots.Count; i++) {
                            if (string.IsNullOrEmpty(slots[i].Element)) {
                                selIndex = i;
                                break;
                            }
                        }
                        if (selIndex == -1) selIndex = slots.Count;
                    }
                    if (selIndex >= 0 && !string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(targetElement)) {
                        AssignSlot(groupName, selIndex, targetElement, targetState, targetSource, null);
                    }
                    return TimelineRefreshRequirement.FullReload;

                case "transition":
                    var transition = new SubsystemTransition {
                        SlotGroup = !string.IsNullOrEmpty(step.SlotGroup) ? step.SlotGroup : slotGroup,
                        Selector = step.Selector,
                        FromState = step.FromState,
                        ToState = step.ToState,
                        CountExpression = step.CountExpression,
                        OnEmptyVerb = step.OnEmptyVerb,
                        SlotIndex = targetSlotIndex,
                        SelectedIndices = selectedIndices
                    };
                    return ExecuteTransition(transition, targetSlotIndex, targetElement, targetSource);

                case "invoke_element_verb":
                    var slot = invokedSlot;
                    if (slot == null && !string.IsNullOrEmpty(slotGroup)) {
                        if (targetSlotIndex.HasValue && targetSlotIndex.Value >= 0) {
                            slot = GetSlot(slotGroup, targetSlotIndex.Value, targetSource);
                        }
                    }
                    if (slot == null && !string.IsNullOrEmpty(targetElement)) {
                        slot = new SubsystemSlot(targetElement, "invoked", targetSource);
                    }
                    return slot != null && !string.IsNullOrEmpty(step.Verb) ? InvokeElementVerb(step.Verb, slot) : TimelineRefreshRequirement.None;

                case "action":
                    if (step.Action != null) {
                        return step.Action.Perform(context);
                    }
                    return TimelineRefreshRequirement.None;

                case "pool":
                    var poolOp = new SubsystemPoolOperation(step.Pool, step.Op, step.Amount);
                    return ExecutePoolOperation(poolOp);

                default:
                    return TimelineRefreshRequirement.None;
            }
        }

        private string ResolveFeatTargetSource(string? featSlot = null) {
            if (context is ICharacterSession session && !string.IsNullOrEmpty(featSlot)) {
                return session.ResolveFeatTargetSource(featSlot);
            }
            if (context?.Character != null && context?.Catalog != null) {
                var adept = context.Character.classLevels.FirstOrDefault(c => {
                    return context.Catalog.TryGetClass(c, out var classDef) &&
                        classDef.Subsystems != null &&
                        classDef.Subsystems.ContainsKey(Id);
                });
                if (!string.IsNullOrEmpty(adept)) return adept;
            }
            return "Feats";
        }

        public void GrantSlot(string slotGroup, string element, string source, string? featSlot) {
            if (string.IsNullOrEmpty(slotGroup) || string.IsNullOrEmpty(element) || string.IsNullOrEmpty(source)) return;
            var group = GetSlotGroup(slotGroup);
            if (group == null) return;
            if (GetElement(element) == null) return;

            if (IsSourcePartitionedGroup(slotGroup)) {
                int totalCap = GetSlotCapacity(slotGroup, source);
                int totalGrantedForSource = group.RawSlots.Count(s =>
                    s != null &&
                    string.Equals(s.Source, source, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(s.FeatSlot));

                bool isNew = !group.RawSlots.Any(s =>
                    s != null &&
                    string.Equals(s.FeatSlot, featSlot, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(s.Source, source, StringComparison.OrdinalIgnoreCase));

                if (isNew) {
                    totalGrantedForSource++;
                }

                int baseClassCapacity = Math.Max(0, totalCap - totalGrantedForSource);

                var sourceIndices = EnsureSourceSlots(slotGroup, source, baseClassCapacity);
                SubsystemSlot? existingSlot = null;
                int existingSourceIdx = -1;
                for (int idx = 0; idx < sourceIndices.Count; idx++) {
                    var s = group[sourceIndices[idx]];
                    if (!string.IsNullOrEmpty(featSlot) && string.Equals(s.FeatSlot, featSlot, StringComparison.OrdinalIgnoreCase)) {
                        existingSlot = s;
                        existingSourceIdx = idx;
                        break;
                    }
                }

                if (existingSlot != null) {
                    existingSlot.Element = element;
                    if (existingSourceIdx < baseClassCapacity) {
                        string defState = GetDefaultState(slotGroup);
                        group[sourceIndices[existingSourceIdx]] = new SubsystemSlot("", defState, source);
                        var updatedIndices = EnsureSourceSlots(slotGroup, source, Math.Max(baseClassCapacity + 1, sourceIndices.Count));
                        int targetFlat = -1;
                        for (int idx = baseClassCapacity; idx < updatedIndices.Count; idx++) {
                            var candidate = group[updatedIndices[idx]];
                            if (string.IsNullOrEmpty(candidate.Element)) {
                                targetFlat = updatedIndices[idx];
                                break;
                            }
                        }
                        if (targetFlat == -1) {
                            var more = EnsureSourceSlots(slotGroup, source, updatedIndices.Count + 1);
                            targetFlat = more.Last();
                        }
                        group[targetFlat] = existingSlot;
                    }
                } else {
                    var updatedIndices = EnsureSourceSlots(slotGroup, source, Math.Max(baseClassCapacity + 1, sourceIndices.Count));
                    int targetFlat = -1;
                    for (int idx = baseClassCapacity; idx < updatedIndices.Count; idx++) {
                        var candidate = group[updatedIndices[idx]];
                        if (string.IsNullOrEmpty(candidate.Element) && string.IsNullOrEmpty(candidate.FeatSlot)) {
                            targetFlat = updatedIndices[idx];
                            break;
                        }
                    }
                    if (targetFlat == -1) {
                        var more = EnsureSourceSlots(slotGroup, source, updatedIndices.Count + 1);
                        targetFlat = more.Last();
                    }
                    group[targetFlat] = new SubsystemSlot(element, GetDefaultState(slotGroup), source, featSlot);
                }
            } else {
                SubsystemSlot? existingSlot = null;
                for (int idx = 0; idx < group.RawSlots.Count; idx++) {
                    var s = group.RawSlots[idx];
                    if (s != null && !string.IsNullOrEmpty(featSlot) && string.Equals(s.FeatSlot, featSlot, StringComparison.OrdinalIgnoreCase)) {
                        existingSlot = s;
                        break;
                    }
                }
                if (existingSlot != null) {
                    existingSlot.Element = element;
                } else {
                    int targetIdx = -1;
                    for (int idx = 0; idx < group.RawSlots.Count; idx++) {
                        var s = group.RawSlots[idx];
                        if (s == null || (string.IsNullOrEmpty(s.Element) && string.IsNullOrEmpty(s.FeatSlot))) {
                            targetIdx = idx;
                            break;
                        }
                    }
                    if (targetIdx == -1) {
                        targetIdx = group.RawSlots.Count;
                    }
                    group[targetIdx] = new SubsystemSlot(element, GetDefaultState(slotGroup), source, featSlot);
                }
            }

            SyncState();
        }

        public void PruneStaleGrantedSlots(IEnumerable<(string SlotGroup, string Source, string FeatSlot)> activeGrants) {
            bool hasAnyFeatSlots = slotGroups.Values.Any(g => g.RawSlots.Any(s => s != null && !string.IsNullOrEmpty(s.FeatSlot)));
            if (!hasAnyFeatSlots) return;

            var activeSet = new HashSet<(string SlotGroup, string Source, string FeatSlot)>(
                activeGrants.Select(g => (g.SlotGroup.ToLowerInvariant(), g.Source.ToLowerInvariant(), g.FeatSlot.ToLowerInvariant()))
            );

            bool anyRemoved = false;
            foreach (var kvp in slotGroups) {
                var groupKey = kvp.Key;
                var group = kvp.Value;
                var kept = new List<SubsystemSlot>();
                bool removedAny = false;
                for (int i = 0; i < group.RawSlots.Count; i++) {
                    var s = group.RawSlots[i];
                    if (s == null) continue;
                    if (!string.IsNullOrEmpty(s.FeatSlot)) {
                        var key = (groupKey.ToLowerInvariant(), (s.Source ?? "").ToLowerInvariant(), s.FeatSlot!.ToLowerInvariant());
                        if (!activeSet.Contains(key)) {
                            activeSlotOrder.Remove(s);
                            removedAny = true;
                            anyRemoved = true;
                            continue;
                        }
                    }
                    if (!string.IsNullOrEmpty(s.Element) || !string.IsNullOrEmpty(s.Source)) {
                        kept.Add(s);
                    }
                }
                if (removedAny) {
                    group.ClearAll();
                    for (int i = 0; i < kept.Count; i++) {
                        group[i] = kept[i];
                    }
                }
            }
            if (anyRemoved) {
                SyncState();
            }
        }

        public void ContributeEffects(EffectPool pool) {
            if (pool == null) return;

            var activeSources = GetActiveSources(pool);

            if ((activeSources.Count > 0 || IsActive) && Definition.ElementStatistics != null && Definition.Elements != null) {
                var baseEffect = new Effect($"{Definition.Id} Element Statistics Base");
                bool hasBaseModifiers = false;
                foreach (var elem in Definition.Elements.Values) {
                    if (string.IsNullOrEmpty(elem.Name)) continue;
                    foreach (var kvp in Definition.ElementStatistics) {
                        string statProp = kvp.Key;
                        int baseVal = elem.GetProperty<int>(statProp);
                        string statName = elem.GetStatisticName(statProp);
                        var stat = new Statistic(statName);
                        if (kvp.Value != null) {
                            foreach (var tag in kvp.Value) {
                                stat.AddTag(tag);
                            }
                        }
                        pool.AddStat(stat);
                        baseEffect.AddModifier(statName, new Modifier(baseVal, ModifierTypes.BASE));
                        hasBaseModifiers = true;
                    }
                }
                if (hasBaseModifiers) {
                    pool.AddEffect(baseEffect);
                }
            }

            if (Definition.SlotGroups != null) {
                foreach (var groupDef in Definition.SlotGroups.Values) {
                    if (groupDef.MaxLevel != null && !string.IsNullOrEmpty(groupDef.MaxLevel.Stat)) {
                        if (groupDef.MaxLevel.Stat.Contains("{source}")) {
                            foreach (var src in activeSources) {
                                string normSrc = src.Trim().ToLowerInvariant();
                                string statName = groupDef.MaxLevel.Stat.Replace("{source}", normSrc);
                                pool.AddStat(new Statistic(statName));
                                pool.AddEffect(new Effect($"{statName} Base")
                                    .AddModifier(statName, new Modifier(groupDef.MaxLevel.Base, ModifierTypes.BASE)));
                                foreach (var effect in groupDef.GetEffectsForSource(normSrc)) {
                                    pool.AddEffect(effect);
                                }
                            }
                        } else {
                            string statName = groupDef.MaxLevel.Stat;
                            pool.AddStat(new Statistic(statName));
                            pool.AddEffect(new Effect($"{statName} Base")
                                .AddModifier(statName, new Modifier(groupDef.MaxLevel.Base, ModifierTypes.BASE)));
                            if (groupDef.Effects != null) {
                                foreach (var effect in groupDef.Effects) {
                                    pool.AddEffect(effect);
                                }
                            }
                        }
                    } else if (groupDef.Effects != null) {
                        foreach (var effect in groupDef.Effects) {
                            pool.AddEffect(effect);
                        }
                    }
                }
            }

            if (Definition.Projections != null) {
                foreach (var rule in Definition.Projections) {
                    if (string.IsNullOrEmpty(rule.SlotGroup)) continue;
                    var group = GetSlotGroup(rule.SlotGroup);
                    if (group == null) continue;

                    for (int i = 0; i < group.RawSlots.Count; i++) {
                        var slot = group.RawSlots[i];
                        if (string.IsNullOrEmpty(slot.Element)) continue;
                        if (!string.Equals(slot.State, rule.State, StringComparison.OrdinalIgnoreCase)) continue;

                        var elem = GetElement(slot.Element);
                        if (elem == null) continue;

                        foreach (var effectKey in rule.Effects) {
                            var effects = Definition.GetElementEffects(elem, effectKey);
                            if (effects != null) {
                                foreach (var eff in effects) {
                                    pool.AddEffect(eff);
                                }
                            }
                        }
                    }
                }
            }

            if (context?.Timeline != null) {
                var timelineActions = new List<SheetAction>();
                if (context.Timeline.ScheduledActions != null) {
                    foreach (var list in context.Timeline.ScheduledActions.Values) {
                        timelineActions.AddRange(list);
                    }
                }
                if (context.Timeline.EncounterActions != null) {
                    timelineActions.AddRange(context.Timeline.EncounterActions);
                }
                foreach (var action in timelineActions) {
                    if (action == null) continue;
                    if (!string.IsNullOrEmpty(action.Subsystem) && !string.Equals(action.Subsystem, Id, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                    string elemName = !string.IsNullOrEmpty(action.Element) ? action.Element : action.Name;
                    if (string.IsNullOrEmpty(elemName)) continue;
                    var elem = GetElement(elemName);
                    if (elem != null) {
                        foreach (var eff in elem.GetEffects("benefits")) {
                            pool.AddEffect(eff);
                        }
                    }
                }
            }
        }

        private bool MatchesFilter(SubsystemElement elem, string filter) {
            if (string.IsNullOrEmpty(filter)) return true;
            if (elem == null) return false;
            if (string.Equals(Id, filter, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Name, filter, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            return elem.MatchesFilter(filter, Definition);
        }

        private int? GetMaxAllowedElementLevel(string? targetSource = null) {
            if (Definition.LevelGating == null || string.IsNullOrEmpty(Definition.LevelGating.ElementStatistic)) return null;
            var gatedSlotGroups = Definition.SlotGroups.Values
                .Where(g => g.MaxLevel != null && !string.IsNullOrEmpty(g.MaxLevel.Stat))
                .ToList();
            if (gatedSlotGroups.Count == 0) return null;

            return gatedSlotGroups.Select(g => ResolveMaxLevel(g, targetSource)).Max();
        }

        public bool IsElementWithinLevelGate(SubsystemElement elem, string? targetSource = null) {
            int? maxAllowed = GetMaxAllowedElementLevel(targetSource);
            if (!maxAllowed.HasValue) return true;
            int elemLevel = elem.GetStatistic(Definition.LevelGating.ElementStatistic, context).Value;
            return elemLevel <= maxAllowed.Value;
        }

        public int CountSamePropertyKnown(string? propertyName, string? propertyValue, string? excludeElementName = null, string? excludeSlot = null) {
            var distinctKnown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in slotGroups.Values.SelectMany(g => g.Slots)) {
                if (string.IsNullOrEmpty(s.Element)) continue;
                if (!string.IsNullOrEmpty(excludeSlot) && !string.IsNullOrEmpty(s.FeatSlot)) {
                    if (string.Equals(s.FeatSlot, excludeSlot, StringComparison.OrdinalIgnoreCase) ||
                        s.FeatSlot.StartsWith(excludeSlot + ":", StringComparison.OrdinalIgnoreCase) ||
                        excludeSlot.StartsWith(s.FeatSlot + ":", StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }
                }
                if (!string.IsNullOrEmpty(excludeElementName) && string.Equals(s.Element, excludeElementName, StringComparison.OrdinalIgnoreCase)) continue;

                var elem = GetElement(s.Element);
                if (elem == null) continue;

                if (string.IsNullOrEmpty(propertyName)) {
                    distinctKnown.Add(elem.Name);
                } else {
                    string? val = elem.GetProperty<string>(propertyName);
                    if (string.Equals(val, propertyValue, StringComparison.OrdinalIgnoreCase)) {
                        distinctKnown.Add(elem.Name);
                    }
                }
            }

            return distinctKnown.Count;
        }

        public IReadOnlyList<ValidationWarning> ValidateChoice(ChoiceDescriptor? descriptor, string choiceValue, string? targetSource = null) {
            if (string.IsNullOrWhiteSpace(choiceValue)) return Array.Empty<ValidationWarning>();
            if (descriptor != null) {
                if (!string.Equals(descriptor.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(descriptor.Domain, Id, StringComparison.OrdinalIgnoreCase)) {
                    return Array.Empty<ValidationWarning>();
                }
                if (!string.IsNullOrEmpty(descriptor.Filter)) {
                    bool hasFilterMatches = Definition.Elements.Values.Any(e => MatchesFilter(e, descriptor.Filter));
                    if (!hasFilterMatches) {
                        return Array.Empty<ValidationWarning>();
                    }
                } else {
                    if (GetElement(choiceValue) == null) {
                        return Array.Empty<ValidationWarning>();
                    }
                }
            }
            string? filter = descriptor?.Filter;
            var chosenElem = GetElement(choiceValue);
            if (chosenElem == null || !MatchesFilter(chosenElem, filter)) {
                string typeLabel = (!string.IsNullOrEmpty(filter) &&
                                    !string.Equals(filter, Id, StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(filter, Name, StringComparison.OrdinalIgnoreCase))
                    ? filter
                    : "element";
                return new List<ValidationWarning> {
                    new ValidationWarning($"'{choiceValue}' is not a valid {typeLabel} in subsystem '{Name}'.", ValidationCategory.Invalid)
                };
            }

            string? effectiveSource = targetSource;
            if (string.IsNullOrWhiteSpace(effectiveSource)) {
                effectiveSource = ResolveFeatTargetSource(null);
            }

            var warnings = new List<ValidationWarning>();
            int? maxAllowed = GetMaxAllowedElementLevel(effectiveSource);
            if (maxAllowed.HasValue) {
                int elemLevel = chosenElem.GetStatistic(Definition.LevelGating!.ElementStatistic, context).Value;
                if (elemLevel > maxAllowed.Value) {
                    string srcSuffix = !string.IsNullOrWhiteSpace(effectiveSource) ? $" for {effectiveSource}" : "";
                    string maxLabel = !string.IsNullOrEmpty(Definition.LevelGating?.MaxLevelLabel)
                        ? Definition.LevelGating!.MaxLevelLabel!
                        : "maximum";
                    warnings.Add(new ValidationWarning($"Level {elemLevel} exceeds {maxLabel} {maxAllowed.Value}{srcSuffix}.", ValidationCategory.LevelGate));
                }
            }

            int prereqCount = chosenElem.GetProperty<int>("prerequisiteCount", 0);
            prereqCount = Math.Max(descriptor?.MinimumPrerequisiteCount ?? 0, prereqCount);
            if (prereqCount > 0) {
                string? prereqProp = Definition.PrerequisiteProperty;
                string? propVal = !string.IsNullOrEmpty(prereqProp) ? chosenElem.GetProperty<string>(prereqProp) : null;
                if (string.IsNullOrEmpty(prereqProp) || !string.IsNullOrEmpty(propVal)) {
                    int known = CountSamePropertyKnown(prereqProp, propVal, excludeElementName: chosenElem.Name);
                    if (known < prereqCount) {
                        string elemUnit = Definition.PrerequisiteUnit ?? (!string.IsNullOrEmpty(filter) ? filter : "element");
                        string plural = prereqCount == 1 ? elemUnit : $"{elemUnit}s";
                        string targetDesc = !string.IsNullOrEmpty(propVal) ? $"{propVal} {plural}" : plural;
                        warnings.Add(new ValidationWarning($"Requires {prereqCount} {targetDesc}, but only {known} known.", ValidationCategory.Prerequisite));
                    }
                }
            }

            if (descriptor == null) {
                foreach (var grp in slotGroups.Values) {
                    foreach (var s in grp.RawSlots) {
                        if (string.Equals(s.Element, chosenElem.Name, StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrEmpty(s.Source) && !string.Equals(s.Source, effectiveSource, StringComparison.OrdinalIgnoreCase)) {
                            string unit = Definition.PrerequisiteUnit ?? "element";
                            string titleUnit = char.ToUpperInvariant(unit[0]) + unit.Substring(1);
                            warnings.Add(new ValidationWarning($"{titleUnit} '{chosenElem.Name}' is already learned under source '{s.Source}'.", ValidationCategory.SourceConflict));
                        }
                    }
                }

                if (!string.IsNullOrEmpty(effectiveSource)) {
                    foreach (var kvp in Definition.SlotGroups) {
                        var groupDef = kvp.Value;
                        var group = GetSlotGroup(kvp.Key);
                        if (group == null) continue;
                        foreach (var cap in groupDef.StateCapacities) {
                            var sourceSlots = group.RawSlots.Where(s => !string.IsNullOrEmpty(s.Element) && string.Equals(s.Source, effectiveSource, StringComparison.OrdinalIgnoreCase)).ToList();
                            if (sourceSlots.Count > 0) {
                                int count = sourceSlots.Count(s => cap.States.Any(st => string.Equals(st, s.State, StringComparison.OrdinalIgnoreCase)));
                                string statName = cap.Stat.Contains("{source}") ? cap.Stat.Replace("{source}", effectiveSource.Trim().ToLowerInvariant()) : cap.Stat;
                                int max = ResolveStatOrParam(statName, 0);
                                if (count > max) {
                                    warnings.Add(new ValidationWarning($"{effectiveSource} has {count} {cap.Label} {groupDef.Name}, exceeding maximum of {max}.", ValidationCategory.Capacity));
                                } else if (count < max && cap.Stat.Contains("{source}")) {
                                    warnings.Add(new ValidationWarning($"{effectiveSource} has {count} {cap.Label} {groupDef.Name}, below capacity of {max}.", ValidationCategory.Capacity));
                                }
                            }
                        }
                    }
                }
            }

            return warnings;
        }

        public List<string> Validate() {
            var warnings = new List<string>();
            var seenElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in slotGroups) {
                string groupKey = kvp.Key;
                var group = kvp.Value;
                Definition.SlotGroups.TryGetValue(groupKey, out var groupDef);

                for (int i = 0; i < group.Slots.Count; i++) {
                    var slot = group.Slots[i];
                    if (string.IsNullOrEmpty(slot.Element)) continue;

                    if (!seenElements.Add(slot.Element)) {
                        warnings.Add($"Duplicate element '{slot.Element}' in slot group '{groupKey}'.");
                    }

                    var elem = GetElement(slot.Element);
                    if (elem == null) {
                        warnings.Add($"Unknown element '{slot.Element}' in slot group '{groupKey}'.");
                        continue;
                    }

                    if (groupDef != null && !string.IsNullOrEmpty(groupDef.Filter)) {
                        if (!MatchesFilter(elem, groupDef.Filter)) {
                            warnings.Add($"Element '{elem.Name}' does not match filter '{groupDef.Filter}' for slot group '{groupKey}'.");
                        }
                    }

                    if (Definition.LevelGating != null && !string.IsNullOrEmpty(Definition.LevelGating.ElementStatistic) && groupDef?.MaxLevel != null) {
                        int elemLevel = elem.GetStatistic(Definition.LevelGating.ElementStatistic, context).Value;
                        int maxLevel = ResolveMaxLevel(groupDef, slot.Source);
                        if (elemLevel > maxLevel) {
                            warnings.Add($"{elem.Name} (level {elemLevel}) exceeds maximum level {maxLevel} for {slot.Source ?? groupKey}.");
                        }
                    }

                    int prereqCount = elem.GetProperty<int>("prerequisiteCount", 0);
                    if (prereqCount > 0) {
                        string? prereqProp = Definition.PrerequisiteProperty;
                        string? propVal = !string.IsNullOrEmpty(prereqProp) ? elem.GetProperty<string>(prereqProp) : null;
                        if (string.IsNullOrEmpty(prereqProp) || !string.IsNullOrEmpty(propVal)) {
                            int totalKnown = CountSamePropertyKnown(prereqProp, propVal, excludeElementName: elem.Name);
                            if (totalKnown < prereqCount) {
                                string elemUnit = Definition.PrerequisiteUnit ?? (!string.IsNullOrEmpty(groupDef?.Filter) ? groupDef.Filter : "element");
                                string plural = prereqCount == 1 ? elemUnit : $"{elemUnit}s";
                                string targetDesc = !string.IsNullOrEmpty(propVal) ? $"{propVal} {plural}" : plural;
                                warnings.Add($"{elem.Name} requires {prereqCount} {targetDesc}, but only {totalKnown} known.");
                            }
                        }
                    }
                }

                if (groupDef != null && groupDef.StateCapacities.Count > 0) {
                    foreach (var cap in groupDef.StateCapacities) {
                        if (!cap.Stat.Contains("{source}")) {
                            var matchingSlots = group.Slots
                                .Where(s => !string.IsNullOrEmpty(s.Element) && cap.States.Any(st => string.Equals(st, s.State, StringComparison.OrdinalIgnoreCase)))
                                .ToList();
                            int totalCount = matchingSlots.Count;
                            int max = ResolveStatOrParam(cap.Stat, 0);
                            if (totalCount > max) {
                                var distinctSources = matchingSlots
                                    .Where(s => !string.IsNullOrEmpty(s.Source))
                                    .Select(s => s.Source!)
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToList();
                                string displaySrc = distinctSources.Count == 1 ? distinctSources[0] : groupDef.Name;
                                string warningMsg = $"{displaySrc} has {totalCount} {cap.Label} {groupDef.Name}, exceeding maximum of {max}.";
                                if (!warnings.Contains(warningMsg)) {
                                    warnings.Add(warningMsg);
                                }
                            }
                        } else {
                            var slotsBySource = group.Slots
                                .Where(s => !string.IsNullOrEmpty(s.Element))
                                .GroupBy(s => s.Source ?? string.Empty, StringComparer.OrdinalIgnoreCase);

                            foreach (var srcGroup in slotsBySource) {
                                string src = srcGroup.Key;
                                int count = srcGroup.Count(s => cap.States.Any(st => string.Equals(st, s.State, StringComparison.OrdinalIgnoreCase)));
                                string statName = cap.Stat.Replace("{source}", src.Trim().ToLowerInvariant());
                                int max = ResolveStatOrParam(statName, 0);
                                if (count > max) {
                                    string displaySrc = !string.IsNullOrEmpty(src) ? src : groupDef.Name;
                                    warnings.Add($"{displaySrc} has {count} {cap.Label} {groupDef.Name}, exceeding maximum of {max}.");
                                }
                            }
                        }
                    }
                }
            }

            return warnings;
        }
    }
}
