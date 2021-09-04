using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace FumisCodex
{
    public class Fixes
    {
        public static void fixShamblingMoundGrapple()
        {
            var grapple_self = Main.library.Get<BlueprintBuff>("fde59ce17ec392e46a33420219e85b23");//ShamblingMoundGrappledCantAttack
            Access.m_DescriptionStr(grapple_self, "A shambling mound can not move while grappling the foe.");
            grapple_self.SetComponents(Array.Empty<BlueprintComponent>());
        }

        [HarmonyLib.HarmonyPatch(typeof(UnitPartGrappleInitiator), "Init")]
        public class UnitPartGrappleInitiatorPatch
        {
            public static void Postfix(UnitPartGrappleInitiator __instance)
            {
                if (Settings.StateManager.State.fixShamblingMoundGrapple)
                    __instance.Owner.State.RemoveCondition(UnitCondition.CantAct);
            }
        }

        public static void fixPummelingBully()
        {
            var PummelingBullyBuff = Main.library.Get<BlueprintBuff>("c4e824f6913ebba499c6d9faf551a9b7");
            PummelingBullyBuff.m_Flags(StayOnDeath: true);
            var trigger = PummelingBullyBuff.GetComponent<AddInitiatorAttackWithWeaponTrigger>();
            trigger.OnlyOnFirstAttack = false;
            trigger.OnlyOnFirstHit = true;

            var PummelingStyleBuff = Main.library.Get<BlueprintBuff>("8cb3816915b1a8348b3872b964a2fa23");

            var WildcardBuffPummelingBully = Main.library.TryGet<BlueprintBuff>("72ca6edf879346528b867b5feb9a6d38");
            if (WildcardBuffPummelingBully == null)
                return;

            var WildcardBuffPummelingCharge = Main.library.TryGet<BlueprintBuff>("d4f403b6e089430f9673d8a62d1ae13f");
            if (WildcardBuffPummelingCharge == null)
                return;

            PummelingStyleBuff.AddComponent(Helper.CreateRecalculateOnFactsChange(WildcardBuffPummelingBully, WildcardBuffPummelingCharge));
        }
    }
}
