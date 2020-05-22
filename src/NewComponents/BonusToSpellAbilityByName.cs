using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex.NewComponents
{
    [AllowedOn(typeof(BlueprintUnitFact))]
    public class BonusToSpellAbilityByName : OwnedGameLogicComponent<UnitDescriptor>, IInitiatorRulebookSubscriber, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>, IRulebookHandler<RuleCalculateAttackBonusWithoutTarget>, IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>, IInitiatorRulebookHandler<RuleCalculateAbilityParams>, IRulebookHandler<RuleCalculateAbilityParams>
    {
        public WeaponCategory Category = WeaponCategory.KineticBlast;
        public string CategoryStr = "BlastAbility";
        public int AttackBonus = 0;
        public int DamageBonus = 0;
        public int DC_Bonus = 0;
        public int CL_Bonus = 0;

        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            if (evt.Weapon.Blueprint.Category == Category)
                evt.AddBonus(this.AttackBonus, this.Fact);
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
        }

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (evt.SourceAbility == null) return;

            if (evt.SourceAbility.name.EndsWith(this.CategoryStr, StringComparison.Ordinal))
                evt.DamageBundle.First?.AddBonus(this.DamageBonus);
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
        }

        public void OnEventAboutToTrigger(RuleCalculateAbilityParams evt)
        {
            if (evt.Spell == null) return;

            if (evt.Spell.name.EndsWith(this.CategoryStr, StringComparison.Ordinal))
            {
                evt.AddBonusDC(this.DC_Bonus);
                evt.AddBonusCasterLevel(this.CL_Bonus);
            }
        }

        public void OnEventDidTrigger(RuleCalculateAbilityParams evt)
        {
        }
    }
}
