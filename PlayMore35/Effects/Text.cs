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

        // If either of these is present, the other must be as well.
        // Any given effect should only have one Text component that has these, or undefined behavior may result
        public string ActionType = "";
        public LengthOfTime RechargeTime = null;

        // If an ability specifies a number of uses that has a fixed component (for example, anklet of translocation
        // is 2, and a cleric's "3 + CHA" turn attempts is 3), it can go here, and when the effect added to a pool
        // via AddEffect, the pool will automagically add a second effect that grants the appropriate number of uses.
        // All variable uses need to be added via a separate modifier that modifies "{name} uses" (for example, "turn undead uses").
        public int StaticUses = 0;

        // Not serialized; these are computed each time the effect is sent to the UI.
        public int DynamicUses = 0;
        public List<string> DynamicUsesExplanation;

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("category", Category),
                new JProperty("text", Description)
            );
            if (RechargeTime != null) {
                json["action"] = ActionType;
                json["recharge"] = RechargeTime.ToJson();
                if (StaticUses != 0) {
                    json["uses"] = StaticUses;
                }
            }
            return json;
        }

        public Text(JObject json) {
            Category = (string)json["category"];
            Description = (string)json["text"];
            if (json["recharge"] != null) {
                ActionType = (string)json["action"];
                RechargeTime = new LengthOfTime((string)json["recharge"]);
                if (json["uses"] != null) {
                    StaticUses = (int)json["uses"];
                }
            }
        }

        public Text() {

        }

        public Text(Text copy) {
            Name = copy.Name;
            Category = copy.Category;
            Description = copy.Description;
            ActionType = copy.ActionType;
            StaticUses = copy.StaticUses;
            RechargeTime = copy.RechargeTime;
        }

        public Text SetName(string name) {
            Name = name;
            return this;
        }

        public Text SetDescription(string newDescription) {
            Description = newDescription;
            return this;
        }

        public Text ComputeUses(EffectPool effects, Equipped items) {
            if (RechargeTime == null || RechargeTime.unit == AT_WILL) {
                return this;
            }
            string usesStat = $"{Name} uses";
            (DynamicUses, DynamicUsesExplanation) = effects.GetStat(usesStat).GetValue(effects, items);
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

        public string ActionUses() {
            return RechargeTime.unit == AT_WILL
                ? $"{ActionType} {AT_WILL}"
                : $"{ActionType} {DynamicUses}/{RechargeTime.ToString()}";
        }
    }
}
