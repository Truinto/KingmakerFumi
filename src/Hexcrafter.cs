using CallOfTheWild;
using FumisCodex.NewComponents;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;
using static Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell;
using Guid = FumisCodex.GuidManager;

namespace FumisCodex
{
    public class Hexcrafter
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
        //public static Dictionary<string, BlueprintAbility> hexes_offensive = new Dictionary<string, BlueprintAbility>();
        public static List<BlueprintAbility> hexes_harmful = new List<BlueprintAbility>();

        public static void createHexcrafter()
        {
            archetype = Helper.Create<BlueprintArchetype>(a =>
            {
                a.name = "HexcrafterArchetype";
                a.LocalizedName = HelperEA.CreateString($"{a.name}.Name", "Hexcrafter");
                a.LocalizedDescription = HelperEA.CreateString($"{a.name}.Description", "A hexcrafter magus has uncovered the secret of using his arcane pool to recreate witch hexes. These magi can hex friend and foe, curse those they strike, and expand their spell selection to include many curses and harmful spells.");
            });

            Access.m_ParentClass(archetype) = magus;
            library.AddAsset(archetype, Guid.i.Reg("d8c2c968311942d19df6e352a97d8428"));

            //hex_engine = new HexEngine(new BlueprintCharacterClass[] { magus }, StatType.Intelligence, StatType.Charisma, archetype);

            attachHexes();
            createAccursedStrike();
            createHexArcana();
            createClassFeatures();

            // add curse spells to spellbook
            //var spell_search = library.GetAllBlueprints().OfType<BlueprintAbility>().Where(b => b.IsSpell && (b.SpellDescriptor & SpellDescriptor.Curse) != 0).ToArray(); //.Cast<BlueprintAbility>().ToArray()
            spellbook = library.CopyAndAdd<BlueprintSpellbook>(magus.Spellbook, "HexcrafterSpellbook", Guid.i.Reg("5d0c1159252d4f38962103f83b7e2483"));
            spellbook.SpellList = Helper.Instantiate(spellbook.SpellList);   //Common.combineSpellLists("HexcrafterSpellList", spellbook.SpellList);
            library.Get<BlueprintAbility>("989ab5c44240907489aba0a8568d0603").AddToSpellList(spellbook.SpellList, 3);    //BestowCurse

            NewSpells.accursed_glare.AddToSpellList(spellbook.SpellList, 3);    //Accursed Glare
            NewSpells.curse_major.AddToSpellList(spellbook.SpellList, 5);    //Bestow Curse, Major

            archetype.ReplaceSpellbook = spellbook;

            var spell_recall = library.Get<BlueprintFeature>("61fc0521e9992624e9c518060bf89c0f");
            var improved_spell_recall = library.Get<BlueprintFeature>("0ef6ec1c2fdfc204fbd3bff9f1609490");

            archetype.RemoveFeatures = new LevelEntry[] {
                HelperEA.LevelEntry(4, spell_recall),
                HelperEA.LevelEntry(11, improved_spell_recall)
            };

            archetype.AddFeatures = new LevelEntry[] {
                HelperEA.LevelEntry(1, hexcrafter_spells, arcana_hexes),
                HelperEA.LevelEntry(4, extra_hex),
                HelperEA.LevelEntry(11, spell_recall)
            };

            Helper.AppendAndReplace(ref magus.Progression.UIDeterminatorsGroup, hexcrafter_spells, arcana_hexes);
            Helper.AppendAndReplace(ref magus.Progression.UIGroups, HelperEA.CreateUIGroup(extra_hex));
            Helper.AppendAndReplace(ref magus.Archetypes, archetype);
        }

        // attaching to witch class of CotW; this means class levels stack with each other in regards to effective hex levels
        // by RAW they should not stack, but it's much smoother to expand features (like hexstrike)
        public static void attachHexes()
        {
            // Hexcrafter has no access to amplified_hex_feat

            //fix Split Hex working for Hexcrafter
            HexEngine.split_hex_feat.GetComponent<PrerequisiteClassLevel>().Group = Prerequisite.GroupType.Any;
            HexEngine.split_major_hex_feat.GetComponent<PrerequisiteClassLevel>().Group = Prerequisite.GroupType.Any;

            HexEngine.accursed_hex_feat.AddComponent(HelperEA.CreatePrerequisiteArchetypeLevel(magus, archetype, 1, true));
            HexEngine.split_hex_feat.AddComponent(HelperEA.CreatePrerequisiteArchetypeLevel(magus, archetype, 10, true));
            HexEngine.split_major_hex_feat.AddComponent(HelperEA.CreatePrerequisiteArchetypeLevel(magus, archetype, 18, true));

            //fix Hex Strike working for Hexcrafter
            HexEngine.hex_strike.AddComponent(HelperEA.CreatePrerequisiteArchetypeLevel(magus, archetype, 1, true));

            //fix flight prerequisite
            Witch.flight_hex.RemoveComponents<PrerequisiteClassLevel>();
            Witch.flight_hex.AddComponent(HelperEA.PrerequisiteClassLevel(Witch.witch_class, 5, true));
            Witch.flight_hex.AddComponent(HelperEA.CreatePrerequisiteArchetypeLevel(magus, archetype, 5, true));

            //Hexes readout
            foreach (var hex in library.Get<BlueprintFeatureSelection>("68bd6449147e4234b6d9a80564ba17ae").AllFeatures) //WitchHexSelection
            {
                int level = hex.GetComponents<PrerequisiteClassLevel>().FirstOrDefault(s => s.CharacterClass == Witch.witch_class)?.Level ?? 0;
                if (level >= 18)
                    hexes_grant.Add(hex);
                else if (level >= 10)
                    hexes_major.Add(hex);
                else
                    hexes.Add(hex);
            }
            hexes.AddRange(hexes_major);
            hexes.AddRange(hexes_grant);

            Main.DebugLog("hexes: " + hexes.Count + " " + string.Join(", ", hexes.Select(a => a.name)));
            Main.DebugLog("hexes_major: " + hexes_major.Count + " " + string.Join(", ", hexes_major.Select(a => a.name)));
            Main.DebugLog("hexes_grant: " + hexes_grant.Count + " " + string.Join(", ", hexes_grant.Select(a => a.name)));

            //get abilities of all hexes for other functionalities
            foreach (var feature in hexes)
            {
                foreach (var addfact in feature.GetComponents<AddFacts>())
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

            //add hexcrafter to all level based features
            foreach (var ability in hexes_ability)
            {
                var ranks = ability.GetComponents<ContextRankConfig>();
                var param = ability.GetComponent<CallOfTheWild.NewMechanics.ContextCalculateAbilityParamsBasedOnClasses>();

                Main.DebugLog($"{ability.name} has {ranks.Count()} rank components and {(param ? "1" : "0")} param component");

                foreach (var rank in ranks)
                {
                    if (rank != null
                        && Access.m_BaseValueType(rank) == ContextRankBaseValueType.ClassLevel
                        && Access.m_Class(rank).Contains(Witch.witch_class)
                        && !Access.m_Class(rank).Contains(magus))
                    {
                        // note, this allows only for a single archetype to be defined. if multiple are required, then replace this with a CustomProperty
                        Access.m_Class(rank) = Access.m_Class(rank).AddToArray(magus);
                        Access.Archetype(rank) = archetype;
                        Access.m_BaseValueType(rank) = ContextRankBaseValueType.MaxClassLevelWithArchetype;
                        Main.DebugLog("Added Hexcrafter to rank component");
                    }
                    else if (rank != null)
                    {
                        Main.DebugLog("Not added to rank type " + Access.m_BaseValueType(rank).ToString());
                    }
                }

                if (param)
                {
                    Helper.AppendAndReplace(ref param.CharacterClasses, magus);
                    Helper.AppendAndReplace(ref param.archetypes, archetype);
                }
            }

            //also add hexcrafter to all the debuffs hexes can inflict
            foreach (var buff in library.GetAllBlueprints().OfType<BlueprintBuff>())
            {
                var ranks = buff.GetComponents<ContextRankConfig>();

                foreach (var rank in ranks)
                {
                    //Main.DebugLog($"{buff.name} rank of {Access.m_BaseValueType(rank)} has witch {Access.m_Class(rank).Contains(Witch.witch_class)} has magus {Access.m_Class(rank).Contains(magus)}");

                    if (rank != null
                        && Access.m_BaseValueType(rank) == ContextRankBaseValueType.ClassLevel
                        && Access.m_Class(rank).Contains(Witch.witch_class)
                        && !Access.m_Class(rank).Contains(magus))
                    {
                        // note, this allows only for a single archetype to be defined. if multiple are required, then replace this with a CustomProperty
                        Access.m_Class(rank) = Access.m_Class(rank).AddToArray(magus);
                        Access.Archetype(rank) = archetype;
                        Access.m_BaseValueType(rank) = ContextRankBaseValueType.MaxClassLevelWithArchetype;
                        Main.DebugLog("Added Hexcrafter to buff " + buff.name);
                    }
                }
            }

            foreach (var feature in hexes_major)
            {
                feature.AddComponent(HelperEA.CreatePrerequisiteArchetypeLevel(magus, archetype, 12, true));
            }

            foreach (var feature in hexes_grant)
            {
                feature.AddComponent(HelperEA.CreatePrerequisiteArchetypeLevel(magus, archetype, 18, true));
            }

            //fix missing harmful flag
            library.Get<BlueprintAbility>("0229165c289947968a20550817524590").EffectOnEnemy = AbilityEffectOnUnit.Harmful;

            foreach (var hex in hexes_ability)
            {
                if (hex.EffectOnEnemy == AbilityEffectOnUnit.Harmful)
                {
                    hexes_harmful.Add(hex);
                }
            }


            Main.DebugLog("hexes_harmful: " + hexes_harmful.Count + " " + string.Join(", ", hexes_harmful.Select(a => a.name)));

            // hexes_offensive.Add("EvilEyeAC",
            //     library.Get<BlueprintAbility>("1c8855dc3c9846a8addb4db4375eafe8"));//EvilEyeACHexAbility
            // hexes_offensive.Add("EvilEyeAttack",
            //     library.Get<BlueprintAbility>("ad14718b3f65491183dd97c4b9f57246"));//EvilEyeAttackHexAbility
            // hexes_offensive.Add("EvilEyeSaves",
            //     library.Get<BlueprintAbility>("cb406009170b447489b32d5b43d88f3f"));//EvilEyeSavesHexAbility
            // hexes_offensive.Add("BeastOfIllOmen",
            //     library.Get<BlueprintAbility>("c19d55421e6f436580423fffc78c11bd"));//BeastOfIllOmenHexAbility
            // hexes_offensive.Add("Misfortune",
            //     library.Get<BlueprintAbility>("08b6595f503f4d3c973424c217f7610e"));//MisfortuneHexAbility
            // hexes_offensive.Add("Slumber",
            //     library.Get<BlueprintAbility>("31f0fa4235ad435e95ebc89d8549c2ce"));//SlumberHexAbility
            // hexes_offensive.Add("Agony",
            //     library.Get<BlueprintAbility>("0229165c289947968a20550817524590"));//WitchAgonyHexAbility
            // hexes_offensive.Add("IceTomb",
            //     library.Get<BlueprintAbility>("a680c3c5fd7646499f1b7e8d95b0f5df"));//WitchIceTombHexAbility
            // hexes_offensive.Add("AnimalServant",
            //     library.Get<BlueprintAbility>("583e661fe4244a319672bc6ccdc51294"));//WitchAnimalServantHexAbility
            // hexes_offensive.Add("DeathCurse",
            //     library.Get<BlueprintAbility>("6913bcf974004951a0542e906b4c201c"));//WitchDeathCurseHexAbility
            // hexes_offensive.Add("LayToRest",
            //     library.Get<BlueprintAbility>("948b588bb57d4ef1bf96940c0bba95c9"));//WitchLayToRestHexAbility
            // hexes_offensive.Add("EternalSlumber",
            //     library.Get<BlueprintAbility>("b03f4347c1974e38acff99a2af092461"));//WitchEternalSlumberHexAbility
            // hexes_offensive.Add("RetributionHex",
            //     library.Get<BlueprintAbility>("685707e1c39a4dc8b61c2b2989c32739"));//WitchRetributionHexAbility
            // hexes_offensive.Add("RestlessSlumber",
            //     library.Get<BlueprintAbility>("e845d92965544e2ba9ca7ab5b1b246ca"));//RestlessSlumberHexAbility
            // hexes_offensive.Add("SummerHeat",
            //     library.Get<BlueprintAbility>("008a70774dbf48058810c565dad93fce"));//WitchSummerHeatHex
            // hexes_offensive.Add("HarrowingCurse",
            //     library.Get<BlueprintAbility>("39cd09cc131e40b49ca20213094d1190"));//WitchHarrowingCurseHex
        }

        public static void createAccursedStrike()   //before createHexArcana()
        {
            //BalefulPolymorph
            //cast needs: AbilityEffectStickyTouch ->
            //effect needs: AbilityDeliverTouch -> bb337517547de1a4189518d404ec49d4:TouchItem

            var baleful = library.Get<BlueprintAbility>("3105d6e9febdc3f41a08d2b7dda1fe74");//BalefulPolymorph
            var baleful_touch = library.CopyAndAdd(baleful, "BalefulPolymorphCast", Guid.i.Reg("32e0d31f45484a64b78bea82f80d42af"));
            baleful.AddComponent(HelperEA.CreateDeliverTouch());
            baleful_touch.Range = AbilityRange.Touch;
            baleful_touch.Animation = CastAnimationStyle.Touch;
            baleful_touch.ReplaceComponent<AbilityEffectRunAction>(HelperEA.CreateStickyTouch(baleful));
            baleful_touch.RemoveComponents<AbilitySpawnFx>();

            var accursed_glare_touch = library.CopyAndAdd(NewSpells.accursed_glare, "AccursedGlareCast", Guid.i.Reg("419f9b711f4f47baa3844f8cbe8eac86"));
            NewSpells.accursed_glare.AddComponent(HelperEA.CreateDeliverTouch()); // TODO: check this doesn't morph the original
            accursed_glare_touch.Range = AbilityRange.Touch;
            accursed_glare_touch.Animation = CastAnimationStyle.Touch;
            accursed_glare_touch.ReplaceComponent<AbilityEffectRunAction>(HelperEA.CreateStickyTouch(NewSpells.accursed_glare));
            accursed_glare_touch.RemoveComponents<AbilitySpawnFx>();

            var bonus_spelllist = new ExtraSpellList(
                    new ExtraSpellList.SpellId(baleful_touch.AssetGuid, 5),
                    new ExtraSpellList.SpellId(accursed_glare_touch.AssetGuid, 3)
                ).createLearnSpellList("MagusBonusSpellList", Guid.i.Reg("d1797a72a3c843459be0c44d22aa7296"), magus, archetype);

            int i = 0;
            accursed_strike_variants = new BlueprintAbility[hexes_harmful.Count];
            foreach (var hex in hexes_harmful)
            {
                var hex_accursed = library.CopyAndAdd(hex, hex.name + "Accursed", hex.AssetGuid, "d0bf27aea4234946bd065710e759b1b5");
                hex_accursed.AddComponent(HelperEA.CreateDeliverTouch());
                hex_accursed.Range = AbilityRange.Touch;
                hexes_ability.Add(hex_accursed);
                Main.DebugLog("Generated " + hex_accursed.name + ":" + hex_accursed.AssetGuid);

                accursed_strike_variants[i] = hex_accursed.CreateTouchSpellCast();
                accursed_strike_variants[i].SetName("Accursed Strike: " + hex.Name);
                accursed_strike_variants[i].ActionType = CommandType.Standard;
                accursed_strike_variants[i++].AddComponent(Helper.CreateAbilityShowIfCasterHasAnyFacts(hex.Parent ? hex.Parent : hex));
            }

            var accursed_strike_ab = HelperEA.CreateAbility(
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
            HelperEA.SetMiscAbilityParametersTouchHarmful(accursed_strike_ab);
            accursed_strike_ab.SetComponents(HelperEA.CreateAbilityVariants(accursed_strike_ab, accursed_strike_variants));

            accursed_strike = HelperEA.CreateFeature(
                "AccursedStrikeFeature",
                "Accursed Strike",
                "Any prepared spell or hex with the curse descriptor can be delivered using the spellstrike ability, even if the spells are not touch attack spells.",
                Guid.i.Reg("842537d10e1a47e7a87d050613b6e85b"),
                accursed_strike_variants[0].Icon,
                FeatureGroup.None,
                HelperEA.CreatePrerequisiteArchetypeLevel(magus, archetype, 1, true),
                HelperEA.CreateAddFact(accursed_strike_ab),
                bonus_spelllist
            );

            //hex_arcana_selection.AllFeatures = hex_arcana_selection.AllFeatures.AddToArray(accursed_strike);
            hexes.Add(accursed_strike);
        }

        // adds hexes to arcana selection
        public static void createHexArcana()   //after attachHexes()
        {
            hex_arcana_selection = HelperEA.CreateFeatureSelection(
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

            Helper.AppendAndReplace(ref magus_arcana_selection.AllFeatures, hex_arcana_feat);
        }

        // makes features, mostly cosmetic
        public static void createClassFeatures()   //after createHexArcana()
        {
            hexcrafter_spells = HelperEA.CreateFeature(
                "ExtendSpellListHexcrafter",
                "Spells",
                "A hexcrafter magus adds the following spells to his magus spell list: bestow curse, major curse, and all other spells of 6th level or lower that have the curse descriptor.",
                Guid.i.Reg("dfe4149516374910b5aee14219ab02d3"),
                null,
                FeatureGroup.None
            );

            arcana_hexes = HelperEA.CreateFeature(
                "HexArcanaFeature",
                "Hex Arcana",
                "The hexcrafter may select any witch hex in place of a magus arcana. He gains the benefit of or uses that hex as if he were a witch of a level equal to his magus level. At 12th level, the hexcrafter may select a hex or major hex in place of a magus arcana. At 18th level, a hexcrafter can select a hex, major hex, or grand hex in place of a magus arcana.",
                Guid.i.Reg("ae960b5eb06645dc887b40a247a758b4"),
                null,
                FeatureGroup.None
            );

            extra_hex = HelperEA.CreateFeatureSelection(
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
            var extra_arcana_feat_selection = HelperEA.CreateFeatureSelection(
                "ExtraArcanaFeat",
                "Extra Arcana",
                "You gain one additional magus arcana. You must meet all the prerequisites for this magus arcana.\n"
                + "Special: You can gain this feat multiple times.Its effects stack, granting a new arcana each time you gain this feat.",
                Guid.i.Reg("dbc7e543d8044952975f53f34310cee0"),
                library.Get<BlueprintFeature>("42f96fc8d6c80784194262e51b0a1d25").Icon, //ExtraArcanePool.Icon
                FeatureGroup.Feat,
                HelperEA.PrerequisiteClassLevel(magus, 1, true)
            );
            extra_arcana_feat_selection.AllFeatures = magus_arcana_selection.AllFeatures;
            extra_arcana_feat = extra_arcana_feat_selection;
            extra_arcana_feat.Ranks = 10;
            extra_arcana_feat.Groups = new FeatureGroup[] { FeatureGroup.Feat };
            HelperEA.AddFeats(library, extra_arcana_feat);
        }

        // known issues:
        // - feat immediately grants access to all hex-strikes instead of just one (won't fix)
        // - swift action can be buffered (won't fix)
        // - not compatible with Shaman, since CotW (originally from Eldritch Arcana) keeps hexes of all classes separate
        public static void createHexStrikeFeat()
        {
            if (hexes_harmful.Count <= 0) throw new InvalidOperationException("Must initialize hexes first!");

            // creates a variant for each offensive hex. as a swift action, it applies a selfbuff to the next unarmed attack.
            // the buff is consumed on hit and will cast the attached hex ability on the target.
            int i = 0;
            BlueprintAbility[] variants = new BlueprintAbility[hexes_harmful.Count];
            foreach (var hex in hexes_harmful)
            {
                var buff = HelperEA.CreateBuff(
                    "HexStrike" + hex.name + "OwnerBuff",
                    "Hex Strike: " + hex.Name,
                    hex.Description,
                    HelperEA.MergeIds(hex.AssetGuid, "1a551ee869ef46d0a67e6281a809d2a3"),
                    hex.Icon,
                    Contexts.NullPrefabLink,
                    // FeralCombatTraining.AddInitiatorAttackWithWeaponTriggerOrFeralTraining.fromAddInitiatorAttackWithWeaponTrigger(
                    //     HelperEA.CreateAddInitiatorAttackWithWeaponTriggerWithCategory(Helper.CreateActionList(HelperEA.CreateConditional(
                    //         condition: Helper.Create<ContextConditionCanTarget>().ObjToArray(),
                    //         ifTrue: new GameAction[] {
                    //             Helper.CreateContextActionCastSpell(hex),
                    //             Helper.CreateContextActionRemoveSelf() })))),
                    FeralCombatTraining.AddInitiatorAttackWithWeaponTriggerOrFeralTraining.fromAddInitiatorAttackWithWeaponTrigger(
                        HelperEA.CreateAddInitiatorAttackWithWeaponTriggerWithCategory(Helper.CreateActionList(
                            Helper.CreateContextActionTryCastSpell(
                                Spell: hex,
                                Succeed: Helper.CreateContextActionRemoveSelf().ObjToArray()))))
                );

                variants[i++] = HelperEA.CreateAbility(
                    "HexStrike" + hex.name,
                    "Hex Strike: " + hex.Name,
                    hex.Description,
                    HelperEA.MergeIds(hex.AssetGuid, "822b902862924c2cb05837e4de8bea47"),
                    hex.Icon,
                    AbilityType.Supernatural,
                    CommandType.Swift,
                    AbilityRange.Personal,
                    hex.LocalizedDuration,
                    hex.LocalizedSavingThrow,
                    HelperEA.CreateRunActions(HelperEA.CreateApplyBuff(buff, Contexts.Duration1Round, false, false, true, permanent: false)),
                    Helper.CreateAbilityCasterHasNoFacts(buff),
                    Helper.CreateAbilityShowIfCasterHasAnyFacts(hex.Parent ? hex.Parent : hex)
                );
            }

            var hexstrike_ab = HelperEA.CreateAbility(
                "HexStrikeAbility",
                "Hex Strike (Fumi's Codex)",
                "If you make a successful unarmed strike against an opponent, in addition to dealing your unarmed strike damage, you can use a swift action to deliver a hex to that opponent. Doing so does not provoke attacks of opportunity.",
                Guid.i.Reg("12d204e3a92c49048b2408ed1c2d8c94"),
                variants[0].Icon,
                AbilityType.Supernatural,
                CommandType.Swift,
                AbilityRange.Personal,
                "",
                ""
            );
            hexstrike_ab.SetComponents(HelperEA.CreateAbilityVariants(hexstrike_ab, variants));

            hexstrike_feat = HelperEA.CreateFeature(
                "HexStrikeFeature",
                hexstrike_ab.Name,
                hexstrike_ab.Description,
                Guid.i.Reg("ce4ddca6abc04990a5ee6f24ac2d0cad"),
                variants[0].Icon,
                FeatureGroup.Feat,
                HelperEA.CreateAddFact(hexstrike_ab),
                HelperEA.PrerequisiteFeature(library.Get<BlueprintFeature>("7812ad3672a4b9a4fb894ea402095167")),//ImprovedUnarmedStrike
                HelperEA.PrerequisiteClassLevel(Witch.witch_class, 1, true),
                HelperEA.CreatePrerequisiteArchetypeLevel(magus, archetype, 1, true)
            );
            Helper.AppendAndReplace(ref hexstrike_feat.Groups, FeatureGroup.CombatFeat);
            HelperEA.AddCombatFeats(library, hexstrike_feat);
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
                }
                catch (System.Exception) { }
                return true;
            }
        }

        #endregion

    }

    public class Bladebound
    {
        static LibraryScriptableObject library => Main.library;
        public static BlueprintCharacterClass magus = library.Get<BlueprintCharacterClass>("45a4607686d96a1498891b3286121780"); //magus class
        public static BlueprintFeatureSelection magus_arcana_selection = library.Get<BlueprintFeatureSelection>("e9dc4dfc73eaaf94aae27e0ed6cc9ada");

        public static BlueprintFeature bladebound_arcana;

        //https://www.d20pfsrd.com/classes/base-classes/magus/archetypes/paizo-magus-archetypes/bladebound
        /*
        A select group of magi are called to carry a black blade—a sentient weapon of often unknown and possibly unknowable purpose. These weapons become valuable tools and allies, as both the magus and weapon typically crave arcane power, but as a black blade becomes more aware, its true motivations manifest, and as does its ability to influence its wielder with its ever-increasing ego.
        Black Blade (Ex)

        At 3rd level, the bladebound magus’ gains a powerful sentient weapon called a black blade, whose weapon type is chosen by the magus (see sidebar). A magus with this class feature cannot take the familiar magus arcana, and cannot have a familiar of any kind, even from another class.

        Instead of the normal arcane pool amount, the bladebound magus’s arcane pool has a number of points equal to 1/3 his level (minimum 1) plus his Intelligence bonus.

        This ability changes the Arcane Pool class feature and replaces the magus arcana gained at 3rd level.
        Black Blade Basics

        A black blade is bonded to a particular magus, much like a familiar, but in more of a partnership than a master-servant relationship.

        Intelligence: This is the intelligence score of the black blade. It starts at 10 and increases by 1 for every two levels of the bladebound magus (at 3rd level, 5th level, and so on).

        Wisdom and Charisma: As the bladebound magus increases in level, so do the Wisdom and Charisma of the black blade. These abilities start at 6 and increase by 1 for every two levels of magus.

        Ego: A black blade starts with an ego of 5, and that ego increases as the blade becomes more powerful, as per Table: Black Blade Progression below. In cases where a wielder and the black blade come into conflict, like any intelligent item, a black blade can attempt to exert its dominance (see Intelligent Items). Due to its flexible and powerful nature, a black blade has a nonstandard ego progression.

        Languages and Skills: A black blade starts with Common as a language. As the black blade increases in Intelligence, it manifests knowledge of languages and arcane lore. Upon reaching an Intelligence of 12, it gains a bonus language of the GM’s choice, and gains 1 rank in Knowledge (arcana). Each time the sword gains a bonus to Intelligence, it gains another language and another rank in Knowledge (arcana).

        Senses: A black blade is aware of everything around it like a creature that can see and hear. It can be blinded and deafened as if it were a creature. It uses the saving throws of its magus, even if the magus is not currently wielding the black blade.

        Black Blade Arcane Pool: A black blade has an arcane pool with a number of points equal to 1 + its Intelligence bonus.
        Black Blade Ability Descriptions

        A black blade has special abilities (or imparts abilities to its wielder) depending on the wielder’s magus level. The abilities are cumulative. A black blade normally refuses to use any of its abilities when wielded by anyone other than its magus, and acts as a masterwork weapon of its type.

        Alertness (Ex): While a magus is wielding his black blade, he gains the Alertness feat.

        Black Blade Strike (Sp): As a free action, the magus can spend a point from the black blade’s arcane pool to grant the black blade a +1 bonus on damage rolls for 1 minute. For every four levels beyond 1st, this ability gives the black blade another +1 on damage rolls.

        Telepathy (Su): While a magus is wielding or carrying his black blade, he can communicate telepathically with the blade in a language that the magus and the black blade share.

        Unbreakable (Ex): As long as it has at least 1 point in its arcane pool, a black blade is immune to the broken condition. If broken, the black blade is unconscious and powerless until repaired. If destroyed, the black blade can be reforged 1 week later through a special ritual that costs 200 gp per magus level. The ritual takes 24 hours to complete.

        Energy Attunement (Su): At 5th level, as a free action, a magus can spend a point of his black blade’s arcane pool to have it deal one of the following types of damage instead of weapon damage: cold, electricity, or fire. He can spend 2 points from the black blade’s arcane pool to deal sonic or force damage instead of weapon damage. This effect lasts until the start of the magus’s next turn.

        Teleport Blade (Sp): As a standard action, a magus of 9th level or higher can expend an arcane point from his or his black blade’s arcane pool, and can call his black blade from as far as 1 mile away, causing it to instantaneously teleport to his hand.

        Transfer Arcana (Su): At 13th level, once per day, a magus can attempt to siphon points from his black blade’s arcane pool into his own arcane pool. Doing so takes a full-round action and the magus must succeed at a Will saving throw with a DC equal to the black blade’s ego. If the magus succeeds, he regains 1 point to his arcane pool for every 2 points he saps from his black blade. If he fails the saving throw, the magus becomes fatigued (but can try again). If he is fatigued, he becomes exhausted instead. He cannot use this ability if he is exhausted.

        Spell Defense (Sp): A magus of 17th level or higher can expend an arcane point from his weapon’s arcane pool as a free action; he then gains SR equal to his black blade’s ego until the start of his next turn.

        Life Drinker (Su): At 19th level, each time the magus kills a living creature with the black blade, he can pick one of the following effects: the black blade restores 2 points to its arcane pool; the black blade restores 1 point to its arcane pool and the magus restores 1 point to his arcane pool; the magus gains a number of temporary hit points equal to the black blade’s ego (these temporary hit points last until spent or 1 minute, whichever is shorter). The creature killed must have a number of Hit Dice equal to half the magus’s character level for this to occur.

        Magus Arcana: The following magus arcana complement the bladebound magus archetype: arcane accuracy, broad study, dispelling strike, and reflection.
        

        Black Blades

        A black blade is a particular form of intelligent weapon gained by a magus with the bladebound archetype. There are several ways a magus might gain this weapon. Sometimes it just appears among the magus’s possessions, and its origin is a mystery. Other times the magus finds a black blade during an adventure or event of some kind. Sometimes a black blade is passed down generation to generation in an ongoing search for a magus who can unlock its true potential.

        A black blade is always a one-handed slashing weapon, a rapier, or a sword cane. The magus chooses the blade’s type upon gaining the blade, and once chosen, it can’t be changed. As a bladebound magus increases in level, his black blade gains power.

        A black blade is independently conscious but features some personality traits reflecting its wielder. A black blade always has the same alignment as its wielder and even changes its alignment if its wielder does. The blade typically works toward its wielder’s goals, but not always without argument or backlash. Each black blade has a mission, and while sometimes two or more black blades will work in concert, each mission is singular in purpose (the black blade’s mission is usually up to the GM and the needs of the campaign or the adventure, or a GM can determine the weapon’s purpose randomly using Table: Intelligent Item Purpose). Some black blades are very open about their missions, but most are secretive. Certain sages have speculated that an invisible hand or arcane purpose moves these weapons.
        Table: Black Blade Progression Magus Class Level 	Enhancement Bonus 	Int 	Wis/Cha 	Ego 	Special
        3rd–4th 	+1 	11 	7 	5 	Alertness, black blade strike, telepathy, unbreakable
        5th–6th 	+2 	12 	8 	8 	Energy attunement
        7th–8th 	+2 	13 	9 	10 	—
        9th–10th 	+3 	14 	10 	12 	Teleport blade
        11th–12th 	+3 	15 	11 	14 	—
        13th–14th 	+4 	16 	12 	16 	Transfer arcana
        15th–16th 	+4 	17 	13 	18 	—
        17th–18th 	+5 	18 	14 	22 	Spell defense
        19th–20th 	+5 	19 	15 	24 	Life drinker
        */

        public void createBlade()
        {
            var pool = library.Get<BlueprintAbilityResource>("effc3e386331f864e9e06d19dc218b37"); //ArcanePoolResourse
            var enduring = library.Get<BlueprintBuff>("3c2fe8e0374d28d4185355121f4c4544"); //EnduringBladeBuff

            // add black blade's arcane pool resource
            var pool_blade = library.CopyAndAdd<BlueprintAbilityResource>(pool, "ArcanePoolResourseBlackBlade", "4293125339b14d57aec9cf237737e36e");
            Helper.Set_BlueprintAbilityResource_MaxAmount(pool_blade, StartingLevel: 1, LevelStep: 4);

            // reduce the magus' arcane pool
            var pool_reduce = Helper.CreateIncreaseResourceCustom(pool, magus.ObjToArray(), null, false, 0, 0, -1, 0, -1, -1, -1, -1, -2, -1, -2, -2, -2, -2, -3, -2, -3, -3, -3, -3, -4);

            // add a permanent unremovable black blade
            var add_black_blade = Helper.Create<AddPermanentWeaponFact>();
            add_black_blade.Weapon = library.CopyAndAdd<BlueprintItemWeapon>("0e2b2a13f286c10499921633a557388c", "BlackBladeRapier", "5373ed7ee8c946aab4e45e478e187972"); //RapierPlus5
            Access.BlueprintItemWeapon_Enchantments(add_black_blade.Weapon) = Helper.CreateBlueprintWeaponEnchantment(
                "BlackBladeEnchantment",
                Guid.i.Reg("d5bb5794430d4917984d597ab08532c5"),
                "Black Blade",
                "A black blade has an enhancement bonus of +1. This bonus increases at level 4 and every 4 level thereafter.",
                0,
                Helper.CreateWeaponEnhancementScaling(magus.ObjToArray(), null, 1, 1, 4, 1)
            ).ObjToArray();

            // create an Arcana as a substitute to the archetype; this is OK, since this archetype simply costs a single arcana; must be restricted to exactly level 3
            bladebound_arcana = HelperEA.CreateFeature(
                "BladeboundArcana",
                "Blade Bound (Archetype)",
                "DESC",
                Guid.i.Get("62014d882b1a4c41abfcc1a03c45711c"),
                Contexts.IconPlaceHolder,
                FeatureGroup.MagusArcana,
                Helper.CreatePrerequisiteExactClassLevel(magus, 3),
                Helper.CreateAddAbilityResources(pool_blade),
                pool_reduce,
                add_black_blade,
                HelperEA.CreateAddFacts(
                    library.Get<BlueprintFeature>("1c04fe9a13a22bc499ffac03e6f79153") //Alertness
                    )
            );

            // 1: Black Blade Strike
            HelperEA.CreateBuff(
                "BlackBladeStrikeBuff",
                "Black Blade Strike",
                "As a free action, the magus can spend a point from the black blade’s arcane pool to grant the black blade a +1 bonus on damage rolls for 1 minute. For every four levels beyond 1st, this ability gives the black blade another +1 on damage rolls.",
                Guid.i.Reg("18c40e95da5b4e9084c7b45c394a5c27"),
                Contexts.IconPlaceHolder,
                Contexts.NullPrefabLink,
                HelperEA.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, classes: magus.ObjToArray(), progression: ContextRankProgression.StartPlusDivStep, startLevel: 1, stepLevel: 4),
                HelperEA.CreateAddContextStatBonus(StatType.AdditionalDamage, ModifierDescriptor.None)
            );

            // 5: Energy Attunement
            // foreach -> create ability -> ContextActionAddBuff -> Buff: WeaponAlterDamageType
            Helper.Create<WeaponAlterDamageType>(a => { a.Weapon = add_black_blade.Weapon; a.EnergyType = DamageEnergyType.Fire; });
            Helper.Create<WeaponAlterDamageType>(a => { a.Weapon = add_black_blade.Weapon; a.EnergyType = DamageEnergyType.Electricity; });
            Helper.Create<WeaponAlterDamageType>(a => { a.Weapon = add_black_blade.Weapon; a.EnergyType = DamageEnergyType.Cold; });
            Helper.Create<WeaponAlterDamageType>(a => { a.Weapon = add_black_blade.Weapon; a.EnergyType = DamageEnergyType.Sonic; });
            Helper.Create<WeaponAlterDamageType>(a => { a.Weapon = add_black_blade.Weapon; a.IsForceDamage = true; });

            // TODO:    AddFeatureOnClassLevel
            // 13: Transfer Arcana
            // 17: Spell Defense
            // 19: Life Drinker
        }

    }

    public class Golemfist
    {
        //https://www.d20pfsrd.com/classes/base-classes/magus/archetypes/paizo-magus-archetypes/golemfist-magus-archetype/
        /*
        Golemfist

        As part of studying the methods used to build golems, these magi graft construct parts onto their own arms.
        Diminished Spellcasting

        A golemfist has one fewer spell slot of each level than a regular magus. If this reduces his number of spell slots of a particular level to zero, he can cast spells of that level only if his Intelligence allows bonus spells of that level.

        This alters the magus’s spellcasting.
        Golem Arm (Ex)

        A golemfist begins play with a golem arm grafted in place of one of his arms. The golem arm is a masterwork weapon that is treated as an unarmed strike and deals damage as if he were a monk 2 levels lower than his class level (minimum 1st level). The golem arm is made from basic parts and mundane materials that grant no additional benefits. A golemfist can recreate his golem arm from other materials by spending 1 day and paying an additional +50% of the normal cost of the special material used in the creation of this new golem arm. For the purposes of determining the weapon type and weight for special materials, the golem arm is treated as a heavy mace. If a golemfist recreates his golem arm, any previously used special materials are destroyed. The arm gains no benefits from items that enhance unarmed strikes, such as an amulet of mighty fists.

        At 1st level, a golemfist can use his arcane pool to enhance his unarmed strikes as if they were manufactured weapons. At 5th level, he can use this ability to add any of the following special abilities to his unarmed strikes: corrosive, corrosive burst, defending, flaming, flaming burst, frost, icy burst, impact, shock, shocking burst, or thundering. At 11th level, he gains access to the following special abilities: brilliant energy and speed.

        This alters arcane pool.
        Improved Unarmed Strike

        A golemfist gains Improved Unarmed Strike as a bonus feat. He prepares one fewer cantrip per day than normal.

        This alters cantrips.
        Empowered Arm (Ex)

        At 3rd level, a golemfist’s golem arm is treated as a magic weapon and gains a +1 enhancement bonus on attack and damage rolls. This bonus increases by 1 at 7th level and every 4 levels thereafter, to a total bonus of +5 at 19th level.

        This replaces the magus arcana gained at 3rd level.
        Unarmed Spellstrike (Su)

        A golemfist can use spellstrike to deliver spells only when attacking with his unarmed strikes.

        This alters spellstrike.
        Magus Arcana

        A golemfist gains access to the following arcana in addition to those normally available to the magus. He cannot select any arcana more than once.

            Break Spell (Ex): A golemfist can use his arm to disrupt spells targeted at him. When a ranged attack generated by a spell effect targets the golemfist, he can spend a point from his arcane pool to attempt a melee attack roll as an attack of opportunity. If the result is greater than the total attack roll of the ranged attack, the attack is negated. Spell effects that do not require attack rolls cannot be deflected. The magus must be at least 13th level and have the shielding arm arcana before selecting this arcana.
            Flurrying Arm (Ex): A golemfist can improve his golem arm to attack faster. He gains the flurry of blows monk class ability, treating his class level as his monk level. A golemfist makes only one additional attack when he uses flurry of blows. The magus must be at least 11th level to select this arcana.
            Sharpened Arm (Ex): A golemfist can rework his arm to more closely resemble a blade. He can choose to deal piercing or slashing damage with his golem arm. When he does so, the golem arm threatens a critical hit on 19–20. The artificer also adds keen and wounding to his list of arcane pool weapon abilities.
            Shielding Arm (Ex): A golemfist learns to deflect blows with his arm. If he is not wielding any other weapons, he gains a shield bonus to his AC equal to his golem arm’s enhancement bonus.
        */
    }
}
