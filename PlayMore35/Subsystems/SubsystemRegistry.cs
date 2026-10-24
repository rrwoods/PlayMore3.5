#nullable enable
using System;
using System.Collections.Generic;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;

namespace PlayMore35.Subsystems {
    public class SubsystemRegistry {
        public static SubsystemRegistry CreateDefault(ICharacterContext? context = null) {
            var registry = new SubsystemRegistry();
            var catalog = new Rules.GameRulesCatalog();
            var effectiveContext = context ?? new CharacterSession(new Character(), catalog);
            if (catalog.TryGetSubsystem("pact_magic", out var pactDef)) {
                registry.Register(new SubsystemEngine(pactDef, effectiveContext), "vestige");
            }
            if (catalog.TryGetSubsystem("martial_maneuvers", out var martialDef)) {
                registry.Register(new SubsystemEngine(martialDef, effectiveContext), "maneuver");
            }
            return registry;
        }

        private readonly List<SubsystemEngine> engines = new List<SubsystemEngine>();
        private readonly Dictionary<string, SubsystemEngine> enginesById = new Dictionary<string, SubsystemEngine>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<SubsystemEngine> All => engines;

        public void Register(SubsystemEngine engine, string? alias = null) {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (!string.IsNullOrEmpty(engine.Id)) {
                if (enginesById.TryGetValue(engine.Id, out var existing)) {
                    engines.Remove(existing);
                }
                enginesById[engine.Id] = engine;
            }
            if (!string.IsNullOrEmpty(alias)) {
                if (enginesById.TryGetValue(alias, out var existingAlias)) {
                    engines.Remove(existingAlias);
                }
                enginesById[alias] = engine;
            }
            if (!engines.Contains(engine)) {
                engines.Add(engine);
            }
        }

        public SubsystemEngine? Get(string? id) {
            if (string.IsNullOrEmpty(id)) return null;
            return enginesById.TryGetValue(id!, out var engine) ? engine : null;
        }
    }
}
