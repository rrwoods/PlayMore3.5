using System.Collections.Generic;
using System.Linq;
using PlayMore35.Items;

namespace PlayMore35.Effects {
    public class WeaponClass {
        public static readonly WeaponClass ALL = new WeaponClass();

        // Making a WeaponClass and adding no matchers to it will cause it to match all attacks.
        // Each matcher represents a requirement that must be met.  For example:
        // new WeaponClass().Martial().OneHandedAttack()
        // will represent any attack with a martial weapon weilded in one hand.

        private List<IMatcher> matchers = new List<IMatcher>();

        public WeaponClass Name(string name) {
            matchers.Add(new NameMatcher(name));
            return this;
        }

        public WeaponClass Tag(string tag) {
            matchers.Add(new TagMatcher(tag));
            return this;
        }

        public WeaponClass SizeCategory(int sizeCategory) {
            matchers.Add(new SizeMatcher(sizeCategory));
            return this;
        }

        public WeaponClass Simple() {
            matchers.Add(new ProficiencyMatcher().AddProficiency(Weapon.Proficiency.SIMPLE));
            return this;
        }

        public WeaponClass Martial() {
            matchers.Add(new ProficiencyMatcher().AddProficiency(Weapon.Proficiency.MARTIAL));
            return this;
        }

        public WeaponClass Exotic() {
            matchers.Add(new ProficiencyMatcher().AddProficiency(Weapon.Proficiency.EXOTIC));
            return this;
        }

        public WeaponClass Light() {
            matchers.Add(new EffortMatcher().AddEffort(Weapon.Effort.LIGHT));
            return this;
        }

        public WeaponClass OneHandedWeapon() {
            matchers.Add(new EffortMatcher().AddEffort(Weapon.Effort.ONE_HANDED));
            return this;
        }

        public WeaponClass PrimaryHandAttack() {
            matchers.Add(new HandMatcher()
                .AddHand(Weapon.Hand.PRIMARY_HAND_TWF)
                .AddHand(Weapon.Hand.ONE_HANDED)
            );
            return this;
        }

        public WeaponClass OffHandAttack() {
            matchers.Add(new HandMatcher()
                .AddHand(Weapon.Hand.OFF_HAND_TWF)
            );
            return this;
        }

        // I don't have a TwoHandedWeapon matcher function here.  You probably don't want it.
        // If you think you want it because of Power Attack, you should read Power Attack ;)

        public WeaponClass TwoHandedAttack() {
            matchers.Add(new HandMatcher().AddHand(Weapon.Hand.TWO_HANDED));
            return this;
        }

        public WeaponClass Melee() {
            matchers.Add(new RangeMatcher()
                .AddRange(Weapon.Range.MELEE)
                .AddRange(Weapon.Range.REACH_MELEE)
            );
            return this;
        }

        public WeaponClass Ranged() {
            matchers.Add(new RangeMatcher()
                .AddRange(Weapon.Range.THROWN)
                .AddRange(Weapon.Range.BOW)
                .AddRange(Weapon.Range.OTHER_PROJECTILE)
            );
            return this;
        }

        public WeaponClass Thrown() {
            matchers.Add(new RangeMatcher()
                .AddRange(Weapon.Range.THROWN)
            );
            return this;
        }

        public WeaponClass Bow() {
            matchers.Add(new RangeMatcher()
                .AddRange(Weapon.Range.BOW)
            );
            return this;
        }

        public bool Matches(Attack attack) {
            return matchers.All(x => x.Matches(attack));
        }
    }

    interface IMatcher {
        bool Matches(Attack attack);
    }

    class NameMatcher : IMatcher {
        private readonly string Name;

        public NameMatcher(string name) {
            Name = name;
        }

        public bool Matches(Attack attack) {
            return attack.WeaponName.Equals(Name);
        }
    }

    class TagMatcher : IMatcher {
        private readonly string Tag;
        
        public TagMatcher(string tag) {
            Tag = tag;
        }

        public bool Matches(Attack attack) {
            return attack.WeaponTags.Contains(Tag);
        }
    }

    class SizeMatcher : IMatcher {
        private readonly int SizeCategory;

        public SizeMatcher(int sizeCategory) {
            SizeCategory = sizeCategory;
        }

        public bool Matches(Attack attack) {
            return attack.WeaponSizeCategory == SizeCategory;
        }
    }

    class ProficiencyMatcher : IMatcher {
        private readonly List<Weapon.Proficiency> Proficiencies = new List<Weapon.Proficiency>();

        public ProficiencyMatcher AddProficiency(Weapon.Proficiency proficiency) {
            Proficiencies.Add(proficiency);
            return this;
        }

        public bool Matches(Attack attack) {
            return Proficiencies.Contains(attack.Proficiency);
        }
    }

    class EffortMatcher : IMatcher {
        private readonly List<Weapon.Effort> Efforts = new List<Weapon.Effort>();

        public EffortMatcher AddEffort(Weapon.Effort effort) {
            Efforts.Add(effort);
            return this;
        }

        public bool Matches(Attack attack) {
            return Efforts.Contains(attack.Effort);
        }
    }
    
    class HandMatcher : IMatcher {
        private readonly List<Weapon.Hand> Hands = new List<Weapon.Hand>();

        public HandMatcher AddHand(Weapon.Hand hand) {
            Hands.Add(hand);
            return this;
        }

        public bool Matches(Attack attack) {
            return Hands.Contains(attack.Hand);
        }
    }

    class RangeMatcher : IMatcher {
        private readonly List<Weapon.Range> Ranges = new List<Weapon.Range>();

        public RangeMatcher AddRange(Weapon.Range range) {
            Ranges.Add(range);
            return this;
        }

        public bool Matches(Attack attack) {
            return Ranges.Contains(attack.Range);
        }
    }
}
