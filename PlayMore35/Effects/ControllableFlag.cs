using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;

namespace PlayMore35.Effects {
    public class ControllableFlag : FlagDefinition {
        private bool on; // not serialized, because it's tied to the character.
        public bool On {
            get { return on; }
            set {
                on = value;
                if (character != null) {
                    character.flagValues[Name] = value;
                }
            }
        }
        private Character character;

        public ControllableFlag(FlagDefinition def, Character character)
            : base(def.Name, def.Category, def.Description, def.Show, def.Resource) {
            this.character = character;
            character?.flagValues.TryGetValue(Name, out on);
        }

        public ControllableFlag(JObject json, Character character)
            : base(json) {
            this.character = character;
            character?.flagValues.TryGetValue(Name, out on);
        }

        public ControllableFlag(string name, Character character, bool on = false)
            : base(name) {
            this.character = character;
            this.on = on;
            if (character != null) {
                character.flagValues[name] = on;
            }
        }

        public void BindCharacter(Character character) {
            this.character = character;
            if (character != null && character.flagValues.TryGetValue(Name, out bool val)) {
                this.on = val;
            }
        }
    }
}

