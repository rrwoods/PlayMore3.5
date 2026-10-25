using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;
using PlayMore35.Items;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Effects {
    public class ActionResource : IJsonable {
        public string ActionType;
        public LengthOfTime RechargeTime;

        public string? Duration; // this is a string because the duration can be stat-dependent

        public SheetAction? ActionAfter;

        // If an ability specifies a number of uses that has a fixed component (for example, anklet of translocation
        // is 2, and a cleric's "3 + CHA" turn attempts is 3), it can go here, and when the effect added to a pool
        // via AddEffect, the pool will automagically add a second effect that grants the appropriate number of uses.
        // All variable uses need to be added via a separate modifier that modifies "{name} uses" (for example, "turn undead uses").
        public int StaticUses = 0;

        // Not serialized; these are computed each time the effect is sent to the UI.
        public int DynamicUses = 0;
        public List<string> DynamicUsesExplanation = new List<string>();

        public JToken ToJson() {
            JObject json = new JObject {
                ["action"] = ActionType,
                ["recharge"] = RechargeTime.ToJson()
            };
            if (StaticUses != 0) {
                json["uses"] = StaticUses;
            }
            if (Duration != null) {
                json["duration"] = Duration;
            }
            if (ActionAfter != null) {
                json["after"] = ActionAfter.ToJson();
            }
            return json;
        }

        public ActionResource(JObject json) {
            ActionType = (string?)json["action"] ?? "";
            RechargeTime = new LengthOfTime((string?)json["recharge"] ?? "");
            if (json["uses"] != null) {
                StaticUses = (int)json["uses"]!;
            }
            if (json["duration"] != null) {
                Duration = (string?)json["duration"];
            }
            if (json["after"] != null) {
                ActionAfter = SheetAction.FromJson((JObject)json["after"]!);
            }
        }

        public void ComputeUses(string effectName, EffectPool effects, Equipped items) {
            if (RechargeTime.unit == AT_WILL) {
                return;
            }
            string usesStat = $"{effectName} uses";
            (DynamicUses, DynamicUsesExplanation) = effects.GetStat(usesStat).GetValue(effects, items);
        }

        public LengthOfTime? ComputeDuration(EffectPool effects, Equipped items) {
            if (Duration == null) return null;
            return new LengthOfTime(Duration.FillStatValues(effects, items));
        }

        public string Description() {
            return RechargeTime.unit == AT_WILL
                ? $"{ActionType} {AT_WILL}"
                : $"{ActionType} {DynamicUses}/{RechargeTime.ToString()}";
        }
    }
}
