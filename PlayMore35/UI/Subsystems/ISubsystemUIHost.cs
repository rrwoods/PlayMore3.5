using PlayMore35.Global;

namespace PlayMore35.UI.Subsystems {
    public interface ISubsystemUIHost {
        ICharacterSession Session { get; }
        void RequestRecalculate();
        void RequestEffectsRefresh();
        void RequestTimelineRefresh();
    }
}
