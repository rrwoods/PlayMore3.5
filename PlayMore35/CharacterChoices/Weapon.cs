using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Effects;
using Newtonsoft.Json.Linq;

namespace PlayMore35.Items {
    public class Weapon : IJsonable {
        public enum Proficiency { SIMPLE, MARTIAL, EXOTIC };
        public enum Effort { LIGHT, ONE_HANDED, TWO_HANDED };
        public enum Hand { ONE_HANDED, PRIMARY_HAND_TWF, OFF_HAND_TWF, TWO_HANDED };
        public enum Range { MELEE, REACH_MELEE, THROWN, BOW, OTHER_PROJECTILE };

        public string Name;
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

        public Weapon(JObject json) {
            Name = (string)json["name"];
            SizeCategory = 0;
            EffortCategory = (Effort)Enum.Parse(typeof(Effort), (string)json["effort"], false);
            if (json["melee"] != null) {
                AttackMode atk = new AttackMode((JObject)json["melee"]);
                bool reach = (json["melee"]["reach"] == null) ? false : (bool)json["melee"]["reach"];
                Melee = (atk, reach);
            }
            if (json["second head"] != null) {
                AttackMode atk = new AttackMode((JObject)json["second head"]);
                bool reach = (json["second head"]["reach"] == null) ? false : (bool)json["second head"]["reach"];
                SecondHead = (atk, reach);
            }
            if (json["thrown"] != null) {
                Thrown = new AttackMode((JObject)json["thrown"]);
            }
            if (json["projectile"] != null) {
                AttackMode atk = new AttackMode((JObject)json["projectile"]);
                int strengthRating = (json["projectile"]["strength rating"] == null) ? -1 : (int)json["projectile"]["strength rating"];
                bool bow = (json["projectile"]["bow"] == null) ? true : (bool)json["projectile"]["bow"];
                Projectile = (atk, bow, strengthRating);
            }
        }

        // When moving a weapon from the definitions list to the available list, we copy it, so that
        // we don't add properties to the base definition.
        public Weapon(Weapon copy) {
            Name = copy.Name;
            SizeCategory = copy.SizeCategory;
            EffortCategory = copy.EffortCategory;

            if (copy.Melee != null) {
                Melee = (new AttackMode(copy.Melee?.atk), (bool) copy.Melee?.reach);
            }
            if (copy.SecondHead != null) {
                SecondHead = (new AttackMode(copy.SecondHead?.atk), (bool) copy.SecondHead?.reach);
            }
            if (copy.Thrown != null) {
                Thrown = new AttackMode(copy.Thrown);
            }
            if (copy.Projectile != null) {
                Projectile = (new AttackMode(copy.Projectile?.atk), (bool) copy.Projectile?.bow, (int) copy.Projectile?.strengthRating);
            }
        }

        public Weapon(string name, int sizeCategory, Effort effort) {
            Name = name;
            SizeCategory = sizeCategory;
            EffortCategory = effort;
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
    }
}
