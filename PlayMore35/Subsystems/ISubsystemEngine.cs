using PlayMore35.Effects;
using PlayMore35.Global;

namespace PlayMore35.Subsystems {
    public interface ISubsystemEngine {
        string Id { get; }
        bool IsActive { get; }
        void Initialize(ICharacterContext context);
        void ContributeEffects(EffectPool pool, ICharacterContext context);
    }
}
