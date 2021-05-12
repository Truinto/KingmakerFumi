using JetBrains.Annotations;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex.NewComponents
{
    public class ActivatableRestrictionBurnCost : ActivatableAbilityRestriction, IKineticistFinalAbilityCostHandler
    {
        public override bool IsAvailable()
        {
            bool allowed = m_Cost >= Min && m_Cost <= Max;
            if (allowed && this.ActivateWhenPossible)
                this.Fact.IsOn = true;
            return allowed;
        }

        public void HandleKineticistFinalAbilityCost(UnitDescriptor caster, BlueprintAbility blueprint, ref KineticistAbilityBurnCost cost)
        {
            if (caster == this.Owner.Unit.Descriptor && (this.Blueprint == null || this.Blueprint.HasItem(blueprint)))
                m_Cost = cost.TotalWithoutGatherPowerAndMetakinesis;
        }

        private int m_Cost;

        public BlueprintAbility[] Blueprint;
        public int Min;
        public int Max;
        public bool ActivateWhenPossible;
    }
}
