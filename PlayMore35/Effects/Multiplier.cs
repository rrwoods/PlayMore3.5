using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class Multiplier {
        public decimal Factor { get; private set; }
        public string Affects { get; private set; }

        public Multiplier(decimal factor, string affects) {
            Factor = factor;
            Affects = affects;
        }
    }
}
