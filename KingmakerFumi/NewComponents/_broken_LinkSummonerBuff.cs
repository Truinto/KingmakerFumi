using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Controllers.Units;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.ElementsSystem;

namespace FumisCodex.NewComponents
{
    [ComponentName("BuffMechanics/Link with Summoner")]
    [AllowedOn(typeof(BlueprintUnitFact))]
    [AllowMultipleComponents]
    public class LinkSummonerBuff : BuffLogic, ITickEachRound
    {
        public BlueprintBuff Buff2;

        public void SelfKill()
        {
            this.Owner.Unit.Descriptor.State.MarkedForDeath = true;
        }

        public void OnNewRound()
        {
            //string ownID = this.Owner.Unit.UniqueId;

            bool flag = false;
            foreach (var buff in this.Owner.Unit.Get<UnitPartSummonedMonster>().Summoner.Buffs)
            {
                if (buff.Context.MaybeCaster == this.Owner.Unit)   // TODO: find right variable
                {
                    flag = true;
                    break;
                }
            }

            Main.DebugLog("LinkSummonerBuff OnNewRound: " + flag.ToString());

            if (!flag) SelfKill();
        }

        public override void OnFactActivate()
        {
            throw new NotImplementedException();

            //this.OnTurnOn();

            //// give summoner buff

            //Main.DebugLog("LinkSummonerBuff OnFactActivate");

            //this.Owner.Unit.Get<UnitPartSummonedMonster>().Summoner.Descriptor.Buffs.AddBuff(this.Buff2, this.Owner.Unit, this.Buff.TimeLeft);
            //// TODO: add ownID to buff?
            //MechanicsContext context = ElementsContext.GetData<MechanicsContext.Data>()?.Context;
        }

        /*public void RunAction() //from ContextActionApplyBuff
        {
            //probably have to make context = new MechanicsContext();
            MechanicsContext context = ElementsContext.GetData<MechanicsContext.Data>()?.Context;
            if (context == null)
            {
                UberDebug.LogError((UnityEngine.Object)this, (object)"Unable to apply buff: no context found", (object[])Array.Empty<object>());
            }
            else
            {
                TimeSpan timeSpan = !this.UseDurationSeconds ? this.DurationValue.Calculate(context).Seconds : this.DurationSeconds.Seconds();
                TimeSpan? duration = !this.Permanent ? new TimeSpan?(timeSpan) : new TimeSpan?();
                UnitEntityData unitEntityData = !this.ToCaster ? this.Target.Unit : context.MaybeCaster;
                if (unitEntityData == null)
                {
                    UberDebug.LogError((UnityEngine.Object)this, (object)"Can't apply buff: target is null", (object[])Array.Empty<object>());
                }
                else
                {
                    Kingmaker.UnitLogic.Buffs.Buff buff1 = unitEntityData.Descriptor.AddBuff(this.Buff, context, duration);
                    if (buff1 == null)
                        return;
                    buff1.IsFromSpell = this.IsFromSpell;
                    buff1.IsNotDispelable = this.IsNotDispelable;
                    if (!this.AsChild)
                        return;
                    Kingmaker.UnitLogic.Buffs.Buff buff2 = ElementsContext.GetData<Kingmaker.UnitLogic.Buffs.Buff.Data>()?.Buff;
                    if (buff2 == null)
                        return;
                    if (buff2.Owner == buff1.Owner)
                        buff2.StoreFact((Fact)buff1);
                    else
                        UberDebug.LogError((UnityEngine.Object)context.AssociatedBlueprint, (object)string.Format("Parent and child buff must have one owner ({0})", (object)context.AssociatedBlueprint.name), (object[])Array.Empty<object>());
                }
            }
        }*/
    }
}
