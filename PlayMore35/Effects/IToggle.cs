#nullable enable
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public interface IToggle : IJsonable {
        bool On(EffectPool pool, Items.Equipped items);
        IToggle ApplyChoice(string choice);
    }

    public static class Toggle {
        public static IToggle? FromJson(JToken? json) {
            if (json == null) return null;
            if (json.ToString().Equals("always")) {
                return AlwaysOn.Instance;
            }
            if (json["all"] != null) {
                return new AllToggle((JArray)json["all"]!);
            }
            if (json["any"] != null) {
                return new AnyToggle((JArray)json["any"]!);
            }
            if (json["choice"] != null) {
                return new ChoiceToggle((string)json["choice"]!);
            }
            if (json["proficient"] != null) {
                return new ArmorProficiencyToggle((string)json["proficient"]!);
            }
            if (json["conditional"] != null) {
                return new ConditionalToggle((JObject)json["conditional"]!);
            }
            if (json["flag"] != null) {
                return new FlagToggle((string)json["flag"]!);
            }
            if (json["not"] != null) {
                return new UnToggle(json["not"]!);
            }
            return null;
        }
    }

    public sealed class AlwaysOn : IToggle {
        private AlwaysOn() { }
        static AlwaysOn() { }

        public JToken ToJson() {
            return new JValue("always");
        }

        public bool On(EffectPool pool, Items.Equipped items) {
            return true;
        }

        public IToggle ApplyChoice(string choice) {
            return this;
        }

        public static IToggle Instance { get; } = new AlwaysOn();
    }
}
