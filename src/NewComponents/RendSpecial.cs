
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Utility;
using Newtonsoft.Json;
using System;

namespace FumisCodex.NewComponents
{
    [AllowedOn(typeof(BlueprintUnitFact))]
    public class RendSpecial : RuleInitiatorLogicComponent<RuleAttackWithWeapon>
    {
        // public override void OnTurnOn()
        // {
        //     base.Owner.State.HasRend.Retain();
        // }

        // public override void OnTurnOff()
        // {
        //     base.Owner.State.HasRend.Release();
        // }
        [JsonProperty]
        private TimeSpan m_LastUseTime;
        
        private int count;

        public override void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
        }

        public override void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            if (this.m_LastUseTime + 1.Rounds().Seconds <= Game.Instance.TimeController.GameTime)
            {
                this.m_LastUseTime = Game.Instance.TimeController.GameTime;
                this.count = 0;
            }

            if (evt.AttackRoll.IsHit && evt.Initiator.Commands.Attack != null && (!CheckWeaponCategory || this.WeaponCategory == evt.Weapon.Blueprint.Category))
            {
                this.count++;

                Main.DebugLog("RendSpecial count=" + this.count);

                if (this.count == 2)
                {
                    BaseDamage damage = this.RendType.GetDamageDescriptor(this.RendDamage, 0).CreateDamage();
                    RuleDealDamage evt2 = new RuleDealDamage(base.Owner.Unit, evt.Target, damage);
                    Game.Instance.Rulebook.TriggerEvent<RuleDealDamage>(evt2);
                    
                    this.TryRunActionInContext(this.Actions, TargetSelf ? evt.Initiator : evt.Target);
                }
            }
        }

        public bool TargetSelf;
        public ActionList Actions;

        public bool CheckWeaponCategory;
        public WeaponCategory WeaponCategory;

        public DiceFormula RendDamage;

        public DamageTypeDescription RendType;
    }
}