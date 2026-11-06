using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;

namespace PlayMore35.Effects {
    public class ControllableFlag : FlagDefinition {
        private bool on; // not serialized, because it's tied to the character.
        public bool On {
            get { return on; }
            set {
                on = value;
                character.flagValues[Name] = value;
            }
        }
        private readonly Character character;

        public ControllableFlag(FlagDefinition def, Character character)
            : base((JObject)(def ?? throw new System.ArgumentNullException(nameof(def))).ToJson()) {
            this.character = character ?? throw new System.ArgumentNullException(nameof(character));
            this.character.flagValues.TryGetValue(Name, out on);
        }

        public ControllableFlag(JObject json, Character character)
            : base(json) {
            this.character = character ?? throw new System.ArgumentNullException(nameof(character));
            this.character.flagValues.TryGetValue(Name, out on);
        }
    }
}

