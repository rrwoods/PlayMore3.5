using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public interface IToggle {
        bool On(EffectPool pool);
    }

    public sealed class AlwaysOn : IToggle {
        private AlwaysOn() { }
        static AlwaysOn() { }

        public bool On(EffectPool pool) {
            return true;
        }

        public static IToggle Instance { get; } = new AlwaysOn();
    }
}
