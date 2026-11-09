using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Items {
    public class Weapon : IJsonable {
        public enum Proficiency { SIMPLE, MARTIAL, ONE_HANDED_EXOTIC, EXOTIC };
        public enum Effort { LIGHT, ONE_HANDED, TWO_HANDED };
        // 2-hand 2-weapon is possible with armor spikes as your "off hand" weapon.
        public enum Hand { ONE_HANDED, PRIMARY_HAND_TWF, OFF_HAND_TWF, TWO_HANDED, TWO_HANDED_TWF };
        public enum Range { MELEE, REACH_MELEE, THROWN, BOW, OTHER_PROJECTILE };

        public string Name;
        public decimal Weight;
        public List<string> Tags = new List<string>(); // not serialized -- applied after loading
        public int SizeCategory; // not serialized -- applied after loading
        public Effort EffortCategory;

        public (AttackMode atk, bool bow, int strengthRating)? Projectile = null;
        public AttackMode Thrown = null;
        public (AttackMode atk, bool reach)? Melee = null;
        public (AttackMode atk, bool reach)? SecondHead = null;

        public JToken ToJson() {
            JObject json = new JObject(
                new JProperty("name", Name),
                new JProperty("weight", Weight),
                new JProperty("effort", EffortCategory)
            );
            if (Melee != null) {
                json["reach"] = Melee?.reach;
                json["melee"] = Melee?.atk.ToJson();
            }
            if (SecondHead != null) {
                json["second head"] = SecondHead?.atk.ToJson();
            }
            if (Thrown != null) {
                json["thrown"] = Thrown.ToJson();
            }
            if (Projectile != null) {
                if (Projectile?.strengthRating != -1) {
                    json["strength rating"] = Projectile?.strengthRating;
                }
                json["bow"] = Projectile?.bow;
                json["projectile"] = Projectile?.atk.ToJson();
            }
            return json;
        }

        public Weapon(
            string name,
            decimal weight = 0m,
            Effort effortCategory = Effort.LIGHT,
            int sizeCategory = 0,
            IEnumerable<string>? tags = null,
            (AttackMode atk, bool reach)? melee = null,
            (AttackMode atk, bool reach)? secondHead = null,
            AttackMode? thrown = null,
            (AttackMode atk, bool bow, int strengthRating)? projectile = null) {
            Name = name ?? "";
            Weight = weight;
            EffortCategory = effortCategory;
            SizeCategory = sizeCategory;
            if (tags != null) {
                Tags.AddRange(tags);
            }
            Melee = melee;
            SecondHead = secondHead;
            Thrown = thrown;
            Projectile = projectile;
        }

        public Weapon(JObject json)
            : this(
                (string)json["name"],
                (decimal)json["weight"],
                (Effort)Enum.Parse(typeof(Effort), (string)json["effort"], false),
                0,
                null,
                ParseMelee(json["melee"]),
                ParseMelee(json["second head"]),
                json["thrown"] != null ? new AttackMode((JObject)json["thrown"]) : null,
                ParseProjectile(json["projectile"])) {
        }

        private static (AttackMode atk, bool reach)? ParseMelee(JToken token) {
            if (token == null) return null;
            AttackMode atk = new AttackMode((JObject)token);
            bool reach = (token["reach"] == null) ? false : (bool)token["reach"];
            return (atk, reach);
        }

        private static (AttackMode atk, bool bow, int strengthRating)? ParseProjectile(JToken token) {
            if (token == null) return null;
            AttackMode atk = new AttackMode((JObject)token);
            int strengthRating = (token["strength rating"] == null) ? -1 : (int)token["strength rating"];
            bool bow = (token["bow"] == null) ? true : (bool)token["bow"];
            return (atk, bow, strengthRating);
        }

        // When moving a weapon from the definitions list to the available list, we copy it, so that
        // we don't add properties to the base definition.
        public Weapon(Weapon definition)
            : this(
                (definition ?? throw new ArgumentNullException(nameof(definition))).Name,
                definition.Weight,
                definition.EffortCategory,
                definition.SizeCategory,
                definition.Tags,
                definition.Melee != null ? (new AttackMode(definition.Melee.Value.atk), definition.Melee.Value.reach) : ((AttackMode atk, bool reach)?)null,
                definition.SecondHead != null ? (new AttackMode(definition.SecondHead.Value.atk), definition.SecondHead.Value.reach) : ((AttackMode atk, bool reach)?)null,
                definition.Thrown != null ? new AttackMode(definition.Thrown) : null,
                definition.Projectile != null ? (new AttackMode(definition.Projectile.Value.atk), definition.Projectile.Value.bow, definition.Projectile.Value.strengthRating) : ((AttackMode atk, bool bow, int strengthRating)?)null) {
        }

        public bool CanBeThrown() {
            if (Thrown != null) {
                return true;
            }
            if (Melee == null) {
                return false;
            }
            return Melee?.atk.RangeIncrement > 0;
        }

        public IEnumerable<Effect> GetGlobalEffects() {
            IEnumerable<Effect> effects = Enumerable.Empty<Effect>();
            if (Projectile != null) {
                effects = effects.Concat(Projectile?.atk.GetGlobalEffects());
            }
            if (Thrown != null) {
                effects = effects.Concat(Thrown.GetGlobalEffects());
            }
            if (Melee != null) {
                effects = effects.Concat(Melee?.atk.GetGlobalEffects());
            }
            if (SecondHead != null) {
                effects = effects.Concat(SecondHead?.atk.GetGlobalEffects());
            }
            return effects;
        }

        public Proficiency GetProficiency() {
            Proficiency proficiency = Proficiency.EXOTIC;
            if (Melee != null && Melee?.atk.ProficencyCategory < proficiency) {
                proficiency = (Proficiency) Melee?.atk.ProficencyCategory;
            }
            if (Projectile != null && Projectile?.atk.ProficencyCategory < proficiency) {
                proficiency = (Proficiency) Projectile?.atk.ProficencyCategory;
            }
            if (Thrown != null && Thrown?.ProficencyCategory < proficiency) {
                proficiency = (Proficiency) Thrown?.ProficencyCategory;
            }
            if (proficiency == Proficiency.ONE_HANDED_EXOTIC) {
                proficiency = Proficiency.EXOTIC;
            }
            return proficiency;
        }
    }
}
