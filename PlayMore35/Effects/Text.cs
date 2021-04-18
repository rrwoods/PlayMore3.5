using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class Text : IJsonable {
        public string Category;
        public string Description;

        public JToken ToJson() {
            return new JObject(
                new JProperty("category", Category),
                new JProperty("text", Description)
            );
        }

        public Text(JObject json) {
            Category = (string)json["category"];
            Description = (string)json["text"];
        }

        public Text() {
            Category = "";
            Description = "";
        }

        public Text ApplyChoice(string choice) {
            return new Text {
                Category = Category,
                Description = Description.Replace("$", choice)
            };
        }
    }
}
