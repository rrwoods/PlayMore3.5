using System;
using System.Linq;
using System.Windows.Forms;
using PlayMore35.Global;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems.PactMagic {
    public class PactMagicUI : ISubsystemUI {
        public string SubsystemId => "pact_magic";

        public bool IsActive(ICharacterSession session) {
            if (session == null) return false;

            var engine = session.Subsystems.Get("pact_magic") as SubsystemEngine;
            if (engine != null && engine.IsActive) return true;

            var capacityStat = session.GetStatistic("number of vestiges");
            if (capacityStat != null && capacityStat.Value > 0) return true;

            if (engine != null) {
                var slots = engine.GetSlots("vestiges");
                if (slots.Any(s => !string.IsNullOrEmpty(s.Element) && s.State != "empty")) {
                    return true;
                }
            }

            return false;
        }

        public Control? CreateDedicatedTab(ISubsystemUIHost host) {
            if (host == null) throw new ArgumentNullException(nameof(host));
            return new PactMagicTabPage(host);
        }

        public ISubsystemElementPicker CreateElementPicker(ICharacterSession session, ElementPickerContext context) {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new PactMagicElementPicker(session, context);
        }
    }
}
