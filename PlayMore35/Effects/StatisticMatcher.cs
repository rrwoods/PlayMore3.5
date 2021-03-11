using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class StatisticMatcher : ISubjectMatcher<Statistic> {
        private readonly string Tag;

        public StatisticMatcher(string tag) {
            Tag = tag;
        }

        public bool Matches(Statistic thing) {
            return thing.MatchesTag(Tag);
        }

        public JToken ToJson() {
            return Tag;
        }

        public ISubjectMatcher<Statistic> ApplyChoice(string choice) { 
            return new StatisticMatcher(Tag.Replace("$", choice));
        }
    }
}
