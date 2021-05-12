using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex
{
    public interface IKineticistFinalAbilityCostHandler : IGlobalSubscriber
    {
        void HandleKineticistFinalAbilityCost(UnitDescriptor caster, BlueprintAbility abilityBlueprint, ref KineticistAbilityBurnCost cost);
    }
}
