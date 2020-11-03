using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Effects {
    public class StatisticClass : ISubjectClass<Statistic> {
        private readonly string Tag;

        public StatisticClass(string tag) {
            Tag = tag;
        }

        public bool Matches(Statistic thing) {
            return thing.HasTag(Tag);
        }
    }
}
