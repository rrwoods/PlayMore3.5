using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PlayMore35.Effects;

namespace PlayMore35.CharacterChoices {
    public class ExternalEffect : IJsonable {
        public List<FlagDefinition> Flags = new List<FlagDefinition>();
        public List<ParameterDefinition> Params = new List<ParameterDefinition>();
        public Effect Effect;

        public JToken ToJson() {
            JObject json = new JObject(new JProperty("effect", Effect.ToJson()));
            json.AddList("flags", Flags);
            json.AddList("params", Params);
            return json;
        }

        public ExternalEffect(JObject json) {
            Effect = new Effect((JObject)json["effect"]);
            if (json["flags"] != null) {
                Flags.AddRange(json["flags"].Select(x => new FlagDefinition((JObject)x)));
            }
            if (json["params"] != null) {
                Params.AddRange(json["params"].Select(x => new ParameterDefinition((JObject)x)));
            }
        }

        public ExternalEffect(JObject json, Character character) : this(json) {
        }
    }
}
