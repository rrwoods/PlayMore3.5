using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.Subsystems {
    public class SubsystemRegistry {
        private readonly List<ISubsystemEngine> engines = new List<ISubsystemEngine>();
        private readonly Dictionary<string, ISubsystemEngine> enginesById = new Dictionary<string, ISubsystemEngine>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<ISubsystemEngine> All => engines;

        public void Register(ISubsystemEngine engine, string alias = null) {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (!string.IsNullOrEmpty(engine.Id)) {
                enginesById[engine.Id] = engine;
            }
            if (!string.IsNullOrEmpty(alias)) {
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
