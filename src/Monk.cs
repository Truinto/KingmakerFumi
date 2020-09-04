using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;
using Kingmaker;
using Kingmaker.Enums;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Blueprints.Items;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.EntitySystem.Stats;
using FumisCodex.NewComponents;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.ElementsSystem;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;
using Guid = FumisCodex.GuidManager;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.View.Animation;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Enums.Damage;
using Kingmaker.UI.ServiceWindow.CharacterScreen;
using Kingmaker.UI.Common;
using System.Reflection.Emit;
using Kingmaker.Items.Slots;
//using CallOfTheWild;

namespace FumisCodex
{
    public class Monk
    {
        static LibraryScriptableObject library => Main.library;

        public static BlueprintCharacterClass monk_class = library.Get<BlueprintCharacterClass>("e8f21e5b58e0569468e420ebea456124");//MonkClass
        public static BlueprintWeaponType weapon_unarmed = library.Get<BlueprintWeaponType>("fcca8e6b85d19b14786ba1ab553e23ad");//Unarmed
        public static BlueprintFeature IUS = library.Get<BlueprintFeature>("7812ad3672a4b9a4fb894ea402095167");//ImprovedUnarmedStrike
        public static BlueprintFeature flurry1 = library.Get<BlueprintFeature>("332362f3bd39ebe46a740a36960fdcb4");//FlurryOfBlows
        public static BlueprintFeature flurry2 = library.Get<BlueprintFeature>("de25523acc24b1448aa90f74d6512a08");//FlurryOfBlowsLevel11
        public static BlueprintFeature TWF = library.Get<BlueprintFeature>("6948b379c0562714d9f6d58ccbfa8faa");//TwoWeaponFightingBasicMechanics
        public static BlueprintFeatureSelection ki_powers = library.Get<BlueprintFeatureSelection>("3049386713ff04245a38b32483362551");//MonkKiPowerSelection

        public static PrerequisiteFeature preq_IUS = HelperEA.PrerequisiteFeature(IUS);
        public static List<BlueprintFeature> combat_styles = new List<BlueprintFeature>();
        public static BlueprintArchetype archetype_MOMS;
        public static ActivatableAbilityGroup MOMS_wildcardgroup = Main.Patch_ActivatableAbilityGroup.GetNewGroup();

        public static BlueprintBuff DualUnarmedBuff = HelperEA.CreateBuff(   //buff for activateable that enables logic
            "DualUnarmedBuff", "Unarmed Dual Wielding", "You may use your empty offhand for Two Weapon Fighting.", "215e193fbc1448bb828ade364df60e72", null, Contexts.NullPrefabLink);

        public static void allowTWFwithFists()
        {
            var dual_unarmed_activatable = HelperEA.CreateActivatableAbility(
                "DualUnarmedActivatable",
                DualUnarmedBuff.Name,
                DualUnarmedBuff.Description,
                Guid.i.Reg("c483908a936b454893117ad67bbacb05"),
                library.Get<BlueprintFeature>("cd96b7275c206da4899c69ae127ffda6").Icon,
                DualUnarmedBuff,
                AbilityActivationType.Immediately,
                CommandType.Free,
                null
            );

            //TWF.AddComponents(
            //    Helpers.Create<Monk.TwoWeaponFightingAttacks>(),
            //    Helpers.Create<Monk.TwoWeaponFightingAttackPenalty>(),
            //    Helpers.Create<Monk.TwoWeaponFightingDamagePenalty>(),
            //    Helpers.CreateAddFact(dual_unarmed_activatable)
            //);
        }

        public static void createStyleMaster(bool enabled = true)
        {
            var stylemaster_feat = HelperEA.CreateFeature(
                "CombatStyleMasterFeature",
                "Combat Style Master",
                "You can fuse two of the styles you knows into a more perfect style. You can have two style feat stances active at once.",
                Guid.i.Reg("e3f51860700148f2b0c66e8cd3d0599b"),
                null,
                FeatureGroup.Feat,
                HelperEA.CreateIncreaseActivatableAbilityGroupSize(ActivatableAbilityGroup.CombatStyle),
                HelperEA.PrerequisiteFeature(IUS),
                HelperEA.PrerequisiteStatValue(StatType.BaseAttackBonus, 6, true),
                HelperEA.PrerequisiteClassLevel(monk_class, 5, true)
            );
            stylemaster_feat.Groups = new FeatureGroup[] { FeatureGroup.Feat, FeatureGroup.CombatFeat };

            if (enabled)
                HelperEA.AddCombatFeats(library, stylemaster_feat);
        }

        public static void createMedusasWrath()
        {
            var feature = HelperEA.CreateFeature(
                "MedusasWrath",
                "Medusa's Wrath",
                "Whenever you use the full-attack action and make at least one unarmed strike, you can make two additional unarmed strikes at your highest base attack bonus. These bonus attacks must be made against a dazed, flat-footed, paralyzed, staggered, stunned, or unconscious foe.",
                Guid.i.Reg("6696238387114063b11ea42ae4291acd"),
                null,
                FeatureGroup.CombatFeat,
                Helper.Create<MedusasWrath>()
            );
            feature.Groups = new FeatureGroup[] { FeatureGroup.Feat, FeatureGroup.CombatFeat };

            Helper.AppendAndReplace(ref library.Get<BlueprintFeatureSelection>("1051170c612d5b844bfaa817d6f4cfff").AllFeatures, feature);//MonkBonusFeatSelectionLevel10
            Helper.AppendAndReplace(ref library.Get<BlueprintFeatureSelection>("c569fc66f22825445a7b7f3b5d6d208f").AllFeatures, feature);//ScaledFistBonusFeatSelectionLevel10
        }

        #region Combat Styles

        // known issues:
        // - snake style gives +2 AC, instead of sense motive check in to block an attack once per round; it's difficult to select which attack should be affected; I find this a fair compensation
        // - snake sidewind gives +4 crit confirmation, instead of sense motive check; this seems more fitting than diplomacy checks to confirm crits
        // - snake sidewind currently applies +5 foot at all times; right now I am not sure how to implement this in the first place; an extra 5 foot step is much better than +5 speed (TODO)
        // - snake fang does not allow for an extra attack as a swift action; no idea how I could implement this
        public static void createSnakeStyle()
        {
            /* Original:
            Snake Style (Combat, Style)
            You watch your foe’s every movement and then punch through its defense.
            Prerequisite: Improved Unarmed Strike, Acrobatics 1 rank, Sense Motive 3 ranks.
            Benefit: You gain a +2 bonus on Sense Motive checks, and you can deal piercing damage with your unarmed strikes. While using the Snake Style feat, when an opponent targets you with a melee or ranged attack, you can spend an immediate action to make a Sense Motive check. You can use the result as your AC or touch AC against that attack. You must be aware of the attack and not flat-footed.
            Normal: An unarmed strike deals bludgeoning damage.

            Snake Sidewind (Combat, Style)
            Your sensitive twisting movements make you difficult to anticipate combat.
            Prerequisite: Improved Unarmed Strike, Snake Style, Acrobatics 3 ranks, Sense Motive 6 ranks.
            Benefit: You gain a +4 bonus to CMD against trip combat maneuvers and on Acrobatics checks and saving throws to avoid being knocked prone. While using the Snake Style feat, whenever you score a critical threat with your unarmed strike, you can make a Sense Motive check in place of the attack roll to confirm the critical hit. Whenever you score a critical hit with your unarmed strike, you can spend an immediate action to take a 5-foot step even if you have otherwise moved this round.
            Normal: You can take a 5-foot step only if you have not otherwise moved this round.

            Snake Fang (Combat, Style)
            You can unleash attacks against an opponent that has dropped its guard.
            Prerequisite: Combat Reflexes, Improved Unarmed Strike, Snake Sidewind, Snake Style, Acrobatics 6 ranks, Sense Motive 9 ranks.
            Benefit: While using the Snake Style feat, when an opponent’s attack misses you, you can make an unarmed strike against that opponent as an attack of opportunity. If this attack of opportunity hits, you can spend an immediate action to make another unarmed strike against the same opponent.
            */

            var icon = Helper.Image2Sprite.Create("snake.png");

            var snakestyle_buff = HelperEA.CreateBuff(
                "SnakeStyleBuff",
                "Snake Style",
                "Snake style emphasizes quick, shifting movements. Its practitioners normally hold their hands flat with the fingers together to mimic the head of a snake. Able to strike when least expected, snake stylists are known for opportunism and blinding speed.",
                Guid.i.Reg("da3d0cebc1884779a70d3223a68d2317"),
                icon,
                Contexts.NullPrefabLink,
                Helper.CreateAddContextStatBonus(StatType.AC, Contexts.ValueTwo, ModifierDescriptor.Dodge)
            );
            var snakestyle_activatable = HelperEA.CreateActivatableAbility(
                "SnakeStyleActivatable",
                "Snake Style",
                snakestyle_buff.Description,
                Guid.i.Reg("f789139421494be29d54431ba2626b48"),
                icon,
                snakestyle_buff,
                AbilityActivationType.Immediately,
                CommandType.Swift,
                null
            );
            snakestyle_activatable.Group = ActivatableAbilityGroup.CombatStyle;
            var snake_style = HelperEA.CreateFeature(
                "SnakeStyle",
                "Snake Style",
                "You watch your foe’s every movement and then punch through its defense. You gain a +2 bonus on Diplomacy checks, and you can deal piercing damage with your unarmed strikes. While using the Snake Style feat, you receive a +2 dodge bonus on your AC.",
                Guid.i.Reg("5d2fd973e148415fbe5f008245e5dc8f"),
                icon,
                FeatureGroup.Feat,
                preq_IUS,
                HelperEA.PrerequisiteStatValue(StatType.SkillAthletics, 1),
                HelperEA.PrerequisiteStatValue(StatType.CheckDiplomacy, 3),
                HelperEA.CreateAddFact(snakestyle_activatable),
                Helper.CreateAddContextStatBonus(StatType.CheckDiplomacy, Contexts.ValueTwo),
                Helper.CreateAddOutgoingPhysicalDamageProperty(WeaponType: weapon_unarmed, Form: PhysicalDamageForm.Piercing)
            );

            var snakesidewind_buff = HelperEA.CreateBuff(
                "SnakeSidewindBuff",
                "Snake Sidewind",
                "",
                Guid.i.Reg("47aba9b9c2e34d62b1fc619659c2df95"),
                icon,
                Contexts.NullPrefabLink,
                Helper.CreateCriticalConfirmationWeaponType(Contexts.ValueFour, WeaponCategory.UnarmedStrike),
                Helper.CreateBuffMovementSpeed(5, ModifierDescriptor.Circumstance)// TODO: should be crit only! createAddInitiatorAttackWithWeaponTriggerWithCategory
            );
            snakesidewind_buff.m_Flags(HiddenInUi: true);
            var snake_sidewind = HelperEA.CreateFeature(
                "SnakeSidewind",
                "Snake Sidewind",
                "Your sensitive twisting movements make you difficult to anticipate combat. You gain a +4 bonus to CMD against trip combat maneuvers. While using the Snake Style feat, whenever you score a critical threat with your unarmed strike, you receive a +4 circumstance bonus on attack rolls made to confirm critical hits. Whenever you score a critical hit with your unarmed strike, you receive a +5 foot circumstance bonus on your movement speed for 1 round.",
                Guid.i.Reg("e550008e19914cde9f3deebcea2ac829"),
                icon,
                FeatureGroup.Feat,
                HelperEA.PrerequisiteFeature(snake_style),
                HelperEA.PrerequisiteStatValue(StatType.SkillAthletics, 3),
                HelperEA.PrerequisiteStatValue(StatType.CheckDiplomacy, 6),
                HelperEA.CreateManeuverDefenseBonus(CombatManeuver.Trip, 4)
            );

            var snakefang_buff = HelperEA.CreateBuff(
                "SnakeFangBuff",
                "Snake Fang",
                "",
                Guid.i.Reg("2cda60f8e18742569eadbd870b10ba79"),
                icon,
                Contexts.NullPrefabLink,
                Helper.Create<SnakeFang>()
            );
            snakefang_buff.m_Flags(HiddenInUi: true);
            var snake_fang = HelperEA.CreateFeature(
                "SnakeFang",
                "Snake Fang",
                "You can unleash attacks against an opponent that has dropped its guard. While using the Snake Style feat, when an opponent’s attack misses you, you can make an attack of opportunity against that opponent.",
                Guid.i.Reg("11478867d1a64638b7a9635c664c3354"),
                icon,
                FeatureGroup.Feat,
                HelperEA.PrerequisiteFeature(snake_style),
                HelperEA.PrerequisiteFeature(snake_sidewind),
                HelperEA.PrerequisiteFeature(library.Get<BlueprintFeature>("0f8939ae6f220984e8fb568abbdfba95")),//CombatReflexes
                HelperEA.PrerequisiteStatValue(StatType.SkillAthletics, 6),
                HelperEA.PrerequisiteStatValue(StatType.CheckDiplomacy, 9)
            );

            snakestyle_buff.AddComponents(
                Helper.CombatStyleHelper(snake_sidewind, snakesidewind_buff),
                Helper.CombatStyleHelper(snake_fang, snakefang_buff)
            );

            snake_style.Groups = new FeatureGroup[] { FeatureGroup.Feat, FeatureGroup.CombatFeat };
            snake_sidewind.Groups = snake_style.Groups;
            snake_fang.Groups = snake_style.Groups;

            HelperEA.AddFeats(library, snake_style, snake_sidewind, snake_fang);
            combat_styles.Add(snake_style);
            combat_styles.Add(snake_sidewind);
            combat_styles.Add(snake_fang);
        }

        // known issues:
        // - boar style, special rend does not check whenever the first and second hit were the same creature (won't fix)
        public static void createBoarStyle()
        {
            /*
            Boar Style (Combat, Style)
            Your sharp teeth and nails rip your foes open.
            Prerequisites: Improved Unarmed Strike, Intimidate 3 ranks.
            Benefit: You can deal bludgeoning damage or slashing damage with your unarmed strikes—changing damage type is a free action. While using this style, once per round when you hit a single foe with two or more unarmed strikes, you can tear flesh. When you do, you deal 2d6 extra points of damage with the attack.
            
            Boar Ferocity (Combat, Style)
            Your flesh-ripping unarmed strikes terrify your victims.
            Prerequisites: Improved Unarmed Strike, Boar Style, Intimidate 6 ranks.
            Benefit: You add piercing damage to the damage types you can deal with your unarmed strikes. Further, you gain a +2 bonus on Intimidate checks to demoralize opponents. While using Boar Style, whenever you tear an opponent’s flesh, you can spend a free action to make an Intimidate check to demoralize that opponent.
            
            Boar Shred (Combat, Style)
            The wounds you inflict with your unarmed strikes bleed, giving you renewed vigor.
            Prerequisites: Improved Unarmed Strike, Boar Ferocity, Boar Style, Intimidate 9 ranks.
            Benefit: You can make an Intimidate check to demoralize an opponent as a move action. While using Boar Style, whenever you tear an opponent’s flesh, once per round at the start of that opponent’s turn he takes 1d6 bleed damage. The bleed damage dealt while using Boar Style persist even if you later switch to a different style.
            */

            var icon = Helper.Image2Sprite.Create("boar.png");

            var boar_ferocity = HelperEA.CreateFeature(
                "BoarFerocity",
                "Boar Ferocity",
                "You add piercing damage to the damage types you can deal with your unarmed strikes. Further, you gain a +2 bonus on Intimidate checks to demoralize opponents. While using Boar Style, whenever you tear an opponent’s flesh, you can spend a free action to make an Intimidate check to demoralize that opponent.",
                Guid.i.Reg("51241be4957847c6ba737bb3aaa0e402"),
                icon,
                FeatureGroup.CombatFeat,
                Helper.CreateAddOutgoingPhysicalDamageProperty(WeaponType: weapon_unarmed, Form: PhysicalDamageForm.Piercing),
                Helper.CreateAddContextStatBonus(StatType.CheckIntimidate, Contexts.ValueTwo, ModifierDescriptor.UntypedStackable),
                preq_IUS,
                HelperEA.PrerequisiteStatValue(StatType.CheckIntimidate, 6)
            );
            var ferocity_conditional = HelperEA.CreateConditional(HelperEA.CreateConditionCasterHasFact(boar_ferocity), 
                ifTrue: library.Get<BlueprintAbility>("7d2233c3b7a0b984ba058a83b736e6ac").GetComponent<AbilityEffectRunAction>().Actions.Actions[0]
            );//we need this for boar style, since it's called from RendSpecial
            
            var intimidate_ab = library.CopyAndAdd<BlueprintAbility>("7d2233c3b7a0b984ba058a83b736e6ac", "IntimidateAsMoveAbility", Guid.i.Reg("d3fe9ad8af284f29b3bb7384e9249b9a"));//PersuasionUseAbility
            intimidate_ab.ActionType = CommandType.Move;
            var boar_shred = HelperEA.CreateFeature(
                "BoarShred",
                "Boar Shred",
                "You can make an Intimidate check to demoralize an opponent as a move action. While using Boar Style, whenever you tear an opponent’s flesh, once per round at the start of that opponent’s turn he takes 1d6 bleed damage. The bleed damage dealt while using Boar Style persist even if you later switch to a different style.",
                Guid.i.Reg("fe358f6c01724011ae848daf71db7fe2"),
                icon,
                FeatureGroup.CombatFeat,
                HelperEA.CreateAddFact(intimidate_ab),
                preq_IUS,
                HelperEA.PrerequisiteStatValue(StatType.CheckIntimidate, 9)
            );
            var shred_conditional = HelperEA.CreateConditional(HelperEA.CreateConditionCasterHasFact(boar_shred), HelperEA.CreateApplyBuff(
                library.Get<BlueprintBuff>("75039846c3d85d940aa96c249b97e562"), Contexts.DurationZero, false, false,
                false, false, true));//we need this for boar style, since it's called from RendSpecial

            var boar_style_buff = HelperEA.CreateBuff(
                "BoarStyleBuff",
                "Boar Style",
                "The objective of the Boar Style is to attack with as much viciousness and cruelty as possible in order to break enemy morale. Fanatical followers of the style use herbal and alchemical reagents to harden their nails and teeth, sometimes performing self-mutilating procedures that result in claw-like nails and sharpened teeth.",
                Guid.i.Reg("c09127b74e2e441f83dec13a3c045ee8"),
                icon,
                Contexts.NullPrefabLink,
                Helper.CreateRendSpecial(new DiceFormula(2, DiceType.D6), null, WeaponCategory.UnarmedStrike, false, ferocity_conditional, shred_conditional)
            );
            var boarstyle_activatable = HelperEA.CreateActivatableAbility(
                "BoarStyleActivatable",
                "Boar Style",
                boar_style_buff.Description,
                Guid.i.Reg("18fd7159e7cc41a5b98ca671251b4573"),
                icon,
                boar_style_buff,
                AbilityActivationType.Immediately,
                CommandType.Swift,
                null
            );
            boarstyle_activatable.Group = ActivatableAbilityGroup.CombatStyle;
            var boar_style = HelperEA.CreateFeature(
                "BoarStyle",
                "Boar Style",
                "You can deal bludgeoning and slashing damage with your unarmed strikes. While using this style, once per round when you hit a single foe with two or more unarmed strikes, you can tear flesh. When you do, you deal 2d6 extra points of damage with the attack.",
                Guid.i.Reg("7b9502f170c549b3a198058f3c1fecea"),
                icon,
                FeatureGroup.CombatFeat,
                HelperEA.CreateAddFact(boarstyle_activatable),
                Helper.CreateAddOutgoingPhysicalDamageProperty(WeaponType: weapon_unarmed, Form: PhysicalDamageForm.Slashing),
                preq_IUS,
                HelperEA.PrerequisiteStatValue(StatType.CheckIntimidate, 3)
            );

            boar_ferocity.AddComponent(HelperEA.PrerequisiteFeature(boar_style));
            boar_shred.AddComponents(HelperEA.PrerequisiteFeature(boar_style), HelperEA.PrerequisiteFeature(boar_ferocity));

            boar_style.Groups = new FeatureGroup[] { FeatureGroup.Feat, FeatureGroup.CombatFeat };
            boar_ferocity.Groups = boar_style.Groups;
            boar_shred.Groups = boar_style.Groups;

            HelperEA.AddFeats(library, boar_style, boar_ferocity, boar_shred);
            combat_styles.Add(boar_style);
            combat_styles.Add(boar_ferocity);
            combat_styles.Add(boar_shred);
        }

        // known issues:
        // - wolf savage triggers once per round instead of using a swift action; it's much easier to code that way
        public static void createWolfStyle()
        {
            /*
            Wolf Style (Combat, Style)
            While in this style, you hamper foes that turn their backs on you.
            Prerequisites: Wis 13, Improved Unarmed Strike, Knowledge (nature) 3 ranks.
            Benefit: While using this style, whenever you deal at least 10 points of damage to a foe with an attack of opportunity, that foe’s base speed decreases by 5 feet until the end of its next turn. For every 10 points of damage your attack deals beyond 10, the foe’s base speed decreases by an additional 5 feet. If the penalty meets or exceeds the total base speed of the foe, you can attempt to trip the foe as a free action after the attack of opportunity is resolved.

            Wolf Trip (Combat, Style)
            You have studied the manner in which wolves bring down their prey.
            Prerequisites: Wis 15, Improved Unarmed Strike, Wolf Style, Knowledge (nature) 6 ranks.
            Benefit: While using Wolf Style, you gain a +2 bonus when you attempt a trip combat maneuver.

            Wolf Savage (Combat, Style)
            You savage your foes so badly that they can become supernaturally disfigured.
            Prerequisites: Wis 17, Improved Unarmed Strike, Wolf Style, Wolf Trip, Knowledge (nature) 9 ranks.
            Benefit: While using Wolf Style, once per round, when you deal at least 10 points of damage to a prone opponent with a natural weapon or an unarmed strike, you can savage that creature. When you do, your opponent must succeed at a Fortitude save (DC = 10 + half your character level + your Wisdom modifier). If the target fails the saving throw, it takes 1d4 Constitution damage.
            */

            var icon = Helper.Image2Sprite.Create("wolf.png");

            var speed_debuff = HelperEA.CreateBuff(
                "WolfStyleSpeedDebuff",
                "Wolf Style Movement Debuff",
                "-5 feet speed",
                Guid.i.Reg("89564ba513894e57b845307f8e62844d"),
                null,
                Contexts.NullPrefabLink,
                Helper.Create<BuffMovementSpeed>( a => {
                    a.Descriptor = ModifierDescriptor.UntypedStackable;
                    a.Value = -5;
                })
            );
            speed_debuff.Stacking = StackingType.Stack;
            speed_debuff.m_Flags(HiddenInUi: true);
            var add_initiatorattack = Helper.Create<AddInitiatorAttackDamageThreshold>( a => {
                a.CheckCategory = true;
                a.CheckForNaturalWeapon = true;
                a.DamageThreshold = 10;
                a.RepeatActions = true;
                a.OnlyOnAttackOfOpportunity = true;
                a.Action = Helper.CreateActionList(
                    HelperEA.CreateApplyBuff(speed_debuff, Contexts.Duration1Round, false, false),
                    HelperEA.CreateConditional(Helper.ToArray<Condition>(Helper.Create<ContextConditionMovespeed>(b => b.Speed = GameConsts.MinUnitSpeedMps),
                                                                         Helper.Create<ContextConditionHasUnitCondition>(c => {c.Condition = UnitCondition.Prone; c.Not = true;})),
                                       ifTrue: Helper.CreateContextActionCombatManeuver(CombatManeuver.Trip).ObjToArray())
                );
            });

            var wolfstyle_buff = HelperEA.CreateBuff(
                "WolfStyleBuff",
                "Wolf Style",
                "While in this style, you hamper foes that turn their backs on you.",
                Guid.i.Reg("86a19898ef7f4df8b9bf94414f90e55c"),
                icon,
                Contexts.NullPrefabLink,
                add_initiatorattack //slow if damage >= 10, trip if speed == minspeed
            );
            var wolfstyle_activatable = HelperEA.CreateActivatableAbility(
                "WolfStyleActivatable",
                "Wolf Style",
                wolfstyle_buff.Description,
                Guid.i.Reg("6341b762347948b6939c39285326407b"),
                icon,
                wolfstyle_buff,
                AbilityActivationType.Immediately,
                CommandType.Swift,
                null
            );
            wolfstyle_activatable.Group = ActivatableAbilityGroup.CombatStyle;
            var wolf_style = HelperEA.CreateFeature(
                "WolfStyle",
                "Wolf Style",
                "While using this style, whenever you deal at least 10 points of damage to a foe with an attack of opportunity, that foe’s base speed decreases by 5 feet until the end of its next turn. For every 10 points of damage your attack deals beyond 10, the foe’s base speed decreases by an additional 5 feet. If the penalty meets or exceeds the total base speed of the foe, you can attempt to trip the foe as a free action after the attack of opportunity is resolved.",
                Guid.i.Reg("3e744fbe64dd4d45ad137820ccfd5b87"),
                icon,
                FeatureGroup.Feat,
                preq_IUS,
                HelperEA.PrerequisiteStatValue(StatType.Wisdom, 13),
                HelperEA.PrerequisiteStatValue(StatType.SkillLoreNature, 3),
                HelperEA.CreateAddFact(wolfstyle_activatable)
            );

            var wolftrip_buff = HelperEA.CreateBuff(
                "WolfTripBuff",
                "Wolf Trip",
                "DESC",
                Guid.i.Reg("99146e0bb3154f86aa2f55a51f59979d"),
                icon,
                Contexts.NullPrefabLink,
                HelperEA.CreateManeuverBonus(CombatManeuver.Trip, 2)
            );
            wolftrip_buff.m_Flags(HiddenInUi: true);
            var wolf_trip = HelperEA.CreateFeature(
                "WolfTrip",
                "Wolf Trip",
                "While using Wolf Style, you gain a +2 bonus when you attempt a trip combat maneuver.",
                Guid.i.Reg("445e5601d4b44da482284785d93e8a84"),
                icon,
                FeatureGroup.Feat,
                preq_IUS,
                HelperEA.PrerequisiteStatValue(StatType.Wisdom, 15),
                HelperEA.PrerequisiteStatValue(StatType.SkillLoreNature, 6),
                HelperEA.PrerequisiteFeature(wolf_style)
            );

            var wolfsavage_buff = HelperEA.CreateBuff(
                "WolfSavageBuff",
                "Wolf Savage",
                "DESC",
                Guid.i.Reg("9372a8110dba4ff49c2098954978adcf"),
                icon,
                Contexts.NullPrefabLink,
                Helper.Create<WolfSavage>()
            );
            wolfsavage_buff.m_Flags(HiddenInUi: true);
            var wolf_savage = HelperEA.CreateFeature(
                "WolfSavage",
                "Wolf Savage",
                "While using Wolf Style, once per round, when you deal at least 10 points of damage to a prone opponent with a natural weapon or an unarmed strike, you can savage that creature. When you do, your opponent must succeed at a Fortitude save (DC = 10 + half your character level + your Wisdom modifier). If the target fails the saving throw, it takes 1d4 Constitution damage.",
                Guid.i.Reg("f2d8d4adc18343b68713cbadca94f5af"),
                icon,
                FeatureGroup.Feat,
                preq_IUS,
                HelperEA.PrerequisiteStatValue(StatType.Wisdom, 17),
                HelperEA.PrerequisiteStatValue(StatType.SkillLoreNature, 9),
                HelperEA.PrerequisiteFeature(wolf_style),
                HelperEA.PrerequisiteFeature(wolf_trip)
            );

            wolfstyle_buff.AddComponents(
                Helper.CombatStyleHelper(wolf_trip, wolftrip_buff),
                Helper.CombatStyleHelper(wolf_savage, wolfsavage_buff)
            );

            wolf_style.Groups = new FeatureGroup[] { FeatureGroup.Feat, FeatureGroup.CombatFeat };
            wolf_trip.Groups = wolf_style.Groups;
            wolf_savage.Groups = wolf_style.Groups;

            HelperEA.AddFeats(library, wolf_style, wolf_trip, wolf_savage);
            combat_styles.Add(wolf_style);
            combat_styles.Add(wolf_trip);
            combat_styles.Add(wolf_savage);
        }

        //Deadhand Style: shaken; HP; inflict negative levels

        private static void loadStyles()
        {
            combat_styles.Add(library.Get<BlueprintFeature>("0c17102f650d9044290922b0fad9132f"));//CraneStyleFeat
            combat_styles.Add(library.Get<BlueprintFeature>("af0aae1b973114f47a19ea532237b5fc"));//CraneStyleWingFeat
            combat_styles.Add(library.Get<BlueprintFeature>("59eb2a5507975244c893402d582bf77b"));//CraneStyleRiposteFeat
            combat_styles.Add(library.Get<BlueprintFeature>("87ec6541cddfa394ab540dd13399d319"));//DragonStyle
            combat_styles.Add(library.Get<BlueprintFeature>("2a681cb9fcaab664286cb36fff761245"));//DragonFerocity
            combat_styles.Add(library.Get<BlueprintFeature>("3fca938ad6a5b8348a8523794127c5bc"));//DragonRoarFeature
            combat_styles.Add(library.Get<BlueprintFeature>("c36562b8e7ae12d408487ba8b532d966"));//PummelingStyle
            combat_styles.Add(library.Get<BlueprintFeature>("bdf58317985383540920c723db07aa3b"));//PummelingBully
            combat_styles.Add(library.Get<BlueprintFeature>("c5a39c8f1a2d6824ca565e6c1e4075a5"));//PummelingCharge

            combat_styles.Add(library.Get<BlueprintFeature>("d3b85be5b7d340b8ab41a6aff9c0fd62"));//LinnormStyleToggleAbilityFeature
            combat_styles.Add(library.Get<BlueprintFeature>("cac51abfb9b142cba0c21feb53ff38ff"));//LinnormVengeanceFeature
            combat_styles.Add(library.Get<BlueprintFeature>("20ac5f7149d642e9be1f0d85ba84194f"));//LinnormWrathFeature
            
            combat_styles.Add(library.Get<BlueprintFeature>("a3899fa598a7454d981cc0486280d855"));//JabbingStyleToggleAbilityFeature
            combat_styles.Add(library.Get<BlueprintFeature>("7acfde42495e42dfbeeb3a54b459f36f"));//JabbingDancerFeature
            combat_styles.Add(library.Get<BlueprintFeature>("6bf8dd704c104f7fb55ca8405a7ac0cf"));//JabbingMasterFeature
        }

        // known issues:
        // - wildcards can be exploited for some styles to have more active that you should have, when turned on/off in the right order (cannot fix)
        // - wildcards can be expanded, even if the actual feature wasn't picked yet
        public static void createMasterOfManyStyles()
        {
            /*
            Master of Many Styles

            The master of many styles is a collector. For every move, he seeks a counter. For every style, he has a riposte. Ultimately, he seeks perfection through the fusion of styles.
            Bonus Feat

            At 1st level, 2nd level, and every four levels thereafter, a master of many styles may select a bonus style feat or the Elemental Fist feat. He does not need to meet the prerequisites of that feat, except the Elemental Fist feat. Starting at 6th level, a master of many styles can choose to instead gain a wildcard style slot. Whenever he enters one or more styles, he can spend his wildcard style slots to gain feats in those styles’ feat paths (such as Earth Child Topple) as long as he meets the prerequisites. Each time he changes styles, he can also change these wildcard style slots.
            This ability replaces a monk’s standard bonus feats.

            Fuse Style (Ex)
            At 1st level, a master of many styles can fuse two of the styles he knows into a more perfect style. The master of many styles can have two style feat stances active at once. Starting a stance provided by a style feat is still a swift action, but when the master of many styles switches to another style feat, he can choose one style whose stance is already active to persist. He may only have two style feat stances active at a time.
            At 8th level, the master of many styles can fuse three styles at once. He can have the stances of three style feats active at the same time. He gains a bonus on attack rolls equal to the number of styles whose stances he currently has active. Furthermore, he can enter up to three stances as a swift action.
            At 15th level, the master of many styles can fuse four styles at once. He can have the stances of four style feats active at the same time. Furthermore, he can enter up to four stances as a free action by spending 1 point from his ki pool.
            This ability replaces flurry of blows.

            Perfect Style (Ex)
            At 20th level, a master of many styles can have the stances of five style feats active at once, and can change those stances as a free action.
            This ability replaces perfect self.
            */

            // get the all the base game styles and from CotW
            loadStyles();

            archetype_MOMS = Helper.Create<BlueprintArchetype>(a =>
            {
                a.name = "MasterOfManyStylesArchetype";
                a.LocalizedName = HelperEA.CreateString($"{a.name}.Name", "Master Of Many Styles");
                a.LocalizedDescription = HelperEA.CreateString($"{a.name}.Description", "The master of many styles is a collector. For every move, he seeks a counter. For every style, he has a riposte. Ultimately, he seeks perfection through the fusion of styles.");
            });

            Access.set_ParentClass(archetype_MOMS, monk_class);
            library.AddAsset(archetype_MOMS, Guid.i.Reg("5205f152da3c40e3a258c574d528d58f"));

            // remove features
            var bonusfeat1 = library.Get<BlueprintFeatureSelection>("ac3b7f5c11bce4e44aeb332f66b75bab");//MonkBonusFeatSelectionLevel1
            var bonusfeat10 = library.Get<BlueprintFeatureSelection>("1051170c612d5b844bfaa817d6f4cfff");//MonkBonusFeatSelectionLevel10
            archetype_MOMS.RemoveFeatures = new LevelEntry[] {
                HelperEA.LevelEntry(1, library.Get<BlueprintFeature>("fd99770e6bd240a4aab70f7af103e56a"),//MonkFlurryOfBlowstUnlock
                                       bonusfeat1),
                HelperEA.LevelEntry(2, bonusfeat1),
                HelperEA.LevelEntry(6, library.Get<BlueprintFeature>("b993f42cb119b4f40ac423ae76394374")),//MonkBonusFeatSelectionLevel6
                HelperEA.LevelEntry(10, bonusfeat10),
                HelperEA.LevelEntry(11, library.Get<BlueprintFeature>("a34b8a9fcc9024b42bacfd5e6b614bfa")),//MonkFlurryOfBlowstLevel11Unlock
                HelperEA.LevelEntry(14, bonusfeat10),
                HelperEA.LevelEntry(18, bonusfeat10),
                HelperEA.LevelEntry(20, library.Get<BlueprintFeature>("3854f693180168a4980646aee9494c72"))//KiPerfectSelfFeature
            };

            // new Wildcard feature
            // - this creates an activatable ability for each subsequent feat in style chains (not the style itself)
            // - activatable is non-active, if prerequisite are not met
            // - since the selection ignores prerequisites, the expand to the activatable group may be picked early; not good
            List<BlueprintActivatableAbility> wildcardlist = new List<BlueprintActivatableAbility>();
            for (int i=0; i < combat_styles.Count; i++)
            {
                if (i%3 == 0) continue;
                var ab = wildcardslots(combat_styles[i]);
                if (ab != null) wildcardlist.Add(ab);
            }
            string description_wildcard = "Whenever he enters one or more styles, he can spend his wildcard style slots to gain feats in those styles’ feat paths (such as Snake Fang) as long as he meets the prerequisites. Each time he changes styles, he can also change these wildcard style slots.";
            var wildcard = HelperEA.CreateFeature(
                "MasterOfManyStylesWildCardSlot",
                "Wildcard Slot",
                "Pick this before you expand your Wildcard Slots!\n"+description_wildcard,
                Guid.i.Reg("f2f97aa042a0445e94aca7257368d3e5"),
                null,
                FeatureGroup.None,
                HelperEA.CreateAddFacts(wildcardlist.ToArray())
            );
            var wildcard_extra1 = HelperEA.CreateFeature(
                "MasterOfManyStylesWildCardSlotExtra1",
                "Wildcard Slot Expand",
                description_wildcard,
                Guid.i.Reg("9a45807387254c3abed6742a7742275f"),
                null,
                FeatureGroup.None,
                HelperEA.CreateIncreaseActivatableAbilityGroupSize(MOMS_wildcardgroup)
            ); 
            var wildcard_extra2 = library.CopyAndAdd(wildcard_extra1, "MasterOfManyStylesWildCardSlotExtra2", "116af8c506094038bdf6df92722eadc1");
            var wildcard_extra3 = library.CopyAndAdd(wildcard_extra1, "MasterOfManyStylesWildCardSlotExtra3", "dbe3668786484dfd9a1030536e97084d");

            var bonusfeat_MOMS = HelperEA.CreateFeatureSelection(
                "MasterOfManyStylesBonusFeatSelection",
                "Monk Bonus Feat",
                "At 1st level, 2nd level, and every four levels thereafter, a master of many styles may select a bonus style feat. He does not need to meet the prerequisites of that feat. Starting at 6th level, a master of many styles can choose to instead gain a wildcard style slot. Whenever he enters one or more styles, he can spend his wildcard style slots to gain feats in those styles’ feat paths (such as Snake Fang) as long as he meets the prerequisites. Each time he changes styles, he can also change these wildcard style slots.",
                Guid.i.Reg("3b09dd2247e34a45a37b4be5007601df"),
                null,
                FeatureGroup.None
            );
            bonusfeat_MOMS.IgnorePrerequisites = true;
            bonusfeat_MOMS.IsClassFeature = true;
            bonusfeat_MOMS.AllFeatures = combat_styles.ToArray();

            var bonusfeat_MOMS6 = library.CopyAndAdd(bonusfeat_MOMS, "MasterOfManyStylesBonusFeatSelection6", Guid.i.Reg("88db5f332ab84d6abf864ab02f6fa65c"));
            Helper.AppendAndReplace(ref bonusfeat_MOMS6.AllFeatures, wildcard, wildcard_extra1, wildcard_extra2, wildcard_extra3);

            // extra combat style slots
            var extra_style_slot1 = HelperEA.CreateFeature("MasterOfManyStylesGroupInc1", "Extra Combat Style", "You can enter an additional combat style simultaneously.", "c6eff162b97740af82746152a01553f3", null, FeatureGroup.None, HelperEA.CreateIncreaseActivatableAbilityGroupSize(ActivatableAbilityGroup.CombatStyle));
            var extra_style_slot2 = library.CopyAndAdd(extra_style_slot1, "MasterOfManyStylesGroupInc2", Guid.i.Reg("3046db1d4a9b4b7bb2d57969465d30bc"));
            var extra_style_slot3 = library.CopyAndAdd(extra_style_slot1, "MasterOfManyStylesGroupInc3", Guid.i.Reg("716f2294a69845938b50615537f89a4f"));
            var extra_style_slot4 = library.CopyAndAdd(extra_style_slot1, "MasterOfManyStylesGroupInc4", Guid.i.Reg("92ab8576d8d54c988b14530f061778e5"));

            archetype_MOMS.AddFeatures = new LevelEntry[] {
                HelperEA.LevelEntry(1, bonusfeat_MOMS,
                                      extra_style_slot1),
                HelperEA.LevelEntry(2, bonusfeat_MOMS),
                HelperEA.LevelEntry(6, bonusfeat_MOMS6),
                HelperEA.LevelEntry(8, extra_style_slot2),
                HelperEA.LevelEntry(10, bonusfeat_MOMS6),
                HelperEA.LevelEntry(14, bonusfeat_MOMS6),
                HelperEA.LevelEntry(15, extra_style_slot3),
                HelperEA.LevelEntry(18, bonusfeat_MOMS6),
                HelperEA.LevelEntry(20, extra_style_slot4)
            };

            Helper.AppendAndReplace(ref monk_class.Archetypes, archetype_MOMS);
        }

        private static BlueprintActivatableAbility wildcardslots(BlueprintFeature style)
        {
            if (style == null) return null;

            string name = "WildcardBuff" + style.name;
            var buff = HelperEA.CreateBuff(
                name,
                "Wildcard: " + style.Name,
                style.Description,
                Guid.i.Get(name),
                style.Icon,
                Contexts.NullPrefabLink
                //,Helpers.Create<AddFactsFromCaster>(a => a.Facts = style.ToArray())
                //Helpers.CreateAddFact(style),
            );

            string name2 = "Wildcard" + style.name;
            var activatable = HelperEA.CreateActivatableAbility(
                name2,
                buff.Name,
                style.Description,
                Guid.i.Get(name2),
                style.Icon,
                buff,
                AbilityActivationType.Immediately,
                CommandType.Free,
                null,
                Helper.Create<ActivatableRestrictionPrerequisite>( a => a.Feature = style)
                //,Helpers.Create<Kingmaker.UnitLogic.ActivatableAbilities.Restrictions.RestrictionHasFact>( a => { a.Feature = style; a.Not = true; } )
            );
            activatable.Group = MOMS_wildcardgroup;
            activatable.DeactivateImmediately = true;

            buff.AddComponent(Helper.Create<AddFactsSafe>( a => { a.Facts = style.ObjToArray(); a.Activatable = activatable; }));

            return activatable;
        }

        #endregion

        #region Ki Powers

        public static void modKiPowers(bool enabled = true)
        {
            if (!enabled) return;

            var diamond_soul = library.Get<BlueprintFeature>("01182bcee8cb41640b7fa1b1ad772421");//KiDiamondSoulFeature
            diamond_soul.GetComponent<PrerequisiteClassLevel>().Level = 12;
        }

        public static void createKiLeech()
        {
            var restore_ki = Helper.CreateContextActionRestoreResource(library.Get<BlueprintAbilityResource>("9d9c90a9a1f52d04799294bf91c80a82"), Contexts.ValueOne, true);//KiPowerResource

            var ki_leech = HelperEA.CreateFeature(
                "KiLeechFeature",
                "Ki Power: Ki Leech",
                "You place your spirit in a receptive state so when you confirm a critical hit against a living enemy or reduce a living enemy to 0 or fewer hit points, you can steal some of that creature’s ki. This replenishes 1 point of ki. This does not allow you to exceed your ki pool’s maximum.",
                Guid.i.Reg("36c5fb7ac2164758ab13b196e9183258"),
                library.Get<BlueprintAbility>("32280b137ca642c45be17e2d92898758").Icon,
                FeatureGroup.KiPowers,
                HelperEA.PrerequisiteClassLevel(monk_class, 10),
                HelperEA.CreateAddInitiatorAttackWithWeaponTrigger(Helper.CreateActionList(restore_ki), critical_hit: true),
                HelperEA.CreateAddInitiatorAttackWithWeaponTrigger(Helper.CreateActionList(restore_ki), reduce_hp_to_zero: true)
            );
            ki_leech.Groups = new FeatureGroup[] { FeatureGroup.KiPowers, FeatureGroup.ScaledFistKiPowers };
            ki_leech.ReapplyOnLevelUp = true;
            ki_leech.IsClassFeature = true;

            Helper.AppendAndReplace(ref ki_powers.AllFeatures, ki_leech);
        }

        // known issues:
        // - works with all weapons, not just unarmed strikes
        public static void createOneTouch()
        {
            // Kingmaker.Designers.Mechanics.Facts.AttackTypeChange
            // RuleAttackRole.AttackType

            var icon = library.Get<BlueprintAbility>("a970537ea2da20e42ae709c0bb8f793f").Icon;

            var one_touch_buff = HelperEA.CreateBuff(
                "OneTouchKiPowerBuff",
                "One Touch Buff",
                "DESC",
                Guid.i.Reg("fded2446731e4079bf26b1262ab1d2d2"),
                icon,
                Contexts.NullPrefabLink,
                Helper.Create<AttackTypeChange>(a => { a.NeedsWeapon = true; a.NewType = AttackType.Touch; }),//same as DimensionStrikeBuff
                HelperEA.CreateContextWeaponTypeDamageBonus(Contexts.ValueRank, weapon_unarmed),
                HelperEA.CreateContextRankConfig(ContextRankBaseValueType.ClassLevel, ContextRankProgression.Div2, classes: monk_class.ObjToArray())
            );
            one_touch_buff.m_Flags(HiddenInUi: true);

            var one_touch_ab = HelperEA.CreateAbility(
                "OneTouchKiPower",
                "Ki Power: One Touch",
                "As a standard action, a monk with this power can make an unarmed strike against a foe as a touch attack. He adds 1/2 his monk level as a bonus on the damage roll. A monk must be at least 12th level before selecting this ki power.",
                Guid.i.Reg("b71a3d7b616f42908a88e4887c92f596"),
                icon,
                AbilityType.Extraordinary,
                CommandType.Standard,
                AbilityRange.Touch,
                "",
                "",
                HelperEA.CreateRunActions(
                    HelperEA.CreateApplyBuff(one_touch_buff, Contexts.Duration1Round, false, false, true, true),
                    Helper.Create<ContextActionMeleeAttack>())
            );
            HelperEA.SetMiscAbilityParametersTouchHarmful(one_touch_ab, false, UnitAnimationActionCastSpell.CastAnimationStyle.Special, CastAnimationStyle.CastActionSpecialAttack);

            var one_touch = HelperEA.CreateFeature(
                "OneTouchKiPowerFeature",
                "Ki Power: One Touch",
                one_touch_ab.Description,
                Guid.i.Reg("a98e18b40bfa4e5399ea52ee05efbf3a"),
                icon,
                FeatureGroup.KiPowers,
                HelperEA.PrerequisiteClassLevel(monk_class, 12),
                HelperEA.CreateAddFact(one_touch_ab)
            );
            one_touch.Groups = new FeatureGroup[] { FeatureGroup.KiPowers, FeatureGroup.ScaledFistKiPowers };

            Helper.AppendAndReplace(ref ki_powers.AllFeatures, one_touch);
        }

        #endregion

        #region TWF

        private static bool CheckBuff(UnitEntityData unit)
        {
            foreach (Buff buff in unit.Buffs)
                if (buff.Blueprint == DualUnarmedBuff)
                    return true;
            return false;
        }

        private static bool CheckFact(UnitEntityData unit, BlueprintFeature fact)
        {
            return unit.Descriptor.HasFact(fact);
        }

        //to be attached to TwoWeaponFightingBasicMechanics
        [AllowedOn(typeof(BlueprintUnitFact))]
        public class TwoWeaponFightingAttacks : RuleInitiatorLogicComponent<RuleCalculateAttacksCount>
        {
            public override void OnEventAboutToTrigger(RuleCalculateAttacksCount evt)
            {
                if (evt.Initiator.Body.PrimaryHand.HasWeapon
                    && evt.Initiator.Body.SecondaryHand.HasWeapon
                    && evt.Initiator.Body.SecondaryHand.Weapon.Blueprint.IsUnarmed
                    && CheckBuff(evt.Initiator))
                {
                    //cannot use flurry and TWF at the same time, so we remove the extra attacks
                    if (CheckFact(evt.Initiator, flurry1))
                    {
                        evt.PrimaryHand.MainAttacks--;
                        if (CheckFact(evt.Initiator, flurry2))
                        {
                            evt.PrimaryHand.MainAttacks--;
                        }
                    }

                    // int TWF_rank = evt.Initiator.Descriptor.GetFact(TWF).GetRank();
                    // evt.SecondaryHand.MainAttacks++;
                    // if (TWF_rank > 2)
                    //     evt.SecondaryHand.PenalizedAttacks++;
                    // if (TWF_rank > 3)
                    //     evt.SecondaryHand.PenalizedAttacks++;

                    evt.SecondaryHand.PenalizedAttacks++;

                    if (base.Fact.GetRank() > 2)
                        evt.SecondaryHand.PenalizedAttacks++;

                    if (base.Fact.GetRank() > 3)
                        evt.SecondaryHand.PenalizedAttacks++;
                }
            }

            public override void OnEventDidTrigger(RuleCalculateAttacksCount evt)
            {
            }
        }

        [AllowedOn(typeof(BlueprintUnitFact))]
        public class TwoWeaponFightingAttackPenalty : RuleInitiatorLogicComponent<RuleCalculateAttackBonusWithoutTarget>
        {
            public override void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
            {
                ItemEntityWeapon primaryWeapon = evt.Initiator.Body.PrimaryHand.MaybeWeapon;
                ItemEntityWeapon secondaryWeapon = evt.Initiator.Body.SecondaryHand.MaybeWeapon;
                if (evt.Weapon != null
                    && primaryWeapon != null
                    && secondaryWeapon != null
                    //&& !primaryWeapon.Blueprint.IsNatural
                    && secondaryWeapon.Blueprint.IsUnarmed
                    //&& primaryWeapon != evt.Initiator.Body.EmptyHandWeapon
                    //&& secondaryWeapon != evt.Initiator.Body.EmptyHandWeapon
                    && (primaryWeapon == evt.Weapon || secondaryWeapon == evt.Weapon)
                    && CheckBuff(evt.Initiator))
                {
                    int rank = base.Fact.GetRank();
                    int pen_prim = (rank <= 1) ? -4 : -2;
                    int pen_sec = (rank <= 1) ? -8 : -2;
                    int penalty = evt.Weapon == primaryWeapon ? pen_prim : pen_sec;
                    UnitPartWeaponTraining unitPartWeaponTraining = base.Owner.Get<UnitPartWeaponTraining>();
                    bool hasEffortlessDualWielding = base.Owner.State.Features.EffortlessDualWielding && unitPartWeaponTraining != null && unitPartWeaponTraining.IsSuitableWeapon(secondaryWeapon);
                    if (!secondaryWeapon.Blueprint.IsLight && !primaryWeapon.Blueprint.Double && !secondaryWeapon.IsShield && !hasEffortlessDualWielding)
                    {
                        penalty += -2;
                    }
                    evt.AddBonus(penalty, base.Fact);
                }
            }

            public override void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt)
            {
            }
        }

        [AllowedOn(typeof(BlueprintUnitFact))]
        public class TwoWeaponFightingDamagePenalty : RuleInitiatorLogicComponent<RuleCalculateWeaponStats>
        {
            public override void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
            {
                ItemEntityWeapon primaryWeapon = evt.Initiator.Body.PrimaryHand.MaybeWeapon;
                ItemEntityWeapon secondaryWeapon = evt.Initiator.Body.SecondaryHand.MaybeWeapon;
                if (evt.Weapon != null
                    && primaryWeapon != null
                    && secondaryWeapon != null
                    //&& !primaryWeapon.Blueprint.IsNatural
                    && secondaryWeapon.Blueprint.IsUnarmed
                    //&& primaryWeapon != evt.Initiator.Body.EmptyHandWeapon
                    //&& secondaryWeapon != evt.Initiator.Body.EmptyHandWeapon
                    && secondaryWeapon == evt.Weapon
                    && !evt.Initiator.Descriptor.State.Features.DoubleSlice
                    && CheckBuff(evt.Initiator))
                {
                    evt.SecondaryWeapon = true;
                }
            }

            public override void OnEventDidTrigger(RuleCalculateWeaponStats evt)
            {
            }
        }
        
        #endregion

        #region Patches

        // TODO: Implement after beta
        // - Dual Wielding with Unarmed Strikes(activatable);
        //[HarmonyLib.HarmonyPatch(typeof(CharSAttack), nameof(CharSAttack.SetupMainAttacks))]
        public static class TWF_Patch
        {
            public static bool Prefix(UnitDescriptor unit, CharSAttack __instance)
            {
                List<UIUtilityItem.AttackData> list = new List<UIUtilityItem.AttackData>
                {
                    new UIUtilityItem.AttackData(),
                    new UIUtilityItem.AttackData()
                };
                ItemEntityWeapon maybeWeapon = unit.Body.PrimaryHand.MaybeWeapon;
                ItemEntityWeapon maybeWeapon2 = unit.Body.SecondaryHand.MaybeWeapon;
                ItemEntityShield maybeShield = unit.Body.SecondaryHand.MaybeShield;
                bool flag = maybeWeapon != null && maybeWeapon.HoldInTwoHands;
                bool flag2 = maybeWeapon2 != null && maybeWeapon2.HoldInTwoHands;
                bool flag3 = false;// maybeWeapon != null && maybeWeapon.Blueprint.IsUnarmed;
                bool flag4 = false;// maybeWeapon2 != null && maybeWeapon2.Blueprint.IsUnarmed;
                bool flag5 = !flag2 && maybeWeapon != null && (!flag3 || flag4 || maybeWeapon2 == null);
                bool flag6 = !flag && maybeWeapon2 != null && !flag4;
                if (flag5)
                {
                    list[0] = UIUtilityItem.GetAttackParametersEntity(maybeWeapon, unit, true);
                }
                int num = (!flag5) ? 0 : 1;
                if (flag6)
                {
                    list[num] = UIUtilityItem.GetAttackParametersEntity(maybeWeapon2, unit, false);
                    if (maybeShield != null)
                    {
                        list[num].Icon = maybeShield.Icon;
                    }
                }
                if (unit.Body.SecondaryHand.MaybeShield != null && (!flag6 || num == 0))
                {
                    list[1] = UIUtilityItem.GetShieldParameters(unit.Body.SecondaryHand.MaybeShield, unit);
                }
                __instance.SetupAttackComponent(list, false, unit);

                return false;
            }
        }

        //[HarmonyLib.HarmonyPatch(typeof(RuleCalculateAttacksCount), nameof(RuleCalculateAttacksCount.OnTrigger))]
        public static class TWF2_Patch
        {
            public static IEnumerable<HarmonyLib.CodeInstruction> __Transpiler(IEnumerable<HarmonyLib.CodeInstruction> instr)
            {
                List<HarmonyLib.CodeInstruction> list = instr.ToList();

                //MethodBase original = typeof(BlueprintItemWeapon).GetMethod(nameof(BlueprintItemWeapon.IsUnarmed));

                MethodInfo replacement = typeof(TWF2_Patch).GetMethod(nameof(NullReplacement), BindingFlags.Static | BindingFlags.NonPublic);
                
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].operand != null)
                        Main.DebugLog("TWF2_Patch:" + i + ":" + (list[i].operand?.ToString() ?? "null") + ":" + list[i].operand.GetType().ToString());
                    if (list[i].opcode == OpCodes.Callvirt && list[i].operand?.ToString() == "Boolean get_IsUnarmed()")
                    {
                        Main.DebugLog("TWF2_Patch at " + i);
                        list[i].operand = replacement;
                    }
                }

                return list;
            }

            static bool NullReplacement(object something)
            {
                return false;
            }

            public static bool Prefix(RulebookEventContext context, RuleCalculateAttacksCount __instance)
            {
                if (!__instance.Initiator.Descriptor.Buffs.HasFact(DualUnarmedBuff))    // only mod this if the buff is active
                    return true;

                HandSlot primaryHand = __instance.Initiator.Body.PrimaryHand;
                HandSlot secondaryHand = __instance.Initiator.Body.SecondaryHand;
                BlueprintItemWeapon primaryWeapon = primaryHand.MaybeWeapon?.Blueprint;
                BlueprintItemWeapon secondaryWeapon;

                if (secondaryHand.MaybeShield != null)
                {
                    if (__instance.Initiator.Descriptor.State.Features.ShieldBash)
                        secondaryWeapon = secondaryHand.MaybeShield.WeaponComponent?.Blueprint;
                    else
                        secondaryWeapon = null;
                }
                else
                    secondaryWeapon = secondaryHand.MaybeWeapon?.Blueprint;

                if (primaryHand.MaybeWeapon == null || secondaryHand.MaybeWeapon == null   // only mod this if two weapons are equipped, neither are two-handed, and at least one unarmed
                    || primaryWeapon.IsTwoHanded || secondaryWeapon.IsTwoHanded
                    || (!primaryWeapon.IsUnarmed && !secondaryWeapon.IsUnarmed))
                    return true;

                int num = __instance.Initiator.Stats.BaseAttackBonus;
                int val = Math.Min(Math.Max(0, num / 5 - ((num % 5 != 0) ? 0 : 1)), 3);

                __instance.PrimaryHand.MainAttacks++;
                __instance.PrimaryHand.PenalizedAttacks += Math.Max(0, val);

                __instance.SecondaryHand.MainAttacks++;

                //bool flag = primaryHand.MaybeWeapon != null && primaryHand.MaybeWeapon.HoldInTwoHands;
                //if ((secondaryHand.MaybeWeapon == null || !secondaryHand.MaybeWeapon.HoldInTwoHands) && primaryWeapon && (!primaryWeapon.IsUnarmed || !secondaryWeapon || secondaryWeapon.IsUnarmed))
                //{
                //    __instance.PrimaryHand.MainAttacks++;
                //    if (!primaryWeapon.IsNatural || __instance.Initiator.Descriptor.State.Features.IterativeNaturalAttacks || __instance.ForceIterativeNaturealAttacks || primaryWeapon.IsUnarmed)
                //    {
                //        __instance.PrimaryHand.PenalizedAttacks += Math.Max(0, val);
                //    }
                //}
                //if (!flag && secondaryWeapon && (!secondaryWeapon.IsUnarmed || !primaryWeapon))
                //{
                //    __instance.SecondaryHand.MainAttacks++;
                //}

                return false;
            }

        }

        //[HarmonyLib.HarmonyPatch(typeof(RuleCalculateAttacksCount), nameof(RuleCalculateAttacksCount.OnTrigger))]
        public static class Test_Patch  // TODO: remove patch
        {
            static void Postfix(RulebookEventContext context, RuleCalculateAttacksCount __instance)
            {
                Main.DebugLog($"Attack calc: PrimaryHand={__instance.Initiator.Body.PrimaryHand.MaybeWeapon?.Blueprint.Name}, SecondaryHand={__instance.Initiator.Body.SecondaryHand.MaybeWeapon?.Blueprint.Name}");


            }
        }

        #endregion
    }
}