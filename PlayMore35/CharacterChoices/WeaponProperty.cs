using PlayMore35.Effects;
using Newtonsoft.Json.Linq;

using static PlayMore35.Effects.Constants;

namespace PlayMore35.Items {
    public class WeaponProperty : IJsonable {
        public readonly string Name;

        // For effects that modify something other than the weapon, like the initiative bonus from the warning property
        public Effect GlobalEffect = null;

        public ExtraAttack ExtraAttack = null;
        public Modifier AttackModifier = null;
        public Modifier DamageModifier = null;
        public (int qty, int size) ExtraDamage = (0, 0);

        public JToken ToJson() {
            JObject json = new JObject(new JProperty("name", Name));
            if (GlobalEffect != null) {
                json["effect"] = GlobalEffect.ToJson();
            }
            if (ExtraAttack != null) {
                json["extra attack"] = ExtraAttack.ToJson();
            }
            if (AttackModifier != null) {
                json["attack modifier"] = AttackModifier.ToJson();
            }
            if (DamageModifier != null) {
                json["damage modifier"] = DamageModifier.ToJson();
            }
            if (ExtraDamage.qty != 0) {
                json["extra damage"] = DiceGroup.ToDNotation(ExtraDamage);
            }
            return json;
        }

        public WeaponProperty(JObject json) {
            Name = (string)json["name"];
            if (json["effect"] != null) {
                GlobalEffect = new Effect((JObject)json["effect"]);
            }
            if (json["extra attack"] != null) {
                ExtraAttack = new ExtraAttack((JObject)json["extra attack"]);
            }
            if (json["attack modifier"] != null) {
                AttackModifier = new Modifier((JObject)json["attack modifier"]);
            }
            if (json["damage modifier"] != null) {
                DamageModifier = new Modifier((JObject)json["damage modifier"]);
            }
            if (json["extra damage"] != null) {
                ExtraDamage = DiceGroup.FromDNotation((string)json["extra damage"]);
            }
        }

        public WeaponProperty(string name) {
            Name = name;
        }

        public static WeaponProperty MASTERWORK = new WeaponProperty("mwk") {
            AttackModifier = new Modifier(+1, ENHANCEMENT) { Name = "mwk" }
        };

        public static WeaponProperty[] PLUS = new WeaponProperty[] {
            null, // don't ask for a +0 weapon.
            new WeaponProperty("+1") {
                AttackModifier = new Modifier(+1, ENHANCEMENT) { Name = "enhancement" },
                DamageModifier = new Modifier(+1, ENHANCEMENT) { Name = "enhancement" },
            },
            new WeaponProperty("+2") {
                AttackModifier = new Modifier(+2, ENHANCEMENT) { Name = "enhancement" },
                DamageModifier = new Modifier(+2, ENHANCEMENT) { Name = "enhancement" },
            },
            new WeaponProperty("+3") {
                AttackModifier = new Modifier(+3, ENHANCEMENT) { Name = "enhancement" },
                DamageModifier = new Modifier(+3, ENHANCEMENT) { Name = "enhancement" },
            },
            new WeaponProperty("+4") {
                AttackModifier = new Modifier(+4, ENHANCEMENT) { Name = "enhancement" },
                DamageModifier = new Modifier(+4, ENHANCEMENT) { Name = "enhancement" },
            },
            new WeaponProperty("+5") {
                AttackModifier = new Modifier(+5, ENHANCEMENT) { Name = "enhancement" },
                DamageModifier = new Modifier(+5, ENHANCEMENT) { Name = "enhancement" },
            },
        };
    }
}
