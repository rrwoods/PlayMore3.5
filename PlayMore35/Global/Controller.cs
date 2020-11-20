using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;


namespace PlayMore35.Global {
    public class Controller {
        // Rules of the game, plus effects inherent to the character.
        public EffectPool Effects;

        public Character Character;

        public Equipped EquippedItems;

        public Dictionary<string, Weapon> WeaponDefinitions;
        public Dictionary<string, Armor> ArmorDefinitions;
        public Dictionary<string, Shield> ShieldDefinitions;
        public Dictionary<string, WeaponProperty> WeaponPropertyDefinitions;
        public Dictionary<string, ArmorProperty> ArmorPropertyDefinitions;
        public Dictionary<string, Item> ItemDefinitions;

        // **** INITIALIZATION AND RELOAD FUNCTIONS ****

        public Controller(Character character) {
            Character = character;
            Reload();
        }

        private static JToken GetJsonResource(byte[] resource) {
            return JToken.Parse(Encoding.UTF8.GetString(resource));
        }

        // The SRD is surprisingly unhelpful when it comes to computing the weights of non-medium
        // weapons.  We're using the same rules as armor, since the two values it does give are
        // consistent with that.  For other items, I'm assuming that a version that fits a differently
        // sized creature weighs the same as a medium one.
        private static readonly Dictionary<int, decimal> weightFactors = new Dictionary<int, decimal> {
            [-4] = 0.1m, [-3] = 0.1m, [-2] = 0.1m, [-1] = 0.5m, [0] = 1, [1] = 2, [2] = 5, [3] = 8, [4] = 12
        };
        
        private Weapon WeaponFromChoice(WeaponChoice choice) {
            Weapon weapon = new Weapon(WeaponDefinitions[choice.Name]) {
                SizeCategory = choice.SizeCategory,
            };
            weapon.Weight *= weightFactors[weapon.SizeCategory];

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
            return weapon;
        }

        private Armor ArmorFromChoice(ArmorChoice choice) {
            Armor armor = new Armor(ArmorDefinitions[choice.Name]) {
                SizeCategory = choice.SizeCategory
            };
            armor.Weight *= weightFactors[armor.SizeCategory];

            foreach (string property in choice.ArmorProperties) {
                armor.Properties.Add(ArmorPropertyDefinitions[property]);
            }
            if (choice.Spiked) {
                armor.ArmorSpikes = new AttackMode(WeaponDefinitions["armor spikes"].Melee?.atk);
                foreach (string property in choice.MeleeProperties) {
                    armor.ArmorSpikes.Properties.Add(WeaponPropertyDefinitions[property]);
                }
            }
            return armor;
        }

        private Shield ShieldFromChoice(ShieldChoice choice) {
            Shield shield = new Shield(ShieldDefinitions[choice.Name]) {
                SizeCategory = choice.SizeCategory
            };
            shield.Weight *= weightFactors[shield.SizeCategory];

            foreach (string property in choice.ArmorProperties) {
                shield.Properties.Add(ArmorPropertyDefinitions[property]);
            }
            foreach (string property in choice.MeleeProperties) {
                shield.ShieldBash.Properties.Add(WeaponPropertyDefinitions[property]);
            }
            return shield;
        }

        private decimal WeightAtLocation(InventoryLocation location) {
            return
                location.weapons.Select(x => WeaponFromChoice(x).Weight).Sum() +
                location.armor.Select(x => ArmorFromChoice(x).Weight).Sum() +
                location.shields.Select(x => ShieldFromChoice(x).Weight).Sum() +
                location.items.Select(x => ItemDefinitions[x].Weight).Sum();
        }

        // Completely toss out every effect and item definition we have, and refresh the rules of the game.
        // Also, re-determine the character's equipped items.
        public void Reload() {
            Effects = new EffectPool();
            Effects.ReadJson(GetJsonResource(Properties.Resources.rules));
            Effects.GenerateCharacterEffects(Character);

            WeaponDefinitions = GetJsonResource(Properties.Resources.weapons)
                .Select(x => new Weapon((JObject)x))
                .ToDictionary(x => x.Name);

            ArmorDefinitions = GetJsonResource(Properties.Resources.armor)
                .Select(x => new Armor((JObject)x))
                .ToDictionary(x => x.Name);

            ShieldDefinitions = GetJsonResource(Properties.Resources.shields)
                .Select(x => new Shield((JObject)x))
                .ToDictionary(x => x.Name);
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
                .ToDictionary(x => x.Name);
            ArmorPropertyDefinitions = GetJsonResource(Properties.Resources.armorProperties)
                .Select(x => new ArmorProperty((JObject)x))
                .ToDictionary(x => x.Name);
            ItemDefinitions = GetJsonResource(Properties.Resources.items)
                .Select(x => new Item((JObject)x))
                .ToDictionary(x => x.ItemEffect.Name);

            EquippedItems = new Equipped();

            if (Character.equipped.TwoHandWeapon != null) {
                Weapon weapon = WeaponFromChoice(Character.equipped.TwoHandWeapon);
                EquippedItems.PrimaryHandWeapon = weapon;
                EquippedItems.OffHandWeapon = weapon;
            } else {
                if (Character.equipped.PrimaryHandWeapon != null) {
                    EquippedItems.PrimaryHandWeapon = WeaponFromChoice(Character.equipped.PrimaryHandWeapon);
                }
                if (Character.equipped.OffHandWeapon != null) {
                    EquippedItems.OffHandWeapon = WeaponFromChoice(Character.equipped.OffHandWeapon);
                }
            }
            if (Character.equipped.DonnedArmor != null) {
                EquippedItems.DonnedArmor = ArmorFromChoice(Character.equipped.DonnedArmor);
            }
            if (Character.equipped.OffHandShield != null) {
                EquippedItems.OffHandShield = ShieldFromChoice(Character.equipped.OffHandShield);
            }

            foreach (var item in Character.equipped.SlottedItems) {
                EquippedItems.SlottedItems[item.Key] = ItemDefinitions[item.Value];
            }
            foreach (string item in Character.equipped.SlotlessItems) {
                EquippedItems.SlotlessItems.Add(ItemDefinitions[item]);
            }

            EquippedItems.UnequippedWeight = Character.inventory.Values
                .Where(x => x.weighs)
                .Select(x => WeightAtLocation(x))
                .Sum();
        }

        // **** MUTATORS ****

        // still to come:  functions that move items around on the character,
        // accounting for how that affects weight and such.

        // **** ACCESSORS ****

        public (int value, List<string> explanation) GetStatistic(string stat) {
            return Effects.GetStat(stat).GetValue(Effects, EquippedItems);
        }
        
        public List<Attack> GetAttacks() {
            // calculate size change -- difference between the current value of the size category
            // and that of the player's race
            return Attack.Generate(Effects, 0, EquippedItems);
        }
    }
}
