using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Subsystems.PactMagic {
    public class PactMagicEngine : ISubsystemEngine {
        public const string SubsystemId = "pact_magic";
        private readonly ISubsystemContentProvider contentProvider;
        private SubsystemSlotGroup<PactSlot> slots;
        private ICharacterContext context;
        private bool stateLoaded = false;

        public PactMagicEngine(ISubsystemContentProvider contentProvider = null) {
            this.contentProvider = contentProvider ?? new ResourceSubsystemContentProvider();
            this.slots = new SubsystemSlotGroup<PactSlot>(
                () => context?.GetStatValue(NUM_VESTIGES, 0) ?? 0,
                () => context?.GetStatValue(MAX_VESTIGE_LEVEL, 0) ?? int.MaxValue,
                slot => contentProvider.GetVestige(slot?.VestigeName)?.Level ?? 0
            );
        }

        public string Id => SubsystemId;

        public bool IsActive => context?.GetStatValue(NUM_VESTIGES, 0) > 0;

        public int SlotCount => slots.Capacity;

        public bool IsSlotLevelValid(int slotIndex) {
            EnsureSlotsLoaded();
            return slots.IsSlotLevelValid(slotIndex);
        }

        public void Initialize(ICharacterContext context) {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.slots = new SubsystemSlotGroup<PactSlot>(
                () => context.GetStatValue(NUM_VESTIGES, 0),
                () => context?.GetStatValue(MAX_VESTIGE_LEVEL, 0) ?? int.MaxValue,
                slot => contentProvider.GetVestige(slot?.VestigeName)?.Level ?? 0
            );
            this.stateLoaded = false;
            EnsureSlotsLoaded();
        }

        private void EnsureSlotsLoaded() {
            if (stateLoaded) return;
            if (context?.Character != null && slots.Capacity > 0) {
                stateLoaded = true;
                if (context.Character.Subsystems.TryGetValue(SubsystemId, out var state) && state is PactMagicState pactState) {
                    for (int i = 0; i < pactState.Slots.Count && i < slots.Capacity; i++) {
                        slots[i] = new PactSlot(pactState.Slots[i].VestigeName, pactState.Slots[i].State);
                    }
                }
                SyncState();
            }
        }

        public PactSlot GetSlot(int slotIndex) {
            EnsureSlotsLoaded();
            if (slotIndex < 0 || slotIndex >= SlotCount) {
                return null;
            }
            var slot = slots[slotIndex];
            if (slot == null) {
                slot = new PactSlot("", PactState.GOOD_PACT);
                slots[slotIndex] = slot;
            }
            return slot;
        }

        private void ResetDefiance(string vestigeName) {
            if (string.IsNullOrEmpty(vestigeName)) return;
            var vestige = contentProvider.GetVestige(vestigeName);
            if (vestige?.Influence == null) return;
            string paramKey = vestige.Influence.ParameterName;
            if (context?.Character?.paramValues != null) {
                context.Character.paramValues[paramKey] = 0;
            }
            if (context?.Effects?.controllableParameters != null &&
                context.Effects.controllableParameters.TryGetValue(paramKey, out var param)) {
                param.Value = 0;
            }
        }

        public void UnbindVestige(int slotIndex) {
            EnsureSlotsLoaded();
            if (slotIndex >= 0 && slotIndex < SlotCount) {
                ResetDefiance(GetSlot(slotIndex)?.VestigeName);
                slots[slotIndex] = new PactSlot("", PactState.GOOD_PACT);
                SyncState();
            }
        }

        public void BindVestige(int slotIndex, string vestigeName, PactState state = PactState.GOOD_PACT) {
            EnsureSlotsLoaded();
            if (slotIndex >= 0 && slotIndex < SlotCount) {
                ResetDefiance(GetSlot(slotIndex)?.VestigeName);
                ResetDefiance(vestigeName);
                slots[slotIndex] = new PactSlot(vestigeName ?? "", state);
                SyncState();
            }
        }

        public void ChangePactState(int slotIndex, PactState state) {
            EnsureSlotsLoaded();
            if (slotIndex >= 0 && slotIndex < SlotCount) {
                var current = GetSlot(slotIndex);
                if (state == PactState.GOOD_PACT) {
                    ResetDefiance(current?.VestigeName);
                }
                current.State = state;
                SyncState();
            }
        }

        public Vestige CurrentVestige(int slotIndex) {
            var slot = GetSlot(slotIndex);
            if (slot == null || string.IsNullOrEmpty(slot.VestigeName)) {
                return null;
            }
            return contentProvider.GetVestige(slot.VestigeName);
        }

        public IEnumerable<Vestige> AvailableVestiges() {
            if (!IsActive) {
                return Enumerable.Empty<Vestige>();
            }
            int maxLevel = context?.GetStatValue(MAX_VESTIGE_LEVEL, 0) ?? 0;
            return contentProvider.GetVestiges()
                .Where(v => v.Level <= maxLevel)
                .OrderBy(v => v.Name);
        }

        public int BindingCheckDc(string vestigeName) {
            if (string.IsNullOrEmpty(vestigeName)) return 0;
            return contentProvider.GetVestige(vestigeName)?.BindingDc ?? 0;
        }

        public (int bonus, List<string> explanation) GetBindingCheckBonus() {
            if (context == null) return (0, new List<string>());
            var stat = context.GetStatistic(BINDING_CHECK);
            return (stat.Value, stat.Explanation.ToList());
        }

        public void ContributeEffects(EffectPool pool, ICharacterContext context) {
            if (pool == null) return;
            List<string> defianceParams = new List<string>();
            for (int i = 0; i < SlotCount; i++) {
                var slot = GetSlot(i);
                if (slot == null || string.IsNullOrEmpty(slot.VestigeName)) continue;

                var vestige = contentProvider.GetVestige(slot.VestigeName);
                if (vestige == null) continue;

                if (slot.State == PactState.POOR_PACT && vestige.Influence != null) {
                    pool.AddEffect(vestige.Influence);
                    defianceParams.Add(vestige.Influence.ParameterName);
                }

                if (vestige.Abilities != null) {
                    foreach (var ability in vestige.Abilities) {
                        pool.AddEffect(ability);
                    }
                }
            }

            if (defianceParams.Any()) {
                Modifier CreateDefianceModifier() {
                    Modifier mod = new Modifier(UNTYPED);
                    foreach (string paramKey in defianceParams) {
                        mod.AddAmount(new MultiplyAmount(-1, new ControllableAmount(paramKey)));
                    }
                    return mod;
                }

                pool.AddEffect(new Effect("Vestige defiance")
                    .AddAttackModifier(AttackMatcher.ALL, CreateDefianceModifier())
                    .AddModifier(ALL_SAVES, CreateDefianceModifier())
                    .AddModifier(ABILITY_CHECKS, CreateDefianceModifier())
                    .AddModifier(ALL_SKILLS, CreateDefianceModifier())
                );
            }
        }

        private void SyncState() {
            if (context?.Character == null) return;
            if (!context.Character.Subsystems.TryGetValue(SubsystemId, out var state) || !(state is PactMagicState pactMagicState)) {
                pactMagicState = new PactMagicState();
                context.Character.Subsystems[SubsystemId] = pactMagicState;
            }
            pactMagicState.Slots.Clear();
            for (int i = 0; i < SlotCount; i++) {
                pactMagicState.Slots.Add(GetSlot(i));
            }
        }
    }
}
