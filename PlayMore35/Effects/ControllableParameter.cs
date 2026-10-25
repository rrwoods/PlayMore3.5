#nullable enable
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;

namespace PlayMore35.Effects {
    public class ControllableParameter : ParameterDefinition {
        private int parameterValue; // not serialized, because it's tied to the character.
        private readonly Character character;
        public int Value {
            get { return parameterValue; }
            set {
                parameterValue = value;
                character.paramValues[Name] = value;
            }
        }

        public ControllableParameter(ParameterDefinition def, Character character)
            : base(def.Name, def.Category, def.Description) {
            this.character = character ?? throw new System.ArgumentNullException(nameof(character));
            this.character.paramValues.TryGetValue(Name, out parameterValue);
        }

        public ControllableParameter(JObject json, Character character)
            : base(json) {
            this.character = character ?? throw new System.ArgumentNullException(nameof(character));
            this.character.paramValues.TryGetValue(Name, out parameterValue);
        }

        public ControllableParameter(string name, Character character)
            : base(name) {
            this.character = character ?? throw new System.ArgumentNullException(nameof(character));
            this.character.paramValues.TryGetValue(name, out parameterValue);
        }
    }
}

