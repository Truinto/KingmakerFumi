using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex.NewComponents
{
    [AllowedOn(typeof(BlueprintAbility))]
    public class AbilityShowIfCasterHasAnyFacts : BlueprintComponent, IAbilityVisibilityProvider
    {
        public BlueprintUnitFact[] UnitFacts;

        public bool IsAbilityVisible(AbilityData ability)
        {
            foreach (var fact in UnitFacts)
            {
                if (ability.Caster.Unit.Abilities.HasFact(fact))
                    return true;

                if (ability.Caster.Progression.Features.HasFact(fact))
                    return true;
            }
            return false;
        }
    }
}
