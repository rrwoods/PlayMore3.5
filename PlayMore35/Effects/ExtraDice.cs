using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class ExtraDice {
        public WeaponClass Affects { get; private set; }
        public (int qty, int size) Dice { get; private set; }

        public ExtraDice(WeaponClass affects, int qty, int size) {
            Dice = (qty, size);
            Affects = affects;
        }
    }
}
