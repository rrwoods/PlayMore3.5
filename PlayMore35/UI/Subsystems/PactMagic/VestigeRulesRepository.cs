using System;
using System.Collections.Generic;
using System.Linq;
using PlayMore35.Subsystems;

namespace PlayMore35.UI.Subsystems.PactMagic {
    public class VestigeAbilityData {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "Passive";
        public string Text { get; set; } = "";
    }

    public class VestigeRulesData {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public int BindingDC { get; set; }
        public string Sign { get; set; } = "";
        public string Influence { get; set; } = "";
        public List<VestigeAbilityData> Abilities { get; set; } = new List<VestigeAbilityData>();
    }

    public static class VestigeRulesRepository {
        private static readonly Dictionary<string, VestigeRulesData> _catalog = new Dictionary<string, VestigeRulesData>(StringComparer.OrdinalIgnoreCase);

        static VestigeRulesRepository() {
            AddVestige(new VestigeRulesData {
                Name = "Amon",
                Level = 1,
                BindingDC = 20,
                Sign = "Ram horns curl from your head.",
                Influence = "You despise fire and holy symbols of sun deities. You must extinguish unprotected flames and refuse to carry sun symbols.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Darkvision 60 ft.", Type = "Passive", Text = "You can see in the dark up to 60 ft." },
                    new VestigeAbilityData { Name = "Fire Breath", Type = "Standard", Text = "Breathe line of fire 10 ft/level (max 50 ft), dealing 1d6 fire per level, Reflex half. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Ram Attack", Type = "Passive", Text = "Natural ram attack dealing 1d6 + 1.5x Str bonus (+1d8 on charge as long as showing Amon's sign)." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Aym",
                Level = 1,
                BindingDC = 15,
                Sign = "A starburst rash covers your skin, and your teeth turn to yellow gold.",
                Influence = "You begrudge every coin or item of value given to another. You must give a coin to every dwarf you meet within 1 minute of learning their name.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Dwarven Step", Type = "Passive", Text = "No movement penalty for wearing medium or heavy armor." },
                    new VestigeAbilityData { Name = "Halo of Fire", Type = "Passive", Text = "Adjacent attackers striking with natural weapons or non-reach take 1d6 fire damage." },
                    new VestigeAbilityData { Name = "Improved Sunder", Type = "Feat", Text = "Gain Improved Sunder feat benefits without provoking attacks of opportunity." },
                    new VestigeAbilityData { Name = "Medium Armor Proficiency", Type = "Proficiency", Text = "Proficient with all medium armors." },
                    new VestigeAbilityData { Name = "Resistance to Fire 10", Type = "Passive", Text = "Gain Fire Resistance 10." },
                    new VestigeAbilityData { Name = "Ruinous Attack", Type = "Passive", Text = "Melee attacks deal double damage to objects and crystalline structures." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Leraje",
                Level = 1,
                BindingDC = 15,
                Sign = "Skin and hair become pale green; eyes turn yellow.",
                Influence = "You must defend your physical prowess; you must challenge anyone who questions your marksmanship or physical ability.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Hide Bonus", Type = "Passive", Text = "Gain +1 competence bonus per level on Hide checks." },
                    new VestigeAbilityData { Name = "Low-Light Vision", Type = "Passive", Text = "See twice as far in starlight or torchlight." },
                    new VestigeAbilityData { Name = "Precise Shot", Type = "Feat", Text = "Shoot into melee without the standard -4 penalty." },
                    new VestigeAbilityData { Name = "Ricochet", Type = "Standard", Text = "Ranged attack with bow bounces to strike a secondary target within 15 ft." },
                    new VestigeAbilityData { Name = "Bow Proficiencies", Type = "Proficiency", Text = "Proficient with composite longbow, composite shortbow, longbow, and shortbow." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Naberius",
                Level = 1,
                BindingDC = 15,
                Sign = "Voice sounds raspy and croaking like a crow or raven.",
                Influence = "You must speak your mind freely and challenge any perceived insult; you cannot tell an intentional untruth without a Bluff check.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Disguise Self", Type = "Standard", Text = "Change physical appearance as the spell at will (standard action)." },
                    new VestigeAbilityData { Name = "Faster Ability Healing", Type = "Passive", Text = "Recover 1 point of lost ability score per hour, and 1 drained point per day." },
                    new VestigeAbilityData { Name = "Naberius's Skills", Type = "Passive", Text = "Can use any untrained skill as if trained, using effective level as ranks." },
                    new VestigeAbilityData { Name = "Persuasive Words", Type = "Standard", Text = "Command as the spell (DC 10 + 1/2 level + Cha). Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Silver Tongue", Type = "Passive", Text = "Take 10 on Bluff and Diplomacy; perform rushed Diplomacy as a standard action with no penalty." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Ronove",
                Level = 1,
                BindingDC = 15,
                Sign = "Mouth and eyes look gaunt; face is sunken and deathly pale.",
                Influence = "Dislikes physical affection; must reject any embrace, touch, or healing touch unless dying.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Magic & Cold Iron Fists", Type = "Passive", Text = "Unarmed strikes count as magic and cold iron weapons." },
                    new VestigeAbilityData { Name = "Far Hand", Type = "Standard", Text = "Mage hand at will; push or pull objects up to 15 lbs." },
                    new VestigeAbilityData { Name = "Ronove's Fists", Type = "Passive", Text = "Unarmed damage scales as a monk of your level." },
                    new VestigeAbilityData { Name = "Sprint", Type = "Passive", Text = "+10 ft enhancement bonus to base land speed." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Dahlver-Nar",
                Level = 2,
                BindingDC = 17,
                Sign = "Dozens of small teeth emerge from your skin in random locations.",
                Influence = "Paranoid and suspicious; must sleep with weapon drawn and cannot accept food from strangers.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Mad Soul", Type = "Passive", Text = "Immune to madness, confusion, insanity, and non-lethal damage." },
                    new VestigeAbilityData { Name = "Maddening Moan", Type = "Standard", Text = "Creatures within 30 ft must save or be shaken for 1 round. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Natural Armor", Type = "Passive", Text = "+1 natural armor bonus per 4 levels." },
                    new VestigeAbilityData { Name = "Shield Self", Type = "Standard", Text = "Designate one creature within 30 ft; it takes half the damage you receive." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Haagenti",
                Level = 2,
                BindingDC = 17,
                Sign = "Face becomes bloated and porcine with thick tusks curving from jaw.",
                Influence = "Extreme gluttony; cannot turn down food or drink offered to you.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Confusing Touch", Type = "Standard", Text = "Touch attack confuses target for 1 round, no save." },
                    new VestigeAbilityData { Name = "Immunity to Transformation", Type = "Passive", Text = "Immune to polymorph, petrification, and shapechange effects." },
                    new VestigeAbilityData { Name = "Shield & Axe Proficiency", Type = "Proficiency", Text = "Proficient with shields, greataxe, and battleaxe." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Malphas",
                Level = 2,
                BindingDC = 15,
                Sign = "Teeth and tongue turn pitch black.",
                Influence = "Greedy for secrets; must open any sealed letter or unlocked container you see.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Bird's Eye Viewing", Type = "Standard", Text = "Summon a raven or dove scout and see/hear through its senses." },
                    new VestigeAbilityData { Name = "Invisibility", Type = "Standard", Text = "Turn invisible as the spell. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Poison Use", Type = "Passive", Text = "Never accidentally poison yourself when applying poison to weapons." },
                    new VestigeAbilityData { Name = "Sudden Strike", Type = "Passive", Text = "+1d6 sudden strike damage against flat-footed targets (+1d6 per 4 levels)." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Savnok",
                Level = 2,
                BindingDC = 20,
                Sign = "Shards of iron armor fuse into your skin, creating a metallic clanking sound.",
                Influence = "Must not yield or retreat in combat; cannot withdraw or fight defensively.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Call Armor", Type = "Standard", Text = "Summon heavy full plate armor made of iron directly onto your body." },
                    new VestigeAbilityData { Name = "Heavy Armor Proficiency", Type = "Proficiency", Text = "Proficient with heavy armor." },
                    new VestigeAbilityData { Name = "Move Ally", Type = "Swift", Text = "Swap positions with an adjacent willing ally." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Focalor",
                Level = 3,
                BindingDC = 20,
                Sign = "You weep continuously with salt tears, and your breath smells of sea brine.",
                Influence = "You become sullen and melancholy, grieving over minor setbacks.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Aura of Sadness", Type = "Passive", Text = "Adjacent enemies take -2 penalty on attacks, saves, and checks." },
                    new VestigeAbilityData { Name = "Focalor's Breath", Type = "Standard", Text = "Blind a creature within 30 ft with a gust of wind for 1 round. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Lightning Strike", Type = "Standard", Text = "Call 3d6 electricity damage strike from sky on target within 10 ft/level." },
                    new VestigeAbilityData { Name = "Water Breathing", Type = "Passive", Text = "Breathe underwater freely." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Karsus",
                Level = 3,
                BindingDC = 21,
                Sign = "Blood leaks from your cuticles when you cast a spell or use a vestige ability.",
                Influence = "You become arrogant and condescending, especially towards arcanists and spellcasters.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Karsus's Senses", Type = "Passive", Text = "Detect magic at will as a continuous effect." },
                    new VestigeAbilityData { Name = "Karsus's Touch", Type = "Standard", Text = "Dispel magic by touch as the spell. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Magic Item Use", Type = "Passive", Text = "Activate spell trigger and spell completion items as a wizard." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Paimon",
                Level = 3,
                BindingDC = 20,
                Sign = "A third eye opens vertically in the middle of your forehead.",
                Influence = "Refuse to dirty your hands; demand utmost etiquette and politeness from companions.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Dance of Death", Type = "Standard", Text = "Move up to speed and make melee attacks against every creature passed. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Paimon's Blades", Type = "Proficiency", Text = "Proficient with rapier and short sword; gain Weapon Finesse." },
                    new VestigeAbilityData { Name = "Uncanny Dodge", Type = "Passive", Text = "Retain Dex bonus to AC even when flat-footed." },
                    new VestigeAbilityData { Name = "Whirlwind Attack", Type = "Feat", Text = "Gain benefits of Whirlwind Attack feat." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Andras",
                Level = 4,
                BindingDC = 24,
                Sign = "Your head sprouts black raven or owl feathers, and your voice takes on a harsh edge.",
                Influence = "Cannot back down from personal challenges or duels of honor.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Mount", Type = "Standard", Text = "Summon a heavy warhorse or dire wolf mount at will." },
                    new VestigeAbilityData { Name = "Sow Discord", Type = "Standard", Text = "Force a target to attack its allies for 1 round. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Sure Blows", Type = "Passive", Text = "Threaten critical hits with any weapon on 19-20." },
                    new VestigeAbilityData { Name = "Weapon Proficiency", Type = "Proficiency", Text = "Proficient with greatsword, rapier, and lance." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Buer",
                Level = 4,
                BindingDC = 20,
                Sign = "Your feet become cloven hooves; your eyes turn goat-like.",
                Influence = "You abhor unnecessary violence; must seek peaceful resolution before drawing steel.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Buer's Purity", Type = "Passive", Text = "Immune to all poisons and natural diseases." },
                    new VestigeAbilityData { Name = "Healing Gift", Type = "Standard", Text = "Cure 1d8 + 1/level damage by touch at will. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Delay Diseases & Toxins", Type = "Passive", Text = "Temporarily halt progression of poisons in allies." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Eurynome",
                Level = 4,
                BindingDC = 21,
                Sign = "Your skin becomes translucent and wet, weeping foul seawater.",
                Influence = "Cannot break an oath once sworn, no matter how inconvenient.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Animal Friend", Type = "Passive", Text = "Animals never attack you unless magically compelled." },
                    new VestigeAbilityData { Name = "Damage Reduction", Type = "Passive", Text = "Gain DR 2/lawful or DR 5/lawful (scales with level)." },
                    new VestigeAbilityData { Name = "Water Walking", Type = "Passive", Text = "Walk on liquid surfaces as if solid ground." },
                    new VestigeAbilityData { Name = "Warhammer", Type = "Standard", Text = "Manifest a +1 warhammer made of hardened water." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Acererak",
                Level = 5,
                BindingDC = 25,
                Sign = "All your teeth turn into sparkling cut gems.",
                Influence = "Fascinated by tombs, undead, and the trapping of souls; utterly indifferent to living suffering.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Detect Undead", Type = "Passive", Text = "Detect undead continuously as the spell." },
                    new VestigeAbilityData { Name = "Hide from Undead", Type = "Passive", Text = "Mindless undead cannot perceive you." },
                    new VestigeAbilityData { Name = "Lich's Touch", Type = "Standard", Text = "Deal 1d6 cold + paralyze target on touch attack. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Paralyzing Touch", Type = "Passive", Text = "Immunity to paralysis, cold, and electric damage." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Balam",
                Level = 5,
                BindingDC = 25,
                Sign = "Your eyes burn with cold silver light; your shadow behaves independently.",
                Influence = "Distrustful of prophecy; must voice skepticism whenever divination is used.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Cold Rays", Type = "Standard", Text = "Fire ray dealing 2d6 cold damage. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Cunning Prescience", Type = "Immediate", Text = "Add +1 to +5 insight bonus to AC or saving throw." },
                    new VestigeAbilityData { Name = "Prescience", Type = "Passive", Text = "Gain +2 insight bonus on initiative, Reflex saves, and AC." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Dantalion",
                Level = 5,
                BindingDC = 25,
                Sign = "A multitude of ghostly human faces hover faintly around your head.",
                Influence = "Curious about everything; cannot walk past a closed room without peeking inside.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Awe of Dantalion", Type = "Standard", Text = "Enemies within 30 ft cannot attack you for 1 round. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Dantalion's Read", Type = "Standard", Text = "Read surface thoughts as detect thoughts." },
                    new VestigeAbilityData { Name = "Thought Travel", Type = "Standard", Text = "Teleport up to 5 ft per level." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Chupoclops",
                Level = 6,
                BindingDC = 25,
                Sign = "A translucent phantom web trails behind you wherever you walk.",
                Influence = "Pessimistic and defeatist; must predict failure for every group plan.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Aura of Despair", Type = "Passive", Text = "Enemies within 10 ft take -2 penalty on saves and attacks." },
                    new VestigeAbilityData { Name = "Ghost Touch", Type = "Passive", Text = "Attacks strike incorporeal and ethereal creatures normally." },
                    new VestigeAbilityData { Name = "Pounce", Type = "Passive", Text = "Make full attack at the end of a charge." },
                    new VestigeAbilityData { Name = "Soulsense", Type = "Passive", Text = "Blindsight 30 ft against living and undead creatures." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Haures",
                Level = 6,
                BindingDC = 25,
                Sign = "Your eyes weep blood when you are surprised or injured.",
                Influence = "Obsessed with death and morbid jokes; cannot laugh at innocent humor.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Inaccessible Mind", Type = "Passive", Text = "Immune to mind-detecting and scrying effects as mind blank." },
                    new VestigeAbilityData { Name = "Major Image", Type = "Standard", Text = "Create complex illusions as major image at will. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Phantasmal Killer", Type = "Standard", Text = "Manifest target's worst fear as phantasmal killer." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Eligor",
                Level = 7,
                BindingDC = 27,
                Sign = "A heavy saddle and barding imprint appears across your skin.",
                Influence = "Despises dishonorable tactics; refuses to attack an unarmed or prone foe.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Eligor's Armor", Type = "Passive", Text = "+3 enhancement bonus to natural armor; gain heavy horse mount." },
                    new VestigeAbilityData { Name = "Eligor's Strength", Type = "Passive", Text = "+4 enhancement bonus to Strength." },
                    new VestigeAbilityData { Name = "Ride Bonus", Type = "Passive", Text = "+1 bonus per level on Ride checks." }
                }
            });

            AddVestige(new VestigeRulesData {
                Name = "Halphax",
                Level = 8,
                BindingDC = 30,
                Sign = "Your skin becomes stony and gray, etched with masonry lines.",
                Influence = "Deeply stubborn; once you take a physical stance or position, you refuse to yield it.",
                Abilities = new List<VestigeAbilityData> {
                    new VestigeAbilityData { Name = "Damage Reduction", Type = "Passive", Text = "Gain DR 10/adamantine." },
                    new VestigeAbilityData { Name = "Iron Wall", Type = "Standard", Text = "Summon wall of iron at will. Recharge: 5 rounds." },
                    new VestigeAbilityData { Name = "Secure Shelter", Type = "Standard", Text = "Conjure fortified stone tower as Leomund's secure shelter." }
                }
            });
        }

        private static void AddVestige(VestigeRulesData data) {
            _catalog[data.Name] = data;
        }

        public static VestigeRulesData GetRulesData(SubsystemElement element) {
            if (element != null && _catalog.TryGetValue(element.Name, out var data)) {
                return data;
            }

            // Fallback dynamically from element properties/effects
            int lvl = element?.GetProperty<int>("level", 1) ?? 1;
            int dc = element?.GetProperty<int>("binding DC", 15) ?? 15;
            var fallback = new VestigeRulesData {
                Name = element?.Name ?? "",
                Level = lvl,
                BindingDC = dc,
                Sign = $"Manifestation sign of {element?.Name}.",
                Influence = $"Behavioral influence of {element?.Name}."
            };

            if (element != null) {
                var abilities = element.GetEffects("abilities");
                foreach (var ab in abilities) {
                    var desc = ab.Text.Count > 0 ? ab.Text[0].Description : "";
                    fallback.Abilities.Add(new VestigeAbilityData {
                        Name = ab.Name,
                        Type = "Granted",
                        Text = desc
                    });
                }
            }

            return fallback;
        }
    }
}
