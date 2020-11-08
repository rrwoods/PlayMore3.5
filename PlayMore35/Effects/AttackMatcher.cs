using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Items;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Effects {
    public class AttackMatcher : ISubjectMatcher<Attack> {
        // If an effect grants an "extra attack", this is the matcher you want -- it does not grant
        // extra off-hand attacks unless it explicitly says it does.
        public static readonly AttackMatcher ALL_EXCEPT_OFF_HAND = new AttackMatcher().NonOffHandAttack();

        // If something modifies "attack rolls" or "damage rolls" in general with no qualifiers,
        // Use this one.
        public static readonly AttackMatcher ALL = new AttackMatcher();

        private List<IMatcher> matchers = new List<IMatcher>();

        public JToken ToJson() {
            return new JObject(matchers.Select(x => x.ToJson()));
        }

        public AttackMatcher(JObject json) {
            foreach (JProperty prop in json.Properties()) {
                if (prop.Name.Equals("name")) {
                    matchers.Add(new NameMatcher((string)prop.Value));
                } else if (prop.Name.Equals("tag")) {
                    matchers.Add(new TagMatcher((string)prop.Value));
                } else if (prop.Name.Equals("size")) {
                    matchers.Add(new SizeMatcher((int)prop.Value));
                } else if (prop.Name.Equals("proficiency")) {
                    ProficiencyMatcher matcher = new ProficiencyMatcher();
                    foreach (JValue value in prop["proficiency"]) {
                        matcher.AddProficiency((Weapon.Proficiency)Enum.Parse(typeof(Weapon.Proficiency), (string)value, false));
                    }
                    matchers.Add(matcher);
                } else if (prop.Name.Equals("effort")) {
                    EffortMatcher matcher = new EffortMatcher();
                    foreach (JValue value in prop.Value) {
                        matcher.AddEffort((Weapon.Effort)Enum.Parse(typeof(Weapon.Effort), (string)value, false));
                    }
                    matchers.Add(matcher);
                } else if (prop.Name.Equals("hand")) {
                    HandMatcher matcher = new HandMatcher();
                    foreach (JValue value in prop.Value) {
                        matcher.AddHand((Weapon.Hand)Enum.Parse(typeof(Weapon.Hand), (string)value, false));
                    }
                    matchers.Add(matcher);
                } else if (prop.Name.Equals("range")) {
                    RangeMatcher matcher = new RangeMatcher();
                    foreach (JValue value in prop.Value) {
                        matcher.AddRange((Weapon.Range)Enum.Parse(typeof(Weapon.Range), (string)value, false));
                    }
                    matchers.Add(matcher);
                }
            }
        }

        public AttackMatcher() {

        }

        public AttackMatcher Name(string name) {
            matchers.Add(new NameMatcher(name));
            return this;
        }

        public AttackMatcher Tag(string tag) {
            matchers.Add(new TagMatcher(tag));
            return this;
        }

        public AttackMatcher SizeCategory(int sizeCategory) {
            matchers.Add(new SizeMatcher(sizeCategory));
            return this;
        }

        public AttackMatcher Simple() {
            matchers.Add(new ProficiencyMatcher().AddProficiency(Weapon.Proficiency.SIMPLE));
            return this;
        }

        public AttackMatcher Martial() {
            matchers.Add(new ProficiencyMatcher().AddProficiency(Weapon.Proficiency.MARTIAL));
            return this;
        }

        public AttackMatcher SimpleOrMartial() {
            matchers.Add(new ProficiencyMatcher()
                .AddProficiency(Weapon.Proficiency.SIMPLE)
                .AddProficiency(Weapon.Proficiency.MARTIAL)
            );
            return this;
        }

        public AttackMatcher Exotic() {
            matchers.Add(new ProficiencyMatcher().AddProficiency(Weapon.Proficiency.EXOTIC));
            return this;
        }

        public AttackMatcher Light() {
            matchers.Add(new EffortMatcher().AddEffort(Weapon.Effort.LIGHT));
            return this;
        }

        public AttackMatcher OneHandedWeapon() {
            matchers.Add(new EffortMatcher().AddEffort(Weapon.Effort.ONE_HANDED));
            return this;
        }

        public AttackMatcher PrimaryHandAttack() {
            matchers.Add(new HandMatcher()
                .AddHand(Weapon.Hand.PRIMARY_HAND_TWF)
                .AddHand(Weapon.Hand.ONE_HANDED)
            );
            return this;
        }

        public AttackMatcher PrimaryHandTwfAttack() {
            matchers.Add(new HandMatcher()
                .AddHand(Weapon.Hand.PRIMARY_HAND_TWF)
            );
            return this;
        }

        public AttackMatcher OffHandAttack() {
            matchers.Add(new HandMatcher()
                .AddHand(Weapon.Hand.OFF_HAND_TWF)
            );
            return this;
        }

        public AttackMatcher OneHandedAttack() {
            matchers.Add(new HandMatcher()
                .AddHand(Weapon.Hand.ONE_HANDED)
                .AddHand(Weapon.Hand.PRIMARY_HAND_TWF)
                .AddHand(Weapon.Hand.OFF_HAND_TWF)
            );
            return this;
        }

        public AttackMatcher NonOffHandAttack() {
            matchers.Add(new HandMatcher()
                .AddHand(Weapon.Hand.ONE_HANDED)
                .AddHand(Weapon.Hand.PRIMARY_HAND_TWF)
                .AddHand(Weapon.Hand.TWO_HANDED)
            );
            return this;
        }

        // I don't have a TwoHandedWeapon matcher function here.  You probably don't want it.
        // If you think you want it because of Power Attack, you should read Power Attack ;)

        public AttackMatcher TwoHandedAttack() {
            matchers.Add(new HandMatcher().AddHand(Weapon.Hand.TWO_HANDED));
            return this;
        }

        public AttackMatcher Melee() {
            matchers.Add(new RangeMatcher()
                .AddRange(Weapon.Range.MELEE)
                .AddRange(Weapon.Range.REACH_MELEE)
            );
            return this;
        }

        public AttackMatcher Ranged() {
            matchers.Add(new RangeMatcher()
                .AddRange(Weapon.Range.THROWN)
                .AddRange(Weapon.Range.BOW)
                .AddRange(Weapon.Range.OTHER_PROJECTILE)
            );
            return this;
        }

        public AttackMatcher Thrown() {
            matchers.Add(new RangeMatcher()
                .AddRange(Weapon.Range.THROWN)
            );
            return this;
        }

        public AttackMatcher Bow() {
            matchers.Add(new RangeMatcher()
                .AddRange(Weapon.Range.BOW)
            );
            return this;
        }

        public bool Matches(Attack attack) {
            return matchers.All(x => x.Matches(attack));
        }
    }

    interface IMatcher : IJsonable {
        bool Matches(Attack attack);
    }

    class NameMatcher : IMatcher {
        private readonly string Name;

        public JToken ToJson() {
            return new JProperty("name", Name);
        }

        public NameMatcher(string name) {
            Name = name;
        }

        public bool Matches(Attack attack) {
            return attack.WeaponName.Equals(Name);
        }
    }

    class TagMatcher : IMatcher {
        private readonly string Tag;

        public JToken ToJson() {
            return new JProperty("tag", Tag);
        }

        public TagMatcher(string tag) {
            Tag = tag;
        }

        public bool Matches(Attack attack) {
            return attack.WeaponTags.Contains(Tag);
        }
    }

    class SizeMatcher : IMatcher {
        private readonly int SizeCategory;

        public JToken ToJson() {
            return new JProperty("size", SizeCategory);
        }

        public SizeMatcher(int sizeCategory) {
            SizeCategory = sizeCategory;
        }

        public bool Matches(Attack attack) {
            return attack.WeaponSizeCategory == SizeCategory;
        }
    }

    class ProficiencyMatcher : IMatcher {
        private readonly List<Weapon.Proficiency> Proficiencies = new List<Weapon.Proficiency>();

        public JToken ToJson() {
            return new JProperty("proficiency", new JArray(Proficiencies.Select(x => x.ToString())));
        }

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

        public JToken ToJson() {
            return new JProperty("effort", new JArray(Efforts.Select(x => x.ToString())));
        }

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

        public JToken ToJson() {
            return new JProperty("hand", new JArray(Hands.Select(x => x.ToString())));
        }

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

        public JToken ToJson() {
            return new JProperty("range", new JArray(Ranges.Select(x => x.ToString())));
        }

        public RangeMatcher AddRange(Weapon.Range range) {
            Ranges.Add(range);
            return this;
        }

        public bool Matches(Attack attack) {
            return Ranges.Contains(attack.Range);
        }
    }
}
