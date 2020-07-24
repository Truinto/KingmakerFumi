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
    class ContextActionDealDamage2 : ContextActionDealDamage
    {
        /// <summary>When set will make an attack roll with given weapon. Missing will skip the damage calculation.</summary>
        [CanBeNull]
        public ItemEntityWeapon WeaponOverride;
        [SerializeField]
        public ContextActionDealDamage2.Type M_Type;
        
        public static ContextActionDealDamage2 CreateNew(DamageEnergyType energy, ContextDiceValue damage, bool isAoE = false, bool halfIfSaved = false, bool IgnoreCritical = false)
        {
            // energy damage
            ContextActionDealDamage2 c = CreateInstance<ContextActionDealDamage2>();
            c.DamageType = new DamageTypeDescription()
            {
                Type = Kingmaker.RuleSystem.Rules.Damage.DamageType.Energy,
                Energy = energy,
                Common = new DamageTypeDescription.CommomData(),
                Physical = new DamageTypeDescription.PhysicalData()
            };
            c.Duration = new ContextDurationValue()
            {
                BonusValue = 0,
                Rate = DurationRate.Rounds,
                DiceCountValue = 0,
                DiceType = DiceType.Zero
            };
            c.Value = damage;
            c.IsAoE = isAoE;
            c.HalfIfSaved = halfIfSaved;
            c.IgnoreCritical = IgnoreCritical;
            return c;
        }

        public static ContextActionDealDamage2 CreateNew(PhysicalDamageForm physical, ContextDiceValue damage, bool isAoE = false, bool halfIfSaved = false, bool IgnoreCritical = false)
        {
            // physical damage
            ContextActionDealDamage2 c = CreateInstance<ContextActionDealDamage2>();
            c.DamageType = new DamageTypeDescription()
            {
                Type = Kingmaker.RuleSystem.Rules.Damage.DamageType.Physical,
                Common = new DamageTypeDescription.CommomData(),
                Physical = new DamageTypeDescription.PhysicalData() { Form = physical }
            };
            c.Duration = new ContextDurationValue()
            {
                BonusValue = 0,
                Rate = DurationRate.Rounds,
                DiceCountValue = 0,
                DiceType = DiceType.Zero
            };
            c.Value = damage;
            c.IsAoE = isAoE;
            c.HalfIfSaved = halfIfSaved;
            c.IgnoreCritical = IgnoreCritical;
            return c;
        }

        public static ContextActionDealDamage2 CreateNew(StatType abilityType, ContextDiceValue damage, bool drain = false, bool isAoE = false, bool halfIfSaved = false, bool IgnoreCritical = false)
        {
            // ability damage
            ContextActionDealDamage2 c = CreateInstance<ContextActionDealDamage2>();
            c.M_Type = Type.AbilityDamage;
            HarmonyLib.AccessTools.Field(typeof(ContextActionDealDamage), "m_type").SetValue(c, 1);   // ?? does this work?
            c.Duration = new ContextDurationValue()
            {
                BonusValue = 0,
                Rate = DurationRate.Rounds,
                DiceCountValue = 0,
                DiceType = DiceType.Zero
            };
            c.AbilityType = abilityType;
            c.Value = damage;
            c.IsAoE = isAoE;
            c.HalfIfSaved = halfIfSaved;
            c.Drain = drain;
            c.IgnoreCritical = IgnoreCritical;
            return c;
        }

        public override string GetCaption()
        {
            string str = base.GetCaption();
            //Main.DebugLog(str);
            return str;
        }

        public override void RunAction()
        {
            //Main.DebugLog($"[ActionDealDamage2] Target:{Target?.Unit?.CharacterName} AC:{Target?.Unit?.Stats?.AC?.ModifiedValue} Flat-AC:{Target?.Unit?.Stats?.AC?.FlatFooted} Touch-AC:{Target?.Unit?.Stats?.AC?.Touch} Caster:{Context?.MaybeCaster?.CharacterName}");
            //Main.DebugLog("Context[0] " + this.Context[AbilityRankType.Default].ToString());
            //AbilityRankType.DamageDice [1]
            //AbilitySharedValue.Damage [0]
            //AbilitySharedValue.Duration [2] only composite
            //for (int i = 0; i < 7; i++)
            //{
            //    Main.DebugLog("AbilitySharedValue:" + Context[(AbilitySharedValue)i].ToString());
            //}
            //for (int i = 0; i < 7; i++)
            //{
            //    Main.DebugLog("AbilityRankType:" + Context[(AbilityRankType)i].ToString());
            //}

            if (this.Target.Unit == null)
                UberDebug.LogError(this, "Invalid target for effect '{0}'", this.GetType().Name);
            else if (this.Context.MaybeCaster == null)
                UberDebug.LogError(this, "Invalid caster for effect '{0}'", this.GetType().Name);
            else
            {
                bool saveSuccess = this.Context.SavingThrow != null && (this.Context.SavingThrow.IsPassed || this.Context.SavingThrow.ImprovedEvasion) && this.HalfIfSaved;
                if (saveSuccess && this.Context.SavingThrow.Evasion && this.Context.SavingThrow.IsPassed)
                {
                    EventBus.RaiseEvent<IUnitEvasionHandler>(h => h.HandleEffectEvaded(this.Target.Unit, this.Context));
                }
                else
                {
                    RuleAttackRoll attackRoll;

                    if (WeaponOverride != null)
                    {
                        attackRoll = new RuleAttackRoll(Context.MaybeCaster, this.Target.Unit, WeaponOverride, 0);

                        if (this.Context[AbilitySharedValue.Heal] == 0)   // if this is the first attack this context
                        {
                            this.Context.TriggerRule(attackRoll);
                            this.Context[AbilitySharedValue.Heal] = attackRoll.Roll;    // save this for other targets
                        }
                        else
                        {
                            //attackRoll.ImmuneToCriticalHit = true; // only the first target may get a crit
                            attackRoll.SuspendCombatLog = true; // don't print, since the roll would be different
                            this.Context.TriggerRule(attackRoll);
                        }

                        int d20 = this.Context[AbilitySharedValue.Heal];    // result of attack roll
                        Main.DebugLog($"Attack:{d20} total:{d20 + attackRoll.AttackBonus} AC:{attackRoll.TargetAC} result:{attackRoll.Result.ToString()}");
                        
                        if (attackRoll.Result == AttackResult.MirrorImage)
                        {
                            // battlelog notification that Mirror Image was hit?
                            attackRoll.ConsumeMirrorImageIfNecessary();
                            return;
                        }

                        if (!this.IsSuccessRoll(d20, attackRoll.AttackBonus, attackRoll.TargetAC))
                        {
                            return;
                        }

                        if (attackRoll.Result == AttackResult.Concealment)
                        {
                            // battlelog notification that concealment prevented hit
                            Kingmaker.Game.Instance.UI.BattleLogManager.LogView.AddLogEntry(attackRoll.AttackLogEntry);
                            return;
                        }
                    }
                    else
                        attackRoll = ElementsContext.GetData<ContextAttackData>()?.AttackRoll;

                    ContextActionDealDamage2.DamageInfo info = new ContextActionDealDamage2.DamageInfo()
                    {
                        Dices = new DiceFormula(this.Value.DiceCountValue.Calculate(this.Context), this.Value.DiceType),
                        Bonus = this.Value.BonusValue.Calculate(this.Context),
                        PreRolledValue = !this.ReadPreRolledFromSharedValue ? new int?() : new int?(this.Context[this.PreRolledSharedValue]),
                        HalfBecauseSavingThrow = saveSuccess,
                        Empower = this.Context.HasMetamagic(Metamagic.Empower),
                        Maximize = this.Context.HasMetamagic(Metamagic.Maximize),
                        CriticalModifier = this.IgnoreCritical || attackRoll == null || !attackRoll.IsCriticalConfirmed ? new DamageCriticalModifierType?() : new DamageCriticalModifierType?(this.AbilityContext == null ? attackRoll.Weapon.Blueprint.CriticalModifier : DamageCriticalModifierType.X2)
                    };

                    int num;
                    switch (this.M_Type)
                    {
                        case ContextActionDealDamage2.Type.Damage:
                            num = this.DealHitPointsDamage(info);
                            break;
                        case ContextActionDealDamage2.Type.AbilityDamage:
                            num = this.DealAbilityScoreDamage(info);
                            break;
                        case ContextActionDealDamage2.Type.EnergyDrain:
                            num = this.DrainEnergy(info);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    if (this.WriteResultToSharedValue)
                        this.Context[this.ResultSharedValue] = num;
                    if (!this.WriteCriticalToSharedValue || attackRoll == null || !attackRoll.IsCriticalConfirmed)
                        return;
                    this.Context[this.CriticalSharedValue] = 1;
                }
            }
        }

        public bool IsSuccessRoll(int d20, int attackBonus, int targetAC)
        {
            switch (d20)
            {
                case 0:
                case 1:
                    return false;
                case 20:
                    return true;
                default:
                    return d20 + attackBonus >= targetAC;
            }
        }

        private int DealHitPointsDamage(ContextActionDealDamage2.DamageInfo info)
        {
            if (this.Context.MaybeCaster == null)
            {
                UberDebug.LogError(this, (object)"Caster is missing", (object[])Array.Empty<object>());
                return 0;
            }
            BaseDamage damage1 = this.DamageType.GetDamageDescriptor(info.Dices, info.Bonus).CreateDamage();
            damage1.EmpowerBonus = !info.Empower ? damage1.EmpowerBonus : 1.5f;
            damage1.Maximized = info.Maximize || damage1.Maximized;
            BaseDamage baseDamage = damage1;
            DamageCriticalModifierType? criticalModifier = info.CriticalModifier;
            int? critModifier = criticalModifier.HasValue ? new int?(criticalModifier.GetValueOrDefault().IntValue()) : new int?();
            baseDamage.CriticalModifier = critModifier;
            damage1.Half = this.Half;
            damage1.AlreadyHalved = info.PreRolledValue.HasValue && this.Half && this.AlreadyHalved;
            damage1.PreRolledValue = info.PreRolledValue;
            if (this.IsAoE && !info.PreRolledValue.HasValue)
            {
                int? nullable2 = this.Context.AoEDamage.Get<ContextActionDealDamage, int?>(this, new int?());
                if (nullable2.HasValue)
                    damage1.PreRolledValue = nullable2;
            }
            ContextAttackData data = ElementsContext.GetData<ContextAttackData>();
            DamageBundle damage2 = (DamageBundle)damage1;
            damage2.Weapon = data?.AttackRoll?.Weapon;
            RuleDealDamage rule = new RuleDealDamage(this.Context.MaybeCaster, this.Target.Unit, damage2)
            {
                Projectile = data?.Projectile,
                AttackRoll = data?.AttackRoll,
                HalfBecauseSavingThrow = info.HalfBecauseSavingThrow,
                MinHPAfterDamage = !this.UseMinHPAfterDamage ? new int?() : new int?(this.MinHPAfterDamage),
                SourceAbility = this.Context.SourceAbility,
                SourceArea = this.Context.AssociatedBlueprint as BlueprintAbilityAreaEffect
            };
            if (this.IsShadowEvocation)
            {
                RuleSavingThrow ruleSavingThrow = this.Context.TriggerRule(new RuleSavingThrow(this.Target.Unit, SavingThrowType.Will, this.Context.Params.DC));
                rule.ReducedBecauseOfShadowEvocation = ruleSavingThrow.IsPassed;
            }
            if (this.IsShadowEvocationGreater)
            {
                RuleSavingThrow ruleSavingThrow = this.Context.TriggerRule(new RuleSavingThrow(this.Target.Unit, SavingThrowType.Will, this.Context.Params.DC));
                rule.ReducedBecauseOfShadowEvocationGreater = ruleSavingThrow.IsPassed;
            }
            this.Context.TriggerRule<RuleDealDamage>(rule);
            if (this.IsAoE && !this.Context.AoEDamage.Get<ContextActionDealDamage, int?>(this, new int?()).HasValue)
                this.Context.AoEDamage[this] = new int?(rule.Calculate.CalculatedDamage.FirstItem<DamageValue>().RolledValue);
            return rule.Damage;
        }

        private int DealAbilityScoreDamage(ContextActionDealDamage2.DamageInfo info)
        {
            if (this.Context.MaybeCaster == null)
            {
                UberDebug.LogError(this, (object)"Caster is missing", (object[])Array.Empty<object>());
                return 0;
            }
            return this.Context.TriggerRule(new RuleDealStatDamage(this.Context.MaybeCaster, this.Target.Unit, this.AbilityType, info.Dices, info.Bonus)
            {
                Empower = info.Empower,
                Maximize = info.Maximize,
                CriticalModifier = info.CriticalModifier,
                IsDrain = this.Drain,
                HalfBecauseSavingThrow = info.HalfBecauseSavingThrow
            }).Damage;
        }
        
        private int DrainEnergy(ContextActionDealDamage2.DamageInfo info)
        {
            if (this.Context.MaybeCaster == null)
            {
                UberDebug.LogError(this, (object)"Caster is missing", (object[])Array.Empty<object>());
                return 0;
            }
            RuleDrainEnergy rule = new RuleDrainEnergy(this.Context.MaybeCaster, this.Target.Unit, this.EnergyDrainType, this.EnergyDrainType == EnergyDrainType.Permanent ? new TimeSpan?() : new TimeSpan?(this.Duration.Calculate(this.Context).Seconds), new DiceFormula(this.Value.DiceCountValue.Calculate(this.Context), this.Value.DiceType), this.Value.BonusValue.Calculate(this.Context));
            rule.CriticalModifier = info.CriticalModifier;
            rule.Empower = info.Empower;
            rule.Maximize = info.Maximize;
            rule.ParentContext = this.Context;
            RuleDrainEnergy ruleDrainEnergy = rule;
            SavingThrowType? type = this.Context.SavingThrow?.Type;
            int num = !type.HasValue ? 1 : (int)type.Value;
            ruleDrainEnergy.SavingThrowType = (SavingThrowType)num;
            return this.Context.TriggerRule<RuleDrainEnergy>(rule).Count;
        }

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
