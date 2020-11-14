using PlayMore35.Effects;

using static PlayMore35.Effects.Constants;

namespace PlayMore35.Items {
    public class ArmorProperty {
        public readonly string Name;

        public ArmorProperty(string name) {
            Name = name;
        }

        public Effect GlobalEffect = null;
        public Modifier AcModifier = null;
        public Modifier MaxDexModifier = null;
        public Modifier AcpModifier = null; // ACP is negative, so a "reduction" here should be positive
        public Modifier AsfModifier = null;

        public static ArmorProperty MASTERWORK = new ArmorProperty("mwk") {
            AcpModifier = new Modifier(+1, UNTYPED) { Name = "mwk" }
        };

        public static ArmorProperty[] PLUS = new ArmorProperty[] {
            null, // don't ask for a +0 armor/shield.
            new ArmorProperty("+1") {
                AcpModifier = new Modifier(+1, UNTYPED) { Name = "mwk" },
                AcModifier = new Modifier(+1, ENHANCEMENT) { Name = "enhancement" }
            },
            new ArmorProperty("+2") {
                AcpModifier = new Modifier(+1, UNTYPED) { Name = "mwk" },
                AcModifier = new Modifier(+2, ENHANCEMENT) { Name = "enhancement" }
            },
            new ArmorProperty("+3") {
                AcpModifier = new Modifier(+1, UNTYPED) { Name = "mwk" },
                AcModifier = new Modifier(+3, ENHANCEMENT) { Name = "enhancement" }
            },
            new ArmorProperty("+4") {
                AcpModifier = new Modifier(+1, UNTYPED) { Name = "mwk" },
                AcModifier = new Modifier(+4, ENHANCEMENT) { Name = "enhancement" }
            },
            new ArmorProperty("+5") {
                AcpModifier = new Modifier(+1, UNTYPED) { Name = "mwk" },
                AcModifier = new Modifier(+5, ENHANCEMENT) { Name = "enhancement" }
            },
        };
    }
}
