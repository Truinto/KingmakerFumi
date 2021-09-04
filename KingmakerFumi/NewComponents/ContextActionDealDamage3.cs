using JetBrains.Annotations;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
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
using Kingmaker.Utility;
using System;
using UnityEngine;

namespace FumisCodex.NewComponents
{
    public class ContextActionDealDamage3 : ContextActionDealDamage
    {
        public override void RunAction()
        {
            if (base.Target.Unit == null)
            {
                UberDebug.LogError(this, "Invalid target for effect '{0}'", new object[]
                {
                    base.GetType().Name
                });
                return;
            }
            bool flag = base.Context.SavingThrow != null && (base.Context.SavingThrow.IsPassed || base.Context.SavingThrow.ImprovedEvasion) && this.HalfIfSaved;
            if (flag && base.Context.SavingThrow.Evasion && base.Context.SavingThrow.IsPassed)
            {
                EventBus.RaiseEvent<IUnitEvasionHandler>(delegate (IUnitEvasionHandler h)
                {
                    h.HandleEffectEvaded(base.Target.Unit, base.Context);
                });
                return;
            }
            ContextAttackData data = ElementsContext.GetData<ContextAttackData>();

#region change
            var lastAttack = Rulebook.CurrentContext.LastEvent<RuleAttackRoll>();
            if (lastAttack != null)
            {
                
            }
#endregion

            RuleAttackRoll ruleAttackRoll = (data != null) ? data.AttackRoll : null;
            DamageInfo info = new DamageInfo
            {
                Dices = new DiceFormula(this.Value.DiceCountValue.Calculate(base.Context), this.Value.DiceType),
                Bonus = this.Value.BonusValue.Calculate(base.Context),
                PreRolledValue = (this.ReadPreRolledFromSharedValue ? new int?(base.Context[this.PreRolledSharedValue]) : default(int?)),
                HalfBecauseSavingThrow = flag,
                Empower = base.Context.HasMetamagic(Metamagic.Empower),
                Maximize = base.Context.HasMetamagic(Metamagic.Maximize),
                CriticalModifier = ((!this.IgnoreCritical && ruleAttackRoll != null && ruleAttackRoll.IsCriticalConfirmed) ? new DamageCriticalModifierType?((base.AbilityContext != null) ? DamageCriticalModifierType.X2 : ruleAttackRoll.Weapon.Blueprint.CriticalModifier) : default(DamageCriticalModifierType?))
            };
            int value;
            switch (this.M_Type)
            {
                case Type.Damage:
                    value = this.DealHitPointsDamage(info);
                    break;
                case Type.AbilityDamage:
                    value = this.DealAbilityScoreDamage(info);
                    break;
                case Type.EnergyDrain:
                    value = this.DrainEnergy(info);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (this.WriteResultToSharedValue)
            {
                base.Context[this.ResultSharedValue] = value;
            }
            if (this.WriteCriticalToSharedValue && ruleAttackRoll != null && ruleAttackRoll.IsCriticalConfirmed)
            {
                base.Context[this.CriticalSharedValue] = 1;
            }
        }

        private int DrainEnergy(DamageInfo info)
        {
            if (base.Context.MaybeCaster == null)
            {
                UberDebug.LogError(this, "Caster is missing", Array.Empty<object>());
                return 0;
            }
            RuleDrainEnergy ruleDrainEnergy = new RuleDrainEnergy(base.Context.MaybeCaster, base.Target.Unit, this.EnergyDrainType, this.IsTemporaryEnergyDrain ? new TimeSpan?(this.Duration.Calculate(base.Context).Seconds) : default(TimeSpan?), new DiceFormula(this.Value.DiceCountValue.Calculate(base.Context), this.Value.DiceType), this.Value.BonusValue.Calculate(base.Context));
            ruleDrainEnergy.CriticalModifier = info.CriticalModifier;
            ruleDrainEnergy.Empower = info.Empower;
            ruleDrainEnergy.Maximize = info.Maximize;
            ruleDrainEnergy.ParentContext = base.Context;
            RuleSavingThrow savingThrow = base.Context.SavingThrow;
            ruleDrainEnergy.SavingThrowType = ((savingThrow != null) ? savingThrow.Type : SavingThrowType.Fortitude);
            RuleDrainEnergy rule = ruleDrainEnergy;
            return base.Context.TriggerRule<RuleDrainEnergy>(rule).Count;
        }

        private int DealHitPointsDamage(DamageInfo info)
        {
            if (base.Context.MaybeCaster == null)
            {
                UberDebug.LogError(this, "Caster is missing", Array.Empty<object>());
                return 0;
            }
            BaseDamage baseDamage = this.DamageType.GetDamageDescriptor(info.Dices, info.Bonus).CreateDamage();
            baseDamage.EmpowerBonus = (info.Empower ? 1.5f : baseDamage.EmpowerBonus);
            baseDamage.Maximized = (info.Maximize || baseDamage.Maximized);
            baseDamage.CriticalModifier = ((info.CriticalModifier != null) ? new int?(info.CriticalModifier.GetValueOrDefault().IntValue()) : default(int?));
            baseDamage.Half = this.Half;
            baseDamage.AlreadyHalved = (info.PreRolledValue != null && this.Half && this.AlreadyHalved);
            baseDamage.PreRolledValue = info.PreRolledValue;
            if (this.IsAoE && info.PreRolledValue == null)
            {
                int? preRolledValue = base.Context.AoEDamage.Get(this, default(int?));
                if (preRolledValue != null)
                {
                    baseDamage.PreRolledValue = preRolledValue;
                }
            }
            ContextAttackData data = ElementsContext.GetData<ContextAttackData>();
            DamageBundle damageBundle = baseDamage;
            DamageBundle damageBundle2 = damageBundle;
            ItemEntityWeapon weapon;
            if (data == null)
            {
                weapon = null;
            }
            else
            {
                RuleAttackRoll attackRoll = data.AttackRoll;
                weapon = ((attackRoll != null) ? attackRoll.Weapon : null);
            }
            damageBundle2.Weapon = weapon;
            RuleDealDamage ruleDealDamage = new RuleDealDamage(base.Context.MaybeCaster, base.Target.Unit, damageBundle)
            {
                Projectile = ((data != null) ? data.Projectile : null),
                AttackRoll = ((data != null) ? data.AttackRoll : null),
                HalfBecauseSavingThrow = info.HalfBecauseSavingThrow,
                MinHPAfterDamage = (this.UseMinHPAfterDamage ? new int?(this.MinHPAfterDamage) : default(int?)),
                SourceAbility = base.Context.SourceAbility,
                SourceArea = (base.Context.AssociatedBlueprint as BlueprintAbilityAreaEffect)
            };
            if (this.IsShadowEvocation)
            {
                RuleSavingThrow ruleSavingThrow = base.Context.TriggerRule<RuleSavingThrow>(new RuleSavingThrow(base.Target.Unit, SavingThrowType.Will, base.Context.Params.DC));
                ruleDealDamage.ReducedBecauseOfShadowEvocation = ruleSavingThrow.IsPassed;
            }
            if (this.IsShadowEvocationGreater)
            {
                RuleSavingThrow ruleSavingThrow2 = base.Context.TriggerRule<RuleSavingThrow>(new RuleSavingThrow(base.Target.Unit, SavingThrowType.Will, base.Context.Params.DC));
                ruleDealDamage.ReducedBecauseOfShadowEvocationGreater = ruleSavingThrow2.IsPassed;
            }
            base.Context.TriggerRule<RuleDealDamage>(ruleDealDamage);
            if (this.IsAoE && base.Context.AoEDamage.Get(this, default(int?)) == null)
            {
                base.Context.AoEDamage[this] = new int?(ruleDealDamage.Calculate.CalculatedDamage.FirstItem<DamageValue>().RolledValue);
            }
            return ruleDealDamage.Damage;
        }

        private int DealAbilityScoreDamage(DamageInfo info)
        {
            if (base.Context.MaybeCaster == null)
            {
                UberDebug.LogError(this, "Caster is missing", Array.Empty<object>());
                return 0;
            }
            RuleDealStatDamage rule = new RuleDealStatDamage(base.Context.MaybeCaster, base.Target.Unit, this.AbilityType, info.Dices, info.Bonus)
            {
                Empower = info.Empower,
                Maximize = info.Maximize,
                CriticalModifier = info.CriticalModifier,
                IsDrain = this.Drain,
                HalfBecauseSavingThrow = info.HalfBecauseSavingThrow
            };
            return base.Context.TriggerRule<RuleDealStatDamage>(rule).Damage;
        }
		
        private bool IsTemporaryEnergyDrain
		{
			get
			{
				return this.M_Type == Type.EnergyDrain && this.EnergyDrainType != EnergyDrainType.Permanent;
			}
		}

        [SerializeField]
        public Type M_Type;

        public enum Type
        {
            Damage,
            AbilityDamage,
            EnergyDrain,
        }

        public struct DamageInfo
        {
            public DiceFormula Dices;
            public int Bonus;
            public int? PreRolledValue;
            public bool HalfBecauseSavingThrow;
            public bool Empower;
            public bool Maximize;
            public DamageCriticalModifierType? CriticalModifier;
        }
    }
}