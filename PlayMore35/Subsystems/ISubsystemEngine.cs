using System.Collections.Generic;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;

namespace PlayMore35.Subsystems {
    public interface ISubsystemEngine {
        string Id { get; }
        bool IsActive { get; }
        void Initialize(ICharacterContext context);
        void ContributeEffects(EffectPool pool, ICharacterContext context);
        IReadOnlyList<string> GetAccessibleElements(string filter, string targetSource);
        List<string> ValidateChoice(string featSlot, ChoiceDescriptor descriptor, string choiceValue, string targetSource, IDictionary<string, string> currentChoices = null);
    }
}
