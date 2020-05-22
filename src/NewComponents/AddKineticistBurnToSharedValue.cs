using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using System.Collections.Generic;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic;
using Kingmaker.Enums;

namespace FumisCodex.NewComponents
{
    [AllowMultipleComponents]
    [AllowedOn(typeof(BlueprintAbility))]
    public class AddKineticistBurnToSharedValue : OwnedGameLogicComponent<UnitDescriptor>, IGlobalSubscriber, IUnitSubscriber, IKineticistCalculateAbilityCostHandler
    {
        public int MinBurn = 0;
        public int MaxBurn = 0;
        public AbilityRankType RankType;
        public int Multiplier = 0;
        public int Bonus = 0;

        private MechanicsContext Context
        {
            get
            {
                return this.Fact.MaybeContext;
            }
        }

        public void HandleKineticistCalculateAbilityCost(UnitDescriptor caster, BlueprintAbility abilityBlueprint, ref KineticistAbilityBurnCost cost)
        {
            if (caster != this.Owner) return;
            int total = cost.Total;

            if (total >= MinBurn && total <= MaxBurn)
            {
                this.Context[RankType] = total * this.Multiplier + this.Bonus;
            }

        }
    }
}
