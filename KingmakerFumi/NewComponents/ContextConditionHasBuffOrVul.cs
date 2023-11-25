using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace FumisCodex.NewComponents
{
    public class ContextConditionHasBuffOrVul : ContextCondition
    {
        public BlueprintBuff[] CheckedBuffs;
        public BlueprintBuff[] AlternativeBuffs;

        public override string GetConditionCaption()
        {
            return "Check if target does not have buff OR has vulnerability debuff";
        }

        public override bool CheckCondition()
        {
            UnitEntityData caster = this.Context.MaybeCaster;
            UnitEntityData unit = this.Context.MainTarget?.Unit;
            if (unit == null)
                return false;
            bool flag1 = false;

            foreach (var CheckedBuff in this.CheckedBuffs)
            {
                foreach (var b in unit.Descriptor.Buffs)
                {
                    flag1 = (b.Blueprint == CheckedBuff) && (b.MaybeContext.MaybeCaster == caster);
                    if (flag1) break;
                }
                if (flag1) break;
            }

            bool flag2 = false;
            foreach (var AlternativeBuff in this.AlternativeBuffs)
            {
                foreach (var b in unit.Descriptor.Buffs)
                {
                    flag2 = (b.Blueprint == AlternativeBuff) && (b.MaybeContext.MaybeCaster == caster);
                    if (flag2) break;
                }
                if (flag2) break;
            }

            if (flag1)
            {
                return flag2;
            }
            return true;
        }
    }
}