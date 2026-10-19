using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class Alterer<AlterType, SubjectType> : IJsonable where AlterType : IJsonable {
        public AlterType mod;
        public ISubjectMatcher<SubjectType> affects;
        public IToggle Toggle = AlwaysOn.Instance;

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("affects", affects.ToJson()),
                new JProperty("mod", mod.ToJson())
            );
            if (Toggle != AlwaysOn.Instance) {
                json.Add("active", Toggle.ToJson());
            }
            return json;
        }

        public Alterer<AlterType, SubjectType> ApplyChoice(string choice) {
            return new Alterer<AlterType, SubjectType> {
                mod = mod,
                affects = affects.ApplyChoice(choice),
                Toggle = Toggle.ApplyChoice(choice)
            };
        }
    }
}
