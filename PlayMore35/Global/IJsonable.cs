using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PlayMore35 {
    public interface IJsonable {
        JToken ToJson();
    }

    public static class JObjectExtensions {
        public static void AddList(this JObject json, string keyName, List<string> list) {
            if (list.Any()) {
                json.Add(keyName, new JArray(list));
            }
        }

        public static void AddList(this JObject json, string keyName, List<int> list) {
            if (list.Any()) {
                json.Add(keyName, new JArray(list));
            }
        }

        public static void AddList<T>(this JObject json, string keyName, List<T> list) where T : IJsonable {
            if (list.Any()) {
                json.Add(keyName, new JArray(list.Select(x => x.ToJson())));
            }
        }

        public static void ReadJson(this List<int> list, string keyName, JObject json) {
            if (json[keyName] != null) {
                list.AddRange(json[keyName].Select(x => (int)x));
            }
        }

        public static void ReadJson(this List<string> list, string keyName, JObject json) {
            if (json[keyName] != null) {
                list.AddRange(json[keyName].Select(x => (string)x));
            }
        }
    }
}
