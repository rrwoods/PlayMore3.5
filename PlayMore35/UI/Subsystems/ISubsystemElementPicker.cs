using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems {
    public interface ISubsystemElementPicker : IDisposable {
        Control Control { get; }
        event Action<SubsystemElement?>? ElementDelivered;
        event Action<IReadOnlyList<SubsystemElement>>? ElementsDelivered;
    }
}
