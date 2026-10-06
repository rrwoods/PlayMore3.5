using System.Collections.Generic;
using PlayMore35.CharacterChoices;

namespace PlayMore35.Subsystems {
    public interface ISubsystemContentProvider {
        IEnumerable<Vestige> GetVestiges();
        Vestige GetVestige(string name);
    }
}
