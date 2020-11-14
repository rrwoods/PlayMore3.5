using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;

namespace PlayMore35.Global {
    public class Controller {
        private EffectPool effects;

        public Controller(JToken rules) {
            ReloadRules(rules);
        }

        // Completely toss out every effect we have, and refresh the rules of the game.  Any supplemental or house rules,
        // or character choices, or item effects are gone after calling this function.
        public void ReloadRules(JToken rules) {
            effects = new EffectPool();
            effects.ReadJson(rules);
        }

        /*
         * ... this is probably a really pointless-looking class right now.
         * 
         * This is going to be the coordinator of all the different kinds of information that come together to
         * figure out what the player needs in order to play the game.  Once we have item definition files, and
         * class features, and feats, this class will be responsible for turning a *choice* about one of those
         * things into the effects that thing represents in the game.
         */
    }
}
