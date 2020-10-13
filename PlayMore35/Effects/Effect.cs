using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class Effect {
        public Effect() {
            this.EffectParts = new List<EffectPart>();
        }

        public List<EffectPart> EffectParts { get; }

        public void AddPart(EffectPart part) {
            this.EffectParts.Add(part);
        }
    }
}
