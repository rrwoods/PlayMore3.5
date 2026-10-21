using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Global;
using PlayMore35.Subsystems.Maneuvers;

namespace PlayMore35.Subsystems {
    public class SubsystemRegistry {
        public static void RegisterDefaultActionParsers() {
            SheetAction.RegisterParser(ManeuverSheetAction.TryParse);
        }

        public static SubsystemRegistry CreateDefault() {
            var registry = new SubsystemRegistry();
            var catalog = new Rules.GameRulesCatalog();
            var pactDef = catalog.GetSubsystem("pact_magic");
            if (pactDef != null) {
                registry.Register(new SubsystemEngine(pactDef), "vestige");
            }
            registry.Register(new MartialManeuverEngine(), "maneuver");
            return registry;
        }

        private readonly List<ISubsystemEngine> engines = new List<ISubsystemEngine>();
        private readonly Dictionary<string, ISubsystemEngine> enginesById = new Dictionary<string, ISubsystemEngine>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<ISubsystemEngine> All => engines;

        public void Register(ISubsystemEngine engine, string alias = null) {
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

        public T Get<T>() where T : class, ISubsystemEngine {
            return engines.OfType<T>().FirstOrDefault();
        }

        public ISubsystemEngine Get(string id) {
            if (string.IsNullOrEmpty(id)) return null;
            return enginesById.TryGetValue(id, out var engine) ? engine : null;
        }
    }
}
