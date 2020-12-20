using System;
using System.Reflection;
using Kingmaker.Enums;
using HarmonyLib;
using static HarmonyLib.AccessTools;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Blueprints.Facts;
using System.Collections.Generic;

namespace FumisCodex
{
    public static class COM
    {
        public static Type FeralCombatTraining; //CallOfTheWild.FeralCombatTraining, CallOfTheWild, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
        public static MethodInfo checkHasFeralCombat;

        public static Dictionary<WeaponCategory, BlueprintUnitFact> natural_weapon_type_fact_map;

        static COM()
        {
            Main.DebugLog("Initializing COM");

            FeralCombatTraining = Type.GetType("CallOfTheWild.FeralCombatTraining, CallOfTheWild");
            if (FeralCombatTraining != null)
            {
                checkHasFeralCombat = Method(FeralCombatTraining, "checkHasFeralCombat");
                natural_weapon_type_fact_map = Field(FeralCombatTraining, "natural_weapon_type_fact_map")?.GetValue(null) as Dictionary<WeaponCategory, BlueprintUnitFact>;
            }

            if (FeralCombatTraining == null) Main.DebugLogAlways("FeralCombatTraining couldn't load");
            if (checkHasFeralCombat == null) Main.DebugLogAlways("checkHasFeralCombat couldn't load");
            if (natural_weapon_type_fact_map == null) Main.DebugLogAlways("natural_weapon_type_fact_map couldn't load");
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