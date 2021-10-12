using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.Global {
    class SheetActionReverser : ISaveableRoundObserver {
        private readonly bool useRounds = true;
        private readonly SheetAction action;
        private long roundsRemaining;
        private bool? currentCombatStatus = null;

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("type", "sheet action reverser"),
                new JProperty("rounds left", useRounds ? roundsRemaining : -1),
                new JProperty("action", action.ToJson())
            );
            if (currentCombatStatus != null) {
                json["combat"] = currentCombatStatus;
            }
            return json;
        }

        public SheetActionReverser(MainForm mainForm, JObject json) {
            long rounds = (long)json["rounds left"];
            if (rounds < 0) {
                useRounds = false;
            } else {
                roundsRemaining = rounds;
            }

            action = new SheetAction(mainForm, (JObject)json["action"]);

            if (json["combat"] != null) {
                currentCombatStatus = (bool)json["combat"];
            }
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

    class SheetAction : IJsonable {
        MainForm mainForm;

        string duration = null; // null for instantaneous, effect will not be reversed
        private readonly List<string> flagsToSet = new List<string>();
        private readonly List<string> flagsToClear = new List<string>();
        private readonly Dictionary<string, int> paramsToAdjust = new Dictionary<string, int>();
        List<SheetAction> actionsAfter = new List<SheetAction>(); // actions to fire at the end of the duration

        public JToken ToJson() {
            JObject json = new JObject();
            if (duration != null) {
                json["duration"] = duration;
            }
            json.AddList("set", flagsToSet);
            json.AddList("clear", flagsToClear);
            if (paramsToAdjust.Any()) {
                json["adjust"] = new JObject(paramsToAdjust.Select(x => new JProperty(x.Key, x.Value)));
            }
            json.AddList("after", actionsAfter);
            return json;
        }

        public SheetAction(MainForm mainForm, JObject json) {
            this.mainForm = mainForm;
            if (json["duration"] != null) {
                duration = (string)json["duration"];
            }
            flagsToSet.ReadJson("set", json);
            flagsToClear.ReadJson("clear", json);
            if (json["adjust"] != null) {
                paramsToAdjust = ((JObject)json["adjust"]).Properties()
                    .ToDictionary(x => x.Name, x => int.Parse((string)x.Value));
            }
            if (json["after"] != null) {
                actionsAfter.AddRange(json["after"].Select(x => new SheetAction(mainForm, (JObject)x)));
            }
        }

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
                mainForm.controller.rounds.RegisterAndSave(new SheetActionReverser(this, rounds));
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
