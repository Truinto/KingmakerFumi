using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace FumisCodex.NewComponents
{
    public class ContextConditionCanTarget : ContextCondition
    {
        protected override string GetConditionCaption()
        {
            return "Check if the ability's target is valid";
        }

        protected override bool CheckCondition()
        {
            return this.AbilityContext.Ability.CanTarget(this.Context.MainTarget);
        }
    }
}