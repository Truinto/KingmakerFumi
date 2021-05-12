using HarmonyLib;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Controllers.Combat;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static Kingmaker.UnitLogic.Parts.UnitPartSpellResistance;

namespace FumisCodex
{
    public class Patches_Activatable
    {
        //uses up move action when triggered; deactivates activatable if no action left
        [HarmonyLib.HarmonyPatch(typeof(ActivatableAbility), nameof(ActivatableAbility.OnNewRound))]
        public static class ActivatableAbility_OnNewRoundPatch
        {
            static void Postfix(ActivatableAbility __instance, bool ___m_ShouldBeDeactivatedInNextRound, bool ___m_WasInCombat)
            {
                //if (__instance.Owner.IsMainCharacter) Main.DebugLog("ActivatableAbility OnNewRound " + __instance.Blueprint.name);
                if (!__instance.Owner.Unit.IsInCombat) return; //cooldowns are not refreshed out of combat, therefore must not increase cooldown out of combat

                bool flag = ___m_ShouldBeDeactivatedInNextRound || !__instance.IsOn || !__instance.IsAvailable || (__instance.Blueprint.DeactivateIfCombatEnded && !__instance.Owner.Unit.IsInCombat && (__instance.Blueprint.ActivateOnCombatStarts || ___m_WasInCombat));
                if (flag)
                {
                }
                else
                {
                    ActivatableAbilityUnitCommand activatableAbilityUnitCommand = __instance.Get<ActivatableAbilityUnitCommand>();
                    if (activatableAbilityUnitCommand && activatableAbilityUnitCommand.Type == UnitCommand.CommandType.Move)
                    {
                        if (!HasMoveCooldown(__instance.Owner))
                        {
                            Main.DebugLog("ActivatableAbility set Move Cooldown! old=" + __instance.Owner.Unit.CombatState.Cooldown.MoveAction);
                            __instance.Owner.Unit.CombatState.Cooldown.MoveAction =
                                Math.Min(__instance.Owner.Unit.CombatState.Cooldown.MoveAction + 3f, 6f);
                        }
                        else
                        {
                            Main.DebugLog("ActivatableAbility turn off because no move action left");
                            __instance.IsOn = false;
                        }
                    }
                }
            }
        }

        public static bool HasMoveCooldown(UnitCombatState state)
        {
            if (state.HasCooldownForCommand(UnitCommand.CommandType.Standard))
                return state.Cooldown.MoveAction > 0f;
            else
                return state.Cooldown.MoveAction > 3f;
        }
        public static bool HasMoveCooldown(UnitDescriptor owner)
        {
            return HasMoveCooldown(owner.Unit.CombatState);
        }
        public static bool HasMoveCooldown(UnitEntityData unit)
        {
            return HasMoveCooldown(unit.CombatState);
        }

        [HarmonyLib.HarmonyPatch(typeof(ActivatableAbility), nameof(ActivatableAbility.HandleUnitRunCommand))]
        public static class ActivatableAbility_HandleUnitRunCommandPatch
        {
            static bool Prefix(UnitCommand command, ActivatableAbility __instance)
            {
                //if (command.Executor.IsMainCharacter) Main.DebugLog("RunCommand " + command.Type.ToString() + " : " + command.Executor.CombatState.HasCooldownForCommand(command) + ", Move=" + command.Executor.CombatState.Cooldown.MoveAction.ToString() + ", Standard=" + command.Executor.CombatState.Cooldown.StandardAction.ToString());
                //if (command.Type == UnitCommand.CommandType.Move && (command.Executor.CombatState.Cooldown.MoveAction <= 3.1f
                //    || command.Executor.CombatState.Cooldown.StandardAction <= 0.1f))
                //    return false;
                //return true;

                if (command.Executor == __instance.Owner.Unit && __instance.IsOn)
                {
                    ActivatableAbilityUnitCommand abilityUnitCommand = __instance.Get<ActivatableAbilityUnitCommand>();
                    if (abilityUnitCommand != null && abilityUnitCommand.Type == command.Type)
                    {
                        UnitActivateAbility unitActivateAbility = command as UnitActivateAbility;

                        //turn off, if ability is not itself AND action is either not move OR move is on cooldown
                        //if (unitActivateAbility?.Ability != __instance && (abilityUnitCommand.Type != UnitCommand.CommandType.Move || HasMoveCooldown(__instance.Owner)))

                        //turn off, if ability is not itself AND action is not move
                        if (unitActivateAbility?.Ability != __instance && abilityUnitCommand.Type != UnitCommand.CommandType.Move)
                        {
                            __instance.IsOn = false;
                        }
                    }
                }
                return false;
            }
        }

        //does not interest us because Blueprint.ActivateWithUnitCommand returns false anyway
        //[HarmonyLib.HarmonyPatch(typeof(ActivatableAbility), nameof(ActivatableAbility.HandleUnitCommandDidAct))]
        public static class ActivatableAbility_HandleUnitCommandDidActPatch
        {
            static bool Prefix(UnitCommand command, ActivatableAbility __instance, bool ___m_IsOn)
            {
                if (command.Executor == __instance.Owner.Unit && __instance.IsOn)
                {
                    ActivatableAbilityUnitCommand abilityUnitCommand = __instance.Get<ActivatableAbilityUnitCommand>();
                    if ((abilityUnitCommand == null || abilityUnitCommand.Type == command.Type) && !__instance.IsRunning && __instance.Blueprint.ActivateWithUnitCommand)
                    {
                        UnitActivateAbility unitActivateAbility = command as UnitActivateAbility;
                        if (unitActivateAbility == null || unitActivateAbility.Ability != __instance && !__instance.IsActivateWithSameCommand(unitActivateAbility.Ability))
                        {
                            __instance.IsOn = false;
                        }
                        else if (unitActivateAbility.Result != UnitCommand.ResultType.Success)
                        {
                            __instance.IsOn = false;
                        }
                        else
                        {
                            __instance.Owner.Ensure<UnitPartActivatableAbility>().OnActivatedWithCommand(__instance);
                            __instance.TryStart();
                        }
                    }
                }
                return false;
            }
        }

        //does not interest us because Blueprint.ActivateWithUnitCommand returns false anyway
        //[HarmonyLib.HarmonyPatch(typeof(ActivatableAbility), nameof(ActivatableAbility.HandleUnitCommandDidEnd))]
        public static class ActivatableAbility_HandleUnitCommandDidEndPatch
        {
            static bool Prefix(UnitCommand command, ActivatableAbility __instance)
            {
                if (__instance.IsOn && !__instance.IsRunning && command.Executor == __instance.Owner.Unit && __instance.Blueprint.ActivateWithUnitCommand)
                {
                    UnitActivateAbility unitActivateAbility = command as UnitActivateAbility;
                    if (unitActivateAbility != null && (unitActivateAbility.Ability == __instance || __instance.IsActivateWithSameCommand(unitActivateAbility.Ability)) && unitActivateAbility.IsFinished && !unitActivateAbility.IsActed)
                        __instance.IsOn = false;
                }
                return false;
            }
        }

        //fixes activatable not being allowed to be active when they have the same action (like 2 move actions)
        [HarmonyLib.HarmonyPatch(typeof(ActivatableAbility), "OnTurnOn")]
        public static class ActivatableAbility_OnTurnOnPatch
        {
            static IEnumerable<HarmonyLib.CodeInstruction> Transpiler(IEnumerable<HarmonyLib.CodeInstruction> instr)
            {
                List<HarmonyLib.CodeInstruction> list = instr.ToList();
                MethodInfo original = HarmonyLib.AccessTools.Method(typeof(Fact), nameof(Fact.Get), null, typeof(ActivatableAbilityUnitCommand).ObjToArray());
                MethodInfo replacement = typeof(ActivatableAbility_OnTurnOnPatch).GetMethod(nameof(NullReplacement), BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); //HarmonyLib.AccessTools.Method(typeof(ActivatableAbility_OnTurnOnPatch), nameof(NullReplacement));
                
                for (int i = 0; i < list.Count; i++)
                {
                    var x = list[i].operand as MethodInfo;
                    if (x != null && x == original)
                    {
                        Main.DebugLog("ActivatableAbility_OnTurnOnPatch at " + i);
                        list[i].operand = replacement;
                    }
                }
                
                return list;
            }

            static object NullReplacement(object something)
            {
                return null;
            }
        }

        //removes validation
        [HarmonyLib.HarmonyPatch(typeof(ActivatableAbilityUnitCommand), nameof(ActivatableAbilityUnitCommand.Validate))]
        public static class ActivatableAbilityUnitCommandPatch
        {
            static bool Prefix()
            {
                return false;
            }
        }

        //fixes activatable not starting the second time, while being outside of combat
        [HarmonyLib.HarmonyPatch(typeof(ActivatableAbility), nameof(ActivatableAbility.TryStart))]
        public static class ActivatableAbility_TryStartPatch
        {
            static void Prefix(ActivatableAbility __instance)
            {
                //Main.DebugLog("ActivatableAbility.TryStart IsRunning=" + __instance.IsRunning + " IsWaitingForTarget=" + __instance.IsWaitingForTarget + " IsAvailable=" + __instance.IsAvailable);
                //Main.DebugLog("ReadyToStart=" + __instance.ReadyToStart + " ActivateOnCombatStarts=" + __instance.Blueprint.ActivateOnCombatStarts + " IsNotInCombat=" + !__instance.Owner.Unit.IsInCombat + " Cooldown=" + __instance.Owner.Unit.CombatState.HasCooldownForCommand(UnitCommand.CommandType.Move));
                //StackFrame frame = new StackFrame(2); Main.DebugLog(frame.ToString()); frame = new StackFrame(3); Main.DebugLog(frame.ToString());

                if (!__instance.Owner.Unit.IsInCombat)
                {
                    __instance.Owner.Unit.CombatState.Cooldown.SwiftAction = 0f;
                    __instance.Owner.Unit.CombatState.Cooldown.MoveAction = 0f;
                }

            }
        }

        //fixes activatable can be activated manually
        [HarmonyLib.HarmonyPatch(typeof(MechanicActionBarSlotActivableAbility), nameof(MechanicActionBarSlotActivableAbility.OnClick))]
        public static class ActionBar
        {
            public static readonly int NoManualOn = 788704819;
            public static readonly int NoManualOff = 788704820;
            public static readonly int NoManualAny = 788704821;

            static bool Prefix(MechanicActionBarSlotActivableAbility __instance)
            {
                if (!__instance.ActivatableAbility.IsOn && __instance.ActivatableAbility.Blueprint.WeightInGroup == NoManualOn)
                {
                    return false;
                }
                if (__instance.ActivatableAbility.IsOn && __instance.ActivatableAbility.Blueprint.WeightInGroup == NoManualOff)
                {
                    return false;
                }
                if (__instance.ActivatableAbility.Blueprint.WeightInGroup == NoManualAny)
                {
                    return false;
                }
                return true;
            }
        }


    }

    public class Patches_Spells
    {
        public enum SpellImmunityTypeExt
        {
            AllExceptSpellDescriptor = 50
        }

        [HarmonyLib.HarmonyPatch(typeof(SpellImmunity), nameof(SpellImmunity.CanApply))]
        public static class SpellImmunityPatch
        {
            public static bool Prefix(MechanicsContext context, SpellImmunity __instance, ref bool __result)
            {
                if (__instance.Type == (SpellImmunityType)SpellImmunityTypeExt.AllExceptSpellDescriptor)
                {
                    if (context == null)
                        __result = false;
                    else if (context.SourceAbility != null)
                        __result = !context.SourceAbility.SpellDescriptor.HasAnyFlag(__instance.SpellDescriptor);
                    else
                        __result = !context.SpellDescriptor.HasAnyFlag(__instance.SpellDescriptor);
                    
                    return false;
                }
                return true;
            }
        }
    }

    public class Patches_LevelUp
    {
        [HarmonyLib.HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.Start))]
        public static class LevelUpController_StartPatch
        {
            public static void Prefix(UnitDescriptor unit)
            {
                EventBus.RaiseEvent(unit.Unit, delegate (IUnitDisableFeaturesBeforeLevelUpHandler h)
                {
                    h.HandleUnitDisableFeaturesBeforeLevelUp();
                });
            }
        }
    }

    public class Patches_TWF    // TODO: remove
    {
        //[HarmonyLib.HarmonyPatch(typeof(TwoWeaponFightingAttacks), nameof(TwoWeaponFightingAttacks.OnEventAboutToTrigger))]
        public static class TwoWeaponFightingAttacks_OnEventAboutToTrigger_Patch
        {
            static bool Prefix(RuleCalculateAttacksCount evt)
            {
                if (evt.Initiator.Body.SecondaryHand.Weapon == evt.Initiator.Body.EmptyHandWeapon)
                {
                    
                }
                return true;
            }
        }
    }

    public class Patches_Rulebook
    {

        [HarmonyLib.HarmonyPatch(typeof(RuleAttackRoll), nameof(RuleAttackRoll.OnTrigger))]
        public class AttackRoll
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
            {
                var code = instr.ToList();
                var d20 = typeof(RulebookEvent.Dice).GetProperty(nameof(RulebookEvent.Dice.D20)).GetMethod;
                int count = 0;

                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Call && (code[i].operand as MethodInfo) == d20)
                    {
                        Main.DebugLog("Patched D20 at line " + i);

                        if (count++ == 1)
                            code[i].operand = typeof(AttackRoll).GetMethod(nameof(D20Crit));
                        else
                            code[i].operand = typeof(AttackRoll).GetMethod(nameof(D20));
                    }
                }

                return code;
            }

            public static RulebookEvent.RollEntry D20()
            {
                var result = RulebookEvent.Dice.D20;
                if (ForceDice != 0)
                {
                    result.Value = ForceDice;
                    if (Reset) ForceDice = 0;
                }
                return result;
            }

            public static RulebookEvent.RollEntry D20Crit()
            {
                var result = RulebookEvent.Dice.D20;
                if (ForceCrit != 0)
                {
                    result.Value = ForceCrit;
                    if (Reset) ForceCrit = 0;
                }
                return result;
            }

            public static int ForceDice = 0;
            public static int ForceCrit = 0;
            public static bool Reset = true;
        }
    }

}
