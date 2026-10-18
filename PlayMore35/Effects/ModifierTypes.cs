using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PlayMore35.Effects {
    public static class ModifierTypes {
        public const string ALCHEMICAL = "alchemical";
        public const string ARMOR = "armor";
        public const string CIRCUMSTANCE = "circumstance";
        public const string COMPETENCE = "competence";
        public const string DEFLECTION = "deflection";
        public const string DODGE = "dodge";
        public const string ENHANCEMENT = "enhancement";
        public const string INSIGHT = "insight";
        public const string LUCK = "luck";
        public const string MORALE = "morale";
        public const string NATURAL_ARMOR = "natural armor";
        public const string PROFANE = "profane";
        public const string RACIAL = "racial";
        public const string RESISTANCE = "resistance";
        public const string SACRED = "sacred";
        public const string SHIELD = "shield";
        public const string SIZE = "size";
        public const string KEEN = "keen";
        public const string INTERNAL = "internal"; // only used for "effects" that need to set a value like initiator level,
                                                   // without accidentally double-counting
        public const string UNTYPED = "untyped"; // no type listed in explainer
        public const string BASE = "base"; // "bonus" type for the base number for a statistic, no type listed
        public const string ABILITY_MODIFIER = "ability modifier"; // listed after base with no type

        public static readonly ReadOnlyCollection<string> All = new ReadOnlyCollection<string>(new[] {
            ALCHEMICAL,
            ARMOR,
            CIRCUMSTANCE,
            COMPETENCE,
            DEFLECTION,
            DODGE,
            ENHANCEMENT,
            INSIGHT,
            LUCK,
            MORALE,
            NATURAL_ARMOR,
            PROFANE,
            RACIAL,
            RESISTANCE,
            SACRED,
            SHIELD,
            SIZE,
            INTERNAL,
            KEEN,
            UNTYPED,
            BASE,
            ABILITY_MODIFIER
        });

        private static readonly HashSet<string> ValidSet =
            new HashSet<string>(All, StringComparer.OrdinalIgnoreCase);

        public static bool IsValid(string type) {
            return !string.IsNullOrWhiteSpace(type) && ValidSet.Contains(type.Trim());
        }
    }
}
