using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Controllers.Units;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics.Components;
using System;
using C = Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType;

namespace FumisCodex.NewComponents
{
    [ComponentName("BuffMechanics/Heal over time")]
    [AllowedOn(typeof(BlueprintUnitFact))]
    [AllowMultipleComponents]
    public class BuffUseCommand : BuffLogic, ITickEachRound
    {
        public C Command;
        public ActionList SuccessAction;
        public ActionList FailedAction;

        private void RunSuccess(C type)
        {
            if (!this.Owner.Unit.IsInCombat)
                return;
            switch (type)
            {
                case C.Standard:
                    this.Owner.Unit.CombatState.Cooldown.StandardAction = 6f;
                    break;
                case C.Move:
                    this.Owner.Unit.CombatState.Cooldown.MoveAction = 6f;
                    break;
                case C.Swift:
                    this.Owner.Unit.CombatState.Cooldown.SwiftAction = 6f;
                    break;
            }

            if (this.SuccessAction.HasActions)
                (this.Fact as IFactContextOwner)?.RunActionInContext(this.SuccessAction, this.Owner.Unit);
        }

        private void RunFailed()
        {
            if (this.FailedAction.HasActions)
                (this.Fact as IFactContextOwner)?.RunActionInContext(this.FailedAction, this.Owner.Unit);
        }
        
        public void OnNewRound()
        {
            Main.DebugLog("BuffUseCommand Cooldown Swift " + this.Owner.Unit.CombatState.HasCooldownForCommand(C.Swift).ToString()
                + " " + this.Owner.Unit.CombatState.Cooldown.SwiftAction.ToString());
            Main.DebugLog("BuffUseCommand Cooldown Move " + this.Owner.Unit.CombatState.HasCooldownForCommand(C.Move).ToString()
                + " " + this.Owner.Unit.CombatState.Cooldown.MoveAction.ToString());
            Main.DebugLog("BuffUseCommand Cooldown Standard " + this.Owner.Unit.CombatState.HasCooldownForCommand(C.Standard).ToString()
                + " " + this.Owner.Unit.CombatState.Cooldown.StandardAction.ToString());
            

            switch (Command)
            {
                case C.Swift:
                    if (this.Owner.Unit.CombatState.Cooldown.SwiftAction <= 1f)
                        RunSuccess(C.Swift);
                    else
                        RunFailed();
                    break;
                case C.Move:
                    if (this.Owner.Unit.CombatState.Cooldown.MoveAction <= 1f)
                        RunSuccess(C.Move);
                    else
                        goto case C.Standard;
                    break;
                case C.Standard:
                    if (this.Owner.Unit.CombatState.Cooldown.StandardAction <= 1f)
                        RunSuccess(C.Standard);
                    else
                        RunFailed();
                    break;
            }

            throw new NotImplementedException("BuffUseCommand");

            //if (!this.Owner.Unit.CombatState.HasCooldownForCommand(Command))
            //    RunSuccess(Command);
            //else if (Command == C.Move && !this.Owner.Unit.CombatState.HasCooldownForCommand(C.Standard))
            //    RunSuccess(C.Standard);
            //else
            //    RunFailed();
        }

        public override void OnFactActivate()
        {
            base.OnFactActivate();

            foreach (var y in this.Owner.ActivatableAbilities)
            {
                if (y.Blueprint == null)
                {
                    y.IsOn = true;
                }
            }

            foreach (var x in this.Owner.Buffs)
            {
                if (x.Blueprint == this.Buff.Blueprint)
                {
                    int count;
                }
            }
        }
    }
}
