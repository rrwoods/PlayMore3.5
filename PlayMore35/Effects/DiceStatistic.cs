using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class DiceStatistic {
        public string Name { get; private set; }
        private readonly List<string> tags = new List<string>();

        public DiceStatistic(string name) {
            Name = name;
            AddTag(name);
        }

        public DiceStatistic AddTag(string tag) {
            tags.Add(tag);
            return this;
        }

        public bool HasTag(string tag) {
            return tags.Contains(tag);
        }

        public (DiceGroup dice, List<string> explanation) GetValue(EffectPool pool) {
            DiceGroup dice = new DiceGroup();
            List<string> explanation = new List<string>();

            foreach (DiceGroup extra in pool.GetExtraDice(this)) {
                dice.AddDice(extra);
                explanation.Add(extra.ToString() + " " + extra.Name);
            }

            return (dice, explanation);
        }
    }
}
