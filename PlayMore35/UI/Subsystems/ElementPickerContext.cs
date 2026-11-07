using System;
using System.Collections.Generic;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems {
    public class ElementPickerContext {
        public string? ElementType { get; set; }
        public string? Filter { get; set; }
        public int AcquisitionLevel { get; set; }
        public Func<SubsystemElement, IReadOnlyList<ValidationWarning>> WarningProvider { get; set; } = _ => Array.Empty<ValidationWarning>();
    }
}
