using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Items;

namespace PlayMore35.Rules {
    public interface IRulesCatalog {
        IReadOnlyDictionary<string, Race> Races { get; }
        IReadOnlyDictionary<string, CharacterClass> Classes { get; }
        IReadOnlyDictionary<string, Feat> Feats { get; }
        IReadOnlyDictionary<string, Weapon> Weapons { get; }
        IReadOnlyDictionary<string, Armor> Armors { get; }
        IReadOnlyDictionary<string, Shield> Shields { get; }
        IReadOnlyDictionary<string, WeaponProperty> WeaponProperties { get; }
        IReadOnlyDictionary<string, ArmorProperty> ArmorProperties { get; }
        IReadOnlyDictionary<string, Item> Items { get; }
        IReadOnlyDictionary<string, ExternalEffect> ExternalEffects { get; }

        Race GetRace(string name);
        CharacterClass GetClass(string name);
        Feat GetFeat(string name);
        Weapon GetWeapon(string name);
        Armor GetArmor(string name);
        Shield GetShield(string name);
        WeaponProperty GetWeaponProperty(string name);
        ArmorProperty GetArmorProperty(string name);
        Item GetItem(string name);
        ExternalEffect GetExternalEffect(string name);

        IEnumerable<string> ChoicesForKey(string choiceDescription);

        IReadOnlyDictionary<string, int> CoreFeatSlots { get; }
        IReadOnlyList<Statistic> CoreStats { get; }
        IReadOnlyList<Effect> CoreEffects { get; }
        IReadOnlyDictionary<string, FlagDefinition> CoreFlags { get; }
        IReadOnlyDictionary<string, ParameterDefinition> CoreParameters { get; }
        JToken CoreRulesJson { get; }
    }
}
