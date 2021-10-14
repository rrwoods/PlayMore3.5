using Newtonsoft.Json.Linq;
using PlayMore35.Effects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayMore35.Global {
    public class SheetAction : IJsonable {
        MainForm mainForm;

        string duration = null; // null for instantaneous, effect will not be reversed
        private readonly List<string> flagsToSet = new List<string>();
        private readonly List<string> flagsToClear = new List<string>();
        private readonly Dictionary<string, int> paramsToAdjust = new Dictionary<string, int>();
        private readonly List<int> unbindSlots = new List<int>();
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
            json.AddList("unbind", unbindSlots);
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
            unbindSlots.ReadJson("unbind", json);
            if (json["after"] != null) {
                actionsAfter.AddRange(json["after"].Select(x => new SheetAction(mainForm, (JObject)x)));
            }
        }

        private SheetAction Revert() {
            SheetAction revert = new SheetAction(mainForm);
            revert.flagsToSet.AddRange(flagsToClear);
            revert.flagsToClear.AddRange(flagsToSet);
            foreach (var adjustment in paramsToAdjust) {
                revert.paramsToAdjust[adjustment.Key] = -adjustment.Value;
            }
            revert.actionsAfter.AddRange(actionsAfter);
            return revert;
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

        public SheetAction UnbindSlot(int slot) {
            unbindSlots.Add(slot);
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
            if (PerformNoRefresh()) {
                mainForm.controller.Reload();
            }
            mainForm.RefreshEffects();
        }

        private bool PerformNoRefresh() {
            bool requireReload = false;

            var effects = mainForm.controller.Effects;
            var items = mainForm.controller.EquippedItems;
            var character = mainForm.controller.Character;
            flagsToSet.ForEach(x => effects.controllableFlags[x].On = true);
            flagsToClear.ForEach(x => effects.controllableFlags[x].On = false);
            foreach (var item in paramsToAdjust) {
                effects.controllableParameters[item.Key].Value += item.Value;
            }
            unbindSlots.ForEach(x => {
                mainForm.controller.SelectVestige(x, "", CharacterChoices.Character.PactState.GOOD_PACT);
                requireReload = true;
            });

            if (duration == null) {
                actionsAfter.ForEach(x => requireReload = x.PerformNoRefresh() || requireReload);
            } else {
                long rounds = new LengthOfTime(duration.FillStatValues(effects, items)).InRounds();
                mainForm.controller.Character.rounds.Schedule(Revert(), rounds);
            }

            return requireReload;
        }
    }
}
