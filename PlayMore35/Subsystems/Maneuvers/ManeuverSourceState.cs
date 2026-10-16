using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMore35.Global;

namespace PlayMore35.Subsystems.Maneuvers {
    public class ManeuverSourceState : IJsonable {
        private readonly List<KnownManeuverSlot> savedKnown = new List<KnownManeuverSlot>();
        private readonly List<StanceSlot> savedStances = new List<StanceSlot>();

        public SubsystemSlotGroup<KnownManeuverSlot> Known { get; private set; }
        public SubsystemSlotGroup<StanceSlot> Stances { get; private set; }

        public ManeuverSourceState() {
            Known = new SubsystemSlotGroup<KnownManeuverSlot>(0);
            Stances = new SubsystemSlotGroup<StanceSlot>(0);
        }

        public ManeuverSourceState(int knownCapacity, int stancesCapacity) {
            Known = new SubsystemSlotGroup<KnownManeuverSlot>(knownCapacity);
            Stances = new SubsystemSlotGroup<StanceSlot>(stancesCapacity);
            while (savedKnown.Count < knownCapacity) savedKnown.Add(null);
            while (savedStances.Count < stancesCapacity) savedStances.Add(null);
        }

        public ManeuverSourceState(
            Func<int> knownCapacityProvider,
            Func<int> knownMaxLevelProvider = null,
            Func<KnownManeuverSlot, int> knownLevelSelector = null,
            Func<int> stancesCapacityProvider = null,
            Func<int> stancesMaxLevelProvider = null,
            Func<StanceSlot, int> stanceLevelSelector = null) {
            Known = new SubsystemSlotGroup<KnownManeuverSlot>(
                knownCapacityProvider ?? (() => 0),
                knownMaxLevelProvider,
                knownLevelSelector
            );
            Stances = new SubsystemSlotGroup<StanceSlot>(
                stancesCapacityProvider ?? (() => 0),
                stancesMaxLevelProvider,
                stanceLevelSelector
            );
        }

        public ManeuverSourceState(JObject json) {
            if (json == null) throw new ArgumentNullException(nameof(json));

            if (json["known"] is JArray knownArr) {
                foreach (var token in knownArr) {
                    if (token is JObject obj) {
                        savedKnown.Add(new KnownManeuverSlot(obj));
                    }
                }
            }

            Known = new SubsystemSlotGroup<KnownManeuverSlot>(savedKnown.Count);
            for (int i = 0; i < savedKnown.Count; i++) {
                Known[i] = savedKnown[i];
            }

            if (json["stances"] is JArray stanceArr) {
                foreach (var token in stanceArr) {
                    if (token is JObject obj) {
                        savedStances.Add(new StanceSlot(obj));
                    }
                }
            }

            Stances = new SubsystemSlotGroup<StanceSlot>(savedStances.Count);
            for (int i = 0; i < savedStances.Count; i++) {
                Stances[i] = savedStances[i];
            }
        }

        private bool isDynamicKnown = false;
        private bool isDynamicStances = false;

        public void Wire(
            Func<int> knownCapacityProvider,
            Func<int> knownMaxLevelProvider = null,
            Func<KnownManeuverSlot, int> knownLevelSelector = null,
            Func<int> stancesCapacityProvider = null,
            Func<int> stancesMaxLevelProvider = null,
            Func<StanceSlot, int> stanceLevelSelector = null) {
            SyncToSaved();
            isDynamicKnown = true;
            isDynamicStances = true;

            Known = new SubsystemSlotGroup<KnownManeuverSlot>(
                knownCapacityProvider ?? (() => 0),
                knownMaxLevelProvider,
                knownLevelSelector
            );

            Stances = new SubsystemSlotGroup<StanceSlot>(
                stancesCapacityProvider ?? (() => 0),
                stancesMaxLevelProvider,
                stanceLevelSelector
            );

            EnsureLoaded();
        }

        public void EnsureLoaded() {
            if (Known != null && Known.Capacity > 0) {
                for (int i = 0; i < savedKnown.Count && i < Known.Capacity; i++) {
                    if (Known[i] == null && savedKnown[i] != null) {
                        Known[i] = savedKnown[i];
                    }
                }
            }

            if (Stances != null && Stances.Capacity > 0) {
                for (int i = 0; i < savedStances.Count && i < Stances.Capacity; i++) {
                    if (Stances[i] == null && savedStances[i] != null) {
                        Stances[i] = savedStances[i];
                    }
                }
            }
        }

        public void SyncToSaved() {
            if (Known != null && Known.Capacity > 0) {
                savedKnown.Clear();
                foreach (var slot in Known.Slots) {
                    savedKnown.Add(slot);
                }
            }
            if (Stances != null && Stances.Capacity > 0) {
                savedStances.Clear();
                foreach (var slot in Stances.Slots) {
                    savedStances.Add(slot);
                }
            }
        }

        public KnownManeuverSlot GetKnownSlot(int index) {
            EnsureLoaded();
            if (index < 0) return null;
            if (Known != null && index < Known.Capacity) return Known[index];
            if (index < savedKnown.Count) return savedKnown[index];
            return null;
        }

        public void SetKnownSlot(int index, KnownManeuverSlot slot) {
            if (index < 0) {
                throw new ArgumentOutOfRangeException(nameof(index), $"Slot index {index} is out of range.");
            }
            if (isDynamicKnown) {
                EnsureLoaded();
                if (Known == null || index >= Known.Capacity) {
                    throw new ArgumentOutOfRangeException(nameof(index), $"Slot index {index} is out of range.");
                }
                Known[index] = slot;
                SyncToSaved();
            } else {
                while (savedKnown.Count <= index) {
                    savedKnown.Add(null);
                }
                savedKnown[index] = slot;
                int cap = Math.Max(Known?.Capacity ?? 0, savedKnown.Count);
                Known = new SubsystemSlotGroup<KnownManeuverSlot>(cap);
                for (int i = 0; i < savedKnown.Count; i++) {
                    Known[i] = savedKnown[i];
                }
            }
        }

        public StanceSlot GetStanceSlot(int index) {
            EnsureLoaded();
            if (index < 0) return null;
            if (Stances != null && index < Stances.Capacity) return Stances[index];
            if (index < savedStances.Count) return savedStances[index];
            return null;
        }

        public void SetStanceSlot(int index, StanceSlot slot) {
            if (index < 0) {
                throw new ArgumentOutOfRangeException(nameof(index), $"Slot index {index} is out of range.");
            }
            if (isDynamicStances) {
                EnsureLoaded();
                if (Stances == null || index >= Stances.Capacity) {
                    throw new ArgumentOutOfRangeException(nameof(index), $"Slot index {index} is out of range.");
                }
                Stances[index] = slot;
                SyncToSaved();
            } else {
                while (savedStances.Count <= index) {
                    savedStances.Add(null);
                }
                savedStances[index] = slot;
                int cap = Math.Max(Stances?.Capacity ?? 0, savedStances.Count);
                Stances = new SubsystemSlotGroup<StanceSlot>(cap);
                for (int i = 0; i < savedStances.Count; i++) {
                    Stances[i] = savedStances[i];
                }
            }
        }

        public IEnumerable<KnownManeuverSlot> ReadiedManeuvers {
            get {
                EnsureLoaded();
                return Known?.Slots?.Where(s => s != null && s.IsReadied) ?? Enumerable.Empty<KnownManeuverSlot>();
            }
        }

        public int ReadiedCount {
            get {
                EnsureLoaded();
                return Known?.Slots?.Count(s => s != null && s.IsReadied) ?? 0;
            }
        }

        public IEnumerable<StanceSlot> ActiveStances {
            get {
                EnsureLoaded();
                return Stances?.Slots?.Where(s => s != null && s.IsActive) ?? Enumerable.Empty<StanceSlot>();
            }
        }

        public IEnumerable<string> AllKnownManeuverNames() {
            EnsureLoaded();
            var slots = (Known != null && Known.Capacity > 0) ? Known.Slots : (IReadOnlyList<KnownManeuverSlot>)savedKnown;
            return slots?
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.ManeuverName))
                .Select(s => s.ManeuverName)
                ?? Enumerable.Empty<string>();
        }

        public IEnumerable<string> AllStanceNames() {
            EnsureLoaded();
            var slots = (Stances != null && Stances.Capacity > 0) ? Stances.Slots : (IReadOnlyList<StanceSlot>)savedStances;
            return slots?
                .Where(s => s != null && !string.IsNullOrWhiteSpace(s.StanceName))
                .Select(s => s.StanceName)
                ?? Enumerable.Empty<string>();
        }

        public bool HasManeuver(string maneuverName) {
            if (string.IsNullOrWhiteSpace(maneuverName)) return false;
            return AllKnownManeuverNames().Any(name => string.Equals(name, maneuverName, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasStance(string stanceName) {
            if (string.IsNullOrWhiteSpace(stanceName)) return false;
            return AllStanceNames().Any(name => string.Equals(name, stanceName, StringComparison.OrdinalIgnoreCase));
        }

        public JToken ToJson() {
            EnsureLoaded();
            SyncToSaved();

            var knownArr = new JArray();
            var knownSlots = (Known != null && Known.Capacity > 0) ? Known.Slots : (IReadOnlyList<KnownManeuverSlot>)savedKnown;
            foreach (var slot in knownSlots) {
                if (slot != null) {
                    knownArr.Add(slot.ToJson());
                } else {
                    knownArr.Add(new KnownManeuverSlot().ToJson());
                }
            }

            var stanceArr = new JArray();
            var stanceSlots = (Stances != null && Stances.Capacity > 0) ? Stances.Slots : (IReadOnlyList<StanceSlot>)savedStances;
            foreach (var slot in stanceSlots) {
                if (slot != null) {
                    stanceArr.Add(slot.ToJson());
                } else {
                    stanceArr.Add(new StanceSlot().ToJson());
                }
            }

            return new JObject(
                new JProperty("known", knownArr),
                new JProperty("stances", stanceArr)
            );
        }
    }
}
