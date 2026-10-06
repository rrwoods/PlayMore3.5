using System;

namespace PlayMore35.Subsystems {
    public class ResourcePool {
        private readonly Func<int> maxCapacityProvider;
        private int current;
        private bool initialized;

        public ResourcePool(string id, Func<int> maxCapacityProvider) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            this.maxCapacityProvider = maxCapacityProvider ?? throw new ArgumentNullException(nameof(maxCapacityProvider));
        }

        public ResourcePool(string id, int fixedMaxCapacity)
            : this(id, () => fixedMaxCapacity) {
        }

        public string Id { get; }

        public int Max => Math.Max(0, maxCapacityProvider());

        public int Current {
            get {
                if (!initialized) {
                    current = Max;
                    initialized = true;
                }
                if (current > Max) {
                    current = Max;
                }
                return current;
            }
            set {
                initialized = true;
                current = Math.Max(0, Math.Min(Max, value));
            }
        }

        public bool Spend(int amount) {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Cannot spend negative amount.");
            if (amount == 0) return true;
            if (Current < amount) return false;
            Current -= amount;
            return true;
        }

        public void Restore(int amount) {
            if (amount <= 0) return;
            Current = Math.Min(Max, Current + amount);
        }

        public void Reset() {
            Current = Max;
        }
    }
}
