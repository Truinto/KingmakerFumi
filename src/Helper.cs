using FumisCodex.NewComponents;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers.Mechanics.Prerequisites;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;

namespace FumisCodex
{
    public static class Extensions
    {
        public static void m_Flags(this BlueprintBuff obj, bool IsFromSpell = false, bool HiddenInUi = false, bool StayOnDeath = false, bool RemoveOnRest = false, bool RemoveOnResurrect = false, bool Harmful = false)
        {
            int value = (IsFromSpell ? 1 : 0) | (StayOnDeath ? 8 : 0) | (RemoveOnRest ? 16 : 0) | (RemoveOnResurrect ? 32 : 0) | (Harmful ? 64 : 0);
#if !DEBUG
            value |= (HiddenInUi?2:0);
#endif
            HarmonyLib.AccessTools.Field(typeof(BlueprintBuff), "m_Flags").SetValue(obj, value);

            // TODO: improve to
            //Access.m_Flags(obj) = value;
        }

        public static int MinMax(this int number, int min, int max)
        {
            return Math.Max(min, Math.Min(number, max));
        }

        public static void AddItemToSpecifiedVendorTable(this BlueprintSharedVendorTable vendor_table, BlueprintItem item, int amount = 1)
        {
            vendor_table.AddComponent(Helper.CreateLootItemsPackFixed(item, amount));
        }

        public static void SetItemNameDescriptionFlavorIcon(this BlueprintItem item, string displayName = null, string description = null, string flavorText = null, Sprite icon = null)
        {
            if (displayName != null)
                Access.BlueprintItem_DisplayNameTextStr(item, displayName);
            if (description != null)
                Access.BlueprintItem_DescriptionTextStr(item, description);
            if (flavorText != null)
                Access.BlueprintItem_FlavorTextStr(item, flavorText);
            if (icon != null)
                Access.BlueprintItem_Icon(item) = icon;
        }

        public static PrerequisiteArchetypeLevel CreatePrerequisite(this BlueprintArchetype @class, int level, bool any = true)
        {
            var result = Helper.Create<PrerequisiteArchetypeLevel>();
            result.CharacterClass = @class.GetParentClass();
            result.Archetype = @class;
            result.Level = level;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }

        /// <summary>Appends objects on array.</summary>
        public static T[] AddToArray<T>(this T[] orig, params T[] objs)
        {
            if (orig == null) orig = new T[0];

            int i, j;
            T[] result = new T[orig.Length + objs.Length];
            for (i = 0; i < orig.Length; i++)
                result[i] = orig[i];
            for (j = 0; i < result.Length; i++)
                result[i] = objs[j++];
            return result;
        }

        private static int getActions_counter;
        public static List<GameAction> GetActions(this BlueprintAbility ability)
        {
            return ability.GetComponent<AbilityEffectRunAction>()?.Actions?.Actions?.GetActions() ?? new List<GameAction>(0);
        }
        public static List<GameAction> GetActions(this GameAction[] actions)
        {
            return GetActions(actions.ToList());
        }
        public static List<GameAction> GetActions(this List<GameAction> actions)
        {
            getActions_counter = 0;
            return getActions(actions);
        }
        private static List<GameAction> getActions(List<GameAction> actions)
        {
            //Main.DebugLog("getActions with " + actions.Count);

            if (++getActions_counter > 50 || actions.Count == 0)
            {
                Main.DebugLogAlways("CRITICAL ERROR: possible infinite loop during getActions");
                return new List<GameAction>(0);
            }

            var result = new List<GameAction>();

            FieldInfo[] fields = new FieldInfo[3];
            foreach (var action in actions)
            {
                //Main.DebugLog("getActions of type: " + action.GetType().ToString());

                fields[0] = action.GetType().GetField("Actions");
                fields[1] = action.GetType().GetField("Succeed");
                fields[2] = action.GetType().GetField("Failed");

                foreach (var field in fields)
                {
                    if (field != null)
                    {
                        if (field.FieldType == typeof(GameAction[]))
                        {
                            var values = field.GetValue(action) as GameAction[];
                            if (values != null)
                                result.AddRange(values);
                        }
                        else if (field.FieldType == typeof(ActionList))
                        {
                            var values = field.GetValue(action) as ActionList;
                            if (values != null && values.HasActions)
                                result.AddRange(values.Actions);
                        }
                    }
                }
            }

            if (result.Count > 0)
                result.AddRange(getActions(result));  //recursive search

            return result;
        }

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
                result.Actions.Actions[i] = ScriptableObject.Instantiate(orig.Actions.Actions[i]);

            if (detach) ability.DetachComponents();
            ability.ReplaceDirty(result);
            return result;
        }

        public static T[] ObjToArray<T>(this T obj)
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

    public class Access
    {
        public static readonly FieldRef<LocalizedString, string> m_Key;
        public static readonly FieldRef<BlueprintScriptableObject, string> m_AssetGuid;

        #region BlueprintUnitFact
        public static readonly FieldRef<BlueprintUnitFact, LocalizedString> m_DisplayName;
        public static void m_DisplayNameStr(BlueprintUnitFact f, string str)
        {
            m_DisplayName(f) = HelperEA.CreateString(f.name + ".DisplayName", str);
        }
        public static readonly FieldRef<BlueprintUnitFact, LocalizedString> m_Description;
        public static void m_DescriptionStr(BlueprintUnitFact f, string str)
        {
            m_Description(f) = HelperEA.CreateString(f.name + ".Description", str);
        }
        public static readonly FieldRef<BlueprintUnitFact, Sprite> m_Icon;
        #endregion

        #region BlueprintItemWeapon
        public static readonly FieldRef<BlueprintItemWeapon, BlueprintWeaponEnchantment[]> m_Enchantments;
        #endregion

        #region BlueprintItemEnchantment
        public static readonly FieldRef<BlueprintItemEnchantment, LocalizedString> BlueprintItemEnchantment_Description;
        public static void m_DescriptionStr(BlueprintItemEnchantment f, string str)
        {
            BlueprintItemEnchantment_Description(f) = HelperEA.CreateString(f.name + ".Description", str);
        }
        public static readonly FieldRef<BlueprintItemEnchantment, LocalizedString> m_EnchantName;
        public static void m_EnchantNameStr(BlueprintItemEnchantment f, string str)
        {
            m_EnchantName(f) = HelperEA.CreateString(f.name + ".Description", str);
        }
        public static readonly FieldRef<BlueprintItemEnchantment, int> m_EnchantmentCost;
        #endregion

        #region BlueprintItem
        public static readonly FieldRef<BlueprintItem, LocalizedString> BlueprintItem_DisplayNameText;
        public static void BlueprintItem_DisplayNameTextStr(BlueprintItem f, string str)
        {
            BlueprintItem_DisplayNameText(f) = HelperEA.CreateString(f.name + ".ItemDisplayName", str);
        }
        public static readonly FieldRef<BlueprintItem, LocalizedString> BlueprintItem_DescriptionText;
        public static void BlueprintItem_DescriptionTextStr(BlueprintItem f, string str)
        {
            BlueprintItem_DescriptionText(f) = HelperEA.CreateString(f.name + ".ItemDescription", str);
        }
        public static readonly FieldRef<BlueprintItem, LocalizedString> BlueprintItem_FlavorText;
        public static void BlueprintItem_FlavorTextStr(BlueprintItem f, string str)
        {
            BlueprintItem_FlavorText(f) = HelperEA.CreateString(f.name + ".ItemFlavorText", str);
        }
        public static readonly FieldRef<BlueprintItem, Sprite> BlueprintItem_Icon;
        public static readonly FieldRef<BlueprintItem, float> BlueprintItem_Weight;
        public static readonly FieldRef<BlueprintItem, int> m_Cost;
        #endregion

        #region ContextRank
        public static readonly FieldRef<ContextRankConfig, ContextRankBaseValueType> m_BaseValueType;
        public static readonly FieldRef<ContextRankConfig, AbilityRankType> ContextRankConfig_Type;
        public static readonly FieldRef<ContextRankConfig, ContextRankProgression> m_Progression;
        public static readonly FieldRef<ContextRankConfig, bool> m_UseMin;
        public static readonly FieldRef<ContextRankConfig, int> m_Min;
        public static readonly FieldRef<ContextRankConfig, bool> m_UseMax;
        public static readonly FieldRef<ContextRankConfig, int> m_Max;
        public static readonly FieldRef<ContextRankConfig, int> m_StartLevel;
        public static readonly FieldRef<ContextRankConfig, int> m_StepLevel;
        public static readonly FieldRef<ContextRankConfig, BlueprintFeature> m_Feature;
        public static readonly FieldRef<ContextRankConfig, bool> m_ExceptClasses;
        public static readonly FieldRef<ContextRankConfig, BlueprintUnitProperty> m_CustomProperty;
        public static readonly FieldRef<ContextRankConfig, StatType> m_Stat;
        public static readonly FieldRef<ContextRankConfig, BlueprintCharacterClass[]> m_Class;
        public static readonly FieldRef<ContextRankConfig, BlueprintArchetype> Archetype;
        public static readonly FieldRef<ContextRankConfig, BlueprintFeature[]> m_FeatureList;
        //public static readonly Type typeof_CustomProgressionItem = typeof(ContextRankConfig).GetNestedType("CustomProgressionItem", BindingFlags.NonPublic);
        //public static readonly FieldRef<object, int> BaseValue = FieldRefAccess<object, int>("BaseValue");
        //public static readonly FieldRef<object, int> ProgressionValue = FieldRefAccess<object, int>("ProgressionValue");
        public static readonly FieldRef<ContextRankConfig, object> m_CustomProgression;
        #endregion

        #region LootItem
        public static readonly FieldRef<LootItem, BlueprintItem> LootItem_Item;
        public static readonly FieldRef<LootItemsPackFixed, LootItem> LootItemsPackFixed_Item;
        public static readonly FieldRef<LootItemsPackFixed, int> LootItemsPackFixed_Count;
        #endregion

        #region other

        public static readonly FieldRef<BlueprintActivatableAbility, CommandType> m_ActivateWithUnitCommand;
        public static readonly FieldRef<BlueprintArchetype, BlueprintCharacterClass> m_ParentClass;
        public static readonly FieldRef<AbilityAoERadius, Feet> m_Radius;
        public static readonly FieldRef<AbilityAoERadius, TargetType> m_TargetType;

        #endregion

        //public static readonly FieldRef<BlueprintBuff, int> m_Flags;
        //public static readonly FieldRef<ContextActionDealDamage, int> m_Type_ContextActionDealDamage = FieldRefAccess<ContextActionDealDamage, int>("m_Type");
        //public static readonly FieldRef<> m_ = FieldRefAccess<>("");

        static Access()
        {
            // no inline definitions, so we get more meaningful debug expections
            try
            {
                Main.DebugLog("Access.m_Key");
                m_Key = FieldRefAccess<LocalizedString, string>("m_Key");

                Main.DebugLog("Access.m_AssetGuid");
                m_AssetGuid = FieldRefAccess<BlueprintScriptableObject, string>("m_AssetGuid");

                Main.DebugLog("Access.BlueprintUnitFact");
                m_DisplayName = FieldRefAccess<BlueprintUnitFact, LocalizedString>("m_DisplayName");
                m_Description = FieldRefAccess<BlueprintUnitFact, LocalizedString>("m_Description");
                m_Icon = FieldRefAccess<BlueprintUnitFact, Sprite>("m_Icon");

                Main.DebugLog("Access.BlueprintItemWeapon");
                m_Enchantments = FieldRefAccess<BlueprintItemWeapon, BlueprintWeaponEnchantment[]>("m_Enchantments");

                Main.DebugLog("Access.BlueprintItemEnchantment");
                BlueprintItemEnchantment_Description = FieldRefAccess<BlueprintItemEnchantment, LocalizedString>("m_Description");
                m_EnchantName = FieldRefAccess<BlueprintItemEnchantment, LocalizedString>("m_EnchantName");
                m_EnchantmentCost = FieldRefAccess<BlueprintItemEnchantment, int>("m_EnchantmentCost");

                Main.DebugLog("Access.BlueprintItem");
                BlueprintItem_DisplayNameText = FieldRefAccess<BlueprintItem, LocalizedString>("m_DisplayNameText");
                BlueprintItem_DescriptionText = FieldRefAccess<BlueprintItem, LocalizedString>("m_DescriptionText");
                BlueprintItem_FlavorText = FieldRefAccess<BlueprintItem, LocalizedString>("m_FlavorText");
                BlueprintItem_Icon = FieldRefAccess<BlueprintItem, Sprite>("m_Icon");
                BlueprintItem_Weight = FieldRefAccess<BlueprintItem, float>("m_Weight");
                m_Cost = FieldRefAccess<BlueprintItem, int>("m_Cost");

                Main.DebugLog("Access.ContextRankConfig");
                m_BaseValueType = FieldRefAccess<ContextRankConfig, ContextRankBaseValueType>("m_BaseValueType");
                ContextRankConfig_Type = FieldRefAccess<ContextRankConfig, AbilityRankType>("m_Type");
                m_Progression = FieldRefAccess<ContextRankConfig, ContextRankProgression>("m_Progression");
                m_UseMin = FieldRefAccess<ContextRankConfig, bool>("m_UseMin");
                m_Min = FieldRefAccess<ContextRankConfig, int>("m_Min");
                m_UseMax = FieldRefAccess<ContextRankConfig, bool>("m_UseMax");
                m_Max = FieldRefAccess<ContextRankConfig, int>("m_Max");
                m_StartLevel = FieldRefAccess<ContextRankConfig, int>("m_StartLevel");
                m_StepLevel = FieldRefAccess<ContextRankConfig, int>("m_StepLevel");
                m_Feature = FieldRefAccess<ContextRankConfig, BlueprintFeature>("m_Feature");
                m_ExceptClasses = FieldRefAccess<ContextRankConfig, bool>("m_ExceptClasses");
                m_CustomProperty = FieldRefAccess<ContextRankConfig, BlueprintUnitProperty>("m_CustomProperty");
                m_Stat = FieldRefAccess<ContextRankConfig, StatType>("m_Stat");
                m_Class = FieldRefAccess<ContextRankConfig, BlueprintCharacterClass[]>("m_Class");
                Archetype = FieldRefAccess<ContextRankConfig, BlueprintArchetype>("Archetype");
                m_FeatureList = FieldRefAccess<ContextRankConfig, BlueprintFeature[]>("m_FeatureList");
                m_CustomProgression = FieldRefAccess<ContextRankConfig, object>("m_CustomProgression");

                Main.DebugLog("Access.LootItem");
                LootItem_Item = FieldRefAccess<LootItem, BlueprintItem>("m_Item");
                LootItemsPackFixed_Item = FieldRefAccess<LootItemsPackFixed, LootItem>("m_Item");
                LootItemsPackFixed_Count = FieldRefAccess<LootItemsPackFixed, int>("m_Count");

                Main.DebugLog("Access.other");
                m_ActivateWithUnitCommand = FieldRefAccess<BlueprintActivatableAbility, CommandType>("m_ActivateWithUnitCommand");
                m_ParentClass = FieldRefAccess<BlueprintArchetype, BlueprintCharacterClass>("m_ParentClass");
                m_Radius = FieldRefAccess<AbilityAoERadius, Feet>("m_Radius");
                m_TargetType = FieldRefAccess<AbilityAoERadius, TargetType>("m_TargetType");

                //Main.DebugLog("Access.new");
                //var mflag_info = HarmonyLib.AccessTools.Field(typeof(BlueprintBuff), "m_Flags");
                //Main.DebugLog($"m_Flag is Enum {mflag_info.FieldType.IsEnum} and underlying: " + Enum.GetUnderlyingType(mflag_info.FieldType).ToString());
                //m_Flags = FieldRefAccess<BlueprintBuff, System.Int32>("m_Flags");

                Main.DebugLog("Access done");
            }
            catch (Exception e)
            {
                Main.DebugError(e);
            }
        }

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

        public static ContextDiceValue DiceZero = HelperEA.CreateContextDiceValue(DiceType.Zero, ValueZero);
        public static ContextDiceValue DiceOne = HelperEA.CreateContextDiceValue(DiceType.One, ValueOne);
        public static ContextDiceValue Dice1d3 = HelperEA.CreateContextDiceValue(DiceType.D3, ValueOne);
        public static ContextDiceValue Dice1d4 = HelperEA.CreateContextDiceValue(DiceType.D4, ValueOne);
        public static ContextDiceValue Dice1d6 = HelperEA.CreateContextDiceValue(DiceType.D6, ValueOne);
        public static ContextDiceValue Dice1d8 = HelperEA.CreateContextDiceValue(DiceType.D8, ValueOne);

        public static ContextDurationValue DurationZero = HelperEA.CreateContextDuration(0);
        public static ContextDurationValue Duration1Round = HelperEA.CreateContextDuration(1);
        public static ContextDurationValue DurationRankInRounds = HelperEA.CreateContextDuration(Contexts.ValueRank, DurationRate.Rounds);
        public static ContextDurationValue DurationRankInMinutes = HelperEA.CreateContextDuration(Contexts.ValueRank, DurationRate.Minutes);
        public static ContextDurationValue Duration24Hours = HelperEA.CreateContextDuration(1, DurationRate.Days);

        public static BlueprintSummonPool SummonPool = Main.library.Get<BlueprintSummonPool>("d94c93e7240f10e41ae41db4c83d1cbe");
        public static ActionList AfterSpawnAction = Helper.CreateActionList(HelperEA.CreateApplyBuff(Main.library.Get<BlueprintBuff>("0dff842f06edace43baf8a2f44207045"), DurationZero, false, false, false, false, true));

        public static Sprite IconPlaceHolder = Helper.Image2Sprite.Create("PlaceHolderIcon.png");
    }

    public static class Strings
    {
        public static LocalizedString Empty = new LocalizedString();
        public static LocalizedString SavingThrowNone = Main.library.Get<BlueprintAbility>("b6010dda6333bcf4093ce20f0063cd41").LocalizedSavingThrow;
        public static LocalizedString RoundsPerLevelDuration = Main.library.Get<BlueprintAbility>("486eaff58293f6441a5c2759c4872f98").LocalizedDuration;
        public static LocalizedString HourPerLevelDuration = Main.library.Get<BlueprintAbility>("9e1ad5d6f87d19e4d8883d63a6e35568").LocalizedDuration;
    }

    public static class HelperEA
    {
        public static T Get<T>(this LibraryScriptableObject library, string assetId) where T : BlueprintScriptableObject
        {
            return (T)library.BlueprintsByAssetId[assetId];
        }

        public static T TryGet<T>(this LibraryScriptableObject library, String assetId) where T : BlueprintScriptableObject
        {
            BlueprintScriptableObject result;
            if (library.BlueprintsByAssetId.TryGetValue(assetId, out result))
            {
                return (T)result;
            }
            return null;
        }

        public static void AddAsset(this LibraryScriptableObject library, BlueprintScriptableObject blueprint, string guid, bool overwrite = false)
        {
            int index = library.GetAllBlueprints().FindIndex(a => a.AssetGuid == guid);

            if (index < 0)
            {
                Access.m_AssetGuid(blueprint) = guid;
                library.GetAllBlueprints().Add(blueprint);
                library.BlueprintsByAssetId[guid] = blueprint;
            }
            else if (overwrite)
            {
                Main.DebugLogAlways("Overwriting Asset: " + guid);
                Access.m_AssetGuid(blueprint) = guid;
                library.GetAllBlueprints()[index] = blueprint;
                library.BlueprintsByAssetId[guid] = blueprint;
            }
            else
                Main.DebugLogAlways("[Error] Duplicate Asset ID: " + guid);
        }

        public static ContextDiceValue CreateContextDiceValue(DiceType dice, ContextValue diceCount = null, ContextValue bonus = null)
        {
            return new ContextDiceValue()
            {
                DiceType = dice,
                DiceCountValue = diceCount ?? CreateContextValueRank(),
                BonusValue = bonus ?? 0
            };
        }

        public static ContextValue CreateContextValueRank(AbilityRankType value = AbilityRankType.Default)
        {
            return CreateContextValue(value);
        }

        public static ContextValue CreateContextValue(AbilityRankType value)
        {
            return new ContextValue() { ValueType = ContextValueType.Rank, ValueRank = value };
        }

        public static ContextValue CreateContextValue(AbilitySharedValue value)
        {
            return new ContextValue() { ValueType = ContextValueType.Shared, ValueShared = value };
        }

        public static ContextDurationValue CreateContextDuration(ContextValue bonus = null, DurationRate rate = DurationRate.Rounds, DiceType diceType = DiceType.Zero, ContextValue diceCount = null)
        {
            return new ContextDurationValue()
            {
                BonusValue = bonus ?? CreateContextValueRank(),
                Rate = rate,
                DiceCountValue = diceCount ?? 0,
                DiceType = diceType
            };
        }

        public static ContextActionApplyBuff CreateApplyBuff(this BlueprintBuff buff, ContextDurationValue duration, bool fromSpell, bool dispellable = true, bool toCaster = false, bool asChild = false, bool permanent = false)
        {
            var result = Helper.Create<ContextActionApplyBuff>();
            result.Buff = buff;
            result.DurationValue = duration;
            result.IsFromSpell = fromSpell;
            result.IsNotDispelable = !dispellable;
            result.ToCaster = toCaster;
            result.AsChild = asChild;
            result.Permanent = permanent;
            return result;
        }

        public static BlueprintFeature CreateFeature(string name, string displayName, string description, string guid, Sprite icon,
            FeatureGroup group, params BlueprintComponent[] components)
        {
            var feat = Helper.Create<BlueprintFeature>();
            SetFeatureInfo(feat, name, displayName, description, guid, icon, group, components);
            return feat;
        }

        public static void SetFeatureInfo(BlueprintFeature feat, string name, string displayName, string description, string guid, Sprite icon,
            FeatureGroup group, params BlueprintComponent[] components)
        {
            feat.name = name;
            feat.SetComponents(components);
            feat.Groups = new FeatureGroup[] { group };
            feat.SetNameDescriptionIcon(displayName, description, icon);
            Main.library.AddAsset(feat, guid);
        }

        public static void SetComponents(this BlueprintScriptableObject obj, params BlueprintComponent[] components)
        {
            // Fix names of components. Generally this doesn't matter, but if they have serialization state,
            // then their name needs to be unique.
            var names = new HashSet<string>();
            foreach (var c in components)
            {
                if (string.IsNullOrEmpty(c.name))
                {
                    c.name = $"${c.GetType().Name}";
                }
                if (!names.Add(c.name))
                {
                    //SaveCompatibility.CheckComponent(obj, c);
                    string name;
                    for (int i = 0; !names.Add(name = $"{c.name}${i}"); i++) ;
                    c.name = name;
                }
                //Log.Validate(c, obj);
            }

            obj.ComponentsArray = components;
        }

        public static void SetComponents(this BlueprintScriptableObject obj, IEnumerable<BlueprintComponent> components)
        {
            SetComponents(obj, components.ToArray());
        }

        public static void AddComponent(this BlueprintScriptableObject obj, BlueprintComponent component)
        {
            obj.SetComponents(Helper.Append(obj.ComponentsArray, component));
        }

        public static void AddComponents(this BlueprintScriptableObject obj, params BlueprintComponent[] components)
        {
            var c = obj.ComponentsArray.ToList();
            c.AddRange(components);
            obj.SetComponents(c.ToArray());
        }

        public static void ReplaceComponent<T>(this BlueprintScriptableObject obj, BlueprintComponent replacement) where T : BlueprintComponent
        {
            ReplaceComponent(obj, obj.GetComponent<T>(), replacement);
        }

        public static void ReplaceComponent<T>(this BlueprintScriptableObject obj, Action<T> action) where T : BlueprintComponent
        {
            var replacement = Helper.Instantiate(obj.GetComponent<T>());
            action(replacement);
            ReplaceComponent(obj, obj.GetComponent<T>(), replacement);
        }

        public static void ReplaceComponent(this BlueprintScriptableObject obj, BlueprintComponent original, BlueprintComponent replacement)
        {
            // Note: make a copy so we don't mutate the original component
            // (in case it's a clone of a game one).
            var components = obj.ComponentsArray;
            var newComponents = new BlueprintComponent[components.Length];
            for (int i = 0; i < components.Length; i++)
            {
                var c = components[i];
                newComponents[i] = c == original ? replacement : c;
            }
            obj.SetComponents(newComponents); // fix up names if needed
        }

        public static void RemoveComponents<T>(this BlueprintScriptableObject obj) where T : BlueprintComponent
        {
            var compnents_to_remove = obj.GetComponents<T>().ToArray();
            foreach (var c in compnents_to_remove)
            {
                obj.SetComponents(obj.ComponentsArray.RemoveFromArray(c));
            }
        }

        public static void RemoveComponents<T>(this BlueprintScriptableObject obj, Predicate<T> predicate) where T : BlueprintComponent
        {
            var compnents_to_remove = obj.GetComponents<T>().ToArray();
            foreach (var c in compnents_to_remove)
            {
                if (predicate(c))
                {
                    obj.SetComponents(obj.ComponentsArray.RemoveFromArray(c));
                }
            }
        }

        public static T[] RemoveFromArray<T>(this T[] array, T value)
        {
            var list = array.ToList();
            return list.Remove(value) ? list.ToArray() : array;
        }

        public static void SetName(this BlueprintUnitFact feature, string displayName)
        {
            Access.m_DisplayName(feature) = CreateString(feature.name + ".Name", displayName);
        }

        public static void SetNameDescriptionIcon(this BlueprintUnitFact feature, string displayName, string description, Sprite icon = null)
        {
            Access.m_DisplayName(feature) = CreateString(feature.name + ".Name", displayName);
            Access.m_Description(feature) = CreateString(feature.name + ".Description", description);
            if (icon != null)
                Access.m_Icon(feature) = icon;
        }

        public static void SetNameDescriptionIcon(this BlueprintUnitFact feature, BlueprintUnitFact feature2)
        {
            Access.m_DisplayName(feature) = CreateString(feature.name + ".Name", feature2.Name);
            Access.m_Description(feature) = CreateString(feature.name + ".Description", feature2.Description);
            Access.m_Icon(feature) = feature.Icon;
        }

        public static LocalizedString CreateString(string key, string value)
        {
            // See if we used the text previously.
            // (It's common for many features to use the same localized text.
            // In that case, we reuse the old entry instead of making a new one.)
            LocalizedString localized;
            if (_textToLocalizedString.TryGetValue(value, out localized))
            {
                return localized;
            }
            var strings = LocalizationManager.CurrentPack.Strings;
            String oldValue;
            if (strings.TryGetValue(key, out oldValue) && value != oldValue)
            {
#if DEBUG
                Main.DebugLogAlways($"Info: duplicate localized string `{key}`, different text.");
#endif
            }
            strings[key] = value;
            localized = new LocalizedString();
            Access.m_Key(localized) = key;
            _textToLocalizedString[value] = localized;
            return localized;
        }

        // All localized strings created in this mod, mapped to their localized key. Populated by CreateString.
        public static Dictionary<String, LocalizedString> _textToLocalizedString = new Dictionary<string, LocalizedString>();

        public static PrerequisiteFeature PrerequisiteFeature(this BlueprintFeature feat, bool any = false)
        {
            var result = Helper.Create<PrerequisiteFeature>();
            result.Feature = feat;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }

        public static PrerequisiteFullStatValue PrerequisiteFullStatValue(this StatType stat, int value, bool any = false)
        {
            var result = Helper.Create<PrerequisiteFullStatValue>();
            result.Stat = stat;
            result.Value = value;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }

        public static BlueprintBuff CreateBuff(String name, String displayName, String description, String guid, Sprite icon, PrefabLink fxOnStart, params BlueprintComponent[] components)
        {
            var buff = Helper.Create<BlueprintBuff>();
            buff.name = name;
            buff.FxOnStart = fxOnStart ?? new PrefabLink();
            buff.FxOnRemove = new PrefabLink();
            buff.SetComponents(components);
            buff.SetNameDescriptionIcon(displayName, description, icon);
            Main.library.AddAsset(buff, guid);
            return buff;
        }

        public static BlueprintActivatableAbility CreateActivatableAbility(String name, String displayName, String description, string assetId, Sprite icon, BlueprintBuff buff, AbilityActivationType activationType, CommandType commandType, AnimationClip activateWithUnitAnimation, params BlueprintComponent[] components)
        {
            var ability = Helper.Create<BlueprintActivatableAbility>();
            ability.name = name;
            ability.SetNameDescriptionIcon(displayName, description, icon);
            ability.Buff = buff;
            ability.ResourceAssetIds = Array.Empty<string>();
            ability.ActivationType = activationType;
            Access.m_ActivateWithUnitCommand(ability) = commandType;
            ability.SetComponents(components);
            ability.ActivateWithUnitAnimation = activateWithUnitAnimation;
            Main.library.AddAsset(ability, assetId);
            return ability;
        }

        public static ContextRankConfig CreateContextRankConfig(ContextRankBaseValueType baseValueType = ContextRankBaseValueType.CasterLevel, ContextRankProgression progression = ContextRankProgression.AsIs, AbilityRankType type = AbilityRankType.Default, int? min = null, int? max = null, int startLevel = 0, int stepLevel = 0, bool exceptClasses = false, StatType stat = StatType.Unknown, BlueprintUnitProperty customProperty = null, BlueprintCharacterClass[] classes = null, BlueprintArchetype archetype = null, BlueprintFeature feature = null, BlueprintFeature[] featureList = null/*, (int, int)[] customProgression = null*/)
        {
            var config = Helper.Create<ContextRankConfig>();
            Access.ContextRankConfig_Type(config) = type;
            Access.m_BaseValueType(config) = baseValueType;
            Access.m_Progression(config) = progression;
            Access.m_UseMin(config) = min.HasValue;
            Access.m_Min(config) = min.GetValueOrDefault();
            Access.m_UseMax(config) = max.HasValue;
            Access.m_Max(config) = max.GetValueOrDefault();
            Access.m_StartLevel(config) = startLevel;
            Access.m_StepLevel(config) = stepLevel;
            Access.m_Feature(config) = feature;
            Access.m_ExceptClasses(config) = exceptClasses;
            Access.m_CustomProperty(config) = customProperty;
            Access.m_Stat(config) = stat;
            Access.m_Class(config) = classes ?? Array.Empty<BlueprintCharacterClass>();
            Access.Archetype(config) = archetype;
            Access.m_FeatureList(config) = featureList ?? Array.Empty<BlueprintFeature>();

            //if (customProgression != null)
            //{
            //    var items = Array.CreateInstance(Access.typeof_CustomProgressionItem, customProgression.Length);
            //    for (int i = 0; i < items.Length; i++)
            //    {
            //        var item = Activator.CreateInstance(Access.typeof_CustomProgressionItem);
            //        var p = customProgression[i];
            //        Access.set_BaseValue(item, p.Item1);
            //        Access.set_ProgressionValue(item, p.Item2);
            //        items.SetValue(item, i);
            //    }
            //    Access.set_CustomProgression(config, items);
            //}

            return config;
        }

        public static SpellDescriptorComponent CreateSpellDescriptor(SpellDescriptor? descriptor = null)
        {
            var s = Helper.Create<SpellDescriptorComponent>();
            s.Descriptor = descriptor ?? SpellDescriptor.None;
            return s;
        }

        public static AddCondition CreateAddCondition(UnitCondition condition)
        {
            var a = Helper.Create<AddCondition>();
            a.Condition = condition;
            return a;
        }

        public static ContextConditionCasterHasFact CreateConditionCasterHasFact(BlueprintUnitFact fact, bool not = false)
        {
            var c = Helper.Create<ContextConditionCasterHasFact>();
            c.Fact = fact;
            c.Not = not;
            return c;
        }

        public static Conditional CreateConditional(Condition condition, GameAction ifTrue, GameAction ifFalse = null, bool OperationAnd = true)
        {
            var c = Helper.Create<Conditional>();
            c.ConditionsChecker = new ConditionsChecker() { Conditions = condition.ObjToArray(), Operation = OperationAnd ? Operation.And : Operation.Or };
            c.IfTrue = Helper.CreateActionList(ifTrue);
            c.IfFalse = Helper.CreateActionList(ifFalse);
            return c;
        }

        public static Conditional CreateConditional(Condition[] condition, GameAction[] ifTrue, GameAction[] ifFalse = null, bool OperationAnd = true)
        {
            var c = Helper.Create<Conditional>();
            c.ConditionsChecker = new ConditionsChecker() { Conditions = condition, Operation = OperationAnd ? Operation.And : Operation.Or };
            c.IfTrue = Helper.CreateActionList(ifTrue);
            c.IfFalse = Helper.CreateActionList(ifFalse);
            return c;
        }

        public static Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff CreateContextActionApplyBuff(BlueprintBuff buff, ContextDurationValue duration, bool is_from_spell = false, bool is_child = false, bool is_permanent = false, bool dispellable = true, int duration_seconds = 0)
        {
            var apply_buff = Helper.Create<ContextActionApplyBuff>();
            apply_buff.IsFromSpell = is_from_spell;
            apply_buff.Buff = buff;
            apply_buff.Permanent = is_permanent;
            apply_buff.DurationValue = duration;
            apply_buff.IsNotDispelable = !dispellable;
            apply_buff.UseDurationSeconds = duration_seconds > 0;
            apply_buff.DurationSeconds = duration_seconds;
            apply_buff.AsChild = is_child;
            apply_buff.ToCaster = false;
            return apply_buff;
        }

        public static void AddCombatFeats(LibraryScriptableObject library, params BlueprintFeature[] feats)
        {
            try
            {
                CallOfTheWild.Helpers.AddCombatFeats(library, feats);
                return;
            }
            catch (System.Exception)
            {
            }

            var featSelectionIds = new string[] {
                "247a4068296e8be42890143f451b4b45", //basicFeatSelection
                "66befe7b24c42dd458952e3c47c93563", //magusFeatSelection                
                "41c8486641f7d6d4283ca9dae4147a9f", //FighterFeatSelection            
                "da03141df23f3fe45b0c7c323a8e5a0e", //EldritchKnightFeatSelection                
                "79c6421dbdb028c4fa0c31b8eea95f16", //WarDomainGreaterFeatSelection
                "c5158a6622d0b694a99efb1d0025d2c1", //combat trick
            };

            foreach (var id in featSelectionIds)
            {
                if (id != null)
                    AddFeats(library, id, feats);
            }
        }

        public static void AddFeats(LibraryScriptableObject library, params BlueprintFeature[] feats)
        {
            Helper.AppendAndReplace(ref library.Get<BlueprintFeatureSelection>("247a4068296e8be42890143f451b4b45").AllFeatures, feats);
        }

        public static void AddFeats(LibraryScriptableObject library, string featSelectionId, params BlueprintFeature[] feats)
        {
            Helper.AppendAndReplace(ref library.Get<BlueprintFeatureSelection>(featSelectionId).AllFeatures, feats);
        }

        public static AddFacts CreateAddFact(BlueprintUnitFact fact)
        {
            var result = Helper.Create<AddFacts>();
            result.name = $"AddFacts${fact.name}";
            result.Facts = new BlueprintUnitFact[] { fact };
            return result;
        }

        public static BlueprintFeature ActivatableAbilityToFeature(BlueprintActivatableAbility ability, bool hide = true, string guid = null)
        {
            //string name = ability.name.EndsWith("ActivatableAbility") ? ability.name.Substring(0, ability.name.Length - 18) : ability.name; name += "Feature";
            string name = ability.name + "Feature";
            var feature = HelperEA.CreateFeature(
                name,
                ability.Name,
                ability.Description,
                guid ?? GuidManager.i.Get(name),
                ability.Icon,
                FeatureGroup.None,
                HelperEA.CreateAddFact(ability));

            if (hide)
            {
                feature.HideInCharacterSheetAndLevelUp = true;
                feature.HideInUI = true;
            }
            return feature;
        }

        public static IncreaseActivatableAbilityGroupSize CreateIncreaseActivatableAbilityGroupSize(ActivatableAbilityGroup group)
        {
            var i = Helper.Create<IncreaseActivatableAbilityGroupSize>();
            i.Group = group;
            return i;
        }

        public static PrerequisiteStatValue PrerequisiteStatValue(StatType stat, int value, bool any = false)
        {
            var result = Helper.Create<PrerequisiteStatValue>();
            result.Stat = stat;
            result.Value = value;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }

        public static PrerequisiteClassLevel PrerequisiteClassLevel(BlueprintCharacterClass @class, int level, bool any = false)
        {
            var result = Helper.Create<PrerequisiteClassLevel>();
            result.CharacterClass = @class;
            result.Level = level;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }

        public static ManeuverDefenceBonus CreateManeuverDefenseBonus(CombatManeuver maneuver_type, int bonus)
        {
            var m = Helper.Create<ManeuverDefenceBonus>();
            m.Bonus = bonus;
            m.Type = maneuver_type;
            return m;
        }

        public static ManeuverBonus CreateManeuverBonus(CombatManeuver maneuver_type, int bonus)
        {
            var m = Helper.Create<ManeuverBonus>();
            m.Bonus = bonus;
            m.Type = maneuver_type;
            return m;
        }

        public static T CopyAndAdd<T>(this LibraryScriptableObject library, String assetId, String newName, String newAssetId, String newAssetId2 = null) where T : BlueprintScriptableObject
        {
            return CopyAndAdd(library, Get<T>(library, assetId), newName, newAssetId, newAssetId2);
        }

        public static T CopyAndAdd<T>(this LibraryScriptableObject library, T original, String newName, String newAssetId, String newAssetId2 = null) where T : BlueprintScriptableObject
        {
            var clone = Helper.Instantiate(original);
            clone.name = newName;
            var id = newAssetId2 != null ? MergeIds(newAssetId, newAssetId2) : newAssetId;
            AddAsset(library, clone, id);
            return clone;
        }

        public static String MergeIds(String guid1, String guid2, String guid3 = null)
        {
            // Parse into low/high 64-bit numbers, and then xor the two halves.
            ulong low = ParseGuidLow(guid1);
            ulong high = ParseGuidHigh(guid1);

            low ^= ParseGuidLow(guid2);
            high ^= ParseGuidHigh(guid2);

            if (guid3 != null)
            {
                low ^= ParseGuidLow(guid3);
                high ^= ParseGuidHigh(guid3);
            }

            var result = high.ToString("x16") + low.ToString("x16");
            Main.DebugLog($"MergeIds {guid1} + {guid2} + {guid3} = {result}");
            return result;
        }
        static ulong ParseGuidLow(String id) => ulong.Parse(id.Substring(id.Length - 16), System.Globalization.NumberStyles.HexNumber);
        static ulong ParseGuidHigh(String id) => ulong.Parse(id.Substring(0, id.Length - 16), System.Globalization.NumberStyles.HexNumber);

        public static LevelEntry LevelEntry(int level, params BlueprintFeatureBase[] features)
        {
            var entry = new LevelEntry() { Level = level };
            entry.Features.AddRange(features);
            return entry;
        }

        public static AddFacts CreateAddFacts(params BlueprintUnitFact[] facts)
        {
            var result = Helper.Create<AddFacts>();
            result.Facts = facts;
            return result;
        }

        public static BlueprintFeatureSelection CreateFeatureSelection(String name, String displayName, String description, String guid, Sprite icon, FeatureGroup group, params BlueprintComponent[] components)
        {
            var feat = Helper.Create<BlueprintFeatureSelection>();
            SetFeatureInfo(feat, name, displayName, description, guid, icon, group, components);
            feat.Group = group;
            return feat;
        }

        public static AddInitiatorAttackWithWeaponTrigger CreateAddInitiatorAttackWithWeaponTrigger(Kingmaker.ElementsSystem.ActionList action, bool only_hit = true, bool critical_hit = false, bool check_weapon_range_type = false, bool reduce_hp_to_zero = false, bool on_initiator = false, AttackTypeAttackBonus.WeaponRangeType range_type = AttackTypeAttackBonus.WeaponRangeType.Melee, bool wait_for_attack_to_resolve = false, bool only_first_hit = false)
        {
            var t = Helper.Create<AddInitiatorAttackWithWeaponTrigger>();
            t.Action = action;
            t.OnlyHit = only_hit;
            t.CriticalHit = critical_hit;
            t.CheckWeaponRangeType = check_weapon_range_type;
            t.RangeType = range_type;
            t.ReduceHPToZero = reduce_hp_to_zero;
            t.ActionsOnInitiator = on_initiator;
            t.WaitForAttackResolve = wait_for_attack_to_resolve;
            t.OnlyOnFirstAttack = only_first_hit;
            return t;
        }

        public static CopyOf.CallOfTheWild.NewMechanics.ContextWeaponTypeDamageBonus CreateContextWeaponTypeDamageBonus(ContextValue bonus, params BlueprintWeaponType[] weapon_types)
        {
            var c = Helper.Create<CopyOf.CallOfTheWild.NewMechanics.ContextWeaponTypeDamageBonus>();
            c.Value = bonus;
            c.weapon_types = weapon_types;
            return c;
        }

        public static AbilityEffectRunAction CreateRunActions(params GameAction[] actions)
        {
            var result = Helper.Create<AbilityEffectRunAction>();
            result.Actions = Helper.CreateActionList(actions);
            return result;
        }

        public static BlueprintAbility CreateAbility(String name, String displayName, String description, String guid, Sprite icon, AbilityType type, CommandType actionType, AbilityRange range, String duration, String savingThrow, params BlueprintComponent[] components)
        {
            var ability = Helper.Create<BlueprintAbility>();
            ability.name = name;
            ability.SetComponents(components);
            ability.SetNameDescriptionIcon(displayName, description, icon);
            ability.ResourceAssetIds = Array.Empty<string>();

            ability.Type = type;
            ability.ActionType = actionType;
            ability.Range = range;
            ability.LocalizedDuration = CreateString($"{name}.Duration", duration);
            ability.LocalizedSavingThrow = CreateString($"{name}.SavingThrow", savingThrow);

            Main.library.AddAsset(ability, guid);
            return ability;
        }

        public static void SetMiscAbilityParametersTouchHarmful(this BlueprintAbility ability, bool works_on_allies = true, Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell.CastAnimationStyle animation = Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell.CastAnimationStyle.Touch, Kingmaker.View.Animation.CastAnimationStyle animation_style = Kingmaker.View.Animation.CastAnimationStyle.CastActionTouch)
        {
            ability.CanTargetFriends = works_on_allies;
            ability.CanTargetEnemies = true;
            ability.CanTargetSelf = works_on_allies;
            ability.CanTargetPoint = false;
            ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            ability.EffectOnAlly = works_on_allies ? AbilityEffectOnUnit.Harmful : AbilityEffectOnUnit.None;
            ability.Animation = animation;
            //ability.AnimationStyle = animation_style;
        }

        public static void SetMiscAbilityParametersSelfOnly(this BlueprintAbility ability, Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell.CastAnimationStyle animation = Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell.CastAnimationStyle.Self, Kingmaker.View.Animation.CastAnimationStyle animation_style = Kingmaker.View.Animation.CastAnimationStyle.CastActionSelf)
        {
            ability.CanTargetFriends = false;
            ability.CanTargetEnemies = false;
            ability.CanTargetSelf = true;
            ability.CanTargetPoint = false;
            ability.EffectOnEnemy = AbilityEffectOnUnit.None;
            ability.EffectOnAlly = AbilityEffectOnUnit.Helpful;
            ability.Animation = animation;
            //ability.AnimationStyle = animation_style;
        }

        public static PrerequisiteFeaturesFromList PrerequisiteFeaturesFromList(BlueprintFeature[] features, bool any = false)
        {
            var result = Helper.Create<PrerequisiteFeaturesFromList>();
            result.Features = features;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            result.Amount = 1;
            return result;
        }

        public static ContextActionDealDamage CreateActionDealDamage(PhysicalDamageForm physical, ContextDiceValue damage, bool isAoE = false, bool halfIfSaved = false, bool IgnoreCritical = false)
        {
            // physical damage
            var c = Helper.Create<ContextActionDealDamage>();
            c.DamageType = new DamageTypeDescription()
            {
                Type = DamageType.Physical,
                Common = new DamageTypeDescription.CommomData(),
                Physical = new DamageTypeDescription.PhysicalData() { Form = physical }
            };
            c.Duration = CreateContextDuration(0);
            c.Value = damage;
            c.IsAoE = isAoE;
            c.HalfIfSaved = halfIfSaved;
            c.IgnoreCritical = IgnoreCritical;
            return c;
        }

        public static ContextActionDealDamage CreateActionDealDamage(DamageEnergyType energy, ContextDiceValue damage, bool isAoE = false, bool halfIfSaved = false, bool IgnoreCritical = false)
        {
            // energy damage
            var c = Helper.Create<ContextActionDealDamage>();
            c.DamageType = new DamageTypeDescription()
            {
                Type = DamageType.Energy,
                Energy = energy,
                Common = new DamageTypeDescription.CommomData(),
                Physical = new DamageTypeDescription.PhysicalData()
            };
            c.Duration = CreateContextDuration(0);
            c.Value = damage;
            c.IsAoE = isAoE;
            c.HalfIfSaved = halfIfSaved;
            c.IgnoreCritical = IgnoreCritical;
            return c;
        }

        public static ContextActionDealDamage CreateActionDealDamageNOTREADY(StatType abilityType, ContextDiceValue damage, bool drain = false, bool isAoE = false, bool halfIfSaved = false, bool IgnoreCritical = false)
        {
            var c = Helper.Create<ContextActionDealDamage>();
            //Access.m_Type_ContextActionDealDamage(c) = 1;  // AbilityDamage // TODO: fix assignment
            c.Duration = HelperEA.CreateContextDuration(0);
            c.AbilityType = abilityType;
            c.Value = damage;
            c.IsAoE = isAoE;
            c.HalfIfSaved = halfIfSaved;
            c.Drain = drain;
            c.IgnoreCritical = IgnoreCritical;
            return c;
        }

        public static PrerequisiteNoFeature PrerequisiteNoFeature(this BlueprintFeature feat, bool any = false)
        {
            var result = Helper.Create<PrerequisiteNoFeature>();
            result.Feature = feat;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }

        public static ContextConditionHasBuff CreateConditionHasBuff(this BlueprintBuff buff)
        {
            var hasBuff = Helper.Create<ContextConditionHasBuff>();
            hasBuff.Buff = buff;
            return hasBuff;
        }

        public static AddContextStatBonus CreateAddContextStatBonus(StatType stat, ModifierDescriptor descriptor, ContextValueType type = ContextValueType.Rank, AbilityRankType rankType = AbilityRankType.Default, int multiplier = 1)
        {
            var addStat = Helper.Create<AddContextStatBonus>();
            addStat.Stat = stat;
            addStat.Value = new ContextValue() { ValueType = type };
            addStat.Descriptor = descriptor;
            addStat.Value.ValueRank = rankType;
            addStat.Multiplier = multiplier;
            return addStat;
        }

        public static AddFactContextActions CreateAddFactContextActions(GameAction activated = null, GameAction deactivated = null, GameAction newRound = null)
        {
            var a = Helper.Create<AddFactContextActions>();
            a.Activated = Helper.CreateActionList(activated);
            a.Deactivated = Helper.CreateActionList(deactivated);
            a.NewRound = Helper.CreateActionList(newRound);
            return a;
        }

        public static AddFactContextActions CreateAddFactContextActions(GameAction[] activated = null, GameAction[] deactivated = null, GameAction[] newRound = null)
        {
            var a = Helper.Create<AddFactContextActions>();
            a.Activated = Helper.CreateActionList(activated);
            a.Deactivated = Helper.CreateActionList(deactivated);
            a.NewRound = Helper.CreateActionList(newRound);
            return a;
        }

        public static bool AddToAbilityVariants(this BlueprintAbility parent, params BlueprintAbility[] variants)
        {
            var comp = parent.GetComponent<AbilityVariants>();

            Helper.AppendAndReplace(ref comp.Variants, variants);

            foreach (var v in variants)
            {
                v.Parent = parent;
            }
            return true;
        }

        public static AbilityVariants CreateAbilityVariants(BlueprintAbility parent, params BlueprintAbility[] variants)
        {
            var a = Helper.Create<AbilityVariants>();
            a.Variants = variants;
            foreach (var v in variants)
            {
                v.Parent = parent;
            }
            return a;
        }

        public static void SetMiscAbilityParametersRangedDirectional(this BlueprintAbility ability, bool works_on_units = true, AbilityEffectOnUnit effect_on_ally = AbilityEffectOnUnit.Harmful, AbilityEffectOnUnit effect_on_enemy = AbilityEffectOnUnit.Harmful, Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell.CastAnimationStyle animation = Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell.CastAnimationStyle.Directional, Kingmaker.View.Animation.CastAnimationStyle animation_style = Kingmaker.View.Animation.CastAnimationStyle.CastActionDirectional)
        {
            ability.CanTargetFriends = works_on_units;
            ability.CanTargetEnemies = works_on_units;
            ability.CanTargetSelf = works_on_units;
            ability.CanTargetPoint = true;
            ability.EffectOnEnemy = effect_on_enemy;
            ability.EffectOnAlly = effect_on_ally;
            ability.Animation = animation;
            //ability.AnimationStyle = animation_style;
        }

        public static SpellComponent CreateSpellComponent(SpellSchool school)
        {
            var s = Helper.Create<SpellComponent>();
            s.School = school;
            return s;
        }

        public static AbilityAoERadius CreateAbilityAoERadius(Feet radius, TargetType target_type)
        {
            var a = Helper.Create<AbilityAoERadius>();

            Access.m_Radius(a) = radius;
            Access.m_TargetType(a) = target_type;
            return a;
        }

        public static AddStatBonus CreateAddStatBonus(StatType stat, int value, ModifierDescriptor descriptor)
        {
            var addStat = Helper.Create<AddStatBonus>();
            addStat.Stat = stat;
            addStat.Value = value;
            addStat.Descriptor = descriptor;
            return addStat;
        }

        public static UIGroup CreateUIGroup(params BlueprintFeatureBase[] features)
        {
            var result = new UIGroup();
            result.Features.AddRange(features);
            return result;
        }

        public static AbilityDeliverTouch CreateDeliverTouch()
        {
            var a = Helper.Create<AbilityDeliverTouch>();
            a.TouchWeapon = Main.library.Get<BlueprintItemWeapon>("bb337517547de1a4189518d404ec49d4");
            return a;
        }

        public static AbilityEffectStickyTouch CreateStickyTouch(BlueprintAbility deliverAbility)
        {
            var a = Helper.Create<AbilityEffectStickyTouch>();
            a.TouchDeliveryAbility = deliverAbility;
            return a;
        }

        public static void AddToSpellList(this BlueprintAbility spell, BlueprintSpellList spellList, int level)
        {
            var feyspeaker_spell_list = ResourcesLibrary.TryGetBlueprint<BlueprintSpellList>("640b4c89527334e45b19d884dd82e500");//feyspeaker
            var comp = Helper.Create<SpellListComponent>();
            comp.SpellLevel = level;
            comp.SpellList = spellList;
            spell.AddComponent(comp);
            spellList.SpellsByLevel[level].Spells.Add(spell);
            if (spellList == Main.library.Get<BlueprintSpellList>("ba0401fdeb4062f40a7aa95b6f07fe89"))
            {
                var school = spell.School;
                var specialistList = specialistSchoolList.Value[(int)school];
                specialistList?.SpellsByLevel[level].Spells.Add(spell);

                for (int i = 0; i < thassilonianSchoolList.Value.Length; i++)
                {
                    if (thassilonianOpposedSchools.Value[i] != null && !thassilonianOpposedSchools.Value[i].Contains(school))
                    {
                        thassilonianSchoolList.Value[i]?.SpellsByLevel[level].Spells.Add(spell);
                    }
                }

                if (school == SpellSchool.Enchantment || school == SpellSchool.Illusion)
                {
                    feyspeaker_spell_list.SpellsByLevel[level].Spells.Add(spell);
                }
            }
        }

        static readonly Lazy<BlueprintSpellList[]> specialistSchoolList = new Lazy<BlueprintSpellList[]>(() =>
        {
            var result = new BlueprintSpellList[(int)SpellSchool.Universalist + 1];
            var library = Main.library;
            result[(int)SpellSchool.Abjuration] = library.Get<BlueprintSpellList>("c7a55e475659a944f9229d89c4dc3a8e");
            result[(int)SpellSchool.Conjuration] = library.Get<BlueprintSpellList>("69a6eba12bc77ea4191f573d63c9df12");
            result[(int)SpellSchool.Divination] = library.Get<BlueprintSpellList>("d234e68b3d34d124a9a2550fdc3de9eb");
            result[(int)SpellSchool.Enchantment] = library.Get<BlueprintSpellList>("c72836bb669f0c04680c01d88d49bb0c");
            result[(int)SpellSchool.Evocation] = library.Get<BlueprintSpellList>("79e731172a2dc1f4d92ba229c6216502");
            result[(int)SpellSchool.Illusion] = library.Get<BlueprintSpellList>("d74e55204daa9b14993b2e51ae861501");
            result[(int)SpellSchool.Necromancy] = library.Get<BlueprintSpellList>("5fe3acb6f439db9438db7d396f02c75c");
            result[(int)SpellSchool.Transmutation] = library.Get<BlueprintSpellList>("becbcfeca9624b6469319209c2a6b7f1");
            return result;
        });

        static readonly Lazy<BlueprintSpellList[]> thassilonianSchoolList = new Lazy<BlueprintSpellList[]>(() =>
        {
            var result = new BlueprintSpellList[(int)SpellSchool.Universalist + 1];
            var library = Main.library;
            result[(int)SpellSchool.Abjuration] = library.Get<BlueprintSpellList>("280dd5167ccafe449a33fbe93c7a875e");
            result[(int)SpellSchool.Conjuration] = library.Get<BlueprintSpellList>("5b154578f228c174bac546b6c29886ce");
            result[(int)SpellSchool.Enchantment] = library.Get<BlueprintSpellList>("ac551db78c1baa34eb8edca088be13cb");
            result[(int)SpellSchool.Evocation] = library.Get<BlueprintSpellList>("17c0bfe5b7c8ac3449da655cdcaed4e7");
            result[(int)SpellSchool.Illusion] = library.Get<BlueprintSpellList>("c311aed33deb7a346ab715baef4a0572");
            result[(int)SpellSchool.Necromancy] = library.Get<BlueprintSpellList>("5c08349132cb6b04181797f58ccf38ae");
            result[(int)SpellSchool.Transmutation] = library.Get<BlueprintSpellList>("f3a8f76b1d030a64084355ba3eea369a");
            return result;
        });

        static readonly Lazy<SpellSchool[][]> thassilonianOpposedSchools = new Lazy<SpellSchool[][]>(() =>
        {
            var result = new SpellSchool[(int)SpellSchool.Universalist + 1][];

            result[(int)SpellSchool.Abjuration] = new SpellSchool[] { SpellSchool.Evocation, SpellSchool.Necromancy };
            result[(int)SpellSchool.Conjuration] = new SpellSchool[] { SpellSchool.Evocation, SpellSchool.Illusion };
            result[(int)SpellSchool.Enchantment] = new SpellSchool[] { SpellSchool.Necromancy, SpellSchool.Transmutation };
            result[(int)SpellSchool.Evocation] = new SpellSchool[] { SpellSchool.Abjuration, SpellSchool.Conjuration };
            result[(int)SpellSchool.Illusion] = new SpellSchool[] { SpellSchool.Conjuration, SpellSchool.Transmutation };
            result[(int)SpellSchool.Necromancy] = new SpellSchool[] { SpellSchool.Abjuration, SpellSchool.Enchantment };
            result[(int)SpellSchool.Transmutation] = new SpellSchool[] { SpellSchool.Enchantment, SpellSchool.Illusion };
            return result;
        });

        public static PrerequisiteArchetypeLevel CreatePrerequisiteArchetypeLevel(BlueprintCharacterClass character_class, BlueprintArchetype archetype, int level, bool any = false)
        {
            var p = Helper.Create<PrerequisiteArchetypeLevel>();
            p.CharacterClass = character_class;
            p.Archetype = archetype;
            p.Level = level;
            p.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return p;
        }

        public static AddKnownSpell CreateAddKnownSpell(this BlueprintAbility spell, BlueprintCharacterClass @class, int level, BlueprintArchetype archetype = null)
        {
            var addSpell = Helper.Create<AddKnownSpell>();
            addSpell.Spell = spell;
            addSpell.SpellLevel = level;
            addSpell.CharacterClass = @class;
            return addSpell;
        }

        public static BlueprintAbility CreateTouchSpellCast(this BlueprintAbility spell, BlueprintAbilityResource resource = null)
        {
            var castSpell = Main.library.CopyAndAdd(spell, $"{spell.name}Cast",
                MergeIds(spell.AssetGuid, "8de5133f37ff4cab8286f16c826651c1"));

            var components = new List<BlueprintComponent>();
            components.Add(CreateStickyTouch(spell));

            var schoolComponent = spell.GetComponent<SpellComponent>();
            if (schoolComponent != null) components.Add(schoolComponent);

            var descriptorComponent = spell.GetComponent<SpellDescriptorComponent>();
            if (descriptorComponent != null) components.Add(descriptorComponent);

            if (resource != null) components.Add(resource.CreateResourceLogic());

            if (spell.GetComponent<AbilityResourceLogic>() != null)
            {
                Main.DebugLogAlways($"Warning: resource logic should be passed to CreateTouchSpellCast instead of a component: {spell.name}");
            }

            castSpell.SetComponents(components);
            return castSpell;
        }

        public static AbilityResourceLogic CreateResourceLogic(this BlueprintAbilityResource resource, bool spend = true, int amount = 1, bool cost_is_custom = false)
        {
            var a = Helper.Create<AbilityResourceLogic>();
            a.IsSpendResource = spend;
            a.RequiredResource = resource;
            a.Amount = amount;
            a.CostIsCustom = cost_is_custom;
            return a;
        }

        public static AddInitiatorAttackWithWeaponTrigger CreateAddInitiatorAttackWithWeaponTriggerWithCategory(Kingmaker.ElementsSystem.ActionList action, bool only_hit = true, bool critical_hit = false, bool check_weapon_range_type = false, bool reduce_hp_to_zero = false, bool on_initiator = false, AttackTypeAttackBonus.WeaponRangeType range_type = AttackTypeAttackBonus.WeaponRangeType.Melee, bool wait_for_attack_to_resolve = false, bool only_first_hit = false, WeaponCategory weapon_category = WeaponCategory.UnarmedStrike)
        {
            var t = Helper.Create<AddInitiatorAttackWithWeaponTrigger>();
            t.Action = action;
            t.OnlyHit = only_hit;
            t.CriticalHit = critical_hit;
            t.CheckWeaponRangeType = check_weapon_range_type;
            t.RangeType = range_type;
            t.ReduceHPToZero = reduce_hp_to_zero;
            t.ActionsOnInitiator = on_initiator;
            t.WaitForAttackResolve = wait_for_attack_to_resolve;
            t.OnlyOnFirstAttack = only_first_hit;
            t.CheckWeaponCategory = true;
            t.Category = weapon_category;
            return t;
        }





    }

    public class Helper
    {
        public static T Create<T>(Action<T> action = null) where T : ScriptableObject
        {
            var result = ScriptableObject.CreateInstance<T>();
            if (action != null)
            {
                action(result);
            }
            return result;
        }

        public static T Instantiate<T>(T obj, Action<T> action = null) where T : ScriptableObject
        {
            var result = ScriptableObject.Instantiate<T>(obj);
            if (action != null)
            {
                action(result);
            }
            return result;
        }

        public static T CreateCopy<T>(T original, Action<T> action = null) where T : UnityEngine.Object
        {
            var clone = UnityEngine.Object.Instantiate(original);
            if (action != null)
            {
                action(clone);
            }
            return clone;
        }

        public static T[] ToArray<T>(params T[] objs)
        {
            return objs;
        }

        /// <summary>Appends objects on array.</summary>
        public static T[] Append<T>(T[] orig, params T[] objs)
        {
            if (orig == null) orig = new T[0];

            int i, j;
            T[] result = new T[orig.Length + objs.Length];
            for (i = 0; i < orig.Length; i++)
                result[i] = orig[i];
            for (j = 0; i < result.Length; i++)
                result[i] = objs[j++];
            return result;
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
                    conditional = Instantiate(conditional);
                    conditional.IfTrue.Actions = RecursiveReplace(conditional.IfTrue.Actions, lambda);
                    conditional.IfFalse.Actions = RecursiveReplace(conditional.IfFalse.Actions, lambda);
                    result.Add(conditional);
                }
                else if (repl)
                {
                    result.Add(Instantiate(repl, lambda));
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
            var result = Helper.Create<AddKineticistBurnValueChangedTrigger>();
            result.Action = new ActionList() { Actions = actions };
            return result;
        }

        public static PrerequisiteFeaturesFromList CreatePrerequisiteFeaturesFromList(bool any, int amount, params BlueprintFeature[] features)
        {
            if (features == null || features[0] == null) throw new ArgumentNullException();
            var result = Helper.Create<PrerequisiteFeaturesFromList>();
            result.Features = features;
            result.Amount = amount;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }

        public static AbilityEffectRunAction CreateAbilityEffectRunAction(SavingThrowType save = SavingThrowType.Unknown, params GameAction[] actions)
        {
            if (actions == null || actions[0] == null) throw new ArgumentNullException();
            var result = Helper.Create<AbilityEffectRunAction>();
            result.SavingThrowType = save;
            result.Actions = new ActionList() { Actions = actions };
            return result;
        }

        public static ContextActionApplyBuff CreateActionApplyBuff(BlueprintBuff buff, int duration = 0, DurationRate rate = DurationRate.Rounds, bool dispellable = false, bool permanent = false)
        {
            return HelperEA.CreateApplyBuff(buff, HelperEA.CreateContextDuration(bonus: new ContextValue() { Value = duration }, rate: rate), fromSpell: false, dispellable: dispellable, permanent: permanent);
        }

        public static BuffSubstitutionOnApply CreateBuffSubstitutionOnApply(BlueprintBuff GainedFact, BlueprintBuff SubstituteBuff)
        {
            var result = Helper.Create<BuffSubstitutionOnApply>();
            result.GainedFact = GainedFact;
            result.SubstituteBuff = SubstituteBuff;
            return result;
        }

        public static SpecificBuffImmunity CreateSpecificBuffImmunity(BlueprintBuff buff)
        {
            var result = Helper.Create<SpecificBuffImmunity>();
            result.Buff = buff;
            return result;
        }

        public static ContextActionRemoveBuff CreateActionRemoveBuff(BlueprintBuff buff, bool toCaster = false)
        {
            var result = Helper.Create<ContextActionRemoveBuff>();
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
                var buff = Helper.Create<ContextConditionHasBuff>();
                buff.Buff = buffs[i];
                buff.Not = true;
                result[i] = buff;
            }

            return result;
        }

        public static AbilityRequirementActionAvailable CreateRequirementActionAvailable(bool Not, ActionType Action)
        {
            var result = Helper.Create<AbilityRequirementActionAvailable>();
            result.Not = Not;
            result.Action = Action;
            return result;
        }

        public static AbilityRequirementHasBuffs CreateAbilityRequirementHasBuffs(bool Not, params BlueprintBuff[] Buffs)
        {
            var result = Helper.Create<AbilityRequirementHasBuffs>();
            result.Not = Not;
            result.Buffs = Buffs;
            return result;
        }

        public static AbilityRequirementHasBuffTimed CreateAbilidtyRequirementHasBuffTimed(CompareType Compare, TimeSpan TimeLeft, params BlueprintBuff[] Buffs)
        {
            var result = Helper.Create<AbilityRequirementHasBuffTimed>();
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
            var result = Helper.Create<AddContextStatBonusMinMax>();
            result.Multiplier = multiplier;
            result.Value = value;
            result.Stat = stat;
            result.Descriptor = descriptor;
            result.MinValue = 0;
            return result;
        }

        public static HasFact CreateHasFact(BlueprintUnitFact fact, UnitEvaluator unit = null)
        {
            var result = Helper.Create<HasFact>();
            result.Fact = fact;
            result.Unit = unit;
            return result;
        }

        public static AbilitySpawnFx CreateAbilitySpawnFx(string AssetId, AbilitySpawnFxTime spawnTime, AbilitySpawnFxAnchor position, AbilitySpawnFxAnchor orientation)
        {
            var spawnFx = Helper.Create<AbilitySpawnFx>();
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
            var result = Helper.Create<ContextActionSpawnMonster>();
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
            var result = Helper.Create<ContextActionSpawnMonsterUnique>();
            result.Blueprint = unit;
            result.SummonPool = pool;
            result.DurationValue = duration ?? Contexts.DurationRankInRounds;
            result.CountValue = amount ?? Contexts.DiceOne;
            result.AfterSpawn = Contexts.AfterSpawnAction;
            return result;
        }

        public static ContextActionSpawnMonsterLeveled CreateContextActionSpawnMonsterLeveled(int[] LevelThreshold, BlueprintUnit[] BlueprintPool)
        {
            var result = Helper.Create<ContextActionSpawnMonsterLeveled>();
            result.LevelThreshold = LevelThreshold;
            result.BlueprintPool = BlueprintPool;
            return result;
        }

        public static ContextActionToggleActivatable CreateContextActionToggleActivatable(bool TurnOn, BlueprintActivatableAbility Activatable, params GameAction[] OnFailure)
        {
            var result = Helper.Create<ContextActionToggleActivatable>();
            result.TurnOn = TurnOn;
            result.Activatable = Activatable;
            result.OnFailure = CreateActionList(OnFailure);
            return result;
        }

        public static ContextActionKillSummons CreateContextActionKillSummons(BlueprintSummonPool SummonPool, params BlueprintBuff[] Buffs)
        {
            var result = Helper.Create<ContextActionKillSummons>();
            result.SummonPool = SummonPool;
            result.Buffs = Buffs;
            return result;
        }

        public static AbilityShowIfCasterHasAnyFacts CreateAbilityShowIfCasterHasAnyFacts(params BlueprintUnitFact[] facts)
        {
            var result = Helper.Create<AbilityShowIfCasterHasAnyFacts>();
            result.UnitFacts = facts;
            return result;
        }

        public static AbilityCasterHasNoFacts CreateAbilityCasterHasNoFacts(params BlueprintUnitFact[] facts)
        {
            var result = Helper.Create<AbilityCasterHasNoFacts>();
            result.Facts = facts;
            return result;
        }

        public static ContextActionCastSpell CreateContextActionCastSpell(BlueprintAbility spell, ContextValue dc = null, ContextValue spellLevel = null)
        {
            var result = Helper.Create<ContextActionCastSpell>();
            result.Spell = spell;
            result.OverrideDC = dc != null;
            result.OverrideSpellLevel = spellLevel != null;
            result.DC = dc;
            result.SpellLevel = spellLevel;
            return result;
        }

        public static AddSpellImmunity CreateAddSpellImmunity(int immunityType, SpellDescriptor descriptor, params BlueprintAbility[] exceptions)
        {
            var result = Helper.Create<AddSpellImmunity>();
            result.SpellDescriptor = new SpellDescriptorWrapper(descriptor);
            result.Type = (SpellImmunityType)immunityType;
            if (exceptions != null) result.Exceptions = exceptions;
            return result;
        }

        public static AddKineticistBurnModifier CreateAddKineticistBurnModifier(int Value, KineticistBurnType BurnType = KineticistBurnType.Infusion, ContextValue BurnValue = null, params BlueprintAbility[] AppliableTo)
        {
            var result = Helper.Create<AddKineticistBurnModifier>();
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
            var result = Helper.Create<ContextActionRemoveSelf>();
            return result;
        }

        public static BuffMovementSpeed CreateBuffMovementSpeed(int Value, ModifierDescriptor Descriptor = ModifierDescriptor.None, int MinimumCap = 0, float MultiplierCap = 0f)
        {
            var result = Helper.Create<BuffMovementSpeed>();
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
            var result = Helper.Create<CriticalConfirmationWeaponType>();
            result.Value = Value;
            result.Type = Type;
            return result;
        }

        public static AddContextStatBonus CreateAddContextStatBonus(StatType Stat, ContextValue Value, ModifierDescriptor Descriptor = ModifierDescriptor.UntypedStackable)
        {
            var result = Helper.Create<AddContextStatBonus>();
            result.Stat = Stat;
            result.Value = Value;
            result.Descriptor = Descriptor;
            return result;
        }

        ///<summary>SubFeature is the second or third feat of the style chain.</summary>
        public static AddFactContextActions CombatStyleHelper(BlueprintFeature SubFeature, BlueprintBuff Buff)
        {
            var applyBuff = Helper.Create<ContextActionApplyBuff>();
            applyBuff.Buff = Buff;
            applyBuff.DurationValue = Contexts.DurationZero;
            applyBuff.IsFromSpell = false;
            applyBuff.IsNotDispelable = false;
            applyBuff.Permanent = true;

            var has = Helper.Create<ContextConditionHasFact>();
            has.Fact = SubFeature;

            var c = Helper.Create<Conditional>();
            c.ConditionsChecker = new ConditionsChecker() { Conditions = new Condition[] { has }, Operation = Operation.And };
            c.IfTrue = CreateActionList(applyBuff);
            c.IfFalse = CreateActionList();

            var result = Helper.Create<AddFactContextActions>();
            result.Activated = CreateActionList(c);
            result.Deactivated = CreateActionList();
            result.NewRound = CreateActionList();

            return result;
        }

        public static ContextActionRestoreResource CreateContextActionRestoreResource(BlueprintAbilityResource Resource, ContextValue Amount, bool ToCaster = false)
        {
            var result = Helper.Create<ContextActionRestoreResource>();
            result.Resource = Resource;
            result.Amount = Amount;
            result.ToCaster = ToCaster;
            return result;
        }

        public static RendSpecial CreateRendSpecial(DiceFormula RendDamage, DamageTypeDescription RendType = null, WeaponCategory? Category = null, bool TargetSelf = false, params GameAction[] Actions)
        {
            var result = Helper.Create<RendSpecial>();
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

        public static AddOutgoingPhysicalDamageProperty CreateAddOutgoingPhysicalDamageProperty(BlueprintWeaponType WeaponType = null, bool CheckRange = false, bool IsRanged = false, bool AddMagic = false, PhysicalDamageMaterial? Material = null, PhysicalDamageForm? Form = null, DamageAlignment? Alignment = null, bool MyAlignment = false, DamageRealityType? Reality = null)
        {
            var result = Helper.Create<AddOutgoingPhysicalDamageProperty>();
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
            var result = Helper.Create<ContextActionCombatManeuver>();
            result.Type = Type;
            result.OnSuccess = CreateActionList(OnSuccess);
            return result;
        }

        public static BlueprintAbilityAreaEffect CreateBlueprintAbilityAreaEffect(string name, string guid)
        {
            var result = Create<BlueprintAbilityAreaEffect>();
            result.name = name;
            result.AssetGuid = guid;

            HelperEA.AddAsset(Main.library, result, guid);
            return result;
        }

        public static ActionList CreateActionList(params GameAction[] actions)
        {
            if (actions == null || actions.Length == 1 && actions[0] == null) actions = Array.Empty<GameAction>();
            return new ActionList() { Actions = actions };
        }

        public static int UnitHasClassLevels(UnitEntityData unit, BlueprintCharacterClass[] classes, BlueprintArchetype[] archetypes = null)
        {
            if (unit == null || classes == null)
            {
                Main.DebugLogAlways("ERROR: UnitHasClassLevels unit or classes is null.");
                return 0;
            }

            int class_level = 0;
            foreach (var c in classes)
            {
                var class_archetypes = archetypes?.Where(a => a.GetParentClass() == c);

                if (class_archetypes == null || class_archetypes.Empty() || class_archetypes.Any(a => unit.Descriptor.Progression.IsArchetype(a)))
                {
                    class_level += unit.Descriptor.Progression.GetClassLevel(c);
                }

            }
            return class_level;
        }

        public static WeaponEnhancementScaling CreateWeaponEnhancementScaling(BlueprintCharacterClass[] Class, BlueprintArchetype[] Archetype, int StartingLevel, int StartValue, int LevelStep, int PerStepIncrease)
        {
            var result = Create<WeaponEnhancementScaling>();
            result.Class = Class ?? new BlueprintCharacterClass[0];
            result.Archetype = Archetype ?? new BlueprintArchetype[0];
            result.StartingLevel = StartingLevel;
            result.StartValue = StartValue;
            result.LevelStep = LevelStep;
            result.PerStepIncrease = PerStepIncrease;
            return result;
        }

        public static BlueprintWeaponEnchantment CreateBlueprintWeaponEnchantment(string name, string guid, string displayName, string Desc, int Cost, params BlueprintComponent[] components)
        {
            var result = Create<BlueprintWeaponEnchantment>();
            result.name = name;
            Access.m_EnchantNameStr(result, displayName);
            Access.m_DescriptionStr(result, Desc);
            Access.m_EnchantmentCost(result) = Cost;

            if (components != null)
                result.SetComponents(components);

            Main.library.AddAsset(result, guid);
            return result;
        }

        public static ContextActionTryCastSpell CreateContextActionTryCastSpell(BlueprintAbility Spell, GameAction[] Succeed = null, GameAction[] Failed = null)
        {
            var result = Create<ContextActionTryCastSpell>();

            result.Spell = Spell;
            result.Succeed = CreateActionList(Succeed);
            result.Failed = CreateActionList(Failed);

            return result;
        }

        public static AbilityRestoreSpellSlot CreateAbilityRestoreSpellSlot(int SpellLevel = 0)
        {
            var result = Create<AbilityRestoreSpellSlot>();

            result.AnySpellLevel = SpellLevel <= 0;
            result.SpellLevel = SpellLevel;

            return result;
        }

        public static AbilityRestoreSpontaneousSpell CreateAbilityRestoreSpontaneousSpell(int SpellLevel = 9)
        {
            var result = Create<AbilityRestoreSpontaneousSpell>();

            result.SpellLevel = SpellLevel;

            return result;
        }

        public static LootItemsPackFixed CreateLootItemsPackFixed(BlueprintItem item, int amount = 1)
        {
            var result_item = new LootItem();
            Access.LootItem_Item(result_item) = item;

            var result = Create<LootItemsPackFixed>();
            Access.LootItemsPackFixed_Item(result) = result_item;
            Access.LootItemsPackFixed_Count(result) = amount;

            return result;
        }

        public static BlueprintAbilityResource CreateBlueprintAbilityResource()
        {
            var result = Create<BlueprintAbilityResource>();
            return result;
        }

        public static BlueprintAbilityResource Set_BlueprintAbilityResource_MaxAmount(BlueprintAbilityResource blueprintAbilityResource,
            int? BaseValue = null, bool? IncreasedByLevel = null, BlueprintCharacterClass[] Class = null,
            BlueprintArchetype[] Archetypes = null, int? LevelIncrease = null,
            bool? IncreasedByLevelStartPlusDivStep = null, int? StartingLevel = null, int? StartingIncrease = null,
            int? LevelStep = null, int? PerStepIncrease = null, int? MinClassLevelIncrease = null,
            BlueprintCharacterClass[] ClassDiv = null, BlueprintArchetype[] ArchetypesDiv = null,
            float? OtherClassesModifier = null, bool? IncreasedByStat = null, StatType? ResourceBonusStat = null)
        {
            var fi_amount = AccessTools.Field(typeof(BlueprintAbilityResource), "m_MaxAmount");
            object amount = fi_amount.GetValue(blueprintAbilityResource);

            foreach (var field in AccessTools.GetDeclaredFields(fi_amount.FieldType))
            {
                switch (field.Name)
                {
                    case nameof(BaseValue):
                        if (BaseValue != null)
                            field.SetValue(amount, BaseValue.Value);
                        break;
                    case nameof(IncreasedByLevel):
                        if (IncreasedByLevel != null)
                            field.SetValue(amount, IncreasedByLevel.Value);
                        break;
                    case nameof(Class):
                        if (Class != null)
                            field.SetValue(amount, Class);
                        break;
                    case nameof(Archetypes):
                        if (Archetypes != null)
                            field.SetValue(amount, Archetypes);
                        break;
                    case nameof(LevelIncrease):
                        if (LevelIncrease != null)
                            field.SetValue(amount, LevelIncrease.Value);
                        break;
                    case nameof(IncreasedByLevelStartPlusDivStep):
                        if (IncreasedByLevelStartPlusDivStep != null)
                            field.SetValue(amount, IncreasedByLevelStartPlusDivStep.Value);
                        break;
                    case nameof(StartingLevel):
                        if (StartingLevel != null)
                            field.SetValue(amount, StartingLevel.Value);
                        break;
                    case nameof(StartingIncrease):
                        if (StartingIncrease != null)
                            field.SetValue(amount, StartingIncrease.Value);
                        break;
                    case nameof(LevelStep):
                        if (LevelStep != null)
                            field.SetValue(amount, LevelStep.Value);
                        break;
                    case nameof(PerStepIncrease):
                        if (PerStepIncrease != null)
                            field.SetValue(amount, PerStepIncrease.Value);
                        break;
                    case nameof(MinClassLevelIncrease):
                        if (MinClassLevelIncrease != null)
                            field.SetValue(amount, MinClassLevelIncrease.Value);
                        break;
                    case nameof(ClassDiv):
                        if (ClassDiv != null)
                            field.SetValue(amount, ClassDiv);
                        break;
                    case nameof(ArchetypesDiv):
                        if (ArchetypesDiv != null)
                            field.SetValue(amount, ArchetypesDiv);
                        break;
                    case nameof(OtherClassesModifier):
                        if (OtherClassesModifier != null)
                            field.SetValue(amount, OtherClassesModifier.Value);
                        break;
                    case nameof(IncreasedByStat):
                        if (IncreasedByStat != null)
                            field.SetValue(amount, IncreasedByStat.Value);
                        break;
                    case nameof(ResourceBonusStat):
                        if (ResourceBonusStat != null)
                            field.SetValue(amount, ResourceBonusStat.Value);
                        break;
                    default:
                        Main.DebugLogAlways("Error: Unkown field in m_MaxAmount" + field.Name);
                        break;
                }
            }
            
            return blueprintAbilityResource;
        }

        public static AddAbilityResources CreateAddAbilityResources(BlueprintAbilityResource Resource, bool RestoreAmount = true, bool RestoreOnLevelUp = false)
        {
            var result = Create<AddAbilityResources>();
            result.RestoreAmount = RestoreAmount;
            result.RestoreOnLevelUp = RestoreOnLevelUp;
            return result;
        }

        public static IncreaseResourceCustom CreateIncreaseResourceCustom(BlueprintAbilityResource Resource, BlueprintCharacterClass[] Classes, BlueprintArchetype[] Archetypes = null, bool Invert = false, params int[] Bonus)
        {
            var result = Create<IncreaseResourceCustom>();
            result.Resource = Resource;
            result.Classes = Classes ?? Array.Empty<BlueprintCharacterClass>();
            result.Archetypes = Archetypes ?? Array.Empty<BlueprintArchetype>();
            result.Invert = Invert;
            result.Bonus = Bonus ?? Array.Empty<int>();
            return result;
        }

        public static PrerequisiteExactClassLevel CreatePrerequisiteExactClassLevel(BlueprintCharacterClass CharacterClass, int Level)
        {
            var result = Create<PrerequisiteExactClassLevel>();
            result.CharacterClass = CharacterClass;
            result.Level = Level;
            return result;
        }

        public static class Image2Sprite
        {
            public static Sprite Create(string filename, int width = 64, int height = 64)
            {
                try
                {
                    var bytes = File.ReadAllBytes(Path.Combine(Main.ModPath, "Icons", filename));
                    var texture = new Texture2D(width, height);
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

    public class ExtraSpellList
    {
        public struct SpellId
        {
            public readonly string guid;
            public readonly int level;
            public SpellId(string spell_guid, int spell_level)
            {
                guid = spell_guid;
                level = spell_level;
            }

            public BlueprintAbility getSpell()
            {
                return Main.library.Get<BlueprintAbility>(guid);
            }
        }

        SpellId[] spells;

        public ExtraSpellList(params SpellId[] list_spells)
        {
            spells = list_spells;
        }



        public ExtraSpellList(params string[] list_spell_guids)
        {
            spells = new SpellId[list_spell_guids.Length];
            for (int i = 0; i < list_spell_guids.Length; i++)
            {
                spells[i] = new SpellId(list_spell_guids[i], i + 1);
            }
        }


        public ExtraSpellList(params BlueprintAbility[] spells_array)
        {
            spells = new SpellId[spells_array.Length];
            for (int i = 0; i < spells_array.Length; i++)
            {
                spells[i] = new SpellId(spells_array[i].AssetGuid, i + 1);
            }
        }


        public Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList createSpellList(string name, string guid)
        {
            var spell_list = Helper.Create<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList>();
            spell_list.name = name;
            Main.library.AddAsset(spell_list, guid);
            spell_list.SpellsByLevel = new SpellLevelList[10];
            for (int i = 0; i < spell_list.SpellsByLevel.Length; i++)
            {
                spell_list.SpellsByLevel[i] = new SpellLevelList(i);
            }
            foreach (var s in spells)
            {
                if (!s.guid.Empty())
                {
                    var spell = Main.library.Get<BlueprintAbility>(s.guid);
                    spell.AddToSpellList(spell_list, s.level);
                }
            }
            return spell_list;
        }


        public Kingmaker.UnitLogic.FactLogic.LearnSpellList createLearnSpellList(string name, string guid, BlueprintCharacterClass character_class, BlueprintArchetype archetype = null)
        {
            Kingmaker.UnitLogic.FactLogic.LearnSpellList learn_spell_list = Helper.Create<Kingmaker.UnitLogic.FactLogic.LearnSpellList>();
            learn_spell_list.Archetype = archetype;
            learn_spell_list.CharacterClass = character_class;
            learn_spell_list.SpellList = createSpellList(name, guid);
            return learn_spell_list;
        }


        public LevelEntry[] createLearnSpellLevelEntries(string name, string description, string guid,
                                                         int[] levels,
                                                         BlueprintCharacterClass character_class, BlueprintArchetype archetype = null)
        {
            LevelEntry[] entires = new LevelEntry[levels.Length];

            for (int i = 0; i < entires.Length; i++)
            {
                var s = spells[i].getSpell();
                var feature = HelperEA.CreateFeature(name + s.name,
                                                    s.Name,
                                                    description + "\n" + s.Name + ": " + s.Description,
                                                    HelperEA.MergeIds(guid, s.AssetGuid),
                                                    s.Icon,
                                                    FeatureGroup.None,
                                                    HelperEA.CreateAddKnownSpell(s, character_class, i + 1, archetype)
                                                    );
                entires[i] = HelperEA.LevelEntry(levels[i], feature);
            }

            return entires;
        }

    }


}
