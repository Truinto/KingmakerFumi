using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Guid = FumisCodex.GuidManager;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
//using CallOfTheWild;

namespace FumisCodex
{
    // known issues: works with any weapon damage, not just slashing (maybe fix later)
    public class Rogue
    {
        private static LibraryScriptableObject library => Main.library;

        public static void createFlensingStrike()
        {
            if (!Main.COTWpresent)
                Main.LoadSafe(createBleedingAttack, true);
            
            var bleed1d6 = library.Get<BlueprintBuff>("75039846c3d85d940aa96c249b97e562");
            var bleeding_attack = library.Get<BlueprintFeature>("6d0d97d4830440e1a1eee235297ff07f");//<--
            var icon = library.Get<BlueprintFeature>("df4f34f7cac73ab40986bc33f87b1a3c").Icon;  //KnifeMasterSneakStab

            // create flensing feat
            // - add it to combat feat selection
            var flensing_strike_feat = HelperEA.CreateFeature(
                "RogueFlensingStrikeFeat",
                "Flensing Strike",
                "When you successfully inflict sneak attack damage on a foe with a slashing weapon, your attack doesn’t go particularly deep, but you do carve away a significant portion of skin and flesh. If this sneak attack inflicts bleed damage, the victim of the sneak attack is sickened by the pain and has its natural armor bonus (if any) reduced by a number of points equal to the number of sneak attack dice you possess. These penalties persist as long as the bleed damage persists. Multiple strikes on the same foe do not stack the bleed damage, but the penalty to natural armor does stack, to a maximum penalty equal to the target’s normal full natural armor score.",
                Guid.i.Reg("ee1567412ec34b2498645db022921523"),
                icon,
                FeatureGroup.Feat,
                HelperEA.PrerequisiteFeature(bleeding_attack),//<--
                HelperEA.PrerequisiteFullStatValue(StatType.SneakAttack, 3)
            );
            Helper.AppendAndReplace(ref flensing_strike_feat.Groups, FeatureGroup.CombatFeat);
            HelperEA.AddCombatFeats(library, flensing_strike_feat);

            // create flensing debuff
            // - writes sneak attack die count into rank
            // - reads rank and applies it as reduction to natural armor and enhancement to natural armor
            // - has spell descriptor bleed, which handles immunities
            // - removes status when out of combat (like other bleeds)
            // - removes status when healed
            // - applies sickened condition
            // - stacks with itself
            var flensing_debuff = HelperEA.CreateBuff(
                "RogueFlensingStrikeBuff",
                flensing_strike_feat.Name,
                "Slashing sneak attacks sliced away swaths of skin and natural armor.",
                Guid.i.Reg("68a02dd55fd04573b05ded373689f684"),
                icon,
                Contexts.NullPrefabLink,
                HelperEA.CreateContextRankConfig(ContextRankBaseValueType.CustomProperty, customProperty: library.TryGet<BlueprintUnitProperty>("a9d8d3c40dab4e8e8d92112cea65dc65") ?? SneakAttackDiceGetter.Blueprint),
                Helper.CreateAddContextStatBonusMin(Contexts.ValueRank, -1, StatType.AC, ModifierDescriptor.NaturalArmor, ModifierDescriptor.NaturalArmorEnhancement),
                HelperEA.CreateSpellDescriptor(SpellDescriptor.Bleed),
                bleed1d6.GetComponent<CombatStateTrigger>(),
                bleed1d6.GetComponent<AddHealTrigger>(),
                HelperEA.CreateAddCondition(UnitCondition.Sickened)
            );
            flensing_debuff.Stacking = StackingType.Stack;

            // applies debuff on bleeding attack use, if caster has flensing feat
            // - checks for feat
            // - makes conditional that requires said feat and applies debuff
            // - gets ability buff that handles BleedingAttack and appends conditional
            Condition hasFeature = HelperEA.CreateConditionCasterHasFact(flensing_strike_feat);
            Conditional flensing_check = HelperEA.CreateConditional(
                hasFeature,
                HelperEA.CreateContextActionApplyBuff(flensing_debuff, Contexts.DurationRankInRounds, dispellable: false, is_permanent: true));
            var trigger = library.Get<BlueprintBuff>("686a99974b6a441f8ce9364d49ebf6d7").GetComponent<AddInitiatorAttackRollTrigger>();//<--
            Helper.AppendAndReplace(ref trigger.Action.Actions, flensing_check);
        }

        internal class SneakAttackDiceGetter : PropertyValueGetter
        {
            public static readonly BlueprintUnitProperty Blueprint = Helper.Create<BlueprintUnitProperty>((p) =>
            {
                p.name = "SneakAttackDiceCustomPropertyAlt";
                Main.library.AddAsset(p, "5848965b8d014e5996668bad93d55d72");
                p.SetComponents(Helper.Create<SneakAttackDiceGetter>());
            });

            public override int GetInt(UnitEntityData unit)
            {
                return unit.Stats.SneakAttack.ModifiedValue;
            }
        }

        private static void createBleedingAttack(bool enabled = false)
        {
            var bleed1d6 = library.Get<BlueprintBuff>("75039846c3d85d940aa96c249b97e562");
            var icon = library.Get<BlueprintFeature>("df4f34f7cac73ab40986bc33f87b1a3c").Icon;  //KnifeMasterSneakStab
            var effect_buff = HelperEA.CreateBuff(
                "RogueBleedingAttackEffectBuff",
                "Bleeding Attack Effect",
                "(Note: Could not find CallOfTheWild. This is a backup solution.\n" + "A rogue with this ability can cause living opponents to bleed by hitting them with a sneak attack. This attack causes the target to take 1 additional point of damage each round for each die of the rogue’s sneak attack (e.g., 4d6 equals 4 points of bleed). Bleeding creatures take that amount of damage every round at the start of each of their turns. The bleeding can be stopped by a successful DC 15 Heal check or the application of any effect that heals hit point damage. Bleed damage from this ability does not stack with itself. Bleed damage bypasses any damage reduction the creature might possess.",
                "8209f443152648eab04de6b8fe3c2008",
                icon,
                null,
                Helper.Create<CopyOf.CallOfTheWild.BleedMechanics.BleedBuff>(b => b.dice_value = HelperEA.CreateContextDiceValue(DiceType.Zero, 0, HelperEA.CreateContextValue(AbilityRankType.Default))),
                HelperEA.CreateContextRankConfig(ContextRankBaseValueType.CustomProperty, customProperty: SneakAttackDiceGetter.Blueprint),
                HelperEA.CreateSpellDescriptor(SpellDescriptor.Bleed),
                bleed1d6.GetComponent<CombatStateTrigger>(),
                bleed1d6.GetComponent<AddHealTrigger>()
            );

            var apply_buff = HelperEA.CreateContextActionApplyBuff(effect_buff, HelperEA.CreateContextDuration(), dispellable: false, is_permanent: true);
            var buff = HelperEA.CreateBuff(
                "RogueBleedingAttackBuff",
                "Bleeding Attack",
                effect_buff.Description,
                "686a99974b6a441f8ce9364d49ebf6d7",
                icon,
                null,
                Helper.Create<AddInitiatorAttackRollTrigger>(a => 
                {
                     a.OnlyHit = true; 
                     a.SneakAttack = true;
                      a.Action = Helper.CreateActionList(apply_buff); 
                })
            );

            var toggle = HelperEA.CreateActivatableAbility(
                "RogueBleedingAttackToggleAbility",
                buff.Name,
                buff.Description,
                "325b9ad52685478f8260a7d904384c2a",
                buff.Icon,
                buff,
                AbilityActivationType.Immediately,
                UnitCommand.CommandType.Free,
                null);
            toggle.Group = ActivatableAbilityGroup.None;    // note: this is a fallback, so I don't care about the group
            toggle.DeactivateImmediately = true;

            var bleeding_attack = HelperEA.ActivatableAbilityToFeature(toggle, false, "6d0d97d4830440e1a1eee235297ff07f");

            bleeding_attack.AddComponent(HelperEA.PrerequisiteFeature(library.Get<BlueprintFeature>("9b9eac6709e1c084cb18c3a366e0ec87")));//sneak attack

            if (enabled)
                AddToTalentSelection(bleeding_attack);
        }

        public static void AddToTalentSelection(BlueprintFeature feature)
        {
            Helper.AppendAndReplace(ref library.Get<BlueprintFeatureSelection>("c074a5d615200494b8f2a9c845799d93").AllFeatures, feature);   //rogue talent
            Helper.AppendAndReplace(ref library.Get<BlueprintFeatureSelection>("04430ad24988baa4daa0bcd4f1c7d118").AllFeatures, feature);   //slayer talent2
            Helper.AppendAndReplace(ref library.Get<BlueprintFeatureSelection>("43d1b15873e926848be2abf0ea3ad9a8").AllFeatures, feature);   //slayer talent6
            Helper.AppendAndReplace(ref library.Get<BlueprintFeatureSelection>("913b9cf25c9536949b43a2651b7ffb66").AllFeatures, feature);   //slayerTalent10
            Helper.AppendAndReplace(ref library.Get<BlueprintFeatureSelection>("67f499218a0e22944abab6fe1c9eaeee").AllFeatures, feature);   //VivsectionistDiscoverySelection
        }

    }
}
