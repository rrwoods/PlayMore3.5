using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class DiceGroup {
        // {size => quantity}, except that die size of 1 represents a flat amount
        private Dictionary<int, int> dice = new Dictionary<int, int>();

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

        public void AddDice(int quantity, int size) {
            if (quantity == 0) {
                return;
            }

            if (dice.ContainsKey(size)) {
                dice[size] += quantity;
            } else {
                dice[size] = quantity;
            }
        }

        public override string ToString() {
            List<string> components = new List<string>();
            foreach(var die in dice.OrderByDescending(d => d.Key)) {
                if (die.Key == 1) {
                    components.Add("" + die.Value);
                } else {
                    components.Add("" + die.Value + "d" + die.Key);
                }
            }
            if (components.Any()) {
                return string.Join(" + ", components);
            }
            return "—";
        }
    }
}
