using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public interface IJsonable {
        JToken ToJson();
    }
}
