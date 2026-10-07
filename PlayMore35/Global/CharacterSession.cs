using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems;
using PlayMore35.Subsystems.PactMagic;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Global {
    public class CharacterSession : ICharacterSession, ICharacterContext {
        private Character character;
        private readonly IReadOnlyCharacter readOnlyCharacter;
        private readonly IRulesCatalog catalog;
        private EffectPool effects;
        private Equipped equippedItems;
        private readonly SubsystemRegistry subsystems = new SubsystemRegistry();
        private int normalSizeCategory;

        public IReadOnlyCharacter Character => readOnlyCharacter;
        public Timeline Timeline => character?.Timeline;
        public IRulesCatalog Catalog => catalog;
        public SubsystemRegistry Subsystems => subsystems;

        public string CreatureType => effects?.CreatureType ?? "";
        public IReadOnlyList<string> SubTypes => (effects?.SubTypes ?? new List<string>()).AsReadOnly();
        public decimal TotalWeight => equippedItems?.TotalWeight() ?? 0m;
        public decimal EquippedWeight => equippedItems?.EquippedWeight() ?? 0m;
        public int LightLoad => GetStatistic(LIGHT_LOAD).Value;
        public int MediumLoad => GetStatistic(MEDIUM_LOAD).Value;
        public int HeavyLoad => GetStatistic(HEAVY_LOAD).Value;

        // ICharacterContext explicit / collaborator implementation
        Character ICharacterContext.Character => character;
        EffectPool ICharacterContext.Effects => effects;
        Equipped ICharacterContext.EquippedItems => equippedItems;
        Timeline ICharacterContext.Timeline => Timeline;
        SubsystemRegistry ICharacterContext.Subsystems => subsystems;
        void ICharacterContext.ClearSlot(string subsystem, int slotIndex) => ClearSlot(subsystem, slotIndex);

        List<AttackRoutine> ICharacterContext.GetAllRoutines() => GetAllRoutines().ToList();
        List<AttackRoutine> ICharacterContext.GetVisibleRoutines() => GetVisibleRoutines().ToList();

        public CharacterSession(Character character, IRulesCatalog catalog) {
            this.catalog = catalog ?? new GameRulesCatalog();
            this.character = character ?? new Character();
            this.readOnlyCharacter = new ReadOnlyCharacter(this.character);

            if (this.character.Timeline != null) {
                this.character.Timeline.Context = this;
            }

            foreach (var skill in this.character.skillRanks) {
                while (skill.Value.Count < this.character.classLevels.Count) {
                    skill.Value.Add(0);
                }
            }

            subsystems.Register(new PactMagicEngine(), "vestige");
            foreach (var engine in subsystems.All) {
                engine.Initialize(this);
            }

            Recalculate();
        }

        public void Mutate(Action<Character> mutate) {
            if (mutate == null) return;
            mutate(character);
            if (character.Timeline != null) {
                character.Timeline.Context = this;
            }
            Recalculate();
        }

        private static readonly Dictionary<int, decimal> weightFactors = new Dictionary<int, decimal> {
            [-4] = 0.1m, [-3] = 0.1m, [-2] = 0.1m, [-1] = 0.5m, [0] = 1, [1] = 2, [2] = 5, [3] = 8, [4] = 12
        };

        private Weapon WeaponFromChoice(WeaponChoice choice) {
            if (choice == null) return null;
            var baseWeapon = catalog.GetWeapon(choice.Name);
            if (baseWeapon == null) return null;
            Weapon weapon = new Weapon(baseWeapon) {
                SizeCategory = choice.SizeCategory,
            };
            weapon.Weight *= weightFactors[weapon.SizeCategory];

            foreach (string property in choice.MeleeProperties) {
                var prop = catalog.GetWeaponProperty(property);
                if (prop != null) weapon.Melee?.atk.Properties.Add(prop);
            }
            foreach (string property in choice.SecondHeadProperties) {
                var prop = catalog.GetWeaponProperty(property);
                if (prop != null) weapon.SecondHead?.atk.Properties.Add(prop);
            }
            foreach (string property in choice.ThrownProperties) {
                var prop = catalog.GetWeaponProperty(property);
                if (prop != null) weapon.Thrown.Properties.Add(prop);
            }
            foreach (string property in choice.ProjectileProperties) {
                var prop = catalog.GetWeaponProperty(property);
                if (prop != null) weapon.Projectile?.atk.Properties.Add(prop);
            }

            return weapon;
        }

        private Armor ArmorFromChoice(ArmorChoice choice) {
            if (choice == null) return null;
            var baseArmor = catalog.GetArmor(choice.Name);
            if (baseArmor == null) return null;
            Armor armor = new Armor(baseArmor) {
                SizeCategory = choice.SizeCategory
            };
            armor.Weight *= weightFactors[armor.SizeCategory];

            foreach (string property in choice.ArmorProperties) {
                var prop = catalog.GetArmorProperty(property);
                if (prop != null) armor.Properties.Add(prop);
            }
            if (choice.Spiked) {
                var spikes = catalog.GetWeapon("armor spikes");
                if (spikes != null) {
                    armor.ArmorSpikes = new AttackMode(spikes.Melee?.atk);
                    foreach (string property in choice.MeleeProperties) {
                        var prop = catalog.GetWeaponProperty(property);
                        if (prop != null) armor.ArmorSpikes.Properties.Add(prop);
                    }
                }
            }
            return armor;
        }

        private Shield ShieldFromChoice(ArmorChoice choice) {
            if (choice == null) return null;
            var baseShield = catalog.GetShield(choice.Name);
            if (baseShield == null) return null;
            Shield shield = new Shield(baseShield) {
                SizeCategory = choice.SizeCategory
            };
            shield.Weight *= weightFactors[shield.SizeCategory];

            foreach (string property in choice.ArmorProperties) {
                var prop = catalog.GetArmorProperty(property);
                if (prop != null) shield.Properties.Add(prop);
            }
            shield.Spiked = choice.Spiked;
            foreach (string property in choice.MeleeProperties) {
                var prop = catalog.GetWeaponProperty(property);
                if (prop != null) shield.ShieldBash.Properties.Add(prop);
            }
            return shield;
        }

        public decimal WeightAtLocation(string location) {
            if (!character.inventory.ContainsKey(location)) {
                return 0;
            }
            return WeightAtLocation(character.inventory[location]);
        }

        private decimal WeightAtLocation(InventoryLocation location) {
            return
                location.weapons.Select(x => WeaponFromChoice(x)?.Weight ?? 0).Sum() +
                location.armor.Select(x => ArmorFromChoice(x)?.Weight ?? 0).Sum() +
                location.shields.Select(x => ShieldFromChoice(x)?.Weight ?? 0).Sum() +
                location.items.Select(x => (catalog.GetItem(x.Key)?.Weight ?? 0) * x.Value).Sum();
        }

        private void ComputeWeight() {
            if (equippedItems != null) {
                equippedItems.UnequippedWeight = character.inventory.Values
                    .Where(x => x.weighs)
                    .Select(x => WeightAtLocation(x))
                    .Sum();
            }
        }

        public void Recalculate() {
            equippedItems = equippedItems ?? new Equipped();
            effects = new EffectPool();
            effects.ReadJson(catalog.CoreRulesJson, character);
            effects.GenerateCharacterEffects(character);

            Race race = catalog.GetRace(character.race);
            if (race != null) {
                effects.GenerateRaceEffects(race);
                normalSizeCategory = race.SizeCategory;
            }

            var classLevels = character.SummarizeClassLevels();
            foreach (var classAndLevel in classLevels) {
                CharacterClass @class = catalog.GetClass(classAndLevel.Key);
                if (@class != null) {
                    effects.GenerateClassEffects(@class, classAndLevel.Value);
                    for (int level = 0; level < classAndLevel.Value && level < @class.ClassFeatures.Count; level++) {
                        foreach (Effect feature in @class.ClassFeatures[level]) {
                            effects.AddEffect(feature);
                        }
                        if (level < @class.Flags.Count) {
                            foreach (FlagDefinition flag in @class.Flags[level]) {
                                effects.controllableFlags[flag.Name] = new ControllableFlag(flag, character);
                            }
                        }
                    }
                    effects.AddStats(@class.NewStats);
                }
            }

            foreach (var feat in character.feats) {
                string featSlot = feat.Key;
                string featName = feat.Value;
                var featDef = catalog.GetFeat(featName);
                if (featDef != null) {
                    effects.GenerateFeatEffects(
                        featDef,
                        character.featChoices.ContainsKey(featSlot) ? character.featChoices[featSlot] : ""
                    );
                }
            }

            foreach (var engine in subsystems.All) {
                if (engine.IsActive) {
                    engine.ContributeEffects(effects, this);
                }
            }

            character.externalEffects.ForEach(x => {
                var ext = catalog.GetExternalEffect(x);
                if (ext != null) effects.GenerateExternalEffect(ext);
            });

            equippedItems = new Equipped();

            if (character.equipped.PrimaryHandWeapon != null) {
                equippedItems.PrimaryHandWeapon = WeaponFromChoice(character.equipped.PrimaryHandWeapon);
            }
            if (character.equipped.OffHandWeapon != null) {
                equippedItems.OffHandWeapon = WeaponFromChoice(character.equipped.OffHandWeapon);
            }
            if (character.equipped.DonnedArmor != null) {
                equippedItems.DonnedArmor = ArmorFromChoice(character.equipped.DonnedArmor);
            }
            if (character.equipped.OffHandShield != null) {
                equippedItems.OffHandShield = ShieldFromChoice(character.equipped.OffHandShield);
            }

            foreach (var itemRef in character.equipped.SlottedItems) {
                Item item = catalog.GetItem(itemRef.Value);
                if (item != null) {
                    equippedItems.SlottedItems[itemRef.Key] = item;
                    item.NewFlags.ForEach(x => effects.controllableFlags[x.Name] = new ControllableFlag(x, character));
                }
            }
            foreach (string itemName in character.equipped.SlotlessItems) {
                Item item = catalog.GetItem(itemName);
                if (item != null) {
                    equippedItems.SlotlessItems.Add(item);
                    item.NewFlags.ForEach(x => effects.controllableFlags[x.Name] = new ControllableFlag(x, character));
                }
            }

            effects.GenerateUses(equippedItems, character);

            foreach (var param in effects.controllableParameters.ToList()) {
                if (character.paramValues.ContainsKey(param.Key)) {
                    param.Value.Value = character.paramValues[param.Key];
                }
            }

            ComputeWeight();
        }

        public Equipped.Slot SlotForItem(string itemName) {
            return catalog.GetItem(itemName)?.ItemSlot ?? Equipped.Slot.SLOTLESS;
        }

        public void CountWeight(string locationName, bool weighs) {
            if (character.inventory.ContainsKey(locationName)) {
                character.inventory[locationName].weighs = weighs;
                ComputeWeight();
            }
        }

        public WeaponChoice RemoveEquippedWeapon(bool primary) {
            WeaponChoice removed = primary ? character.equipped.PrimaryHandWeapon : character.equipped.OffHandWeapon;
            if (primary) {
                character.equipped.PrimaryHandWeapon = null;
            } else {
                character.equipped.OffHandWeapon = null;
            }

            Recalculate();
            return removed;
        }

        public ArmorChoice RemoveDonnedArmor() {
            ArmorChoice removed = character.equipped.DonnedArmor;
            character.equipped.DonnedArmor = null;

            Recalculate();
            return removed;
        }

        public ArmorChoice RemoveEquippedShield() {
            ArmorChoice removed = character.equipped.OffHandShield;
            character.equipped.OffHandShield = null;

            Recalculate();
            return removed;
        }

        public int RemoveItem(string from, string itemName, int quantity) {
            if (!character.inventory.ContainsKey(from) || !character.inventory[from].items.ContainsKey(itemName)) {
                return 0;
            }

            int maxQuantity = character.inventory[from].items[itemName];
            if (quantity >= maxQuantity) {
                quantity = maxQuantity;
                character.inventory[from].items.Remove(itemName);
            } else {
                character.inventory[from].items[itemName] -= quantity;
            }

            ComputeWeight();
            return quantity;
        }

        public void RemoveThing(string from, object thing) {
            if (!character.inventory.ContainsKey(from)) return;

            if (thing is WeaponChoice weapon) {
                character.inventory[from].weapons.Remove(weapon);
                ComputeWeight();
            } else if (thing is ArmorChoice armor) {
                if (armor.IsShield) {
                    character.inventory[from].shields.Remove(armor);
                } else {
                    character.inventory[from].armor.Remove(armor);
                }
                ComputeWeight();
            } else if (thing is string itemName) {
                RemoveItem(from, itemName, 1);
            }
        }

        public void RemoveLocation(string location) {
            character.inventory.Remove(location);
        }

        public void UnequipWeapon(string to, bool primary) {
            if (!character.inventory.ContainsKey(to)) return;

            WeaponChoice unequipped = RemoveEquippedWeapon(primary);
            if (unequipped != null) {
                character.inventory[to].weapons.Add(unequipped);
            }

            Recalculate();
        }

        public void UndonArmor(string to) {
            if (!character.inventory.ContainsKey(to)) return;

            ArmorChoice removed = RemoveDonnedArmor();
            if (removed != null) {
                character.inventory[to].armor.Add(removed);
            }

            Recalculate();
        }

        public void UnequipShield(string to) {
            if (!character.inventory.ContainsKey(to)) return;

            ArmorChoice removed = RemoveEquippedShield();
            if (removed != null) {
                character.inventory[to].shields.Add(removed);
            }

            Recalculate();
        }

        public void UnequipItem(Equipped.Slot from, string to) {
            var items = character.equipped.SlottedItems;
            if (!items.ContainsKey(from)) return;

            string removed = items[from];
            items.Remove(from);
            AddItem(to, removed, 1);

            Recalculate();
        }

        public void UnequipSlotlessItem(string to, string item) {
            if (!character.equipped.SlotlessItems.Contains(item)) return;

            character.equipped.SlotlessItems.Remove(item);
            Item toRemove = equippedItems.SlotlessItems.FirstOrDefault(x => x.ItemEffect.Name.Equals(item));
            if (toRemove != null) {
                equippedItems.SlotlessItems.Remove(toRemove);
            }
            AddItem(to, item, 1);

            Recalculate();
        }

        public void EquipWeapon(string from, bool primary, WeaponChoice weaponChoice) {
            if (!character.inventory.ContainsKey(from) || !character.inventory[from].weapons.Contains(weaponChoice)) {
                return;
            }

            character.inventory[from].weapons.Remove(weaponChoice);
            UnequipWeapon(from, primary);
            if (primary) {
                character.equipped.PrimaryHandWeapon = weaponChoice;
            } else {
                character.equipped.OffHandWeapon = weaponChoice;
                if (equippedItems.OffHandShield != null && equippedItems.OffHandShield.AttackRollPenalty == null) {
                    UnequipShield(from);
                }
            }

            Recalculate();
        }

        public void DonArmor(string from, ArmorChoice armorChoice) {
            if (!character.inventory.ContainsKey(from) || !character.inventory[from].armor.Contains(armorChoice)) {
                return;
            }

            character.inventory[from].armor.Remove(armorChoice);
            UndonArmor(from);
            character.equipped.DonnedArmor = armorChoice;

            Recalculate();
        }

        public void EquipShield(string from, ArmorChoice shieldChoice) {
            if (!character.inventory.ContainsKey(from) || !character.inventory[from].shields.Contains(shieldChoice)) {
                return;
            }

            character.inventory[from].shields.Remove(shieldChoice);
            character.equipped.OffHandShield = shieldChoice;
            Shield shield = ShieldFromChoice(shieldChoice);
            if (shield != null && shield.AttackRollPenalty == null) {
                UnequipWeapon(from, false);
            }

            Recalculate();
        }

        public void EquipItem(string from, Equipped.Slot to, string itemName) {
            if (!character.inventory.ContainsKey(from)) return;

            Item item = catalog.GetItem(itemName);
            if (item == null || item.ItemSlot != to) return;

            if (to == Equipped.Slot.SLOTLESS) {
                character.equipped.SlotlessItems.Add(itemName);
                RemoveItem(from, itemName, 1);
            } else {
                string previouslyEquipped = character.equipped.SlottedItems.ContainsKey(to)
                    ? character.equipped.SlottedItems[to]
                    : null;
                character.equipped.SlottedItems[to] = itemName;
                RemoveItem(from, itemName, 1);

                if (previouslyEquipped != null) {
                    if (character.inventory[from].items.ContainsKey(previouslyEquipped)) {
                        character.inventory[from].items[previouslyEquipped] += 1;
                    } else {
                        character.inventory[from].items[previouslyEquipped] = 1;
                    }
                }
            }

            Recalculate();
        }

        public void SwapWeapons() {
            WeaponChoice swapChoice = character.equipped.PrimaryHandWeapon;
            character.equipped.PrimaryHandWeapon = character.equipped.OffHandWeapon;
            character.equipped.OffHandWeapon = swapChoice;

            Recalculate();
        }

        public void MoveItem(string from, string to, object item, int quantity) {
            if (!character.inventory.ContainsKey(from) || !character.inventory.ContainsKey(to)) {
                return;
            }

            if (item is string itemName) {
                if (character.inventory[from].items.ContainsKey(itemName)) {
                    quantity = RemoveItem(from, itemName, quantity);
                    AddItem(to, itemName, quantity);
                }
            } else if (item is WeaponChoice weapon) {
                if (character.inventory[from].weapons.Remove(weapon)) {
                    character.inventory[to].weapons.Add(weapon);
                }
            } else if (item is ArmorChoice armor) {
                if (armor.IsShield) {
                    if (character.inventory[from].shields.Remove(armor)) {
                        character.inventory[to].shields.Add(armor);
                    }
                } else if (character.inventory[from].armor.Remove(armor)) {
                    character.inventory[to].armor.Add(armor);
                }
            }

            ComputeWeight();
        }

        public void AddWeapon(string to, string weaponName, List<string> properties) {
            if (!character.inventory.ContainsKey(to)) return;
            character.inventory[to].weapons.Add(new WeaponChoice {
                Name = weaponName,
                SizeCategory = GetSizeCategory(),
                MeleeProperties = properties ?? new List<string>()
            });
            ComputeWeight();
        }

        public void AddArmor(string to, string armorName, List<string> properties) {
            if (!character.inventory.ContainsKey(to)) return;
            character.inventory[to].armor.Add(new ArmorChoice {
                Name = armorName,
                SizeCategory = GetSizeCategory(),
                ArmorProperties = properties ?? new List<string>()
            });
            ComputeWeight();
        }

        public void AddShield(string to, string shieldName, List<string> properties) {
            if (!character.inventory.ContainsKey(to)) return;
            character.inventory[to].shields.Add(new ArmorChoice {
                Name = shieldName,
                SizeCategory = GetSizeCategory(),
                ArmorProperties = properties ?? new List<string>(),
                IsShield = true
            });
            ComputeWeight();
        }

        public void AddItem(string to, string itemName, int quantity = 1) {
            if (!character.inventory.ContainsKey(to)) return;
            if (character.inventory[to].items.ContainsKey(itemName)) {
                character.inventory[to].items[itemName] += quantity;
            } else {
                character.inventory[to].items[itemName] = quantity;
            }
        }

        private void AddFeatSlot(SortedDictionary<int, List<string>> feats, string slotName, int level) {
            if (feats.ContainsKey(level)) {
                feats[level].Add(slotName);
            } else {
                feats[level] = new List<string> { slotName };
            }
        }

        public SortedDictionary<int, List<string>> GetFeatSlots() {
            int level = character.classLevels.Count;
            SortedDictionary<int, List<string>> feats = new SortedDictionary<int, List<string>>();
            var race = catalog.GetRace(character.race);
            if (race != null) {
                foreach (var feat in race.BonusFeats) {
                    if (feat.Value <= level) {
                        AddFeatSlot(feats, feat.Key, feat.Value);
                    }
                }
            }
            foreach (var feat in effects.featSlots) {
                if (feat.Value <= level) {
                    AddFeatSlot(feats, feat.Key, feat.Value);
                }
            }
            List<(string, int)> classLevels = character.DetailClassLevels();
            for (int levelIndex = 0; levelIndex < character.classLevels.Count; levelIndex++) {
                (string @class, int classLevel) = classLevels[levelIndex];
                var classDef = catalog.GetClass(@class);
                if (classDef != null) {
                    foreach (var feat in classDef.BonusFeats) {
                        if (feat.Value == classLevel) {
                            AddFeatSlot(feats, feat.Key, levelIndex + 1);
                        }
                    }
                }
            }
            return feats;
        }

        public int GetSizeCategory() {
            return catalog.GetRace(character.race)?.SizeCategory ?? 0;
        }

        public int GetStartingIntelligence() {
            return character.assignedAbilityScores[INT] + (catalog.GetRace(character.race)?.Int ?? 0);
        }

        public int BaseHp() {
            int conScore = character.assignedAbilityScores[CON] +
                (catalog.GetRace(character.race)?.Con ?? 0) +
                character.abilityScoreIncrements.Values.Where(x => x == "CON").Count();
            int conMod = (conScore / 2) - 5;
            int hp = 0;
            foreach (int roll in character.rolledHitDice) {
                if (roll == 0) continue;
                hp += Math.Max(1, roll + conMod);
            }
            return hp;
        }

        public bool TooManyRanks(string skill, int levelIndex) {
            decimal maxClassRanks = levelIndex + 4;
            decimal maxRanks = Enumerable.Range(0, levelIndex + 1).Any(i => IsClassSkill(skill, i))
                ? maxClassRanks
                : maxClassRanks / 2;
            decimal ranksAtLevel = character.skillRanks.ContainsKey(skill)
                ? character.skillRanks[skill].Take(levelIndex + 1).Sum()
                : 0;
            return ranksAtLevel > maxRanks;
        }

        public bool IsClassSkill(string skill, int levelIndex) {
            if (levelIndex < character.classLevels.Count) {
                var @class = catalog.GetClass(character.classLevels[levelIndex]);
                if (@class != null && @class.ClassSkills.Contains(skill)) {
                    return true;
                }
            }
            return false;
        }

        public int SkillPointsRemainingAtLevel(int levelIndex) {
            int spent = 0;
            foreach (var skillRanks in character.skillRanks) {
                int ranks = skillRanks.Value[levelIndex];
                spent += IsClassSkill(skillRanks.Key, levelIndex)
                    ? ranks
                    : 2 * ranks;
            }

            int intScore = GetStartingIntelligence();
            foreach (var asi in character.abilityScoreIncrements) {
                if ((asi.Key - 1 <= levelIndex) && (asi.Value == "INT")) {
                    intScore++;
                }
            }
            int allowed = Math.Max(1, (catalog.GetClass(character.classLevels[levelIndex])?.SkillPoints ?? 2) + ((intScore / 2) - 5));
            if (levelIndex == 0) {
                allowed *= 4;
            }

            return allowed - spent;
        }

        public int TotalSkillRanks(string skill) {
            return character.skillRanks.ContainsKey(skill)
                ? character.skillRanks[skill].Sum()
                : 0;
        }

        public void SetFlag(string flagName, bool value) {
            character.flagValues[flagName] = value;
            if (effects.controllableFlags.TryGetValue(flagName, out var flag)) {
                flag.On = value;
            }
        }

        public void SetParameter(string parameterName, int value) {
            character.paramValues[parameterName] = value;
            if (effects.controllableParameters.TryGetValue(parameterName, out var param)) {
                param.Value = value;
            }
        }

        public void ForceHiddenFlagsOff() {
            foreach (var flag in effects.controllableFlags) {
                if (!flag.Value.Show.On(effects, equippedItems)) {
                    flag.Value.On = false;
                }
            }
        }

        public bool HasFlag(string flag) {
            return effects?.HasFlag(flag, equippedItems) ?? false;
        }

        public TimelineRefreshRequirement PerformAction(SheetAction action) {
            if (action == null) return TimelineRefreshRequirement.None;
            var req = action.Perform(this);
            if (req == TimelineRefreshRequirement.FullReload) {
                Recalculate();
            }
            return req;
        }

        public StatView GetStatistic(string name) {
            if (effects != null && effects.stats.TryGetValue(name, out var stat)) {
                var (val, expl) = stat.GetValue(effects, equippedItems);
                return new StatView(name, val, expl.AsReadOnly());
            }
            return new StatView(name, 0);
        }

        public bool IsStatRelevant(string name) {
            if (effects != null && effects.stats.TryGetValue(name, out var stat)) {
                return stat.Relevant(effects, equippedItems);
            }
            return false;
        }

        public IEnumerable<StatView> GetRelevantStats(string tag) {
            if (effects == null) return Enumerable.Empty<StatView>();
            return effects.RelevantStatsWithTag(tag, equippedItems).Select(s => {
                var (val, expl) = s.GetValue(effects, equippedItems);
                return new StatView(s.Name, val, expl.AsReadOnly());
            });
        }

        public IEnumerable<ToggleView> GetControllableFlags() {
            if (effects == null) return Enumerable.Empty<ToggleView>();
            return effects.controllableFlags.Values.Select(f =>
                new ToggleView(
                    f.Name,
                    f.Category,
                    f.Description != null ? f.Description.FillStatValues(effects, equippedItems) : "",
                    f.On,
                    f.Show.On(effects, equippedItems),
                    f.Resource
                )
            );
        }

        public IEnumerable<ParamView> GetControllableParameters() {
            if (effects == null) return Enumerable.Empty<ParamView>();
            return effects.controllableParameters.Values.Select(p =>
                new ParamView(
                    p.Name,
                    p.Category,
                    p.Description,
                    p.Value
                )
            );
        }

        public IReadOnlyList<AttackRoutine> GetAllRoutines() {
            if (effects == null || equippedItems == null) return new List<AttackRoutine>().AsReadOnly();
            int sizeChange = effects.stats[SIZE_CATEGORY].GetValue(effects, equippedItems).value - normalSizeCategory;
            return AttackRoutineEvaluator.EvaluateRoutines(effects, equippedItems, sizeChange).AsReadOnly();
        }

        public IReadOnlyList<AttackRoutine> GetVisibleRoutines() {
            var all = GetAllRoutines();
            var prefs = character?.RoutinePreferences ?? new AttackRoutinePreferences();
            return all.Where(r => prefs.IsCategoryVisible(r.Category)).ToList().AsReadOnly();
        }

        public Dictionary<string, List<Text>> GetTextEffects() {
            return effects?.GetTextEffects(equippedItems) ?? new Dictionary<string, List<Text>>();
        }

        public TimelineAdvanceResult AdvanceTimeline(LengthOfTime duration) {
            if (duration == null) return new TimelineAdvanceResult();
            return AdvanceTimeline(duration.InRounds());
        }

        public TimelineAdvanceResult AdvanceTimeline(long rounds) {
            if (character.Timeline == null) return new TimelineAdvanceResult();
            character.Timeline.Context = this;
            var result = character.Timeline.Advance(rounds, this);
            Recalculate();
            return result;
        }

        public TimelineAdvanceResult SetTime(DateTime time) {
            if (character.Timeline == null) return new TimelineAdvanceResult();
            character.Timeline.Context = this;
            var result = character.Timeline.SetTime(time, this);
            Recalculate();
            return result;
        }

        public TimelineAdvanceResult StartCombat() {
            if (character.Timeline == null) return new TimelineAdvanceResult();
            character.Timeline.Context = this;
            var result = character.Timeline.StartCombat(this);
            Recalculate();
            return result;
        }

        public TimelineAdvanceResult EndCombat() {
            if (character.Timeline == null) return new TimelineAdvanceResult();
            character.Timeline.Context = this;
            var result = character.Timeline.EndCombat(this);
            Recalculate();
            return result;
        }

        public void ClearSlot(string subsystem, int slotIndex) {
            var slotted = subsystems.Get(subsystem) as ISlottedSubsystem;
            slotted?.ClearSlot(slotIndex);
            Recalculate();
        }

        public void AddLevel() {
            Mutate(c => {
                c.classLevels.Add(c.classLevels.DefaultIfEmpty(PlayMore35.CharacterChoices.Character.DEFAULT_CLASS).Last());
                foreach (var skill in c.skillRanks) {
                    skill.Value.Add(0);
                }
            });
        }
    }
}
