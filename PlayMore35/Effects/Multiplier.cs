#nullable enable
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class Multiplier : IJsonable {
        public string Name = "";
        public decimal Factor { get; private set; }

        public JToken ToJson() {
            return Factor;
        }

        public Multiplier(decimal factor) {
            Factor = factor;
        }
    }
}
