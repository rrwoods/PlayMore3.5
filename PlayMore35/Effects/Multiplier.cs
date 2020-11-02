using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class Multiplier {
        public string Name = "";
        public decimal Factor { get; private set; }

        public Multiplier(decimal factor) {
            Factor = factor;
        }
    }
}
