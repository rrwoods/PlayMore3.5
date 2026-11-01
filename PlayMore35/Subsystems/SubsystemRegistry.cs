using System;
using System.Collections.Generic;
using PlayMore35.CharacterChoices;
using PlayMore35.Global;

namespace PlayMore35.Subsystems {
    public class SubsystemRegistry {
        public static SubsystemRegistry CreateDefault(ICharacterContext? context = null) {
            var registry = new SubsystemRegistry();
            var catalog = context?.Catalog ?? new Rules.GameRulesCatalog();
            var effectiveContext = context ?? new CharacterSession(new Character(), catalog);
            if (catalog.Subsystems != null) {
                foreach (var subDef in catalog.Subsystems.Values) {
                    registry.Register(new SubsystemEngine(subDef, effectiveContext));
                }
            }
            return registry;
        }

        private readonly List<SubsystemEngine> engines = new List<SubsystemEngine>();
        private readonly Dictionary<string, SubsystemEngine> enginesById = new Dictionary<string, SubsystemEngine>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<SubsystemEngine> All => engines;
        internal event Action<SubsystemEngine>? EngineRegistered;

        public void Register(SubsystemEngine engine) {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (!string.IsNullOrEmpty(engine.Id)) {
                if (enginesById.TryGetValue(engine.Id, out var existing)) {
                    engines.Remove(existing);
                }
                enginesById[engine.Id] = engine;
            }
            if (!engines.Contains(engine)) {
                engines.Add(engine);
            }
            EngineRegistered?.Invoke(engine);
        }

        public SubsystemEngine? Get(string? id) {
            if (string.IsNullOrEmpty(id)) return null;
            return enginesById.TryGetValue(id!, out var engine) ? engine : null;
        }
    }
}
