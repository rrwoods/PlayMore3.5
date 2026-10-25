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
            : base(def.Name, def.Category, def.Description, def.Show, def.Resource) {
            this.character = character ?? throw new System.ArgumentNullException(nameof(character));
            this.character.flagValues.TryGetValue(Name, out on);
        }

        public ControllableFlag(JObject json, Character character)
            : base(json) {
            this.character = character ?? throw new System.ArgumentNullException(nameof(character));
            this.character.flagValues.TryGetValue(Name, out on);
        }

        public ControllableFlag(string name, Character character, bool on = false)
            : base(name) {
            this.character = character ?? throw new System.ArgumentNullException(nameof(character));
            this.on = on;
            this.character.flagValues[name] = on;
        }
    }
}

