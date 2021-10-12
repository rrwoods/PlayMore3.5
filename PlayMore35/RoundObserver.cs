using Newtonsoft.Json.Linq;
using PlayMore35.Global;
using System;

namespace PlayMore35 {
    public interface IRoundObserver {
        // returns whether this observer should be removed; this is useful for observers whose job
        // it is to turn off a temporary effect at a later point.  if your observer is needed
        // forever, just always return false.
        bool SetState(DateTime time, long currentAbsoluteRound, long currentEncounterRound, long advanced, bool combat);
    }

    public static class RoundObserver {
        public static ISaveableRoundObserver FromJson(MainForm mainForm, JToken json) {
            string type = (string)json["type"];
            if (type == "sheet action reverser") {
                return new SheetActionReverser(mainForm, (JObject)json);
            }
            throw new NotImplementedException($"No round observer implementation corresponding to type '{type}'");
        }
    }

    public interface ISaveableRoundObserver : IRoundObserver, IJsonable { }
}
