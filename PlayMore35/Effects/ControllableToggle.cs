using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class ControllableToggle : IToggle {
        public string Name { get; }
        public bool on = false;

        public ControllableToggle(string name) {
            Name = name;
        }

        public bool On(EffectPool pool) {
            return on;
        }
    }
}
