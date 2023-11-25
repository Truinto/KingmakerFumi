
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace FumisCodex.NewComponents
{
    public class ContextConditionHasUnitCondition : ContextCondition
    {
        public UnitCondition Condition;

        public override string GetConditionCaption()
		{
			return "Check target for condition";
		}

        public override bool CheckCondition()
		{
            if (this.Condition == UnitCondition.Prone)
                return base.Target.Unit.Descriptor.State.Prone.ShouldBeActive || base.Target.Unit.Descriptor.State.Prone.Active;

            return this.Context.MainTarget.Unit.Descriptor.State.HasCondition(this.Condition);
            //return base.Target.Unit.Descriptor.State.HasCondition(Condition);
        }
    }
}