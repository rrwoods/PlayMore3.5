using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems {
    public interface ISubsystemState : IJsonable {
        string SubsystemId { get; }
    }
}
