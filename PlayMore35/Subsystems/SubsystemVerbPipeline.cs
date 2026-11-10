using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems {
    public class SubsystemVerbPipeline {
        private readonly SubsystemEngine engine;

        public SubsystemEngine Engine => engine;

        public SubsystemVerbPipeline(SubsystemEngine engine) {
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public TimelineRefreshRequirement ExecuteVerb(string verbName, SubsystemTarget? target) {
            return ExecuteVerb(verbName, target?.SlotGroup, target?.SlotIndex, target?.Element, target?.Source, target?.SelectedIndices);
        }

        public TimelineRefreshRequirement ExecuteVerb(string verbName, string? slotGroup = null, int? slotIndex = null, string? elementName = null, string? source = null, List<int>? selectedIndices = null) {
            if (string.IsNullOrEmpty(verbName)) {
                return TimelineRefreshRequirement.None;
            }

            // When invoked across all sources (e.g. reset_encounter with source == null and no specific slot targeted),
            // dispatch per source if multiple sources or source-specific verb overrides exist.
            if (string.IsNullOrEmpty(source) && !slotIndex.HasValue && string.IsNullOrEmpty(elementName)) {
                var slotSources = engine.SlotGroups.Values
                    .SelectMany(g => g.Slots)
                    .Where(s => !string.IsNullOrEmpty(s.Source))
                    .Select(s => s.Source!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                bool anySourceOverride = slotSources.Any(src =>
                    engine.Context?.Catalog != null &&
                    engine.Context.Catalog.TryGetClass(src, out var cls) &&
                    cls.Subsystems != null &&
                    cls.Subsystems.TryGetValue(engine.Id, out var sf) &&
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
                var group = engine.GetSlotGroup(slotGroup);
                if (group != null) {
                    if (slotIndex.HasValue && slotIndex.Value >= 0) {
                        int flatIdx = engine.ResolveFlatSlotIndex(slotGroup, slotIndex.Value, source, false);
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
                foreach (var kvp in engine.SlotGroups) {
                    var grp = kvp.Value;
                    if (slotIndex.HasValue && slotIndex.Value >= 0) {
                        int flatIdx = engine.ResolveFlatSlotIndex(kvp.Key, slotIndex.Value, source, false);
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

            var target = new SubsystemTarget(slotGroup, slotIndex, elementName, effectiveSource, selectedIndices);
            TimelineRefreshRequirement requirement = TimelineRefreshRequirement.None;
            foreach (var step in steps) {
                var req = ExecuteStep(step, target);
                if (step.IsTransition && req == TimelineRefreshRequirement.None) {
                    break;
                }
                if (req > requirement) requirement = req;
            }

            return requirement;
        }

        public TimelineRefreshRequirement InvokeElementVerb(string verbName, SubsystemSlot slot) {
            if (string.IsNullOrEmpty(verbName) || slot == null || string.IsNullOrEmpty(slot.Element)) {
                return TimelineRefreshRequirement.None;
            }

            var elem = engine.GetElement(slot.Element);
            List<SubsystemStep>? steps = null;

            if (elem != null && elem.Verbs.TryGetValue(verbName, out var elemSteps)) {
                steps = elemSteps;
            } else if (engine.Definition.DefaultElementVerbs.TryGetValue(verbName, out var defaultSteps)) {
                steps = defaultSteps;
            }

            if (steps == null || steps.Count == 0) {
                return TimelineRefreshRequirement.None;
            }

            var target = new SubsystemTarget(element: slot.Element, source: slot.Source);
            TimelineRefreshRequirement requirement = TimelineRefreshRequirement.None;
            foreach (var step in steps) {
                var req = ExecuteStep(step, target);
                if (req > requirement) requirement = req;
            }
            return requirement;
        }

        private TimelineRefreshRequirement ExecuteTransitionStep(SubsystemStep step, SubsystemTarget? target) {
            string groupName = !string.IsNullOrEmpty(step.SlotGroup) ? step.SlotGroup : target?.SlotGroup ?? "";
            if (string.IsNullOrEmpty(groupName)) return TimelineRefreshRequirement.None;
            var group = engine.GetSlotGroup(groupName);
            if (group == null) return TimelineRefreshRequirement.None;

            string selector = step.Selector?.ToLowerInvariant() ?? "invoked";
            string from = step.FromState ?? "";
            string to = step.ToState ?? "";
            engine.Definition.SlotGroups.TryGetValue(groupName, out var groupDef);
            bool anyTransitioned = false;

            bool MatchesFrom(SubsystemSlot slot) {
                if (string.IsNullOrEmpty(slot.Element)) return false;
                return MatchesState(slot.State, from);
            }

            int rawIndex = target?.SlotIndex ?? -1;
            string? targetElement = target?.Element;
            string? targetSource = target?.Source;
            var selectedIndices = target?.SelectedIndices;

            void TransitionSlot(SubsystemSlot slot) {
                string priorState = slot.State;
                string elemName = slot.Element;

                var targetCap = groupDef?.StateCapacities.FirstOrDefault(c => c.States.Any(s => string.Equals(s, to, StringComparison.OrdinalIgnoreCase)));
                if (targetCap != null) {
                    if (string.Equals(priorState, to, StringComparison.OrdinalIgnoreCase)) {
                        anyTransitioned = true;
                        return;
                    }
                    bool priorInTarget = targetCap.States.Any(s => string.Equals(s, priorState, StringComparison.OrdinalIgnoreCase));
                    if (!priorInTarget) {
                        string statName = targetCap.Stat.Contains("{source}") && !string.IsNullOrEmpty(slot.Source)
                            ? targetCap.Stat.Replace("{source}", slot.Source.Trim().ToLowerInvariant())
                            : targetCap.Stat;
                        int maxActive = Math.Max(1, engine.ResolveStatOrParam(statName, 1));
                        var currentlyActive = group.Slots
                            .Where(s => !string.IsNullOrEmpty(s.Element) && s != slot &&
                                        (string.IsNullOrEmpty(slot.Source) || !targetCap.Stat.Contains("{source}") || string.Equals(s.Source, slot.Source, StringComparison.OrdinalIgnoreCase)) &&
                                        targetCap.States.Any(st => string.Equals(st, s.State, StringComparison.OrdinalIgnoreCase)))
                            .ToList();

                        string inactiveState = !string.IsNullOrEmpty(groupDef.DefaultState) ? groupDef.DefaultState : "inactive";
                        while (currentlyActive.Count >= maxActive) {
                            SubsystemSlot? toDeactivate = null;
                            if (rawIndex >= 0 && selectedIndices != null && selectedIndices.Count > 0) {
                                int repFlatIdx = engine.ResolveFlatSlotIndex(groupName, selectedIndices[0], targetSource, false);
                                if (repFlatIdx >= 0 && repFlatIdx < group.Slots.Count) {
                                    var candidate = group.Slots[repFlatIdx];
                                    if (currentlyActive.Contains(candidate)) {
                                        toDeactivate = candidate;
                                    }
                                }
                            }
                            if (toDeactivate == null) {
                                toDeactivate = engine.ActiveSlotOrder.FirstOrDefault(s => currentlyActive.Contains(s)) ?? currentlyActive[0];
                            }
                            toDeactivate.State = inactiveState;
                            currentlyActive.Remove(toDeactivate);
                            engine.ActiveSlotOrder.Remove(toDeactivate);
                        }
                    }
                    engine.ActiveSlotOrder.Remove(slot);
                    engine.ActiveSlotOrder.Add(slot);
                } else {
                    engine.ActiveSlotOrder.Remove(slot);
                }

                slot.State = to;
                anyTransitioned = true;
                if (string.Equals(to, "empty", StringComparison.OrdinalIgnoreCase)) {
                    engine.ResetElementParameters(elemName);
                    slot.Element = "";
                } else if (!string.Equals(priorState, to, StringComparison.OrdinalIgnoreCase)) {
                    engine.ResetDeactivatedParameters(elemName, groupName, priorState, to);
                }
            }

            switch (selector) {
                case "invoked":
                    if (rawIndex < 0 && string.IsNullOrEmpty(targetElement) && selectedIndices != null && selectedIndices.Count > 0) {
                        foreach (var idx in selectedIndices) {
                            int flatIdx = engine.ResolveFlatSlotIndex(groupName, idx, targetSource, false);
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
                        int flatIdx = engine.ResolveFlatSlotIndex(groupName, rawIndex, targetSource, false);
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
                    if (selectedIndices != null && selectedIndices.Count > 0) {
                        foreach (var idx in selectedIndices) {
                            int flatIdx = engine.ResolveFlatSlotIndex(groupName, idx, targetSource, false);
                            if (flatIdx >= 0 && flatIdx < group.Slots.Count) {
                                var slot = group.Slots[flatIdx];
                                if (MatchesFrom(slot)) {
                                    TransitionSlot(slot);
                                }
                            }
                        }
                    } else {
                        int selIndex = rawIndex;
                        if (selIndex >= 0) {
                            int flatIdx = engine.ResolveFlatSlotIndex(groupName, selIndex, targetSource, false);
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
                    if (candidates.Count == 0 && !string.IsNullOrEmpty(step.OnEmptyVerb)) {
                        return ExecuteVerb(step.OnEmptyVerb, groupName, rawIndex >= 0 ? (int?)rawIndex : null, targetElement, targetSource, selectedIndices);
                    }
                    int count = ResolveCount(step.CountExpression, targetSource);
                    if (candidates.Count <= count) {
                        foreach (var c in candidates) {
                            TransitionSlot(c);
                        }
                    } else {
                        var poolList = new List<SubsystemSlot>(candidates);
                        for (int i = 0; i < count && poolList.Count > 0; i++) {
                            int pick = engine.Rng.Next(poolList.Count);
                            TransitionSlot(poolList[pick]);
                            poolList.RemoveAt(pick);
                        }
                    }
                    break;
            }

            if (!anyTransitioned) {
                return TimelineRefreshRequirement.None;
            }

            engine.SyncState();
            return TimelineRefreshRequirement.FullReload;
        }

        private List<SubsystemStep>? ResolveVerbSteps(string verbName, string? source) {
            if (!string.IsNullOrEmpty(source) && engine.Context?.Catalog != null) {
                if (engine.Context.Catalog.TryGetClass(source!, out var cls) && cls.Subsystems != null && cls.Subsystems.TryGetValue(engine.Id, out var subFeature)) {
                    if (subFeature.Verbs != null && subFeature.Verbs.TryGetValue(verbName, out var classSteps)) {
                        return classSteps;
                    }
                }
            }
            if (engine.Definition.Verbs.TryGetValue(verbName, out var defSteps)) {
                return defSteps;
            }
            return null;
        }

        public TimelineRefreshRequirement ExecuteStep(SubsystemStep step, SubsystemTarget? target = null) {
            if (step == null) return TimelineRefreshRequirement.None;
            string type = step.Type?.ToLowerInvariant() ?? "";

            switch (type) {
                case "assign":
                    string? groupName = !string.IsNullOrEmpty(step.SlotGroup) ? step.SlotGroup : target?.SlotGroup;
                    string targetState = !string.IsNullOrEmpty(step.ToState) ? step.ToState : "assigned";
                    int selIndex = target?.SlotIndex ?? -1;
                    string? targetSource = target?.Source;
                    string? targetElement = target?.Element;

                    if (selIndex == -1 && !string.IsNullOrEmpty(groupName)) {
                        var slots = engine.GetSlots(groupName, targetSource);
                        for (int i = 0; i < slots.Count; i++) {
                            if (string.IsNullOrEmpty(slots[i].Element)) {
                                selIndex = i;
                                break;
                            }
                        }
                        if (selIndex == -1) selIndex = slots.Count;
                    }
                    if (selIndex >= 0 && !string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(targetElement)) {
                        engine.AssignSlot(groupName, selIndex, targetElement, targetState, targetSource, null);
                    }
                    return TimelineRefreshRequirement.FullReload;

                case "transition":
                    return ExecuteTransitionStep(step, target);

                case "invoke_element_verb":
                    string elemGroup = !string.IsNullOrEmpty(step.SlotGroup) ? step.SlotGroup : target?.SlotGroup ?? "";
                    int? elemIndex = target?.SlotIndex;
                    string? elemName = target?.Element;
                    string? elemSource = target?.Source;

                    SubsystemSlot? slot = null;
                    if (!string.IsNullOrEmpty(elemGroup) && elemIndex.HasValue && elemIndex.Value >= 0) {
                        slot = engine.GetSlot(elemGroup, elemIndex.Value, elemSource);
                    }
                    if (slot == null && !string.IsNullOrEmpty(elemName)) {
                        slot = new SubsystemSlot(elemName, "invoked", elemSource);
                    }
                    return slot != null && !string.IsNullOrEmpty(step.Verb) ? InvokeElementVerb(step.Verb, slot) : TimelineRefreshRequirement.None;

                case "action":
                    if (step.Action != null) {
                        return step.Action.Perform(engine.Context);
                    }
                    return TimelineRefreshRequirement.None;

                case "pool":
                    if (string.IsNullOrEmpty(step.PoolId)) return TimelineRefreshRequirement.None;
                    string op = step.Op?.ToLowerInvariant() ?? "consume";
                    switch (op) {
                        case "consume":
                            engine.ConsumePool(step.PoolId, step.Amount);
                            break;
                        case "recharge":
                            engine.RechargePool(step.PoolId, step.Amount);
                            break;
                        case "set":
                            engine.SetPoolValue(step.PoolId, step.Amount);
                            break;
                    }
                    return TimelineRefreshRequirement.RecomputeDerived;

                default:
                    return TimelineRefreshRequirement.None;
            }
        }

        private int ResolveCount(string? countExpr, string? source) {
            if (string.IsNullOrEmpty(countExpr)) return 1;
            if (int.TryParse(countExpr, out int direct)) return Math.Max(0, direct);

            string statName = countExpr;
            if (!string.IsNullOrEmpty(source) && statName.Contains("{source}")) {
                statName = statName.Replace("{source}", source.Trim().ToLowerInvariant());
            }

            if (engine.Context != null) {
                return Math.Max(0, engine.Context.GetStatValue(statName, 1));
            }
            return 1;
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
    }
}
