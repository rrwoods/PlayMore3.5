using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class Effect {
        public Effect(string name) {
            this.Name = name;
            this.EffectParts = new List<EffectPart>();
        }

        public string Name { get; private set; }

        public List<EffectPart> EffectParts { get; }

        public Effect AddPart(EffectPart part) {
            this.EffectParts.Add(part);
            return this; // method chaining
        }

    }
}
