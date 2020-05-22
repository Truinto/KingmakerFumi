using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FumisCodex.NewComponents
{
    [ComponentName("Add stat bonus custom")]
    [AllowedOn(typeof(BlueprintFeature))]
    [AllowedOn(typeof(BlueprintBuff))]
    [AllowMultipleComponents]
    public class AddContextStatBonusMinMax : OwnedGameLogicComponent<UnitDescriptor>
    {
        private List<ModifiableValue.Modifier> m_Modifier = new List<ModifiableValue.Modifier>(2);
        public int Multiplier = 1;
        public ContextValue Value;
        public StatType Stat;
        public ModifierDescriptor[] Descriptor;
        public int MinValue = int.MinValue;
        public int MaxValue = int.MaxValue;

        private MechanicsContext Context
        {
            get
            {
                return this.Fact.MaybeContext;
            }
        }

        public override void OnTurnOn()
        {
            int leftover = this.Value.Calculate(this.Context) * this.Multiplier;
            foreach (ModifierDescriptor subdescriptor in this.Descriptor)
            {
                int amount = this.Owner.Stats.GetStat(this.Stat).GetDescriptorBonus(subdescriptor);

                int newamount = Math.Max(Math.Min(amount + leftover, MaxValue), MinValue);
                int diff = newamount - amount;
                if (diff != 0)
                {
                    var mod = this.Owner.Stats.GetStat(this.Stat).AddModifier(diff, this, subdescriptor);
                    mod.StackMode = ModifiableValue.StackMode.ForceStack;
                    this.m_Modifier.Add(mod);
                    leftover -= diff;
                }
                Main.DebugLog("leftover=" + leftover.ToString());
                Main.DebugLog("amount=" + amount.ToString());
                Main.DebugLog("newamount=" + newamount.ToString());
                Main.DebugLog("diff=" + diff.ToString());
                if (leftover == 0) break;
            }

            if (m_Modifier.Count == 0) RemoveSelf();
        }

        public override void OnTurnOff()
        {
            foreach (ModifiableValue.Modifier modifier in this.m_Modifier)
                modifier.Remove();
            m_Modifier.Clear();
        }

        public void RemoveSelf()
        {
            Buff.Data data = ElementsContext.GetData<Buff.Data>();
            data?.Buff.Remove();

            //var x = this.Owner.Unit.Buffs.GetBuff((BlueprintBuff)this.Context.AssociatedBlueprint);
            //x.Remove();
            //this.Owner.Unit.Buffs.Enumerable.
        }
    }
}
