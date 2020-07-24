
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.ContextData;

namespace FumisCodex.NewComponents
{
    public class AddInitiatorAttackDamageThreshold : GameLogicComponent, IInitiatorRulebookSubscriber, IInitiatorRulebookHandler<RuleAttackWithWeaponResolve>, IRulebookHandler<RuleAttackWithWeaponResolve>
    {
        public bool CheckCategory;
        public bool CheckForNaturalWeapon;
        public WeaponCategory WeaponCategory;
        public int DamageThreshold;
        public bool RepeatActions;

        public bool OnlyOnAttackOfOpportunity;

        public bool ActionsOnInitiator;
        public ActionList Action;
        
        public void OnEventAboutToTrigger(RuleAttackWithWeaponResolve evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackWithWeaponResolve evt)
        {
            int damage_dealt = evt.Damage.Damage;
            WeaponCategory weapon_used = evt.AttackWithWeapon.Weapon.Blueprint.Category;
            
            if(damage_dealt >= DamageThreshold
                && (!CheckCategory || weapon_used == WeaponCategory || CheckForNaturalWeapon && evt.AttackWithWeapon.Weapon.Blueprint.IsNatural)
                && (!OnlyOnAttackOfOpportunity || evt.AttackWithWeapon.IsAttackOfOpportunity))
            {
                using (new ContextAttackData(evt.AttackWithWeapon.AttackRoll, null))
                {
                    IFactContextOwner contextOwner = base.Fact as IFactContextOwner;
                    if (contextOwner != null)
                        for (int i = 0; i < damage_dealt / DamageThreshold; i++)
                            contextOwner.RunActionInContext(this.Action, this.ActionsOnInitiator ? evt.Initiator : evt.Target);
                }
            }
        }
    }
}