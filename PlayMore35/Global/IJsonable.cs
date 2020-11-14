using Newtonsoft.Json.Linq;

namespace PlayMore35 {
    public interface IJsonable {
        JToken ToJson();
    }
}
