using System;
using System.Collections.Generic;

namespace PlayMore35.Global {
    public enum TimelineRefreshRequirement {
        None,
        RecomputeDerived,
        FullReload
    }

    public class TimelineAdvanceResult {
        public long RoundsAdvanced { get; set; }
        public DateTime PreviousTime { get; set; }
        public DateTime CurrentTime { get; set; }
        public TimelineRefreshRequirement RefreshRequirement { get; set; } = TimelineRefreshRequirement.None;
        public List<SheetAction> ExpiredActions { get; } = new List<SheetAction>();
    }
}
