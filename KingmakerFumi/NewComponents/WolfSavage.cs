
using System;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.ContextData;
using Kingmaker.Utility;
using Newtonsoft.Json;

namespace FumisCodex.NewComponents
{
    public class WolfSavage : GameLogicComponent, IInitiatorRulebookSubscriber, IInitiatorRulebookHandler<RuleAttackWithWeaponResolve>, IRulebookHandler<RuleAttackWithWeaponResolve>
    {
        [JsonProperty]
        private TimeSpan m_LastUseTime;
        public void OnEventAboutToTrigger(RuleAttackWithWeaponResolve evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackWithWeaponResolve evt)
        {
            if (this.m_LastUseTime + 1.Rounds().Seconds > Game.Instance.TimeController.GameTime)
                return;

            if (evt.Damage.Damage >= 10
                && evt.AttackWithWeapon.Weapon.Blueprint.IsNatural 
                && (evt.Target.Descriptor.State.Prone.ShouldBeActive || evt.Target.Descriptor.State.Prone.Active))
            {
                this.m_LastUseTime = Game.Instance.TimeController.GameTime;
                using (new ContextAttackData(evt.AttackWithWeapon.AttackRoll, null))
                {
                    MechanicsContext context = (base.Fact as IFactContextOwner).Context;

                    RuleSavingThrow save = new RuleSavingThrow(evt.Target, SavingThrowType.Fortitude, 10 + evt.Initiator.Descriptor.Progression.CharacterLevel/2 + evt.Initiator.Descriptor.Stats.Wisdom.Bonus);
                    if (!context.TriggerRule<RuleSavingThrow>(save).IsPassed)
                    {
                        RuleDealStatDamage dmg = new RuleDealStatDamage(context.MaybeCaster, evt.Target, StatType.Constitution, new DiceFormula(1, DiceType.D4), 0);
                        context.TriggerRule<RuleDealStatDamage>(dmg);
                    }
                }

            }
        }
    }
}