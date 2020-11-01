using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class DiceGroup {
        // {size => quantity}, except that die size of 1 represents a flat amount
        private Dictionary<int, int> dice = new Dictionary<int, int>();

        public string Name = "";

        public DiceGroup() {

        }

        public DiceGroup(int quantity, int size) {
            if (quantity != 0) {
                dice[size] = quantity;
            }
        }

        public DiceGroup((int quantity, int size) dice)
            : this(dice.quantity, dice.size) {

        }

        public DiceGroup AddDice(int quantity, int size) {
            if (quantity == 0) {
                return this;
            }

            if (dice.ContainsKey(size)) {
                dice[size] += quantity;
                // the use case here is applying a flat modifier that cancels out another flat modifier
                if (dice[size] == 0) {
                    dice.Remove(size);
                }
            } else {
                dice[size] = quantity;
            }

            return this;
        }

        public DiceGroup AddDice(DiceGroup additionalDice) {
            foreach (var die in additionalDice.dice) {
                AddDice(die.Value, die.Key);
            }
            return this;
        }

        public void Multiply(int multiplier) {
            foreach (int size in dice.Keys.ToList()) { // copy the key list to keep from having its order shift while we iterate
                dice[size] *= multiplier;
            }
        }

        public override string ToString() {
            if (!dice.Any()) {
                return "—";
            }

            List<string> components = new List<string>();
            foreach(var die in dice.OrderByDescending(d => d.Key)) {
                if (die.Key == 1) {
                    continue;
                } else {
                    components.Add("" + die.Value + "d" + die.Key);
                }
            }

            if (!components.Any()) {
                // we get here when the only potential component is of die size 1
                // this means we're looking at only a flat amount, so just return it.
                return "" + dice[1];
            }

            string ret = string.Join(" + ", components);
            if (!dice.ContainsKey(1)) {
                return ret;
            }
            int modifier = dice[1];
            return modifier > 0
                ? ret + " + " + modifier
                : ret + " - " + (-modifier); // a lot of work to put a space before it, but it keeps things consistent
        }
    }
}
