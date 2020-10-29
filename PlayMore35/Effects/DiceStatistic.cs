using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class DiceStatistic {
        public string Name { get; private set; }
        private readonly List<string> tags = new List<string>();

        public DiceGroup Dice { get; private set; } = new DiceGroup();

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

        public DiceStatistic AddDice(DiceGroup dice) {
            Dice.AddDice(dice);
            return this;
        }

        public DiceGroup GetValue(EffectPool pool) {
            return new DiceGroup()
                .AddDice(Dice)
                .AddDice(pool.GetExtraDice(this));
        }
    }
}
