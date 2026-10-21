using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Subsystems {
    public class SubsystemEngine : ISubsystemEngine {
        private readonly Dictionary<string, SubsystemSlotGroup<SubsystemSlot>> slotGroups = new Dictionary<string, SubsystemSlotGroup<SubsystemSlot>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> poolValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private ICharacterContext context;

        public SubsystemDefinition Definition { get; }
        public string Id => Definition.Id;
        public string Name => Definition.Name;
        public bool IsActive {
            get {
                if (slotGroups.Values.Any(g => g.Capacity > 0)) return true;
                if (slotGroups.Values.Any(g => g.Slots.Any(s => s != null && !string.IsNullOrEmpty(s.Element)))) return true;
                if (Definition.Pools.Keys.Any(k => GetPoolCapacity(k, context) > 0)) return true;
                return false;
            }
        }
        public Random Rng { get; private set; }

        public SubsystemEngine(SubsystemDefinition definition, Random rng = null) {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Rng = rng ?? new Random();
            InitializeStorage();
        }

        public void SetRandom(Random rng) {
            Rng = rng ?? new Random();
        }

        private void InitializeStorage() {
            slotGroups.Clear();
            foreach (var kvp in Definition.SlotGroups) {
                var groupDef = kvp.Value;
                string groupKey = kvp.Key;
                var group = new SubsystemSlotGroup<SubsystemSlot>(
                    capacityProvider: () => ResolveCapacity(groupDef),
                    maxLevelProvider: () => ResolveMaxLevel(groupDef),
                    levelSelector: slot => GetElementLevel(slot?.Element)
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

        private int ResolveCapacity(SlotGroupDefinition def) {
            if (context != null && !string.IsNullOrEmpty(def.CapacityStat)) {
                return ResolveStatOrParam(def.CapacityStat, context, def.FixedCapacity);
            }
            return def.FixedCapacity;
        }

        private int ResolveMaxLevel(SlotGroupDefinition def) {
            if (context != null && !string.IsNullOrEmpty(def.MaxLevelStat)) {
                return ResolveStatOrParam(def.MaxLevelStat, context, int.MaxValue);
            }
            return int.MaxValue;
        }

        private int ResolveStatOrParam(string name, ICharacterContext ctx, int defaultValue) {
            if (string.IsNullOrEmpty(name)) return defaultValue;
            if (ctx != null) {
                if (ctx.Effects != null && ctx.Effects.stats.ContainsKey(name)) {
                    return ctx.GetStatValue(name, defaultValue);
                }
                if (ctx.Effects != null && ctx.Effects.controllableParameters.TryGetValue(name, out var p)) {
                    return p.Value;
                }
                if (ctx.Character != null && ctx.Character.paramValues.TryGetValue(name, out var pv)) {
                    return pv;
                }
                return ctx.GetStatValue(name, defaultValue);
            }
            return defaultValue;
        }

        public int GetElementLevel(string elementName) {
            if (string.IsNullOrEmpty(elementName)) return 1;
            var elem = GetElement(elementName);
            return elem?.Level ?? 1;
        }

        public SubsystemElement GetElement(string elementName, ICharacterContext ctx = null) {
            if (string.IsNullOrEmpty(elementName)) return null;
            if (Definition.Elements.TryGetValue(elementName, out var elem)) {
                return elem;
            }
            var activeContext = ctx ?? context;
            if (activeContext?.Catalog is Rules.IRulesCatalog cat) {
                var subDef = cat.GetSubsystem(Id);
                if (subDef != null && subDef.Elements.TryGetValue(elementName, out var catalogElem)) {
                    return catalogElem;
                }
            }
            return null;
        }

        public void Initialize(ICharacterContext context) {
            this.context = context;
            if (context?.Character != null) {
                if (context.Character.Subsystems.TryGetValue(Id, out var state) && state != null) {
                    LoadState(state);
                }
            }
            foreach (var kvp in Definition.Pools) {
                var poolDef = kvp.Value;
                if (!poolValues.ContainsKey(kvp.Key)) {
                    int cap = GetPoolCapacity(kvp.Key, context);
                    poolValues[kvp.Key] = cap;
                }
            }
            if (context?.Character != null && (IsActive || context.Character.Subsystems.ContainsKey(Id))) {
                SyncState();
            }
        }

        public void LoadState(SubsystemState state) {
            if (state == null) return;

            if (state.Slots != null) {
                foreach (var kvp in state.Slots) {
                    var group = GetSlotGroup(kvp.Key);
                    if (group != null) {
                        group.ClearAll();
                        for (int i = 0; i < kvp.Value.Count; i++) {
                            var s = kvp.Value[i];
                            if (s != null) {
                                group[i] = new SubsystemSlot(s.Element, s.State, s.Source, s.FeatSlot);
                            }
                        }
                    }
                }
            }

            if (state.Pools != null) {
                foreach (var kvp in state.Pools) {
                    poolValues[kvp.Key] = kvp.Value;
                }
            }
        }

        public SubsystemState ExportState() {
            var state = new SubsystemState();
            foreach (var kvp in slotGroups) {
                var groupKey = kvp.Key;
                var group = kvp.Value;
                var slots = group.Slots;
                int lastIndex = -1;
                for (int i = slots.Count - 1; i >= 0; i--) {
                    var s = slots[i];
                    if (s != null && (!string.IsNullOrEmpty(s.Element) || !string.IsNullOrEmpty(s.State) || !string.IsNullOrEmpty(s.Source))) {
                        lastIndex = i;
                        break;
                    }
                }
                var list = new List<SubsystemSlot>();
                for (int i = 0; i <= lastIndex; i++) {
                    var s = slots[i];
                    if (s != null) {
                        list.Add(new SubsystemSlot(s.Element, s.State, s.Source, s.FeatSlot));
                    } else {
                        list.Add(new SubsystemSlot());
                    }
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
            if (context?.Character?.Subsystems != null) {
                context.Character.Subsystems[Id] = ExportState();
            }
        }

        public void SetSlotState(string groupName, int index, string state) {
            var slot = GetSlot(groupName, index);
            if (slot != null) {
                string priorState = slot.State;
                slot.State = state ?? "";
                if (string.Equals(state, "good_pact", StringComparison.OrdinalIgnoreCase) && string.Equals(priorState, "poor_pact", StringComparison.OrdinalIgnoreCase)) {
                    ResetElementParameters(slot.Element);
                }
                SyncState();
            }
        }

        public SubsystemSlotGroup<SubsystemSlot> GetSlotGroup(string groupName) {
            if (string.IsNullOrEmpty(groupName)) return null;
            return slotGroups.TryGetValue(groupName, out var group) ? group : null;
        }

        public IReadOnlyList<SubsystemSlot> GetSlots(string groupName, string source = null) {
            var group = GetSlotGroup(groupName);
            if (group == null) return new List<SubsystemSlot>();
            var all = group.Slots;
            if (string.IsNullOrEmpty(source)) {
                return all;
            }
            return all.Where(s => s != null && string.Equals(s.Source, source, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public SubsystemSlot GetSlot(string groupName, int index) {
            var group = GetSlotGroup(groupName);
            if (group == null || index < 0 || index >= group.Slots.Count) return null;
            return group[index];
        }

        public int GetSlotCapacity(string groupName) => GetSlotGroup(groupName)?.Capacity ?? 0;

        public string GetSlotElement(string groupName, int index) {
            var slot = GetSlot(groupName, index);
            return string.IsNullOrEmpty(slot?.Element) ? null : slot.Element;
        }

        public string GetSlotState(string groupName, int index) {
            var slot = GetSlot(groupName, index);
            return string.IsNullOrEmpty(slot?.State) ? "empty" : slot.State;
        }

        public bool IsSlotLevelValid(string groupName, int slotIndex) {
            return GetSlotGroup(groupName)?.IsSlotLevelValid(slotIndex) ?? true;
        }

        public void ResetElementParameters(string elementName, ICharacterContext ctx = null) {
            if (string.IsNullOrEmpty(elementName)) return;
            var activeContext = ctx ?? context;
            var elem = GetElement(elementName, activeContext);
            if (elem == null) return;
            foreach (var kindList in elem.Kinds.Values) {
                foreach (var eff in kindList) {
                    if (!string.IsNullOrEmpty(eff.ParameterName)) {
                        string paramKey = eff.ParameterName;
                        if (activeContext?.Character?.paramValues != null) {
                            activeContext.Character.paramValues[paramKey] = 0;
                        }
                        if (activeContext?.Effects?.controllableParameters != null &&
                            activeContext.Effects.controllableParameters.TryGetValue(paramKey, out var param)) {
                            param.Value = 0;
                        }
                    }
                }
            }
        }

        public void AssignSlot(string groupName, int index, string element, string state = null, string source = null, string featSlot = null, ICharacterContext ctx = null) {
            var group = GetSlotGroup(groupName);
            if (group == null) throw new ArgumentException($"Slot group '{groupName}' not found in subsystem '{Id}'.");
            string targetState = state;
            if (string.IsNullOrEmpty(targetState)) {
                if (Definition.SlotGroups.TryGetValue(groupName, out var def) && !string.IsNullOrEmpty(def.DefaultState)) {
                    targetState = def.DefaultState;
                } else {
                    targetState = "available";
                }
            }
            var activeContext = ctx ?? context;
            var existing = (index >= 0 && index < group.Slots.Count) ? group[index] : null;
            if (existing != null && !string.IsNullOrEmpty(existing.Element)) {
                ResetElementParameters(existing.Element, activeContext);
            }
            ResetElementParameters(element, activeContext);
            group[index] = new SubsystemSlot(element, targetState, source, featSlot);
            SyncState();
        }

        public void ClearSlot(string groupName, int index, ICharacterContext ctx = null) {
            var activeContext = ctx ?? context;
            var group = GetSlotGroup(groupName);
            if (group != null && index >= 0 && index < group.Slots.Count) {
                var existing = group[index];
                if (existing != null && !string.IsNullOrEmpty(existing.Element)) {
                    ResetElementParameters(existing.Element, activeContext);
                }
            }
            group?.ClearSlot(index);
            SyncState();
        }

        public int GetPoolCapacity(string poolId, ICharacterContext ctx = null) {
            if (string.IsNullOrEmpty(poolId) || !Definition.Pools.TryGetValue(poolId, out var def)) return 0;
            var activeContext = ctx ?? context;
            if (activeContext != null && !string.IsNullOrEmpty(def.CapacityStat)) {
                return ResolveStatOrParam(def.CapacityStat, activeContext, def.DefaultCapacity);
            }
            return def.DefaultCapacity;
        }

        public int GetPoolValue(string poolId, ICharacterContext ctx = null) {
            if (string.IsNullOrEmpty(poolId)) return 0;
            int cap = GetPoolCapacity(poolId, ctx);
            int current = poolValues.TryGetValue(poolId, out var val) ? val : cap;
            return Math.Min(current, cap);
        }

        public void SetPoolValue(string poolId, int value, ICharacterContext ctx = null) {
            if (string.IsNullOrEmpty(poolId)) return;
            int cap = GetPoolCapacity(poolId, ctx);
            poolValues[poolId] = Math.Max(0, Math.Min(value, cap));
            SyncState();
        }

        public void ConsumePool(string poolId, int amount, ICharacterContext ctx = null) {
            if (string.IsNullOrEmpty(poolId) || amount <= 0) return;
            int current = GetPoolValue(poolId, ctx);
            SetPoolValue(poolId, current - amount, ctx);
        }

        public void RechargePool(string poolId, int amount, ICharacterContext ctx = null) {
            if (string.IsNullOrEmpty(poolId) || amount <= 0) return;
            int current = GetPoolValue(poolId, ctx);
            SetPoolValue(poolId, current + amount, ctx);
        }

        public TimelineRefreshRequirement ExecutePoolOperation(SubsystemPoolOperation operation, ICharacterContext ctx) {
            if (operation == null || string.IsNullOrEmpty(operation.Pool)) return TimelineRefreshRequirement.None;
            var activeContext = ctx ?? context;
            string op = operation.Op?.ToLowerInvariant() ?? "consume";
            switch (op) {
                case "consume":
                    ConsumePool(operation.Pool, operation.Amount, activeContext);
                    break;
                case "recharge":
                    RechargePool(operation.Pool, operation.Amount, activeContext);
                    break;
                case "set":
                    SetPoolValue(operation.Pool, operation.Amount, activeContext);
                    break;
            }
            return TimelineRefreshRequirement.RecomputeDerived;
        }

        private List<SubsystemSlot> GetMatchingSlots(SubsystemSlotGroup<SubsystemSlot> group, string fromState, string targetSource) {
            var result = new List<SubsystemSlot>();
            for (int i = 0; i < group.Slots.Count; i++) {
                var slot = group.Slots[i];
                if (slot == null || string.IsNullOrEmpty(slot.Element)) continue;
                if (!string.IsNullOrEmpty(targetSource) && !string.Equals(slot.Source, targetSource, StringComparison.OrdinalIgnoreCase)) continue;
                if (fromState == null || fromState == "*" || string.Equals(slot.State, fromState, StringComparison.OrdinalIgnoreCase)) {
                    result.Add(slot);
                }
            }
            return result;
        }

        public TimelineRefreshRequirement ExecuteTransition(SubsystemTransition transition, ICharacterContext ctx, SubsystemTarget target) {
            return ExecuteTransition(transition, ctx, target?.SlotIndex, target?.Element, target?.Source);
        }

        public TimelineRefreshRequirement ExecuteTransition(SubsystemTransition transition, ICharacterContext ctx, int? targetSlotIndex = null, string targetElement = null, string targetSource = null) {
            if (transition == null || string.IsNullOrEmpty(transition.SlotGroup)) return TimelineRefreshRequirement.None;
            var group = GetSlotGroup(transition.SlotGroup);
            if (group == null) return TimelineRefreshRequirement.None;

            var activeContext = ctx ?? context;
            string selector = transition.Selector?.ToLowerInvariant() ?? "invoked";
            string from = transition.FromState;
            string to = transition.ToState;

            bool MatchesFrom(SubsystemSlot slot) {
                if (slot == null || string.IsNullOrEmpty(slot.Element)) return false;
                if (from == null || from == "*") return true;
                return string.Equals(slot.State, from, StringComparison.OrdinalIgnoreCase);
            }

            void TransitionSlot(SubsystemSlot slot) {
                if (slot == null) return;
                string priorState = slot.State;
                string elemName = slot.Element;
                slot.State = to;
                if (string.Equals(to, "empty", StringComparison.OrdinalIgnoreCase)) {
                    ResetElementParameters(elemName, activeContext);
                    slot.Element = "";
                } else if (string.Equals(priorState, "poor_pact", StringComparison.OrdinalIgnoreCase) && string.Equals(to, "good_pact", StringComparison.OrdinalIgnoreCase)) {
                    ResetElementParameters(elemName, activeContext);
                }
            }

            switch (selector) {
                case "invoked":
                    SubsystemSlot targetSlot = null;
                    int index = transition.SlotIndex ?? targetSlotIndex ?? -1;
                    if (index >= 0 && index < group.Slots.Count) {
                        targetSlot = group[index];
                    } else {
                        string elemName = targetElement;
                        if (!string.IsNullOrEmpty(elemName)) {
                            for (int i = 0; i < group.Slots.Count; i++) {
                                var s = group.Slots[i];
                                if (s != null && string.Equals(s.Element, elemName, StringComparison.OrdinalIgnoreCase)) {
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
                            if (idx >= 0 && idx < group.Slots.Count) {
                                var slot = group.Slots[idx];
                                if (slot != null && MatchesFrom(slot)) {
                                    TransitionSlot(slot);
                                }
                            }
                        }
                    } else {
                        int selIndex = transition.SlotIndex ?? targetSlotIndex ?? -1;
                        if (selIndex >= 0 && selIndex < group.Slots.Count) {
                            var slot = group.Slots[selIndex];
                            if (slot != null && MatchesFrom(slot)) {
                                TransitionSlot(slot);
                            }
                        }
                    }
                    break;

                case "random":
                    var candidates = GetMatchingSlots(group, from, targetSource);
                    int count = ResolveCount(transition.CountExpression, targetSource, activeContext);
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

            SyncState();
            return TimelineRefreshRequirement.FullReload;
        }

        private int ResolveCount(string countExpr, string source, ICharacterContext ctx) {
            if (string.IsNullOrEmpty(countExpr)) return 1;
            if (int.TryParse(countExpr, out int direct)) return Math.Max(0, direct);

            string statName = countExpr;
            if (!string.IsNullOrEmpty(source) && statName.Contains("{source}")) {
                statName = statName.Replace("{source}", source.Trim().ToLowerInvariant());
            }

            if (ctx != null) {
                return Math.Max(0, ctx.GetStatValue(statName, 1));
            }
            return 1;
        }

        public TimelineRefreshRequirement InvokeElementVerb(string verbName, SubsystemSlot slot, ICharacterContext ctx) {
            if (string.IsNullOrEmpty(verbName) || slot == null || string.IsNullOrEmpty(slot.Element)) {
                return TimelineRefreshRequirement.None;
            }

            var activeContext = ctx ?? context;
            var elem = GetElement(slot.Element, activeContext);
            List<SubsystemStep> steps = null;

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
                var req = ExecuteStep(step, activeContext, slotGroup: null, targetSlotIndex: null, targetElement: slot.Element, targetSource: slot.Source, invokedSlot: slot);
                if (req > requirement) requirement = req;
            }
            return requirement;
        }

        public TimelineRefreshRequirement ExecuteVerb(string verbName, ICharacterContext ctx, SubsystemTarget target = null) {
            return ExecuteVerb(verbName, ctx, target?.SlotGroup, target?.SlotIndex, target?.Element, target?.Source, target?.SelectedIndices);
        }

        public TimelineRefreshRequirement ExecuteVerb(string verbName, ICharacterContext ctx, string slotGroup = null, int? slotIndex = null, string elementName = null, string source = null, List<int> selectedIndices = null) {
            if (string.IsNullOrEmpty(verbName) || !Definition.Verbs.TryGetValue(verbName, out var steps)) {
                return TimelineRefreshRequirement.None;
            }

            var activeContext = ctx ?? context;
            TimelineRefreshRequirement requirement = TimelineRefreshRequirement.None;

            SubsystemSlot invokedSlot = null;
            if (!string.IsNullOrEmpty(slotGroup)) {
                var group = GetSlotGroup(slotGroup);
                if (group != null) {
                    if (slotIndex.HasValue && slotIndex.Value >= 0 && slotIndex.Value < group.Slots.Count) {
                        invokedSlot = group[slotIndex.Value];
                    } else if (!string.IsNullOrEmpty(elementName)) {
                        invokedSlot = group.Slots.FirstOrDefault(s => s != null && string.Equals(s.Element, elementName, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }

            foreach (var step in steps) {
                var req = ExecuteStep(step, activeContext, slotGroup, slotIndex, elementName, source, invokedSlot, selectedIndices);
                if (req > requirement) requirement = req;
            }

            return requirement;
        }

        private TimelineRefreshRequirement ExecuteStep(SubsystemStep step, ICharacterContext ctx, string slotGroup, int? targetSlotIndex, string targetElement, string targetSource, SubsystemSlot invokedSlot, List<int> selectedIndices = null) {
            if (step == null) return TimelineRefreshRequirement.None;
            string type = step.Type?.ToLowerInvariant() ?? "";

            switch (type) {
                case "assign":
                    string groupName = !string.IsNullOrEmpty(step.SlotGroup) ? step.SlotGroup : slotGroup;
                    string targetState = !string.IsNullOrEmpty(step.ToState) ? step.ToState : "assigned";
                    int selIndex = targetSlotIndex ?? -1;
                    if (selIndex == -1 && !string.IsNullOrEmpty(groupName)) {
                        var grp = GetSlotGroup(groupName);
                        if (grp != null) {
                            for (int i = 0; i < grp.Slots.Count; i++) {
                                if (grp.Slots[i] == null || string.IsNullOrEmpty(grp.Slots[i].Element)) {
                                    selIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                    if (selIndex >= 0) {
                        AssignSlot(groupName, selIndex, targetElement, targetState, targetSource, null, ctx);
                    }
                    return TimelineRefreshRequirement.FullReload;

                case "transition":
                    var transition = new SubsystemTransition {
                        SlotGroup = !string.IsNullOrEmpty(step.SlotGroup) ? step.SlotGroup : slotGroup,
                        Selector = step.Selector,
                        FromState = step.FromState,
                        ToState = step.ToState,
                        CountExpression = step.CountExpression,
                        SlotIndex = targetSlotIndex,
                        SelectedIndices = selectedIndices
                    };
                    return ExecuteTransition(transition, ctx, targetSlotIndex, targetElement, targetSource);

                case "invoke_element_verb":
                    var slot = invokedSlot;
                    if (slot == null && !string.IsNullOrEmpty(slotGroup)) {
                        var group = GetSlotGroup(slotGroup);
                        if (group != null && targetSlotIndex.HasValue && targetSlotIndex.Value >= 0 && targetSlotIndex.Value < group.Slots.Count) {
                            slot = group[targetSlotIndex.Value];
                        }
                    }
                    if (slot == null && !string.IsNullOrEmpty(targetElement)) {
                        slot = new SubsystemSlot(targetElement, "invoked", targetSource);
                    }
                    return InvokeElementVerb(step.Verb, slot, ctx);

                case "action":
                    if (step.Action != null) {
                        return step.Action.Perform(ctx);
                    }
                    return TimelineRefreshRequirement.None;

                case "pool":
                    var poolOp = new SubsystemPoolOperation(step.Pool, step.Op, step.Amount);
                    return ExecutePoolOperation(poolOp, ctx);

                default:
                    return TimelineRefreshRequirement.None;
            }
        }

        public void ContributeEffects(EffectPool pool, ICharacterContext ctx) {
            if (pool == null || Definition.Projections == null || Definition.Projections.Count == 0) return;
            var activeContext = ctx ?? context;

            var defianceParams = new List<string>();

            foreach (var rule in Definition.Projections) {
                if (string.IsNullOrEmpty(rule.SlotGroup)) continue;
                var group = GetSlotGroup(rule.SlotGroup);
                if (group == null) continue;

                for (int i = 0; i < group.Slots.Count; i++) {
                    var slot = group.Slots[i];
                    if (slot == null || string.IsNullOrEmpty(slot.Element)) continue;
                    if (!string.Equals(slot.State, rule.State, StringComparison.OrdinalIgnoreCase)) continue;

                    var elem = GetElement(slot.Element, activeContext);
                    if (elem == null) continue;

                    foreach (var kind in rule.Kinds) {
                        var effects = elem.GetEffectsByKind(kind);
                        if (effects != null) {
                            foreach (var eff in effects) {
                                pool.AddEffect(eff);
                                if (string.Equals(kind, "influence", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(eff.ParameterName)) {
                                    defianceParams.Add(eff.ParameterName);
                                }
                            }
                        }
                    }
                }
            }

            if (defianceParams.Any()) {
                Modifier CreateDefianceModifier() {
                    Modifier mod = new Modifier(ModifierTypes.UNTYPED);
                    foreach (string paramKey in defianceParams) {
                        mod.AddAmount(new MultiplyAmount(-1, new ControllableAmount(paramKey)));
                    }
                    return mod;
                }

                pool.AddEffect(new Effect("Vestige defiance")
                    .AddAttackModifier(AttackMatcher.ALL, CreateDefianceModifier())
                    .AddModifier(ALL_SAVES, CreateDefianceModifier())
                    .AddModifier(ABILITY_CHECKS, CreateDefianceModifier())
                    .AddModifier(ALL_SKILLS, CreateDefianceModifier())
                );
            }
        }

        public IReadOnlyList<string> GetAccessibleElements(string filter, string targetSource, int level) {
            var list = new List<string>();
            foreach (var elem in Definition.Elements.Values) {
                if (level > 0 && elem.Level > level) continue;
                if (!string.IsNullOrEmpty(filter)) {
                    bool matches = string.Equals(elem.Type, filter, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(Id, filter, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(Name, filter, StringComparison.OrdinalIgnoreCase);
                    if (!matches) continue;
                }
                list.Add(elem.Name);
            }
            return list.AsReadOnly();
        }

        public List<string> ValidateChoice(string featSlot, ChoiceDescriptor descriptor, string choiceValue, string targetSource, IDictionary<string, string> currentChoices = null) {
            if (descriptor == null || string.IsNullOrWhiteSpace(choiceValue)) return new List<string>();
            if (!string.Equals(descriptor.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(descriptor.Domain, Id, StringComparison.OrdinalIgnoreCase)) {
                return new List<string>();
            }
            if (!string.IsNullOrEmpty(descriptor.Filter)) {
                bool hasFilterMatches = Definition.Elements.Values.Any(e => string.Equals(e.Type, descriptor.Filter, StringComparison.OrdinalIgnoreCase) || string.Equals(Id, descriptor.Filter, StringComparison.OrdinalIgnoreCase));
                if (!hasFilterMatches) {
                    return new List<string>();
                }
            } else {
                if (!Definition.Elements.ContainsKey(choiceValue)) {
                    return new List<string>();
                }
            }
            var valid = GetAccessibleElements(descriptor?.Filter, targetSource, int.MaxValue);
            if (!valid.Contains(choiceValue, StringComparer.OrdinalIgnoreCase)) {
                return new List<string> { $"'{choiceValue}' is not a valid element in subsystem '{Name}'." };
            }
            return new List<string>();
        }
    }
}
