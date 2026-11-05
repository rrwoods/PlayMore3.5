using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Global {
    public class CharacterSession : ICharacterSession, ICharacterContext {
        private readonly Character character;
        private readonly IReadOnlyCharacter readOnlyCharacter;
        private readonly IRulesCatalog catalog;
        private EffectPool effects;
        private Equipped equippedItems;
        private readonly Dictionary<string, SubsystemEngine> _subsystems = new Dictionary<string, SubsystemEngine>(StringComparer.OrdinalIgnoreCase);
        private int normalSizeCategory;

        private readonly Dictionary<string, string> rememberedFeats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> rememberedFeatChoices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<(int level, bool includeEquipped), ICharacterSession> _levelSessionCache =
            new ConcurrentDictionary<(int level, bool includeEquipped), ICharacterSession>();
        private readonly bool _isSnapshot;
        internal bool IsSnapshot => _isSnapshot;

        public IReadOnlyCharacter Character => readOnlyCharacter;
        public IReadOnlyDictionary<string, string> RememberedFeats => rememberedFeats;
        public IReadOnlyDictionary<string, string> RememberedFeatChoices => rememberedFeatChoices;
        public Timeline Timeline => character.Timeline;
        public IRulesCatalog Catalog => catalog;
        public IEnumerable<SubsystemEngine> Subsystems => _subsystems.Values;

        public SubsystemEngine? GetSubsystem(string? id) {
            if (string.IsNullOrEmpty(id)) return null;
            return _subsystems.TryGetValue(id!, out var engine) ? engine : null;
        }

        public string CreatureType => effects.CreatureType;
        public IReadOnlyList<string> SubTypes => effects.SubTypes.AsReadOnly();
        public decimal TotalWeight => equippedItems.TotalWeight();
        public decimal EquippedWeight => equippedItems.EquippedWeight();
        public int LightLoad => GetStatistic(LIGHT_LOAD).Value;
        public int MediumLoad => GetStatistic(MEDIUM_LOAD).Value;
        public int HeavyLoad => GetStatistic(HEAVY_LOAD).Value;

        // ICharacterContext explicit / collaborator implementation
        Character ICharacterContext.Character => character;
        EffectPool ICharacterContext.Effects => effects;
        Equipped ICharacterContext.EquippedItems => equippedItems;
        Timeline ICharacterContext.Timeline => Timeline;
        IRulesCatalog ICharacterContext.Catalog => catalog;

        List<AttackRoutine> ICharacterContext.GetAllRoutines() => GetAllRoutines().ToList();
        List<AttackRoutine> ICharacterContext.GetVisibleRoutines() => GetVisibleRoutines().ToList();
        SubsystemEngine? ICharacterContext.GetSubsystem(string? id) => GetSubsystem(id);

        public CharacterSession(Character character, IRulesCatalog catalog)
            : this(character, catalog, false) { }

        internal CharacterSession(Character character, IRulesCatalog catalog, bool isSnapshot) {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.character = character ?? throw new ArgumentNullException(nameof(character));
            _isSnapshot = isSnapshot;
            this.readOnlyCharacter = new ReadOnlyCharacter(this.character);

            this.character.Timeline.Context = this;

            equippedItems = new Equipped();
            effects = new EffectPool(this.character);

            if (this.catalog.Subsystems != null) {
                foreach (var subDef in this.catalog.Subsystems.Values) {
                    var engine = new SubsystemEngine(subDef, this);
                    _subsystems[subDef.Id] = engine;
                }
            }

            foreach (var skill in this.character.skillRanks) {
                while (skill.Value.Count < this.character.classLevels.Count) {
                    skill.Value.Add(0);
                }
            }

            Recalculate();
        }

        private void InvalidateLevelSnapshots() {
            _levelSessionCache.Clear();
        }

        internal T Mutate<T>(Func<Character, T> mutate) {
            if (mutate == null) throw new ArgumentNullException(nameof(mutate));
            var result = mutate(character);
            character.Timeline.Context = this;
            InvalidateLevelSnapshots();
            Recalculate();
            return result;
        }

        public void Mutate(Action<Character> mutate) {
            if (mutate == null) throw new ArgumentNullException(nameof(mutate));
            mutate(character);
            character.Timeline.Context = this;
            InvalidateLevelSnapshots();
            Recalculate();
        }

        private static readonly Dictionary<int, decimal> weightFactors = new Dictionary<int, decimal> {
            [-4] = 0.1m, [-3] = 0.1m, [-2] = 0.1m, [-1] = 0.5m, [0] = 1, [1] = 2, [2] = 5, [3] = 8, [4] = 12
        };

        private Weapon? WeaponFromChoice(WeaponChoice? choice) {
            if (choice == null) return null;
            if (!catalog.TryGetWeapon(choice.Name, out var baseWeapon)) {
                throw new KeyNotFoundException($"Saved character references nonexistent weapon '{choice.Name}'.");
            }
            Weapon weapon = new Weapon(baseWeapon) {
                SizeCategory = choice.SizeCategory,
            };
            weapon.Weight *= weightFactors[weapon.SizeCategory];

            foreach (string property in choice.MeleeProperties) {
                if (!catalog.TryGetWeaponProperty(property, out var prop)) {
                    throw new KeyNotFoundException($"Saved character references nonexistent weapon property '{property}'.");
                }
                weapon.Melee?.atk.Properties.Add(prop);
            }
            foreach (string property in choice.SecondHeadProperties) {
                if (!catalog.TryGetWeaponProperty(property, out var prop)) {
                    throw new KeyNotFoundException($"Saved character references nonexistent weapon property '{property}'.");
                }
                weapon.SecondHead?.atk.Properties.Add(prop);
            }
            foreach (string property in choice.ThrownProperties) {
                if (!catalog.TryGetWeaponProperty(property, out var prop)) {
                    throw new KeyNotFoundException($"Saved character references nonexistent weapon property '{property}'.");
                }
                weapon.Thrown.Properties.Add(prop);
            }
            foreach (string property in choice.ProjectileProperties) {
                if (!catalog.TryGetWeaponProperty(property, out var prop)) {
                    throw new KeyNotFoundException($"Saved character references nonexistent weapon property '{property}'.");
                }
                weapon.Projectile?.atk.Properties.Add(prop);
            }

            return weapon;
        }

        private Armor? ArmorFromChoice(ArmorChoice? choice) {
            if (choice == null) return null;
            if (!catalog.TryGetArmor(choice.Name, out var baseArmor)) {
                throw new KeyNotFoundException($"Saved character references nonexistent armor '{choice.Name}'.");
            }
            Armor armor = new Armor(baseArmor) {
                SizeCategory = choice.SizeCategory
            };
            armor.Weight *= weightFactors[armor.SizeCategory];

            foreach (string property in choice.ArmorProperties) {
                if (!catalog.TryGetArmorProperty(property, out var prop)) {
                    throw new KeyNotFoundException($"Saved character references nonexistent armor property '{property}'.");
                }
                armor.Properties.Add(prop);
            }
            if (choice.Spiked) {
                var spikes = catalog.GetWeapon("armor spikes");
                armor.ArmorSpikes = new AttackMode(spikes.Melee?.atk);
                foreach (string property in choice.MeleeProperties) {
                    if (!catalog.TryGetWeaponProperty(property, out var prop)) {
                        throw new KeyNotFoundException($"Saved character references nonexistent weapon property '{property}'.");
                    }
                    armor.ArmorSpikes.Properties.Add(prop);
                }
            }
            return armor;
        }

        private Shield? ShieldFromChoice(ArmorChoice? choice) {
            if (choice == null) return null;
            if (!catalog.TryGetShield(choice.Name, out var baseShield)) {
                throw new KeyNotFoundException($"Saved character references nonexistent shield '{choice.Name}'.");
            }
            Shield shield = new Shield(baseShield) {
                SizeCategory = choice.SizeCategory
            };
            shield.Weight *= weightFactors[shield.SizeCategory];

            foreach (string property in choice.ArmorProperties) {
                if (!catalog.TryGetArmorProperty(property, out var prop)) {
                    throw new KeyNotFoundException($"Saved character references nonexistent armor property '{property}'.");
                }
                shield.Properties.Add(prop);
            }
            shield.Spiked = choice.Spiked;
            foreach (string property in choice.MeleeProperties) {
                if (!catalog.TryGetWeaponProperty(property, out var prop)) {
                    throw new KeyNotFoundException($"Saved character references nonexistent weapon property '{property}'.");
                }
                shield.ShieldBash.Properties.Add(prop);
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
                location.items.Select(x => {
                    if (!catalog.TryGetItem(x.Key, out var item)) {
                        throw new KeyNotFoundException($"Saved character references nonexistent item '{x.Key}'.");
                    }
                    return item.Weight * x.Value;
                }).Sum();
        }

        private void ComputeWeight() {
            equippedItems.UnequippedWeight = character.inventory.Values
                .Where(x => x.weighs)
                .Select(x => WeightAtLocation(x))
                .Sum();
        }

        public virtual void Recalculate() {
            equippedItems = equippedItems ?? new Equipped();
            effects = new EffectPool(character);
            effects.ReadJson(catalog.CoreRulesJson);
            effects.GenerateCharacterEffects();

            Race race = catalog.GetRace(character.race);
            effects.GenerateRaceEffects(race);
            normalSizeCategory = race.SizeCategory;

            var classLevels = character.SummarizeClassLevels();
            foreach (var classAndLevel in classLevels) {
                CharacterClass @class = catalog.GetClass(classAndLevel.Key);
                effects.GenerateClassEffects(@class, classAndLevel.Value);
                for (int level = 0; level < classAndLevel.Value && level < @class.ClassFeatures.Count; level++) {
                    foreach (Effect feature in @class.ClassFeatures[level]) {
                        effects.AddEffect(feature);
                    }
                    if (level < @class.Flags.Count) {
                        foreach (FlagDefinition flag in @class.Flags[level]) {
                            effects.AddFlag(flag);
                        }
                    }
                }
                effects.AddStats(@class.NewStats);
            }

            ReconcileFeats();

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
                equippedItems.SlottedItems[itemRef.Key] = item;
            }
            foreach (string itemName in character.equipped.SlotlessItems) {
                Item item = catalog.GetItem(itemName);
                equippedItems.SlotlessItems.Add(item);
            }

            var activeGrants = new List<(string Subsystem, string SlotGroup, string Element, string Source, string FeatSlot)>();

            foreach (var feat in character.feats) {
                string featSlot = feat.Key;
                string featName = feat.Value;
                var featDef = catalog.GetFeat(featName);
                var featChoicesList = new List<string>();
                var choicesDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in character.featChoices) {
                    choicesDict[kvp.Key] = kvp.Value;
                }

                if (featDef.Choices.Any()) {
                    foreach (var ch in featDef.Choices) {
                        object? val = ResolveFeatChoice(featSlot, ch);
                        if (val != null) {
                            string strVal = val.ToString();
                            if (!string.IsNullOrEmpty(strVal)) {
                                choicesDict[ch.Id] = val;
                                featChoicesList.Add(strVal);
                            }
                        }
                    }
                } else if (character.featChoices.TryGetValue(featSlot, out var c) && !string.IsNullOrWhiteSpace(c)) {
                    featChoicesList.Add(c);
                    choicesDict[""] = c;
                }

                effects.GenerateFeatEffects(
                    featDef,
                    featChoicesList
                );

                if (featDef.ClassSkills.Any()) {
                    int acquisitionLevel = Math.Max(1, GetFeatSlotLevel(featSlot));
                    foreach (var template in featDef.ClassSkills) {
                        string skill = catalog.ResolveTemplate(template, choicesDict);
                        if (!string.IsNullOrEmpty(skill)) {
                            effects.GrantClassSkill(skill, acquisitionLevel);
                        }
                    }
                }

                if (featDef.Benefit?.Slots != null && featDef.Benefit.Slots.Any()) {
                    foreach (var slotGrant in featDef.Benefit.Slots) {
                        string subsystem = catalog.ResolveTemplate(slotGrant.Subsystem, choicesDict);
                        string slotGroup = catalog.ResolveTemplate(slotGrant.SlotGroup, choicesDict);
                        string element = catalog.ResolveTemplate(slotGrant.Element, choicesDict);
                        string source = catalog.ResolveTemplate(slotGrant.Source, choicesDict);

                        if (!string.IsNullOrEmpty(subsystem) &&
                            !string.IsNullOrEmpty(slotGroup) &&
                            !string.IsNullOrEmpty(element) &&
                            !string.IsNullOrEmpty(source)) {
                            activeGrants.Add((subsystem, slotGroup, element, source, featSlot));
                        }
                    }
                }
            }

            var activeSlottedItems = equippedItems.DonnedArmor == null
                ? equippedItems.SlottedItems.Values
                : equippedItems.SlottedItems.Where(x => x.Key != Equipped.Slot.BODY).Select(x => x.Value);
            foreach (var item in activeSlottedItems.Concat(equippedItems.SlotlessItems)) {
                if (item?.ItemEffect?.Slots != null) {
                    foreach (var slotGrant in item.ItemEffect.Slots) {
                        if (!string.IsNullOrEmpty(slotGrant.Subsystem) &&
                            !string.IsNullOrEmpty(slotGrant.SlotGroup) &&
                            !string.IsNullOrEmpty(slotGrant.Element)) {
                            activeGrants.Add((slotGrant.Subsystem, slotGrant.SlotGroup, slotGrant.Element, slotGrant.Source ?? "Item", "Equipped:" + item.ItemEffect.Name));
                        }
                    }
                }
            }

            foreach (var engine in _subsystems.Values) {
                if (character.Subsystems.TryGetValue(engine.Id, out var state) && state != null) {
                    engine.LoadState(state);
                }

                var engineGrants = activeGrants.Where(g => string.Equals(g.Subsystem, engine.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                engine.PruneStaleGrantedSlots(engineGrants.Select(g => (g.SlotGroup, g.Source, g.FeatSlot)));

                foreach (var g in engineGrants) {
                    engine.GrantSlot(g.SlotGroup, g.Element, g.Source, g.FeatSlot);
                }

                if (engine.IsActive || character.Subsystems.ContainsKey(engine.Id)) {
                    character.Subsystems[engine.Id] = engine.ExportState();
                }
                if (engine.IsActive) {
                    engine.ContributeEffects(effects);
                }
            }

            character.externalEffects.ForEach(x => {
                var ext = catalog.GetExternalEffect(x);
                effects.GenerateExternalEffect(ext);
            });

            equippedItems.ContributeEffects(effects);

            ComputeWeight();
        }

        public Equipped.Slot SlotForItem(string itemName) {
            return catalog.TryGetItem(itemName, out var item) ? item.ItemSlot : Equipped.Slot.SLOTLESS;
        }

        public void CountWeight(string locationName, bool weighs) {
            if (character.inventory.ContainsKey(locationName)) {
                character.inventory[locationName].weighs = weighs;
                ComputeWeight();
            }
        }

        private WeaponChoice? RemoveEquippedWeaponCore(bool primary) {
            WeaponChoice? removed = primary ? character.equipped.PrimaryHandWeapon : character.equipped.OffHandWeapon;
            if (primary) {
                character.equipped.PrimaryHandWeapon = null;
            } else {
                character.equipped.OffHandWeapon = null;
            }
            return removed;
        }

        public WeaponChoice? RemoveEquippedWeapon(bool primary) {
            WeaponChoice? current = primary ? character.equipped.PrimaryHandWeapon : character.equipped.OffHandWeapon;
            if (current == null) {
                return null;
            }

            return Mutate(c => RemoveEquippedWeaponCore(primary));
        }

        private ArmorChoice? RemoveDonnedArmorCore() {
            ArmorChoice? removed = character.equipped.DonnedArmor;
            character.equipped.DonnedArmor = null;
            return removed;
        }

        public ArmorChoice? RemoveDonnedArmor() {
            if (character.equipped.DonnedArmor == null) {
                return null;
            }

            return Mutate(c => RemoveDonnedArmorCore());
        }

        private ArmorChoice? RemoveEquippedShieldCore() {
            ArmorChoice? removed = character.equipped.OffHandShield;
            character.equipped.OffHandShield = null;
            return removed;
        }

        public ArmorChoice? RemoveEquippedShield() {
            if (character.equipped.OffHandShield == null) {
                return null;
            }

            return Mutate(c => RemoveEquippedShieldCore());
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

        private void UnequipWeaponCore(string to, bool primary) {
            WeaponChoice? unequipped = RemoveEquippedWeaponCore(primary);
            if (unequipped != null) {
                character.inventory[to].weapons.Add(unequipped);
            }
        }

        public void UnequipWeapon(string to, bool primary) {
            if (string.IsNullOrEmpty(to) || !character.inventory.ContainsKey(to)) {
                return;
            }

            WeaponChoice? current = primary ? character.equipped.PrimaryHandWeapon : character.equipped.OffHandWeapon;
            if (current == null) {
                return;
            }

            Mutate(c => UnequipWeaponCore(to, primary));
        }

        private void UndonArmorCore(string to) {
            ArmorChoice? removed = RemoveDonnedArmorCore();
            if (removed != null) {
                character.inventory[to].armor.Add(removed);
            }
        }

        public void UndonArmor(string to) {
            if (string.IsNullOrEmpty(to) || !character.inventory.ContainsKey(to)) {
                return;
            }

            if (character.equipped.DonnedArmor == null) {
                return;
            }

            Mutate(c => UndonArmorCore(to));
        }

        private void UnequipShieldCore(string to) {
            ArmorChoice? removed = RemoveEquippedShieldCore();
            if (removed != null) {
                character.inventory[to].shields.Add(removed);
            }
        }

        public void UnequipShield(string to) {
            if (string.IsNullOrEmpty(to) || !character.inventory.ContainsKey(to)) {
                return;
            }

            if (character.equipped.OffHandShield == null) {
                return;
            }

            Mutate(c => UnequipShieldCore(to));
        }

        private void UnequipItemCore(Equipped.Slot from, string to) {
            var items = character.equipped.SlottedItems;
            if (!items.ContainsKey(from)) return;

            string removed = items[from];
            items.Remove(from);
            AddItem(to, removed, 1);
        }

        public void UnequipItem(Equipped.Slot from, string to) {
            if (string.IsNullOrEmpty(to) || !character.inventory.ContainsKey(to)) {
                return;
            }

            if (!character.equipped.SlottedItems.ContainsKey(from)) {
                return;
            }

            Mutate(c => UnequipItemCore(from, to));
        }

        private void UnequipSlotlessItemCore(string to, string item) {
            character.equipped.SlotlessItems.Remove(item);
            Item toRemove = equippedItems.SlotlessItems.FirstOrDefault(x => x.ItemEffect.Name.Equals(item));
            if (toRemove != null) {
                equippedItems.SlotlessItems.Remove(toRemove);
            }
            AddItem(to, item, 1);
        }

        public void UnequipSlotlessItem(string to, string item) {
            if (string.IsNullOrEmpty(to) || !character.inventory.ContainsKey(to)) {
                return;
            }

            if (string.IsNullOrEmpty(item) || !character.equipped.SlotlessItems.Contains(item)) {
                return;
            }

            Mutate(c => UnequipSlotlessItemCore(to, item));
        }

        private void EquipWeaponCore(string from, bool primary, WeaponChoice weaponChoice) {
            character.inventory[from].weapons.Remove(weaponChoice);
            UnequipWeaponCore(from, primary);
            if (primary) {
                character.equipped.PrimaryHandWeapon = weaponChoice;
            } else {
                character.equipped.OffHandWeapon = weaponChoice;
                if (equippedItems.OffHandShield != null && equippedItems.OffHandShield.AttackRollPenalty == null) {
                    UnequipShieldCore(from);
                }
            }
        }

        public void EquipWeapon(string from, bool primary, WeaponChoice weaponChoice) {
            if (string.IsNullOrEmpty(from) || weaponChoice == null) {
                return;
            }

            if (!character.inventory.ContainsKey(from) || !character.inventory[from].weapons.Contains(weaponChoice)) {
                return;
            }

            Mutate(c => EquipWeaponCore(from, primary, weaponChoice));
        }

        private void DonArmorCore(string from, ArmorChoice armorChoice) {
            character.inventory[from].armor.Remove(armorChoice);
            UndonArmorCore(from);
            character.equipped.DonnedArmor = armorChoice;
        }

        public void DonArmor(string from, ArmorChoice armorChoice) {
            if (string.IsNullOrEmpty(from) || armorChoice == null) {
                return;
            }

            if (!character.inventory.ContainsKey(from) || !character.inventory[from].armor.Contains(armorChoice)) {
                return;
            }

            Mutate(c => DonArmorCore(from, armorChoice));
        }

        private void EquipShieldCore(string from, ArmorChoice shieldChoice) {
            character.inventory[from].shields.Remove(shieldChoice);
            UnequipShieldCore(from);
            character.equipped.OffHandShield = shieldChoice;
            Shield? shield = ShieldFromChoice(shieldChoice);
            if (shield != null && shield.AttackRollPenalty == null) {
                UnequipWeaponCore(from, false);
            }
        }

        public void EquipShield(string from, ArmorChoice shieldChoice) {
            if (string.IsNullOrEmpty(from) || shieldChoice == null) {
                return;
            }

            if (!character.inventory.ContainsKey(from) || !character.inventory[from].shields.Contains(shieldChoice)) {
                return;
            }

            Mutate(c => EquipShieldCore(from, shieldChoice));
        }

        private void EquipItemCore(string from, Equipped.Slot to, string itemName) {
            if (to == Equipped.Slot.SLOTLESS) {
                character.equipped.SlotlessItems.Add(itemName);
                RemoveItem(from, itemName, 1);
            } else {
                string? previouslyEquipped = character.equipped.SlottedItems.ContainsKey(to)
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
        }

        public void EquipItem(string from, Equipped.Slot to, string itemName) {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(itemName)) {
                return;
            }

            if (!character.inventory.ContainsKey(from)) {
                return;
            }

            if (!character.inventory[from].items.ContainsKey(itemName) || character.inventory[from].items[itemName] <= 0) {
                return;
            }

            if (!catalog.TryGetItem(itemName, out var item) || item.ItemSlot != to) {
                return;
            }

            Mutate(c => EquipItemCore(from, to, itemName));
        }

        private void SwapWeaponsCore() {
            WeaponChoice swapChoice = character.equipped.PrimaryHandWeapon;
            character.equipped.PrimaryHandWeapon = character.equipped.OffHandWeapon;
            character.equipped.OffHandWeapon = swapChoice;
        }

        public void SwapWeapons() {
            if (character.equipped.PrimaryHandWeapon == character.equipped.OffHandWeapon) {
                return;
            }

            Mutate(c => SwapWeaponsCore());
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
            foreach (var feat in race.BonusFeats) {
                if (feat.Value <= level) {
                    AddFeatSlot(feats, feat.Key, feat.Value);
                }
            }
            if (effects.featSlots != null) {
                foreach (var feat in effects.featSlots) {
                    if (feat.Value <= level) {
                        AddFeatSlot(feats, feat.Key, feat.Value);
                    }
                }
            }
            List<(string, int)> classLevels = character.DetailClassLevels();
            for (int levelIndex = 0; levelIndex < character.classLevels.Count; levelIndex++) {
                (string @class, int classLevel) = classLevels[levelIndex];
                var classDef = catalog.GetClass(@class);
                foreach (var feat in classDef.BonusFeats) {
                    if (feat.Value == classLevel) {
                        AddFeatSlot(feats, feat.Key, levelIndex + 1);
                    }
                }
            }
            return feats;
        }

        private static bool MatchesFeatSlot(string choiceKey, string slot) =>
            string.Equals(choiceKey, slot, StringComparison.OrdinalIgnoreCase) ||
            choiceKey.StartsWith(slot + ":", StringComparison.OrdinalIgnoreCase);

        private void ReconcileFeats() {
            var activeSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var levelSlots in GetFeatSlots().Values) {
                foreach (var slot in levelSlots) {
                    activeSlots.Add(slot);
                }
            }

            // 1. Clean up active slots: if explicitly cleared to empty/whitespace, remove and clear memory
            var activeKeys = character.feats.Keys.Where(slot => activeSlots.Contains(slot)).ToList();
            foreach (var slot in activeKeys) {
                if (string.IsNullOrWhiteSpace(character.feats[slot])) {
                    character.feats.Remove(slot);
                    rememberedFeats.Remove(slot);
                    var choiceKeysToRemove = character.featChoices.Keys.Where(k => MatchesFeatSlot(k, slot)).ToList();
                    foreach (var ck in choiceKeysToRemove) {
                        character.featChoices.Remove(ck);
                        rememberedFeatChoices.Remove(ck);
                    }
                } else {
                    rememberedFeats.Remove(slot);
                }
            }

            // 2. Deactivate: move inactive slots and choices from Character to session memory
            var inactiveSlots = character.feats.Keys.Where(slot => !activeSlots.Contains(slot)).ToList();
            foreach (var slot in inactiveSlots) {
                string featName = character.feats[slot];
                if (!string.IsNullOrWhiteSpace(featName)) {
                    rememberedFeats[slot] = featName;
                }
                character.feats.Remove(slot);

                var matchingChoiceKeys = character.featChoices.Keys.Where(k => MatchesFeatSlot(k, slot)).ToList();
                foreach (var ck in matchingChoiceKeys) {
                    string choiceVal = character.featChoices[ck];
                    if (!string.IsNullOrWhiteSpace(choiceVal)) {
                        rememberedFeatChoices[ck] = choiceVal;
                    }
                    character.featChoices.Remove(ck);
                }
            }

            var orphanChoiceKeys = character.featChoices.Keys.Where(k => {
                string slotName = k.Contains(':') ? k.Substring(0, k.IndexOf(':')) : k;
                return !activeSlots.Contains(slotName);
            }).ToList();
            foreach (var ck in orphanChoiceKeys) {
                string choiceVal = character.featChoices[ck];
                if (!string.IsNullOrWhiteSpace(choiceVal)) {
                    rememberedFeatChoices[ck] = choiceVal;
                }
                character.featChoices.Remove(ck);
            }

            // 3. Restore: for active slots not currently chosen, restore from remembered memory if present
            foreach (var slot in activeSlots) {
                if (!character.feats.ContainsKey(slot)) {
                    if (rememberedFeats.TryGetValue(slot, out var rememberedFeat) && !string.IsNullOrWhiteSpace(rememberedFeat)) {
                        character.feats[slot] = rememberedFeat;
                        rememberedFeats.Remove(slot);

                        var choicesToRestore = rememberedFeatChoices.Keys.Where(k => MatchesFeatSlot(k, slot)).ToList();
                        foreach (var ck in choicesToRestore) {
                            character.featChoices[ck] = rememberedFeatChoices[ck];
                            rememberedFeatChoices.Remove(ck);
                        }
                    }
                }
            }
        }

        public int GetFeatSlotLevel(string featSlot) {
            if (string.IsNullOrEmpty(featSlot)) return 0;
            foreach (var levelEntry in GetFeatSlots()) {
                if (levelEntry.Value.Contains(featSlot, StringComparer.OrdinalIgnoreCase)) {
                    return levelEntry.Key;
                }
            }
            if (featSlot.StartsWith("Level ", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(featSlot.Substring(6), out int parsedLevel)) {
                return parsedLevel;
            }
            return 0;
        }

        public string ResolveFeatTargetSource(string featSlot) {
            if (character.feats.TryGetValue(featSlot, out var featName)) {
                var featDef = catalog.GetFeat(featName);
                var classChoice = featDef?.Choices?.FirstOrDefault(c => string.Equals(c.Domain, "CharacterClasses", StringComparison.OrdinalIgnoreCase));
                if (classChoice != null) {
                    return ResolveClassChoice(featSlot, classChoice);
                }
            }

            int level = GetFeatSlotLevel(featSlot);
            var adeptClasses = GetEligibleClassesAtLevel(level, "adept");

            if (character.featChoices.TryGetValue($"{featSlot}:class", out var chosenClass) && !string.IsNullOrWhiteSpace(chosenClass)) {
                var matched = adeptClasses.FirstOrDefault(c => string.Equals(c, chosenClass, StringComparison.OrdinalIgnoreCase));
                if (matched != null) return matched;
                if (string.Equals(chosenClass, "Feats", StringComparison.OrdinalIgnoreCase)) return "Feats";
                return chosenClass;
            }

            if (adeptClasses.Count == 0) {
                return "Feats";
            }
            return adeptClasses[0];
        }

        public object? ResolveFeatChoice(string featSlot, ChoiceDescriptor descriptor) {
            if (descriptor == null) return null;
            if (string.Equals(descriptor.Domain, "CharacterClasses", StringComparison.OrdinalIgnoreCase)) {
                return ResolveClassChoice(featSlot, descriptor);
            }

            string? rawChoice = null;
            string choiceKey = descriptor.Id;
            if (!string.IsNullOrEmpty(choiceKey) &&
                character.featChoices.TryGetValue($"{featSlot}:{choiceKey}", out var val) &&
                !string.IsNullOrWhiteSpace(val)) {
                rawChoice = val;
            } else if (character.featChoices.TryGetValue(featSlot, out var defaultChoice) &&
                       !string.IsNullOrWhiteSpace(defaultChoice)) {
                rawChoice = defaultChoice;
            } else if (!string.IsNullOrEmpty(descriptor.Fallback)) {
                rawChoice = descriptor.Fallback;
            }

            if (string.IsNullOrEmpty(rawChoice)) return null;

            if (string.Equals(descriptor.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase)) {
                foreach (var engine in _subsystems.Values) {
                    var elem = engine.GetElement(rawChoice!);
                    if (elem != null) return elem;
                }
            }

            return rawChoice;
        }

        public string ResolveClassChoice(string featSlot, ChoiceDescriptor descriptor) {
            if (descriptor == null) return "";
            int level = GetFeatSlotLevel(featSlot);
            var eligibleClasses = GetEligibleClassesAtLevel(level, descriptor.Filter);

            string choiceKey = descriptor.Id;
            if (!string.IsNullOrEmpty(choiceKey) &&
                character.featChoices.TryGetValue($"{featSlot}:{choiceKey}", out var chosenClass) &&
                !string.IsNullOrWhiteSpace(chosenClass)) {
                var matched = eligibleClasses.FirstOrDefault(c => string.Equals(c, chosenClass, StringComparison.OrdinalIgnoreCase));
                if (matched != null) return matched;
                if (!string.IsNullOrEmpty(descriptor.Fallback) && string.Equals(chosenClass, descriptor.Fallback, StringComparison.OrdinalIgnoreCase)) {
                    return descriptor.Fallback!;
                }
                return chosenClass;
            }

            if (eligibleClasses.Count == 0) {
                return descriptor.Fallback ?? "";
            }
            return eligibleClasses[0];
        }

        public bool IsClassEligible(string className, string? filter) {
            if (string.IsNullOrEmpty(filter)) return true;
            if (catalog.TryGetClass(className, out var cls)) {
                if (cls.Tags != null && cls.Tags.Any(t => string.Equals(t, filter, StringComparison.OrdinalIgnoreCase))) {
                    return true;
                }
                if (cls.Subsystems != null && cls.Subsystems.ContainsKey(filter!)) {
                    return true;
                }
            }
            return string.Equals(className, filter, StringComparison.OrdinalIgnoreCase);
        }

        public IReadOnlyList<string> GetEligibleClassesAtLevel(int level, string? filter = null) {
            int cappedLevel = Math.Min(level, character.classLevels.Count);
            if (cappedLevel <= 0) return new List<string>().AsReadOnly();
            var candidateClasses = character.classLevels
                .Take(cappedLevel)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(filter)) {
                return candidateClasses.ToList().AsReadOnly();
            }

            return candidateClasses
                .Where(c => IsClassEligible(c, filter))
                .ToList()
                .AsReadOnly();
        }

        public void SetFeatChoice(string featSlot, string choice) {
            Mutate(c => {
                c.featChoices[featSlot] = choice;
            });
        }

        public void SetFeatChoice(string featSlot, string key, string choice) {
            Mutate(c => {
                c.featChoices[$"{featSlot}:{key}"] = choice;
            });
        }

        public string GetFeatChoice(string featSlot, string? key = null) {
            if (string.IsNullOrEmpty(key)) {
                return character.featChoices.TryGetValue(featSlot, out var choice) ? choice : "";
            }
            return character.featChoices.TryGetValue($"{featSlot}:{key}", out var compChoice) ? compChoice : "";
        }

        private IReadOnlyList<ValidationWarning> EvaluateDualPass(
            int evalLevel,
            Func<CharacterSession, IReadOnlyList<ValidationWarning>> evaluate,
            IReadOnlyDictionary<string, string>? currentChoices = null,
            string? validatingSlotKey = null) {
            int totalLevels = character.classLevels.Count;
            bool hasOverlay = currentChoices != null && currentChoices.Count > 0;

            if (evalLevel >= 0 && evalLevel <= totalLevels) {
                var intrinsicSnapshot = hasOverlay
                    ? CreateSessionAtLevel(evalLevel, includeEquipped: false, currentChoices, validatingSlotKey)
                    : (CharacterSession)GetSessionAtLevel(evalLevel, includeEquipped: false);
                var intrinsicWarnings = evaluate(intrinsicSnapshot);

                if (intrinsicWarnings.Count == 0) {
                    return Array.Empty<ValidationWarning>();
                }

                var equippedSnapshot = hasOverlay
                    ? CreateSessionAtLevel(evalLevel, includeEquipped: true, currentChoices, validatingSlotKey)
                    : (CharacterSession)GetSessionAtLevel(evalLevel, includeEquipped: true);
                var equippedWarnings = evaluate(equippedSnapshot);

                var results = new List<ValidationWarning>();
                foreach (var w in intrinsicWarnings) {
                    bool satisfiedInEquipped = !equippedWarnings.Any(ew => ew.Category == w.Category && string.Equals(ew.Message, w.Message, StringComparison.Ordinal));
                    if (satisfiedInEquipped && w.Category == ValidationCategory.Prerequisite) {
                        results.Add(new ValidationWarning(w.Message, WarningKind.ItemDependent, w.Category));
                    } else {
                        results.Add(w);
                    }
                }
                return results.AsReadOnly();
            }

            return evaluate(this);
        }

        public IReadOnlyList<ValidationWarning> ValidateProgressionChoice(ProgressionChoiceContext context) {
            if (context == null || string.IsNullOrWhiteSpace(context.ChoiceValue)) {
                return Array.Empty<ValidationWarning>();
            }

            if (context.Domain == ProgressionChoiceDomain.Class) {
                return ValidateClassProgressionChoice(context);
            }

            if (context.Domain == ProgressionChoiceDomain.Feat && context.Descriptor == null && catalog.TryGetFeat(context.ChoiceValue, out _)) {
                return Array.Empty<ValidationWarning>();
            }

            // ProgressionChoiceDomain.Feat or Subsystem
            int slotLevel = !string.IsNullOrEmpty(context.SlotKey) ? GetFeatSlotLevel(context.SlotKey) : 0;
            int totalLevels = character.classLevels.Count;
            int acquisitionLevel = context.AcquisitionLevel > 0
                ? context.AcquisitionLevel
                : (slotLevel > 0 ? slotLevel : totalLevels);

            var currentChoices = context.CurrentChoices != null
                ? context.CurrentChoices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                : null;

            string? validatingKey = context.Descriptor != null && !string.IsNullOrEmpty(context.SlotKey) && !string.IsNullOrEmpty(context.Descriptor.Id)
                ? $"{context.SlotKey}:{context.Descriptor.Id}"
                : context.SlotKey;

            if (context.Domain == ProgressionChoiceDomain.Feat && context.Descriptor != null && !string.IsNullOrEmpty(context.SlotKey)) {
                bool hasFeatInSlot = character.feats.ContainsKey(context.SlotKey!) || (currentChoices != null && currentChoices.ContainsKey(context.SlotKey!));
                if (!hasFeatInSlot) {
                    var parentFeat = catalog.FindFeatByChoiceDescriptor(context.Descriptor);
                    if (parentFeat != null) {
                        currentChoices = currentChoices != null ? new Dictionary<string, string>(currentChoices) : new Dictionary<string, string>();
                        currentChoices[context.SlotKey!] = parentFeat.Benefit?.Name ?? "";
                    }
                }
                if (!string.IsNullOrEmpty(context.TargetSource)) {
                    string classChoiceKey = $"{context.SlotKey}:class";
                    if ((currentChoices == null || !currentChoices.ContainsKey(classChoiceKey)) && !character.featChoices.ContainsKey(classChoiceKey)) {
                        currentChoices = currentChoices != null ? new Dictionary<string, string>(currentChoices) : new Dictionary<string, string>();
                        currentChoices[classChoiceKey] = context.TargetSource!;
                    }
                }
            }

            return EvaluateDualPass(
                acquisitionLevel,
                s => s.EvaluateSubsystemWarnings(context.Descriptor, context.ChoiceValue, context.TargetSource, context.SlotKey),
                currentChoices,
                validatingKey);
        }

        private IReadOnlyList<ValidationWarning> ValidateClassProgressionChoice(ProgressionChoiceContext context) {
            int acquisitionLevel = context.AcquisitionLevel;
            if (acquisitionLevel <= 0 && !string.IsNullOrEmpty(context.SlotKey)) {
                if (context.SlotKey!.StartsWith("Level ", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(context.SlotKey.Substring(6), out int parsed)) {
                    acquisitionLevel = parsed;
                }
            }
            if (acquisitionLevel <= 0) {
                acquisitionLevel = Math.Max(1, character.classLevels.Count);
            }

            int evalLevel = Math.Max(0, acquisitionLevel - 1);
            return EvaluateDualPass(
                evalLevel,
                s => s.EvaluateClassWarnings(context.ChoiceValue),
                context.CurrentChoices,
                context.SlotKey);
        }

        private static readonly Dictionary<string, (string StatName, string Abbr)> AbilityScoreMap = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase) {
            ["STR"] = (STR_SCORE, "STR"),
            ["Strength"] = (STR_SCORE, "STR"),
            ["DEX"] = (DEX_SCORE, "DEX"),
            ["Dexterity"] = (DEX_SCORE, "DEX"),
            ["CON"] = (CON_SCORE, "CON"),
            ["Constitution"] = (CON_SCORE, "CON"),
            ["INT"] = (INT_SCORE, "INT"),
            ["Intelligence"] = (INT_SCORE, "INT"),
            ["WIS"] = (WIS_SCORE, "WIS"),
            ["Wisdom"] = (WIS_SCORE, "WIS"),
            ["CHA"] = (CHA_SCORE, "CHA"),
            ["Charisma"] = (CHA_SCORE, "CHA")
        };

        private IReadOnlyList<ValidationWarning> EvaluateClassWarnings(string className) {
            if (!catalog.TryGetClass(className, out var classDef)) {
                return new List<ValidationWarning> {
                    new ValidationWarning($"Class '{className}' is not recognized.", ValidationCategory.Invalid)
                }.AsReadOnly();
            }

            if (string.IsNullOrWhiteSpace(classDef.Prerequisites)) {
                return Array.Empty<ValidationWarning>();
            }

            var warnings = new List<ValidationWarning>();
            var clauses = classDef.Prerequisites.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawClause in clauses) {
                string clause = rawClause.Trim();
                if (string.IsNullOrEmpty(clause)) continue;

                // 1. Base attack bonus: "Base attack bonus +4", "BAB +6", etc.
                var babMatch = Regex.Match(clause, @"^(?:base attack bonus|bab)\s*\+?(\d+)$", RegexOptions.IgnoreCase);
                if (babMatch.Success) {
                    int requiredBab = int.Parse(babMatch.Groups[1].Value);
                    int actualBab = GetStatistic(Constants.BASE_ATTACK_BONUS)?.Value ?? 0;
                    if (actualBab < requiredBab) {
                        warnings.Add(new ValidationWarning($"Requires Base attack bonus +{requiredBab}.", ValidationCategory.Prerequisite));
                    }
                    continue;
                }

                // 2. Ability score: "STR 14", "Strength 13", etc.
                var abilityMatch = Regex.Match(clause, @"^([A-Za-z]+)\s+(\d+)$", RegexOptions.IgnoreCase);
                if (abilityMatch.Success && AbilityScoreMap.TryGetValue(abilityMatch.Groups[1].Value, out var abilityInfo)) {
                    int requiredScore = int.Parse(abilityMatch.Groups[2].Value);
                    int actualScore = GetStatistic(abilityInfo.StatName)?.Value ?? 0;
                    if (actualScore < requiredScore) {
                        warnings.Add(new ValidationWarning($"Requires {abilityInfo.Abbr} {requiredScore}.", ValidationCategory.Prerequisite));
                    }
                    continue;
                }

                // 3. Skill ranks: "Jump 4 ranks", "Hide 8 ranks", etc.
                var skillMatch = Regex.Match(clause, @"^(.+?)\s+(\d+)\s+ranks?$", RegexOptions.IgnoreCase);
                if (skillMatch.Success) {
                    string skillName = skillMatch.Groups[1].Value.Trim();
                    int requiredRanks = int.Parse(skillMatch.Groups[2].Value);
                    int actualRanks = TotalSkillRanks(skillName);
                    if (actualRanks < requiredRanks) {
                        warnings.Add(new ValidationWarning($"Requires {skillName} {requiredRanks} ranks.", ValidationCategory.Prerequisite));
                    }
                    continue;
                }

                // 4. Feat requirement: check if character has the feat
                bool hasFeat = character.feats.Values.Any(f => string.Equals(f, clause, StringComparison.OrdinalIgnoreCase));
                if (!hasFeat) {
                    warnings.Add(new ValidationWarning($"Requires {clause} feat.", ValidationCategory.Prerequisite));
                }
            }

            return warnings.AsReadOnly();
        }

        private IReadOnlyList<ValidationWarning> EvaluateSubsystemWarnings(ChoiceDescriptor? descriptor, string choiceValue, string? targetSource, string? slotKey = null) {
            IEnumerable<SubsystemEngine> candidateEngines = _subsystems.Values;

            if (descriptor != null &&
                !string.IsNullOrEmpty(descriptor.Domain) &&
                !string.Equals(descriptor.Domain, "SubsystemElements", StringComparison.OrdinalIgnoreCase)) {
                candidateEngines = candidateEngines.Where(e =>
                    string.Equals(e.Id, descriptor.Domain, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(e.Name, descriptor.Domain, StringComparison.OrdinalIgnoreCase));
            } else if (!string.IsNullOrEmpty(targetSource) &&
                       !string.Equals(targetSource, "Feats", StringComparison.OrdinalIgnoreCase) &&
                       catalog.TryGetClass(targetSource, out var cls) &&
                       cls.Subsystems != null && cls.Subsystems.Count > 0) {
                candidateEngines = candidateEngines.Where(e => cls.Subsystems.ContainsKey(e.Id));
            }

            var matchingEngines = candidateEngines
                .Where(e => e.GetElement(choiceValue) != null)
                .ToList();

            var targetEngines = matchingEngines.Count > 0 ? matchingEngines : candidateEngines.ToList();

            var allWarnings = new List<ValidationWarning>();
            foreach (var engine in targetEngines) {
                var warnings = engine.ValidateChoice(descriptor, choiceValue, targetSource, slotKey);
                if (warnings != null && warnings.Count > 0) {
                    allWarnings.AddRange(warnings);
                }
            }
            return allWarnings.AsReadOnly();
        }

        private Race GetRequiredRace() {
            return catalog.GetRace(character.race);
        }

        public int GetSizeCategory() {
            return GetRequiredRace().SizeCategory;
        }

        public int GetStartingIntelligence() {
            return character.assignedAbilityScores[INT] + GetRequiredRace().Int;
        }

        public int BaseHp() {
            var race = GetRequiredRace();
            int conScore = character.assignedAbilityScores[CON] +
                race.Con +
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
                if (@class.ClassSkills.Contains(skill)) {
                    return true;
                }
            }
            if (effects.HasGrantedClassSkill(skill, levelIndex)) {
                return true;
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
            int allowed = Math.Max(1, catalog.GetClass(character.classLevels[levelIndex]).SkillPoints + ((intScore / 2) - 5));
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
            return effects.HasFlag(flag, equippedItems);
        }

        public TimelineRefreshRequirement PerformAction(SheetAction? action) {
            if (action == null) return TimelineRefreshRequirement.None;
            var req = action.Perform(this);
            if (req == TimelineRefreshRequirement.FullReload) {
                Recalculate();
            }
            return req;
        }

        public StatView GetStatistic(string name) {
            return effects.GetStatistic(name, equippedItems);
        }

        public int GetStatValue(string name, int defaultValue = 0) {
            return effects.GetStatValue(name, equippedItems, defaultValue);
        }

        public bool IsStatRelevant(string name) {
            if (effects.stats.TryGetValue(name, out var stat)) {
                return stat.Relevant(effects, equippedItems);
            }
            return false;
        }

        public IEnumerable<StatView> GetRelevantStats(string tag) {
            return effects.RelevantStatsWithTag(tag, equippedItems).Select(s => {
                var (val, expl) = s.GetValue(effects, equippedItems);
                return new StatView(s.Name, val, expl.AsReadOnly());
            });
        }

        public IEnumerable<ToggleView> GetControllableFlags() {
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
            int sizeChange = effects.stats[SIZE_CATEGORY].GetValue(effects, equippedItems).value - normalSizeCategory;
            return AttackRoutineEvaluator.EvaluateRoutines(effects, equippedItems, sizeChange).AsReadOnly();
        }

        public IReadOnlyList<AttackRoutine> GetVisibleRoutines() {
            var all = GetAllRoutines();
            var prefs = character.RoutinePreferences;
            return all.Where(r => prefs.IsCategoryVisible(r.Category)).ToList().AsReadOnly();
        }

        public Dictionary<string, List<Text>> GetTextEffects() {
            return effects.GetTextEffects(equippedItems);
        }

        public TimelineAdvanceResult AdvanceTimeline(LengthOfTime? duration) {
            if (duration == null) return new TimelineAdvanceResult();
            return AdvanceTimeline(duration.InRounds());
        }

        public TimelineAdvanceResult AdvanceTimeline(long rounds) {
            character.Timeline.Context = this;
            var result = character.Timeline.Advance(rounds, this);
            Recalculate();
            return result;
        }

        public TimelineAdvanceResult SetTime(DateTime time) {
            character.Timeline.Context = this;
            var result = character.Timeline.SetTime(time, this);
            Recalculate();
            return result;
        }

        public TimelineAdvanceResult StartCombat() {
            character.Timeline.Context = this;
            var result = character.Timeline.StartCombat(this);
            Recalculate();
            return result;
        }

        public TimelineAdvanceResult EndCombat() {
            character.Timeline.Context = this;
            var result = character.Timeline.EndCombat(this);
            Recalculate();
            return result;
        }

        public void AddLevel() {
            Mutate(c => {
                c.classLevels.Add(c.classLevels.DefaultIfEmpty(PlayMore35.CharacterChoices.Character.DEFAULT_CLASS).Last());
                foreach (var skill in c.skillRanks) {
                    skill.Value.Add(0);
                }
            });
        }

        public virtual ICharacterSession GetSessionAtLevel(int level, bool includeEquipped = false) {
            if (level < 0) {
                throw new ArgumentOutOfRangeException(nameof(level), "Level must be greater than or equal to 0.");
            }
            int maxLevel = character.classLevels.Count;
            if (level > maxLevel) {
                throw new ArgumentOutOfRangeException(nameof(level), $"Level {level} cannot exceed character level {maxLevel}.");
            }

            return _levelSessionCache.GetOrAdd((level, includeEquipped), key => CreateSessionAtLevel(key.level, key.includeEquipped));
        }

        internal CharacterSession CreateSessionAtLevel(
            int level,
            bool includeEquipped,
            IReadOnlyDictionary<string, string>? currentChoices = null,
            string? validatingSlotKey = null) {
            var slotLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var levelEntry in GetFeatSlots()) {
                foreach (var slot in levelEntry.Value) {
                    slotLevels[slot] = levelEntry.Key;
                }
            }

            int ResolveSlotLevel(string slot) {
                if (slotLevels.TryGetValue(slot, out int slotLvl)) return slotLvl;
                if (slot.StartsWith("Level ", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(slot.Substring(6), out int parsedLevel)) {
                    return parsedLevel;
                }
                return 0;
            }

            var sliced = new Character {
                name = character.name,
                gender = character.gender,
                alignment = character.alignment,
                race = character.race,
                bonusLanguages = new List<string>(character.bonusLanguages ?? Enumerable.Empty<string>()),
                assignedAbilityScores = new Dictionary<string, int>(character.assignedAbilityScores ?? new Dictionary<string, int>()),
                classLevels = character.classLevels.Take(level).ToList(),
                abilityScoreIncrements = character.abilityScoreIncrements
                    .Where(kvp => kvp.Key <= level)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                rolledHitDice = character.rolledHitDice.Take(level).ToList(),
                skillRanks = character.skillRanks
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Take(level).ToList()),
                feats = new Dictionary<string, string>(),
                featChoices = new Dictionary<string, string>(),
                equipped = includeEquipped ? character.equipped.Clone() : new EquippedChoices(),
                inventory = new Dictionary<string, InventoryLocation>(),
                transactions = new List<GpTransaction>(),
                sessions = new List<XpSessionEntry>(),
                externalEffects = new List<string>(),
                flagValues = new Dictionary<string, bool>(),
                paramValues = new Dictionary<string, int>(),
                Timeline = new Timeline(),
                RoutinePreferences = new AttackRoutinePreferences()
            };

            foreach (var kvp in character.feats) {
                int slotLevel = ResolveSlotLevel(kvp.Key);
                if (slotLevel > 0 && slotLevel <= level) {
                    sliced.feats[kvp.Key] = kvp.Value;
                }
            }

            foreach (var kvp in character.featChoices) {
                if (!string.IsNullOrEmpty(validatingSlotKey) && MatchesFeatSlot(kvp.Key, validatingSlotKey)) {
                    continue;
                }
                string slot = kvp.Key.Contains(':') ? kvp.Key.Substring(0, kvp.Key.IndexOf(':')) : kvp.Key;
                int slotLevel = ResolveSlotLevel(slot);
                if (slotLevel > 0 && slotLevel <= level) {
                    sliced.featChoices[kvp.Key] = kvp.Value;
                }
            }

            foreach (var kvp in character.Subsystems) {
                var subState = new SubsystemState();
                foreach (var pool in kvp.Value.Pools) {
                    subState.Pools[pool.Key] = pool.Value;
                }
                foreach (var slotGrp in kvp.Value.Slots) {
                    var slotsList = new List<SubsystemSlot>();
                    foreach (var s in slotGrp.Value) {
                        if (!string.IsNullOrEmpty(validatingSlotKey) &&
                            !string.IsNullOrEmpty(s.FeatSlot) &&
                            (MatchesFeatSlot(s.FeatSlot, validatingSlotKey) || MatchesFeatSlot(validatingSlotKey, s.FeatSlot))) {
                            continue;
                        }
                        if (s.IsGrantedByEquippedItem) {
                            if (includeEquipped) {
                                slotsList.Add(new SubsystemSlot(s.Element, s.State, s.Source, s.FeatSlot));
                            }
                        } else if (!string.IsNullOrEmpty(s.FeatSlot)) {
                            int slotLevel = ResolveSlotLevel(s.FeatSlot);
                            if (slotLevel > 0 && slotLevel <= level) {
                                slotsList.Add(new SubsystemSlot(s.Element, s.State, s.Source, s.FeatSlot));
                            }
                        } else {
                            if (level > 0 && (string.IsNullOrEmpty(s.Source) || sliced.classLevels.Contains(s.Source, StringComparer.OrdinalIgnoreCase))) {
                                slotsList.Add(new SubsystemSlot(s.Element, s.State, s.Source, s.FeatSlot));
                            }
                        }
                    }
                    subState.Slots[slotGrp.Key] = slotsList;
                }
                sliced.Subsystems[kvp.Key] = subState;
            }

            if (currentChoices != null && level > 0) {
                foreach (var kvp in currentChoices) {
                    if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value)) {
                        continue;
                    }
                    if (!string.IsNullOrEmpty(validatingSlotKey) && MatchesFeatSlot(kvp.Key, validatingSlotKey)) {
                        continue;
                    }
                    string slot = kvp.Key.Contains(':') ? kvp.Key.Substring(0, kvp.Key.IndexOf(':')) : kvp.Key;
                    int slotLevel = ResolveSlotLevel(slot);
                    if (slotLevel <= 0 || slotLevel <= level) {
                        sliced.featChoices[kvp.Key] = kvp.Value;
                        if (!kvp.Key.Contains(':')) {
                            if (catalog.TryGetFeat(kvp.Value, out _)) {
                                sliced.feats[kvp.Key] = kvp.Value;
                            } else if (catalog.Subsystems != null) {
                                foreach (var subDef in catalog.Subsystems.Values) {
                                    if (subDef.Elements.TryGetValue(kvp.Value, out var elem) && elem != null) {
                                        string? targetGroup = subDef.SlotGroups.Keys.FirstOrDefault(g => {
                                            subDef.SlotGroups.TryGetValue(g, out var gDef);
                                            return gDef != null && (string.IsNullOrEmpty(gDef.Filter) || elem.MatchesFilter(gDef.Filter, subDef));
                                        });
                                        if (!string.IsNullOrEmpty(targetGroup)) {
                                            if (!sliced.Subsystems.TryGetValue(subDef.Id, out var subState)) {
                                                subState = new SubsystemState();
                                                sliced.Subsystems[subDef.Id] = subState;
                                            }
                                            if (!subState.Slots.TryGetValue(targetGroup, out var slotList)) {
                                                slotList = new List<SubsystemSlot>();
                                                subState.Slots[targetGroup] = slotList;
                                            }
                                            string source = sliced.classLevels.FirstOrDefault(c => IsClassEligible(c, subDef.Id)) ?? "Feats";
                                            string defaultState = subDef.SlotGroups[targetGroup].DefaultState;
                                            slotList.Add(new SubsystemSlot(elem.Name, defaultState, source, null));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var snapSession = new CharacterSession(sliced, catalog, isSnapshot: true);

            foreach (var engine in snapSession._subsystems.Values) {
                engine.TrimSlotsToCapacity();
            }

            return snapSession;
        }
    }
}
