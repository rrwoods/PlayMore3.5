using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class Effect {
        public Effect(string name) {
            this.Name = name;
            this.Modifiers = new List<Modifier>();
        }

        public string Name { get; private set; }

        public List<Modifier> Modifiers { get; }

        public Effect AddModifier(Modifier mod) {
            this.Modifiers.Add(mod);
            return this; // method chaining
        }
    }
}
