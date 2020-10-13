using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class Statistic {
        private static string[] stackingTypes = { "circumstance", "dodge", "racial", "untyped" };

        public Statistic(string name) {
            this.Name = name;
            this.AddTag(name);
        }

        public string Name { get; private set; }

        private readonly List<string> tags = new List<string>();

        public void AddTag(string tag) {
            this.tags.Add(tag);
        }

        public bool HasTag(string tag) {
            return this.tags.Contains(tag);
        }

        public int GetValue(EffectPool pool) {
            int total = 0;
            Dictionary<string, int> penalties = new Dictionary<string, int>();
            Dictionary<string, int> bonuses = new Dictionary<string, int>();
            foreach (EffectPart part in pool.GetEffects(this)) {
                int amount = part.Amount.Get(pool);
                if (amount == 0) {
                    continue;
                }

                string type = part.Type;
                if (stackingTypes.Contains(type)) {
                    total += amount;
                    continue;
                }

                Dictionary<string, int> dict = (amount < 0) ? penalties : bonuses;
                amount = Math.Abs(amount);

                dict.TryGetValue(type, out int current);
                dict[type] = Math.Max(current, amount);
            }

            return total - penalties.Values.Sum() + bonuses.Values.Sum();
        }
    }
}
