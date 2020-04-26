using Kingmaker.Blueprints.Classes;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex
{
    //[Harmony12.HarmonyPatch(typeof(ContextCalculateSharedValue), "Calculate")]
    class Tests
    {
        static void Postfix(ContextCalculateSharedValue __instance, MechanicsContext context)
        {
            Main.DebugLog("ContextCalculateSharedValue:Calculate");
            Main.DebugLog(new System.Diagnostics.StackTrace().ToString());
        }
    }
}
