using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;

namespace PlayMore35.Subsystems {
    public class ResourceSubsystemContentProvider : ISubsystemContentProvider {
        private readonly Func<byte[]> resourceAccessor;
        private Dictionary<string, Vestige> cache;

        public ResourceSubsystemContentProvider(Func<byte[]> resourceAccessor = null) {
            this.resourceAccessor = resourceAccessor ?? (() => Properties.Resources.vestiges);
        }

        private Dictionary<string, Vestige> EnsureCache() {
            if (cache == null) {
                byte[] bytes = resourceAccessor();
                string json = Encoding.UTF8.GetString(bytes);
                var array = JArray.Parse(json);
                cache = array.Select(x => new Vestige((JObject)x)).ToDictionary(x => x.Name, x => x);
            }
            return cache;
        }

        public IEnumerable<Vestige> GetVestiges() {
            return EnsureCache().Values;
        }

        public Vestige GetVestige(string name) {
            if (string.IsNullOrEmpty(name)) return null;
            return EnsureCache().TryGetValue(name, out var vestige) ? vestige : null;
        }
    }
}
