using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using PlayMore35.CharacterChoices;
using PlayMore35.Effects;
using PlayMore35.Global;
using PlayMore35.Items;
using PlayMore35.Subsystems;

namespace PlayMore35.Rules {
    [AuthorizedDomainApi("Intentional shared rules catalog contract")]
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
        IReadOnlyDictionary<string, SubsystemDefinition> Subsystems { get; }

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
        SubsystemDefinition GetSubsystem(string name);

        bool TryGetRace(string name, [NotNullWhen(true)] out Race? result);
        bool TryGetClass(string name, [NotNullWhen(true)] out CharacterClass? result);
        bool TryGetFeat(string name, [NotNullWhen(true)] out Feat? result);
        bool TryGetWeapon(string name, [NotNullWhen(true)] out Weapon? result);
        bool TryGetArmor(string name, [NotNullWhen(true)] out Armor? result);
        bool TryGetShield(string name, [NotNullWhen(true)] out Shield? result);
        bool TryGetWeaponProperty(string name, [NotNullWhen(true)] out WeaponProperty? result);
        bool TryGetArmorProperty(string name, [NotNullWhen(true)] out ArmorProperty? result);
        bool TryGetItem(string name, [NotNullWhen(true)] out Item? result);
        bool TryGetExternalEffect(string name, [NotNullWhen(true)] out ExternalEffect? result);
        bool TryGetSubsystem(string name, [NotNullWhen(true)] out SubsystemDefinition? result);

        Feat? FindFeatByChoiceDescriptor(ChoiceDescriptor descriptor);

        string ResolveTemplate(string template, IReadOnlyDictionary<string, object> choices);

        IEnumerable<string> ChoicesForKey(string choiceDescription);

        IReadOnlyDictionary<string, int> CoreFeatSlots { get; }
        IReadOnlyList<Statistic> CoreStats { get; }
        IReadOnlyList<Effect> CoreEffects { get; }
        IReadOnlyDictionary<string, FlagDefinition> CoreFlags { get; }
        IReadOnlyDictionary<string, ParameterDefinition> CoreParameters { get; }
        JToken CoreRulesJson { get; }
    }
}
