using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using PlayMore35.Character;
using PlayMore35.Effects;
using PlayMore35.Items;


namespace PlayMore35.Global {
    public class Controller {
        public CharacterChoices Choices;

        public EffectPool Effects;
        public Dictionary<string, Weapon> WeaponDefinitions;

        public Controller(CharacterChoices choices) {
            Choices = choices;
            Reload();
        }

        private JToken GetJsonResource(byte[] resource) {
            return JToken.Parse(Encoding.UTF8.GetString(resource));
        }

        // Completely toss out every effect we have, and refresh the rules of the game.
        public void Reload() {
            Effects = new EffectPool();

            Effects.ReadJson(GetJsonResource(Properties.Resources.rules));
            Effects.GenerateCharacterEffects(Choices);

            WeaponDefinitions = GetJsonResource(Properties.Resources.weapons)
                .Select(x => new Weapon((JObject)x))
                .ToDictionary(x => x.Name, x => x);
        }



        // This rebuilds each weapon on each call.  Only re-call this method when something
        // has changed about the available weapons (be it a property, or a definition, whatever)
        public List<Weapon> AvailableWeapons() {
            List<Weapon> available = new List<Weapon>();
            foreach (WeaponChoice choice in Choices.weapons) {
                Weapon w = new Weapon(WeaponDefinitions[choice.Name]);
                // TODO : weapon property definitions
                foreach (string property in choice.MeleeProperties) {
                    w.Melee?.atk.Properties.Add(new WeaponProperty(property));
                }
                foreach (string property in choice.SecondHeadProperties) {
                    w.SecondHead?.atk.Properties.Add(new WeaponProperty(property));
                }
                foreach (string property in choice.ThrownProperties) {
                    w.Thrown.Properties.Add(new WeaponProperty(property));
                }
                foreach (string property in choice.ProjectileProperties) {
                    w.Projectile?.atk.Properties.Add(new WeaponProperty(property));
                }
                available.Add(w);
            }
            return available;
        }
    }
}
