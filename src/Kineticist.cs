using CallOfTheWild;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.Kineticist.ActivatableAbility;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FumisCodex
{
    /*
     * Notes:
     * 
     */
    public class Kineticist
    {
        static LibraryScriptableObject library => Main.library;

        //base game stuff
        static public BlueprintCharacterClass kineticist_class = library.Get<BlueprintCharacterClass>("42a455d9ec1ad924d889272429eb8391");
        static public BlueprintFeatureSelection infusion_selection = library.Get<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea");    //InfusionSelection
        static public BlueprintFeatureSelection wildtalent_selection = library.Get<BlueprintFeatureSelection>("5c883ae0cd6d7d5448b7a420f51f8459");    //WildTalentSelection
        static public BlueprintAbility earth_base = library.Get<BlueprintAbility>("e53f34fb268a7964caf1566afb82dadd");   //EarthBlastBase
        static public BlueprintAbility cold_base = library.Get<BlueprintAbility>("7980e876b0749fc47ac49b9552e259c1");   //ColdBlastBase
        static public BlueprintAbility metal_base = library.Get<BlueprintAbility>("6276881783962284ea93298c1fe54c48");   //MetalBlastBase
        static public BlueprintAbility ice_base = library.Get<BlueprintAbility>("403bcf42f08ca70498432cf62abee434");   //IceBlastBase
        //static public BlueprintFeature kinetic_blast_feature = library.Get<BlueprintFeature>("93efbde2764b5504e98e6824cab3d27c");   //KineticBlastFeature
        static public BlueprintItemWeapon weapon_blast_physical = library.Get<BlueprintItemWeapon>("65951e1195848844b8ab8f46d942f6e8");   //KineticBlastPhysicalWeapon
        static public BlueprintItemWeapon weapon_blast_energy = library.Get<BlueprintItemWeapon>("4d3265a5b9302ee4cab9c07adddb253f");   //KineticBlastEnergyWeapon
        static public BlueprintFeatureSelection elemental_focus = library.Get<BlueprintFeatureSelection>("1f3a15a3ae8a5524ab8b97f469bf4e3d");   //ElementalFocusSelection
        static public List<BlueprintAbility> all_base = library.Get<BlueprintBuff>("f5f3aa17dd579ff49879923fb7bc2adb").GetComponent<AutoMetamagic>().Abilities;

        //new stuff
        static public BlueprintFeature infusion_impale_feature;
        static public BlueprintBuff mobile_debuff;
        static public BlueprintFeature hurricane_queen_feat;

        //helpers
        static public ContextDiceValue physical_dice = Helper.CreateContextDiceValue(DiceType.D6, diceType: ContextValueType.Rank, diceRank: AbilityRankType.DamageDice, bonusType: ContextValueType.Shared);
        static public ContextDiceValue energy_dice = Helper.CreateContextDiceValue(DiceType.D6, diceType: ContextValueType.Rank, diceRank: AbilityRankType.DamageDice, bonusType: ContextValueType.Rank, bonusRank: AbilityRankType.DamageDice);

        // known issues:
        // - composite blasts consisting of two elements (ice) count as two attacks and will roll concealment/mirror-image individually. also true for crit and crit confirm
        // - "Miss" text doesn't show, if attack roll on consecutive hits was too high compared to initial roll
        // - "Miss" text shows up, if attack roll on consecutive hits was too low compared to initial roll
        // ! found "RuleAttackRoll ruleAttackRoll = Rulebook.CurrentContext.LastEvent<RuleAttackRoll>();" which might be useful for improvement!
        static public void createImpaleInfusion()
        {
            var earth_blast = library.Get<BlueprintFeature>("7f5f82c1108b961459c9884a0fa0f5c4");    //EarthBlastFeature
            var earth_blast_ab = library.Get<BlueprintAbility>("b28c336c10eb51c4a8ded0258d5742e1"); //EarthBlastAbility
            var metal_blast = library.Get<BlueprintFeature>("ad20bc4e586278c4996d4a81b2448998");    //MetalBlastFeature
            var metal_blast_ab = library.Get<BlueprintAbility>("665cfd3718c4f284d80538d85a2791c9");    //MetalBlastAbility
            var ice_blast = library.Get<BlueprintFeature>("a8cc34ca1a5e55a4e8aa5394efe2678e");    //IceBlastFeature
            var ice_blast_ab = library.Get<BlueprintAbility>("519e36decde7c964d87c2ffe4d3d8459");    //IceBlastAbility
            var icon = library.Get<BlueprintFeature>("2aad85320d0751340a0786de073ee3d5").Icon;  //TorrentInfusionFeature

            // create new impale feature
            // - add to selection of infusions
            infusion_impale_feature = Helpers.CreateFeature(
                    "InfusionImpaleFeature",
                    "Impale",
                    "Element: earth\nType: form infusion\nLevel: 3\nBurn: 2\nAssociated Blasts: earth, metal, ice\n"
                        + "You extend a long, sharp spike of elemental matter along a line, impaling multiple foes. Make a single attack roll against each creature or object in a 30-foot line.",
                    "f942f82c01c34c7da5f1131f5484e8b4",
                    icon,
                    FeatureGroup.KineticBlastInfusion,
                    Helpers.PrerequisiteFeaturesFromList(new BlueprintFeature[] { earth_blast, metal_blast, ice_blast }, true),
                    Helpers.PrerequisiteClassLevel(kineticist_class, 6),
                    Helpers.PrerequisiteFeature(elemental_focus)
                );
            infusion_impale_feature.IsClassFeature = true;
            Helper.AppendAndReplace(ref infusion_selection.AllFeatures, infusion_impale_feature);

            #region create impale ability - earth
            // - clone from water torrent
            // - replace EffectRunAction to a custom one: damage as usual for kineticist, only piercing, no saving throw, must roll attack, can crit
            // - replace projectile to what we want
            // - replace AbilityKineticist to fix CachedDamageInfo
            // - replace spawnFx
            // - replace requirement to need the impale feature
            // - add as variant to parent blast
            var earth_impale_ability = library.CopyAndAdd<BlueprintAbility>("93cc42235edc6824fa7d54b83ed4e1fe", "EarthImpaleAbility", "adcf52d3bc874d9a94250053b7ebf6e4"); // TorrentWaterBlastAbility
            earth_impale_ability.SetNameDescriptionIcon(infusion_impale_feature);
            earth_impale_ability.LocalizedSavingThrow = Helpers.savingThrowNone;
            earth_impale_ability.Parent = earth_base;

            //var damage_roll = Helpers.CreateActionDealDamage(PhysicalDamageForm.Piercing, damage_dice, isAoE: false, halfIfSaved: false, IgnoreCritical: false);
            var earth_damage_roll = NewComponents.ContextActionDealDamage2.CreateNew(PhysicalDamageForm.Piercing, physical_dice, isAoE: true, halfIfSaved: false, IgnoreCritical: false);
            earth_damage_roll.Half = false;
            earth_damage_roll.WeaponOverride = new ItemEntityWeapon(weapon_blast_physical);
            var earth_actions = Helper.CreateAbilityEffectRunAction(0, earth_damage_roll);
            earth_impale_ability.ReplaceComponent<AbilityEffectRunAction>(earth_actions);

            earth_impale_ability.ReplaceComponent<AbilityDeliverProjectile>(a =>
            {
                a.Type = AbilityProjectileType.Line;
                a.Length = new Feet(30f);
                a.LineWidth = new Feet(5f);
                a.NeedAttackRoll = true;
                a.Weapon = weapon_blast_physical;
                a.Projectiles = library.Get<BlueprintProjectile>("5d66a6c3cac5124469b2d0474e53ecab").ToArray();  // Kinetic_EarthBlastLine00: 5d66a6c3cac5124469b2d0474e53ecab
            });

            earth_impale_ability.ReplaceComponent<AbilityKineticist>(a =>
            {
                a.InfusionBurnCost = 2;
                a.BlastBurnCost = 0;
                a.Amount = 1;
                a.CachedDamageInfo = earth_blast_ab.GetComponent<AbilityKineticist>().CachedDamageInfo;
            });

            //earth_impale_ability.RemoveComponents<SpellDescriptorComponent>();
            earth_impale_ability.RemoveComponents<AbilitySpawnFx>(); earth_impale_ability.AddComponents(earth_blast_ab.GetComponents<AbilitySpawnFx>());
            earth_impale_ability.ReplaceComponent<AbilityCasterHasFacts>( a => a.Facts = infusion_impale_feature.ToArray() );
            earth_impale_ability.ReplaceComponent<AbilityShowIfCasterHasFact>(a => a.UnitFact = infusion_impale_feature);
            #endregion
            

            #region create impale ability - metal
            var metal_impale_ability = library.CopyAndAdd<BlueprintAbility>("82db79a0b4e91dc4ea2938192e6fc7af", "MetalImpaleAbility", "c25c7c8f05284317984ddcbcba69f53c");  //TorrentSandstormBlastAbility
            metal_impale_ability.SetNameDescriptionIcon(infusion_impale_feature);
            metal_impale_ability.LocalizedSavingThrow = Helpers.savingThrowNone;
            metal_impale_ability.Parent = metal_base;

            metal_impale_ability.ReplaceComponent<AbilityEffectRunAction>(earth_actions);   //this can be reused, because both deal full physical damage

            // AbilityDeliverProjectile can be kept, because the fx and form/range is the same as sandstorm-torrent

            metal_impale_ability.ReplaceComponent<AbilityKineticist>(a =>
            {
                a.InfusionBurnCost = 2;
                a.BlastBurnCost = 2;
                a.Amount = 1;
                a.CachedDamageInfo = metal_blast_ab.GetComponent<AbilityKineticist>().CachedDamageInfo;
            });

            // AbilitySpawnFx can be kept
            metal_impale_ability.ReplaceComponent<AbilityCasterHasFacts>(a => a.Facts = infusion_impale_feature.ToArray());
            metal_impale_ability.ReplaceComponent<AbilityShowIfCasterHasFact>(a => a.UnitFact = infusion_impale_feature);
            #endregion
            
            
            #region create impale ability - ice
            var ice_impale_ability = library.CopyAndAdd<BlueprintAbility>("d02fba9ae78f12642b4111a4bbbdc023", "IceImpaleAbility", "50486077f898441dba7c2922a3d251ed");  //TorrentBlizzardBlastAbility
            ice_impale_ability.SetNameDescriptionIcon(infusion_impale_feature);
            ice_impale_ability.LocalizedSavingThrow = Helpers.savingThrowNone;
            ice_impale_ability.Parent = ice_base;

            //var damage_roll = Helpers.CreateActionDealDamage(PhysicalDamageForm.Piercing, damage_dice, isAoE: false, halfIfSaved: false, IgnoreCritical: false);
            var ice_damage_roll = NewComponents.ContextActionDealDamage2.CreateNew(PhysicalDamageForm.Piercing, physical_dice, isAoE: true, halfIfSaved: false, IgnoreCritical: false);
            ice_damage_roll.Half = false;
            ice_damage_roll.WeaponOverride = new ItemEntityWeapon(weapon_blast_physical);
            var ice_damage_roll2 = NewComponents.ContextActionDealDamage2.CreateNew(DamageEnergyType.Cold, physical_dice, isAoE: true, halfIfSaved: false, IgnoreCritical: false);
            ice_damage_roll2.Half = false;
            ice_damage_roll2.WeaponOverride = new ItemEntityWeapon(weapon_blast_physical);  //this is correct, because the bonus damage is the same as physical
            //ice_damage_roll2.ReadPreRolledFromSharedValue = true;
            //ice_damage_roll2.PreRolledSharedValue = AbilitySharedValue.Duration;
            ice_impale_ability.ReplaceComponent<AbilityEffectRunAction>(Helper.CreateAbilityEffectRunAction(0, ice_damage_roll, ice_damage_roll2));

            // AbilityDeliverProjectile can be kept, because the fx and form/range is the same as blizzard-torrent

            ice_impale_ability.ReplaceComponent<AbilityKineticist>(a =>
            {
                a.InfusionBurnCost = 2;
                a.BlastBurnCost = 2;
                a.Amount = 1;
                a.CachedDamageInfo = ice_blast_ab.GetComponent<AbilityKineticist>().CachedDamageInfo;
            });

            // AbilitySpawnFx can be kept
            ice_impale_ability.ReplaceComponent<AbilityCasterHasFacts>(a => a.Facts = infusion_impale_feature.ToArray());
            ice_impale_ability.ReplaceComponent<AbilityShowIfCasterHasFact>(a => a.UnitFact = infusion_impale_feature);
            #endregion

            Helper.AppendAndReplace(ref earth_base.GetComponent<AbilityVariants>().Variants, earth_impale_ability);
            Helper.AppendAndReplace(ref metal_base.GetComponent<AbilityVariants>().Variants, metal_impale_ability);
            Helper.AppendAndReplace(ref ice_base.GetComponent<AbilityVariants>().Variants, ice_impale_ability);
        }

        static public void extendSprayInfusion(bool enabled = true)
        {
            // references from base game
            // - cold_base
            var cold_blast = library.Get<BlueprintFeature>("ce625487d909b154c9305e60e4fc7d60"); //ColdBlastFeature
            var cold_blast_ab = library.Get<BlueprintAbility>("f6d32ecd20ebacb4e964e2ece1c70826"); //ColdBlastAbility

            // edits to base game
            // - rename spray infusion to reflect new element
            // - add cold as valid prerequisite
            var spray_infusion_feature = library.Get<BlueprintFeature>("b5852e8287f12d34ca6f84fcc7019f07");
            if (enabled)
            {
                spray_infusion_feature.SetNameDescription("Spray", "Element: water\nType: form infusion\nLevel: 4\nBurn: 3\nAssociated Blasts: charged water, water, cold\n"
                    + "You diffuse your kinetic blast to spray out wildly. All creatures and objects in a 30-foot cone take half your normal amount of blast damage (or full damage for energy blasts). The saving throw DC is Dexterity-based.");
                Helper.AppendAndReplace(ref spray_infusion_feature.GetComponent<PrerequisiteFeaturesFromList>().Features, cold_blast);
            }

            // new features
            // - clone from any energy simple blast
            // - change EffectRunAction to what we want
            // - change projectile to what we want
            // - adjust burn cost and fix displayed DamageInfo to that of cold blast
            // - replace spawnFx
            // - replace requirement to need the spray feature
            // - add as variant to parent blast
            var cold_spray_ability = library.CopyAndAdd<BlueprintAbility>("a240a6d61e1aee040bf7d132bfe1dc07", "SprayColdBlastAbility", "a8a06a9a236b4f6bbfe55c25d65067f3"); // FanOfFlamesFireBlastAbility
            cold_spray_ability.SetNameDescriptionIcon(spray_infusion_feature);
            cold_spray_ability.ResourceAssetIds = cold_blast_ab.ResourceAssetIds;
            cold_spray_ability.Parent = cold_base;
            
            cold_spray_ability.ReplaceComponent<AbilityEffectRunAction>(a => 
            {
                a.SavingThrowType = SavingThrowType.Reflex;
                a.Actions = cold_blast_ab.GetComponent<AbilityEffectRunAction>().Actions;
                a.Actions.Actions[0] = Helpers.CreateActionDealDamage(DamageEnergyType.Cold, energy_dice, isAoE: true, halfIfSaved: true, IgnoreCritical: true);
            });

            cold_spray_ability.ReplaceComponent<AbilityDeliverProjectile>(a =>
            {
                a.Type = AbilityProjectileType.Cone;
                a.Length = new Feet(30f);
                a.LineWidth = new Feet(5f);
                a.NeedAttackRoll = true;
                a.Weapon = weapon_blast_energy;
                a.Projectiles = library.Get<BlueprintProjectile>("c202b61bf074a7442bf335b27721853f").ToArray();  //ColdCone30Feet00
            });

            cold_spray_ability.ReplaceComponent<AbilityKineticist>(a =>
            {
                a.InfusionBurnCost = 3;
                a.BlastBurnCost = 0;
                a.Amount = 1;
                a.CachedDamageInfo = cold_blast_ab.GetComponent<AbilityKineticist>().CachedDamageInfo;
            });

            cold_spray_ability.RemoveComponents<AbilityExecuteActionOnCast>();
            cold_spray_ability.RemoveComponents<AbilitySpawnFx>(); cold_spray_ability.AddComponents(cold_blast_ab.GetComponents<AbilitySpawnFx>());
            cold_spray_ability.ReplaceComponent<SpellDescriptorComponent>(Helpers.CreateSpellDescriptor(SpellDescriptor.Cold));
            cold_spray_ability.ReplaceComponent<AbilityCasterHasFacts>(a => a.Facts = spray_infusion_feature.ToArray());
            cold_spray_ability.ReplaceComponent<AbilityShowIfCasterHasFact>(a => a.UnitFact = spray_infusion_feature);

            if (enabled)
                Helper.AppendAndReplace(ref cold_base.GetComponent<AbilityVariants>().Variants, cold_spray_ability);
        }

        static public void createExtraWildTalentFeat(bool enabled = true)
        {
            var extra_wild_talent_selection = Helpers.CreateFeatureSelection(
                "ExtraWildTalentFeat",
                "Extra Wild Talent",
                "You gain a wild talent for which you meet the prerequisites. You can select an infusion or a non-infusion wild talent, but not a blast or defense wild talent.\nSpecial: You can take this feat multiple times. Each time, you must choose a different wild talent.",
                "3714a8b82ec048e9bef62fa1cfc6c105",
                library.Get<BlueprintFeature>("42f96fc8d6c80784194262e51b0a1d25").Icon, //ExtraArcanePool.Icon
                FeatureGroup.Feat,
                kineticist_class.PrerequisiteClassLevel(1, true)
            );
            extra_wild_talent_selection.AllFeatures = infusion_selection.AllFeatures.AppendRange(   //InfusionSelection
                                                      wildtalent_selection.AllFeatures);            //+WildTalentSelection

            BlueprintFeature extra_wild_talent_feat = extra_wild_talent_selection;
            extra_wild_talent_feat.Ranks = 10;
            extra_wild_talent_feat.Groups = new FeatureGroup[] { FeatureGroup.Feat };

            if (enabled)
                library.AddFeats(extra_wild_talent_feat);
        }

        static public void createPreciseBlastTalent(bool enabled = true)
        {
            var metamagic_comp = ScriptableObject.CreateInstance<AutoMetamagic>();
            Harmony12.AccessTools.Field(typeof(AutoMetamagic), "m_AllowedAbilities").SetValue(metamagic_comp, 2); //enum AllowedType.KineticistBlast
            metamagic_comp.Metamagic = (Metamagic)CallOfTheWild.MetamagicFeats.MetamagicExtender.Selective;
            metamagic_comp.Abilities = all_base;

            var precise_blast_feature = ScriptableObject.CreateInstance<BlueprintFeature>();
            precise_blast_feature.name = "PreciseBlast";
            precise_blast_feature.SetNameDescriptionIcon("Precise Blast", "You have fine control over your kinetic blast. Your allies are excluded from the effects of your blasts with a duration of instantaneous.\nDoes not affect Wall, Deadly Earth, Cloud, or other blasts that have a duration other than instantaneous.", MetamagicFeats.selective_metamagic.Icon);
            precise_blast_feature.Groups = FeatureGroup.KineticWildTalent.ToArray();
            precise_blast_feature.Ranks = 1;
            precise_blast_feature.IsClassFeature = true;
            precise_blast_feature.SetComponents(metamagic_comp, Helpers.PrerequisiteNoFeature(precise_blast_feature));
            library.AddAsset(precise_blast_feature, "5beb96c3591a4506bf65e2b4e5aff883");

            if (enabled)
                Helper.AppendAndReplace(ref wildtalent_selection.AllFeatures, precise_blast_feature);
        }

        // known issue:
        // - does not restrict your move action to be used for moving (won't fix)
        // - gathering long should not slow you, since it already consumes a standard action (won't fix)
        // - the slow effect is ignored, if applied while moving. the ability should interrupt movement shortly
        static public void createMobileGatheringFeat()
        {
            // --- base game stuff ---
            var buff1 = library.Get<BlueprintBuff>("e6b8b31e1f8c524458dc62e8a763cfb1");   //GatherPowerBuffI
            var buff2 = library.Get<BlueprintBuff>("3a2bfdc8bf74c5c4aafb97591f6e4282");   //GatherPowerBuffII
            var buff3 = library.Get<BlueprintBuff>("82eb0c274eddd8849bb89a8e6dbc65f8");   //GatherPowerBuffIII
            var gather_original_ab = library.Get<BlueprintAbility>("6dcbffb8012ba2a4cb4ac374a33e2d9a");    //GatherPower
            //var slowed_debuff = library.Get<BlueprintBuff>("488e53ede2802ff4da9372c6a494fb66");    //Slowed
            // -----------------------

            buff1.SetIcon(gather_original_ab.Icon);
            buff1.SetName(buff1.Name + " Lv1");
            buff2.SetIcon(gather_original_ab.Icon);
            buff2.SetName(buff2.Name + " Lv2");
            buff3.SetIcon(gather_original_ab.Icon);
            buff3.SetName(buff3.Name + " Lv3");

            // new buff that halves movement speed, disallows normal gathering, penalty on concentration?
            mobile_debuff = ScriptableObject.CreateInstance<BlueprintBuff>();
            mobile_debuff.name = "MobileGatheringDebuff";
            mobile_debuff.SetNameDescriptionIcon("Mobile Gathering Debuff", "Your movement speed is halved after gathering power.", Helper.Image2Sprite.Create("GatherMobileHigh.png"));
            //Harmony12.AccessTools.Field(typeof(BlueprintBuff), "m_Flags").SetValue(mobile_debuff, 2); //HiddenInUi
            mobile_debuff.IsClassFeature = true;
            mobile_debuff.SetComponents(UnitCondition.Slowed.CreateAddCondition());
            library.AddAsset(mobile_debuff, "ffd79fee05bf4e6dad7156e895f3cf27");
            var can_gather = Helper.CreateAbilidtyRequirementHasBuffTimed(NewComponents.CompareType.LessOrEqual, 1.Rounds().Seconds, buff1, buff2, buff3);
            
            // cannot use usual gathering after used mobile gathering
            gather_original_ab.AddComponent(Helper.CreateAbilityRequirementHasBuffs(true, mobile_debuff));

            // ability as free action that applies buff and 1 level of gatherpower
            // - is free action
            // - increases gather power by 1 level, similiar to GatherPower:6dcbffb8012ba2a4cb4ac374a33e2d9a
            // - applies debuff
            // - get same restriction as usual gathering
            var mobile_gathering_short_ab = Helpers.CreateAbility(
                "MobileGatheringShort",
                "Mobile Gathering (Move Action)",
                "You may move up to half your normal speed while gathering power.",
                "a482da35c21a4a0e801849610e03df87",
                Helper.Image2Sprite.Create("GatherMobileLow.png"),
                AbilityType.Special,
                UnitCommand.CommandType.Free,
                AbilityRange.Personal,
                "",
                ""
            );
            mobile_gathering_short_ab.CanTargetSelf = true;
            mobile_gathering_short_ab.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Self;//UnitAnimationActionCastSpell.CastAnimationStyle.Kineticist;
            mobile_gathering_short_ab.HasFastAnimation = true;
            //Helpers.CreateApplyBuff(mobile_debuff, Helpers.CreateContextDuration(1), false, false, true);
            var apply_debuff = Helper.CreateActionApplyBuff(mobile_debuff, 1);
            var three2three = Helpers.CreateConditional(Helpers.CreateConditionHasBuff(buff3), new GameAction[] { Helper.CreateActionApplyBuff(buff3, 2) });
            var two2three = Helpers.CreateConditional(Helpers.CreateConditionHasBuff(buff2), new GameAction[] { Helper.CreateActionRemoveBuff(buff2), Helper.CreateActionApplyBuff(buff3, 2) });
            var one2two = Helpers.CreateConditional(Helpers.CreateConditionHasBuff(buff1), new GameAction[] { Helper.CreateActionRemoveBuff(buff1), Helper.CreateActionApplyBuff(buff2, 2) });
            var zero2one = Helpers.CreateConditional(Helper.CreateConditionHasNoBuff(buff1, buff2, buff3), new GameAction[] { Helper.CreateActionApplyBuff(buff1, 2) });
            var hasMoveAction = Helper.CreateRequirementActionAvailable(false, NewComponents.ActionType.Move);
            mobile_gathering_short_ab.SetComponents(can_gather, hasMoveAction, Helper.CreateAbilityEffectRunAction(0, apply_debuff, three2three, two2three, one2two, zero2one));
            
            // same as above but standard action and 2 levels of gatherpower
            var mobile_gathering_long_ab = Helpers.CreateAbility(
                "MobileGatheringLong",
                "Mobile Gathering (Full Round)",
                "You may move up to half your normal speed while gathering power.\nTip for Turn-Based Combat: Move before using this ability.",
                "e7cd3a8200f04c8fae099d5d2f4afa0b",
                Helper.Image2Sprite.Create("GatherMobileMedium.png"),
                AbilityType.Special,
                UnitCommand.CommandType.Standard,
                AbilityRange.Personal,
                "",
                ""
            );
            mobile_gathering_long_ab.CanTargetSelf = true;
            mobile_gathering_long_ab.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Self;
            mobile_gathering_long_ab.HasFastAnimation = true;
            var one2three = Helpers.CreateConditional(Helpers.CreateConditionHasBuff(buff1), new GameAction[] { Helper.CreateActionRemoveBuff(buff1), Helper.CreateActionApplyBuff(buff3, 2) });
            var zero2two = Helpers.CreateConditional(Helper.CreateConditionHasNoBuff(buff1, buff2, buff3), new GameAction[] { Helper.CreateActionApplyBuff(buff2, 2) });
            mobile_gathering_long_ab.SetComponents(can_gather, Helper.CreateAbilityEffectRunAction(0, apply_debuff, three2three, two2three, one2three, zero2two));
            
            var mobile_gathering_feat = Helpers.CreateFeature(
                "MobileGatheringFeat",
                "Mobile Gathering",
                "While gathering power, you can move up to half your normal speed. This movement provokes attacks of opportunity as normal.",
                "60edffeba6d74e0f831c00692e5fc621",
                mobile_debuff.Icon,
                FeatureGroup.Feat,
                kineticist_class.PrerequisiteClassLevel(7, true),
                Helpers.CreateAddFacts(mobile_gathering_short_ab, mobile_gathering_long_ab)
            );
            mobile_gathering_feat.Ranks = 1;
            mobile_gathering_feat.IsClassFeature = true;
            library.AddFeats(mobile_gathering_feat);
        }

        // known issue: sleet storm still prevents bow attacks, however I don't think any level 18 kineticist will want to use a bow ever
        static public void createHurricaneQueen()
        {
            //base stuff
            var winds_feature = library.Get<BlueprintFeature>("bb0de2047c448bd46aff120be3b39b7a");  //EnvelopingWinds
            var winds_stack = library.Get<BlueprintFeature>("bbba1600582cf8446bb515a33bd89af8");    //EnvelopingWindsEffectFeature
            var weather_rain = library.Get<BlueprintBuff>("f37b708de9eeb2c4ab248d79bb5b5aa7");    //RainModerateBuff
            var weather_storm = library.Get<BlueprintBuff>("7c260a8970e273d439f2a2e19b7196af");    //RainStormBuff
            //845332298344c6447972dc9b131add08 SnowModerateBuff
            
            //stuff from other mods
            var sleet_storm = library.Get<BlueprintBuff>("c1a3c2f5d8824f66b7adfa9800194547");    //SleetStormBuff - CotW

            // - adds wild talent "HurricaneQueen"
            // - Patch hooks into RuleAttackRole IncreaseMissChance(), checks for HurricaneQueen and increase max to 100
            // - make unit ignore weather effects, components from CotW
            hurricane_queen_feat = Helpers.CreateFeature(
                "HurricaneQueenFeature",
                "Hurricane Queen",
                "You are one with the hurricane. Your enveloping winds defense wild talent has an additional 25% chance of deflecting non-magical ranged attacks, and your total deflection chance can exceed the usual cap of 75%. All wind and weather (including creatures using the whirlwind monster ability) don't affect you; for example, you could shoot arrows directly through a tornado without penalty.",
                "5d5cbd74010e41f089fe4b96fd2fc50e",
                cold_base.Icon,
                FeatureGroup.KineticWildTalent,
                Helpers.PrerequisiteClassLevel(kineticist_class, 18),
                Helpers.PrerequisiteFeature(winds_feature),
                Helpers.CreateAddFacts(winds_stack, winds_stack, winds_stack, winds_stack, winds_stack),
                //Helper.CreateSpecificBuffImmunity(weather_rain),
                //Helper.CreateSpecificBuffImmunity(weather_storm),
                Helper.CreateSpecificBuffImmunity(sleet_storm),
                Helpers.Create<CallOfTheWild.ConcealementMechanics.IgnoreFogConcelement>(),
                Helpers.CreateAddFact(CallOfTheWild.NewSpells.immunity_to_wind)
            );

            Helper.AppendAndReplace(ref wildtalent_selection.AllFeatures, hurricane_queen_feat);
        }
        
        static public void createMindShield(bool enabled = true)
        {
            var psychokineticist = library.Get<BlueprintArchetype>("f2847dd4b12fffd41beaa3d7120d27ad");//PsychokineticistArchetype
            var burn_number = library.Get<BlueprintUnitProperty>("02c5943c77717974cb7fa1b7c0dc51f8");//BurnNumberProperty
            var buff_daze = library.Get<BlueprintBuff>("9934fedff1b14994ea90205d189c8759");//DazeBuff
            var buff_confusion = library.Get<BlueprintBuff>("886c7407dc629dc499b9f1465ff382df");//Confusion
            var buff_cowering = library.Get<BlueprintBuff>("02924d88f80c5374eacbbccb499d5778");//CoweringCommonBuff
            var buff_frightened = library.Get<BlueprintBuff>("f08a7239aa961f34c8301518e71d4cdf");//Frightened
            var buff_dominateperson = library.Get<BlueprintBuff>("c0f4e1c24c9cd334ca988ed1bd9d201f");//DominatePersonBuff
            //var buff_panicked = library.Get<BlueprintBuff>("");//

            var custom_daze = library.CopyAndAdd(buff_daze, "Dazed", "4b35a9e429fb472aaf45585d7d82e3e1");
            custom_daze.AddComponent( buff_dominateperson.ComponentsArray[2] );
            
            // - simply gives counter bonus to skill penalties
            // - also replaces most common will based debuffs with daze (idea being that daze is better than being mind-controlled or running in circles)
            // - also give new saving throw like DominatePersonBuff
            BlueprintBuff mind_shield_buff = Helpers.CreateBuff(
                "MindShieldBuff",
                "Mind Shield",
                "You ignore the penalties of Mind Burn to Wisdom-based skill checks. Additionally if you fail a Will saving throw against a mind-affecting spell, you are instead dazed for the duration of the spell, but you may re-try the saving throw each round.",
                "0ba3718f568a4c9097307a7af57c8f88",
                null,
                null
            );
            Harmony12.AccessTools.Field(typeof(BlueprintBuff), "m_Flags").SetValue(mind_shield_buff, 2);//HiddenInUi
            var b_comp1 = Helpers.CreateAddContextStatBonus(StatType.SkillPerception, ModifierDescriptor.UntypedStackable, ContextValueType.CasterCustomProperty, multiplier:2);
            b_comp1.Value.CustomProperty = burn_number;
            var b_comp2 = Helpers.CreateAddContextStatBonus(StatType.SkillLoreNature, ModifierDescriptor.UntypedStackable, ContextValueType.CasterCustomProperty, multiplier: 2);
            b_comp2.Value.CustomProperty = burn_number;
            var b_comp3 = Helpers.CreateAddContextStatBonus(StatType.SkillLoreReligion, ModifierDescriptor.UntypedStackable, ContextValueType.CasterCustomProperty, multiplier: 2);
            b_comp3.Value.CustomProperty = burn_number;
            mind_shield_buff.SetComponents(b_comp1, b_comp2, b_comp3,
                Helper.CreateBuffSubstitutionOnApply(buff_confusion, custom_daze),
                Helper.CreateBuffSubstitutionOnApply(buff_cowering, custom_daze),
                Helper.CreateBuffSubstitutionOnApply(buff_frightened, custom_daze),
                Helper.CreateBuffSubstitutionOnApply(buff_dominateperson, custom_daze)
            );
            
            var mind_shield_feature = Helpers.CreateFeature(
                "MindShieldFeature",
                mind_shield_buff.Name,
                mind_shield_buff.Description,
                "eeab6701ee7447d3b62996a62e315bd7",
                cold_base.Icon,
                FeatureGroup.KineticWildTalent
            );
            var f_comp1 = psychokineticist.CreatePrerequisite(6);
            var f_comp2 = Helper.CreateAddKineticistBurnValueChangedTrigger(Helper.CreateActionApplyBuff(mind_shield_buff, permanent: true));
            var f_comp3 = Helpers.PrerequisiteNoFeature(mind_shield_feature);
            var f_comp4 = Helpers.CreateAddFact(mind_shield_buff);
            mind_shield_feature.SetComponents(f_comp1, f_comp2, f_comp3, f_comp4);

            if (enabled)
                Helper.AppendAndReplace(ref wildtalent_selection.AllFeatures, mind_shield_feature);
        }

        // future
        static public void fixExpandElement()
        {

        }

        // work on hold! does not work as intended; composite blasts are not granted, simple blasts are not granted, or other issues
        static public void createExpandElementalFocus()
        {
            var expand_element_selection = Helpers.CreateFeatureSelection(
                "ExpandElementalFocus",
                "Expand Elemental Focus",
                "You learn to use another element. You can select any element and gain one of that element’s simple blast wild talents that you do not already possess.",
                "0e2aa6b75b334b019fbfa7375ecaca85",
                elemental_focus.Icon,   //ElementalFocusSelection.Icon
                FeatureGroup.Feat,
                kineticist_class.PrerequisiteClassLevel(1, true)
            );
            expand_element_selection.AllFeatures = new BlueprintFeature[6];
            //expand_element_selection.AllFeatures[0] = library.Get<BlueprintProgression>("6f1d86ae43adf1049834457ce5264003");  //AirBlastProgression
            //expand_element_selection.AllFeatures[1] = library.Get<BlueprintProgression>("e4027e0fec48e8048a172c6627d4eba9");  //WaterBlastProgression
            //expand_element_selection.AllFeatures[2] = library.Get<BlueprintProgression>("dbb1159b0e8137c4ea20434a854ae6a8");  //ColdBlastProgression
            //expand_element_selection.AllFeatures[3] = library.Get<BlueprintProgression>("fbed3ca8c0d89124ebb3299ccf68c439");  //FireBlastProgression
            //expand_element_selection.AllFeatures[4] = library.Get<BlueprintProgression>("d945ac76fc6a06e44b890252824db30a");  //EarthBlastProgression
            //expand_element_selection.AllFeatures[5] = library.Get<BlueprintProgression>("ba7767cb03f7f3949ad08bd3ff8a646f");  //ElectricBlastProgression

            expand_element_selection.AllFeatures = library.Get<BlueprintFeatureSelection>("4204bc10b3d5db440b1f52f0c375848b").AllFeatures;    //SecondatyElementalFocusSelection
            foreach (BlueprintProgression focus_selection in expand_element_selection.AllFeatures)
            {
                //focus_selection.LevelEntries[0].Level = 1;
                foreach (var entry in focus_selection.LevelEntries)
                    entry.Level = 1;
            }

            BlueprintFeature expand_element_feat = expand_element_selection;
            expand_element_feat.Ranks = 1;
            expand_element_feat.Groups = new FeatureGroup[] { FeatureGroup.Feat };
            library.AddFeats(expand_element_feat);
        }

        #region Patches

        [Harmony12.HarmonyPatch(typeof(RuleAttackRoll), "IncreaseMissChance")]
        class RemoveMissChanceLimitPatch
        {
            static bool Prefix(RuleAttackRoll __instance, int value)
            {
                if (value > 0
                    && __instance.Target.IsPlayerFaction
                    && Kineticist.hurricane_queen_feat != null
                    && __instance.Target.Descriptor.Progression.Features.HasFact(Kineticist.hurricane_queen_feat))
                {
                    Harmony12.AccessTools.Property(typeof(RuleAttackRoll), "MissChance").SetValue(__instance, Math.Min(value, 100));
                    return false;
                }
                return true;
            }
        }

        [Harmony12.HarmonyPatch(typeof(BuffSubstitutionOnApply), "OnEventAboutToTrigger")]
        class FixBuffSubstitutionDCLossPatch
        {
            static bool Prefix(BuffSubstitutionOnApply __instance, RuleApplyBuff evt)
            {
                if (evt.Blueprint == __instance.GainedFact)
                {
                    evt.CanApply = false;
                    //__instance.Owner.AddBuff(__instance.SubstituteBuff, evt.Initiator, evt.Duration, null);
                    __instance.Owner.AddBuff(__instance.SubstituteBuff, evt.Context, evt.Duration); // this fixed the lost context parameters, which in turn made it much easier to dispel the replacement buff
                }

                return false;
            }
        }

        #endregion
        
    }
}
