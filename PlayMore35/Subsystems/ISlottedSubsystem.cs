namespace PlayMore35.Subsystems {
    public interface ISlottedSubsystem : ISubsystemEngine {
        int SlotCount { get; }
        void ClearSlot(int slotIndex);
    }
}
