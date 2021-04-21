using Newtonsoft.Json.Linq;
using PlayMore35.Items;
using System.Collections.Generic;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Effects {
    public class Text : IJsonable {
        // not serialized; only populated when shipping effects over to the UI.
        public string Name = "";

        public string Category = "";
        public string Description = "";

        // Any given effect should only have one Text component that has this, or undefined behavior may result
        public ActionResource Resource = null;

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("category", Category),
                new JProperty("text", Description)
            );
            if (Resource != null) {
                json["resource"] = Resource.ToJson();
            }
            return json;
        }

        public Text(JObject json) {
            Category = (string)json["category"];
            Description = (string)json["text"];
            if (json["resource"] != null) {
                Resource = new ActionResource((JObject)json["resource"]);
            }
        }

        public Text() {

        }

        public Text(Text copy) {
            Name = copy.Name;
            Category = copy.Category;
            Description = copy.Description;
            Resource = copy.Resource;
        }

        public Text SetName(string name) {
            Name = name;
            return this;
        }

        public Text SetDescription(string newDescription) {
            Description = newDescription;
            return this;
        }

        public Text ApplyChoice(string choice) {
            return new Text(this).SetDescription(Description.Replace("$", choice));
        }

        public Text FillStatValues(EffectPool effects, Equipped items, string name) {
            string newDescription = Description;
            while (newDescription.Contains("{{")) {
                int start = newDescription.IndexOf("{{") + 2;
                int end = newDescription.IndexOf("}}", start);
                string statName = newDescription.Substring(start, end - start);
                int statValue = effects.GetStat(statName).GetValue(effects, items).value;
                newDescription = newDescription.Replace("{{" + statName + "}}", "" + statValue);
            }
            return new Text(this).SetName(name).SetDescription(newDescription).ComputeUses(effects, items);
        }

        public Text ComputeUses(EffectPool effects, Equipped items) {
            if (Resource != null) {
                Resource.ComputeUses(Name, effects, items);
            }
            return this;
        }
    }
}
