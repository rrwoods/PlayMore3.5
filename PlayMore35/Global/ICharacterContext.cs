using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;

namespace PlayMore35.Global {
    public interface ICharacterContext {
        Character Character { get; }
        EffectPool Effects { get; }
        Equipped EquippedItems { get; }
        Rounds Timeline { get; }
        void ClearSlot(string subsystem, int slotIndex);
    }
}
