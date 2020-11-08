using PlayMore35.Effects;

using static PlayMore35.Effects.Constants;

namespace PlayMore35.Items {
    public class WeaponProperty {
        public readonly string Name;

        public WeaponProperty(string name) {
            Name = name;
        }

        // For effects that modify something other than the weapon, like the initiative bonus from the warning property
        public Effect GlobalEffect = null;

        public ExtraAttack ExtraAttack = null;
        public Modifier AttackModifier = null;
        public Modifier DamageModifier = null;
        public (int qty, int size) ExtraDamage = (0, 0);

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
