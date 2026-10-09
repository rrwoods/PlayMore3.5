using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;

namespace PlayMore35.CharacterChoices {
    public class ReadOnlyCharacter : IReadOnlyCharacter {
        private readonly Character character;

        public ReadOnlyCharacter(Character character) {
            this.character = character ?? throw new ArgumentNullException(nameof(character));
        }

        public string name => character.name;
        public string gender => character.gender;
        public string alignment => character.alignment;
        public string race => character.race;
        public IReadOnlyDictionary<string, int> assignedAbilityScores => character.assignedAbilityScores;
        public IReadOnlyList<string> bonusLanguages => character.bonusLanguages;
        public IReadOnlyList<string> classLevels => character.classLevels;
        public IReadOnlyDictionary<int, string> abilityScoreIncrements => character.abilityScoreIncrements;
        public IReadOnlyList<int> rolledHitDice => character.rolledHitDice;
        public IReadOnlyDictionary<string, List<int>> skillRanks => character.skillRanks;
        public IReadOnlyDictionary<string, string> feats => character.feats;
        public IReadOnlyDictionary<string, string> featChoices => character.featChoices;
        public IReadOnlyList<GpTransaction> transactions => character.transactions;
        public IReadOnlyDictionary<string, InventoryLocation> inventory => character.inventory;
        public EquippedChoices equipped => character.equipped;
        public IReadOnlyList<XpSessionEntry> sessions => character.sessions;
        public IReadOnlyList<string> externalEffects => character.externalEffects;
        public IReadOnlyDictionary<string, ISubsystemState> Subsystems => character.Subsystems;
        public IReadOnlyDictionary<string, bool> flagValues => character.flagValues;
        public IReadOnlyDictionary<string, int> paramValues => character.paramValues;
        public Timeline Timeline => character.Timeline;
        public AttackRoutinePreferences RoutinePreferences => character.RoutinePreferences;

        public int ExperienceToNextLevel() => character.ExperienceToNextLevel();
        public int TotalExperience() => character.TotalExperience();
        public decimal TotalGold() => character.TotalGold();
        public Dictionary<string, int> SummarizeClassLevels() => character.SummarizeClassLevels();
        public List<(string, int)> DetailClassLevels() => character.DetailClassLevels();
        public JToken ToJson() => character.ToJson();
    }
}
