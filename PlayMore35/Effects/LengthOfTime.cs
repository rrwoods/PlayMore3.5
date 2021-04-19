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

        public JToken ToJson() {
            return ToString();
        }

        public LengthOfTime(string json) {
            if (json.Contains(" ")) {
                string[] qtyAndUnit = json.Split(' ');
                quantity = new DiceGroup((JToken)qtyAndUnit[0]);
                unit = qtyAndUnit[1];
            } else {
                quantity = new DiceGroup(1, 1);
                unit = json;
            }
        }

        public override string ToString() {
            return quantity.IsSingleOne()
                ? unit
                : $"{quantity.ToString()} {unit}";
        }
    }
}
