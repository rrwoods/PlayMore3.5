using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

using PlayMore35.Items;

namespace PlayMore35.Effects {
    public class Effect {
        public Effect(string name) {
            Name = name;
        }

        public string Name { get; private set; }
        public IToggle Toggle = AlwaysOn.Instance;

        private readonly List<Alterer<ExtraAttack, Attack>> ExtraAttacks = new List<Alterer<ExtraAttack, Attack>>();
        private readonly List<Alterer<Modifier, Attack>> AttackModifiers = new List<Alterer<Modifier, Attack>>();
        private readonly List<Alterer<Modifier, Attack>> DamageModifiers = new List<Alterer<Modifier, Attack>>();
        private readonly List<Alterer<DiceGroup, Attack>> ExtraDice = new List<Alterer<DiceGroup, Attack>>();
        private readonly List<Alterer<Modifier, Attack>> ThreatModifiers = new List<Alterer<Modifier, Attack>>();
        private readonly List<Alterer<Multiplier, Attack>> RangeMultipliers = new List<Alterer<Multiplier, Attack>>();

        private readonly List<Alterer<Modifier, Statistic>> Modifiers = new List<Alterer<Modifier, Statistic>>();
        private readonly List<Alterer<Multiplier, Statistic>> Multipliers = new List<Alterer<Multiplier, Statistic>>();

        private readonly List<AttackMatcher> WeaponProficiencies = new List<AttackMatcher>();
        private readonly List<Armor.WeightCategory> ArmorProficiencies = new List<Armor.WeightCategory>();
        private readonly List<bool> ShieldProficiencies = new List<bool>(); // false = shield prof, true = shield+tower prof

        private readonly List<(int level, string selectFrom)> Feats = new List<(int, string)>();
        private readonly List<string> SpecificFeats = new List<string>();
        private readonly int SkillPointsEachLevel = 0; // x4 at first is assumed
        // TODO : rethink this; effects that grant skill points take many forms
        // human / that feat that grants 5 skill points once / nymph's kiss -- all are different

        private readonly List<string> Flags = new List<string>();
        public readonly List<(string category, string text)> Text = new List<(string, string)>();

        public override string ToString() {
            return Name;
        }

        public JToken ToJson() {
            JObject json = new JObject(new JProperty("name", Name));
            if (Toggle != AlwaysOn.Instance) {
                json.Add("active", Toggle.ToJson());
            }
            if (ExtraAttacks.Any()) {
                json.Add("extra attacks", new JArray(ExtraAttacks.Select(x => x.ToJson())));
            }
            if (AttackModifiers.Any()) {
                json.Add("attack", new JArray(AttackModifiers.Select(x => x.ToJson())));
            }
            if (DamageModifiers.Any()) {
                json.Add("damage", new JArray(DamageModifiers.Select(x => x.ToJson())));
            }
            if (ExtraDice.Any()) {
                json.Add("dice", new JArray(ExtraDice.Select(x => x.ToJson())));
            }
            if (ThreatModifiers.Any()) {
                json.Add("threat", new JArray(ThreatModifiers.Select(x => x.ToJson())));
            }
            if (RangeMultipliers.Any()) {
                json.Add("range", new JArray(RangeMultipliers.Select(x => x.ToJson())));
            }
            if (Modifiers.Any()) {
                json.Add("modifiers", new JArray(Modifiers.Select(x => x.ToJson())));
            }
            if (Multipliers.Any()) {
                json.Add("multipliers", new JArray(Multipliers.Select(x => x.ToJson())));
            }
            if (WeaponProficiencies.Any()) {
                json.Add("weapon proficiencies", new JArray(WeaponProficiencies.Select(x => x.ToJson())));
            }
            if (ArmorProficiencies.Any()) {
                json.Add("armor proficiencies", new JArray(ArmorProficiencies));
            }
            if (ShieldProficiencies.Any()) {
                json.Add("shield proficiencies", new JArray(ShieldProficiencies.Select(x => x ? "tower" : "normal")));
            }
            if (Feats.Any()) {
                json.Add("feats", new JArray(Feats.Select(x => new JObject(
                    new JProperty("level", x.level),
                    new JProperty("select from", x.selectFrom)
                ))));
            }
            if (SpecificFeats.Any()) {
                json.Add("specific feats", new JArray(SpecificFeats));
            }
            if (SkillPointsEachLevel != 0) {
                json.Add("skill points", SkillPointsEachLevel);
            }
            if (Flags.Any()) {
                json.Add("flags", new JArray(Flags));
            }
            if (Text.Any()) {
                json.Add("text", new JArray(Text.Select(x => new JObject(
                    new JProperty("category", x.category),
                    new JProperty("text", x.text)
                ))));
            }
            return json;
        }

        public Effect(JObject json) {
            Name = (string)json["name"];
            if (json["active"] != null) {
                Toggle = Effects.Toggle.FromJson(json["active"]);
            }
            if (json["extra attacks"] != null) {
                ExtraAttacks.AddRange(json["extra attacks"].Select(x => new Alterer<ExtraAttack, Attack> {
                    affects = new AttackMatcher((JObject)x["affects"]),
                    mod = new ExtraAttack((JObject)x["mod"])
                }));
            }
            if (json["attack"] != null) {
                AttackModifiers.AddRange(json["attack"].Select(x => new Alterer<Modifier, Attack> {
                    affects = new AttackMatcher((JObject)x["affects"]),
                    mod = new Modifier((JObject)x["mod"]) {
                        Name = Name
                    }
                }));
            }
            if (json["damage"] != null) {
                DamageModifiers.AddRange(json["damage"].Select(x => new Alterer<Modifier, Attack> {
                    affects = new AttackMatcher((JObject)x["affects"]),
                    mod = new Modifier((JObject)x["mod"]) {
                        Name = Name
                    }
                }));
            }
            if (json["dice"] != null) {
                ExtraDice.AddRange(json["dice"].Select(x => new Alterer<DiceGroup, Attack> {
                    affects = new AttackMatcher((JObject)x["affects"]),
                    mod = new DiceGroup((JObject)x["mod"]) {
                        Name = Name
                    }
                }));
            }
            if (json["threat"] != null) {
                ThreatModifiers.AddRange(json["threat"].Select(x => new Alterer<Modifier, Attack> {
                    affects = new AttackMatcher((JObject)x["affects"]),
                    mod = new Modifier((JObject)x["mod"]) {
                        Name = Name
                    }
                }));
            }
            if (json["range"] != null) {
                RangeMultipliers.AddRange(json["range"].Select(x => new Alterer<Multiplier, Attack> {
                    affects = new AttackMatcher((JObject)x["affects"]),
                    mod = new Multiplier((decimal)x["mod"]) {
                        Name = Name
                    }
                }));
            }
            if (json["modifiers"] != null) {
                Modifiers.AddRange(json["modifiers"].Select(x => new Alterer<Modifier, Statistic> {
                    affects = new StatisticMatcher((string)x["affects"]),
                    mod = new Modifier((JObject)x["mod"]) {
                        Name = Name
                    }
                }));
            }
            if (json["multipliers"] != null) {
                Multipliers.AddRange(json["multipliers"].Select(x => new Alterer<Multiplier, Statistic> {
                    affects = new StatisticMatcher((string)x["affects"]),
                    mod = new Multiplier((decimal)x["mod"]) {
                        Name = Name
                    }
                }));
            }
            if (json["weapon proficiencies"] != null) {
                WeaponProficiencies.AddRange(json["weapon proficiencies"].Select(x => new AttackMatcher((JObject)x)));
            }
            if (json["armor proficiencies"] != null) {
                ArmorProficiencies.AddRange(json["armor proficiencies"].Select(x =>
                    (Armor.WeightCategory) Enum.Parse(typeof(Armor.WeightCategory), (string)x, false)
                ));
            }
            if (json["shield proficiencies"] != null) {
                ShieldProficiencies.AddRange(json["shield proficiencies"].Select(x => x.Equals("tower")));
            }
            if (json["feats"] != null) {
                Feats.AddRange(json["feats"].Select(x => ((int)x["level"], (string)x["select from"])));
            }
            if (json["specific feats"] != null) {
                SpecificFeats.AddRange(json["specific feats"].Select(x => (string)x));
            }
            if (json["skill points"] != null) {
                SkillPointsEachLevel = (int)json["skill points"];
            }
            if (json["flags"] != null) {
                Flags.AddRange(json["flags"].Select(x => (string)x));
            }
            if (json["text"] != null) {
                foreach (JObject obj in json["text"]) {
                    Text.Add(((string)obj["category"], (string)obj["text"]));
                }
            }
        }

        public Effect SetToggle(IToggle toggle) {
            Toggle = toggle;
            return this;
        }

        public Effect AddExtraAttack(AttackMatcher affects, ExtraAttack extraAttack) {
            ExtraAttacks.Add(new Alterer<ExtraAttack, Attack> {
                affects = affects,
                mod = extraAttack
            });
            return this;
        }

        public Effect AddAttackModifier(AttackMatcher affects, Modifier mod) {
            mod.Name = Name;
            AttackModifiers.Add(new Alterer<Modifier, Attack> {
                affects = affects,
                mod = mod
            });
            return this;
        }

        public Effect AddDamageModifier(AttackMatcher affects, Modifier mod) {
            mod.Name = Name;
            DamageModifiers.Add(new Alterer<Modifier, Attack> {
                affects = affects,
                mod = mod
            });
            return this;
        }

        public Effect AddExtraDice(AttackMatcher affects, int qty, int size) {
            ExtraDice.Add(new Alterer<DiceGroup, Attack> {
                affects = affects,
                mod = new DiceGroup(Name).AddDice(qty, size)
            });
            return this;
        }

        public Effect AddModifier(string affects, Modifier mod) {
            mod.Name = Name;
            Modifiers.Add(new Alterer<Modifier, Statistic> {
                affects = new StatisticMatcher(affects),
                mod = mod
            });
            return this;
        }

        public Effect AddMultiplier(string affects, Multiplier multiplier) {
            multiplier.Name = Name;
            Multipliers.Add(new Alterer<Multiplier, Statistic> {
                affects = new StatisticMatcher(affects),
                mod = multiplier
            });
            return this;
        }

        public Effect AddProficiency(AttackMatcher proficiency) {
            WeaponProficiencies.Add(proficiency);
            return this;
        }

        public Effect AddProficiency(Armor.WeightCategory proficiency) {
            ArmorProficiencies.Add(proficiency);
            return this;
        }

        public Effect AddShieldProficiency(bool tower) {
            ShieldProficiencies.Add(tower);
            return this;
        }

        public Effect AddFlag(string flag) {
            Flags.Add(flag);
            return this;
        }

        public Effect AddText(string category, string text) {
            Text.Add((category, text));
            return this;
        }

        private IEnumerable<AlterType> GetAlterers<AlterType, SubjectType> (
            EffectPool pool,
            Equipped items,
            SubjectType affecting,
            List<Alterer<AlterType, SubjectType>> alterers
        ) where AlterType : IJsonable {
            // In order to prevent infinite recursion, we have to check whether we care about what this effect
            // does before checking whether it applies.
            IEnumerable<AlterType> potential = alterers
                .Where(x => x.affects.Matches(affecting))
                .Select(x => x.mod);
            if (!potential.Any()) {
                return potential;
            }

            return Toggle.On(pool, items)
                ? potential
                : Enumerable.Empty<AlterType>();
        }

        public IEnumerable<ExtraAttack> GetExtraAttacks(EffectPool pool, Equipped items, Attack affecting) {
            return GetAlterers(pool, items, affecting, ExtraAttacks);
        }

        public IEnumerable<Modifier> GetAttackModifiers(EffectPool pool, Equipped items, Attack affecting) {
            return GetAlterers(pool, items, affecting, AttackModifiers);
        }

        public IEnumerable<Modifier> GetDamageModifiers(EffectPool pool, Equipped items, Attack affecting) {
            return GetAlterers(pool, items, affecting, DamageModifiers);
        }

        public IEnumerable<DiceGroup> GetExtraDice(EffectPool pool, Equipped items, Attack affecting) {
            return GetAlterers(pool, items, affecting, ExtraDice);
        }

        public IEnumerable<Modifier> GetThreatModifiers(EffectPool pool, Equipped items, Attack affecting) {
            return GetAlterers(pool, items, affecting, ThreatModifiers);
        }

        public IEnumerable<Multiplier> GetRangeMultipliers(EffectPool pool, Equipped items, Attack affecting) {
            return GetAlterers(pool, items, affecting, RangeMultipliers);
        }

        public IEnumerable<Modifier> GetModifiers(EffectPool pool, Equipped items, Statistic affecting) {
            return GetAlterers(pool, items, affecting, Modifiers);
        }

        public IEnumerable<Multiplier> GetMultipliers(EffectPool pool, Equipped items, Statistic affecting) {
            return GetAlterers(pool, items, affecting, Multipliers);
        }

        public bool Proficient(EffectPool pool, Equipped items, Attack attack) {
            return Toggle.On(pool, items)
                ? WeaponProficiencies.Any(x => x.Matches(attack))
                : false;
        }

        public bool ProficienctWithArmor(EffectPool pool, Equipped items) {
            if (items.DonnedArmor == null) {
                return true; // if you're not wearing any armor, don't take nonproficiency penalties
            }
            if (!ArmorProficiencies.Contains(items.DonnedArmor.Type)) {
                return false;
            }
            return Toggle.On(pool, items);
        }

        public bool ProficienctWithShield(EffectPool pool, Equipped items) {
            if (items.OffHandShield == null) {
                return true;
            }
            bool potential =
                (items.OffHandShield.Tower && ShieldProficiencies.Contains(true)) ||
                (!items.OffHandShield.Tower && ShieldProficiencies.Any());
            return potential && Toggle.On(pool, items);
        }

        public bool HasFlag(string flag) {
            return Flags.Contains(flag);
        }
    }
}
