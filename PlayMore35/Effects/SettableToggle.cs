using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class SettableToggle : IToggle {
        public bool Active = true;

        public bool On(EffectPool pool) {
            return Active;
        }
    }
}
