using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Mechanics.Actions;
using JetBrains.Annotations;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic.Parts;
using Kingmaker;
using System.Linq;

namespace FumisCodex.NewComponents
{
    public class ContextActionMoveUniqueArea : ContextAction
    {
        [NotNull]
        public BlueprintUnitFact Feature;

        public override string GetCaption()
        {
            return "Move area effect " + Feature.name;
        }

        public override void RunAction()
        {
            //this.Context.MaybeCaster
            string areaId = this.AbilityContext.Caster.Get<UnitPartUniqueAreaEffects>()?.Areas.FirstOrDefault(a => a.Feature == this.Feature)?.AreaId;

            if (areaId != null)
            {
                AreaEffectEntityData areaData = Game.Instance.State.AreaEffects[areaId];
                if (areaData != null)
                    areaData.View.transform.position = this.Target.Point;
            }
        }

    }
}
