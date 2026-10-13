using System.Collections.Generic;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;
using PlayMore35.Subsystems;

namespace PlayMore35.Global {
    public interface ICharacterContext {
        Character Character { get; }
        EffectPool Effects { get; }
        Equipped EquippedItems { get; }
        Timeline Timeline { get; }
        SubsystemRegistry Subsystems { get; }
        List<AttackRoutine> GetAllRoutines();
        List<AttackRoutine> GetVisibleRoutines();
    }
}
