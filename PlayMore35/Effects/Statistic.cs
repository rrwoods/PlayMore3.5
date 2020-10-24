using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class Statistic {
        private static string[] stackingTypes = { "circumstance", "dodge", "racial", "untyped" };

        public Statistic(string name) {
            Name = name;
            AddTag(name);
        }

        // For things like "an Initiative check is a Dexterity check",
        // or setting up Dexterity checks themselves to be the Dexterity modifier plus some stuff.
        public Statistic(string name, Statistic basedOn) {
            Name = name;
            tags.AddRange(basedOn.tags);
            ignoredModifierTypes.AddRange(basedOn.ignoredModifierTypes);
        }

        public string Name { get; private set; }

        private readonly List<string> tags = new List<string>();

        public Statistic AddTag(string tag) {
            tags.Add(tag);
            return this; // method chaining
        }

        public bool HasTag(string tag) {
            return tags.Contains(tag);
        }

        private readonly List<string> ignoredModifierTypes = new List<string>();

        public Statistic ignoreModifierType(string type) {
            ignoredModifierTypes.Add(type);
            return this; // method chaining
        }

        public int GetValue(EffectPool pool) {
            int total = 0;
            Dictionary<string, int> penalties = new Dictionary<string, int>();
            Dictionary<string, int> bonuses = new Dictionary<string, int>();
            foreach (Modifier mod in pool.GetModifiers(this)) {
                int amount = mod.ComputeAmount(pool);
                if (amount == 0) {
                    continue;
                }

                string type = mod.Type;
                if (ignoredModifierTypes.Contains(type)) {
                    continue;
                }

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
