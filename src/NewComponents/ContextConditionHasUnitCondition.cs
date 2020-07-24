
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace FumisCodex.NewComponents
{
    public class ContextConditionHasUnitCondition : ContextCondition
    {
        public UnitCondition Condition;

		protected override string GetConditionCaption()
		{
			return "Check target for condition";
		}

		protected override bool CheckCondition()
		{
            if (this.Condition == UnitCondition.Prone)
                return base.Target.Unit.Descriptor.State.Prone.ShouldBeActive || base.Target.Unit.Descriptor.State.Prone.Active;

            return this.Context.MainTarget.Unit.Descriptor.State.HasCondition(this.Condition);
            //return base.Target.Unit.Descriptor.State.HasCondition(Condition);
        }
    }
}