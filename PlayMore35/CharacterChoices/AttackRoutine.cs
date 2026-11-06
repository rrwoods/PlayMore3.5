using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.Items {
    public enum RoutineActionType {
        Standard,
        FullAttack
    }

    public enum RoutineCategory {
        PrimarySolo,
        TwoWeaponFighting,
        DoubleWeapon,
        ShieldBashSolo,
        OffHandShieldBash,
        ArmorSpikesSolo,
        ArmorSpikesTWF
    }

    public class AttackRoutine {
        public RoutineActionType ActionType { get; }
        public RoutineCategory Category { get; }
        public string DisplayName { get; }
        public IReadOnlyList<Attack> Attacks { get; }

        public AttackRoutine(
            RoutineActionType actionType,
            RoutineCategory category,
            string displayName,
            IEnumerable<Weapon> weapons,
            IEnumerable<Attack> attacks
        ) {
            ActionType = actionType;
            Category = category;
            DisplayName = displayName ?? string.Empty;
            Attacks = (attacks ?? Enumerable.Empty<Attack>()).ToList().AsReadOnly();
        }
    }
}
