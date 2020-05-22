using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CallOfTheWild;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic;
using Kingmaker.Localization;

namespace FumisCodex
{
    public class Fixes
    {
        public static void fixShamblingMoundGrapple()
        {
            var grapple_self = Main.library.Get<BlueprintBuff>("fde59ce17ec392e46a33420219e85b23");//ShamblingMoundGrappledCantAttack
            grapple_self.SetDescription("A shambling mound can not move while grappling the foe.");
            grapple_self.SetComponents(Array.Empty<BlueprintComponent>());
        }

        [Harmony12.HarmonyPatch(typeof(UnitPartGrappleInitiator), "Init")]
        public class UnitPartGrappleInitiatorPatch
        {
            static void Postfix(UnitPartGrappleInitiator __instance)
            {
                if (Settings.StateManager.State.fixShamblingMoundGrapple)
                    __instance.Owner.State.RemoveCondition(UnitCondition.CantAct);
            }
        }
    }
}
