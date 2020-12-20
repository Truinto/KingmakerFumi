using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex.NewComponents
{
    [AllowMultipleComponents]
    public class WeaponAlterDamageType : GameLogicComponent, IInitiatorRulebookHandler<RuleCalculateDamage>, IRulebookHandler<RuleCalculateDamage>, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateDamage evt)
        {
            if (evt.Reason.Item.Blueprint != Weapon)
                return;

            List<BaseDamage> output = new List<BaseDamage>();

            foreach (var damage in evt.DamageBundle)
            {
                PhysicalDamage physical = damage as PhysicalDamage;

                if (physical == null)
                {
                    output.Add(damage);
                }
                else if (this.IsForceDamage)
                {
                    ForceDamage force = new ForceDamage(damage.Dice);
                    force.AddBonus(damage.Bonus);
                    force.AddBonusTargetRelated(damage.BonusTargetRelated);
                    force.Precision = damage.Precision;
                    force.CriticalModifier = damage.CriticalModifier;
                    force.Sneak = damage.Sneak;
                    output.Add(force);
                }
                else
                {
                    EnergyDamage energy = new EnergyDamage(damage.Dice, EnergyType);
                    energy.AddBonus(damage.Bonus);
                    energy.AddBonusTargetRelated(damage.BonusTargetRelated);
                    energy.Precision = damage.Precision;
                    energy.CriticalModifier = damage.CriticalModifier;
                    energy.Sneak = damage.Sneak;
                    output.Add(energy);
                }
            }
        }

        public void OnEventDidTrigger(RuleCalculateDamage evt)
        {
            //foreach (var damage_value in evt.CalculatedDamage)
            {
            }
        }

        public BlueprintItemWeapon Weapon;

        public DamageEnergyType EnergyType;

        public bool IsForceDamage;
    }
}
