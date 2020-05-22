using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CallOfTheWild;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.UnitLogic.Mechanics.Properties;

namespace FumisCodex
{
    // known issues: works with any weapon damage, not just slashing (maybe fix later)
    public class Rogue
    {
        static LibraryScriptableObject library => Main.library;

        public static void createFlensingStrike()
        {
            var bleed1d6 = library.Get<BlueprintBuff>("75039846c3d85d940aa96c249b97e562");
            var bleeding_attack = library.Get<BlueprintFeature>("6d0d97d4830440e1a1eee235297ff07f");

            // create flensing feat
            // - add it to combat feat selection
            var flensing_strike_feat = Helpers.CreateFeature(
                "RogueFlensingStrikeFeat",
                "Flensing Strike",
                "When you successfully inflict sneak attack damage on a foe with a slashing weapon, your attack doesn’t go particularly deep, but you do carve away a significant portion of skin and flesh. If this sneak attack inflicts bleed damage, the victim of the sneak attack is sickened by the pain and has its natural armor bonus (if any) reduced by a number of points equal to the number of sneak attack dice you possess. These penalties persist as long as the bleed damage persists. Multiple strikes on the same foe do not stack the bleed damage, but the penalty to natural armor does stack, to a maximum penalty equal to the target’s normal full natural armor score.",
                "ee1567412ec34b2498645db022921523",
                NewSpells.deadly_juggernaut.Icon,
                FeatureGroup.Feat,
                Helpers.PrerequisiteFeature(bleeding_attack),
                Helpers.PrerequisiteFullStatValue(StatType.SneakAttack, 3)
            );
            Helper.AppendAndReplace(ref flensing_strike_feat.Groups, FeatureGroup.CombatFeat);
            library.AddCombatFeats(flensing_strike_feat);

            // create flensing debuff
            // - writes sneak attack die count into rank
            // - reads rank and applies it as reduction to natural armor and enhancement to natural armor
            // - has spell descriptor bleed, which handles immunities
            // - removes status when out of combat (like other bleeds)
            // - removes status when healed
            // - applies sickened condition
            // - stacks with itself
            var flensing_debuff = Helpers.CreateBuff(
                "RogueFlensingStrikeBuff",
                flensing_strike_feat.Name,
                "Slashing sneak attacks sliced away swaths of skin and natural armor.",
                "68a02dd55fd04573b05ded373689f684",
                NewSpells.deadly_juggernaut.Icon,
                Contexts.NullPrefabLink,
                Helpers.CreateContextRankConfig(ContextRankBaseValueType.CustomProperty, customProperty: library.Get<BlueprintUnitProperty>("a9d8d3c40dab4e8e8d92112cea65dc65")),// (Harmony12.AccessTools.Field(Type.GetType("CallOfTheWild.NewMechanics.SneakAttackDiceGetter"), "Blueprint").GetValue(null) as Lazy<BlueprintUnitProperty>).Value), //  CallOfTheWild.NewMechanics.SneakAttackDiceGetter.Blueprint.Value),
                Helper.CreateAddContextStatBonusMin(Contexts.DefaultRank, -1, StatType.AC, ModifierDescriptor.NaturalArmor, ModifierDescriptor.NaturalArmorEnhancement),
                Helpers.CreateSpellDescriptor(SpellDescriptor.Bleed),
                bleed1d6.GetComponent<CombatStateTrigger>(),
                bleed1d6.GetComponent<AddHealTrigger>(),
                UnitCondition.Sickened.CreateAddCondition()
            );
            flensing_debuff.Stacking = StackingType.Stack;

            // applies debuff on bleeding attack use, if caster has flensing feat
            // - checks for feat
            // - makes conditional that requires said feat and applies debuff
            // - gets ability buff that handles BleedingAttack and appends conditional
            Condition hasFeature = Helpers.CreateConditionCasterHasFact(flensing_strike_feat);
            Conditional flensing_check = Helpers.CreateConditional(
                Helpers.CreateConditionsCheckerAnd(hasFeature),
                Common.createContextActionApplyBuff(flensing_debuff, Helpers.CreateContextDuration(), dispellable: false, is_permanent: true));
            var trigger = library.Get<BlueprintBuff>("686a99974b6a441f8ce9364d49ebf6d7").GetComponent<AddInitiatorAttackRollTrigger>();
            Helper.AppendAndReplace(ref trigger.Action.Actions, flensing_check);
        }
    }
}
