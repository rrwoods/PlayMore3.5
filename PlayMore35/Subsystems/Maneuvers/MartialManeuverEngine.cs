using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.Maneuvers {
    public class MartialManeuverEngine : ISubsystemEngine {
        public const string SubsystemId = "martial_maneuvers";
        public static readonly string[] StandardSources = new[] { "Warblade", "Crusader", "Swordsage", "Feats" };

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
                if (context.GetStatValue("initiator level", 0) > 0 ||
                    context.GetStatValue("maneuvers known", 0) > 0 ||
                    context.GetStatValue("stances known", 0) > 0) {
                    return true;
                }

                var allSources = StandardSources.Union(state.Sources.Keys, StringComparer.OrdinalIgnoreCase);
                foreach (var source in allSources) {
                    string lower = source.ToLowerInvariant();
                    if (context.GetStatValue($"initiator level:{lower}", 0) > 0 ||
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
                    bool alreadyLearned = false;
                    if (otherSource.Known != null) {
                        for (int i = 0; i < otherSource.Known.Capacity; i++) {
                            var s = otherSource.Known[i];
                            if (s != null && string.Equals(s.ManeuverName, maneuverName, StringComparison.OrdinalIgnoreCase)) {
                                alreadyLearned = true;
                                break;
                            }
                        }
                    }
                    if (!alreadyLearned && otherSource.Stances != null) {
                        for (int i = 0; i < otherSource.Stances.Capacity; i++) {
                            var s = otherSource.Stances[i];
                            if (s != null && string.Equals(s.StanceName, maneuverName, StringComparison.OrdinalIgnoreCase)) {
                                alreadyLearned = true;
                                break;
                            }
                        }
                    }
                    if (alreadyLearned) {
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
            }

            return warnings;
        }

        private int CountKnownInDiscipline(string discipline, string excludingManeuverName) {
            var distinctKnown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var src in state.Sources.Values) {
                if (src.Known != null) {
                    for (int i = 0; i < src.Known.Capacity; i++) {
                        var slot = src.Known[i];
                        if (slot != null && !string.IsNullOrEmpty(slot.ManeuverName) &&
                            !string.Equals(slot.ManeuverName, excludingManeuverName, StringComparison.OrdinalIgnoreCase)) {
                            var m = contentProvider.GetManeuver(slot.ManeuverName);
                            if (m != null && string.Equals(m.Discipline, discipline, StringComparison.OrdinalIgnoreCase)) {
                                distinctKnown.Add(slot.ManeuverName);
                            }
                        }
                    }
                }

                if (src.Stances != null) {
                    for (int i = 0; i < src.Stances.Capacity; i++) {
                        var slot = src.Stances[i];
                        if (slot != null && !string.IsNullOrEmpty(slot.StanceName) &&
                            !string.Equals(slot.StanceName, excludingManeuverName, StringComparison.OrdinalIgnoreCase)) {
                            var m = contentProvider.GetManeuver(slot.StanceName);
                            if (m != null && string.Equals(m.Discipline, discipline, StringComparison.OrdinalIgnoreCase)) {
                                distinctKnown.Add(slot.StanceName);
                            }
                        }
                    }
                }
            }

            return distinctKnown.Count;
        }

        public void ContributeEffects(EffectPool pool, ICharacterContext context) {
            if (pool == null || context == null) return;
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
        }
    }
}
