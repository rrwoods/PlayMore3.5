using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class Alterer<AlterType, SubjectType> : IJsonable where AlterType : IJsonable {
        public AlterType mod;
        public ISubjectMatcher<SubjectType> affects;

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("affects", affects.ToJson()),
                new JProperty("mod", mod.ToJson())
            );
            return json;
        }
    }
}
