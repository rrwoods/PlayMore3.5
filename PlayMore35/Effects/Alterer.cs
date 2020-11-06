using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class Alterer<AlterType, SubjectType> : IJsonable where AlterType : IJsonable {
        public AlterType mod;
        public ISubjectClass<SubjectType> affects;

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("mod", mod.ToJson()),
                new JProperty("affects", affects.ToJson())
            );
            return json;
        }
    }
}
