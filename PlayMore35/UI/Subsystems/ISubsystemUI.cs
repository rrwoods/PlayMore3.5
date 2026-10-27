using System.Windows.Forms;
using PlayMore35.Global;

namespace PlayMore35.UI.Subsystems {
    public interface ISubsystemUI {
        string SubsystemId { get; }
        bool IsActive(ICharacterSession session);
        Control? CreateDedicatedTab(ISubsystemUIHost host);
        ISubsystemElementPicker CreateElementPicker(ICharacterSession session, ElementPickerContext context);
    }
}
