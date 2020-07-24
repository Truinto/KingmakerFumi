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
using CallOfTheWild;
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
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;
using FumisCodex.NewComponents;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using static Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell;
using Guid = FumisCodex.GuidManager;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.ElementsSystem;

namespace FumisCodex
{
    class Hexcrafter
    {
        static LibraryScriptableObject library => Main.library;
        public static BlueprintCharacterClass magus = library.Get<BlueprintCharacterClass>("45a4607686d96a1498891b3286121780"); //magus class
        public static BlueprintFeatureSelection magus_arcana_selection = library.Get<BlueprintFeatureSelection>("e9dc4dfc73eaaf94aae27e0ed6cc9ada");

        public static BlueprintArchetype archetype;
        public static BlueprintFeature hexcrafter_spells;
        public static BlueprintFeature arcana_hexes;
        public static BlueprintFeatureSelection hex_arcana_selection;
        public static BlueprintFeatureSelection extra_hex;
        public static BlueprintSpellbook spellbook;

        //feats
        public static BlueprintFeature extra_arcana_feat;
        public static BlueprintFeature hexstrike_feat;

        //magus hex
        public static BlueprintFeature accursed_strike;
        public static BlueprintAbility[] accursed_strike_variants;

        //lists; available after attachHexes
        public static List<BlueprintFeature> hexes = new List<BlueprintFeature>();
        public static List<BlueprintFeature> hexes_major = new List<BlueprintFeature>();
        public static List<BlueprintFeature> hexes_grant = new List<BlueprintFeature>();
        public static List<BlueprintAbility> hexes_ability = new List<BlueprintAbility>();
        public static Dictionary<string, BlueprintAbility> hexes_offensive = new Dictionary<string, BlueprintAbility>();

        public static void createHexcrafter()
        {
            archetype = Helpers.Create<BlueprintArchetype>(a =>
            {
                a.name = "HexcrafterArchetype";
                a.LocalizedName = Helpers.CreateString($"{a.name}.Name", "Hexcrafter");
                a.LocalizedDescription = Helpers.CreateString($"{a.name}.Description", "A hexcrafter magus has uncovered the secret of using his arcane pool to recreate witch hexes. These magi can hex friend and foe, curse those they strike, and expand their spell selection to include many curses and harmful spells.");
            });

            Helpers.SetField(archetype, "m_ParentClass", magus);
            library.AddAsset(archetype, Guid.i.Reg("d8c2c968311942d19df6e352a97d8428"));

            //hex_engine = new HexEngine(new BlueprintCharacterClass[] { magus }, StatType.Intelligence, StatType.Charisma, archetype);

            attachHexes();
            createAccursedStrike();
            createHexArcana();
            createClassFeatures();

            // add curse spells to spellbook
            //var spell_search = library.GetAllBlueprints().OfType<BlueprintAbility>().Where(b => b.IsSpell && (b.SpellDescriptor & SpellDescriptor.Curse) != 0).ToArray(); //.Cast<BlueprintAbility>().ToArray()
            spellbook = library.CopyAndAdd<BlueprintSpellbook>(magus.Spellbook, "HexcrafterSpellbook", Guid.i.Reg("5d0c1159252d4f38962103f83b7e2483"));
            spellbook.SpellList = spellbook.SpellList.CreateCopy();   //Common.combineSpellLists("HexcrafterSpellList", spellbook.SpellList);
            library.Get<BlueprintAbility>("989ab5c44240907489aba0a8568d0603").AddToSpellList(spellbook.SpellList, 3);    //BestowCurse
            archetype.ReplaceSpellbook = spellbook;

            var spell_recall = library.Get<BlueprintFeature>("61fc0521e9992624e9c518060bf89c0f");
            var improved_spell_recall = library.Get<BlueprintFeature>("0ef6ec1c2fdfc204fbd3bff9f1609490");

            archetype.RemoveFeatures = new LevelEntry[] {
                Helpers.LevelEntry(4, spell_recall),
                Helpers.LevelEntry(11, improved_spell_recall)
            };

            archetype.AddFeatures = new LevelEntry[] {
                Helpers.LevelEntry(1, hexcrafter_spells, arcana_hexes),
                Helpers.LevelEntry(4, extra_hex),
                Helpers.LevelEntry(11, spell_recall)
            };

            magus.Progression.UIDeterminatorsGroup = magus.Progression.UIDeterminatorsGroup.AddToArray(hexcrafter_spells, arcana_hexes);
            magus.Progression.UIGroups = magus.Progression.UIGroups.AddToArray(Helpers.CreateUIGroup(extra_hex));
            magus.Archetypes = magus.Archetypes.AddToArray(archetype);
        }

        // attaching to witch class of CotW; this means class levels stack with each other in regards to effective hex levels
        // by RAW they should not stack, but it's much smoother to expand features (like hexstrike)
        static void attachHexes()
        {
            // Hexcrafter has no access to amplified_hex_feat

            //fix Split Hex working for Hexcrafter
            HexEngine.split_hex_feat.GetComponent<PrerequisiteClassLevel>().Group = Prerequisite.GroupType.Any;
            HexEngine.split_major_hex_feat.GetComponent<PrerequisiteClassLevel>().Group = Prerequisite.GroupType.Any;

            HexEngine.accursed_hex_feat.AddComponent(Common.createPrerequisiteArchetypeLevel(magus, archetype, 1, true));
            HexEngine.split_hex_feat.AddComponent(Common.createPrerequisiteArchetypeLevel(magus, archetype, 10, true));
            HexEngine.split_major_hex_feat.AddComponent(Common.createPrerequisiteArchetypeLevel(magus, archetype, 18, true));

            hexes_grant.Add(Witch.animal_servant);
            hexes_grant.Add(Witch.death_curse);
            hexes_grant.Add(Witch.lay_to_rest);
            hexes_grant.Add(Witch.life_giver);
            hexes_grant.Add(Witch.eternal_slumber);

            hexes_major.Add(Witch.major_ameliorating);
            hexes_major.Add(Witch.major_healing);
            hexes_major.Add(Witch.animal_skin);
            hexes_major.Add(Witch.agony);
            hexes_major.Add(Witch.beast_gift);
            hexes_major.Add(Witch.harrowing_curse);
            hexes_major.Add(Witch.ice_tomb);
            hexes_major.Add(Witch.regenerative_sinew);
            hexes_major.Add(Witch.retribution);
            hexes_major.Add(Witch.restless_slumber);

            hexes.Add(Witch.healing);
            hexes.Add(Witch.beast_of_ill_omen);
            hexes.Add(Witch.slumber_hex);
            hexes.Add(Witch.misfortune_hex);
            hexes.Add(Witch.fortune_hex);
            hexes.Add(Witch.flight_hex);
            hexes.Add(Witch.iceplant_hex);
            hexes.Add(Witch.murksight_hex);
            hexes.Add(Witch.ameliorating);
            hexes.Add(Witch.evil_eye);
            hexes.Add(Witch.summer_heat);
            hexes.Add(Witch.cackle);
            hexes.Add(Witch.ward);
            hexes.Add(Witch.swamps_grasp);
            hexes.AddRange(hexes_major);
            hexes.AddRange(hexes_grant);

            foreach (var feature in hexes)
            {
                //get abilities of all hexes for other functionalities
                foreach(var addfact in feature.GetComponents<AddFacts>())
                {
                    foreach (var fact in addfact.Facts)
                    {
                        if (fact is BlueprintAbility)
                        {
                            var variants = fact.GetComponent<AbilityVariants>();
                            if (variants == null)
                                hexes_ability.Add((BlueprintAbility)fact);
                            else
                                foreach (var variant in variants.Variants)
                                    hexes_ability.Add(variant);
                        }
                    }
                }
            }

            foreach (var ability in hexes_ability)
            {
                var ranks = ability.GetComponents<ContextRankConfig>();
                var param = ability.GetComponent<CallOfTheWild.NewMechanics.ContextCalculateAbilityParamsBasedOnClasses>();

                Main.DebugLog($"{ability.name} has {ranks.Count()} rank components and {(param ? "1":"0")} param component");

                foreach (var rank in ranks)
                {
                    if (rank.m_BaseValueType() == ContextRankBaseValueType.ClassLevel)
                    {
                        rank.m_BaseValueType(ContextRankBaseValueType.CasterLevel);
                    }

                    //ability.ReplaceComponent<ContextRankConfig>(rank.Convert(magus.ToArray(), archetype.ToArray()));
                }

                if (param)
                {
                    Helper.AppendAndReplace(ref param.CharacterClasses, magus);
                    Helper.AppendAndReplace(ref param.archetypes, archetype);
                }
            }

            foreach (var feature in hexes_major)
            {
                feature.AddComponent(Common.createPrerequisiteArchetypeLevel(magus, archetype, 12, true));
            }

            foreach (var feature in hexes_grant)
            {
                feature.AddComponent(Common.createPrerequisiteArchetypeLevel(magus, archetype, 18, true));
            }

            hexes_offensive.Add("EvilEyeAC",
                library.Get<BlueprintAbility>("1c8855dc3c9846a8addb4db4375eafe8"));//EvilEyeACHexAbility
            hexes_offensive.Add("EvilEyeAttack",
                library.Get<BlueprintAbility>("ad14718b3f65491183dd97c4b9f57246"));//EvilEyeAttackHexAbility
            hexes_offensive.Add("EvilEyeSaves",
                library.Get<BlueprintAbility>("cb406009170b447489b32d5b43d88f3f"));//EvilEyeSavesHexAbility
            hexes_offensive.Add("BeastOfIllOmen",
                library.Get<BlueprintAbility>("c19d55421e6f436580423fffc78c11bd"));//BeastOfIllOmenHexAbility
            hexes_offensive.Add("Misfortune",
                library.Get<BlueprintAbility>("08b6595f503f4d3c973424c217f7610e"));//MisfortuneHexAbility
            hexes_offensive.Add("Slumber",
                library.Get<BlueprintAbility>("31f0fa4235ad435e95ebc89d8549c2ce"));//SlumberHexAbility
            hexes_offensive.Add("Agony",
                library.Get<BlueprintAbility>("0229165c289947968a20550817524590"));//WitchAgonyHexAbility
            hexes_offensive.Add("IceTomb",
                library.Get<BlueprintAbility>("a680c3c5fd7646499f1b7e8d95b0f5df"));//WitchIceTombHexAbility
            hexes_offensive.Add("AnimalServant",
                library.Get<BlueprintAbility>("583e661fe4244a319672bc6ccdc51294"));//WitchAnimalServantHexAbility
            hexes_offensive.Add("DeathCurse",
                library.Get<BlueprintAbility>("6913bcf974004951a0542e906b4c201c"));//WitchDeathCurseHexAbility
            hexes_offensive.Add("LayToRest",
                library.Get<BlueprintAbility>("948b588bb57d4ef1bf96940c0bba95c9"));//WitchLayToRestHexAbility
            hexes_offensive.Add("EternalSlumber",
                library.Get<BlueprintAbility>("b03f4347c1974e38acff99a2af092461"));//WitchEternalSlumberHexAbility
            hexes_offensive.Add("RetributionHex",
                library.Get<BlueprintAbility>("685707e1c39a4dc8b61c2b2989c32739"));//WitchRetributionHexAbility
            hexes_offensive.Add("RestlessSlumber",
                library.Get<BlueprintAbility>("e845d92965544e2ba9ca7ab5b1b246ca"));//RestlessSlumberHexAbility
            hexes_offensive.Add("SummerHeat",
                library.Get<BlueprintAbility>("008a70774dbf48058810c565dad93fce"));//WitchSummerHeatHex
            hexes_offensive.Add("HarrowingCurse",
                library.Get<BlueprintAbility>("39cd09cc131e40b49ca20213094d1190"));//WitchHarrowingCurseHex
        }

        static void createAccursedStrike()   //before createHexArcana()
        {
            //BalefulPolymorph
            //cast needs: AbilityEffectStickyTouch ->
            //effect needs: AbilityDeliverTouch -> bb337517547de1a4189518d404ec49d4:TouchItem

            var baleful = library.Get<BlueprintAbility>("3105d6e9febdc3f41a08d2b7dda1fe74");//BalefulPolymorph
            var baleful_touch = library.CopyAndAdd(baleful, "BalefulPolymorphCast", Guid.i.Reg("32e0d31f45484a64b78bea82f80d42af"));
            baleful.AddComponent(Helpers.CreateDeliverTouch());
            baleful_touch.Range = AbilityRange.Touch;
            baleful_touch.Animation = CastAnimationStyle.Touch;
            baleful_touch.ReplaceComponent<AbilityEffectRunAction>(Helpers.CreateStickyTouch(baleful));
            baleful_touch.RemoveComponents<AbilitySpawnFx>();

            var bonus_spelllist = new Common.ExtraSpellList(new Common.SpellId("32e0d31f45484a64b78bea82f80d42af", 5)).createLearnSpellList("MagusBonusSpellList", Guid.i.Reg("d1797a72a3c843459be0c44d22aa7296"), magus, archetype);

            int i = 0;
            accursed_strike_variants = new BlueprintAbility[hexes_offensive.Count];
            foreach (var hex in hexes_offensive)
            {
                accursed_strike_variants[i] = hex.Value.CreateTouchSpellCast();
                accursed_strike_variants[i++].AddComponent(Helper.CreateAbilityShowIfCasterHasAnyFacts(hex.Value.Parent ? hex.Value.Parent : hex.Value));
                hex.Value.AddComponent(Helpers.CreateDeliverTouch());

                //hex.Value.AddComponent(Helpers.CreateDeliverTouch());
                //string name = "AccursedStrike"+hex.Key+"Ability";
                //accursed_strike_variants[i] = Helpers.CreateAbility(
                //    name,
                //    "Accursed Strike: "+hex.Value.GetName(),
                //    hex.Value.Description,
                //    Guid.i.Get(name),
                //    hex.Value.Icon,
                //    AbilityType.Supernatural,
                //    hex.Value.ActionType,
                //    AbilityRange.Touch,
                //    hex.Value.LocalizedDuration,
                //    hex.Value.LocalizedSavingThrow,
                //    Helpers.CreateStickyTouch(hex.Value),
                //    Helper.CreateAbilityShowIfCasterHasAnyFacts(hex.Value.Parent ? hex.Value.Parent : hex.Value)
                //);
                //accursed_strike_variants[i++].setMiscAbilityParametersTouchHarmful();
            }

            var accursed_strike_ab = Helpers.CreateAbility(
                "AccursedStrikeAbility",
                "Accursed Strike",
                "Any prepared spell or hex with the curse descriptor can be delivered using the spellstrike ability, even if the spells are not touch attack spells.",
                Guid.i.Reg("5dcaf35eddf74885bbbdc9c045fdd3ff"),
                accursed_strike_variants[0].Icon,
                AbilityType.Supernatural,
                CommandType.Standard,
                AbilityRange.Touch,
                "",
                ""
            );
            accursed_strike_ab.setMiscAbilityParametersTouchHarmful();
            accursed_strike_ab.SetComponents(Helpers.CreateAbilityVariants(accursed_strike_ab, accursed_strike_variants));

            accursed_strike = Helpers.CreateFeature(
                "AccursedStrikeFeature",
                "Accursed Strike",
                "Any prepared spell or hex with the curse descriptor can be delivered using the spellstrike ability, even if the spells are not touch attack spells.",
                Guid.i.Reg("842537d10e1a47e7-a87d050613b6e85b"),
                accursed_strike_variants[0].Icon,
                FeatureGroup.None,
                Common.createPrerequisiteArchetypeLevel(magus, archetype, 1, true),
                Helpers.CreateAddFact(accursed_strike_ab),
                bonus_spelllist
            );

            //hex_arcana_selection.AllFeatures = hex_arcana_selection.AllFeatures.AddToArray(accursed_strike);
            hexes.Add(accursed_strike);
        }
        
        // adds hexes to arcana selection
        static void createHexArcana()   //after attachHexes()
        {
            hex_arcana_selection = Helpers.CreateFeatureSelection(
                "HexArcanaFeat",
                "Hex Arcana",
                "You gain one hex. You must meet the prerequisites for this hex.\n"
                + "Special: You can take this arcana multiple times. Each time you do, you gain another hex.",
                Guid.i.Reg("c12a85028ea843dcbcef31612fc8f3c4"),
                library.Get<BlueprintAbility>("0bd54216d38852947930320f6269a9d7").Icon,
                FeatureGroup.None,
                archetype.CreatePrerequisite(1, true)
            );

            // hex_arcana_selection.AllFeatures = new BlueprintFeature[] {
            //     ameliorating, healing, beast_of_ill_omen, slumber_hex, misfortune_hex, fortune_hex, iceplant_hex, murksight_hex, evil_eye, summer_heat, cackle, ward, swamps_grasp, flight_hex,
            //     major_healing,  major_ameliorating, animal_skin, agony, beast_gift, harrowing_curse, ice_tomb, regenerative_sinew, retribution, restless_slumber,
            //     animal_servant, death_curse, lay_to_rest, life_giver, eternal_slumber
            // };
            hex_arcana_selection.AllFeatures = hexes.ToArray();

            BlueprintFeature hex_arcana_feat = hex_arcana_selection;
            hex_arcana_feat.Ranks = 20;
            hex_arcana_feat.Groups = new FeatureGroup[] { FeatureGroup.None };
            //hex_arcana_feat.AddComponent(archetype.CreatePrerequisite(1, true));
            
            magus_arcana_selection.AllFeatures = magus_arcana_selection.AllFeatures.AddToArray(hex_arcana_feat);
        }

        // makes features, mostly cosmetic
        static void createClassFeatures()   //after createHexArcana()
        {
            hexcrafter_spells = Helpers.CreateFeature(
                "ExtendSpellListHexcrafter",
                "Spells",
                "A hexcrafter magus adds the following spells to his magus spell list: bestow curse, major curse, and all other spells of 6th level or lower that have the curse descriptor.",
                Guid.i.Reg("dfe4149516374910b5aee14219ab02d3"),
                null,
                FeatureGroup.None
            );

            arcana_hexes = Helpers.CreateFeature(
                "HexArcanaFeature",
                "Hex Arcana",
                "The hexcrafter may select any witch hex in place of a magus arcana. He gains the benefit of or uses that hex as if he were a witch of a level equal to his magus level. At 12th level, the hexcrafter may select a hex or major hex in place of a magus arcana. At 18th level, a hexcrafter can select a hex, major hex, or grand hex in place of a magus arcana.",
                Guid.i.Reg("ae960b5eb06645dc887b40a247a758b4"),
                null,
                FeatureGroup.None
            );

            extra_hex = Helpers.CreateFeatureSelection(
                "ExtraHexArcanaFeature",
                "Hex Magus",
                "At 4th level, the hexcrafter magus picks one hex from the witch’s hex class feature. This feature replaces spell recall.",
                Guid.i.Reg("2344442d1c7f4675ac8855f9511ebd58"),
                null,
                FeatureGroup.None
            );
            extra_hex.AllFeatures = hex_arcana_selection.AllFeatures;
        }
        
        // --- general feats ---

        public static void createExtraArcanaFeat()  //after createHexcrafter() 
        {
            var extra_arcana_feat_selection = Helpers.CreateFeatureSelection(
                "ExtraArcanaFeat",
                "Extra Arcana",
                "You gain one additional magus arcana. You must meet all the prerequisites for this magus arcana.\n"
                + "Special: You can gain this feat multiple times.Its effects stack, granting a new arcana each time you gain this feat.",
                Guid.i.Reg("dbc7e543d8044952975f53f34310cee0"),
                library.Get<BlueprintFeature>("42f96fc8d6c80784194262e51b0a1d25").Icon, //ExtraArcanePool.Icon
                FeatureGroup.Feat,
                magus.PrerequisiteClassLevel(1, true)
            );
            extra_arcana_feat_selection.AllFeatures = magus_arcana_selection.AllFeatures;
            extra_arcana_feat = extra_arcana_feat_selection;
            extra_arcana_feat.Ranks = 10;
            extra_arcana_feat.Groups = new FeatureGroup[] { FeatureGroup.Feat };
            library.AddFeats(extra_arcana_feat);
        }

        // known issues:
        // - feat immediately grants access to all hex-strikes instead of just one (won't fix)
        // - swift action can be buffered (won't fix)
        // - not compatible with Shaman, since CotW (originally from Eldritch Arcana) keeps hexes of all classes separate
        public static void createHexStrikeFeat()
        {
            if (hexes_offensive.Count <= 0) throw new InvalidOperationException("Must initialize hexes first!");

            // creates a variant for each offensive hex. as a swift action, it applies a selfbuff to the next unarmed attack.
            // the buff is consumed on hit and will cast the attached hex ability on the target.
            int i = 0;
            BlueprintAbility[] variants = new BlueprintAbility[hexes_offensive.Count];
            foreach (var hex in hexes_offensive)
            {
                string name = "HexStrike"+hex.Key+"OwnerBuff";
                var buff = Helpers.CreateBuff(
                    name,
                    "Hex Strike: "+hex.Value.GetName(),
                    hex.Value.Description,
                    Guid.i.Get(name),
                    hex.Value.Icon,
                    Contexts.NullPrefabLink, 
                    Common.createAddInitiatorAttackWithWeaponTriggerWithCategory(Helpers.CreateActionList(Helpers.CreateConditional(
                        Helpers.Create<ContextConditionCanTarget>(), new GameAction[] { 
                            Helper.CreateContextActionCastSpell(hex.Value), 
                            Helper.CreateContextActionRemoveSelf() })))
                );

                name = "HexStrike"+hex.Key+"Ability";
                variants[i++] = Helpers.CreateAbility(
                    name,
                    "Hex Strike: "+hex.Value.GetName(),
                    hex.Value.Description,
                    Guid.i.Get(name),
                    hex.Value.Icon,
                    AbilityType.Supernatural,
                    CommandType.Swift,
                    AbilityRange.Personal,
                    hex.Value.LocalizedDuration,
                    hex.Value.LocalizedSavingThrow,
                    Helpers.CreateRunActions(Helpers.CreateApplyBuff(buff, Contexts.Duration1Round, false, false, true, permanent: false)),
                    Helper.CreateAbilityCasterHasNoFacts(buff),
                    Helper.CreateAbilityShowIfCasterHasAnyFacts(hex.Value.Parent ? hex.Value.Parent : hex.Value)
                );
            }
            
            var hexstrike_ab = Helpers.CreateAbility(
                "HexStrikeAbility",
                "Hex Strike",
                "Choose one hex that you can use to affect no more than one opponent. If you make a successful unarmed strike against an opponent, in addition to dealing your unarmed strike damage, you can use a swift action to deliver the effects of the chosen hex to that opponent. Doing so does not provoke attacks of opportunity.",
                Guid.i.Reg("12d204e3a92c49048b2408ed1c2d8c94"),
                variants[0].Icon,
                AbilityType.Supernatural,
                CommandType.Swift,
                AbilityRange.Personal,
                "",
                ""
            );
            hexstrike_ab.SetComponents(Helpers.CreateAbilityVariants(hexstrike_ab, variants));

            hexstrike_feat = Helpers.CreateFeature(
                "HexStrikeFeature",
                "Hex Strike",
                hexstrike_ab.Description,
                Guid.i.Reg("ce4ddca6abc04990a5ee6f24ac2d0cad"),
                variants[0].Icon,
                FeatureGroup.Feat,
                Helpers.CreateAddFact(hexstrike_ab),
                Helpers.PrerequisiteFeature(library.Get<BlueprintFeature>("7812ad3672a4b9a4fb894ea402095167")),//ImprovedUnarmedStrike
                Helpers.PrerequisiteClassLevel(Witch.witch_class, 1, true),
                Common.createPrerequisiteArchetypeLevel(magus, archetype, 1, true)
            );
            Helper.AppendAndReplace(ref hexstrike_feat.Groups, FeatureGroup.CombatFeat);
            library.AddCombatFeats(hexstrike_feat);
        }

        #region Patches
        
        [HarmonyLib.HarmonyPatch(typeof(UnitPartMagus), nameof(UnitPartMagus.IsSpellFromMagusSpellList))]
        public static class CountHexesAsMagusSpellPatch
        {
            static bool Prefix(AbilityData spell, ref bool __result)
            {
                try
                {
                    if (hexes_ability.HasItem(spell.Blueprint) || accursed_strike_variants.HasItem(spell.Blueprint))
                    {
                        __result = true;
                        return false;
                    }
                } catch (System.Exception) { }
                return true;
            }
        }

        #endregion

    }
}
