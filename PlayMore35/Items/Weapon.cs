using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static PlayMore35.GameRules.Constants;

namespace PlayMore35.Items {
    public class Weapon {
        public string Name = "Dagger";

        public List<Attack> Attacks = new List<Attack>();

        public List<string> Tags = new List<string>();
    }
}
