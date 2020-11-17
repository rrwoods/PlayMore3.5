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
        public CharacterChoices.Character Choices;

        public EffectPool Effects;
        public Dictionary<string, Weapon> WeaponDefinitions;
        public Dictionary<string, Armor> ArmorDefinitions;
        public Dictionary<string, Shield> ShieldDefinitions;
        public Dictionary<string, WeaponProperty> WeaponPropertyDefinitions;
        public Dictionary<string, ArmorProperty> ArmorPropertyDefinitions;

        public Controller(CharacterChoices.Character choices) {
            Choices = choices;
            Reload();
        }

        private JToken GetJsonResource(byte[] resource) {
            return JToken.Parse(Encoding.UTF8.GetString(resource));
        }

        // Completely toss out every effect and item definition we have, and refresh the rules of the game.
        public void Reload() {
            Effects = new EffectPool();
            Effects.ReadJson(GetJsonResource(Properties.Resources.rules));
            Effects.GenerateCharacterEffects(Choices);

            WeaponDefinitions = GetJsonResource(Properties.Resources.weapons)
                .Select(x => new Weapon((JObject)x))
                .ToDictionary(x => x.Name, x => x);

            ArmorDefinitions = GetJsonResource(Properties.Resources.armor)
                .Select(x => new Armor((JObject)x))
                .ToDictionary(x => x.Name, x => x);

            ShieldDefinitions = GetJsonResource(Properties.Resources.shields)
                .Select(x => new Shield((JObject)x))
                .ToDictionary(x => x.Name, x => x);
            List<Shield> spiked = ShieldDefinitions.Values
                .Where(x => x.ShieldBashEffort != null)
                .Select(x => new Shield(x) {
                    Name = "spiked " + x.Name
                })
                .ToList(); // Finalize the iteration before modifying the dictionary it's based on
            spiked.ForEach(x => ShieldDefinitions[x.Name] = x);
            foreach (Shield shield in ShieldDefinitions.Values.Where(x => x.ShieldBashEffort != null)) {
                shield.ShieldBash = new AttackMode(WeaponDefinitions[shield.Name + " bash"].Melee?.atk);
            }

            WeaponPropertyDefinitions = GetJsonResource(Properties.Resources.weaponProperties)
                .Select(x => new WeaponProperty((JObject)x))
                .ToDictionary(x => x.Name, x => x);

            ArmorPropertyDefinitions = GetJsonResource(Properties.Resources.armorProperties)
                .Select(x => new ArmorProperty((JObject)x))
                .ToDictionary(x => x.Name, x => x);
        }

        // This rebuilds each weapon on each call.  Only re-call this method when something
        // has changed about the available weapons (be it a property, or a definition, whatever)
        public List<Weapon> AvailableWeapons() {
            List<Weapon> available = new List<Weapon>();
            foreach (WeaponChoice choice in Choices.Weapons) {
                Weapon weapon = new Weapon(WeaponDefinitions[choice.Name]) {
                    SizeCategory = choice.SizeCategory
                };
                // TODO : weapon property definitions
                foreach (string property in choice.MeleeProperties) {
                    weapon.Melee?.atk.Properties.Add(WeaponPropertyDefinitions[property]);
                }
                foreach (string property in choice.SecondHeadProperties) {
                    weapon.SecondHead?.atk.Properties.Add(WeaponPropertyDefinitions[property]);
                }
                foreach (string property in choice.ThrownProperties) {
                    weapon.Thrown.Properties.Add(WeaponPropertyDefinitions[property]);
                }
                foreach (string property in choice.ProjectileProperties) {
                    weapon.Projectile?.atk.Properties.Add(WeaponPropertyDefinitions[property]);
                }
                available.Add(weapon);
            }
            return available;
        }

        public List<Armor> AvailableArmor() {
            List<Armor> available = new List<Armor>();
            foreach (ArmorChoice choice in Choices.Armor) {
                Armor armor = new Armor(ArmorDefinitions[choice.Name]) {
                    SizeCategory = choice.SizeCategory
                };
                // TODO : armor property definitions
                foreach (string property in choice.ArmorProperties) {
                    armor.Properties.Add(ArmorPropertyDefinitions[property]);
                }
                if (choice.Spiked) {
                    armor.ArmorSpikes = new AttackMode(WeaponDefinitions["armor spikes"].Melee?.atk);
                    foreach (string property in choice.MeleeProperties) {
                        armor.ArmorSpikes.Properties.Add(WeaponPropertyDefinitions[property]);
                    }
                }
                available.Add(armor);
            }
            return available;
        }

        public List<Shield> AvailableShields() {
            List<Shield> available = new List<Shield>();
            foreach (ShieldChoice choice in Choices.Shields) {
                Shield shield = new Shield(ShieldDefinitions[choice.Name]) {
                    SizeCategory = choice.SizeCategory
                };
                foreach (string property in choice.ArmorProperties) {
                    shield.Properties.Add(ArmorPropertyDefinitions[property]);
                }
                foreach (string property in choice.MeleeProperties) {
                    shield.ShieldBash.Properties.Add(WeaponPropertyDefinitions[property]);
                }
                available.Add(shield);
            }
            return available;
        }
    }
}
