using static PlayMore35.Effects.Constants;

namespace PlayMore35.Items {
    public class WeaponProperty {
        public readonly string Name;

        public WeaponProperty(string name) {
            Name = name;
        }

        // For effects that modify something other than the weapon, like the initiative bonus from the warning property
        public Effects.Effect GlobalEffect;

        public Effects.Modifier AttackModifier = null;
        public Effects.Modifier DamageModifier = null;
        public (int qty, int size) ExtraDamage = (0, 0);

        public static WeaponProperty MASTERWORK = new WeaponProperty("mwk") {
            AttackModifier = new Effects.Modifier(+1, ENHANCEMENT) { Name = "mwk" }
        };

        public static WeaponProperty[] PLUS = new WeaponProperty[] {
            null, // don't ask for a +0 weapon.
            new WeaponProperty("+1") {
                AttackModifier = new Effects.Modifier(+1, ENHANCEMENT) { Name = "weapon" },
                DamageModifier = new Effects.Modifier(+1, ENHANCEMENT) { Name = "weapon" },
            },
            new WeaponProperty("+2") {
                AttackModifier = new Effects.Modifier(+2, ENHANCEMENT) { Name = "weapon" },
                DamageModifier = new Effects.Modifier(+2, ENHANCEMENT) { Name = "weapon" },
            },
            new WeaponProperty("+3") {
                AttackModifier = new Effects.Modifier(+3, ENHANCEMENT) { Name = "weapon" },
                DamageModifier = new Effects.Modifier(+3, ENHANCEMENT) { Name = "weapon" },
            },
            new WeaponProperty("+4") {
                AttackModifier = new Effects.Modifier(+4, ENHANCEMENT) { Name = "weapon" },
                DamageModifier = new Effects.Modifier(+4, ENHANCEMENT) { Name = "weapon" },
            },
            new WeaponProperty("+5") {
                AttackModifier = new Effects.Modifier(+5, ENHANCEMENT) { Name = "weapon" },
                DamageModifier = new Effects.Modifier(+5, ENHANCEMENT) { Name = "weapon" },
            },
        };
    }
}
