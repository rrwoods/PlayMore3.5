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

        public List<string> Toggles = new List<string>();
        public List<Effect> Effects = new List<Effect>();

        public JToken ToJson() {
            JObject json = new JObject(new JProperty("name", Name), new JProperty("type", CreatureType));
            if (SubTypes.Any()) {
                json["subtypes"] = new JArray(SubTypes);
            }
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
            if (AutomaticLanguages.Any()) {
                json["automatic languages"] = new JArray(AutomaticLanguages);
            }
            if (BonusLanguages.Any()) {
                json["bonus languages"] = new JArray(BonusLanguages);
            }
            if (FamiliarWeapons.Any()) {
                json["weapon familiarity"] = new JArray(FamiliarWeapons);
            }
            if (Toggles.Any()) {
                json["toggles"] = new JArray(Toggles);
            }
            if (Effects.Any()) {
                json["effects"] = new JArray(Effects.Select(x => x.ToJson()));
            }
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
            if (json["automatic languages"] != null) {
                AutomaticLanguages.AddRange(json["automatic languages"].Select(x => (string)x));
            }
            if (json["bonus languages"] != null) {
                BonusLanguages.AddRange(json["bonus languages"].Select(x => (string)x));
            }
            if (json["weapon familiarity"] != null) {
                FamiliarWeapons.AddRange(json["weapon familiarity"].Select(x => (string)x));
            }
            if (json["toggles"] != null) {
                Toggles.AddRange(json["toggles"].Select(x => (string)x));
            }
            if (json["effects"] != null) {
                Effects.AddRange(json["effects"].Select(x => new Effect((JObject)x)));
            }
        }
    }
}
