using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlayMore35.CharacterChoices {
    public class WeaponChoice : IJsonable {
        public string Name;
        public int SizeCategory;
        public List<string> MeleeProperties;
        public List<string> SecondHeadProperties;
        public List<string> ProjectileProperties;
        public List<string> ThrownProperties;

        public JToken ToJson() {
            return new JObject(
                new JProperty("name", Name),
                new JProperty("size", SizeCategory),
                new JProperty("melee properties", new JArray(MeleeProperties)),
                new JProperty("second head properties", new JArray(SecondHeadProperties)),
                new JProperty("projectile properties", new JArray(ProjectileProperties)),
                new JProperty("thrown properties", new JArray(ThrownProperties))
            );
        }

        public WeaponChoice(JObject json) {
            Name = (string)json["name"];
            SizeCategory = (int)json["size"];
            MeleeProperties = ((JArray)json["melee properties"]).Select(x => (string)x).ToList();
            SecondHeadProperties = ((JArray)json["second head properties"]).Select(x => (string)x).ToList();
            ProjectileProperties = ((JArray)json["projectile properties"]).Select(x => (string)x).ToList();
            ThrownProperties = ((JArray)json["thrown properties"]).Select(x => (string)x).ToList();
        }

        public WeaponChoice() {
            Name = "";
            SizeCategory = 0;
            MeleeProperties = new List<string>();
            SecondHeadProperties = new List<string>();
            ProjectileProperties = new List<string>();
            ThrownProperties = new List<string>();
        }

        public string FullName() {
            return
                string.Join("", MeleeProperties.Select(x => x + " ")) +
                string.Join("", SecondHeadProperties.Select(x => x + " ")) +
                string.Join("", ProjectileProperties.Select(x => x + " ")) +
                string.Join("", ThrownProperties.Select(x => x + " ")) +
                Name;
        }

        public override string ToString() {
            return FullName();
        }
    }
}
