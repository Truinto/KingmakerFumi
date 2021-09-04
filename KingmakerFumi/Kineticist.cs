//using CallOfTheWild;
using FumisCodex.NewComponents;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.Designers.EventConditionActionSystem.Events;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.Kineticist.ActivatableAbility;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.Utility;
using Kingmaker.View.Animation;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using Guid = FumisCodex.GuidManager;

namespace FumisCodex
{
    /*
     * Notes:
     */
    public class Kineticist
    {
        #region References

        static LibraryScriptableObject library => Main.library;

        //base game stuff
        public static BlueprintCharacterClass kineticist_class = library.Get<BlueprintCharacterClass>("42a455d9ec1ad924d889272429eb8391");
        public static BlueprintProgression kineticist_progression = library.Get<BlueprintProgression>("b79e92dd495edd64e90fb483c504b8df");//KineticistProgression
        public static BlueprintFeatureSelection infusion_selection = library.Get<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea");    //InfusionSelection
        public static BlueprintFeatureSelection wildtalent_selection = library.Get<BlueprintFeatureSelection>("5c883ae0cd6d7d5448b7a420f51f8459");    //WildTalentSelection
        public static BlueprintItemWeapon weapon_blast_physical = library.Get<BlueprintItemWeapon>("65951e1195848844b8ab8f46d942f6e8");   //KineticBlastPhysicalWeapon
        public static BlueprintItemWeapon weapon_blast_energy = library.Get<BlueprintItemWeapon>("4d3265a5b9302ee4cab9c07adddb253f");   //KineticBlastEnergyWeapon
        public static BlueprintFeatureSelection elemental_focus = library.Get<BlueprintFeatureSelection>("1f3a15a3ae8a5524ab8b97f469bf4e3d");   //ElementalFocusSelection

        //new stuff
        public static BlueprintFeature infusion_impale_feature;
        public static BlueprintBuff mobile_debuff;
        public static BlueprintFeature hurricane_queen_feat;
        public static BlueprintFeatureSelection extra_wild_talent_selection;

        //Helper
        public static ContextDiceValue physical_dice = Helper.CreateContextDiceValue(DiceType.D6, diceType: ContextValueType.Rank, diceRank: AbilityRankType.DamageDice, bonusType: ContextValueType.Shared);
        public static ContextDiceValue energy_dice = Helper.CreateContextDiceValue(DiceType.D6, diceType: ContextValueType.Rank, diceRank: AbilityRankType.DamageDice, bonusType: ContextValueType.Rank, bonusRank: AbilityRankType.DamageDice);

        public static ContextCalculateAbilityParamsBasedOnClass calcParamsBasedOnDex = Helper.Create<ContextCalculateAbilityParamsBasedOnClass>(a => { a.CharacterClass = kineticist_class; a.StatType = StatType.Dexterity; });
        public static ContextCalculateAbilityParamsBasedOnClass calcParamsBasedOnMainStat = Helper.Create<ContextCalculateAbilityParamsBasedOnClass>(a => { a.CharacterClass = kineticist_class; a.UseKineticistMainStat = true; });

        public static PrerequisiteFeaturesFromList prerequisite_air = library.Get<BlueprintFeature>("c8719b3c5c0d4694cb13abcc3b7e893b").GetComponent<PrerequisiteFeaturesFromList>();
        public static PrerequisiteFeature prerequisite_fire = HelperEA.PrerequisiteFeature(library.Get<BlueprintFeature>("cbc88c4c166a0ce4a95375a0a721bd01"));
        public static PrerequisiteFeature prerequisite_earth = HelperEA.PrerequisiteFeature(library.Get<BlueprintFeature>("7f5f82c1108b961459c9884a0fa0f5c4"));
        public static PrerequisiteFeaturesFromList prerequisite_water = library.Get<BlueprintFeature>("3ef666973adfa8f40af6c0679bd98ba5").GetComponent<PrerequisiteFeaturesFromList>();

        public static List<BlueprintAbility> all_base = library.Get<BlueprintBuff>("f5f3aa17dd579ff49879923fb7bc2adb").GetComponent<AutoMetamagic>().Abilities;
        public static OrderedDictionary blasts_byelement = new OrderedDictionary();
        //public static Dictionary<string, BlueprintAbility[]> blasts_byelement = new Dictionary<string, BlueprintAbility[]>();
        public static Dictionary<string, BlueprintAbility> base_byname = new Dictionary<string, BlueprintAbility>();
        public static Dictionary<string, List<BlueprintAbility>> blast_variants = new Dictionary<string, List<BlueprintAbility>>();

        #endregion

        #region Helper

        public static void init() // TODO: extend blast_variants (e.g. Impale) 
        {
            Main.DebugLog("base blast list: ");
            foreach (var blast in all_base)
            {
                int index = blast.name.IndexOf("Blast", StringComparison.Ordinal);
                if (index < 0)
                {
                    Main.DebugLog("Invalid blast: " + blast.name);
                    continue;
                }
                string name = blast.name.Substring(0, index);

                if (name == "KineticBladeChargeWater") name = "KineticBladeChargedWater";   // fix typo, not permanent

                if (blast.GetComponent<AbilityVariants>() != null && blast.name.EndsWith("Base", StringComparison.Ordinal))
                {
                    base_byname[name] = blast;
                    if (!blast_variants.ContainsKey(name)) blast_variants[name] = new List<BlueprintAbility>();
                    blast_variants[name].AddRange(blast.GetComponent<AbilityVariants>().Variants);
                    Main.DebugLog(name + ":" + blast.name);
                }
                else if (name.StartsWith("KineticBlade", StringComparison.Ordinal))
                {
                    name = name.Substring(12);
                    if (!blast_variants.ContainsKey(name)) blast_variants[name] = new List<BlueprintAbility>();
                    blast_variants[name].Add(blast);
                }
                else
                    Main.DebugLog("notfound?" + name + ":" + blast.name);
            }

            blasts_byelement["Air"] = new BlueprintAbility[] { base_byname["Air"], base_byname["Electric"] };
            blasts_byelement["Earth"] = new BlueprintAbility[] { base_byname["Earth"] };
            blasts_byelement["Fire"] = new BlueprintAbility[] { base_byname["Fire"] };
            blasts_byelement["Water"] = new BlueprintAbility[] { base_byname["Water"], base_byname["Cold"] };
        }

        public static void routineNewElement(string name, string displayName, string desc, string guid = null)
        {
            guid = guid ?? Guid.i.Get(name);

            throw new NotImplementedException();
        }

        public static void routineNewVariant(string name, string displayName, string desc, BlueprintFeature[] infusions_form, BlueprintFeature[] infusions_substance, bool isComposite = false, string guid = null)
        {
            guid = guid ?? Guid.i.Get(name);

            // create new feature (as SandstormBlastFeature)

            // create new base blast (as SandstormBlastBase)

            // add form infusions

            // add substance infusions

            throw new NotImplementedException();
        }

        public static void routineExpandVariant()
        {
            throw new NotImplementedException();
        }

        public static void routineNewFormInfusion(string name, string displayName, string desc, Sprite icon, Kin_Element[] elements, int class_level, int burn, 
            AbilityRange range, string duration, string SavingThrow, BlueprintComponent[] components, string guid = null)
        {
            bool isAoE = false, isSpawnArea = false, isAttackRoll = false;
            bool physicalHalfed, halfed;

            AbilityEffectRunAction abilityEffectRunAction = null;  //"Half": true if physical
            AbilityDeliverProjectile abilityDeliverProjectile = null;
            BlueprintAbilityAreaEffect abilityAreaEffect = null;

            // create new feature
            guid = guid ?? Guid.i.Get(name);
            var infusion_feature = HelperEA.CreateFeature(name, displayName, desc, guid, icon, FeatureGroup.KineticBlastInfusion,
                HelperEA.PrerequisiteClassLevel(kineticist_class, class_level, true),
                HelperEA.PrerequisiteFeaturesFromList(elements.Select(s => s.BlastFeature).ToArray()),
                HelperEA.PrerequisiteFeature(elemental_focus)
            );

            if (isAoE)
            {
                Helper.AppendAndReplace(ref components,
                    Contexts.CalculateRankDamageBonus);
            }
            else if (isSpawnArea)
            {
                Helper.AppendAndReplace(ref components,
                    calcParamsBasedOnMainStat);

                abilityAreaEffect.AddComponents(
                    Contexts.CalculateRankDamageBonus);
            }
            else if (isAttackRoll)
            {
                Helper.AppendAndReplace(ref components,
                    calcParamsBasedOnDex);
            }

            // add universal components (regardless of element)
            var cache = Helper.CreateAbilityKineticist(burn);   // TODO: cached
            Helper.AppendAndReplace(ref components,
                cache,
                Helper.CreateAbilityCasterHasFacts(infusion_feature),       // feature required to cast
                Helper.CreateAbilityShowIfCasterHasFact(infusion_feature)); // feature required to see


            foreach (var element in elements)
            {
                // create new variants
                string ab_name = name + element.Name + "BlastAbility";
                var ability = HelperEA.CreateAbility(
                    ab_name,
                    displayName,
                    desc,
                    Guid.i.Get(ab_name),
                    icon,
                    AbilityType.SpellLike,
                    UnitCommand.CommandType.Standard,
                    range,
                    duration,
                    SavingThrow,
                    components
                );
                ability.AddComponents(element.BasicRank);
                ability.AddComponents(element.BasicSFX);

                ability.SpellResistance = element.BasicVariant.IsEnergy;
                ability.Parent = element.BaseBlast;
                element.BaseBlast.AddToAbilityVariants(ability);

                var variant = new Kin_Variant()
                {
                    Parent = element,
                    InfusionAbility = ability,
                    InfusionFeature = infusion_feature,
                    //DamageDice,
                    //DamageBonus,
                    //Duration,
                    //AllActions,
                    //DamageActions,
                    //AreaAction,
                    BlastBurnCost = element.BasicVariant.BlastBurnCost,
                    InfusionBurnCost = burn,
                    PForm = 0,
                    EForm = (DamageEnergyType)(-1)
                };
            }

            Helper.AppendAndReplace(ref infusion_selection.AllFeatures, infusion_feature);
            throw new NotImplementedException();
        }

        public static void routineNewSubstanceInfusion()
        {
            throw new NotImplementedException();
        }

        public static void routineExpandSubstanceInfusion(BlueprintFeature substance, params BlueprintAbility[] baseAbility)
        {
            var fact = substance.GetComponent<AddFacts>()?.Facts;
            var activatable = fact != null && fact.Length == 1 ? fact[0] as BlueprintActivatableAbility : null;

            if (activatable != null)
                routineExpandSubstanceInfusion(activatable.Buff, baseAbility);
            else
                Main.DebugLogAlways("error: routineExpandSubstanceInfusion activatable is null " + substance.name);
        }
        public static void routineExpandSubstanceInfusion(BlueprintBuff substance, params BlueprintAbility[] baseAbility)
        {
            var trigger = substance.GetComponent<AddAreaDamageTrigger>();
            var burn = substance.GetComponent<AddKineticistBurnModifier>();

            if (trigger == null || burn == null)
            {
                Main.DebugLogAlways("error: routineExpandSubstanceInfusion couldn't process " + substance.name);
                return;
            }

            Helper.AppendAndReplace(ref trigger.AbilityList, baseAbility);
            Helper.AppendAndReplace(ref burn.AppliableTo, baseAbility);
        }

        public static bool isFeatureTouch(BlueprintFeature blast)
        {
            var variants = blast.GetComponent<AddFeatureIfHasFact>()?.Feature?.GetComponent<AbilityVariants>()?.Variants;

            if (variants == null)
            {
                Main.DebugLogAlways("isFeatureTouch error: variants is null");
                return false;
            }

            foreach (var variant in variants)
            {
                var weapon = variant.GetComponent<AbilityDeliverProjectile>()?.Weapon;

                if (weapon != null)
                {
                    if (weapon.DamageType.Type == DamageType.Physical)
                        return false;
                    return true;
                }
            }

            Main.DebugLogAlways("isFeatureTouch error: no match");
            return false;
        }

        #endregion

        // known issues:
        // - composite blasts consisting of two elements (ice) count as two attacks and will roll concealment/mirror-image individually. also true for crit and crit confirm
        // - "Miss" text doesn't show, if attack roll on consecutive hits was too high compared to initial roll
        // - "Miss" text shows up, if attack roll on consecutive hits was too low compared to initial roll
        // ! found "RuleAttackRoll ruleAttackRoll = Rulebook.CurrentContext.LastEvent<RuleAttackRoll>();" which might be useful for improvement!
        public static void createImpaleInfusion()
        {
            var earth_base = library.Get<BlueprintAbility>("e53f34fb268a7964caf1566afb82dadd");   //EarthBlastBase
            var metal_base = library.Get<BlueprintAbility>("6276881783962284ea93298c1fe54c48");   //MetalBlastBase
            var ice_base = library.Get<BlueprintAbility>("403bcf42f08ca70498432cf62abee434");   //IceBlastBase

            var earth_blast = library.Get<BlueprintFeature>("7f5f82c1108b961459c9884a0fa0f5c4");    //EarthBlastFeature
            var earth_blast_ab = library.Get<BlueprintAbility>("b28c336c10eb51c4a8ded0258d5742e1"); //EarthBlastAbility
            var metal_blast = library.Get<BlueprintFeature>("ad20bc4e586278c4996d4a81b2448998");    //MetalBlastFeature
            var metal_blast_ab = library.Get<BlueprintAbility>("665cfd3718c4f284d80538d85a2791c9");    //MetalBlastAbility
            var ice_blast = library.Get<BlueprintFeature>("a8cc34ca1a5e55a4e8aa5394efe2678e");    //IceBlastFeature
            var ice_blast_ab = library.Get<BlueprintAbility>("519e36decde7c964d87c2ffe4d3d8459");    //IceBlastAbility
            var icon = library.Get<BlueprintFeature>("2aad85320d0751340a0786de073ee3d5").Icon;  //TorrentInfusionFeature

            // create new impale feature
            // - add to selection of infusions
            infusion_impale_feature = HelperEA.CreateFeature(
                    "InfusionImpaleFeature",
                    "Impale",
                    "Element: earth\nType: form infusion\nLevel: 3\nBurn: 2\nAssociated Blasts: earth, metal, ice\n"
                        + "You extend a long, sharp spike of elemental matter along a line, impaling multiple foes. Make a single attack roll against each creature or object in a 30-foot line.",
                    Guid.i.Reg("f942f82c01c34c7da5f1131f5484e8b4"),
                    icon,
                    FeatureGroup.KineticBlastInfusion,
                    HelperEA.PrerequisiteFeaturesFromList(new BlueprintFeature[] { earth_blast, metal_blast, ice_blast }, true),
                    HelperEA.PrerequisiteClassLevel(kineticist_class, 6),
                    HelperEA.PrerequisiteFeature(elemental_focus)
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
            var earth_impale_ability = library.CopyAndAdd<BlueprintAbility>("93cc42235edc6824fa7d54b83ed4e1fe", "EarthImpaleBlastAbility", Guid.i.Reg("adcf52d3bc874d9a94250053b7ebf6e4")); // TorrentWaterBlastAbility
            earth_impale_ability.SetNameDescriptionIcon(infusion_impale_feature);
            earth_impale_ability.LocalizedSavingThrow = Strings.SavingThrowNone;
            earth_impale_ability.Parent = earth_base;

            //var damage_roll = HelperEA.CreateActionDealDamage(PhysicalDamageForm.Piercing, damage_dice, isAoE: false, halfIfSaved: false, IgnoreCritical: false);
            var earth_damage_roll = ContextActionDealDamage2.CreateNew(PhysicalDamageForm.Piercing, physical_dice, isAoE: true, halfIfSaved: false, IgnoreCritical: false);
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
                a.Projectiles = library.Get<BlueprintProjectile>("5d66a6c3cac5124469b2d0474e53ecab").ObjToArray();  // Kinetic_EarthBlastLine00: 5d66a6c3cac5124469b2d0474e53ecab
            });

            earth_impale_ability.ReplaceComponent<AbilityKineticist>(a =>
            {
                a.InfusionBurnCost = 2;
                a.BlastBurnCost = 0;
                a.Amount = 1;
                a.CachedDamageInfo = earth_blast_ab.GetComponent<AbilityKineticist>().CachedDamageInfo;
            });

            //earth_impale_ability.RemoveComponents<SpellDescriptorComponent>();
            earth_impale_ability.RemoveComponents<AbilitySpawnFx>(); earth_impale_ability.AddComponents(earth_blast_ab.GetComponents<AbilitySpawnFx>().ToArray());
            earth_impale_ability.ReplaceComponent<AbilityCasterHasFacts>(a => a.Facts = infusion_impale_feature.ObjToArray());
            earth_impale_ability.ReplaceComponent<AbilityShowIfCasterHasFact>(a => a.UnitFact = infusion_impale_feature);
            #endregion


            #region create impale ability - metal
            var metal_impale_ability = library.CopyAndAdd<BlueprintAbility>("82db79a0b4e91dc4ea2938192e6fc7af", "MetalImpaleBlastAbility", Guid.i.Reg("c25c7c8f05284317984ddcbcba69f53c"));  //TorrentSandstormBlastAbility
            metal_impale_ability.SetNameDescriptionIcon(infusion_impale_feature);
            metal_impale_ability.LocalizedSavingThrow = Strings.SavingThrowNone;
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
            metal_impale_ability.ReplaceComponent<AbilityCasterHasFacts>(a => a.Facts = infusion_impale_feature.ObjToArray());
            metal_impale_ability.ReplaceComponent<AbilityShowIfCasterHasFact>(a => a.UnitFact = infusion_impale_feature);
            #endregion


            #region create impale ability - ice
            var ice_impale_ability = library.CopyAndAdd<BlueprintAbility>("d02fba9ae78f12642b4111a4bbbdc023", "IceImpaleBlastAbility", Guid.i.Reg("50486077f898441dba7c2922a3d251ed"));  //TorrentBlizzardBlastAbility
            ice_impale_ability.SetNameDescriptionIcon(infusion_impale_feature);
            ice_impale_ability.LocalizedSavingThrow = Strings.SavingThrowNone;
            ice_impale_ability.Parent = ice_base;

            //var damage_roll = HelperEA.CreateActionDealDamage(PhysicalDamageForm.Piercing, damage_dice, isAoE: false, halfIfSaved: false, IgnoreCritical: false);
            var ice_damage_roll = ContextActionDealDamage2.CreateNew(PhysicalDamageForm.Piercing, physical_dice, isAoE: true, halfIfSaved: false, IgnoreCritical: false);
            ice_damage_roll.Half = false;
            ice_damage_roll.WeaponOverride = new ItemEntityWeapon(weapon_blast_physical);
            var ice_damage_roll2 = ContextActionDealDamage2.CreateNew(DamageEnergyType.Cold, physical_dice, isAoE: true, halfIfSaved: false, IgnoreCritical: false);
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
            ice_impale_ability.ReplaceComponent<AbilityCasterHasFacts>(a => a.Facts = infusion_impale_feature.ObjToArray());
            ice_impale_ability.ReplaceComponent<AbilityShowIfCasterHasFact>(a => a.UnitFact = infusion_impale_feature);
            #endregion

            HelperEA.AddToAbilityVariants(earth_base, earth_impale_ability);
            HelperEA.AddToAbilityVariants(metal_base, metal_impale_ability);
            HelperEA.AddToAbilityVariants(ice_base, ice_impale_ability);
        }

        public static void extendSprayInfusion(bool enabled = true)
        {
            // references from base game
            var cold_base = library.Get<BlueprintAbility>("7980e876b0749fc47ac49b9552e259c1");   //ColdBlastBase
            var cold_blast = library.Get<BlueprintFeature>("ce625487d909b154c9305e60e4fc7d60"); //ColdBlastFeature
            var cold_blast_ab = library.Get<BlueprintAbility>("f6d32ecd20ebacb4e964e2ece1c70826"); //ColdBlastAbility

            // edits to base game
            // - rename spray infusion to reflect new element
            // - add cold as valid prerequisite
            var spray_infusion_feature = library.Get<BlueprintFeature>("b5852e8287f12d34ca6f84fcc7019f07");
            if (enabled)
            {
                spray_infusion_feature.SetNameDescriptionIcon("Spray", "Element: water\nType: form infusion\nLevel: 4\nBurn: 3\nAssociated Blasts: charged water, water, cold\n"
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
            var cold_spray_ability = library.CopyAndAdd<BlueprintAbility>("a240a6d61e1aee040bf7d132bfe1dc07", "SprayColdBlastAbility", Guid.i.Reg("a8a06a9a236b4f6bbfe55c25d65067f3")); // FanOfFlamesFireBlastAbility
            cold_spray_ability.SetNameDescriptionIcon(spray_infusion_feature);
            cold_spray_ability.ResourceAssetIds = cold_blast_ab.ResourceAssetIds;
            cold_spray_ability.Parent = cold_base;

            cold_spray_ability.ReplaceComponent<AbilityEffectRunAction>(a =>
            {
                a.SavingThrowType = SavingThrowType.Reflex;
                a.Actions = cold_blast_ab.GetComponent<AbilityEffectRunAction>().Actions;
                a.Actions.Actions[0] = HelperEA.CreateActionDealDamage(DamageEnergyType.Cold, energy_dice, isAoE: true, halfIfSaved: true, IgnoreCritical: true);
            });

            cold_spray_ability.ReplaceComponent<AbilityDeliverProjectile>(a =>
            {
                a.Type = AbilityProjectileType.Cone;
                a.Length = new Feet(30f);
                a.LineWidth = new Feet(5f);
                a.NeedAttackRoll = true;
                a.Weapon = weapon_blast_energy;
                a.Projectiles = library.Get<BlueprintProjectile>("c202b61bf074a7442bf335b27721853f").ObjToArray();  //ColdCone30Feet00
            });

            cold_spray_ability.ReplaceComponent<AbilityKineticist>(a =>
            {
                a.InfusionBurnCost = 3;
                a.BlastBurnCost = 0;
                a.Amount = 1;
                a.CachedDamageInfo = cold_blast_ab.GetComponent<AbilityKineticist>().CachedDamageInfo;
            });

            cold_spray_ability.RemoveComponents<AbilityExecuteActionOnCast>();
            cold_spray_ability.RemoveComponents<AbilitySpawnFx>(); cold_spray_ability.AddComponents(cold_blast_ab.GetComponents<AbilitySpawnFx>().ToArray());
            cold_spray_ability.ReplaceComponent<SpellDescriptorComponent>(HelperEA.CreateSpellDescriptor(SpellDescriptor.Cold));
            cold_spray_ability.ReplaceComponent<AbilityCasterHasFacts>(a => a.Facts = spray_infusion_feature.ObjToArray());
            cold_spray_ability.ReplaceComponent<AbilityShowIfCasterHasFact>(a => a.UnitFact = spray_infusion_feature);

            if (enabled)
                Helper.AppendAndReplace(ref cold_base.GetComponent<AbilityVariants>().Variants, cold_spray_ability);
        }

        public static void createExtraWildTalentFeat(bool enabled = true)
        {
            extra_wild_talent_selection = HelperEA.CreateFeatureSelection(
                "ExtraWildTalentFeat",
                "Extra Wild Talent",
                "You gain a wild talent for which you meet the prerequisites. You can select an infusion or a non-infusion wild talent, but not a blast or defense wild talent.\nSpecial: You can take this feat multiple times. Each time, you must choose a different wild talent.",
                Guid.i.Reg("3714a8b82ec048e9bef62fa1cfc6c105"),
                library.Get<BlueprintFeature>("42f96fc8d6c80784194262e51b0a1d25").Icon, //ExtraArcanePool.Icon
                FeatureGroup.Feat,
                HelperEA.PrerequisiteClassLevel(kineticist_class, 1, true)
            );

            extra_wild_talent_selection.AllFeatures = Helper.Append(infusion_selection.AllFeatures,     //InfusionSelection
                                                                    wildtalent_selection.AllFeatures);  //+WildTalentSelection

            BlueprintFeature extra_wild_talent_feat = extra_wild_talent_selection;
            extra_wild_talent_feat.Ranks = 10;
            extra_wild_talent_feat.Groups = new FeatureGroup[] { FeatureGroup.Feat };

            if (enabled)
                HelperEA.AddFeats(library, extra_wild_talent_feat);
        }

        public static void createPreciseBlastTalent(bool enabled = true)
        {
            if (!Main.COTWpresent)
                enabled = false;

            var metamagic_comp = Helper.Create<AutoMetamagic>();
            HarmonyLib.AccessTools.Field(typeof(AutoMetamagic), "m_AllowedAbilities").SetValue(metamagic_comp, 2); //enum AllowedType.KineticistBlast
            metamagic_comp.Metamagic = enabled ? (Metamagic)CallOfTheWild.MetamagicFeats.MetamagicExtender.Selective : (Metamagic)0;
            metamagic_comp.Abilities = all_base;

            var precise_blast_feature = ScriptableObject.CreateInstance<BlueprintFeature>();
            precise_blast_feature.name = "PreciseBlast";
            precise_blast_feature.SetNameDescriptionIcon("Precise Blast", "You have fine control over your kinetic blast. Your allies are excluded from the effects of your blasts with a duration of instantaneous.\nDoes not affect Wall, Deadly Earth, Cloud, or other blasts that have a duration other than instantaneous.",
                                                         Main.COTWpresent ? CallOfTheWild.MetamagicFeats.selective_metamagic.Icon : null);
            precise_blast_feature.Groups = FeatureGroup.KineticWildTalent.ObjToArray();
            precise_blast_feature.Ranks = 1;
            precise_blast_feature.IsClassFeature = true;
            precise_blast_feature.SetComponents(metamagic_comp, HelperEA.PrerequisiteNoFeature(precise_blast_feature));
            library.AddAsset(precise_blast_feature, Guid.i.Reg("5beb96c3591a4506bf65e2b4e5aff883"));

            if (enabled)
                Helper.AppendAndReplace(ref wildtalent_selection.AllFeatures, precise_blast_feature);
        }

        // known issue:
        // - gathering long consumes the remaining move range (cannot fix)
        // - gathering long works while weapon is equiped
        public static void createMobileGatheringFeat()
        {
            // --- base game stuff ---
            var buff1 = library.Get<BlueprintBuff>("e6b8b31e1f8c524458dc62e8a763cfb1");   //GatherPowerBuffI
            var buff2 = library.Get<BlueprintBuff>("3a2bfdc8bf74c5c4aafb97591f6e4282");   //GatherPowerBuffII
            var buff3 = library.Get<BlueprintBuff>("82eb0c274eddd8849bb89a8e6dbc65f8");   //GatherPowerBuffIII
            var gather_original_ab = library.Get<BlueprintAbility>("6dcbffb8012ba2a4cb4ac374a33e2d9a");    //GatherPower
            // ---------------

            Access.m_Icon(buff1) = gather_original_ab.Icon;
            Access.m_DisplayNameStr(buff1, buff1.Name + " Lv1");

            Access.m_Icon(buff2) = gather_original_ab.Icon;
            Access.m_DisplayNameStr(buff2, buff2.Name + " Lv2");

            Access.m_Icon(buff3) = gather_original_ab.Icon;
            Access.m_DisplayNameStr(buff3, buff3.Name + " Lv3");

            // new buff that halves movement speed, disallows normal gathering
            mobile_debuff = ScriptableObject.CreateInstance<BlueprintBuff>();
            mobile_debuff.name = "MobileGatheringDebuff";
            mobile_debuff.SetNameDescriptionIcon("Mobile Gathering Debuff", "Your movement speed is halved after gathering power.", Helper.Image2Sprite.Create("GatherMobileHigh.png"));
            mobile_debuff.IsClassFeature = true;
            mobile_debuff.SetComponents(Helper.Create<TurnBasedBuffMovementSpeed>(a => a.Multiplier = 0.5f));// HelperEA.CreateAddCondition(UnitCondition.Slowed));
            library.AddAsset(mobile_debuff, Guid.i.Reg("ffd79fee05bf4e6dad7156e895f3cf27"));
            var apply_debuff = Helper.CreateActionApplyBuff(mobile_debuff, 1);
            var can_gather = Helper.CreateAbilityRequirementHasBuffTimed(CompareType.LessOrEqual, 1.Rounds().Seconds, buff1, buff2, buff3);

            // cannot use usual gathering after used mobile gathering
            gather_original_ab.AddComponent(Helper.CreateAbilityRequirementHasBuffs(true, mobile_debuff));

            // ability as free action that applies buff and 1 level of gatherpower
            // - is free action
            // - increases gather power by 1 level, similiar to GatherPower:6dcbffb8012ba2a4cb4ac374a33e2d9a
            // - applies debuff
            // - get same restriction as usual gathering
            var mobile_gathering_short_ab = HelperEA.CreateAbility(
                "MobileGatheringShort",
                "Mobile Gathering (Move Action)",
                "You may move up to half your normal speed while gathering power.",
                Guid.i.Reg("a482da35c21a4a0e801849610e03df87"),
                Helper.Image2Sprite.Create("GatherMobileLow.png"),
                AbilityType.Special,
                UnitCommand.CommandType.Move,
                AbilityRange.Personal,
                "",
                ""
            );
            mobile_gathering_short_ab.CanTargetSelf = true;
            mobile_gathering_short_ab.Animation = UnitAnimationActionCastSpell.CastAnimationStyle.Self;//UnitAnimationActionCastSpell.CastAnimationStyle.Kineticist;
            mobile_gathering_short_ab.HasFastAnimation = true;
            var three2three = HelperEA.CreateConditional(HelperEA.CreateConditionHasBuff(buff3), Helper.CreateActionApplyBuff(buff3, 2));
            var two2three = HelperEA.CreateConditional(HelperEA.CreateConditionHasBuff(buff2).ObjToArray(), new GameAction[] { Helper.CreateActionRemoveBuff(buff2), Helper.CreateActionApplyBuff(buff3, 2) });
            var one2two = HelperEA.CreateConditional(HelperEA.CreateConditionHasBuff(buff1).ObjToArray(), new GameAction[] { Helper.CreateActionRemoveBuff(buff1), Helper.CreateActionApplyBuff(buff2, 2) });
            var zero2one = HelperEA.CreateConditional(Helper.CreateConditionHasNoBuff(buff1, buff2, buff3), new GameAction[] { Helper.CreateActionApplyBuff(buff1, 2) });
            //var hasMoveAction = Helper.CreateRequirementActionAvailable(false, ActionType.Move);
            var regain_halfmove = Helper.Create<ContextActionUndoAction>(a => a.Command = UnitCommand.CommandType.Move);
            //mobile_gathering_short_ab.SetComponents(can_gather, hasMoveAction, Helper.CreateAbilityEffectRunAction(0, apply_debuff, three2three, two2three, one2two, zero2one));
            mobile_gathering_short_ab.SetComponents(can_gather, Helper.CreateAbilityEffectRunAction(0, regain_halfmove, apply_debuff, three2three, two2three, one2two, zero2one));


            // same as above but standard action and 2 levels of gatherpower
            var mobile_gathering_long_ab = HelperEA.CreateAbility(
                "MobileGatheringLong",
                "Mobile Gathering (Full Round)",
                "You may move up to half your normal speed while gathering power.\nNote: Use half your speed before using this.",
                Guid.i.Reg("e7cd3a8200f04c8fae099d5d2f4afa0b"),
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
            var one2three = HelperEA.CreateConditional(HelperEA.CreateConditionHasBuff(buff1).ObjToArray(), new GameAction[] { Helper.CreateActionRemoveBuff(buff1), Helper.CreateActionApplyBuff(buff3, 2) });
            var zero2two = HelperEA.CreateConditional(Helper.CreateConditionHasNoBuff(buff1, buff2, buff3), new GameAction[] { Helper.CreateActionApplyBuff(buff2, 2) });
            var hasMoveAction = Helper.CreateAbilityRequirementActionAvailable(false, ActionType.Move, 4.5f);
            var lose_halfmove = Helper.Create<ContextActionUndoAction>(a => { a.Command = UnitCommand.CommandType.Move; a.Amount = -1.5f; });
            mobile_gathering_long_ab.SetComponents(can_gather, hasMoveAction, Helper.CreateAbilityEffectRunAction(0, lose_halfmove, apply_debuff, three2three, two2three, one2three, zero2two));

            var mobile_gathering_feat = HelperEA.CreateFeature(
                "MobileGatheringFeat",
                "Mobile Gathering",
                "While gathering power, you can move up to half your normal speed. This movement provokes attacks of opportunity as normal.",
                Guid.i.Reg("60edffeba6d74e0f831c00692e5fc621"),
                mobile_debuff.Icon,
                FeatureGroup.Feat,
                HelperEA.PrerequisiteClassLevel(kineticist_class, 7, true),
                HelperEA.CreateAddFacts(mobile_gathering_short_ab, mobile_gathering_long_ab)
            );
            mobile_gathering_feat.Ranks = 1;
            mobile_gathering_feat.IsClassFeature = true;
            HelperEA.AddFeats(library, mobile_gathering_feat);
        }

        // known issue:
        // - sleet storm still prevents bow attacks, however I don't think any level 18 kineticist will want to use a bow ever
        // - maybe should make you immune against cloud infusions
        public static void createHurricaneQueen()
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
            hurricane_queen_feat = HelperEA.CreateFeature(
                "HurricaneQueenFeature",
                "Hurricane Queen",
                "You are one with the hurricane. Your enveloping winds defense wild talent has an additional 25% chance of deflecting non-magical ranged attacks, and your total deflection chance can exceed the usual cap of 75%. All wind and weather (including creatures using the whirlwind monster ability) don't affect you; for example, you could shoot arrows directly through a tornado without penalty.",
                Guid.i.Reg("5d5cbd74010e41f089fe4b96fd2fc50e"),
                library.Get<BlueprintFeature>("f2fa7541f18b8af4896fbaf9f2a21dfe").Icon, //CycloneInfusion
                FeatureGroup.KineticWildTalent,
                HelperEA.PrerequisiteClassLevel(kineticist_class, 18),
                HelperEA.PrerequisiteFeature(winds_feature),
                HelperEA.CreateAddFacts(winds_stack, winds_stack, winds_stack, winds_stack, winds_stack),
                //Helper.CreateSpecificBuffImmunity(weather_rain),
                //Helper.CreateSpecificBuffImmunity(weather_storm),
                Helper.CreateSpecificBuffImmunity(sleet_storm)
            );

            try
            {
                hurricane_queen_feat.AddComponents(
                    Helper.Create<CallOfTheWild.ConcealementMechanics.IgnoreFogConcelement>(),
                    HelperEA.CreateAddFact(CallOfTheWild.NewSpells.immunity_to_wind));
            }
            catch (System.Exception)
            {
            }

            Helper.AppendAndReplace(ref wildtalent_selection.AllFeatures, hurricane_queen_feat);
        }

        public static void createMindShield(bool enabled = true)
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
            custom_daze.AddComponent(buff_dominateperson.ComponentsArray[2]);

            // - simply gives counter bonus to skill penalties
            // - also replaces most common will based debuffs with daze (idea being that daze is better than being mind-controlled or running in circles)
            // - also give new saving throw like DominatePersonBuff
            BlueprintBuff mind_shield_buff = HelperEA.CreateBuff(
                "MindShieldBuff",
                "Mind Shield",
                "You reduce penalties of Mind Burn to Wisdom-based skill checks by 1. Additionally if you fail a Will saving throw against a mind-affecting spell, you are instead dazed for the duration of the spell, but you may re-try the saving throw each round.",
                Guid.i.Reg("0ba3718f568a4c9097307a7af57c8f88"),
                null,
                null
            );
            mind_shield_buff.m_Flags(HiddenInUi: true);
            var b_comp1 = HelperEA.CreateAddContextStatBonus(StatType.SkillPerception, ModifierDescriptor.UntypedStackable, ContextValueType.CasterCustomProperty, multiplier: 1);
            b_comp1.Value.CustomProperty = burn_number;
            var b_comp2 = HelperEA.CreateAddContextStatBonus(StatType.SkillLoreNature, ModifierDescriptor.UntypedStackable, ContextValueType.CasterCustomProperty, multiplier: 1);
            b_comp2.Value.CustomProperty = burn_number;
            var b_comp3 = HelperEA.CreateAddContextStatBonus(StatType.SkillLoreReligion, ModifierDescriptor.UntypedStackable, ContextValueType.CasterCustomProperty, multiplier: 1);
            b_comp3.Value.CustomProperty = burn_number;
            mind_shield_buff.SetComponents(b_comp1, b_comp2, b_comp3,
                Helper.CreateBuffSubstitutionOnApply(buff_confusion, custom_daze),
                Helper.CreateBuffSubstitutionOnApply(buff_cowering, custom_daze),
                Helper.CreateBuffSubstitutionOnApply(buff_frightened, custom_daze),
                Helper.CreateBuffSubstitutionOnApply(buff_dominateperson, custom_daze)
            );

            var mind_shield_feature = HelperEA.CreateFeature(
                "MindShieldFeature",
                mind_shield_buff.Name,
                mind_shield_buff.Description,
                Guid.i.Reg("eeab6701ee7447d3b62996a62e315bd7"),
                library.Get<BlueprintFeature>("ed01d50910ae67b4dadc050f16d93bdf").Icon, //KineticRestorationFeature
                FeatureGroup.KineticWildTalent
            );
            var f_comp1 = psychokineticist.CreatePrerequisite(6);
            var f_comp2 = Helper.CreateAddKineticistBurnValueChangedTrigger(Helper.CreateActionApplyBuff(mind_shield_buff, permanent: true));
            var f_comp3 = HelperEA.PrerequisiteNoFeature(mind_shield_feature);
            var f_comp4 = HelperEA.CreateAddFact(mind_shield_buff);
            mind_shield_feature.SetComponents(f_comp1, f_comp2, f_comp3, f_comp4);

            if (enabled)
                Helper.AppendAndReplace(ref wildtalent_selection.AllFeatures, mind_shield_feature);
        }

        public static void fixExpandElement()
        {
            var air1 = library.Get<BlueprintProgression>("2bd0d44953a536f489082534c48f8e31");//ElementalFocusAir
            var earth1 = library.Get<BlueprintProgression>("c6816ad80a3df9c4ea7d3b012b06bacd");//ElementalFocusEarth
            var fire1 = library.Get<BlueprintProgression>("3d8d3d6678b901444a07984294a1bc24");//ElementalFocusFire
            var water1 = library.Get<BlueprintProgression>("7ab8947ce2e19c44a9edcf5fd1466686");//ElementalFocusWater

            var air2 = library.Get<BlueprintProgression>("659c39542b728c04b83e969c834782a9");//SecondaryElementAir
            var earth2 = library.Get<BlueprintProgression>("956b65effbf37e5419c13100ab4385a3");//SecondaryElementEarth
            var fire2 = library.Get<BlueprintProgression>("caa7edca64af1914d9e14785beb6a143");//SecondaryElementFire
            var water2 = library.Get<BlueprintProgression>("faa5f1233600d864fa998bc0afe351ab");//SecondaryElementWater

            var air3 = library.Get<BlueprintProgression>("651570c873e22b84f893f146ce2de502");//ThirdElementAir
            var earth3 = library.Get<BlueprintProgression>("c43d9c2d23e56fb428a4eb60da9ba1cb");//ThirdElementEarth
            var fire3 = library.Get<BlueprintProgression>("56e2fc3abed8f2247a621ac37e75f303");//ThirdElementFire
            var water3 = library.Get<BlueprintProgression>("86eff374d040404438ad97fedd7218bc");//ThirdElementWater

            air3.RemoveComponents<PrerequisiteNoFeature>();
            earth3.RemoveComponents<PrerequisiteNoFeature>();
            fire3.RemoveComponents<PrerequisiteNoFeature>();
            water3.RemoveComponents<PrerequisiteNoFeature>();

            var mastery_1_2or3 = HelperEA.CreateFeatureSelection("KineticistMastery12or3", "Element Mastery", "Kineticist focusing on their primary element gain a bonus wild talent.", Guid.i.Reg("d112af8b633f42fab942a9aeab605000"), null, FeatureGroup.None);
            mastery_1_2or3.SetComponents(
                Helper.CreatePrerequisiteFeaturesFromList(true, 2, air1, air2, air3),
                Helper.CreatePrerequisiteFeaturesFromList(true, 2, earth1, earth2, earth3),
                Helper.CreatePrerequisiteFeaturesFromList(true, 2, fire1, fire2, fire3),
                Helper.CreatePrerequisiteFeaturesFromList(true, 2, water1, water2, water3),
                HelperEA.PrerequisiteNoFeature(mastery_1_2or3, false)
            );
            mastery_1_2or3.IsClassFeature = true;
            mastery_1_2or3.AllFeatures = extra_wild_talent_selection.AllFeatures;

            var mastery_1_2_3 = HelperEA.CreateFeatureSelection("KineticistMastery123", "Greater Element Mastery", mastery_1_2or3.Description, Guid.i.Reg("e2a98be90b38454a8428b78ccfe56649"), null, FeatureGroup.None,
                Helper.CreatePrerequisiteFeaturesFromList(true, 3, air1, air2, air3),
                Helper.CreatePrerequisiteFeaturesFromList(true, 3, earth1, earth2, earth3),
                Helper.CreatePrerequisiteFeaturesFromList(true, 3, fire1, fire2, fire3),
                Helper.CreatePrerequisiteFeaturesFromList(true, 3, water1, water2, water3),
                Helper.Create<BonusToSpellAbilityByName>(a =>
                {
                    a.Category = WeaponCategory.KineticBlast;
                    a.CategoryStr = "BlastAbility";
                    a.AttackBonus = 1;
                    a.DamageBonus = 1;
                    a.DC_Bonus = 1;
                    a.CL_Bonus = 1;
                })
            );
            mastery_1_2_3.IsClassFeature = true;
            mastery_1_2_3.AllFeatures = extra_wild_talent_selection.AllFeatures;

            var mastery_selection = HelperEA.CreateFeatureSelection("KineticistMasterySelection", "Element Mastery", mastery_1_2or3.Description, Guid.i.Reg("50cbc6cf64a94ae7b2f5a4dc45170efa"), null, FeatureGroup.None);
            mastery_selection.SetComponents(Helper.Create<PrerequisiteSelectionPossible>(a => a.ThisFeature = mastery_selection));
            mastery_selection.AllFeatures = new BlueprintFeature[] { mastery_1_2or3, mastery_1_2_3 };

            if (!Main.COTWpresent)
            {
                kineticist_progression.LevelEntries.FirstOrDefault(x => x.Level == 7)?.Features.Add(mastery_selection);
                kineticist_progression.LevelEntries.FirstOrDefault(x => x.Level == 15)?.Features.Add(mastery_selection);
            }
        }

        // work on hold! does not work as intended; composite blasts are not granted, simple blasts are not granted, or other issues
        public static void createExpandElementalFocus()
        {
            var expand_element_selection = HelperEA.CreateFeatureSelection(
                "ExpandElementalFocus",
                "Expand Elemental Focus",
                "You learn to use another element. You can select any element and gain one of that element’s simple blast wild talents that you do not already possess.",
                Guid.i.Reg("0e2aa6b75b334b019fbfa7375ecaca85"),
                elemental_focus.Icon,   //ElementalFocusSelection.Icon
                FeatureGroup.Feat,
                HelperEA.PrerequisiteClassLevel(kineticist_class, 1, true)
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
            HelperEA.AddFeats(library, expand_element_feat);
        }

        public static void createFlight()
        {
            var icon = Main.COTWpresent ? CallOfTheWild.NewSpells.fly.Icon : null;
            var buff = Main.COTWpresent ? CallOfTheWild.NewSpells.fly_buff : HelperEA.CreateBuff(
                "FlyBuff",
                "",
                "",
                "",
                null,
                null,
                HelperEA.CreateAddFact(library.Get<BlueprintFeature>("70cffb448c132fa409e49156d013b175")),  //Airborne
                HelperEA.CreateAddStatBonus(StatType.Speed, 10, ModifierDescriptor.UntypedStackable)
            );

            buff.FxOnStart = Helper.Resource("1091271ddadd4a34f8d012428252dd4d");

            var air_flight_ab = HelperEA.CreateActivatableAbility("WingsOfAirActivatableAbility", "Wings of Air",
                "The air bends to your will, allowing you to soar to great heights. You are constantly under the effects of fly. If this effect is dispelled, you can call it forth again as a standard action.",
                Guid.i.Reg("92f8f7652891406b97938f57dec47734"),
                icon,
                buff,
                AbilityActivationType.Immediately,
                UnitCommand.CommandType.Free,
                null
            );
            air_flight_ab.Group = ActivatableAbilityGroup.Wings;
            air_flight_ab.ResourceAssetIds = "1091271ddadd4a34f8d012428252dd4d".ObjToArray();

            var air_flight_feat = HelperEA.CreateFeature("WingsOfAirFeature", air_flight_ab.Name, air_flight_ab.Description,
                Guid.i.Reg("3a4d9d7fc4cc4073b41c011c1e8ae528"),
                air_flight_ab.Icon,
                FeatureGroup.None,
                HelperEA.CreateAddFact(air_flight_ab),
                HelperEA.PrerequisiteClassLevel(kineticist_class, 6),
                prerequisite_air
            );


            var fire_flight_ab = HelperEA.CreateActivatableAbility("FlameJetGreaterActivatableAbility", "Greater Flame Jet",
                "You shoot a burst of flame behind you as a move action, propelling you up to into the air. You also emanate a mild jet of flame, allowing you to hover without spending an action.",
                Guid.i.Reg("4eb438ef2bf64e1d82edcba3689e8d20"),
                icon,
                buff,
                AbilityActivationType.Immediately,
                UnitCommand.CommandType.Free,
                null
            );
            fire_flight_ab.Group = ActivatableAbilityGroup.Wings;

            var fire_flight_feat = HelperEA.CreateFeature("FlameJetGreaterFeature", fire_flight_ab.Name, fire_flight_ab.Description,
                Guid.i.Reg("f8c4287f5c8d4681a379fc974c223974"),
                fire_flight_ab.Icon,
                FeatureGroup.None,
                HelperEA.CreateAddFact(fire_flight_ab),
                HelperEA.PrerequisiteClassLevel(kineticist_class, 10),
                prerequisite_fire
            );


            var water_flight_ab = HelperEA.CreateActivatableAbility("IcePathActivatableAbility", "Ice Path",
                "You freeze water vapor in the air, allowing you to travel above the ground as air walk by walking along the ice, and leaving a path of ice behind you that lasts for 1 round before it melts.",
                Guid.i.Reg("d2deb8ad5507458b91c160ad8e6e186a"),
                icon,
                buff,
                AbilityActivationType.Immediately,
                UnitCommand.CommandType.Free,
                null
            );
            water_flight_ab.Group = ActivatableAbilityGroup.Wings;

            var water_flight_feat = HelperEA.CreateFeature("IcePathFeature", water_flight_ab.Name, water_flight_ab.Description,
                Guid.i.Reg("901fc305b5b840f0913a78d749ca1e9f"),
                water_flight_ab.Icon,
                FeatureGroup.None,
                HelperEA.CreateAddFact(water_flight_ab),
                HelperEA.PrerequisiteClassLevel(kineticist_class, 12),
                prerequisite_water
            );

            if (!Main.COTWpresent)
                Helper.AppendAndReplace(ref wildtalent_selection.AllFeatures, air_flight_feat, fire_flight_feat, water_flight_feat);
        }

        public static void createShiftEarth()
        {
            var spiked_pit = library.Get<BlueprintAbility>("46097f610219ac445b4d6403fc596b9f");
            var pit_effect = library.Get<BlueprintAbilityAreaEffect>("beccc33f543b1f8469c018982c23ac06").GetComponent<AreaEffectPit>();

            //fix spiked pit not dealing damage every round
            pit_effect.EveryRoundAction = Helper.CreateActionList(HelperEA.CreateActionDealDamage(PhysicalDamageForm.Piercing, HelperEA.CreateContextDiceValue(DiceType.D6, 1), false, false, true));

            var shifting_earth_feat = HelperEA.CreateFeature("ShiftEarthFeature", "Shift Earth",
                "Element: earth\nType: utility\nBurn: 0\nSaving Throw: Reflex negates\nSpell Resistance: no\nYou can move earth and stone to create one spiked pit within 30 feet of you.\nOriginal text: As a standard action, you can push or pull a 5-foot cube of earth or unworked stone within 30 feet, moving the cube 5 feet in any direction. You can create raised platforms, stairs up a cliff, holes, or other useful features. This doesn’t cause the earth to float in the air, although in areas with plenty of earth, you can move a cube upward, creating a short pillar. If you move the earth beneath a creature’s feet, it can attempt a DC 20 Reflex save to leap elsewhere and avoid moving along with the earth.",
                Guid.i.Reg("fd7a363d1fa54c1ba25a5817acde2c1f"),
                spiked_pit.Icon,
                FeatureGroup.None,
                HelperEA.PrerequisiteClassLevel(kineticist_class, 8),
                prerequisite_earth
            );

            var shift_earth_ab = HelperEA.CreateAbility("ShiftEarthAbility", shifting_earth_feat.Name, shifting_earth_feat.Description,
                Guid.i.Reg("4dbe165d5a074579b78ba285b1a98770"),
                spiked_pit.Icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Close,
                spiked_pit.LocalizedDuration,
                spiked_pit.LocalizedSavingThrow,
                spiked_pit.GetComponent<AbilityEffectRunAction>(),
                spiked_pit.GetComponent<ContextRankConfig>(),
                spiked_pit.GetComponent<AbilityAoERadius>(),
                //Helper.Create<UniqueAreaEffect>(a => a.Feature = shifting_earth_feat),
                Helper.Create<AbilityKineticist>(a =>
                {
                    a.CachedDamageSource = (spiked_pit.GetComponent<AbilityEffectRunAction>().Actions.Actions[0] as ContextActionSpawnAreaEffect)?.AreaEffect;
                }),
                Helper.Create<ContextCalculateAbilityParamsBasedOnClass>(a =>
                {
                    a.UseKineticistMainStat = true;
                    a.StatType = StatType.Charisma;
                    a.CharacterClass = kineticist_class;
                })
            );
            shift_earth_ab.SetMiscAbilityParametersRangedDirectional(animation_style: CastAnimationStyle.CastActionOmni);

            shifting_earth_feat.AddComponent(HelperEA.CreateAddFact(shift_earth_ab));
            Helper.AppendAndReplace(ref wildtalent_selection.AllFeatures, shifting_earth_feat);
        }

        public static void fixWallInfusion()
        {
            foreach (var variants in blast_variants.Values)
            {
                try
                {
                    BlueprintAbility wall = variants.FirstOrDefault(a => a.name.StartsWith("Wall", StringComparison.Ordinal));

                    //var area = (wall.GetComponent<AbilityEffectRunAction>().Actions.Actions[0] as ContextActionSpawnAreaEffect).AreaEffect.GetComponent<AbilityAreaEffectRunAction>();
                    //area.Round = area.UnitEnter;

                    if (wall != null)
                        Helper.RecursiveAction<ContextActionSpawnAreaEffect>(wall.GetComponent<AbilityEffectRunAction>().Actions.Actions, a =>
                        {
                            var area = a.AreaEffect.GetComponent<AbilityAreaEffectRunAction>();
                            area.Round = area.UnitEnter;
                        });
                }
                catch (Exception e)
                {
                    Main.DebugLogAlways("fixWallInfusion failed: " + e.ToString());
                }
            }
        }

        public static void createMobileBlast()
        {

            string description = "Element: universal\nType: form infusion\nLevel 3\nBurn 2\nAssociated Blasts: any\nSaving Throw Reflex negates\nYou send an elemental mass, energy ball, or object into a particular square within 30 feet, dealing 1/4 the normal amount of damage (or half the normal amount of damage, for an energy blast) to all creatures in that square. The mobile blast lasts until you dismiss it. Each round on your turn as a move action, you can move the mobile blast to another square within range; either way, it continues to deal damage to all creatures in its final square each round if they fail their saving throws.\nYou can have only a single mobile blast in existence at a time. It lasts for a maximum number of rounds equal to your Constitution modifier. A mobile blast is always extremely loud and visible. The saving throw DC is Dexterity-based.";
            var icon = library.Get<BlueprintAbility>("fc432e7a63f5a3545a93118af13bcb89").Icon;//createAggressiveThunderCloud

            // add new mobile blast feature
            // - grants MobileBlastMoveAbility
            // - visibility checked by base blast
            var mobileblast_feature = HelperEA.CreateFeature("MobileBlastFeature",
                "Mobile Blast",
                description,
                Guid.i.Reg("18765cbb34684878ac3ae9245e4049e4"),
                icon,
                FeatureGroup.None
            );

            // add new ability that let's you move the area
            // - does nothing when no mobile blast active
            var move_ability = HelperEA.CreateAbility("MobileBlastMoveAbility",
                "Mobile Blast",
                "Use this ability to move Mobile Blast's area.",
                Guid.i.Reg("dbf892456edb454a823d252c1e47927d"),
                icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Move,
                AbilityRange.Close,
                "",
                "",
                HelperEA.CreateRunActions(Helper.Create<ContextActionMoveUniqueArea>(a => a.Feature = mobileblast_feature)),
                HelperEA.CreateAbilityAoERadius(2.Feet(), TargetType.Any)
            );
            move_ability.SetMiscAbilityParametersRangedDirectional();
            mobileblast_feature.SetComponents(HelperEA.CreateAddFact(move_ability),
                HelperEA.PrerequisiteClassLevel(kineticist_class, 6));

            // - shared component that makes sure only one effect is active at a time
            var area_unique = Helper.Create<UniqueAreaEffect>(a => a.Feature = mobileblast_feature);
            var fx = Helper.Resource("cfacbb7d39eaf624382c58bad8ba2df1"); //will o wisp fx

            foreach (var e in blast_variants)
            {
                BlueprintAbility wall = e.Value.FirstOrDefault(a => a.name.StartsWith("Wall", StringComparison.Ordinal));

                if (wall == null)
                {
                    Main.DebugLogAlways(e.Key + " has no Wall variant!");
                    continue;
                }

                // clones wall spawn effect
                // - modifies size and shape
                BlueprintAbilityAreaEffect area_effect = null;
                var run_action = Helper.Instantiate(wall.GetComponent<AbilityEffectRunAction>());
                run_action.Actions.Actions = Helper.RecursiveReplace<ContextActionSpawnAreaEffect>(run_action.Actions.Actions, spawn_area =>
                {
                    if (area_effect == null)
                    {
                        string name = e.Key + "MobileBlastArea";
                        spawn_area.AreaEffect = library.CopyAndAdd(spawn_area.AreaEffect, name, Guid.i.Get(name));
                        spawn_area.AreaEffect.Shape = AreaEffectShape.Cylinder;
                        spawn_area.AreaEffect.Size = 2.Feet();
                        spawn_area.AreaEffect.ReplaceComponent<UniqueAreaEffect>(area_unique);
                        spawn_area.AreaEffect.Fx = fx;
                        area_effect = spawn_area.AreaEffect;
                    }
                    else
                        spawn_area.AreaEffect = area_effect;
                });
                //var spawn_area = (wall.GetComponent<AbilityEffectRunAction>().Actions.Actions[0] as ContextActionSpawnAreaEffect).CreateCopy();
                //spawn_area.AreaEffect = library.CopyAndAdd(spawn_area.AreaEffect, e.Key+"MobileBlastArea", Guid.i.Get(e.Key+"MobileBlastArea"));
                //spawn_area.AreaEffect.Shape = AreaEffectShape.Cylinder;
                //spawn_area.AreaEffect.Size = 2.Feet();
                //spawn_area.AreaEffect.ReplaceComponent<UniqueAreaEffect>(area_unique);
                //spawn_area.AreaEffect.Fx = fx;
                //var comp_spawn_area = HelperEA.CreateRunActions(spawn_area);


                // clones wall and applies new spawn effect
                // - corrects new damage source
                // - reduces burn cost from 3 to 2
                // - displays the target area (wall had no indicator)
                string name2 = e.Key + "MobileBlastAbility";
                var mobileblast_ab = library.CopyAndAdd(wall, name2, Guid.i.Get(name2));
                mobileblast_ab.SetNameDescriptionIcon("Mobile Blast", description, icon);
                mobileblast_ab.Range = AbilityRange.Close;
                mobileblast_ab.ReplaceComponent<AbilityEffectRunAction>(run_action);
                mobileblast_ab.ReplaceComponent<AbilityKineticist>(a =>
                {
                    a.CachedDamageSource = area_effect;
                    a.InfusionBurnCost = 2;
                });
                mobileblast_ab.ReplaceComponent<AbilityShowIfCasterHasFact>(a => a.UnitFact = mobileblast_feature);
                mobileblast_ab.AddComponent(HelperEA.CreateAbilityAoERadius(2.Feet(), TargetType.Any));

                // applies to variants list
                e.Value.Add(mobileblast_ab);
            }
        }

        // known issues:
        // - summon ability consumes move action immediately, if available (won't fix)
        // - when a summon dies, concentration does not deactivate automatically (won't fix)
        // - non-turn-based, bad casting timing of a second spark can cancel the first (won't fix)
        public static void createSparkofLife()
        {
            Sprite icon_ele = library.Get<BlueprintAbility>("650f8c91aaa5b114db83f541addd66d6").Icon;//ElementalBodyIAir
            Sprite icon_con = library.Get<BlueprintBuff>("456a099d26ef1e84c9401666870de095").Icon;//ResiliencyJudgmentBuffGood
            string description = "Element: universal\nType: utility\nBurn: 0\nYou breathe a semblance of life into elemental matter, which takes the form of a Medium elemental of any of your elements as if summoned by summon monster IV with a caster level equal to your kineticist level, except the elemental gains the mindless trait. Each round on your turn, you must take a move action to guide the elemental or it collapses back into its component element. By accepting 1 point of burn, you can pour a bit of your own sentience into the elemental, removing the mindless quality and allowing it to persist for 1 round per kineticist level without requiring any further actions. At 12th level, you can choose to form a Large elemental as if by summon monster V; at 14th level, you can choose to form a Huge elemental as if by summon monster VI; at 16th level, you can choose to form a greater elemental as if by summon monster VII; and at 18th level, you can choose to form an elder elemental as if by summon monster VIII.";

            // adds new ability that can be expanded
            // - has actual abilities in its sub menu
            var spark_ab = HelperEA.CreateAbility("SparkOfLifeAbility", "Spark of Life (Fumi's Codex)", description,
                Guid.i.Reg("f4f2e77c3c2841ce9ef7cd48e32fb7fc"),
                icon_ele,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Close,
                Strings.RoundsPerLevelDuration,
                ""
            );
            spark_ab.CanTargetPoint = true;
            spark_ab.CanTargetSelf = true;
            spark_ab.AvailableMetamagic = Metamagic.Quicken | Metamagic.Extend | Metamagic.Heighten | Metamagic.Reach;

            // arrays that hold the asset-ids for the new variants
            // ! improvement, save accessible
            string[] a_elements = new string[] { "Air", "Earth", "Fire", "Water" };	//blasts_byelement.Keys;
            //string[] a_newGUIDs = new string[] { "4ccf7ae25b5a4f02bbc89d6ab0db33fd", "b474f1af159742e3aa0af7b83b6b73ce", "48eb1f0409574d28a594106926efefd2", "deccb97954b44fd4a934e7dfe213b2b7", "c23912ef94b44de1ae73fe84b08b4c7e", "c8061dd89d6c4ffd87f1c23945a2148c", "e90c5c76713d4234901d6810aeb5f625", "7d7cd0cdbbf449faaaa0cdabaff2facd" };
            int[] levelthreshold = new int[] { 12, 14, 16, 18 };
            string[] a_spawns = new string[]
            {
                "676f8b7d0a170674cb6e504e0e30b4f0",	//"Medium", "Air"
			    "3764b43791a00e1468257adbca43ce9b",	//"Large", "Air"
			    "2e24256e459468743b91fbb9aa85e1ab",	//"Huge", "Air"
			    "e770cfbb96b528c4db258d7d03fe6533",	//"Greater", "Air"
			    "33bb90ffd13c87b4c8e45d920313752a",	//"Elder", "Air"

			    "812c9a0348e004242ba4e46efa91e38e",	//"Medium", "Earth"
			    "d3d9ab560534bd948b10ac00abbff083",	//"Large", "Earth"
			    "3b86a449e7264174eaccef9b8f02fe20",	//"Huge", "Earth"
			    "cda7013db24f4c547b79bfc5c617066b",	//"Greater", "Earth"
			    "6b4cb9b6116f2194192e1e7e379c48d7",	//"Elder", "Earth"

			    "a0ab0c31b1a92554291a82e598f39ba4",	//"Medium", "Fire"
			    "ba5026596b06b204eb2efed2b411c5b9",	//"Large", "Fire"
			    "640fb7efb7c916945837bbcab995267e",	//"Huge", "Fire"
			    "b0b4091bdaebb464e903857a95189dea",	//"Greater", "Fire"
			    "ea0f0bbc6e5e471428d535501b21eb26",	//"Elder", "Fire"

			    "62a3e860e6e72e6499c38bb8b2fe303e",	//"Medium", "Water"			
			    "680b5b61c80af664daec46af7644486c",	//"Large", "Water"			
			    "877c154a296ee8e45be1a00668319923",	//"Huge", "Water"			
			    "fcc939e3acf355b458ddf9617d8c6c28",	//"Greater", "Water"			
			    "3bd31a0b4d800f04a8c5b7b1a6d7061e"  //"Elder", "Water"
            };

            int groupcount = a_spawns.Length / a_elements.Length; //5
            BlueprintAbility[] sparks = new BlueprintAbility[a_elements.Length * 2];

            var master = library.Get<BlueprintAbility>("333efbf776ab61c4da53e9622751d95f").GetComponent<AbilityEffectRunAction>().Actions.Actions[0] as ContextActionSpawnMonster;  //SummonElementalElderAir

            // new pool specifically for this ability
            // - pool is shared between all creatures using this ability, but can be filtered by ID
            var link_pool = Helper.Create<BlueprintSummonPool>(a => a.name = "SummonLinkPool"); library.AddAsset(link_pool, Guid.i.Reg("3bf4ef9db31b46b296c68cb922f292aa"));

            // add two activatables and corresponding buffs
            // - losing the buff will banish excess summons randomly (CreateContextActionKillSummons will check for amount of buffs and summons)
            // - the buff will trigger concentration throws when damage is taken, failure will cause the activatable to deactivate
            var link_buff = HelperEA.CreateBuff("SummoningLinkBuff", "Concentrating on Spell", "You must concentrate to maintain the spell.", Guid.i.Reg("f71379d31c2e4165bc2ca6837311e149"), null, Contexts.NullPrefabLink);
            //link_buff.Stacking = StackingType.Stack;
            var link_buff2 = library.CopyAndAdd(link_buff, "SummoningLinkBuff2", Guid.i.Reg("0bd6390b8b6d410eb28b6effca6b188d"));

            var link_activatable = HelperEA.CreateActivatableAbility("SummoningLinkActivatable", "Concentration: Spark of Life", "Concentrating on Spark of Life is a move action. You may concentrate on up to two summons. Activates automatically, when you start concentrating. Deactivate manually, when you want to crease concentrating.",
                Guid.i.Reg("839ebda612774374a6ec05dda0a3d149"), icon_con, link_buff, AbilityActivationType.Immediately, UnitCommand.CommandType.Free, null,
                Helper.Create<ActivatableAbilityUnitCommand>(a => a.Type = UnitCommand.CommandType.Move));
            link_activatable.WeightInGroup = Patches_Activatable.ActionBar.NoManualOn;
            link_activatable.DeactivateIfOwnerUnconscious = true;
            var link_activatable2 = library.CopyAndAdd(link_activatable, "SummoningLinkActivatable2", Guid.i.Reg("7a863a9a1eec415c9552ba0d0ec5edfd"));
            link_activatable2.Buff = link_buff2;

            var link_concentration = Helper.Create<ContextActionConcentration>(a => { a.Ability = spark_ab; a.FailedAction = Helper.CreateActionList(Helper.Create<ContextActionToggleActivatable>(b => { b.Activatable = link_activatable; b.TurnOn = false; })); });
            var link_concentration2 = Helper.Create<ContextActionConcentration>(a => { a.Ability = spark_ab; a.FailedAction = Helper.CreateActionList(Helper.Create<ContextActionToggleActivatable>(b => { b.Activatable = link_activatable2; b.TurnOn = false; })); });

            var link_actionkill = Helper.CreateContextActionKillSummons(link_pool, link_buff, link_buff2);
            var link_kill = HelperEA.CreateAddFactContextActions(deactivated: link_actionkill);
            link_buff.SetComponents(link_concentration, link_kill);
            link_buff2.SetComponents(link_concentration2, link_kill);

            // - casting the summon ability will activate the first activatable, or the second if first is already active, of a random summon will be banished if both are already active
            var link_start = Helper.CreateContextActionToggleActivatable(true, link_activatable, Helper.CreateContextActionToggleActivatable(true, link_activatable2, link_actionkill));

            for (int i = 0; i < a_elements.Length; i++)
            {
                // select summon depending on caster level
                // - free summons will have hours per level instead infinite to prevent unintended perma summons
                var pool = new BlueprintUnit[groupcount - 1];
                for (int j = 0; j < pool.Length; j++)
                    pool[j] = library.Get<BlueprintUnit>(a_spawns[i * groupcount + j + 1]);
                var actionSpawn = Helper.CreateContextActionSpawnMonsterLeveled(levelthreshold, pool);
                actionSpawn.Blueprint = library.Get<BlueprintUnit>(a_spawns[i * groupcount]);    //this is the default
                actionSpawn.AfterSpawn = master.AfterSpawn;
                actionSpawn.SummonPool = link_pool;//master.SummonPool;
                actionSpawn.DurationValue = HelperEA.CreateContextDuration(Contexts.ValueRank, DurationRate.Hours);
                actionSpawn.CountValue = master.CountValue;

                var actionSpawn2 = Helper.Instantiate(actionSpawn);
                actionSpawn2.SummonPool = master.SummonPool;
                actionSpawn2.DurationValue = master.DurationValue;

                // add one ability per element to summon elemental of that element
                string name = a_elements[i] + "FreeSparkOfLifeAbility";
                sparks[i * 2] = HelperEA.CreateAbility(
                    name, "Spark of Life: " + a_elements[i] + " Elemental", description,
                    Guid.i.Get(name),//a_newGUIDs[i*2],
                    (blasts_byelement[a_elements[i]] as BlueprintAbility[])[0].Icon,
                    AbilityType.SpellLike,
                    UnitCommand.CommandType.Standard,
                    AbilityRange.Close,
                    Strings.HourPerLevelDuration,
                    "",
                    HelperEA.CreateRunActions(actionSpawn, link_start),
                    Helper.Create<ContextRankConfig>(),
                    HelperEA.CreateSpellComponent(SpellSchool.Conjuration),
                    Helper.Create<AbilityKineticist>(a => { a.WildTalentBurnCost = 0; }),
                    Helper.Create<AbilityShowIfCasterHasAnyFacts>(a => a.UnitFacts = (BlueprintAbility[])blasts_byelement[a_elements[i]])
                );
                sparks[i * 2].CanTargetPoint = true;
                sparks[i * 2].CanTargetSelf = true;
                sparks[i * 2].AvailableMetamagic = Metamagic.Quicken | Metamagic.Heighten | Metamagic.Reach;

                // add another ability, this one with burn cost but without concentration
                string name2 = a_elements[i] + "SparkOfLifeAbility";
                sparks[i * 2 + 1] = HelperEA.CreateAbility(
                    name2, "Spark of Life: " + a_elements[i] + " Elemental", description,
                    Guid.i.Get(name2),//a_newGUIDs[i*2+1],
                    (blasts_byelement[a_elements[i]] as BlueprintAbility[])[0].Icon,
                    AbilityType.SpellLike,
                    UnitCommand.CommandType.Standard,
                    AbilityRange.Close,
                    Strings.RoundsPerLevelDuration,
                    "",
                    HelperEA.CreateRunActions(actionSpawn2),
                    Helper.Create<ContextRankConfig>(),
                    HelperEA.CreateSpellComponent(SpellSchool.Conjuration),
                    Helper.Create<AbilityKineticist>(a => { a.WildTalentBurnCost = 1; }),
                    Helper.CreateAbilityShowIfCasterHasAnyFacts((BlueprintAbility[])blasts_byelement[a_elements[i]])
                );
                sparks[i * 2 + 1].CanTargetPoint = true;
                sparks[i * 2 + 1].CanTargetSelf = true;
                sparks[i * 2 + 1].AvailableMetamagic = Metamagic.Quicken | Metamagic.Extend | Metamagic.Heighten | Metamagic.Reach;
            }

            spark_ab.SetComponents(HelperEA.CreateAbilityVariants(spark_ab, sparks));

            // final feature that can be chosen in the talent selection
            var spark_feat = HelperEA.CreateFeature("SparkOfLifeFeature", spark_ab.Name, description,
                Guid.i.Reg("4ba4c89d8393441c94bdbd52172a4b95"),
                icon_ele,
                FeatureGroup.None,
                HelperEA.CreateAddFacts(spark_ab, link_activatable, link_activatable2),
                HelperEA.PrerequisiteClassLevel(kineticist_class, 10)
            );

            Helper.AppendAndReplace(ref wildtalent_selection.AllFeatures, spark_feat);


        }

        // known issues:
        // - in turn-combat, you can switch weapons and benefit from reach until you next turn (won't fix)
        // - for AoO, you can disable/enable metakinesis and infusions you didn't pay the burn cost for (won't fix)
        public static void addKineticWhipActivatable()
        {
            //var blade_enabled_buff = library.Get<BlueprintBuff>("426a9c079ee7ac34aa8e0054f2218074");
            //var apply_buff = Helper.CreateActionApplyBuff(blade_enabled_buff, permanent: true);
            //var kinetic_whip_buff = library.TryGet<BlueprintBuff>("46b25924ea144661b965fcf60c166f72");      //KineticWhipBuff

            var kinetic_whip_ab = library.TryGet<BlueprintAbility>("4c97c30bfda44b619e9053e9a7200493");     //KineticWhipAbility
            var kinetic_whip_feat = library.TryGet<BlueprintFeature>("0c18f66288764d8ca7bbc322c078bda3");   //KineticWhipAbilityFeature

            if (kinetic_whip_ab == null || kinetic_whip_feat == null)
                return;

            var whip_buff = HelperEA.CreateBuff(
                "KineticWhipActivatableBuff",
                kinetic_whip_ab.Name,
                kinetic_whip_ab.Description,
                "7c75d69432c749feb52e7cecc0e37419",
                kinetic_whip_ab.Icon,
                Contexts.NullPrefabLink
            );

             var whip_activatable = HelperEA.CreateActivatableAbility(
                "KineticWhipActivatable",
                whip_buff.Name,
                "Upgrades Kinetic Blade to Kinetic Whip.\n" + whip_buff.Description,
                "7a3e642ca3cd4d04afc176d6e9cc3546",
                whip_buff.Icon,
                whip_buff,
                AbilityActivationType.Immediately,
                UnitCommand.CommandType.Free,
                null,
                Helper.Create<ActivatableRestrictionKineticWhip>()
                //,Helper.Create<ActivatableRestrictionBurnCost>(a => a.Blueprint = kinetic_whip_ab.ObjToArray())
            );
            whip_activatable.IsOnByDefault = true;
            whip_activatable.DeactivateImmediately = true;

            var weapons = library.GetAllBlueprints().Where(w => w.GetComponent<WeaponKineticBlade>()).OfType<BlueprintItemWeapon>();
            var target_burn = weapons.Select(s => s.GetComponent<WeaponKineticBlade>().ActivationAbility).ToArray();

            // increase burn cost of kinetic blades by 1
            // increase reach by 4
            // allow Attack of Opportunity
            var inc_burn = Helper.CreateAddKineticistBurnModifier(1, KineticistBurnType.Infusion, null, target_burn);
            var reach_comp = HelperEA.CreateAddStatBonus(StatType.Reach, 4, ModifierDescriptor.Enhancement);
            var can_AoO = Helper.Create<AddConditionImmunity>(a => a.Condition = UnitCondition.DisableAttacksOfOpportunity);
            whip_buff.AddComponents(inc_burn, reach_comp, can_AoO);

            kinetic_whip_feat.AddComponent(HelperEA.CreateAddFact(whip_activatable));
        }

        //future
        public static void createaForestSiege()
        {
            //Heavy: These Gargantuan siege engines are too large to be transported to the battlefield in one piece, and require assembly. They typically hurl large stones indirectly at a target (targeting DC 25). Heavy catapults have a hardness of 5 and 200 hit points. Heavy catapult stones cost 25 gp and weigh 90 pounds each.
            //Catapult, heavy 	1,000 gp 	8d6 	x2 	300 ft.
            //(100 ft. min.) 	B 	4 	3 	3 	0 ft.
            //Catapult, Heavy: A heavy catapult is a massive engine capable of throwing rocks or heavy objects with great force. Because the catapult throws its payload in a high arc, it can hit squares out of its line of sight. To fire a heavy catapult, the crew chief makes a special check against DC 15 using only his base attack bonus, Intelligence modifier, range increment penalty, and the appropriate modifiers from the lower section of Table: Siege Engines. If the check succeeds, the catapult stone hits the square the catapult was aimed at, dealing the indicated damage to any object or character in the square. Characters who succeed on a DC 15 Reflex save take half damage. Once a catapult stone hits a square, subsequent shots hit the same square unless the catapult is reaimed or the wind changes direction or speed.
            //If a catapult stone misses, roll 1d8 to determine where it lands. This determines the misdirection of the throw, with 1 being back toward the catapult and 2 through 8 counting clockwise around the target square. Finally, count 1d4 squares away from the target square for every range increment of the attack.

            //CR8_TreantStandard
            //TreantSummoned:"1a4b042c3209f0f4991f407cd49b96ee"
            //TrollRockThrow
            //BombStandart
            //AlchemistFireBomb00
        }

        public static void createWoodSoldiers()
        {
            //4 wood golems with the advanced template, only one active, first use no burn, then 1 burn

            //Fx_Treant00_Green // TreantCreature00_Cycle
            //advanced template: AC increase natural armor by +2; Ability Scores +4 to all ability scores (except Int scores of 2 or less)
            var frost_heals = new AddBuffOnApplyingSpell.SpellConditionAndBuff();//Context[AbilitySharedValue.Damage] ? //FireVulnerability?
            frost_heals.Buff = library.Get<BlueprintBuff>("29c7bcbb38f2d73499bb0d52e000c27e");//FastHealing10
            frost_heals.Duration = HelperEA.CreateContextDuration(1);
            frost_heals.Descriptor = new SpellDescriptorWrapper(SpellDescriptor.Cold);
            var immunity_woodgolem = HelperEA.CreateFeature("WoodGolemImmunity", "Immunity to Magic", "A creature immune to any spell or spell-like ability that allows spell resistance.", "94a75af21339418d951556f1f7d8171e", (Sprite)null, FeatureGroup.None,
                Helper.Create<AddBuffOnApplyingSpell>(a => a.Buffs = frost_heals.ObjToArray()),
                Helper.CreateAddSpellImmunity((int)Patches_Spells.SpellImmunityTypeExt.AllExceptSpellDescriptor, SpellDescriptor.Fire));

            var unit_woodgolem = library.CopyAndAdd<BlueprintUnit>("dfd21dba15fe7dd4f95961ff27d91836", "WoodGolemSummoned", Guid.i.Reg("8bfad949944e449a92f659021b34ea07"));
            unit_woodgolem.LocalizedName = Helper.Create<SharedStringAsset>(a => a.String = HelperEA.CreateString("216d9aef-c1f3-46a7-9d0c-d91f6f6eb83e", "Wood Golem"));
            unit_woodgolem.Size = Size.Medium;
            unit_woodgolem.Faction = library.Get<BlueprintFaction>("1b08d9ed04518ec46a9b3e4e23cb5105");//Summoned
            unit_woodgolem.Strength = 18 + 4;
            unit_woodgolem.Dexterity = 17 + 4;
            unit_woodgolem.Constitution = 10 + 4;
            unit_woodgolem.Intelligence = 10 + 4;
            unit_woodgolem.Wisdom = 17 + 4;
            unit_woodgolem.Charisma = 1 + 4;
            unit_woodgolem.Speed = 30.Feet();
            unit_woodgolem.AddFacts = new BlueprintUnitFact[] {
                library.Get<BlueprintFeature>("8e934134fec60ab4c8972c85a7b62f89"),  //FireVulnerability
				library.Get<BlueprintUnitFact>("b9342e2a6dc5165489ba3412c50ca3d1"),	//NaturalArmor8
				library.Get<BlueprintFeature>("fb88b018013dc8e419150f86540c07f2"),	//DR5
				immunity_woodgolem };   //StoneGolemImmunity:"2617c0ea094687643a14fd99c4529523"
            unit_woodgolem.ReplaceComponent<AddClassLevels>(Helper.Create<AddClassLevels>(a =>
            {
                a.CharacterClass = library.Get<BlueprintCharacterClass>("fd66bdea5c33e5f458e929022322e6bf");//ConstructClass
                a.Levels = 6;
            }));

            //missing: Splintering (Su) As a free action once every 1d4+1 rounds, a wood golem can launch a barrage of razor-sharp wooden splinters from its body in a 20-foot-radius burst. All creatures caught within this area take 6d6 points of slashing damage (Reflex DC 14 halves). The save DC is Constitution-based.

            //buff self increases burn by 1 for 24 hours
            //ability summon
            //feature WoodSoldiers
            BlueprintAbility ability = HelperEA.CreateAbility(
                "WoodSoldiersAbility",
                "Wood Soldiers",
                "Element: wood\nType: utility\nBurn: 1"
                + "\nYour presence animates surrounding plant life and causes it to fight by your side. Each time you recover burn, you can use this ability once and ignore the burn cost. This functions as wooden phalanx, except you always animate four golems, and the duration lasts until the next time you recover burn."
                + "\nWooden Phalanx: You create 1d4+2 wood golems with the advanced template. The golems willingly aid you in combat or battle, perform a specific mission, or serve as bodyguards. You can only have one wooden phalanx spell in effect at one time. If you cast this spell while another casting is still in effect, the previous casting is dispelled.",
                Guid.i.Reg("03ca687cbbc44d7fb9c34908dc3b93ba"),
                null,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Medium,
                Strings.HourPerLevelDuration,
                Strings.SavingThrowNone
            );

            var cost_inc_buff = HelperEA.CreateBuff(
                "WoodSoldiersBurnBuff",
                "WoodSoldiersBurnBuff",
                "",
                Guid.i.Reg("638e0925d4974d4fbc570057c88bf15c"),
                null,
                Contexts.NullPrefabLink,
                Helper.CreateAddKineticistBurnModifier(1, KineticistBurnType.WildTalent, null, ability)
            );
            //Helper#.SetField(cost_inc_buff, "m_Flags", 2 | 8 | 16); //HiddenInUi, StayOnDeath, RemoveOnRest
            cost_inc_buff.m_Flags(HiddenInUi: true, StayOnDeath: true, RemoveOnRest: true);

            var summonpool = Helper.Create<BlueprintSummonPool>(a => a.name = "SummonWoodSoldiersPool"); library.AddAsset(summonpool, Guid.i.Reg("4b7b0b1cc82347eda11e994c86cb222b"));

            ability.SetComponents(HelperEA.CreateRunActions(
                HelperEA.CreateApplyBuff(cost_inc_buff, Contexts.Duration24Hours, false, false, true, false),
                Helper.CreateContextActionSpawnMonsterUnique(unit_woodgolem, summonpool, HelperEA.CreateContextDiceValue(DiceType.Zero, 0, 4))
            ));

            var feature = HelperEA.CreateFeature(
                "WoodSoldiersFeature",
                "Wood Soldiers",
                ability.Description,
                Guid.i.Reg("dcf263a5f5144cc1acec2e0177a8a4dd"),
                null,
                FeatureGroup.None,
                HelperEA.PrerequisiteClassLevel(kineticist_class, 16),
                prerequisite_earth
            );

            Helper.AppendAndReplace(ref wildtalent_selection.AllFeatures, feature);
        }

        //Wood
        //Healing Burst; twice the healing
        //Kinetic Revivification;
        //Forest Siege; summons treants with rock throw
        //Wood Soldiers; summons treants or golems
        //Foxfire; as Foxfire for fire
        //Thorn Flesh; as jagged flesh

        //Void
        //Healing Burst; twice and negative energy
        //Void Healer; as kinetic healer
        //Spark of Life summons Undead like Corpse Puppet
        //Gravity Control; as flame jet

        #region Patches

        [HarmonyLib.HarmonyPatch(typeof(RuleAttackRoll), "IncreaseMissChance")]
        public class RemoveMissChanceLimitPatch
        {
            public static bool Prefix(RuleAttackRoll __instance, int value)
            {
                if (value > 0
                    && __instance.Target.IsPlayerFaction
                    && Kineticist.hurricane_queen_feat != null
                    && __instance.Target.Descriptor.Progression.Features.HasFact(Kineticist.hurricane_queen_feat))
                {
                    HarmonyLib.AccessTools.Property(typeof(RuleAttackRoll), "MissChance").SetValue(__instance, Math.Min(value, 100));
                    return false;
                }
                return true;
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(BuffSubstitutionOnApply), "OnEventAboutToTrigger")]
        public class FixBuffSubstitutionDCLossPatch
        {
            public static bool Prefix(BuffSubstitutionOnApply __instance, RuleApplyBuff evt)
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

        [HarmonyLib.HarmonyPatch(typeof(AbilityKineticist), nameof(AbilityKineticist.CalculateBurnCost), typeof(UnitDescriptor), typeof(BlueprintAbility))]
        public class Patch_CalculateBurnCostFinal
        {
            public static void Postfix(UnitDescriptor caster, BlueprintAbility abilityBlueprint, ref KineticistAbilityBurnCost __result)
            {
                try
                {
                    KineticistAbilityBurnCost cost = __result;
                    EventBus.RaiseEvent<IKineticistFinalAbilityCostHandler>(h => h.HandleKineticistFinalAbilityCost(caster, abilityBlueprint, ref cost));
                    __result = cost;
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion

    }

    public class Kin_Element
    {
        public string Name; //like Cold
        public Sprite Icon;
        [CanBeNull] public BlueprintProgression ElementalSelection; //found in 1f3a15a3ae8a5524ab8b97f469bf4e3d.AllFeatures
        [CanBeNull] public BlueprintFeatureSelection BlastSelection;
        [CanBeNull] public BlueprintProgression BlastProgression;       //like ColdBlastProgression
        [CanBeNull] public BlueprintProgression SecondaryProgression;   //only for composite blasts
        [CanBeNull] public BlueprintProgression ThirdProgression;       //only for composite blasts
        public BlueprintFeature BlastFeature;   //like ColdBlastFeature
        public BlueprintAbility BaseBlast;  //like ColdBlastBase
        public AbilitySpawnFx[] BasicSFX;
        public ContextRankConfig[] BasicRank;
        public SpellDescriptorComponent SpellDescriptor;
        public Kin_Variant BasicVariant;
        public List<Kin_Variant> Variants;

        public bool Validate()
        {
            return Name != null
                && (ElementalSelection || SecondaryProgression)
                && (BlastProgression || ThirdProgression)
                && BlastFeature
                && BaseBlast
                && BasicSFX != null
                && BasicRank != null
                && BasicRank[0]
                && BasicRank[1]
                && SpellDescriptor
                && BasicVariant != null
                && Variants != null
                && Variants.Count > 0;
        }

        public static List<Kin_Element> All = new List<Kin_Element>();
        public static List<BlueprintFeature> AllFormInfusions = new List<BlueprintFeature>();
        public static List<BlueprintFeature> AllSubstanceInfusions = new List<BlueprintFeature>();

        public static void Load(BlueprintProgression prog, BlueprintFeature focus, BlueprintFeatureBase fact)
        {
            if (prog.LevelEntries == null
                || prog.LevelEntries.Length < 1
                || prog.LevelEntries[0].Features == null
                || prog.LevelEntries[0].Features.Count < 1)
                Main.DebugLogAlways("error: prog in progressions is empty " + prog.name);
            else
            {
                var kin = new Kin_Element();
                kin.Name = prog.name; //this gets overwritten, just for debug
                kin.ElementalSelection = (BlueprintProgression)focus;   //cannot be null
                kin.BlastSelection = fact as BlueprintFeatureSelection; //can be null
                kin.BlastProgression = prog;
                kin.BlastFeature = prog.LevelEntries[0].Features[0] as BlueprintFeature;

                All.Add(kin);
            }
        }

        public static void LoadAll()
        {
            // this algorithm should be future prove
            // we start with the first selection, which are Air, Earth, Fire, Water
            var master_selection = Main.library.Get<BlueprintFeatureSelection>("1f3a15a3ae8a5524ab8b97f469bf4e3d"); //ElementalFocusSelection
            var composite_buff = Main.library.Get<BlueprintBuff>("cb30a291c75def84090430fbf2b5c05e");               //CompositeBlastBuff
            var second_selection = Main.library.Get<BlueprintFeatureSelection>("e2c1718828fc843479f18ab4d75ded86"); //SecondatyElementalFocusSelection
            var third_selection = Main.library.Get<BlueprintFeatureSelection>("4204bc10b3d5db440b1f52f0c375848b");  //ThirdElementalFocusSelection

            // adds Kin_Elements from lv1 selections
            foreach (var focus in master_selection.AllFeatures)
            {
                // then we are looking for all the facts we get at level 1
                // usually we should get class skills and either an element (fire, earth)
                // or another selection (air, water => air/electric, water/cold)
                var lv1_facts = (focus as BlueprintProgression)?.LevelEntries?.FirstOrDefault(s => s.Level == 1)?.Features;
                if (lv1_facts == null)
                {
                    Main.DebugLogAlways("error: lv1_facts is not BlueprintProgression " + focus.name);
                    continue;
                }

                List<BlueprintProgression> progressions = new List<BlueprintProgression>();
                foreach (var fact in lv1_facts) // this includes ClassSkills (BlueprintFeature)
                {
                    // we extract the blueprint, if it is indeed a selection (air, water)
                    progressions.Clear();
                    if (fact is BlueprintProgression)
                        progressions.Add(fact as BlueprintProgression);
                    else if (fact is BlueprintFeatureSelection)
                    {
                        //progressions.AddRange((fact as BlueprintFeatureSelection).AllFeatures.OfType<BlueprintProgression>());
                        foreach (var f in (fact as BlueprintFeatureSelection).AllFeatures)
                        {
                            if (f is BlueprintProgression)
                                progressions.Add(f as BlueprintProgression);
                            else
                                Main.DebugLogAlways("error: unexpected type in progselection " + f.name + " of " + f.GetType());
                        }
                    }

                    // now we can get the basic blueprints for each of the blast types
                    foreach (var prog in progressions)
                    {
                        if (prog.LevelEntries == null
                            || prog.LevelEntries.Length < 1
                            || prog.LevelEntries[0].Features == null
                            || prog.LevelEntries[0].Features.Count < 1)
                            Main.DebugLogAlways("error: prog in progressions is empty " + prog.name);
                        else
                        {
                            var kin = new Kin_Element();
                            kin.Name = prog.name; //this gets overwritten, just for debug
                            kin.ElementalSelection = focus as BlueprintProgression; //cannot be null here
                            kin.BlastSelection = fact as BlueprintFeatureSelection; //can be null
                            kin.BlastProgression = prog;
                            kin.BlastFeature = prog.LevelEntries[0].Features[0] as BlueprintFeature;

                            All.Add(kin);
                        }
                    }
                }
            }

            // adds Kin_Elements from CompositeBlastBuff
            // everytime an element is picked it also reapplies this buff, which checks for missing composite blasts
            foreach (var actions in composite_buff.GetComponent<AddFactContextActions>().Activated.Actions)
            {
                var kin = new Kin_Element();

                // the conditions are two blast progressions and NOT having the composite feature
                var conditions = (actions as Conditional)?.ConditionsChecker?.Conditions;//?.Select(s => s as ContextConditionHasFact).Where(s => s != null);
                foreach (var condition in conditions)
                {
                    if (condition is ContextConditionHasFact)
                    {
                        var fact = (condition as ContextConditionHasFact).Fact;
                        if (condition.Not && fact is BlueprintFeature)
                            kin.BlastFeature = fact as BlueprintFeature;
                        else if (kin.BlastProgression == null && fact is BlueprintProgression)
                            kin.BlastProgression = fact as BlueprintProgression;
                        else if (fact is BlueprintProgression)
                            kin.SecondaryProgression = fact as BlueprintProgression;
                        else
                            Main.DebugLogAlways("error: composite invalid condition fact " + fact.name);
                    }
                    else
                        Main.DebugLogAlways($"error: composite invalid entry {condition.name}:{condition.GetType()}");
                }

                kin.Name = kin.BlastFeature?.name;

                All.Add(kin);
            }

            foreach (var focus in second_selection.AllFeatures)
            {
                var trigger = focus.GetComponent<ActivateTrigger>();

                var kin = new Kin_Element();
                kin.Name = focus.name;
                kin.ElementalSelection = (trigger?.Conditions?.Conditions?[0] as HasFact)?.Fact as BlueprintProgression;
                kin.SecondaryProgression = focus as BlueprintProgression;
                kin.ThirdProgression = third_selection.AllFeatures.FirstOrDefault(s => (s.GetComponent<ActivateTrigger>()?.Conditions?.Conditions?[0] as HasFact).Fact == kin.ElementalSelection) as BlueprintProgression;
                kin.BlastFeature = (trigger?.Actions?.Actions?[0] as AddFact)?.Fact as BlueprintFeature;

                All.Add(kin);
            }


            // adds new List<Kin_Variant> to Kin_Element
            foreach (var kin in All)
            {
                // now that we have the basic blueprints, we break them apart
                // to have a nice organized data structure

                // composite blasts grant the base blast with AddFacts, so we check for that first
                // all other blasts use AddFeatureIfHasFact, so we check if no AddFacts where present
                // if the feature doesn't contain "BlastBase", then something is wrong and we stop
                kin.Icon = kin.BlastFeature.Icon;
                kin.BaseBlast = kin.BlastFeature?.GetComponent<AddFacts>()?.Facts?[0] as BlueprintAbility;
                kin.BaseBlast = kin.BaseBlast ?? kin.BlastFeature?.GetComponent<AddFeatureIfHasFact>()?.Feature as BlueprintAbility;
                if (kin.BaseBlast == null)
                {
                    Main.DebugLogAlways("error: blastbase is null " + kin.BlastFeature?.name);
                    continue;
                }
                {
                    int index = kin.BaseBlast.name.IndexOf("BlastBase");
                    if (index > 0)
                        kin.Name = kin.BaseBlast.name.Substring(0, index);
                    else
                    {
                        Main.DebugLogAlways("error: kin.BaseBlast.name " + kin.BaseBlast.name);
                        continue;
                    }
                }

                // we extract all the variants; one basic blast and another for each form infusion
                kin.Variants = new List<Kin_Variant>();
                foreach (var variant in kin.BaseBlast.GetComponent<AbilityVariants>()?.Variants ?? Array.Empty<BlueprintAbility>())
                {
                    var kvar = new Kin_Variant();
                    kvar.Parent = kin;  // might block the GC, but we never dispose this anyway
                    kvar.InfusionAbility = variant;
                    kvar.InfusionFeature = variant.GetComponent<AbilityShowIfCasterHasFact>()?.UnitFact as BlueprintFeature;

                    // we are looking for the ranks
                    // area duration (as for wall infusion) is usually AbilityRankType.Default
                    // but sometimes AbilityRankType.DamageDice is used instead
                    // technically duration could be any type, but we will assume good practise
                    // as for blasts (non-area) DamageDice and DamageBonus is what we are looking for
                    foreach (var rank in variant.GetComponents<ContextRankConfig>() ?? Array.Empty<ContextRankConfig>())
                    {
                        if (rank.Type == AbilityRankType.DamageDice)
                            kvar.DamageDice = rank;
                        else if (rank.Type == AbilityRankType.DamageBonus)
                            kvar.DamageBonus = rank;
                        else
                            kvar.Duration = rank;
                    }

                    // we recursively look for all the GameActions
                    // we should have 1, +1 if the element is split physical and energy
                    // or 1 total if it's ContextActionSpawnAreaEffect
                    kvar.AllActions = variant.GetActions();
                    kvar.AreaAction = kvar.AllActions.FirstOrDefault(s => s is ContextActionSpawnAreaEffect) as ContextActionSpawnAreaEffect;

                    if (kvar.AreaAction != null)
                    {
                        // if it indeed is an AreaEffect, then we should have gotten Duration
                        // unless it was saved in either DamageDice or DamageBonus
                        // so we fix Duration and look for DamageDice and DamageBonus again
                        // this time in the AreaEffect
                        if (kvar.Duration == null)
                            kvar.Duration = kvar.DamageDice ?? kvar.DamageBonus;
                        foreach (var rank in kvar.AreaAction.AreaEffect.GetComponents<ContextRankConfig>() ?? Array.Empty<ContextRankConfig>())
                        {
                            if (rank.Type == AbilityRankType.DamageDice)
                                kvar.DamageDice = rank;
                            else if (rank.Type == AbilityRankType.DamageBonus)
                                kvar.DamageBonus = rank;
                            else
                                Main.DebugLog("AreaEffect has unknown ContextRankConfig " + kvar.AreaAction.AreaEffect.name);
                        }

                        // we will also need the actions of the AreaEffect
                        // this adds 1 action, or 2 if it deals both physical and energy
                        var area_run = kvar.AreaAction.AreaEffect.GetComponent<AbilityAreaEffectRunAction>();
                        if (area_run == null)
                            Main.DebugLogAlways("area_run is null " + kvar.AreaAction.AreaEffect.name);
                        else
                        {
                            kvar.AllActions.AddRange(area_run.UnitEnter.Actions);
                        }
                    }

                    kvar.DamageActions = kvar.AllActions.OfType<ContextActionDealDamage>().ToArray();

#if DEBUG
                    Main.DebugLog("Printout of ContextActionDealDamage " + kvar.InfusionAbility.name);
                    for (int i = 0; i < kvar.DamageActions.Count(); i++)
                    {
                        var act = kvar.DamageActions[i];
                        Main.DebugLog($" Type={act.DamageType} Value={act.Value} IsAoE={act.IsAoE} Half={act.Half} HalfIfSaved={act.HalfIfSaved}");
                    }
#endif

                    // now that we have all the actions, we can look for the damage types
                    // it can be any combination of physical and energy type
                    foreach (var element in kvar.DamageActions)
                    {
                        if (element.DamageType.Type == DamageType.Physical)
                            kvar.PForm = element.DamageType.Physical.Form;
                        else if (element.DamageType.Type == DamageType.Energy)
                            kvar.EForm = element.DamageType.Energy;
                    }

                    // burn cost should also be easy
                    // WildTalentBurnCost should always be 0, so we ignore it
                    var burn = variant.GetComponent<AbilityKineticist>();
                    if (burn == null)
                        Main.DebugLogAlways("error: AbilityKineticist is null " + variant.name);
                    else
                    {
                        kvar.BlastBurnCost = burn.BlastBurnCost;
                        kvar.InfusionBurnCost = burn.InfusionBurnCost;
                    }

                    if (kvar.Validate())
                    {
                        kin.Variants.Add(kvar);
                        Main.DebugLog("Kin_Variant loaded: " + kvar.InfusionAbility.name);
                    }
                    else
                        Main.DebugLogAlways("error: validate failed for " + kvar.InfusionAbility.name);

                    // here we point to the basic blast, to have it easier
                    // only the basic blast should not have AbilityShowIfCasterHasFact
                    if (kvar.InfusionFeature == null)
                    {
                        // if we already have a basic blast registered, then something is wrong
                        if (kin.BasicVariant == null)
                        {
                            kin.BasicVariant = kvar;
                            kin.BasicRank = new ContextRankConfig[] { kvar.DamageDice, kvar.DamageBonus };
                            kin.SpellDescriptor = kvar.InfusionAbility.GetComponent<SpellDescriptorComponent>();
                            kin.BasicSFX = kvar.InfusionAbility.GetComponents<AbilitySpawnFx>().ToArray();
                        }
                        else
                        {
                            Main.DebugLogAlways("error: basic blast already defined " + variant.name);
                        }
                    }
                }
            }

            var infusion_selection = Main.library.Get<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea");    //InfusionSelection
            foreach (var infusion in infusion_selection.AllFeatures)
            {
                if (infusion.GetComponent<AddFacts>() && infusion.AssetGuid != "80fdf049d396c33408a805d9e21a42e1")
                {
                    AllSubstanceInfusions.Add(infusion);
                    Main.DebugLog("listing as substance: " + infusion.name);
                }
                else
                {
                    AllFormInfusions.Add(infusion);
                    Main.DebugLog("listing as form: " + infusion.name);
                }
            }

            for (int i = All.Count - 1; i >= 0; i--)
            {
                if (All[i].Validate())
                    Main.DebugLog("Kin_Element loaded: " + All[i].Name);
                else
                {
                    Main.DebugLogAlways("error: validate failed for " + All[i].Name);
                    All.RemoveAt(i);
                }
            }
        }
    }

    public class Kin_Variant
    {
        public Kin_Element Parent;
        public BlueprintAbility InfusionAbility; //like WallColdBlastAbility
        [CanBeNull] public BlueprintFeature InfusionFeature; //like WallInfusion
        public ContextRankConfig DamageDice;
        public ContextRankConfig DamageBonus;
        [CanBeNull] public ContextRankConfig Duration;
        public List<GameAction> AllActions;
        public ContextActionDealDamage[] DamageActions;
        [CanBeNull] public ContextActionSpawnAreaEffect AreaAction;
        public int BlastBurnCost;
        public int InfusionBurnCost;
        public PhysicalDamageForm PForm = 0;
        public DamageEnergyType EForm = (DamageEnergyType)(-1);
        public bool IsComposite { get { return BlastBurnCost > 0; } }
        public bool IsPhysical { get { return PForm != 0; } }
        public bool HasPhysical { get { return PForm != 0; } }
        public bool IsEnergy { get { return PForm == 0; } }
        public bool HasEnergy { get { return EForm >= 0; } }

        public bool Validate()
        {
            return Parent != null
                && InfusionAbility != null
                && DamageDice != null
                && DamageBonus != null
                && AllActions != null
                && DamageActions != null
                && (HasPhysical || HasEnergy);
        }

        public void RecalculateDamage() // reapplies changes to PForm, EForm, and BlastBurnCost
        {
            foreach (var action in DamageActions)
            {
                //action.
            }
        }
    }
}
