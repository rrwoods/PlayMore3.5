using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Items;
using PlayMore35.Subsystems;

namespace PlayMore35.Tests {
    [TestClass]
    public class NonNullCollectionInvariantTests {
        [TestMethod]
        public void Character_ParameterlessConstructor_InitializesAllCollectionsToNonNull() {
            var character = new Character();

            Assert.IsNotNull(character.assignedAbilityScores);
            Assert.IsNotNull(character.bonusLanguages);
            Assert.IsNotNull(character.classLevels);
            Assert.IsNotNull(character.abilityScoreIncrements);
            Assert.IsNotNull(character.rolledHitDice);
            Assert.IsNotNull(character.skillRanks);
            Assert.IsNotNull(character.feats);
            Assert.IsNotNull(character.featChoices);
            Assert.IsNotNull(character.transactions);
            Assert.IsNotNull(character.inventory);
            Assert.IsNotNull(character.sessions);
            Assert.IsNotNull(character.externalEffects);
            Assert.IsNotNull(character.Subsystems);
            Assert.IsNotNull(character.flagValues);
            Assert.IsNotNull(character.paramValues);
        }

        [TestMethod]
        public void Character_JsonConstructor_WithEmptyOrMissingProperties_InitializesAllCollectionsToNonNull() {
            var emptyJson = new JObject();
            var character = new Character(emptyJson);

            Assert.IsNotNull(character.assignedAbilityScores);
            Assert.IsNotNull(character.bonusLanguages);
            Assert.IsNotNull(character.classLevels);
            Assert.IsNotNull(character.abilityScoreIncrements);
            Assert.IsNotNull(character.rolledHitDice);
            Assert.IsNotNull(character.skillRanks);
            Assert.IsNotNull(character.feats);
            Assert.IsNotNull(character.featChoices);
            Assert.IsNotNull(character.transactions);
            Assert.IsNotNull(character.inventory);
            Assert.IsNotNull(character.sessions);
            Assert.IsNotNull(character.externalEffects);
            Assert.IsNotNull(character.Subsystems);
            Assert.IsNotNull(character.flagValues);
            Assert.IsNotNull(character.paramValues);
        }

        [TestMethod]
        public void SubsystemState_Constructors_EnsureSlotsAndPoolsAreNonNull() {
            var emptyState = new SubsystemState();
            Assert.IsNotNull(emptyState.Slots);
            Assert.IsNotNull(emptyState.Pools);

            var jsonState = new SubsystemState(new JObject());
            Assert.IsNotNull(jsonState.Slots);
            Assert.IsNotNull(jsonState.Pools);
        }

        [TestMethod]
        public void Feat_Constructors_EnsureCollectionsAreNonNull() {
            var jsonFeat = new Feat(new JObject {
                ["benefit"] = new JObject { ["name"] = "Power Attack" }
            });
            Assert.IsNotNull(jsonFeat.Descriptors);
            Assert.IsNotNull(jsonFeat.Choices);
            Assert.IsNotNull(jsonFeat.ClassSkills);
            Assert.IsNotNull(jsonFeat.Flags);
            Assert.IsNotNull(jsonFeat.Parameters);
            Assert.IsNotNull(jsonFeat.NewStats);
        }

        [TestMethod]
        public void Item_Constructors_EnsureCollectionsAreNonNull() {
            var jsonItem = new Item(new JObject {
                ["slot"] = "RING1",
                ["effect"] = new JObject { ["name"] = "Ring of Protection" },
                ["weight"] = 0m
            });
            Assert.IsNotNull(jsonItem.NewStats);
            Assert.IsNotNull(jsonItem.NewFlags);
        }

        [TestMethod]
        public void Armor_Constructors_EnsureCollectionsAreNonNull() {
            var jsonArmor = new Armor(new JObject {
                ["name"] = "Leather",
                ["type"] = "LIGHT",
                ["weight"] = 15m,
                ["armor bonus"] = 2,
                ["max dex"] = 6,
                ["armor check penalty"] = 0,
                ["spell failure"] = 10
            });
            Assert.IsNotNull(jsonArmor.Properties);

            var copyArmor = new Armor(jsonArmor);
            Assert.IsNotNull(copyArmor.Properties);
        }

        [TestMethod]
        public void Weapon_Constructors_EnsureCollectionsAreNonNull() {
            var weapon = new Weapon("Dagger", 0, Weapon.Effort.LIGHT);
            Assert.IsNotNull(weapon.Tags);

            var jsonWeapon = new Weapon(new JObject {
                ["name"] = "Dagger",
                ["weight"] = 1m,
                ["effort"] = "LIGHT"
            });
            Assert.IsNotNull(jsonWeapon.Tags);

            var copyWeapon = new Weapon(jsonWeapon);
            Assert.IsNotNull(copyWeapon.Tags);
        }

        [TestMethod]
        public void Shield_Constructors_EnsureCollectionsAreNonNull() {
            var jsonShield = new Shield(new JObject {
                ["name"] = "Buckler",
                ["weight"] = 5m,
                ["shield bonus"] = 1,
                ["tower"] = false,
                ["armor check penalty"] = -1,
                ["spell failure"] = 5
            });
            Assert.IsNotNull(jsonShield.Properties);

            var copyShield = new Shield(jsonShield);
            Assert.IsNotNull(copyShield.Properties);
        }
    }
}
