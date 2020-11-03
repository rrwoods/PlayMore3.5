using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    class LookupAmount : IAmount {
        private readonly IAmount Basis;
        private readonly Dictionary<int, int> Lookup;

        public LookupAmount(string basis, Dictionary<int, int> lookup) {
            Basis = new VariableAmount(basis);
            Lookup = lookup;
        }

        public int Get(EffectPool pool) {
            return Lookup[Basis.Get(pool)];
        }
    }
}
