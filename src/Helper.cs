using CallOfTheWild;
using FumisCodex.NewComponents;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace FumisCodex
{
    public static class Extensions
    {
        private static readonly FastSetter setBaseValueType = Helpers.CreateFieldSetter<ContextRankConfig>("m_BaseValueType");
        private static readonly FastGetter getBaseValueType = Helpers.CreateFieldGetter<ContextRankConfig>("m_BaseValueType");
        public static ContextRankBaseValueType m_BaseValueType(this ContextRankConfig config)
        {
            return (ContextRankBaseValueType)getBaseValueType(config);
        }
        public static void m_BaseValueType(this ContextRankConfig config, ContextRankBaseValueType value)
        {
            setBaseValueType(config, value);
        }

        //private static HarmonyLib.SetterHandler<BlueprintBuff, object> getm_Flags = HarmonyLib.FastAccess.CreateSetterHandler<BlueprintBuff, object>(HarmonyLib.AccessTools.Field(typeof(BlueprintBuff), "m_Flags"));
        public static void m_Flags(this BlueprintBuff obj, bool IsFromSpell = false, bool HiddenInUi = false, bool StayOnDeath = false, bool RemoveOnRest = false, bool RemoveOnResurrect = false, bool Harmful = false)
        {
            int value = (IsFromSpell?1:0) | (StayOnDeath?8:0) | (RemoveOnRest?16:0) | (RemoveOnResurrect?32:0) | (Harmful?64:0);
#if !DEBUG
            value |= (HiddenInUi?2:0);
#endif
            HarmonyLib.AccessTools.Field(typeof(BlueprintBuff), "m_Flags").SetValue(obj, value);
        }

        public static T CloneUnity<T>(this T obj) where T : UnityEngine.Object
        {
            return UnityEngine.Object.Instantiate<T>(obj);
        }


        public static PrerequisiteArchetypeLevel CreatePrerequisite(this BlueprintArchetype @class, int level, bool any = true)
        {
            var result = CallOfTheWild.Helpers.Create<PrerequisiteArchetypeLevel>();
            result.CharacterClass = @class.GetParentClass();
            result.Archetype = @class;
            result.Level = level;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }
        
        /// <returns>Action on index 0</returns>
        public static GameAction getAction(this BlueprintAbility ability)
        {
            return ability.GetComponent<AbilityEffectRunAction>().Actions.Actions[0];
        }
        //public static GameAction getAction(this AbilityEffectRunAction effectAndRun) { return effectAndRun.Actions.Actions[0]; }

        /// <param name="savingThrow">SavingThrowType.Unknown means "None"</param>
        public static void setSavingThrow(this BlueprintAbility ability, SavingThrowType savingThrow)
        {
            ability.GetComponent<AbilityEffectRunAction>().SavingThrowType = savingThrow;
        }
        public static void setSavingThrow(this AbilityEffectRunAction effectAndRun, SavingThrowType savingThrow)
        {
            effectAndRun.SavingThrowType = savingThrow;
        }

        /// <summary>Clones ComponentsArray and overrides reference.
        /// This makes it so replacing components on a copy does not mutate the original.</summary>
        /// <returns>Reference to new array.</returns>
        public static BlueprintComponent[] DetachComponents(this BlueprintScriptableObject obj)
        {
            var result = new BlueprintComponent[obj.ComponentsArray.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = obj.ComponentsArray[i];
            obj.ComponentsArray = result;
            return result;
        }

        /// <summary>Looks for the first component of the replacement type and overrides the reference.
        /// Note that you need to detach the component first, otherwise the original gets mutated.</summary>
        /// <param name="replacement">The new component that should take it's place.</param>
        /// <returns>Itself for chaining.</returns>
        public static BlueprintScriptableObject ReplaceDirty(this BlueprintScriptableObject obj, BlueprintComponent replacement)
        {
            Type replacementType = replacement.GetType();
            for (int i = 0; i < obj.ComponentsArray.Length; i++)
            {
                if (obj.ComponentsArray[i].GetType() == replacementType)
                {
                    obj.ComponentsArray[i] = replacement;
                    return obj;
                }
            }
            return null;
        }

        /// <summary>Makes an copy of an already existing AbilityEffectRunAction component.</summary>
        /// <param name="detach">You only need to detach once per copy, this this might be unnecessary.</param>
        /// <returns></returns>
        public static AbilityEffectRunAction CloneAbilityActionList(this BlueprintAbility ability, bool detach = true)
        {
            //this might do something similar, I couldn't figure out what it does: ability.ReplaceComponent<AbilityEffectRunAction>( a => { } );
            var orig = ability.GetComponent<AbilityEffectRunAction>();

            var result = new AbilityEffectRunAction();
            result.SavingThrowType = orig.SavingThrowType;
            result.Actions = new ActionList();
            result.Actions.Actions = new GameAction[orig.Actions.Actions.Length];
            
            for (int i = 0; i < orig.Actions.Actions.Length; i++)
                result.Actions.Actions[i] = UnityEngine.Object.Instantiate(orig.Actions.Actions[i]);

            if (detach) ability.DetachComponents();
            ability.ReplaceDirty(result);
            return result;
        }

        public static T[] ToArray<T>(this T obj)
        {
            return new T[] { obj };
        }

        /// <summary>Appends objects on array. Does not change the original.</summary>
        public static T[] AppendRange<T>(this T[] orig, params T[] objs)
        {
            int i, j;
            T[] result = new T[orig.Length + objs.Length];
            for (i = 0; i < orig.Length; i++)
                result[i] = orig[i];
            for (j = 0; i < result.Length; i++)
                result[i] = objs[j++];
            return result;
        }

        //public static T CloneComponent<T>(this T orig) where T : class, new()
        //{
        //    Type type = orig.GetType();
        //    T result = new T();
        //    foreach (var fi in type.GetFields().Where(f => f.IsPublic || f.IsPrivate))
        //        fi.SetValue(result, fi.GetValue(orig));
        //    return result;
        //}
        //public static void CloneAndReplace<T> (this T orig, BlueprintScriptableObject parent) where T : BlueprintComponent
        //{
        //    parent.ReplaceComponent<T>(orig.CloneComponent());
        //}
    }

    public static class Contexts
    {
        public static PrefabLink NullPrefabLink = new PrefabLink();

        public static ContextValue ValueRank = new ContextValue() { ValueType = ContextValueType.Rank, ValueRank = AbilityRankType.Default };
        public static ContextValue ValueShared = new ContextValue() { ValueType = ContextValueType.Shared, ValueShared = AbilitySharedValue.Damage };
        public static ContextValue ValueZero = new ContextValue() { ValueType = ContextValueType.Simple, Value = 0 };
        public static ContextValue ValueOne = new ContextValue() { ValueType = ContextValueType.Simple, Value = 1 };
        public static ContextValue ValueTwo = new ContextValue() { ValueType = ContextValueType.Simple, Value = 2 };
        public static ContextValue ValueFour = new ContextValue() { ValueType = ContextValueType.Simple, Value = 4 };

        public static ContextDiceValue DiceZero = Helpers.CreateContextDiceValue(DiceType.Zero, ValueZero);
        public static ContextDiceValue DiceOne = Helpers.CreateContextDiceValue(DiceType.One, ValueOne);
        public static ContextDiceValue Dice1d3 = Helpers.CreateContextDiceValue(DiceType.D3, ValueOne);
        public static ContextDiceValue Dice1d4 = Helpers.CreateContextDiceValue(DiceType.D4, ValueOne);
        public static ContextDiceValue Dice1d6 = Helpers.CreateContextDiceValue(DiceType.D6, ValueOne);
        public static ContextDiceValue Dice1d8 = Helpers.CreateContextDiceValue(DiceType.D8, ValueOne);

        public static ContextDurationValue DurationZero = Helpers.CreateContextDuration(0);
        public static ContextDurationValue Duration1Round = Helpers.CreateContextDuration(1);
        public static ContextDurationValue DurationRankInRounds = Helpers.CreateContextDuration(Contexts.ValueRank, DurationRate.Rounds);
        public static ContextDurationValue DurationRankInMinutes = Helpers.CreateContextDuration(Contexts.ValueRank, DurationRate.Minutes);
        public static ContextDurationValue Duration24Hours = Helpers.CreateContextDuration(1, DurationRate.Days);

        public static BlueprintSummonPool SummonPool = Main.library.Get<BlueprintSummonPool>("d94c93e7240f10e41ae41db4c83d1cbe");
        public static ActionList AfterSpawnAction = Helper.CreateActionList(Helpers.CreateApplyBuff(Main.library.Get<BlueprintBuff>("0dff842f06edace43baf8a2f44207045"), DurationZero, false, false, false, false, true));
    }

    public class Helper
    {
        public static T[] ToArray<T>(params T[] objs)
        {
            return objs;
        }
		
        /// <summary>Appends objects on array and overwrites the original.</summary>
        public static T[] AppendAndReplace<T>(ref T[] orig, params T[] objs)
        {
            if (orig == null) orig = new T[0];

            int i, j;
            T[] result = new T[orig.Length + objs.Length];
            for (i = 0; i < orig.Length; i++)
                result[i] = orig[i];
            for (j = 0; i < result.Length; i++)
                result[i] = objs[j++];
            orig = result;
            return result;
        }

        public static GameAction[] RecursiveReplace<T>(GameAction[] actions, Action<T> lambda) where T : GameAction
        {
            var result = new List<GameAction>();

            foreach (var action in actions)
            {
                var conditional = action as Conditional;
                var repl = action as T;
                if (conditional)
                {
                    conditional = conditional.CreateCopy();
                    conditional.IfTrue.Actions = RecursiveReplace(conditional.IfTrue.Actions, lambda);
                    conditional.IfFalse.Actions = RecursiveReplace(conditional.IfFalse.Actions, lambda);
                    result.Add(conditional);
                }
                else if (repl)
                {
                    result.Add(repl.CreateCopy(lambda));
                }
                else
                {
                    result.Add(action);
                }
            }

            return result.ToArray();
        }

        public static void RecursiveAction<T>(GameAction[] actions, Action<T> lambda) where T : GameAction
        {
            foreach (var action in actions)
            {
                var repl = action as T;
                if (repl)
                {
                    lambda(repl);
                }

                var conditional = action as Conditional;
                if (conditional)
                {
                    RecursiveAction(conditional.IfTrue.Actions, lambda);
                    RecursiveAction(conditional.IfFalse.Actions, lambda);
                }
            }
        }

        public static AddKineticistBurnValueChangedTrigger CreateAddKineticistBurnValueChangedTrigger(params GameAction[] actions)
        {
            var result = ScriptableObject.CreateInstance<AddKineticistBurnValueChangedTrigger>();
            result.Action = new ActionList() { Actions = actions };
            return result;
        }

        public static PrerequisiteFeaturesFromList CreatePrerequisiteFeaturesFromList(bool any, int amount, params BlueprintFeature[] features)
        {
            if (features == null || features[0] == null) throw new ArgumentNullException();
            var result = ScriptableObject.CreateInstance<PrerequisiteFeaturesFromList>();
            result.Features = features;
            result.Amount = amount;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }

        public static AbilityEffectRunAction CreateAbilityEffectRunAction(SavingThrowType save = SavingThrowType.Unknown, params GameAction[] actions)
        {
            if (actions == null || actions[0] == null) throw new ArgumentNullException();
            var result = ScriptableObject.CreateInstance<AbilityEffectRunAction>();
            result.SavingThrowType = save;
            result.Actions = new ActionList() { Actions = actions };
            return result;
        }

        public static ContextActionApplyBuff CreateActionApplyBuff(BlueprintBuff buff, int duration = 0, DurationRate rate = DurationRate.Rounds, bool dispellable = false, bool permanent = false)
        {
            return Helpers.CreateApplyBuff(buff, Helpers.CreateContextDuration(bonus: new ContextValue() { Value = duration }, rate: rate), fromSpell: false, dispellable: dispellable, permanent: permanent);
        }

        public static BuffSubstitutionOnApply CreateBuffSubstitutionOnApply(BlueprintBuff GainedFact, BlueprintBuff SubstituteBuff)
        {
            var result = ScriptableObject.CreateInstance<BuffSubstitutionOnApply>();
            result.GainedFact = GainedFact;
            result.SubstituteBuff = SubstituteBuff;
            return result;
        }

        public static SpecificBuffImmunity CreateSpecificBuffImmunity(BlueprintBuff buff)
        {
            var result = ScriptableObject.CreateInstance<SpecificBuffImmunity>();
            result.Buff = buff;
            return result;
        }

        public static ContextActionRemoveBuff CreateActionRemoveBuff(BlueprintBuff buff, bool toCaster = false)
        {
            var result = ScriptableObject.CreateInstance<ContextActionRemoveBuff>();
            result.Buff = buff;
            result.ToCaster = toCaster;
            return result;
        }

        public static ContextCondition[] CreateConditionHasNoBuff(params BlueprintBuff[] buffs)
        {
            if (buffs == null || buffs[0] == null) throw new ArgumentNullException();
            var result = new ContextCondition[buffs.Length];

            for (int i = 0; i < result.Length; i++)
            {
                var buff = ScriptableObject.CreateInstance<ContextConditionHasBuff>();
                buff.Buff = buffs[i];
                buff.Not = true;
                result[i] = buff;
            }

            return result;
        }

        public static AbilityRequirementActionAvailable CreateRequirementActionAvailable(bool Not, ActionType Action)
        {
            var result = ScriptableObject.CreateInstance<AbilityRequirementActionAvailable>();
            result.Not = Not;
            result.Action = Action;
            return result;
        }

        public static AbilityRequirementHasBuffs CreateAbilityRequirementHasBuffs(bool Not, params BlueprintBuff[] Buffs)
        {
            var result = ScriptableObject.CreateInstance<AbilityRequirementHasBuffs>();
            result.Not = Not;
            result.Buffs = Buffs;
            return result;
        }

        public static AbilityRequirementHasBuffTimed CreateAbilidtyRequirementHasBuffTimed(CompareType Compare, TimeSpan TimeLeft, params BlueprintBuff[] Buffs)
        {
            var result = ScriptableObject.CreateInstance<AbilityRequirementHasBuffTimed>();
            result.Compare = Compare;
            result.Buffs = Buffs;
            result.TimeLeft = TimeLeft;
            return result;
        }

        /// <summary>AbilityRankType is only relevant for ContextValueType.Rank</summary> 
        public static ContextDiceValue CreateContextDiceValue(DiceType Dx, ContextValueType diceType = ContextValueType.Simple, AbilityRankType diceRank = AbilityRankType.Default, int diceValue = 0, ContextValueType bonusType = ContextValueType.Simple, AbilityRankType bonusRank = AbilityRankType.Default, int bonusValue = 0)
        {
            var result = new ContextDiceValue();
            result.DiceType = Dx;

            result.DiceCountValue = new ContextValue();
            result.DiceCountValue.ValueType = diceType;
            result.DiceCountValue.ValueRank = diceRank;
            result.DiceCountValue.Value = diceValue;

            result.BonusValue = new ContextValue();
            result.BonusValue.ValueType = bonusType;
            result.BonusValue.ValueRank = bonusRank;
            result.BonusValue.Value = bonusValue;

            return result;
        }

        public static AddContextStatBonusMinMax CreateAddContextStatBonusMin(ContextValue value, int multiplier, StatType stat, params ModifierDescriptor[] descriptor)
        {
            var result = ScriptableObject.CreateInstance<AddContextStatBonusMinMax>();
            result.Multiplier = multiplier;
            result.Value = value;
            result.Stat = stat;
            result.Descriptor = descriptor;
            result.MinValue = 0;
            return result;
        }

        public static HasFact CreateHasFact(BlueprintUnitFact fact, UnitEvaluator unit = null)
        {
            var result = ScriptableObject.CreateInstance<HasFact>();
            result.Fact = fact;
            result.Unit = unit;
            return result;
        }

        public static AbilitySpawnFx CreateAbilitySpawnFx(string AssetId, AbilitySpawnFxTime spawnTime, AbilitySpawnFxAnchor position, AbilitySpawnFxAnchor orientation)
        {
            var spawnFx = ScriptableObject.CreateInstance<AbilitySpawnFx>();
            spawnFx.PrefabLink = new Kingmaker.ResourceLinks.PrefabLink();
            spawnFx.PrefabLink.AssetId = AssetId;
            spawnFx.Time = AbilitySpawnFxTime.OnPrecastStart;
            spawnFx.PositionAnchor = position;
            spawnFx.OrientationAnchor = orientation;
            return spawnFx;
        }
		
		public static PrefabLink Resource(string AssetId)
		{
			var result = new Kingmaker.ResourceLinks.PrefabLink();
			result.AssetId = AssetId;
			return result;
		}

        public static ContextActionSpawnMonster CreateContextActionSpawnMonster(BlueprintUnit unit, ContextDiceValue amount = null, ContextDurationValue duration = null, BlueprintSummonPool pool = null)
        {
            var result = ScriptableObject.CreateInstance<ContextActionSpawnMonster>();
            result.Blueprint = unit;
            result.SummonPool = pool ?? Contexts.SummonPool;
            result.DurationValue = duration ?? Contexts.DurationRankInRounds;
            result.CountValue = amount ?? Contexts.DiceOne;
            result.AfterSpawn = Contexts.AfterSpawnAction;
            return result;
        }

        public static ContextActionSpawnMonsterUnique CreateContextActionSpawnMonsterUnique(BlueprintUnit unit, BlueprintSummonPool pool, ContextDiceValue amount = null, ContextDurationValue duration = null)
        {
            if (pool == null) throw new ArgumentNullException();
            var result = ScriptableObject.CreateInstance<ContextActionSpawnMonsterUnique>();
            result.Blueprint = unit;
            result.SummonPool = pool;
            result.DurationValue = duration ?? Contexts.DurationRankInRounds;
            result.CountValue = amount ?? Contexts.DiceOne;
            result.AfterSpawn = Contexts.AfterSpawnAction;
            return result;
        }

        public static ContextActionSpawnMonsterLeveled CreateContextActionSpawnMonsterLeveled(int[] LevelThreshold, BlueprintUnit[] BlueprintPool)
        {
            var result = ScriptableObject.CreateInstance<ContextActionSpawnMonsterLeveled>();
            result.LevelThreshold = LevelThreshold;
            result.BlueprintPool = BlueprintPool;
            return result;
        }

        public static ContextActionToggleActivatable CreateContextActionToggleActivatable(bool TurnOn, BlueprintActivatableAbility Activatable, params GameAction[] OnFailure)
        {
            var result = ScriptableObject.CreateInstance<ContextActionToggleActivatable>();
            result.TurnOn = TurnOn;
            result.Activatable = Activatable;
			result.OnFailure = CreateActionList(OnFailure);
            return result;
        }

        public static ContextActionKillSummons CreateContextActionKillSummons(BlueprintSummonPool SummonPool, params BlueprintBuff[] Buffs)
        {
            var result = ScriptableObject.CreateInstance<ContextActionKillSummons>();
            result.SummonPool = SummonPool;
            result.Buffs = Buffs;
            return result;
        }

        public static AbilityShowIfCasterHasAnyFacts CreateAbilityShowIfCasterHasAnyFacts(params BlueprintUnitFact[] facts)
        {
            var result = ScriptableObject.CreateInstance<AbilityShowIfCasterHasAnyFacts>();
            result.UnitFacts = facts;
            return result;
        }

        public static AbilityCasterHasNoFacts CreateAbilityCasterHasNoFacts(params BlueprintUnitFact[] facts)
        {
            var result = ScriptableObject.CreateInstance<AbilityCasterHasNoFacts>();
            result.Facts = facts;
            return result;
        }

        public static ContextActionCastSpell CreateContextActionCastSpell(BlueprintAbility spell, ContextValue dc = null, ContextValue spellLevel = null)
        {
            var result = ScriptableObject.CreateInstance<ContextActionCastSpell>();
            result.Spell = spell;
            result.OverrideDC = dc != null;
            result.OverrideSpellLevel = spellLevel != null;
            result.DC = dc;
            result.SpellLevel = spellLevel;
            return result;
        }

        public static AddSpellImmunity CreateAddSpellImmunity(int immunityType, SpellDescriptor descriptor, params BlueprintAbility[] exceptions)
        {
            var result = ScriptableObject.CreateInstance<AddSpellImmunity>();
            result.SpellDescriptor = new SpellDescriptorWrapper(descriptor);
            result.Type = (SpellImmunityType)immunityType;
            if (exceptions != null) result.Exceptions = exceptions;
            return result;
        }

        public static AddKineticistBurnModifier CreateAddKineticistBurnModifier(int Value, KineticistBurnType BurnType = KineticistBurnType.Infusion, ContextValue BurnValue = null, params BlueprintAbility[] AppliableTo)
        {
            var result = ScriptableObject.CreateInstance<AddKineticistBurnModifier>();
            result.Value = Value;
            result.BurnType = BurnType;
            result.BurnValue = BurnValue;
            result.UseContextValue = BurnValue != null;
            result.RemoveBuffOnAcceptBurn = false;
            result.AppliableTo = AppliableTo ?? new BlueprintAbility[0];
            return result;
        }

        public static ContextActionRemoveSelf CreateContextActionRemoveSelf()
        {
            var result = ScriptableObject.CreateInstance<ContextActionRemoveSelf>();
            return result;
        }

        public static BuffMovementSpeed CreateBuffMovementSpeed(int Value, ModifierDescriptor Descriptor = ModifierDescriptor.None, int MinimumCap = 0, float MultiplierCap = 0f)
        {
            var result = ScriptableObject.CreateInstance<BuffMovementSpeed>();
            result.Value = Value;
            result.Descriptor = Descriptor;
            result.CappedOnMultiplier = (MultiplierCap != 0f);
            result.MultiplierCap = MultiplierCap;
            result.CappedMinimum = (MinimumCap > 0);
            result.MinimumCap = MinimumCap;
            return result;
        }

        public static CriticalConfirmationWeaponType CreateCriticalConfirmationWeaponType(ContextValue Value, WeaponCategory Type)
        {
            var result = ScriptableObject.CreateInstance<CriticalConfirmationWeaponType>();
            result.Value = Value;
            result.Type = Type;
            return result;
        }

        public static AddContextStatBonus CreateAddContextStatBonus(StatType Stat, ContextValue Value, ModifierDescriptor Descriptor = ModifierDescriptor.UntypedStackable)
        {
            var result = ScriptableObject.CreateInstance<AddContextStatBonus>();
            result.Stat = Stat;
            result.Value = Value;
            result.Descriptor = Descriptor;
            return result;
        }

        ///<summary>SubFeature is the second or third feat of the style chain.</summary>
        public static AddFactContextActions CombatStyleHelper(BlueprintFeature SubFeature, BlueprintBuff Buff)
        {
            var applyBuff = ScriptableObject.CreateInstance<ContextActionApplyBuff>();
            applyBuff.Buff = Buff;
            applyBuff.DurationValue = Contexts.DurationZero;
            applyBuff.IsFromSpell = false;
            applyBuff.IsNotDispelable = false;
            applyBuff.Permanent = true;

            var has = ScriptableObject.CreateInstance<ContextConditionHasFact>();
            has.Fact = SubFeature;

            var c = ScriptableObject.CreateInstance<Conditional>();
            c.ConditionsChecker = new ConditionsChecker() { Conditions = new Condition[] { has }, Operation = Operation.And };
            c.IfTrue = CreateActionList(applyBuff);
            c.IfFalse = CreateActionList();

            var result = ScriptableObject.CreateInstance<AddFactContextActions>();
            result.Activated = CreateActionList(c);
            result.Deactivated = CreateActionList();
            result.NewRound = CreateActionList();

            return result;
        }

        public static ContextActionRestoreResource CreateContextActionRestoreResource(BlueprintAbilityResource Resource, ContextValue Amount, bool ToCaster = false)
        {
            var result = ScriptableObject.CreateInstance<ContextActionRestoreResource>();
            result.Resource = Resource;
            result.Amount = Amount;
            result.ToCaster = ToCaster;
            return result;
        }

        public static RendSpecial CreateRendSpecial(DiceFormula RendDamage, DamageTypeDescription RendType = null, WeaponCategory? Category = null, bool TargetSelf = false, params GameAction[] Actions)
        {
            var result = ScriptableObject.CreateInstance<RendSpecial>();
            result.RendType = RendType ?? CreateDamageTypeDescription();
            result.RendDamage = RendDamage;
            result.TargetSelf = TargetSelf;
            result.CheckWeaponCategory = Category != null;
            result.WeaponCategory = Category.GetValueOrDefault();
            result.Actions = CreateActionList(Actions);
            return result;
        }

        public static DamageTypeDescription CreateDamageTypeDescription(DamageType type = DamageType.Physical)
        {
            var result = new DamageTypeDescription()
            {
                Type = type,
                Common = new DamageTypeDescription.CommomData(),
                Physical = new DamageTypeDescription.PhysicalData() { Form = (PhysicalDamageForm)7 }
            };
            return result;
        }

        public static AddOutgoingPhysicalDamageProperty CreateAddOutgoingPhysicalDamageProperty(
            BlueprintWeaponType WeaponType = null, bool CheckRange = false, bool IsRanged = false, bool AddMagic = false,
            PhysicalDamageMaterial? Material = null, PhysicalDamageForm? Form = null, DamageAlignment? Alignment = null,
            bool MyAlignment = false, DamageRealityType? Reality = null)
        {
            var result = ScriptableObject.CreateInstance<AddOutgoingPhysicalDamageProperty>();
            result.CheckWeaponType = WeaponType != null;
            result.WeaponType = WeaponType;
            result.CheckRange = CheckRange;
            result.IsRanged = IsRanged;
            result.AddMagic = AddMagic;
            result.AddMaterial = Material != null;
            result.Material = Material.GetValueOrDefault();
            result.AddForm = Form != null;
            result.Form = Form.GetValueOrDefault();
            result.AddAlignment = Alignment != null || MyAlignment;
            result.Alignment = Alignment.GetValueOrDefault();
            result.AddReality = Reality != null;
            result.Reality = Reality.GetValueOrDefault();
            return result;
        }

        public static ContextActionCombatManeuver CreateContextActionCombatManeuver(CombatManeuver Type, params GameAction[] OnSuccess)
        {
            var result = ScriptableObject.CreateInstance<ContextActionCombatManeuver>();
            result.Type = Type;
            result.OnSuccess = CreateActionList(OnSuccess);
            return result;
        }

        public static ActionList CreateActionList(params GameAction[] actions)
        {
            if (actions == null || actions.Length == 1 && actions[0] == null) actions = Array.Empty<GameAction>();
            return new ActionList() { Actions = actions };
        }
        
        public static class Image2Sprite
        {
            public static Sprite Create(string filename)
            {
                try
                {
                    var bytes = File.ReadAllBytes(Path.Combine(Main.ModPath, "Icons", filename));
                    var texture = new Texture2D(64, 64);
                    texture.LoadImage(bytes);
                    return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0, 0));
                }
                catch (Exception e)
                {
                    Main.DebugLogAlways(e.Message);
                    return null;
                }
            }
        }
    }
}
