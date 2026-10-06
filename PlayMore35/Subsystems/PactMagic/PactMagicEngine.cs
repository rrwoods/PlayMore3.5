using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Subsystems.PactMagic {
    public class PactMagicEngine : ISlottedSubsystem {
        public const string SubsystemId = "pact_magic";
        private readonly ISubsystemContentProvider contentProvider;
        private readonly SubsystemSlotGroup<PactSlot> slots;
        private ICharacterContext context;

        public PactMagicEngine(ISubsystemContentProvider contentProvider = null) {
            this.contentProvider = contentProvider ?? new ResourceSubsystemContentProvider();
            this.slots = new SubsystemSlotGroup<PactSlot>(() => {
                if (context?.Effects?.stats?.TryGetValue(NUM_VESTIGES, out var stat) == true) {
                    return stat.GetValue(context.Effects, context.EquippedItems).value;
                }
                return 0;
            });
        }

        public string Id => SubsystemId;

        public bool IsActive {
            get {
                if (context?.Effects?.stats?.TryGetValue(NUM_VESTIGES, out var stat) == true) {
                    return stat.GetValue(context.Effects, context.EquippedItems).value > 0;
                }
                return false;
            }
        }

        public int SlotCount => slots.Capacity;

        public void Initialize(ICharacterContext context) {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            if (context.Character != null) {
                if (context.Character.Subsystems.TryGetValue(SubsystemId, out var state) && state is PactMagicState pactState) {
                    for (int i = 0; i < pactState.Slots.Count && i < slots.Capacity; i++) {
                        slots[i] = pactState.Slots[i];
                    }
                } else if (context.Character.vestiges != null && context.Character.vestiges.Count > 0) {
                    for (int i = 0; i < context.Character.vestiges.Count && i < slots.Capacity; i++) {
                        var (name, pState) = context.Character.vestiges[i];
                        slots[i] = new PactSlot(name, (PactState)(int)pState);
                    }
                    SyncState();
                } else {
                    SyncState();
                }
            }
        }

        public PactSlot GetSlot(int slotIndex) {
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

        public void ClearSlot(int slotIndex) {
            if (slotIndex >= 0 && slotIndex < SlotCount) {
                slots[slotIndex] = new PactSlot("", PactState.GOOD_PACT);
                SyncState();
            }
        }

        public void BindVestige(int slotIndex, string vestigeName, PactState state = PactState.GOOD_PACT) {
            if (slotIndex >= 0 && slotIndex < SlotCount) {
                slots[slotIndex] = new PactSlot(vestigeName ?? "", state);
                SyncState();
            }
        }

        public void ChangePactState(int slotIndex, PactState state) {
            if (slotIndex >= 0 && slotIndex < SlotCount) {
                var current = GetSlot(slotIndex);
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
            int maxLevel = 0;
            if (context?.Effects?.stats?.TryGetValue(MAX_VESTIGE_LEVEL, out var stat) == true) {
                maxLevel = stat.GetValue(context.Effects, context.EquippedItems).value;
            }
            return contentProvider.GetVestiges()
                .Where(v => v.Level <= maxLevel)
                .OrderBy(v => v.Name);
        }

        public int BindingCheckDc(string vestigeName) {
            if (string.IsNullOrEmpty(vestigeName)) return 0;
            return contentProvider.GetVestige(vestigeName)?.BindingDc ?? 0;
        }

        public (int bonus, List<string> explanation) GetBindingCheckBonus() {
            if (context?.Effects?.stats?.TryGetValue(BINDING_CHECK, out var stat) == true) {
                return stat.GetValue(context.Effects, context.EquippedItems);
            }
            return (0, new List<string>());
        }

        public void ContributeEffects(EffectPool pool, ICharacterContext context) {
            if (pool == null) return;
            for (int i = 0; i < SlotCount; i++) {
                var slot = GetSlot(i);
                if (slot == null || string.IsNullOrEmpty(slot.VestigeName)) continue;

                var vestige = contentProvider.GetVestige(slot.VestigeName);
                if (vestige == null) continue;

                if (slot.State == PactState.POOR_PACT) {
                    pool.AddEffect(vestige.Influence);
                } else if (slot.State == PactState.DEFIED) {
                    pool.AddEffect(new Effect($"Defied {vestige.Name}")
                        .AddAttackModifier(AttackMatcher.ALL, new Modifier(-1, UNTYPED))
                        .AddModifier(ALL_SAVES, new Modifier(-1, UNTYPED))
                        .AddModifier(ABILITY_CHECKS, new Modifier(-1, UNTYPED))
                        .AddModifier(ALL_SKILLS, new Modifier(-1, UNTYPED))
                    );
                }

                if (vestige.Abilities != null) {
                    foreach (var ability in vestige.Abilities) {
                        pool.AddEffect(ability);
                    }
                }
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
