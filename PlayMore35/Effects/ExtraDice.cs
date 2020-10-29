using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class ExtraDice {
        public string Affects { get; private set; }
        public DiceGroup Dice { get; private set; }

        public ExtraDice(DiceGroup dice, string affects) {
            Dice = dice;
            Affects = affects;
        }


    }
}
