using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;

namespace PlayMore35.Items {
    public class Equipped {
        public enum Slot {
            HEAD, FACE, NECK, TORSO, BODY, WAIST, SHOULDERS, ARMS, HANDS, RING1, RING2, FEET, SLOTLESS
        }

        public Weapon PrimaryHandWeapon = null;
        public Weapon OffHandWeapon = null;

        // Donned armor will override a body-slot item.
        public Armor DonnedArmor = null;
        public Shield OffHandShield = null;

        public Dictionary<Slot, Item> SlottedItems = new Dictionary<Slot, Item>();
        public List<Item> SlotlessItems = new List<Item>();
        
        // the total weight of all items NOT accounted for above that still contribute
        // to the player's total weight carried.
        public decimal UnequippedWeight;

        public decimal EquippedWeight() {
            return (PrimaryHandWeapon?.Weight ?? 0) +
                (PrimaryHandWeapon == OffHandWeapon ? 0 : OffHandWeapon?.Weight ?? 0) +
                (DonnedArmor?.Weight ?? 0) +
                (OffHandShield?.Weight ?? 0) +
                (SlotlessItems.Select(x => x.Weight).Sum()) +
                (SlottedItems.Values.Select(x => x.Weight).Sum());
        }

        public decimal TotalWeight() {
            return UnequippedWeight + EquippedWeight();
        }

        public IEnumerable<Effect> GetGlobalEffects() {
            IEnumerable<Effect> all = Enumerable.Empty<Effect>();
            if (PrimaryHandWeapon != null) {
                all = all.Concat(PrimaryHandWeapon.GetGlobalEffects());
            }
            if (OffHandWeapon != null) {
                all = all.Concat(OffHandWeapon.GetGlobalEffects());
            }
            if (DonnedArmor != null) {
                all = all.Concat(DonnedArmor.GetGlobalEffects());
            }
            if (OffHandShield != null) {
                all = all.Concat(OffHandShield.GetGlobalEffects());
            }

            all = all.Concat(SlotlessItems.Select(x => x.ItemEffect));
            if (DonnedArmor == null) {
                all = all.Concat(SlottedItems.Values.Select(x => x.ItemEffect));
            } else {
                all = all.Concat(SlottedItems.Where(x => x.Key != Slot.BODY).Select(x => x.Value.ItemEffect));
            }

            return all;
        }
    }
}
