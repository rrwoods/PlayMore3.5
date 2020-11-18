using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;

namespace PlayMore35.Items {
    public class Equipped {
        public enum Slot {
            HEAD, EYES, NECK, TORSO, BODY, WAIST, SHOULDERS, ARMS, HANDS, RING1, RING2, FEET, SLOTLESS
        }

        public Weapon PrimaryHandWeapon = null;
        public Weapon OffHandWeapon = null;

        // Donned armor will override a body-slot item.
        public Armor DonnedArmor = null;
        public Shield OffHandShield = null;

        public Dictionary<Slot, Item> SlottedItems = new Dictionary<Slot, Item>();
        public List<Item> SlotlessItems = new List<Item>();

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
