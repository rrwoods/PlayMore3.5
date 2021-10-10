using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using PlayMore35.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayMore35.Global {
    class SheetActionReverser : IRoundObserver, IJsonable {
        private readonly bool useRounds = true;
        private readonly SheetAction action;
        private long roundsRemaining;
        private bool? currentCombatStatus = null;

        public JToken ToJson() {
            throw new NotImplementedException();
        }

        // if rounds < 0, this reverser will run the next time combat status changes
        // if rounds > 0, this reverser will run after that many rounds
        // don't do rounds = 0
        public SheetActionReverser(SheetAction action, long rounds) {
            if (rounds < 0) {
                useRounds = false;
            } else {
                roundsRemaining = rounds;
            }
            this.action = action;
        }

        public bool SetState(DateTime time, long currentAbsoluteRound, long currentEncounterRound, long advanced, bool combat) {
            if (useRounds) {
                roundsRemaining -= advanced;
                if (roundsRemaining > 0) {
                    return false;
                }
                action.Reverse();
                return true;
            }

            // if currentCombatStatus is null, this is the first SetState call
            if (currentCombatStatus == null) {
                currentCombatStatus = combat;
                return false;
            }

            // otherwise, we are supposed to fire when combat status changes
            if (currentCombatStatus == combat) {
                return false;
            }

            action.Reverse();
            return true;
        }
    }

    class SheetAction {
        MainForm mainForm;

        string duration = null; // null for instantaneous, effect will not be reversed
        List<SheetAction> actionsAfter = new List<SheetAction>(); // actions to fire at the end of the duration

        private readonly List<string> flagsToSet = new List<string>();
        private readonly List<string> flagsToClear = new List<string>();
        private readonly Dictionary<string, int> paramsToAdjust = new Dictionary<string, int>();

        public SheetAction(MainForm mainForm) {
            this.mainForm = mainForm;
        }

        public SheetAction SetFlag(string flag) {
            flagsToSet.Add(flag);
            return this;
        }

        public SheetAction ClearFlag(string flag) {
            flagsToClear.Add(flag);
            return this;
        }

        public SheetAction AdjustParam(string param, int adjustment) {
            paramsToAdjust[param] = adjustment;
            return this;
        }

        public SheetAction Duration(string duration) {
            this.duration = duration;
            return this;
        }

        public SheetAction AndThen(SheetAction after) {
            actionsAfter.Add(after);
            return this;
        }

        public void Perform() {
            var effects = mainForm.controller.Effects;
            var items = mainForm.controller.EquippedItems;
            var character = mainForm.controller.Character;
            flagsToSet.ForEach(x => {
                effects.controllableFlags[x].On = true;
            });
            flagsToClear.ForEach(x => {
                effects.controllableFlags[x].On = false;
            });
            foreach(var item in paramsToAdjust) {
                effects.controllableParameters[item.Key].Value += item.Value;
            }
            mainForm.RefreshEffects();

            if (duration == null) {
                actionsAfter.ForEach(x => x.Perform());
            } else {
                long rounds = new LengthOfTime(duration.FillStatValues(effects, items)).InRounds();
                mainForm.controller.rounds.RegisterAndSave(new SheetActionReverser(this, rounds), mainForm.controller);
            }
        }

        public void Reverse() {
            var effects = mainForm.controller.Effects;
            var character = mainForm.controller.Character;
            flagsToSet.ForEach(x => {
                effects.controllableFlags[x].On = false;
            });
            flagsToClear.ForEach(x => {
                effects.controllableFlags[x].On = true;
            });
            foreach (var item in paramsToAdjust) {
                effects.controllableParameters[item.Key].Value -= item.Value;
            }
            mainForm.RefreshEffects();

            actionsAfter.ForEach(x => x.Perform());
        }
    }
}
