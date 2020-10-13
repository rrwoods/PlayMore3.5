using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public interface IAmount {
        int Get(EffectPool pool);
    }
}
