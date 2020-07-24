
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace FumisCodex.NewComponents
{
    public class ContextActionRestoreResource : ContextAction
    {
        public ContextValue Amount;
        public BlueprintAbilityResource Resource;
        public bool ToCaster;
        public bool Drain;

        public override string GetCaption()
        {
            return "ContextActionRestoreResource: " + Resource.name;
        }

        public override void RunAction()
        {
            UnitEntityData unit = ToCaster ? this.Context.MaybeCaster : this.Target.Unit;
            int value = Amount.Calculate(this.Context);

            if (!unit.Descriptor.Resources.ContainsResource(this.Resource))
            {
                //nothing
            }
            else if (!this.Drain)
            {
                unit.Descriptor.Resources.Restore(this.Resource, value);
            }
            else if (unit.Descriptor.Resources.HasEnoughResource(this.Resource, value))
            {
                unit.Descriptor.Resources.Spend(this.Resource, value);
            }
        }
    }
}