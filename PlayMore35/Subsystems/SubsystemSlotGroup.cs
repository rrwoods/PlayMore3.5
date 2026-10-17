using System;
using System.Collections.Generic;

namespace PlayMore35.Subsystems {
    public class SubsystemSlotGroup<TSlot> {
        private readonly Func<int> capacityProvider;
        private readonly Func<int> maxLevelProvider;
        private readonly Func<TSlot, int> levelSelector;
        private readonly List<TSlot> slots = new List<TSlot>();

        public SubsystemSlotGroup(Func<int> capacityProvider, Func<int> maxLevelProvider = null, Func<TSlot, int> levelSelector = null) {
            this.capacityProvider = capacityProvider ?? throw new ArgumentNullException(nameof(capacityProvider));
            this.maxLevelProvider = maxLevelProvider;
            this.levelSelector = levelSelector;
        }

        public SubsystemSlotGroup(int fixedCapacity, Func<int> maxLevelProvider = null, Func<TSlot, int> levelSelector = null)
            : this(() => fixedCapacity, maxLevelProvider, levelSelector) {
        }

        public int Capacity => Math.Max(0, capacityProvider());

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
            if (EqualityComparer<TSlot>.Default.Equals(item, default(TSlot))) {
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
                    result.Add(default(TSlot));
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
                slots[slotIndex] = default(TSlot);
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
                slots.Add(default(TSlot));
            }
        }
    }
}
