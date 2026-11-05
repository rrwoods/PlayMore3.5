using System;
using System.Collections.Generic;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems;

namespace PlayMore35.Global {
    public interface ICharacterSession {
        IReadOnlyCharacter Character { get; }
        IReadOnlyDictionary<string, string> RememberedFeats { get; }
        IReadOnlyDictionary<string, string> RememberedFeatChoices { get; }
        Timeline Timeline { get; }
        IRulesCatalog Catalog { get; }
        IEnumerable<SubsystemEngine> Subsystems { get; }
        SubsystemEngine? GetSubsystem(string? id);

        string CreatureType { get; }
        IReadOnlyList<string> SubTypes { get; }
        decimal TotalWeight { get; }
        decimal EquippedWeight { get; }
        int LightLoad { get; }
        int MediumLoad { get; }
        int HeavyLoad { get; }

        bool HasFlag(string flag);
        TimelineRefreshRequirement PerformAction(SheetAction action);

        void Mutate(Action<Character> mutate);
        void Recalculate();

        void EquipWeapon(string from, bool primary, WeaponChoice weaponChoice);
        void DonArmor(string from, ArmorChoice armorChoice);
        void EquipShield(string from, ArmorChoice shieldChoice);
        void EquipItem(string from, Equipped.Slot to, string itemName);
        WeaponChoice? RemoveEquippedWeapon(bool primary);
        ArmorChoice? RemoveDonnedArmor();
        ArmorChoice? RemoveEquippedShield();
        void UnequipWeapon(string to, bool primary);
        void UndonArmor(string to);
        void UnequipShield(string to);
        void UnequipItem(Equipped.Slot from, string to);
        void UnequipSlotlessItem(string to, string item);
        void SwapWeapons();
        void MoveItem(string from, string to, object item, int quantity);
        void AddWeapon(string to, string weaponName, List<string> properties);
        void AddArmor(string to, string armorName, List<string> properties);
        void AddShield(string to, string shieldName, List<string> properties);
        void AddItem(string to, string itemName, int quantity = 1);
        void RemoveThing(string from, object thing);
        int RemoveItem(string from, string itemName, int quantity);
        void RemoveLocation(string location);
        void CountWeight(string locationName, bool weighs);
        decimal WeightAtLocation(string location);
        Equipped.Slot SlotForItem(string itemName);

        void SetFlag(string flagName, bool value);
        void SetParameter(string parameterName, int value);
        void ForceHiddenFlagsOff();

        StatView GetStatistic(string name);
        bool IsStatRelevant(string name);
        IEnumerable<StatView> GetRelevantStats(string tag);
        IEnumerable<ToggleView> GetControllableFlags();
        IEnumerable<ParamView> GetControllableParameters();
        IReadOnlyList<AttackRoutine> GetAllRoutines();
        IReadOnlyList<AttackRoutine> GetVisibleRoutines();
        Dictionary<string, List<Text>> GetTextEffects();

        TimelineAdvanceResult AdvanceTimeline(LengthOfTime duration);
        TimelineAdvanceResult AdvanceTimeline(long rounds);
        TimelineAdvanceResult SetTime(DateTime time);
        TimelineAdvanceResult StartCombat();
        TimelineAdvanceResult EndCombat();

        int GetSizeCategory();
        int GetStartingIntelligence();
        int BaseHp();
        bool TooManyRanks(string skill, int levelIndex);
        bool IsClassSkill(string skill, int levelIndex);
        int SkillPointsRemainingAtLevel(int levelIndex);
        int TotalSkillRanks(string skill);
        SortedDictionary<int, List<string>> GetFeatSlots();
        int GetFeatSlotLevel(string featSlot);
        string ResolveFeatTargetSource(string featSlot);
        IReadOnlyList<string> GetEligibleClassesAtLevel(int level, string filter = null);
        object ResolveFeatChoice(string featSlot, ChoiceDescriptor descriptor);
        string ResolveClassChoice(string featSlot, ChoiceDescriptor descriptor);
        bool IsClassEligible(string className, string filter);
        void SetFeatChoice(string featSlot, string choice);
        void SetFeatChoice(string featSlot, string key, string choice);
        string GetFeatChoice(string featSlot, string key = null);
        IReadOnlyList<ValidationWarning> ValidateProgressionChoice(ProgressionChoiceContext context);
        void AddLevel();
        ICharacterSession GetSessionAtLevel(int level, bool includeEquipped = false);
    }
}
