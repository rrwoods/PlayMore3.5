using System;
using System.Collections.Generic;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Rules;
using PlayMore35.Subsystems;

namespace PlayMore35.Tests {
    public class FakeCharacterContext : ICharacterContext {
        public Character Character { get; set; }
        public EffectPool Effects { get; set; }
        public Equipped EquippedItems { get; set; }
        public Timeline Timeline => Character?.Timeline;
        public Dictionary<string, SubsystemEngine> SubsystemsMap { get; } = new Dictionary<string, SubsystemEngine>(StringComparer.OrdinalIgnoreCase);
        public IRulesCatalog Catalog { get; set; }

        public FakeCharacterContext(Character character = null, EffectPool effects = null, Equipped equipped = null, IRulesCatalog catalog = null) {
            Character = character ?? new Character();
            Effects = effects ?? new EffectPool(Character);
            EquippedItems = equipped ?? new Equipped();
            Catalog = catalog;
            if (Character.Timeline != null) {
                Character.Timeline.Context = this;
            }
        }

        public virtual List<AttackRoutine> GetAllRoutines() => new List<AttackRoutine>();
        public virtual List<AttackRoutine> GetVisibleRoutines() => new List<AttackRoutine>();

        public virtual StatView GetStatistic(string name) =>
            Effects?.GetStatistic(name, EquippedItems) ?? new StatView(name ?? "", 0);

        public virtual int GetStatValue(string name, int defaultValue = 0) =>
            Effects?.GetStatValue(name, EquippedItems, defaultValue) ?? defaultValue;

        public virtual SubsystemEngine? GetSubsystem(string? id) {
            if (string.IsNullOrEmpty(id)) return null;
            return SubsystemsMap.TryGetValue(id!, out var engine) ? engine : null;
        }
    }
}
