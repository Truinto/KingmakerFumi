using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex
{
    //[Harmony12.HarmonyPatch(typeof(LevelUpController), "CanLevelUp")]
    class Tests
    {
        static void Test1(ContextCalculateSharedValue __instance, MechanicsContext context)
        {
            Main.DebugLog("ContextCalculateSharedValue:Calculate");
            Main.DebugLog(new System.Diagnostics.StackTrace().ToString());

            IEnumerable<BlueprintCharacterClass> casterClasses = Kingmaker.Game.Instance.BlueprintRoot.Progression.CharacterClasses.Where((c => c.Spellbook != null));
            casterClasses.FirstOrDefault(c => c.Name == "your class name here");

            UnitEntityData u = null;
            IEnumerable<Spellbook> books = u.Descriptor.Progression.Classes.Select((c) => u.Descriptor.GetSpellbook(c.CharacterClass)).Where((s) => s != null && s.Blueprint.Spontaneous && s.Blueprint.IsArcane && s.LastSpellbookLevel == 9);

        }

        [Harmony12.HarmonyPostfix()]
        static void InfiniteLevelUp(UnitDescriptor unit, ref bool __result)
        {
            __result = true;
        }
    }

    //[Harmony12.HarmonyPatch(typeof(LevelUpController), "AddLevel")]
    class Tests2
    {
        static void Prefix()
        {

        }

        static void Postfix(LevelUpController __result)
        {

        }
    }

    //[Harmony12.HarmonyPatch(typeof(AddFactContextActions), nameof(AddFactContextActions.OnFactDeactivate))]
    public static class TickTestOnFactDeactivate
    {
        static void Prefix(AddFactContextActions __instance)
        {
            Main.DebugLog("IFactContextOwner of " + __instance.Fact.Name + " is " + (__instance.Fact as IFactContextOwner).ToString());
        }
    }


    //[Harmony12.HarmonyPatch(typeof(UnitTicksController), "TickOnUnit")]
    //[Harmony12.HarmonyPriority(0)]
    public static class TickPatch
    {
        static void xPrefix(UnitEntityData unit, UnitTicksController __instance)
        {
            if (unit.IsMainCharacter)
                Main.DebugLog("Tick=" + unit.TimeToNextRoundTick.ToString() + " Delta=" + Game.Instance.TimeController.GameDeltaTime.ToString());
        }

        static void Postfix(UnitEntityData unit, UnitTicksController __instance)
        {
            if (unit.IsMainCharacter)
                Main.DebugLog("Tick=" + unit.TimeToNextRoundTick.ToString() + " Delta=" + Game.Instance.TimeController.GameDeltaTime.ToString());
            //unit.TimeToNextRoundTick += Game.Instance.TimeController.GameDeltaTime;
            //if (unit.TimeToNextRoundTick < 6f)
            //    return;
            //unit.TimeToNextRoundTick -= 6f;
            //TickNextRound(unit);
        }

        static void TickNextRound(UnitEntityData unit)
        {
            unit.CombatState.AIData.TickRound();
            unit.Logic.CallFactComponents<ITickEachRound>(a => a.OnNewRound());
        }
    }
}
