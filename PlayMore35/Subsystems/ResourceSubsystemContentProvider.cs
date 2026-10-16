using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Subsystems.Maneuvers;

namespace PlayMore35.Subsystems {
    public class ResourceSubsystemContentProvider : ISubsystemContentProvider {
        private readonly Func<byte[]> vestigeAccessor;
        private readonly Func<byte[]> maneuverAccessor;
        private Dictionary<string, Vestige> vestigeCache;
        private Dictionary<string, Maneuver> maneuverCache;

        public ResourceSubsystemContentProvider(Func<byte[]> vestigeAccessor = null, Func<byte[]> maneuverAccessor = null) {
            this.vestigeAccessor = vestigeAccessor ?? (() => Properties.Resources.vestiges);
            this.maneuverAccessor = maneuverAccessor ?? (() => Properties.Resources.maneuvers);
        }

        private Dictionary<string, Vestige> EnsureVestigeCache() {
            if (vestigeCache == null) {
                byte[] bytes = vestigeAccessor();
                string json = Encoding.UTF8.GetString(bytes);
                var array = JArray.Parse(json);
                vestigeCache = array.Select(x => new Vestige((JObject)x)).ToDictionary(x => x.Name, x => x);
            }
            return vestigeCache;
        }

        private Dictionary<string, Maneuver> EnsureManeuverCache() {
            if (maneuverCache == null) {
                byte[] bytes = maneuverAccessor();
                string json = Encoding.UTF8.GetString(bytes);
                var array = JArray.Parse(json);
                maneuverCache = array.Select(x => new Maneuver((JObject)x)).ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);
            }
            return maneuverCache;
        }

        public IEnumerable<Vestige> GetVestiges() {
            return EnsureVestigeCache().Values;
        }

        public Vestige GetVestige(string name) {
            if (string.IsNullOrEmpty(name)) return null;
            return EnsureVestigeCache().TryGetValue(name, out var vestige) ? vestige : null;
        }

        public IEnumerable<Maneuver> GetManeuvers() {
            return EnsureManeuverCache().Values;
        }

        public Maneuver GetManeuver(string name) {
            if (string.IsNullOrEmpty(name)) return null;
            return EnsureManeuverCache().TryGetValue(name, out var maneuver) ? maneuver : null;
        }
    }
}
