using Kingmaker.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingmaker.Blueprints.Validation;
using Kingmaker.Blueprints.Items.Weapons;
using UnityEngine;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace FumisCodex
{
    [HarmonyLib.HarmonyPatch(typeof(LevelUpController), "Commit")]
    public static class WeaponFocusGroupsPatch
    {
        static void Postfix(LevelUpController __instance)
        {
            if (!Settings.StateManager.State.cheatCombineParametrizedFeats)
                return;

            Main.DebugLog("Commit: Char=" + __instance.Unit?.CharacterName);

            if (__instance.State == null || __instance.State.Selections == null)
                return;

            foreach (var selection in __instance.State.Selections)
            {
                if (selection.SelectedItem?.Feature?.AssetGuid == null || selection.SelectedItem.Param?.WeaponCategory == null)
                    continue;

                Main.DebugLog("Selection: " + selection.SelectedItem.Name);

                if (guid_list.Contains(selection.SelectedItem.Feature.AssetGuid))
                    Process(__instance.Unit, selection.SelectedItem.Feature.AssetGuid, selection.SelectedItem.Param.WeaponCategory.Value);
            }
        }

        static void Process(UnitDescriptor unit, string guid, WeaponCategory category)
        {
            Main.DebugLog("Category: " + category.ToString());

            var group = GetGroup(category);
            foreach (var sub in group)
            {
                Main.DebugLog("Try give Feat: " + sub.ToString());
                GiveFeat(unit, guid, sub);
            }
        }

        public static void GiveFeat(UnitDescriptor unit, string Guid, WeaponCategory category)
        {
            BlueprintFeature feature = Kingmaker.Cheats.Utilities.GetBlueprintByGuid<BlueprintFeature>(Guid);
            FeatureParam param = new FeatureParam(new WeaponCategory?(category));

            if (unit.GetFeature(feature, param) == null)
                unit.AddFact(Kingmaker.Cheats.Utilities.GetBlueprintByGuid<BlueprintFeature>(Guid), null, param);
        }

        public static WeaponCategory[] GetGroup(WeaponCategory category)
        {
            foreach (WeaponCategory[] group in groups)
            {
                if (group.Contains(category))
                    return group;
            }
            return Array.Empty<WeaponCategory>();
        }
        
        static string[] guid_list = new string[]
        {
            "1e1f627d26ad36f43bbd26cc2bf8ac7e", //Weapon Focus
            "09c9e82965fb4334b984a1e9df3bd088", //Weapon Focus, Greater
            //"47b352ea0f73c354aba777945760b441", //FencingGrace
            "f4201c85a991369408740c6888362e20", //ImprovedCritical
            //"697d64669eb2c0543abb9c9b07998a38", //SlashingGrace
            "c0b4ec0175e3ff940a45fc21f318a39a", //SwordSaintChosenWeaponFeature //not all
            "38ae5ac04463a8947b7c06a6c72dd6bb", //WeaponMasteryParametrized
            "31470b17e8446ae4ea0dacd6c5817d86", //WeaponSpecialization
            "7cf5edc65e785a24f9cf93af987d66b3"  //WeaponSpecializationGreater
        };
        
        static WeaponCategory[][] groups = new WeaponCategory[][] 
        {
            new WeaponCategory[]    //thrown + slings
            {
                WeaponCategory.Bomb,
                WeaponCategory.Dart,
                WeaponCategory.Javelin,
                WeaponCategory.Sling,
                WeaponCategory.SlingStaff
            },

            new WeaponCategory[]    //two-handed swords
            {
                WeaponCategory.Scythe,
                WeaponCategory.DoubleSword,
                WeaponCategory.ElvenCurvedBlade,
                WeaponCategory.Greatsword,
                WeaponCategory.Falchion
            },

            new WeaponCategory[]    //one-handed swords
            {
                WeaponCategory.Sickle,
                WeaponCategory.Estoc,
                WeaponCategory.Falcata,
                WeaponCategory.DuelingSword,
                WeaponCategory.Rapier,
                WeaponCategory.Scimitar,
                WeaponCategory.BastardSword,
                WeaponCategory.Longsword,
                WeaponCategory.Shortsword
            },

            new WeaponCategory[]    //axes
            {
                WeaponCategory.LightPick,
                WeaponCategory.HeavyPick,
                WeaponCategory.DwarvenWaraxe,
                WeaponCategory.Tongi,
                WeaponCategory.Battleaxe,
                WeaponCategory.DoubleAxe,
                WeaponCategory.Greataxe,
                WeaponCategory.Handaxe,
                WeaponCategory.ThrowingAxe,
                WeaponCategory.Urgrosh
            },

            new WeaponCategory[]    //maces + hammers
            {
                WeaponCategory.Club,
                WeaponCategory.Greatclub,
                WeaponCategory.Flail,
                WeaponCategory.HeavyFlail,
                WeaponCategory.HeavyMace,
                WeaponCategory.LightMace,
                WeaponCategory.HookedHammer,
                WeaponCategory.EarthBreaker,
                WeaponCategory.LightHammer,
                WeaponCategory.Warhammer
            },

            new WeaponCategory[]    //daggers
            {
                WeaponCategory.PunchingDagger,
                WeaponCategory.Dagger,
                WeaponCategory.Kukri,
                WeaponCategory.Starknife
            },

            new WeaponCategory[]    //spears + polearms
            {
                WeaponCategory.Longspear,
                WeaponCategory.Shortspear,
                WeaponCategory.Spear,
                WeaponCategory.Trident,
                WeaponCategory.Bardiche,
                WeaponCategory.Glaive,
                WeaponCategory.Fauchard
            },

            new WeaponCategory[]    //bows + crossbows
            {
                WeaponCategory.Longbow,
                WeaponCategory.Shortbow,
                WeaponCategory.HandCrossbow,
                WeaponCategory.HeavyCrossbow,
                WeaponCategory.HeavyRepeatingCrossbow,
                WeaponCategory.LightCrossbow,
                WeaponCategory.LightRepeatingCrossbow
            },

            new WeaponCategory[]    //magic
            {
                WeaponCategory.Touch,
                WeaponCategory.Ray,
                WeaponCategory.KineticBlast
            },

            new WeaponCategory[]    //natural weapons
            {
                WeaponCategory.Bite,
                WeaponCategory.Gore,
                WeaponCategory.Claw,
                WeaponCategory.OtherNaturalWeapons
            },

            new WeaponCategory[]    //shields
            {
                WeaponCategory.SpikedHeavyShield,
                WeaponCategory.SpikedLightShield,
                WeaponCategory.WeaponHeavyShield,
                WeaponCategory.WeaponLightShield
            },

            new WeaponCategory[]    //monk
            {
                WeaponCategory.Quarterstaff,
                WeaponCategory.Sai,
                WeaponCategory.Kama,
                WeaponCategory.Siangham,
                WeaponCategory.Nunchaku,
                WeaponCategory.Shuriken,
                WeaponCategory.UnarmedStrike
            }
        };
        /* all
            new WeaponCategory[]
            {
                WeaponCategory.Bardiche,
                WeaponCategory.BastardSword,
                WeaponCategory.Battleaxe,
                WeaponCategory.Bite,
                WeaponCategory.Bomb,
                WeaponCategory.Claw,
                WeaponCategory.Club,
                WeaponCategory.Dagger,
                WeaponCategory.Dart,
                WeaponCategory.DoubleAxe,
                WeaponCategory.DoubleSword,
                WeaponCategory.DuelingSword,
                WeaponCategory.DwarvenWaraxe,
                WeaponCategory.EarthBreaker,
                WeaponCategory.ElvenCurvedBlade,
                WeaponCategory.Estoc,
                WeaponCategory.Falcata,
                WeaponCategory.Falchion,
                WeaponCategory.Fauchard,
                WeaponCategory.Flail,
                WeaponCategory.Glaive,
                WeaponCategory.Gore,
                WeaponCategory.Greataxe,
                WeaponCategory.Greatclub,
                WeaponCategory.Greatsword,
                WeaponCategory.Handaxe,
                WeaponCategory.HandCrossbow,
                WeaponCategory.HeavyCrossbow,
                WeaponCategory.HeavyFlail,
                WeaponCategory.HeavyMace,
                WeaponCategory.HeavyPick,
                WeaponCategory.HeavyRepeatingCrossbow,
                WeaponCategory.HookedHammer,
                WeaponCategory.Javelin,
                WeaponCategory.Kama,
                WeaponCategory.KineticBlast,
                WeaponCategory.Kukri,
                WeaponCategory.LightCrossbow,
                WeaponCategory.LightHammer,
                WeaponCategory.LightMace,
                WeaponCategory.LightPick,
                WeaponCategory.LightRepeatingCrossbow,
                WeaponCategory.Longbow,
                WeaponCategory.Longspear,
                WeaponCategory.Longsword,
                WeaponCategory.Nunchaku,
                WeaponCategory.OtherNaturalWeapons,
                WeaponCategory.PunchingDagger,
                WeaponCategory.Quarterstaff,
                WeaponCategory.Rapier,
                WeaponCategory.Ray,
                WeaponCategory.Sai,
                WeaponCategory.Scimitar,
                WeaponCategory.Scythe,
                WeaponCategory.Shortbow,
                WeaponCategory.Shortspear,
                WeaponCategory.Shortsword,
                WeaponCategory.Shuriken,
                WeaponCategory.Siangham,
                WeaponCategory.Sickle,
                WeaponCategory.Sling,
                WeaponCategory.SlingStaff,
                WeaponCategory.Spear,
                WeaponCategory.SpikedHeavyShield,
                WeaponCategory.SpikedLightShield,
                WeaponCategory.Starknife,
                WeaponCategory.ThrowingAxe,
                WeaponCategory.Tongi,
                WeaponCategory.Touch,
                WeaponCategory.Trident,
                WeaponCategory.UnarmedStrike,
                WeaponCategory.Urgrosh,
                WeaponCategory.Warhammer,
                WeaponCategory.WeaponHeavyShield,
                WeaponCategory.WeaponLightShield
            }
        */
    }

}
