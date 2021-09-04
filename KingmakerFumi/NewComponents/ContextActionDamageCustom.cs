using JetBrains.Annotations;
using Kingmaker.Controllers.Combat;
using Kingmaker.ElementsSystem;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.ContextData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex.NewComponents
{
    public class ContextActionDamageCustom : ContextAction
    {
        public override string GetCaption()
        {
            return "ContextActionDamageCustom";
        }

        public override void RunAction()
        {
            var weapon = WeaponOverride ?? this.Context.MaybeCaster?.GetThreatHand()?.Weapon;
            int attack = this.Context[SharedAttack];
            int damage = this.Context[SharedDamage];
            RuleAttackRoll attackRoll = null;

            Main.DebugLog($"ContextActionDamageCustom attack={attack} damage={damage}");

            // check for saving throw; evades attack when HalfIfSaved and has Evasion
            bool passedSavingThrow = this.Context.SavingThrow != null && (this.Context.SavingThrow.IsPassed || this.Context.SavingThrow.ImprovedEvasion) && this.HalfIfSaved;
            if (passedSavingThrow && this.Context.SavingThrow.Evasion && this.Context.SavingThrow.IsPassed)
            {
                EventBus.RaiseEvent<IUnitEvasionHandler>(h => h.HandleEffectEvaded(this.Target.Unit, this.Context));
                return;
            }

            // get attackroll or make a new roll
            if (DoAttackRoll)
            {
                attackRoll = new RuleAttackRoll(this.Context.MaybeCaster, this.Target.Unit, weapon, 0);
                if (attack != 0 && MergeAttackRolls)
                {
                    attackRoll.DoNotProvokeAttacksOfOpportunity = true;
                    Patches_Rulebook.AttackRoll.ForceDice = attack;
                }
                this.Context.TriggerRule(attackRoll);
                attack = attackRoll.Roll;
                this.Context[SharedAttack] = attack;
                Patches_Rulebook.AttackRoll.ForceDice = 0;
            }
            else
            {
                attackRoll = ElementsContext.GetData<ContextAttackData>()?.AttackRoll;
            }

            // deal damage
            if (!DoAttackRoll || (attackRoll != null && attackRoll.IsHit))
            {
                if (damage == 0)
                {
                    damage = this.Value.Calculate(this.Context); //includes Maximize+Empower
                    this.Context[SharedDamage] = damage;
                }

                DamageBundle damageBundle;
                if (!SplitDamage)
                {
                    BaseDamage primaryDamage = this.PrimaryType.CreateDamage(DiceFormula.One, 0);
                    primaryDamage.PreRolledValue = damage;
                    primaryDamage.Half = this.Half;

                    damageBundle = new DamageBundle(primaryDamage);
                }
                else
                {
                    BaseDamage primaryDamage = this.PrimaryType.CreateDamage(DiceFormula.One, 0);
                    primaryDamage.PreRolledValue = damage / 2;
                    primaryDamage.Half = this.Half;

                    BaseDamage secondaryDamage = this.SecondaryType.CreateDamage(DiceFormula.One, 0);
                    secondaryDamage.PreRolledValue = damage / 2;
                    secondaryDamage.Half = this.Half;

                    damageBundle = new DamageBundle(primaryDamage, secondaryDamage);
                }

                RuleDealDamage dealDamage = new RuleDealDamage(base.Context.MaybeCaster, base.Target.Unit, damageBundle);
                dealDamage.HalfBecauseSavingThrow = passedSavingThrow;
                dealDamage.AttackRoll = attackRoll;
                dealDamage.Projectile = ElementsContext.GetData<ContextAttackData>()?.Projectile;
                dealDamage.SourceAbility = this.Context.SourceAbility;
                dealDamage.SourceArea = this.Context.AssociatedBlueprint as BlueprintAbilityAreaEffect;
                this.Context.TriggerRule(dealDamage);
            }
        }

        [CanBeNull]
        public ItemEntityWeapon WeaponOverride;

        public bool DoAttackRoll;
        public bool MergeAttackRolls;
        public bool SplitDamage;

        public bool Half;
        public bool HalfIfSaved;
        public ContextDiceValue Value;
        public DamageTypeDescription PrimaryType;
        public DamageTypeDescription SecondaryType;

        public AbilitySharedValue SharedDamage = AbilitySharedValue.Damage;
        public AbilitySharedValue SharedAttack = AbilitySharedValue.Heal;
    }
}
