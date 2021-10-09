using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;

namespace PlayMore35.CharacterChoices {
    public class ExternalEffect : IJsonable {
        public List<ControllableFlag> Flags = new List<ControllableFlag>();
        public List<ControllableParameter> Params = new List<ControllableParameter>();
        public Effect Effect;

        public JToken ToJson() {
            JObject json = new JObject(new JProperty("effect", Effect.ToJson()));
            json.AddList("flags", Flags);
            json.AddList("params", Params);
            return json;
        }

        public ExternalEffect(JObject json, Character character) {
            Effect = new Effect((JObject)json["effect"]);
            if (json["flags"] != null) {
                Flags.AddRange(json["flags"].Select(x => new ControllableFlag((JObject)x, character)));
            }
            if (json["params"] != null) {
                Params.AddRange(json["params"].Select(x => new ControllableParameter((JObject)x, character)));
            }
        }
    }
}
