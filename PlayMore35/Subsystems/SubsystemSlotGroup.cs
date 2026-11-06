using System;
using System.Collections.Generic;
using PlayMore35.Global;

namespace PlayMore35.Subsystems {
    [AuthorizedDomainApi("Domain container for subsystem slot groups")]
    public class SubsystemSlotGroup<TSlot> {
        private readonly Func<int> capacityProvider;
        private readonly Func<int>? maxLevelProvider;
        private readonly Func<TSlot, int>? levelSelector;
        private readonly Func<TSlot> slotFactory;
        private readonly IList<TSlot> slots;

        public SubsystemSlotGroup(Func<int> capacityProvider, Func<int>? maxLevelProvider = null, Func<TSlot, int>? levelSelector = null, IList<TSlot>? storage = null, Func<TSlot>? slotFactory = null) {
            this.capacityProvider = capacityProvider ?? throw new ArgumentNullException(nameof(capacityProvider));
            this.maxLevelProvider = maxLevelProvider;
            this.levelSelector = levelSelector;
            this.slotFactory = slotFactory ?? (() => {
                try {
                    return Activator.CreateInstance<TSlot>();
                } catch (MissingMethodException) {
                    return default(TSlot)!;
                }
            });
            this.slots = storage ?? new List<TSlot>();
        }

        public SubsystemSlotGroup(int fixedCapacity, Func<int>? maxLevelProvider = null, Func<TSlot, int>? levelSelector = null, IList<TSlot>? storage = null, Func<TSlot>? slotFactory = null)
            : this(() => fixedCapacity, maxLevelProvider, levelSelector, storage, slotFactory) {
        }

        public int Capacity => Math.Max(0, capacityProvider());

        internal IList<TSlot> RawSlots => slots;

        public int MaxLevel => maxLevelProvider != null ? maxLevelProvider() : int.MaxValue;

        public bool IsSlotLevelValid(int slotIndex) {
            ValidateIndex(slotIndex);
            if (levelSelector == null) {
                return true;
            }
            if (slotIndex >= slots.Count) {
                return true;
            }
            TSlot item = slots[slotIndex];
            if (EqualityComparer<TSlot>.Default.Equals(item, default(TSlot)!)) {
                return true;
            }
            return levelSelector(item) <= MaxLevel;
        }

        public IReadOnlyList<TSlot> Slots {
            get {
                int count = Math.Max(Capacity, slots.Count);
                var result = new List<TSlot>(count);
                for (int i = 0; i < slots.Count; i++) {
                    result.Add(slots[i]);
                }
                while (result.Count < count) {
                    result.Add(slotFactory());
                }
                return result;
            }
        }

        public TSlot this[int index] {
            get {
                ValidateIndex(index);
                EnsureCapacity(index + 1);
                return slots[index];
            }
            set {
                ValidateIndex(index);
                EnsureCapacity(index + 1);
                slots[index] = value;
            }
        }

        public void ClearSlot(int slotIndex) {
            ValidateIndex(slotIndex);
            if (slotIndex < slots.Count) {
                slots[slotIndex] = slotFactory();
            }
        }

        public void ClearAll() {
            slots.Clear();
        }

        private static void ValidateIndex(int index) {
            if (index < 0) {
                throw new ArgumentOutOfRangeException(nameof(index), $"Slot index {index} must be non-negative.");
            }
        }

        private void EnsureCapacity(int size) {
            while (slots.Count < size) {
                slots.Add(slotFactory());
            }
        }
    }
}
