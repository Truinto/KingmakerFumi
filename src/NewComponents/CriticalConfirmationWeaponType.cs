
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;

namespace FumisCodex.NewComponents
{
    [AllowedOn(typeof(BlueprintUnitFact))]
    public class CriticalConfirmationWeaponType : RuleInitiatorLogicComponent<RuleAttackRoll>
    {
        public ContextValue Value;
        public WeaponCategory Type;

        private MechanicsContext Context
        {
            get
            {
                MechanicsContext context = (this.Fact as Buff)?.Context;
                if (context != null)
                    return context;
                return (this.Fact as Feature)?.Context;
            }
        }

        public override void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
            if (evt.Weapon.Blueprint.Category == Type)
                evt.CriticalConfirmationBonus += this.Value.Calculate(this.Context);
        }

        public override void OnEventDidTrigger(RuleAttackRoll evt)
        {
        }
    }
}