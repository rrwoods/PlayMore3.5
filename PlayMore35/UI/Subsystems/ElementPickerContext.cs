using System;
using System.Collections.Generic;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems {
    public class ElementPickerContext {
        public string? ElementType { get; set; }
        public string? Filter { get; set; }
        public int AcquisitionLevel { get; set; }
        public bool MultiSelect { get; set; }
        public IReadOnlyList<SubsystemElement>? InitialSelections { get; set; }
        public Func<SubsystemElement, IReadOnlyList<string>>? WarningProvider { get; set; }
    }
}
