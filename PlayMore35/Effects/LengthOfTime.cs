#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class LengthOfTime : IJsonable {
        // for at-will abilities, just use 1.  when parsing from json this will just work when parsing "at-will"
        public DiceGroup quantity;
        // this is where "at-will" goes
        public string unit;

        public static Dictionary<string, long> UNIT_ROUNDS = new Dictionary<string, long> {
            ["round"] = 1, ["minute"] = 10, ["hour"] = 600, ["day"] = 14400, ["week"] = 100800,
        };

        public JToken ToJson() {
            return ToString();
        }

        public LengthOfTime(string json) {
            if (json.EndsWith("s")) {
                json = json.Remove(json.Length - 1);
            }
            if (json.Contains(" ")) {
                string[] qtyAndUnit = json.Split(' ');
                quantity = new DiceGroup((JToken)qtyAndUnit[0]);
                unit = qtyAndUnit[1];
            } else {
                quantity = new DiceGroup(1, 1);
                unit = json;
            }
        }

        // this DOES NOT CURRENTLY WORK with variable durations, it will silently report 0 for all die results.
        // eventually i want to have a version that accepts a roll result.
        public long InRounds() {
            if (unit == "at-will") {
                return 0;
            }
            if (unit == "encounter") {
                return -1;
            }
            return UNIT_ROUNDS[unit] * (quantity.dice.ContainsKey(1) ? quantity.dice[1] : 0);
        }

        public override string ToString() {
            return quantity.IsSingleOne()
                ? unit
                : $"{quantity.ToString()} {unit}";
        }
    }
}
