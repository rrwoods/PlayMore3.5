using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class StatisticClass : ISubjectClass<Statistic> {
        private readonly string Tag;

        public StatisticClass(string tag) {
            Tag = tag;
        }

        public bool Matches(Statistic thing) {
            return thing.HasTag(Tag);
        }

        public JToken ToJson() {
            return Tag;
        }
    }
}
