using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;

namespace PlayMore35.CharacterChoices {
    public interface IReadOnlyCharacter {
        string name { get; }
        string gender { get; }
        string alignment { get; }
        string race { get; }
        IReadOnlyDictionary<string, int> assignedAbilityScores { get; }
        IReadOnlyList<string> bonusLanguages { get; }
        IReadOnlyList<string> classLevels { get; }
        IReadOnlyDictionary<int, string> abilityScoreIncrements { get; }
        IReadOnlyList<int> rolledHitDice { get; }
        IReadOnlyDictionary<string, List<int>> skillRanks { get; }
        IReadOnlyDictionary<string, string> feats { get; }
        IReadOnlyDictionary<string, string> featChoices { get; }
        IReadOnlyList<GpTransaction> transactions { get; }
        IReadOnlyDictionary<string, InventoryLocation> inventory { get; }
        EquippedChoices equipped { get; }
        IReadOnlyList<XpSessionEntry> sessions { get; }
        IReadOnlyList<string> externalEffects { get; }
        IReadOnlyDictionary<string, ISubsystemState> Subsystems { get; }
        IReadOnlyDictionary<string, bool> flagValues { get; }
        IReadOnlyDictionary<string, int> paramValues { get; }
        Timeline Timeline { get; }
        AttackRoutinePreferences RoutinePreferences { get; }

        int ExperienceToNextLevel();
        int TotalExperience();
        decimal TotalGold();
        Dictionary<string, int> SummarizeClassLevels();
        List<(string, int)> DetailClassLevels();
        JToken ToJson();
    }
}
