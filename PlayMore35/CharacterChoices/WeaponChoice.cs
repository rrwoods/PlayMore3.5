using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.CharacterChoices {
    public class WeaponChoice : IJsonable {
        public string Name;
        public List<string> MeleeProperties;
        public List<string> SecondHeadProperties;
        public List<string> ProjectileProperties;
        public List<string> ThrownProperties;

        public JToken ToJson() {
            return new JObject(
                new JProperty("name", Name),
                new JProperty("melee properties", new JArray(MeleeProperties)),
                new JProperty("second head properties", new JArray(SecondHeadProperties)),
                new JProperty("projectile properties", new JArray(ProjectileProperties)),
                new JProperty("thrown properties", new JArray(ThrownProperties))
            );
        }

        public WeaponChoice(JObject json) {
            Name = (string)json["name"];
            MeleeProperties = ((JArray)json["melee properties"]).Select(x => (string)x).ToList();
            SecondHeadProperties = ((JArray)json["second head properties"]).Select(x => (string)x).ToList();
            ProjectileProperties = ((JArray)json["projectile properties"]).Select(x => (string)x).ToList();
            ThrownProperties = ((JArray)json["thrown properties"]).Select(x => (string)x).ToList();
        }

        public WeaponChoice() {
            Name = "";
            MeleeProperties = new List<string>();
            SecondHeadProperties = new List<string>();
            ProjectileProperties = new List<string>();
            ThrownProperties = new List<string>();
        }
    }
}
