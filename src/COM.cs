using System;
using System.Reflection;
using Kingmaker.Enums;
using HarmonyLib;
using static HarmonyLib.AccessTools;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Blueprints.Facts;
using System.Collections.Generic;
using Kingmaker.Blueprints.Classes.Selection;

namespace FumisCodex
{
    public static class COM
    {
        //CallOfTheWild.FeralCombatTraining, CallOfTheWild, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
        public static Type FeralCombatTraining; 
        public static MethodInfo checkHasFeralCombat;
        public static Dictionary<WeaponCategory, BlueprintUnitFact> natural_weapon_type_fact_map;

        //CraftMagicItems.Main.RenderCraftingSkillInformation, CraftMagicItems
        public static MethodInfo RenderCraftingSkillInformation;
        public static MethodInfo WorkOnProjects;
        //CraftMagicItems.Settings.CasterLevelIsSinglePrerequisite, CraftMagicItems
        public static FieldInfo CasterLevelIsSinglePrerequisite;

        static COM()
        {
            Main.DebugLog("Initializing COM");

            try
            {
                FeralCombatTraining = Type.GetType("CallOfTheWild.FeralCombatTraining, CallOfTheWild");
                if (FeralCombatTraining != null)
                {
                    checkHasFeralCombat = Method(FeralCombatTraining, "checkHasFeralCombat");
                    natural_weapon_type_fact_map = Field(FeralCombatTraining, "natural_weapon_type_fact_map")?.GetValue(null) as Dictionary<WeaponCategory, BlueprintUnitFact>;
                }
            } catch (Exception e) { Main.DebugLog(e.ToString()); }

            try
            {
                RenderCraftingSkillInformation = Method("CraftMagicItems.Main:RenderCraftingSkillInformation");
                WorkOnProjects = Method("CraftMagicItems.Main:WorkOnProjects");
                CasterLevelIsSinglePrerequisite = Field(Type.GetType("CraftMagicItems.Settings, CraftMagicItems"), "CasterLevelIsSinglePrerequisite");
            } catch (Exception e) { Main.DebugLog(e.ToString()); }

            // logging
            if (FeralCombatTraining == null) Main.DebugLogAlways("FeralCombatTraining couldn't load");
            if (checkHasFeralCombat == null) Main.DebugLogAlways("checkHasFeralCombat couldn't load");
            if (natural_weapon_type_fact_map == null) Main.DebugLogAlways("natural_weapon_type_fact_map couldn't load");
            if (RenderCraftingSkillInformation == null) Main.DebugLogAlways("RenderCraftingSkillInformation couldn't load");
            if (WorkOnProjects == null) Main.DebugLogAlways("WorkOnProjects couldn't load");
            if (CasterLevelIsSinglePrerequisite == null) Main.DebugLogAlways("CasterLevelIsSinglePrerequisite couldn't load");
        }

        public static bool CheckWeaponOverride(UnitEntityData unit, ItemEntityWeapon weapon, WeaponCategory categoryShouldBe, bool allowFeralCombat = true)
        {
            if (weapon.Blueprint.Category == categoryShouldBe)
                return true;

            if (!allowFeralCombat)
                return false;

            try
            {
                if (checkHasFeralCombat != null)
                {
                    if (categoryShouldBe == WeaponCategory.UnarmedStrike)
                    {
                        return (bool)checkHasFeralCombat.Invoke(null, Params(unit, weapon, false, false));
                    }
                }
            }
            catch (Exception e)
            {
                Main.DebugError(e);
            }

            return false;
        }

        public static object[] Params(params object[] Params)
        {
            return Params;
        }
    }
}