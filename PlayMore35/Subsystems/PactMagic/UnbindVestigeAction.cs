using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.PactMagic {
    public class UnbindVestigeAction : SheetAction {
        public int SlotIndex { get; }

        public UnbindVestigeAction(int slotIndex) {
            SlotIndex = slotIndex;
            Name = $"Unbind vestige slot {slotIndex}";
        }

        public UnbindVestigeAction(JObject json) : base(json) {
            SlotIndex = (int)json["unbind_vestige"];
            if (string.IsNullOrEmpty(Name)) {
                Name = $"Unbind vestige slot {SlotIndex}";
            }
        }

        public override TimelineRefreshRequirement Perform(ICharacterContext context) {
            base.Perform(context);
            var engine = context?.Subsystems?.Get<PactMagicEngine>();
            engine?.UnbindVestige(SlotIndex);
            return TimelineRefreshRequirement.FullReload;
        }

        public override JToken ToJson() {
            var json = (JObject)base.ToJson();
            json["unbind_vestige"] = SlotIndex;
            return json;
        }

        public override bool Matches(SheetAction action) {
            return base.Matches(action) &&
                   action is UnbindVestigeAction u &&
                   u.SlotIndex == SlotIndex;
        }
    }
}
