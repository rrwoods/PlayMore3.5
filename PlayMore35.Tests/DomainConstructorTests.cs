using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;

namespace PlayMore35.Tests {
    [TestClass]
    public class DomainConstructorTests {
        [TestMethod]
        public void Item_CanonicalConstructor_InitializesFieldsAndCollections() {
            var effect = new Effect("Ring of Protection");
            var stat = new Statistic("ring_stat");
            var flag = new FlagDefinition("ring_flag");

            var item = new Item(Equipped.Slot.RING1, effect, 0.5m, new[] { stat }, new[] { flag });

            Assert.AreEqual(Equipped.Slot.RING1, item.ItemSlot);
            Assert.AreSame(effect, item.ItemEffect);
            Assert.AreEqual(0.5m, item.Weight);
            Assert.AreEqual(1, item.NewStats.Count);
            Assert.AreEqual("ring_stat", item.NewStats[0].Name);
            Assert.AreEqual(1, item.NewFlags.Count);
            Assert.AreEqual("ring_flag", item.NewFlags[0].Name);
        }

        [TestMethod]
        public void Item_CanonicalConstructor_NullCollections_InitializesNonNullEmptyLists() {
            var effect = new Effect("Cloak");
            var item = new Item(Equipped.Slot.SHOULDERS, effect);

            Assert.AreEqual(Equipped.Slot.SHOULDERS, item.ItemSlot);
            Assert.AreSame(effect, item.ItemEffect);
            Assert.AreEqual(0m, item.Weight);
            Assert.IsNotNull(item.NewStats);
            Assert.AreEqual(0, item.NewStats.Count);
            Assert.IsNotNull(item.NewFlags);
            Assert.AreEqual(0, item.NewFlags.Count);
        }

        [TestMethod]
        public void Weapon_CanonicalConstructor_InitializesFieldsAndCollections() {
            var atkMode = new AttackMode {
                ProficencyCategory = Weapon.Proficiency.MARTIAL,
                DamageDieMedium = (1, 8),
                DamageType = "slashing",
                ThreatRange = 19,
                CriticalMultiplier = 2
            };
            var weapon = new Weapon(
                "Longsword",
                4.0m,
                Weapon.Effort.ONE_HANDED,
                0,
                new[] { "blade", "slashing" },
                melee: (atkMode, false));

            Assert.AreEqual("Longsword", weapon.Name);
            Assert.AreEqual(4.0m, weapon.Weight);
            Assert.AreEqual(Weapon.Effort.ONE_HANDED, weapon.EffortCategory);
            Assert.AreEqual(0, weapon.SizeCategory);
            CollectionAssert.AreEqual(new[] { "blade", "slashing" }, weapon.Tags);
            Assert.IsNotNull(weapon.Melee);
            Assert.AreSame(atkMode, weapon.Melee.Value.atk);
            Assert.IsFalse(weapon.Melee.Value.reach);
            Assert.IsNull(weapon.Thrown);
            Assert.IsNull(weapon.Projectile);
            Assert.IsNull(weapon.SecondHead);
        }

        [TestMethod]
        public void Weapon_CanonicalConstructor_NullCollections_InitializesNonNullEmptyTags() {
            var weapon = new Weapon("Dagger");

            Assert.AreEqual("Dagger", weapon.Name);
            Assert.AreEqual(0m, weapon.Weight);
            Assert.AreEqual(Weapon.Effort.LIGHT, weapon.EffortCategory);
            Assert.IsNotNull(weapon.Tags);
            Assert.AreEqual(0, weapon.Tags.Count);
        }

        [TestMethod]
        public void ArmorProperty_CanonicalConstructor_InitializesProperties() {
            var effect = new Effect("Shadow");
            var acMod = new Modifier(2, "armor");
            var maxDexMod = new Modifier(1, "dex");
            var acpMod = new Modifier(2, "acp");
            var asfMod = new Modifier(-5, "asf");

            var prop = new ArmorProperty("Shadow", effect, acMod, maxDexMod, acpMod, asfMod);

            Assert.AreEqual("Shadow", prop.Name);
            Assert.AreSame(effect, prop.GlobalEffect);
            Assert.AreSame(acMod, prop.AcModifier);
            Assert.AreSame(maxDexMod, prop.MaxDexModifier);
            Assert.AreSame(acpMod, prop.AcpModifier);
            Assert.AreSame(asfMod, prop.AsfModifier);
        }

        [TestMethod]
        public void CharacterClass_CanonicalConstructor_InitializesFieldsAndCollections() {
            var feature = new Effect("Sneak Attack");
            var flag = new FlagDefinition("trapfinding");
            var stat = new Statistic("rogue_stat");
            var matcher = new AttackMatcher();

            var cls = new CharacterClass(
                name: "Rogue",
                prerequisites: "None",
                hitDie: 6,
                skillPoints: 8,
                baseAttackBonus: CharacterClass.BonusProgression.MEDIUM,
                baseFortSave: CharacterClass.BonusProgression.SLOW,
                baseRefSave: CharacterClass.BonusProgression.FAST,
                baseWillSave: CharacterClass.BonusProgression.SLOW,
                classSkills: new[] { "Hide", "Move Silently" },
                weaponProficiencies: new[] { matcher },
                armorProficiencies: new[] { Armor.WeightCategory.LIGHT },
                shieldProficiencies: new[] { false },
                exoticShieldProficiencies: new[] { "extreme shield" },
                classFeatures: new[] { new[] { feature } },
                flags: new[] { new[] { flag } },
                newStats: new[] { stat },
                tags: new[] { "expert", "stealth" },
                bonusFeats: new Dictionary<string, int> { ["Weapon Finesse"] = 1 },
                subsystems: new Dictionary<string, SubsystemFeature> {
                    ["martial"] = new SubsystemFeature { Stats = new List<string> { "maneuvers_readied" } }
                });

            Assert.AreEqual("Rogue", cls.Name);
            Assert.AreEqual("None", cls.Prerequisites);
            Assert.AreEqual(6, cls.HitDie);
            Assert.AreEqual(8, cls.SkillPoints);
            Assert.AreEqual(CharacterClass.BonusProgression.MEDIUM, cls.BaseAttackBonus);
            Assert.AreEqual(CharacterClass.BonusProgression.FAST, cls.BaseRefSave);
            CollectionAssert.AreEqual(new[] { "Hide", "Move Silently" }, cls.ClassSkills);
            Assert.AreEqual(1, cls.WeaponProficiencies.Count);
            Assert.AreEqual(1, cls.ArmorProficiencies.Count);
            Assert.AreEqual(1, cls.ShieldProficiencies.Count);
            CollectionAssert.AreEqual(new[] { "extreme shield" }, cls.ExoticShieldProficiencies);
            Assert.AreEqual(1, cls.ClassFeatures.Count);
            Assert.AreEqual(1, cls.ClassFeatures[0].Count);
            Assert.AreEqual(1, cls.Flags.Count);
            Assert.AreEqual(1, cls.Flags[0].Count);
            Assert.AreEqual(1, cls.NewStats.Count);
            CollectionAssert.AreEqual(new[] { "expert", "stealth" }, cls.Tags);
            Assert.AreEqual(1, cls.BonusFeats["Weapon Finesse"]);
            Assert.IsTrue(cls.Subsystems.ContainsKey("martial"));
        }

        [TestMethod]
        public void CharacterClass_CanonicalConstructor_NullCollections_InitializesNonNullEmptyCollections() {
            var cls = new CharacterClass("Fighter");

            Assert.AreEqual("Fighter", cls.Name);
            Assert.IsNotNull(cls.ClassSkills);
            Assert.AreEqual(0, cls.ClassSkills.Count);
            Assert.IsNotNull(cls.WeaponProficiencies);
            Assert.AreEqual(0, cls.WeaponProficiencies.Count);
            Assert.IsNotNull(cls.ArmorProficiencies);
            Assert.AreEqual(0, cls.ArmorProficiencies.Count);
            Assert.IsNotNull(cls.ShieldProficiencies);
            Assert.AreEqual(0, cls.ShieldProficiencies.Count);
            Assert.IsNotNull(cls.ExoticShieldProficiencies);
            Assert.AreEqual(0, cls.ExoticShieldProficiencies.Count);
            Assert.IsNotNull(cls.ClassFeatures);
            Assert.AreEqual(0, cls.ClassFeatures.Count);
            Assert.IsNotNull(cls.Flags);
            Assert.AreEqual(0, cls.Flags.Count);
            Assert.IsNotNull(cls.NewStats);
            Assert.AreEqual(0, cls.NewStats.Count);
            Assert.IsNotNull(cls.Tags);
            Assert.AreEqual(0, cls.Tags.Count);
            Assert.IsNotNull(cls.BonusFeats);
            Assert.AreEqual(0, cls.BonusFeats.Count);
            Assert.IsNotNull(cls.Subsystems);
            Assert.AreEqual(0, cls.Subsystems.Count);
        }

        [TestMethod]
        public void Attack_CanonicalConstructor_InitializesCombatAndDamagePropertiesWithoutReflection() {
            var baseDmg = new DiceGroup("base").AddDice(1, 8);
            var bonusDie = new DiceGroup("fire").AddDice(1, 6);
            var critBonusDie = new DiceGroup("crit_burst").AddDice(1, 10);

            var attack = new Attack(
                weaponName: "Flaming Longsword",
                attackBonus: (8, new List<string> { "+5 BAB", "+3 STR" }),
                baseDamage: baseDmg,
                bonusDice: new[] { bonusDie },
                criticalBonusDice: new[] { critBonusDie },
                criticalMultiplier: 3,
                threatRange: 19,
                rangeIncrement: 10,
                damageExplanation: new[] { "1d8 base", "+3 STR", "+1d6 fire" },
                criticalDamageExplanation: new[] { "1d8 base ×3", "+3 STR ×3", "+1d10 burst" },
                weaponTags: new[] { "melee", "slashing", "fire" },
                weaponSizeCategory: 0,
                proficiency: Weapon.Proficiency.MARTIAL,
                effort: Weapon.Effort.ONE_HANDED,
                hand: Weapon.Hand.ONE_HANDED,
                range: Weapon.Range.MELEE,
                strengthRating: -1);

            Assert.AreEqual("Flaming Longsword", attack.WeaponName);
            Assert.AreEqual(8, attack.AttackBonus.bonus);
            CollectionAssert.AreEqual(new[] { "+5 BAB", "+3 STR" }, attack.AttackBonus.explanation);
            Assert.AreSame(baseDmg, attack.BaseDamage);
            Assert.AreEqual(1, attack.BonusDice.Count);
            Assert.AreSame(bonusDie, attack.BonusDice[0]);
            Assert.AreEqual(1, attack.CriticalBonusDice.Count);
            Assert.AreSame(critBonusDie, attack.CriticalBonusDice[0]);
            Assert.AreEqual(3, attack.CriticalMultiplier);
            Assert.AreEqual(19, attack.ThreatRange);
            Assert.AreEqual(10, attack.RangeIncrement);
            CollectionAssert.AreEqual(new[] { "1d8 base", "+3 STR", "+1d6 fire" }, attack.DamageExplanation);
            CollectionAssert.AreEqual(new[] { "1d8 base ×3", "+3 STR ×3", "+1d10 burst" }, attack.CriticalDamageExplanation);
            CollectionAssert.AreEqual(new[] { "melee", "slashing", "fire" }, attack.WeaponTags);
            Assert.AreEqual(0, attack.WeaponSizeCategory);
            Assert.AreEqual(Weapon.Proficiency.MARTIAL, attack.Proficiency);
            Assert.AreEqual(Weapon.Effort.ONE_HANDED, attack.Effort);
            Assert.AreEqual(Weapon.Hand.ONE_HANDED, attack.Hand);
            Assert.AreEqual(Weapon.Range.MELEE, attack.Range);
            Assert.AreEqual(-1, attack.StrengthRating);

            // Computed damage groups
            Assert.IsNotNull(attack.TotalDamage);
            Assert.IsNotNull(attack.CriticalBaseDamage);
            Assert.IsNotNull(attack.CriticalTotalDamage);
            Assert.IsNotNull(attack.Damage);
            Assert.IsNotNull(attack.CriticalDamage);
        }

        [TestMethod]
        public void Attack_CanonicalConstructor_IntBonusOverload_SetsAttackBonusAndDefaults() {
            var baseDmg = new DiceGroup("base").AddDice(2, 6);
            var attack = new Attack("Greatsword", 6, baseDmg);

            Assert.AreEqual("Greatsword", attack.WeaponName);
            Assert.AreEqual(6, attack.AttackBonus.bonus);
            CollectionAssert.AreEqual(new[] { "+6 base attack" }, attack.AttackBonus.explanation);
            Assert.AreSame(baseDmg, attack.BaseDamage);
            Assert.IsNotNull(attack.BonusDice);
            Assert.AreEqual(0, attack.BonusDice.Count);
            Assert.IsNotNull(attack.CriticalBonusDice);
            Assert.AreEqual(0, attack.CriticalBonusDice.Count);
            Assert.AreEqual(2, attack.CriticalMultiplier);
            Assert.AreEqual(20, attack.ThreatRange);
            Assert.AreEqual(0, attack.RangeIncrement);
            Assert.IsNotNull(attack.WeaponTags);
            Assert.AreEqual(0, attack.WeaponTags.Count);
            Assert.AreEqual(1, attack.DamageExplanation.Count);
            Assert.AreEqual(1, attack.CriticalDamageExplanation.Count);

            var iterative = attack.WithIterativePenalty(-5);
            Assert.AreEqual(1, iterative.AttackBonus.bonus);
            Assert.IsTrue(iterative.AttackBonus.explanation.Any(x => x.Contains("iterative attack")));
        }

        [TestMethod]
        public void Modifier_CanonicalConstructor_InitializesTypeAndAmounts() {
            var amt1 = new FixedAmount(3);
            var amt2 = new VariableAmount("STR");

            var mod = new Modifier("morale", new IAmount[] { amt1, amt2 }, "Heroism");

            Assert.AreEqual("morale", mod.Type);
            Assert.AreEqual("Heroism", mod.Name);
        }

        [TestMethod]
        public void Modifier_CanonicalConstructor_NullAmounts_InitializesEmpty() {
            var mod = new Modifier("enhancement");

            Assert.AreEqual("enhancement", mod.Type);
            Assert.AreEqual("", mod.Name);
        }

        [TestMethod]
        public void LookupAmount_CanonicalConstructor_EvaluatesCorrectly() {
            var value = new FixedAmount(5);
            var table = new Dictionary<int, int> {
                [3] = 10,
                [5] = 20,
                [7] = 30
            };

            var lookup = new LookupAmount(value, table);
            var result = lookup.Get(new EffectPool(new Character()), new Equipped());

            Assert.AreEqual(20, result);
        }

        [TestMethod]
        public void FlagDefinition_CanonicalConstructor_InitializesProperties() {
            var resource = new ActionResource(new JObject { ["action"] = "Turn Undead", ["recharge"] = "DAY", ["uses"] = 3 });
            var flag = new FlagDefinition("Divine Might", "Combat", "Spend turn attempt to gain CHA to damage", AlwaysOn.Instance, resource);

            Assert.AreEqual("Divine Might", flag.Name);
            Assert.AreEqual("Combat", flag.Category);
            Assert.AreEqual("Spend turn attempt to gain CHA to damage", flag.Description);
            Assert.AreSame(AlwaysOn.Instance, flag.Show);
            Assert.AreSame(resource, flag.Resource);
        }

        [TestMethod]
        public void FlagDefinition_CanonicalConstructor_Defaults_AreApplied() {
            var flag = new FlagDefinition();

            Assert.AreEqual("", flag.Name);
            Assert.AreEqual("", flag.Category);
            Assert.AreEqual("", flag.Description);
            Assert.AreSame(AlwaysOn.Instance, flag.Show);
            Assert.IsNull(flag.Resource);
        }

        [TestMethod]
        public void ControllableFlag_CanonicalConstructor_DoesNotSerializeToJsonAndSyncsWithCharacter() {
            var character = new Character();
            var def = new FlagDefinition("Power Attack", "Combat", "Trade attack bonus for damage");

            var flag = new ControllableFlag(def, character);
            Assert.AreEqual("Power Attack", flag.Name);
            Assert.AreEqual("Combat", flag.Category);
            Assert.IsFalse(flag.On);

            flag.On = true;
            Assert.IsTrue(character.flagValues["Power Attack"]);

            var flag2 = new ControllableFlag("Power Attack", character, on: true);
            Assert.IsTrue(flag2.On);
        }

        [TestMethod]
        public void SubsystemStep_CanonicalConstructor_InitializesAllProperties() {
            var action = new SheetAction();
            var step = new SubsystemStep(
                type: "transition",
                slotGroup: "maneuvers",
                selector: "invoked",
                fromState: "readied",
                toState: "expended",
                countExpression: "1",
                onEmptyVerb: "recover",
                verb: "initiate",
                action: action,
                pool: "grit",
                op: "consume",
                amount: 2);

            Assert.AreEqual("transition", step.Type);
            Assert.AreEqual("maneuvers", step.SlotGroup);
            Assert.AreEqual("invoked", step.Selector);
            Assert.AreEqual("readied", step.FromState);
            Assert.AreEqual("expended", step.ToState);
            Assert.AreEqual("1", step.CountExpression);
            Assert.AreEqual("recover", step.OnEmptyVerb);
            Assert.AreEqual("initiate", step.Verb);
            Assert.AreSame(action, step.Action);
            Assert.AreEqual("grit", step.Pool);
            Assert.AreEqual("consume", step.Op);
            Assert.AreEqual(2, step.Amount);
        }

        [TestMethod]
        public void MaxLevelDefinition_CanonicalConstructor_InitializesProperties() {
            var maxLvl = new MaxLevelDefinition("initiator_level", 1);

            Assert.AreEqual("initiator_level", maxLvl.Stat);
            Assert.AreEqual(1, maxLvl.Base);
        }

        [TestMethod]
        public void LevelGatingDefinition_CanonicalConstructor_InitializesProperties() {
            var lg = new LevelGatingDefinition("grade", "Max Inscription Grade");

            Assert.AreEqual("grade", lg.ElementStatistic);
            Assert.AreEqual("Max Inscription Grade", lg.MaxLevelLabel);
        }

        [TestMethod]
        public void StateCapacityDefinition_CanonicalConstructor_InitializesProperties() {
            var sc = new StateCapacityDefinition(new[] { "readied", "granted" }, "capacity_stat", "Readied Maneuvers");

            CollectionAssert.AreEqual(new[] { "readied", "granted" }, sc.States.ToList());
            Assert.AreEqual("capacity_stat", sc.Stat);
            Assert.AreEqual("Readied Maneuvers", sc.Label);
        }

        [TestMethod]
        public void StateCapacityDefinition_CanonicalConstructor_NullStates_InitializesEmpty() {
            var sc = new StateCapacityDefinition(null, "stat", "label");

            Assert.IsNotNull(sc.States);
            Assert.AreEqual(0, sc.States.Count);
        }

        [TestMethod]
        public void SlotGroupDefinition_CanonicalConstructor_InitializesFieldsAndCollections() {
            var sc = new StateCapacityDefinition(new[] { "prepared" }, "prep_stat", "Prepared");
            var maxLvl = new MaxLevelDefinition("level", 0);
            var eff = new Effect("Slot Group Effect");

            var sg = new SlotGroupDefinition(
                name: "spells",
                filter: "arcane",
                capacityStat: "max_spells",
                stateCapacities: new[] { sc },
                fixedCapacity: 4,
                maxLevel: maxLvl,
                effects: new[] { eff },
                states: new[] { "unprepared", "prepared", "cast" },
                defaultState: "unprepared");

            Assert.AreEqual("spells", sg.Name);
            Assert.AreEqual("arcane", sg.Filter);
            Assert.AreEqual("max_spells", sg.CapacityStat);
            Assert.AreEqual(1, sg.StateCapacities.Count);
            Assert.AreEqual(4, sg.FixedCapacity);
            Assert.AreSame(maxLvl, sg.MaxLevel);
            Assert.AreEqual(1, sg.Effects.Count);
            CollectionAssert.AreEqual(new[] { "unprepared", "prepared", "cast" }, sg.States);
            Assert.AreEqual("unprepared", sg.DefaultState);
        }

        [TestMethod]
        public void SlotGroupDefinition_CanonicalConstructor_NullCollections_InitializesNonNullEmpty() {
            var sg = new SlotGroupDefinition("talents");

            Assert.AreEqual("talents", sg.Name);
            Assert.IsNotNull(sg.StateCapacities);
            Assert.AreEqual(0, sg.StateCapacities.Count);
            Assert.IsNotNull(sg.Effects);
            Assert.AreEqual(0, sg.Effects.Count);
            Assert.IsNotNull(sg.States);
            Assert.AreEqual(0, sg.States.Count);
        }

        [TestMethod]
        public void PoolDefinition_CanonicalConstructor_InitializesProperties() {
            var pool = new PoolDefinition("ki", "Ki Pool", "ki_max", 5);

            Assert.AreEqual("ki", pool.Id);
            Assert.AreEqual("Ki Pool", pool.Name);
            Assert.AreEqual("ki_max", pool.CapacityStat);
            Assert.AreEqual(5, pool.DefaultCapacity);
        }

        [TestMethod]
        public void EffectProjectionRule_CanonicalConstructor_InitializesProperties() {
            var rule = new EffectProjectionRule("maneuvers", "readied", new[] { "iron_heart_surge", "white_raven_tactics" });

            Assert.AreEqual("maneuvers", rule.SlotGroup);
            Assert.AreEqual("readied", rule.State);
            CollectionAssert.AreEqual(new[] { "iron_heart_surge", "white_raven_tactics" }, rule.Effects);
        }

        [TestMethod]
        public void EffectProjectionRule_CanonicalConstructor_NullEffects_InitializesEmpty() {
            var rule = new EffectProjectionRule("maneuvers", "expended");

            Assert.IsNotNull(rule.Effects);
            Assert.AreEqual(0, rule.Effects.Count);
        }

        [TestMethod]
        public void TaxonomyEntry_CanonicalConstructor_InitializesProperties() {
            var props = new Dictionary<string, object> {
                ["discipline"] = "Diamond Mind",
                ["level"] = 3
            };
            var entry = new TaxonomyEntry("Ruby Nightmare Blade", props);

            Assert.AreEqual("Ruby Nightmare Blade", entry.Name);
            Assert.AreEqual("Diamond Mind", entry.GetProperty<string>("discipline"));
            Assert.AreEqual(3, entry.GetProperty<int>("level"));
        }

        [TestMethod]
        public void SubsystemDefinition_CanonicalConstructor_InitializesAllSubsystemComponents() {
            var lg = new LevelGatingDefinition("grade", "Max Grade");
            var sg = new SlotGroupDefinition("infusions");
            var step = new SubsystemStep("recover");
            var pool = new PoolDefinition("points");
            var proj = new EffectProjectionRule("infusions", "active", new[] { "eff1" });
            var elem = new SubsystemElement();
            elem.Name = "TestInfusion";
            elem.SetProperty("grade", 1);
            elem.SetProperty("school", "Transmutation");
            var action = new SheetAction();

            var taxEntry = new TaxonomyEntry("Transmutation");
            var taxMap = new Dictionary<string, IDictionary<string, TaxonomyEntry>> {
                ["school"] = new Dictionary<string, TaxonomyEntry> { ["Transmutation"] = taxEntry }
            };

            var subDef = new SubsystemDefinition(
                id: "artificer",
                name: "Infusions",
                prerequisiteUnit: "class",
                prerequisiteProperty: "Artificer",
                levelGating: lg,
                slotGroups: new Dictionary<string, SlotGroupDefinition> { ["infusions"] = sg },
                verbs: new Dictionary<string, List<SubsystemStep>> { ["recover"] = new List<SubsystemStep> { step } },
                defaultElementVerbs: new Dictionary<string, List<SubsystemStep>> { ["imbue"] = new List<SubsystemStep> { step } },
                pools: new Dictionary<string, PoolDefinition> { ["points"] = pool },
                effectTemplates: new Dictionary<string, JObject> { ["tmpl"] = new JObject { ["key"] = "val" } },
                projections: new[] { proj },
                requiredElementProperties: new[] { "school" },
                elementStatistics: new Dictionary<string, List<string>> { ["grade"] = new List<string> { "infusion_grade" } },
                taxonomies: taxMap,
                elements: new[] { elem },
                actions: new[] { action });

            Assert.AreEqual("artificer", subDef.Id);
            Assert.AreEqual("Infusions", subDef.Name);
            Assert.AreEqual("class", subDef.PrerequisiteUnit);
            Assert.AreEqual("Artificer", subDef.PrerequisiteProperty);
            Assert.AreSame(lg, subDef.LevelGating);
            Assert.IsTrue(subDef.SlotGroups.ContainsKey("infusions"));
            Assert.IsTrue(subDef.Verbs.ContainsKey("recover"));
            Assert.IsTrue(subDef.DefaultElementVerbs.ContainsKey("imbue"));
            Assert.IsTrue(subDef.Pools.ContainsKey("points"));
            Assert.IsTrue(subDef.EffectTemplates.ContainsKey("tmpl"));
            Assert.AreEqual(1, subDef.Projections.Count);
            CollectionAssert.AreEqual(new[] { "school" }, subDef.RequiredElementProperties);
            Assert.IsTrue(subDef.ElementStatistics.ContainsKey("grade"));
            Assert.IsTrue(subDef.Taxonomies.ContainsKey("school"));
            Assert.IsTrue(subDef.Elements.ContainsKey("TestInfusion"));
            Assert.AreEqual(1, subDef.Actions.Count);
        }

        [TestMethod]
        public void SubsystemDefinition_CanonicalConstructor_NullCollections_InitializesNonNullEmpty() {
            var subDef = new SubsystemDefinition("simple_subsystem");

            Assert.AreEqual("simple_subsystem", subDef.Id);
            Assert.AreEqual("simple_subsystem", subDef.Name);
            Assert.IsNotNull(subDef.SlotGroups);
            Assert.IsNotNull(subDef.Verbs);
            Assert.IsNotNull(subDef.DefaultElementVerbs);
            Assert.IsNotNull(subDef.Pools);
            Assert.IsNotNull(subDef.EffectTemplates);
            Assert.IsNotNull(subDef.Projections);
            Assert.IsNotNull(subDef.RequiredElementProperties);
            Assert.IsNotNull(subDef.ElementStatistics);
            Assert.IsNotNull(subDef.Taxonomies);
            Assert.IsNotNull(subDef.Elements);
            Assert.IsNotNull(subDef.Actions);
        }

        [TestMethod]
        public void JsonConstructorDelegation_ProducesEquivalentObjects() {
            // 1. Item
            var itemJson = new JObject {
                ["slot"] = "RING1",
                ["effect"] = new JObject { ["name"] = "Ring of Feather Falling" },
                ["weight"] = 0.1m,
                ["stats"] = new JArray(new JObject { ["name"] = "ring_stat" }),
                ["flags"] = new JArray(new JObject { ["name"] = "ring_flag" })
            };
            var item = new Item(itemJson);
            Assert.AreEqual(Equipped.Slot.RING1, item.ItemSlot);
            Assert.AreEqual("Ring of Feather Falling", item.ItemEffect.Name);
            Assert.AreEqual(0.1m, item.Weight);
            Assert.AreEqual(1, item.NewStats.Count);
            Assert.AreEqual(1, item.NewFlags.Count);

            // 2. Weapon
            var weaponJson = new JObject {
                ["name"] = "Scimitar",
                ["weight"] = 4m,
                ["effort"] = "ONE_HANDED",
                ["melee"] = new JObject {
                    ["proficiency"] = "MARTIAL",
                    ["damage"] = "1d6",
                    ["type"] = "slashing",
                    ["threat"] = 18,
                    ["crit"] = 2
                }
            };
            var weapon = new Weapon(weaponJson);
            Assert.AreEqual("Scimitar", weapon.Name);
            Assert.AreEqual(Weapon.Effort.ONE_HANDED, weapon.EffortCategory);
            Assert.IsNotNull(weapon.Melee);
            Assert.AreEqual(18, weapon.Melee.Value.atk.ThreatRange);

            // 3. ArmorProperty
            var propJson = new JObject {
                ["name"] = "Fortification",
                ["effect"] = new JObject { ["name"] = "Crit Immunity" }
            };
            var prop = new ArmorProperty(propJson);
            Assert.AreEqual("Fortification", prop.Name);
            Assert.IsNotNull(prop.GlobalEffect);

            // 4. CharacterClass
            var classJson = new JObject {
                ["name"] = "Wizard",
                ["prerequisites"] = "",
                ["hit die"] = 4,
                ["skill points"] = 2,
                ["base attack bonus"] = "SLOW",
                ["base fort save"] = "SLOW",
                ["base ref save"] = "SLOW",
                ["base will save"] = "FAST",
                ["class skills"] = new JArray("Spellcraft", "Knowledge"),
                ["weapon proficiencies"] = new JArray(),
                ["armor proficiencies"] = new JArray(),
                ["shield proficiencies"] = new JArray(),
                ["class features"] = new JArray()
            };
            var cls = new CharacterClass(classJson);
            Assert.AreEqual("Wizard", cls.Name);
            Assert.AreEqual(2, cls.ClassSkills.Count);
            Assert.AreEqual(CharacterClass.BonusProgression.FAST, cls.BaseWillSave);

            // 5. Modifier
            var modJson = new JObject {
                ["type"] = "enhancement",
                ["amounts"] = new JArray(new JObject { ["fixed"] = 2 })
            };
            var mod = new Modifier(modJson);
            Assert.AreEqual("enhancement", mod.Type);
            Assert.AreEqual(2, mod.ComputeAmount(new EffectPool(new Character()), new Equipped()));

            // 6. LookupAmount
            var lookupJson = new JObject {
                ["value"] = new JObject { ["fixed"] = 10 },
                ["table"] = new JObject { ["10"] = 50 }
            };
            var lookup = new LookupAmount(lookupJson);
            Assert.AreEqual(50, lookup.Get(new EffectPool(new Character()), new Equipped()));

            // 7. FlagDefinition & ControllableFlag
            var flagJson = new JObject {
                ["name"] = "Power Attack",
                ["category"] = "Combat"
            };
            var flagDef = new FlagDefinition(flagJson);
            Assert.AreEqual("Power Attack", flagDef.Name);
            Assert.AreEqual("Combat", flagDef.Category);

            var charCtx = new Character();
            var ctrlFlag = new ControllableFlag(flagJson, charCtx);
            Assert.AreEqual("Power Attack", ctrlFlag.Name);
            Assert.IsFalse(ctrlFlag.On);

            // 8. Subsystem models
            var stepJson = new JObject {
                ["type"] = "transition",
                ["slot_group"] = "spells",
                ["to"] = "cast"
            };
            var step = new SubsystemStep(stepJson);
            Assert.AreEqual("transition", step.Type);
            Assert.AreEqual("cast", step.ToState);

            var maxLvlJson = new JObject { ["stat"] = "caster_level", ["base"] = 0 };
            var maxLvl = new MaxLevelDefinition(maxLvlJson);
            Assert.AreEqual("caster_level", maxLvl.Stat);

            var lgJson = new JObject { ["element_statistic"] = "level" };
            var lg = new LevelGatingDefinition(lgJson);
            Assert.AreEqual("level", lg.ElementStatistic);

            var scJson = new JObject { ["states"] = new JArray("prepared"), ["stat"] = "slots", ["label"] = "Slots" };
            var sc = new StateCapacityDefinition(scJson);
            Assert.AreEqual(1, sc.States.Count);
            Assert.AreEqual("slots", sc.Stat);

            var sgJson = new JObject { ["name"] = "prepared_spells" };
            var sg = new SlotGroupDefinition(sgJson);
            Assert.AreEqual("prepared_spells", sg.Name);

            var poolJson = new JObject { ["id"] = "mana", ["name"] = "Mana" };
            var pool = new PoolDefinition(poolJson);
            Assert.AreEqual("mana", pool.Id);

            var projJson = new JObject { ["slot_group"] = "spells", ["state"] = "prepared", ["effects"] = new JArray("effect1") };
            var proj = new EffectProjectionRule(projJson);
            Assert.AreEqual(1, proj.Effects.Count);

            var taxJson = new JObject { ["school"] = "Evocation" };
            var tax = new TaxonomyEntry("Fireball", taxJson);
            Assert.AreEqual("Evocation", tax.GetProperty<string>("school"));

            var subJson = new JObject {
                ["subsystem"] = new JObject {
                    ["id"] = "spellcasting",
                    ["name"] = "Spellcasting"
                }
            };
            var sub = new SubsystemDefinition(subJson);
            Assert.AreEqual("spellcasting", sub.Id);
            Assert.AreEqual("Spellcasting", sub.Name);
        }
    }
}
