using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class FlagToggle : IToggle {
        private readonly string flag;

        public FlagToggle(string flag) {
            this.flag = flag;
        }

        public bool On(EffectPool pool) {
            return pool.HasFlag(flag);
        }
    }
}
