using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;

namespace PlayMore35.Effects {
    public class ControllableParameter : IJsonable {
        public string Name;
        public string Category; // this is blank for "uses" parameters.
        public string Description; // this is blank for "uses" parameters.

        private int parameterValue; // not serialized, because it's tied to the character.
        private Character character;
        public int Value {
            get { return parameterValue; }
            set {
                parameterValue = value;
                character.paramValues[Name] = value;
            }
        }

        public JToken ToJson() {
            return new JObject(
                new JProperty("name", Name),
                new JProperty("category", Category),
                new JProperty("description", Description)
            );
        }

        public ControllableParameter(JObject json, Character character) {
            Name = (string)json["name"];
            Category = (string)json["category"];
            Description = (string)json["description"];
            this.character = character;
            parameterValue = 0;
        }

        public ControllableParameter(Character character) {
            this.character = character;
            parameterValue = 0;
        }
    }
}
