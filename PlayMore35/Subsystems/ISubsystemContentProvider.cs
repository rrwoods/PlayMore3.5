using System.Collections.Generic;
using PlayMore35.CharacterChoices;
using PlayMore35.Subsystems.Maneuvers;

namespace PlayMore35.Subsystems {
    public interface ISubsystemContentProvider {
        IEnumerable<Vestige> GetVestiges();
        Vestige GetVestige(string name);
        IEnumerable<Maneuver> GetManeuvers();
        Maneuver GetManeuver(string name);
    }
}
