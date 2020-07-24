
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace FumisCodex.NewComponents
{
    public class ContextConditionMovespeed : ContextCondition
    {
        public bool TargetFasterThan;
        public float Speed;

		protected override string GetConditionCaption()
		{
			return "Check target move speed";
		}

		protected override bool CheckCondition()
		{
            return TargetFasterThan ? base.Target.Unit.CombatSpeedMps > Speed : base.Target.Unit.CombatSpeedMps <= Speed;
		}
    }
}