using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

using PlayMore35.Items;
using static PlayMore35.Effects.Constants;

namespace PlayMore35.Effects {
    public class Effect : IJsonable {
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

        public readonly List<string> WeaponFamiliarity = new List<string>();
        private readonly List<AttackMatcher> WeaponProficiencies = new List<AttackMatcher>();

        // This could be done as an ArmorMatcher, but that feels too heavyweight right now.  If it turns out
        // armor proficiences get any more complicated than this, though, I will unjankify this and make an ArmorMatcher instead.
        private readonly List<Armor.WeightCategory> ArmorProficiencies = new List<Armor.WeightCategory>();
        private readonly List<string> ExoticArmorProficiencies = new List<string>();

        // See above not about armor proficiencies.
        private readonly List<bool> ShieldProficiencies = new List<bool>(); // false = shield prof, true = shield+tower prof
        private readonly List<string> ExoticShieldProficiencies = new List<string>();

        private readonly List<string> Flags = new List<string>();
        public readonly List<Text> Text = new List<Text>();

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
            json.AddList("extra attacks", ExtraAttacks);
            json.AddList("attack", AttackModifiers);
            json.AddList("damage", DamageModifiers);
            json.AddList("dice", ExtraDice);
            json.AddList("threat", ThreatModifiers);
            json.AddList("range", RangeMultipliers);
            json.AddList("modifiers", Modifiers);
            json.AddList("multipliers", Multipliers);
            json.AddList("weapon familiarity", WeaponFamiliarity);
            json.AddList("weapon proficiencies", WeaponProficiencies);
            if (ArmorProficiencies.Any()) {
                json.Add("armor proficiencies", new JArray(ArmorProficiencies));
            }
            if (ExoticArmorProficiencies.Any()) {
                json.Add("exotic armor proficiencies", new JArray(ExoticArmorProficiencies));
            }
            if (ShieldProficiencies.Any()) {
                json.Add("shield proficiencies", new JArray(ShieldProficiencies.Select(x => x ? "tower" : "normal")));
            }
            if (ExoticShieldProficiencies.Any()) {
                json.Add("exotic shield proficiencies", new JArray(ExoticShieldProficiencies));
            }
            json.AddList("flags", Flags);
            json.AddList("text", Text);
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
            if (json["weapon familiarity"] != null) {
                WeaponFamiliarity.AddRange(json["weapon familiarity"].Select(x => (string)x));
            }
            if (json["weapon proficiencies"] != null) {
                WeaponProficiencies.AddRange(json["weapon proficiencies"].Select(x => new AttackMatcher((JObject)x)));
            }
            if (json["armor proficiencies"] != null) {
                ArmorProficiencies.AddRange(json["armor proficiencies"].Select(x =>
                    (Armor.WeightCategory) Enum.Parse(typeof(Armor.WeightCategory), (string)x, false)
                ));
            }
            if (json["exotic armor proficiencies"] != null) {
                ExoticArmorProficiencies.AddRange(json["exotic armor proficiencies"].Select(x => (string)x));
            }
            if (json["shield proficiencies"] != null) {
                ShieldProficiencies.AddRange(json["shield proficiencies"].Select(x => x.Equals("tower")));
            }
            if (json["exotic shield proficiencies"] != null) {
                ExoticShieldProficiencies.AddRange(json["exotic shield proficiencies"].Select(x => (string)x));
            }
            if (json["flags"] != null) {
                Flags.AddRange(json["flags"].Select(x => (string)x));
            }
            if (json["text"] != null) {
                Text.AddRange(json["text"].Select(x => new Text((JObject)x)));
            }
        }

        public Effect ApplyChoice(string choice) {
            Effect copy = new Effect(Name) {
                Toggle = Toggle
            };

            copy.ExtraAttacks.AddRange(ExtraAttacks.Select(x => x.ApplyChoice(choice)));
            copy.AttackModifiers.AddRange(AttackModifiers.Select(x => x.ApplyChoice(choice)));
            copy.DamageModifiers.AddRange(DamageModifiers.Select(x => x.ApplyChoice(choice)));
            copy.ExtraDice.AddRange(ExtraDice.Select(x => x.ApplyChoice(choice)));
            copy.ThreatModifiers.AddRange(ThreatModifiers.Select(x => x.ApplyChoice(choice)));
            copy.RangeMultipliers.AddRange(RangeMultipliers.Select(x => x.ApplyChoice(choice)));
            copy.Modifiers.AddRange(Modifiers.Select(x => x.ApplyChoice(choice)));
            copy.Multipliers.AddRange(Multipliers.Select(x => x.ApplyChoice(choice)));

            copy.WeaponFamiliarity.AddRange(WeaponFamiliarity.Select(x => x.Replace("$", choice)));
            copy.WeaponProficiencies.AddRange(WeaponProficiencies.Select(x => (AttackMatcher) x.ApplyChoice(choice)));

            copy.ArmorProficiencies.AddRange(ArmorProficiencies);
            copy.ExoticArmorProficiencies.AddRange(ExoticArmorProficiencies.Select(x => x.Replace("$", choice)));

            copy.ShieldProficiencies.AddRange(ShieldProficiencies);
            copy.ExoticShieldProficiencies.AddRange(ExoticShieldProficiencies.Select(x => x.Replace("$", choice)));

            copy.Flags.AddRange(Flags.Select(x => x.Replace("$", choice)));
            copy.Text.AddRange(Text.Select(x => x.ApplyChoice(choice)));

            return copy;
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

        public Effect AddFamiliarity(string weapon) {
            WeaponFamiliarity.Add(weapon);
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

        public Effect AddExoticArmorProficiency(string armor) {
            ExoticArmorProficiencies.Add(armor);
            return this;
        }

        public Effect AddShieldProficiency(bool tower) {
            ShieldProficiencies.Add(tower);
            return this;
        }

        public Effect AddExoticShieldProficiency(string shield) {
            ExoticShieldProficiencies.Add(shield);
            return this;
        }

        public Effect AddFlag(string flag) {
            Flags.Add(flag);
            return this;
        }

        public Effect AddText(string category, string text) {
            Text.Add(new Text { Category = category, Description = text });
            return this;
        }

        public Statistic Uses() {
            foreach (Text text in Text) {
                if (text.RechargeTime != null) {
                    if (text.StaticUses != 0) {
                        AddModifier(this.Name + " uses", new Modifier(text.StaticUses, UNTYPED));
                    }
                    return (new Statistic(Name + " uses"));
                }
            }
            return null;
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

        public bool ProficientWithArmor(EffectPool pool, Equipped items) {
            if (items.DonnedArmor == null) {
                return true; // if you're not wearing any armor, don't take nonproficiency penalties
            }
            if (items.DonnedArmor.Exotic) {
                if (!ExoticArmorProficiencies.Contains(items.DonnedArmor.Name)) {
                    return false;
                }
            } else {
                if (!ArmorProficiencies.Contains(items.DonnedArmor.Type)) {
                    return false;
                }
            }
            return Toggle.On(pool, items);
        }

        public bool ProficientWithShield(EffectPool pool, Equipped items) {
            if (items.OffHandShield == null) {
                return true;
            }
            if (items.OffHandShield.Exotic) {
                if (!ExoticShieldProficiencies.Contains(items.OffHandShield.Name)) {
                    return false;
                }
            } else if (items.OffHandShield.Tower) {
                if (!ShieldProficiencies.Contains(true)) {
                    return false;
                }
            } else {
                if (!ShieldProficiencies.Any()) {
                    return false;
                }
            }
            return Toggle.On(pool, items);
        }

        public bool HasFlag(string flag) {
            return Flags.Contains(flag);
        }
    }
}
