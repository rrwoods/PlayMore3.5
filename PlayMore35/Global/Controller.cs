using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;

using static PlayMore35.Effects.Constants;

namespace PlayMore35.Global {
    public class Controller {
        private string saveDir;

        // Rules of the game, plus effects inherent to the character.
        public EffectPool Effects;

        public Character Character;
        private int NormalSizeCategory;
        public Dictionary<string, Race> RaceDefinitions;
        public Dictionary<string, CharacterClass> ClassDefinitions;
        public Dictionary<string, Feat> FeatDefinitions;

        public Equipped EquippedItems;

        public Dictionary<string, Weapon> WeaponDefinitions;
        public Dictionary<string, Armor> ArmorDefinitions;
        public Dictionary<string, Shield> ShieldDefinitions;
        public Dictionary<string, WeaponProperty> WeaponPropertyDefinitions;
        public Dictionary<string, ArmorProperty> ArmorPropertyDefinitions;
        public Dictionary<string, Item> ItemDefinitions;

        // **** CONVENIENCE METHODS THAT ALL UIS WILL PROBABLY WANT ****


        private static readonly Dictionary<string, string> customShortNames = new Dictionary<string, string> {
            ["Control Shape"] = "CS",
            ["Decipher Script"] = "DS",
            ["Disable Device"] = "DD",
            ["Gather Information"] = "Info",
            ["Knowledge (arcana)"] = "K:arc",
            ["Knowledge (architecture and engineering)"] = "K:eng",
            ["Knowledge (dungeoneering)"] = "K:dun",
            ["Knowledge (geography)"] = "K:geo",
            ["Knowledge (history)"] = "K:his",
            ["Knowledge (local)"] = "K:loc",
            ["Knowledge (nature)"] = "K:nat",
            ["Knowledge (nobility and royalty)"] = "K:nob",
            ["Knowledge (psionics)"] = "K:psi",
            ["Knowledge (religion)"] = "K:rel",
            ["Knowledge (the planes)"] = "K:pla",
            ["Martial Lore"] = "ML",
            ["Move Silently"] = "MS",
            ["Open Lock"] = "Open",
            ["Sleight Of Hand"] = "SOH",
            ["Speak Language"] = "Lang",
            ["Use Magic Device"] = "UMD",
            ["Use Psionic Device"] = "UPD",
            ["Use Rope"] = "Rope"
        };
        public static string ShortName(string skill) {
            if (customShortNames.ContainsKey(skill)) {
                return customShortNames[skill];
            }
            if (skill.Count() == 4) {
                return skill;
            }
            return skill.Substring(0, 3);
        }

        // **** INITIALIZATION AND RELOAD FUNCTIONS ****

        public Controller(Character character) {
            Character = character;
            Reload();
        }

        private JToken GetResource(byte[] resource, string altFilename) {
            string json;
            string savePath = $@"{saveDir}\{altFilename}";
            if (File.Exists(savePath)) {
                json = File.ReadAllText(savePath);
            } else {
                json = Encoding.UTF8.GetString(resource);
                File.WriteAllText(savePath, json);
            }
            return JToken.Parse(json);
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
            // TODO : make an AddSpikes method on armor that takes care of this
            // and get rid of the armor spikes weapon def
            if (choice.Spiked) {
                armor.ArmorSpikes = new AttackMode(WeaponDefinitions["armor spikes"].Melee?.atk);
                foreach (string property in choice.MeleeProperties) {
                    armor.ArmorSpikes.Properties.Add(WeaponPropertyDefinitions[property]);
                }
            }
            return armor;
        }

        private Shield ShieldFromChoice(ArmorChoice choice) {
            Shield shield = new Shield(ShieldDefinitions[choice.Name]) {
                SizeCategory = choice.SizeCategory
            };
            shield.Weight *= weightFactors[shield.SizeCategory];

            foreach (string property in choice.ArmorProperties) {
                shield.Properties.Add(ArmorPropertyDefinitions[property]);
            }
            shield.Spiked = choice.Spiked;
            foreach (string property in choice.MeleeProperties) {
                shield.ShieldBash.Properties.Add(WeaponPropertyDefinitions[property]);
            }
            return shield;
        }

        public decimal WeightAtLocation(string location) {
            return WeightAtLocation(Character.inventory[location]);
        }

        private decimal WeightAtLocation(InventoryLocation location) {
            return
                location.weapons.Select(x => WeaponFromChoice(x).Weight).Sum() +
                location.armor.Select(x => ArmorFromChoice(x).Weight).Sum() +
                location.shields.Select(x => ShieldFromChoice(x).Weight).Sum() +
                location.items.Select(x => ItemDefinitions[x.Key].Weight * x.Value).Sum();
        }

        // Completely toss out every effect and item definition we have, and refresh the rules of the game.
        // Also, re-determine the character's equipped items.
        public void Reload() {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveDir = $@"{documentsPath}\PlayMore3.5";
            if (!Directory.Exists(saveDir)) {
                DirectoryInfo saveDirectory = Directory.CreateDirectory(saveDir);
            }

            Effects = new EffectPool();
            Effects.ReadJson(GetResource(Properties.Resources.rules, "rules.json"));
            // TODO : we eventually need a way for someone to add a rule and save it off, without the effects
            // we're about to add that are specific to their character making it out to the houserules file.

            Effects.GenerateCharacterEffects(Character);

            RaceDefinitions = GetResource(Properties.Resources.races, "races.json")
                .Select(x => new Race((JObject)x))
                .ToDictionary(x => x.Name);
            RaceDefinitions[""] = new Race(); // For completely new characters.
            ClassDefinitions = GetResource(Properties.Resources.classes, "classes.json")
                .Select(x => new CharacterClass((JObject)x))
                .ToDictionary(x => x.Name);
            FeatDefinitions = GetResource(Properties.Resources.feats, "feats.json")
                .Select(x => new Feat((JObject)x))
                .ToDictionary(x => x.Benefit.Name);

            WeaponDefinitions = GetResource(Properties.Resources.weapons, "weapons.json")
                .Select(x => new Weapon((JObject)x))
                .ToDictionary(x => x.Name);
            ArmorDefinitions = GetResource(Properties.Resources.armor, "armor.json")
                .Select(x => new Armor((JObject)x))
                .ToDictionary(x => x.Name);
            ShieldDefinitions = GetResource(Properties.Resources.shields, "shields.json")
                .Select(x => new Shield((JObject)x))
                .ToDictionary(x => x.Name);

            WeaponPropertyDefinitions = GetResource(Properties.Resources.weaponProperties, "weaponProperties.json")
                .Select(x => new WeaponProperty((JObject)x))
                .ToDictionary(x => x.Name);
            ArmorPropertyDefinitions = GetResource(Properties.Resources.armorProperties, "armorProperties.json")
                .Select(x => new ArmorProperty((JObject)x))
                .ToDictionary(x => x.Name);
            ItemDefinitions = GetResource(Properties.Resources.items, "items.json")
                .Select(x => new Item((JObject)x))
                .ToDictionary(x => x.ItemEffect.Name);
            Effects.AddStats(ItemDefinitions.Values.SelectMany(x => x.NewStats));

            Race race = RaceDefinitions[Character.race];
            Effects.GenerateRaceEffects(race);
            NormalSizeCategory = race.SizeCategory;

            var classLevels = Character.SummarizeClassLevels();
            foreach (var classAndLevel in classLevels) {
                CharacterClass @class = ClassDefinitions[classAndLevel.Key];
                Effects.GenerateClassEffects(@class, classAndLevel.Value);
                for (int level = 0; level < classAndLevel.Value && level < @class.ClassFeatures.Count; level++) {
                    foreach (Effect feature in @class.ClassFeatures[level]) {
                        Effects.AddEffect(feature);
                    }
                }
            }
            foreach (string featName in Character.feats.Values) {
                Effects.GenerateFeatEffects(FeatDefinitions[featName]);
            }

            EquippedItems = new Equipped();

            if (Character.equipped.PrimaryHandWeapon != null) {
                EquippedItems.PrimaryHandWeapon = WeaponFromChoice(Character.equipped.PrimaryHandWeapon);
            }
            if (Character.equipped.OffHandWeapon != null) {
                EquippedItems.OffHandWeapon = WeaponFromChoice(Character.equipped.OffHandWeapon);
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

            ComputeWeight();
        }

        public void ComputeWeight() {
            EquippedItems.UnequippedWeight = Character.inventory.Values
                .Where(x => x.weighs)
                .Select(x => WeightAtLocation(x))
                .Sum();
        }

        public Equipped.Slot SlotForItem(string itemName) {
            return ItemDefinitions[itemName].ItemSlot;
        }

        // **** MUTATORS ****

        public void CountWeight(string locationName, bool weighs) {
            Character.inventory[locationName].weighs = weighs;
            ComputeWeight();
        }

        // remove methods do NOT move them into inventory.

        public WeaponChoice RemoveEquippedWeapon(bool primary) {
            WeaponChoice removed = primary ? Character.equipped.PrimaryHandWeapon : Character.equipped.OffHandWeapon;
            if (primary) {
                Character.equipped.PrimaryHandWeapon = null;
                EquippedItems.PrimaryHandWeapon = null;
            } else {
                Character.equipped.OffHandWeapon = null;
                EquippedItems.OffHandWeapon = null;
            }

            ComputeWeight();
            return removed;
        }

        public ArmorChoice RemoveDonnedArmor() {
            ArmorChoice removed = Character.equipped.DonnedArmor;
            Character.equipped.DonnedArmor = null;
            EquippedItems.DonnedArmor = null;

            ComputeWeight();
            return removed;
        }

        public ArmorChoice RemoveEquippedShield() {
            ArmorChoice removed = Character.equipped.OffHandShield;
            Character.equipped.OffHandShield = null;
            EquippedItems.OffHandShield = null;

            ComputeWeight();
            return removed;
        }

        // returns the quantity actually removed
        public int RemoveItem(string from, string itemName, int quantity) {
            if (!Character.inventory[from].items.ContainsKey(itemName)) {
                return 0;
            }

            int maxQuantity = Character.inventory[from].items[itemName];
            if (quantity >= maxQuantity) {
                quantity = maxQuantity;
                Character.inventory[from].items.Remove(itemName);
            } else {
                Character.inventory[from].items[itemName] -= quantity;
            }

            ComputeWeight();
            return quantity;
        }

        // unequip methods move the item into the specified inventory location.

        public void UnequipWeapon(string to, bool primary) {
            if (!Character.inventory.ContainsKey(to)) {
                return;
            }

            WeaponChoice unequipped = RemoveEquippedWeapon(primary);
            if (unequipped != null) {
                Character.inventory[to].weapons.Add(unequipped);
            }

            ComputeWeight();
        }

        public void UndonArmor(string to) {
            if (!Character.inventory.ContainsKey(to)) {
                return;
            }

            ArmorChoice removed = RemoveDonnedArmor();
            if (removed != null) {
                Character.inventory[to].armor.Add(removed);
            }

            ComputeWeight();
        }

        public void UnequipShield(string to) {
            if (!Character.inventory.ContainsKey(to)) {
                return;
            }

            ArmorChoice removed = RemoveEquippedShield();
            if (removed != null) {
                Character.inventory[to].shields.Add(removed);
            }

            ComputeWeight();
        }

        public void UnequipItem(Equipped.Slot from, string to) {
            Dictionary<Equipped.Slot, string> items = Character.equipped.SlottedItems;
            if (!items.ContainsKey(from)) {
                return;
            }

            string removed = items[from];
            items.Remove(from);
            AddItem(to, removed, 1);

            ComputeWeight();
        }

        public void UnequipSlotlessItem(string to, string item) {
            if (!Character.equipped.SlotlessItems.Contains(item)) {
                return;
            }
            Character.equipped.SlotlessItems.Remove(item);
            Item toRemove = EquippedItems.SlotlessItems.Where(x => x.ItemEffect.Name.Equals(item)).First();
            EquippedItems.SlotlessItems.Remove(toRemove);
            AddItem(to, item, 1);

            ComputeWeight();
        }

        public void EquipWeapon(string from, bool primary, WeaponChoice weaponChoice) {
            if (!Character.inventory.ContainsKey(from) || !Character.inventory[from].weapons.Contains(weaponChoice)) {
                return;
            }

            Character.inventory[from].weapons.Remove(weaponChoice);
            Weapon weapon = WeaponFromChoice(weaponChoice);
            UnequipWeapon(from, primary);
            if (primary) {
                Character.equipped.PrimaryHandWeapon = weaponChoice;
                EquippedItems.PrimaryHandWeapon = weapon;
            } else {
                Character.equipped.OffHandWeapon = weaponChoice;
                EquippedItems.OffHandWeapon = weapon;
                if (EquippedItems.OffHandShield.AttackRollPenalty == null) {
                    UnequipShield(from);
                }
            }

            ComputeWeight();
        }

        public void DonArmor(string from, ArmorChoice armorChoice) {
            if (!Character.inventory.ContainsKey(from) || !Character.inventory[from].armor.Contains(armorChoice)) {
                return;
            }

            Character.inventory[from].armor.Remove(armorChoice);
            Armor armor = ArmorFromChoice(armorChoice);
            UndonArmor(from);
            Character.equipped.DonnedArmor = armorChoice;
            EquippedItems.DonnedArmor = armor;

            ComputeWeight();
        }

        public void EquipShield(string from, ArmorChoice shieldChoice) {
            if (!Character.inventory.ContainsKey(from) || !Character.inventory[from].shields.Contains(shieldChoice)) {
                return;
            }

            Character.inventory[from].shields.Remove(shieldChoice);
            Shield shield = ShieldFromChoice(shieldChoice);

            Character.equipped.OffHandShield = shieldChoice;
            EquippedItems.OffHandShield = shield;
            if (shield.AttackRollPenalty == null) {
                UnequipWeapon(from, false);
            }

            ComputeWeight();
        }

        public void EquipItem(string from, Equipped.Slot to, string itemName) {
            if (!Character.inventory.ContainsKey(from)) {
                return;
            }

            Item item = ItemDefinitions[itemName];
            if (item.ItemSlot != to) {
                return;
            }

            if (to == Equipped.Slot.SLOTLESS) {
                EquippedItems.SlotlessItems.Add(ItemDefinitions[itemName]);
                Character.equipped.SlotlessItems.Add(itemName);
                RemoveItem(from, itemName, 1);
            } else {
                string previouslyEquipped = EquippedItems.SlottedItems.ContainsKey(to)
                    ? EquippedItems.SlottedItems[to].ItemEffect.Name
                    : null;
                EquippedItems.SlottedItems[to] = item;
                Character.equipped.SlottedItems[to] = itemName;
                RemoveItem(from, itemName, 1);

                if (previouslyEquipped != null) {
                    if (Character.inventory[from].items.ContainsKey(previouslyEquipped)) {
                        Character.inventory[from].items[previouslyEquipped] += 1;
                    } else {
                        Character.inventory[from].items[previouslyEquipped] = 1;
                    }
                }
            }

            ComputeWeight();
        }

        public void SwapWeapons() {
            WeaponChoice swapChoice = Character.equipped.PrimaryHandWeapon;
            Character.equipped.PrimaryHandWeapon = Character.equipped.OffHandWeapon;
            Character.equipped.OffHandWeapon = swapChoice;

            Weapon swap = EquippedItems.PrimaryHandWeapon;
            EquippedItems.PrimaryHandWeapon = EquippedItems.OffHandWeapon;
            EquippedItems.OffHandWeapon = swap;
        }

        public void MoveItem(string from, string to, object item, int quantity) {
            if (
                !Character.inventory.ContainsKey(from) ||
                !Character.inventory.ContainsKey(to)
            ) {
                return;
            }

            if (item is string itemName) {
                if (Character.inventory[from].items.ContainsKey(itemName)) {
                    quantity = RemoveItem(from, itemName, quantity);
                    AddItem(to, itemName, quantity);
                }
            } else if (item is WeaponChoice weapon) {
                if (Character.inventory[from].weapons.Remove(weapon)) {
                    Character.inventory[to].weapons.Add(weapon);
                }
            } else if (item is ArmorChoice armor) {
                if (armor.IsShield) {
                    if (Character.inventory[from].shields.Remove(armor)) {
                        Character.inventory[to].shields.Add(armor);
                    }
                } else if (Character.inventory[from].armor.Remove(armor)) {
                    Character.inventory[to].armor.Add(armor);
                }
            }

            ComputeWeight();
        }

        public void AddItem(string to, string itemName, int quantity) {
            if (Character.inventory[to].items.ContainsKey(itemName)) {
                Character.inventory[to].items[itemName] += quantity;
            } else {
                Character.inventory[to].items[itemName] = quantity;
            }
        }

        // **** ACCESSORS ****

        private void AddFeatSlot(SortedDictionary<int, List<string>> feats, int tiebreak, KeyValuePair<string, int> featSlot) {
            const int SEGMENT_SIZE = 4; // one more than the biggest constant in GetFeatSlots
            int k = featSlot.Value * SEGMENT_SIZE + tiebreak;
            if (feats.ContainsKey(k)) {
                feats[k].Add(featSlot.Key);
            } else {
                feats[k] = new List<string> { featSlot.Key };
            }
        }

        public List<string> GetFeatSlots() {
            // This is a cheeky way of ensuring a consistent ordering while maintaining sanity while iterating.
            // That ordering is: Lower level first; within a level, always race - rules - class - feats.
            // (We don't yet have feats that grant feats, but those will come last.  Vow of Poverty is complicated.)

            int level = Character.classLevels.Count;

            const int RACE = 0;
            const int RULES = 1;
            const int CLASS = 2;
            //const int FEATS = 3;

            SortedDictionary<int, List<string>> feats = new SortedDictionary<int, List<string>>();
            foreach (var feat in RaceDefinitions[Character.race].BonusFeats) {
                if (feat.Value <= level) {
                    AddFeatSlot(feats, RACE, feat);
                }
            }
            foreach (var feat in Effects.featSlots) {
                if (feat.Value <= level) {
                    AddFeatSlot(feats, RULES, feat);
                }
            }
            foreach (var classAndLevel in Character.SummarizeClassLevels()) {
                foreach (var feat in ClassDefinitions[classAndLevel.Key].BonusFeats) {
                    if (feat.Value <= classAndLevel.Value) {
                        AddFeatSlot(feats, CLASS, feat);
                    }
                }
            }

            return feats.Values.SelectMany(x => x).ToList();
        }

        public int GetStartingIntelligence() {
            return (Character.assignedAbilityScores[INT] + RaceDefinitions[Character.race].Int) / 2 - 5;
        }

        // "Naked" HP from hit dice including CON modifier, and no other source.
        public int BaseHp() {
            int conScore =
                Character.assignedAbilityScores[CON] +
                RaceDefinitions[Character.race].Con +
                Character.abilityScoreIncrements.Values.Where(x => x == "CON").Count();
            int conMod = (conScore / 2) - 5;
            int hp = 0;
            foreach (int roll in Character.rolledHitDice) {
                hp += Math.Max(1, roll + conMod);
            }
            return hp;
        }

        public bool IsClassSkill(string skill, int levelIndex) {
            if (ClassDefinitions[Character.classLevels[levelIndex]].ClassSkills.Contains(skill)) {
                return true;
            }

            // this might get more complicated once we consider things like racial class skills
            return false;
        }

        // If the return value is positive, you can spend that many more skill points.
        // If it's negative, you have spent that many points more than you have.
        // If it's zero, you have spend the correct number of skill points.
        public int SkillPointsRemainingAtLevel(int levelIndex) {
            int spent = 0;
            foreach (var skillRanks in Character.skillRanks) {
                int ranks = skillRanks.Value[levelIndex];
                spent += IsClassSkill(skillRanks.Key, levelIndex)
                    ? ranks
                    : 2 * ranks;
            }

            int intMod = GetStartingIntelligence();
            foreach (var asi in Character.abilityScoreIncrements) {
                if ((asi.Key - 1 <= levelIndex) && (asi.Value == "INT")) {
                    intMod++;
                }
            }
            int allowed = Math.Max(1, ClassDefinitions[Character.classLevels[levelIndex]].SkillPoints + intMod);
            if (levelIndex == 0) {
                allowed *= 4;
            }

            return allowed - spent;
        }

        public int TotalSkillRanks(string skill) {
            return Character.skillRanks.ContainsKey(skill)
                ? Character.skillRanks[skill].Sum()
                : 0;
        }

        public (int value, List<string> explanation) GetStatistic(string stat) {
            return Effects.GetStat(stat).GetValue(Effects, EquippedItems);
        }
        
        public List<Attack> GetAttacks() {
            int sizeChange = Effects.stats[SIZE_CATEGORY].GetValue(Effects, EquippedItems).value - NormalSizeCategory;
            return Attack.Generate(Effects, sizeChange, EquippedItems);
        }

        public List<List<Attack>> GetFullAttacks() {
            int sizeChange = Effects.stats[SIZE_CATEGORY].GetValue(Effects, EquippedItems).value - NormalSizeCategory;
            return Attack.GenerateMultiweaponFullAttacks(Effects, sizeChange, EquippedItems);
        }
    }
}
