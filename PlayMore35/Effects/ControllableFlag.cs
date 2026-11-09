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

        public ControllableFlag(
            string name,
            Character character,
            string category = "",
            string description = "",
            IToggle? show = null,
            ActionResource? resource = null,
            bool on = false)
            : base(name, category, description, show, resource) {
            this.character = character ?? throw new System.ArgumentNullException(nameof(character));
            if (on) {
                this.character.flagValues[Name] = true;
            }
            this.character.flagValues.TryGetValue(Name, out this.on);
        }

        public ControllableFlag(FlagDefinition def, Character character)
            : this(
                (def ?? throw new System.ArgumentNullException(nameof(def))).Name,
                character,
                def.Category,
                def.Description,
                def.Show,
                def.Resource) {
        }

        public ControllableFlag(JObject json, Character character)
            : this(
                (string?)json?["name"] ?? "",
                character,
                (string?)json?["category"] ?? "",
                (string?)json?["description"] ?? "",
                json?["show"] != null ? Toggle.FromJson(json["show"]!) : AlwaysOn.Instance,
                json?["resource"] != null ? new ActionResource((JObject)json["resource"]!) : null) {
        }
    }
}

