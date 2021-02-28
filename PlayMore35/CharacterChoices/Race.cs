using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;

namespace PlayMore35.CharacterChoices {
    public class Race : IJsonable {
        public string Name = "";
        public string CreatureType = "";
        public List<string> SubTypes = new List<string>();
        public int SizeCategory = 0;
        public int BaseSpeed = 30;

        public int Str = 0;
        public int Dex = 0;
        public int Con = 0;
        public int Int = 0;
        public int Wis = 0;
        public int Cha = 0;

        public List<string> AutomaticLanguages = new List<string>();
        public List<string> BonusLanguages = new List<string>();
        public List<string> FamiliarWeapons = new List<string>();

        public List<Effect> Effects = new List<Effect>();
        public Dictionary<string, int> BonusFeats = new Dictionary<string, int>();

        public List<ControllableFlag> Flags = new List<ControllableFlag>();
        public List<Statistic> NewStats = new List<Statistic>();

        public JToken ToJson() {
            JObject json = new JObject(new JProperty("name", Name), new JProperty("type", CreatureType));
            json.AddList("subtypes", SubTypes);
            if (SizeCategory != 0) {
                json["size"] = SizeCategory;
            }
            if (BaseSpeed != 30) {
                json["speed"] = BaseSpeed;
            }
            if (Str != 0) {
                json["str"] = Str;
            }
            if (Dex != 0) {
                json["dex"] = Dex;
            }
            if (Con != 0) {
                json["con"] = Con;
            }
            if (Int != 0) {
                json["int"] = Int;
            }
            if (Wis != 0) {
                json["wis"] = Wis;
            }
            if (Cha != 0) {
                json["cha"] = Cha;
            }
            json.AddList("automatic languages", AutomaticLanguages);
            json.AddList("bonus languages", BonusLanguages);
            json.AddList("weapon familiarity", FamiliarWeapons);
            if (BonusFeats.Any()) {
                json["bonus feats"] = new JObject(BonusFeats.Select(x => new JProperty(x.Key, x.Value)));
            }
            json.AddList("effects", Effects);
            json.AddList("flags", Flags);
            json.AddList("stats", NewStats);
            return json;
        }

        public Race(JObject json) {
            Name = (string)json["name"];
            CreatureType = (string)json["type"];
            if (json["subtypes"] != null) {
                SubTypes.AddRange(json["subtypes"].Select(x => (string)x));
            }
            if (json["size"] != null) {
                SizeCategory = (int)json["size"];
            }
            if (json["speed"] != null) {
                BaseSpeed = (int)json["speed"];
            }
            if (json["str"] != null) {
                Str = (int)json["str"];
            }
            if (json["dex"] != null) {
                Dex = (int)json["dex"];
            }
            if (json["con"] != null) {
                Con = (int)json["con"];
            }
            if (json["int"] != null) {
                Int = (int)json["int"];
            }
            if (json["wis"] != null) {
                Wis = (int)json["wis"];
            }
            if (json["cha"] != null) {
                Cha = (int)json["cha"];
            }
            AutomaticLanguages.ReadJson("automatic languages", json);
            BonusLanguages.ReadJson("bonus languages", json);
            FamiliarWeapons.ReadJson("weapon familiarity", json);
            if (json["bonus feats"] != null) {
                foreach (JProperty feat in json["bonus feats"]) {
                    BonusFeats[feat.Name] = (int)feat.Value;
                }
            }
            if (json["flags"] != null) {
                Flags.AddRange(json["flags"].Select(x => new ControllableFlag((JObject)x)));
            }
            if (json["effects"] != null) {
                Effects.AddRange(json["effects"].Select(x => new Effect((JObject)x)));
            }
            if (json["stats"] != null) {
                NewStats.AddRange(json["stats"].Select(x => new Statistic((JObject)x)));
            }
        }

        public Race() {
            
        }
    }
}
