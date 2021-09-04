using UnityEngine;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.EntitySystem.Stats;
using System.Linq;
using Kingmaker.Utility;
using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Experience;
using Kingmaker.EntitySystem.Entities;
using Kingmaker;
using System.Reflection;
using CallOfTheWild;

namespace FumisCodex.NewComponents
{
    public static class ContextRankConfigExt
    {
        ///<summery>classes: adds to existing classes
        ///archetypes: overwrites any existing archetypes</summary>
        public static ContextRankConfig2 Convert(this ContextRankConfig org,
            BlueprintCharacterClass[] classes,
            BlueprintArchetype[] archetypes)
        {
            return ContextRankConfig2.Convert(org, classes, archetypes);
        }
    }

    ///<summery>Allows to define multiple archetypes.</summary>
    public class ContextRankConfig2 : ContextRankConfig
    {
        public BlueprintArchetype[] Archetypes;

        public static ContextRankConfig2 Convert(ContextRankConfig org,
            BlueprintCharacterClass[] classes,
            BlueprintArchetype[] archetypes)
        {
            var result = ScriptableObject.CreateInstance<ContextRankConfig2>();
            
            var fields_org = typeof(ContextRankConfig).GetFields(BindingFlags.NonPublic);
            var fields_target = typeof(ContextRankConfig2).GetFields(BindingFlags.NonPublic);

            foreach (var field in fields_org)
            {
                if (field.Name == "m_Class")
                    fields_target.FirstOrDefault(a => a.Name == field.Name)?.SetValue(result,
                        (field.GetValue(org) as BlueprintCharacterClass[])?.AddToArray(classes) ?? classes);
                else
                    fields_target.FirstOrDefault(a => a.Name == field.Name)?.SetValue(result,
                        field.GetValue(org));
            }

            result.Archetypes = archetypes;
            return result;
        }

        [HarmonyLib.HarmonyPatch(typeof(ContextRankConfig), "GetBaseValue")]
        public static class GetBaseValue_Patch
        {
            static bool Prefix(
                MechanicsContext context,
                ref int __result,
                ContextRankConfig __instance,
                ContextRankBaseValueType ___m_BaseValueType,
                bool ___m_ExceptClasses,
                BlueprintCharacterClass[] ___m_Class,
                BlueprintFeature ___m_Feature,
                BlueprintFeature[] ___m_FeatureList,
                StatType ___m_Stat,
                BlueprintUnitProperty ___m_CustomProperty)  //BlueprintArchetype ___Archetype
            {
                var _this = __instance as ContextRankConfig2;
                if (_this != null)
                {
                    __result = OriginalModded(context, __instance, ___m_BaseValueType, ___m_ExceptClasses, ___m_Class, ___m_Feature, ___m_FeatureList, ___m_Stat, ___m_CustomProperty,
                        _this.Archetypes);
                    return false;
                }
                return true;
            }

            public static bool Check(
                ClassData classdata,
                bool m_ExceptClasses,
                BlueprintCharacterClass[] m_Class,
                BlueprintArchetype[] Archetypes) // parent must be in m_Class also
            {
                if ((m_ExceptClasses && !m_Class.HasItem(classdata.CharacterClass) || 
                        !m_ExceptClasses && m_Class.HasItem(classdata.CharacterClass)) 
                    && (Archetypes == null ||
                        !classdata.CharacterClass.Archetypes.Intersect(Archetypes).Any() || 
                        classdata.Archetypes.Intersect(Archetypes).Any()))
                {
                    return true;
                }
                return false;
            }

            public static int OriginalModded(MechanicsContext context, ContextRankConfig __instance,
                ContextRankBaseValueType m_BaseValueType,
                bool m_ExceptClasses,
                BlueprintCharacterClass[] m_Class,
                BlueprintFeature m_Feature,
                BlueprintFeature[] m_FeatureList,
                StatType m_Stat,
                BlueprintUnitProperty m_CustomProperty,
                BlueprintArchetype[] Archetype)
            {
                if (context.MaybeCaster == null)
                {
                    Main.DebugLogAlways("Caster is missing");
                    return 0;
                }
                switch (m_BaseValueType)
                {
                case ContextRankBaseValueType.CasterLevel:
                    return context.Params.CasterLevel;
                case ContextRankBaseValueType.ClassLevel:
                {
                    int num = context.Params.RankBonus;
                    foreach (ClassData classData in context.MaybeCaster.Descriptor.Progression.Classes)
                    {
                        if (Check(classData, m_ExceptClasses, m_Class, Archetype))
                        {
                            num += classData.Level;
                        }
                    }
                    return num;
                }
                case ContextRankBaseValueType.FeatureRank:
                    return context.MaybeCaster.Descriptor.Progression.Features.GetRank(m_Feature);
                case ContextRankBaseValueType.StatBonus:
                {
                    ModifiableValueAttributeStat stat = context.MaybeCaster.Descriptor.Stats.GetStat<ModifiableValueAttributeStat>(m_Stat);
                    int? num2 = (stat != null) ? new int?(stat.Bonus) : null;
                    return (num2 == null) ? 0 : num2.Value;
                }
                case ContextRankBaseValueType.BaseAttack:
                    return context.MaybeCaster.Descriptor.Stats.BaseAttackBonus;
                case ContextRankBaseValueType.CharacterLevel:
                    return context.MaybeCaster.Descriptor.Progression.CharacterLevel;
                case ContextRankBaseValueType.FeatureList:
                {
                    int num3 = 0;
                    foreach (BlueprintFeature blueprint in m_FeatureList)
                    {
                        if (context.MaybeCaster.Descriptor.Progression.Features.HasFact(blueprint))
                        {
                            num3++;
                        }
                    }
                    return num3;
                }
                case ContextRankBaseValueType.FeatureListRanks:
                {
                    int num4 = 0;
                    foreach (BlueprintFeature feature in m_FeatureList)
                    {
                        num4 += context.MaybeCaster.Descriptor.Progression.Features.GetRank(feature);
                    }
                    return num4;
                }
                case ContextRankBaseValueType.MaxCasterLevel:
                {
                    int num5 = context.Params.RankBonus;
                    foreach (ClassData classData2 in context.MaybeCaster.Descriptor.Progression.Classes)
                    {
                        if (classData2.Spellbook != null)
                        {
                            num5 += Math.Max(classData2.Level + classData2.Spellbook.CasterLevelModifier, 0);
                        }
                    }
                    return num5;
                }
                case ContextRankBaseValueType.MasterFeatureRank:
                    return (context.MaybeCaster.Descriptor.Master.Value == null) ? 0 : context.MaybeCaster.Descriptor.Master.Value.Descriptor.Progression.Features.GetRank(m_Feature);
                case ContextRankBaseValueType.MaxClassLevelWithArchetype:
                {
                    int num6 = 0;
                    foreach (ClassData classData3 in context.MaybeCaster.Descriptor.Progression.Classes)
                    {
                        if (Check(classData3, m_ExceptClasses, m_Class, Archetype))
                        {
                            num6 = Math.Max(num6, classData3.Level + context.Params.RankBonus);
                        }
                    }
                    return num6;
                }
                case ContextRankBaseValueType.CasterCR:
                {
                    Experience component = context.MaybeCaster.Blueprint.GetComponent<Experience>();
                    int? num7 = (component != null) ? new int?(component.CR) : null;
                    return (num7 == null) ? 0 : num7.Value;
                }
                case ContextRankBaseValueType.SummClassLevelWithArchetype:
                {
                    int num8 = 0;
                    foreach (ClassData classData4 in context.MaybeCaster.Descriptor.Progression.Classes)
                    {
                        if (Check(classData4, m_ExceptClasses, m_Class, Archetype))
                        {
                            num8 += classData4.Level + context.Params.RankBonus;
                        }
                    }
                    return num8;
                }
                case ContextRankBaseValueType.BaseStat:
                {
                    ModifiableValue stat2 = context.MaybeCaster.Descriptor.Stats.GetStat(m_Stat);
                    int? num9 = (stat2 != null) ? new int?(stat2.BaseValue) : null;
                    return (num9 == null) ? 0 : num9.Value;
                }
                case ContextRankBaseValueType.CustomProperty:
                {
                    BlueprintUnitProperty blueprintUnitProperty = m_CustomProperty.Or(null);
                    int? num10 = (blueprintUnitProperty != null) ? new int?(blueprintUnitProperty.GetInt(context.MaybeCaster)) : null;
                    return (num10 == null) ? 0 : num10.Value;
                }
                case ContextRankBaseValueType.DungeonStage:
                    return Game.Instance.Player.DungeonState.Stage;
                case ContextRankBaseValueType.OwnerSummClassLevelWithArchetype:
                {
                    UnitEntityData maybeOwner = context.MaybeOwner;
                    if (maybeOwner == null)
                    {
                        return 0;
                    }
                    int num11 = 0;
                    foreach (ClassData classData5 in maybeOwner.Descriptor.Progression.Classes)
                    {
                        if (Check(classData5, m_ExceptClasses, m_Class, Archetype))
                        {
                            num11 += classData5.Level + context.Params.RankBonus;
                        }
                    }
                    return num11;
                }
                case ContextRankBaseValueType.Bombs:
                {
                    int num12 = 0;
                    foreach (ClassData classData6 in context.MaybeCaster.Descriptor.Progression.Classes)
                    {
                        if (Check(classData6, m_ExceptClasses, m_Class, Archetype))
                        {
                            num12 += classData6.Level + context.Params.RankBonus;
                        }
                    }
                    int rank = context.MaybeCaster.Descriptor.Progression.Features.GetRank(m_Feature);
                    return (rank <= 0) ? (1 + (num12 + 1) / 2) : (rank + num12 / 2);
                }
                default:
                    Main.DebugLogAlways(string.Format("Invalid rank base value: {0}", context.AssociatedBlueprint));
                    return 0;
                }
            }

        }
    }
}