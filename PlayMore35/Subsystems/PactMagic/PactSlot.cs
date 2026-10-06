namespace PlayMore35.Subsystems.PactMagic {
    public class PactSlot {
        public string VestigeName { get; set; } = "";
        public PactState State { get; set; } = PactState.GOOD_PACT;

        public PactSlot() { }

        public PactSlot(string vestigeName, PactState state) {
            VestigeName = vestigeName ?? "";
            State = state;
        }
    }
}
