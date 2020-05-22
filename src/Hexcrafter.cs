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

namespace FumisCodex
{
    class Hexcrafter
    {
        static LibraryScriptableObject library => Main.library;
        public static BlueprintCharacterClass magus = library.Get<BlueprintCharacterClass>("45a4607686d96a1498891b3286121780"); //magus class
        public static BlueprintFeatureSelection magus_arcana_selection = library.Get<BlueprintFeatureSelection>("e9dc4dfc73eaaf94aae27e0ed6cc9ada");

        public static HexEngine hex_engine;
        public static BlueprintArchetype archetype;
        public static BlueprintFeature hexcrafter_spells;
        public static BlueprintFeature arcana_hexes;
        public static BlueprintFeatureSelection hex_arcana_selection;
        public static BlueprintFeatureSelection extra_hex;

        //feats
        public static BlueprintFeature extra_arcana_feat;

        //magus hex
        public static BlueprintFeature accursed_strike;
        //hexes
        public static BlueprintFeature healing;
        public static BlueprintFeature beast_of_ill_omen;
        public static BlueprintFeature slumber_hex;
        public static BlueprintFeature misfortune_hex;
        public static BlueprintFeature fortune_hex;
        public static BlueprintFeature flight_hex;
        public static BlueprintFeature iceplant_hex;
        public static BlueprintFeature murksight_hex;
        public static BlueprintFeature ameliorating;
        public static BlueprintFeature evil_eye;
        public static BlueprintFeature summer_heat;
        public static BlueprintFeature cackle;
        public static BlueprintFeature ward;
        public static BlueprintFeature swamps_grasp;
        //major hexes
        public static BlueprintFeature major_ameliorating;
        public static BlueprintFeature major_healing;
        public static BlueprintFeature animal_skin;
        public static BlueprintFeature agony;
        public static BlueprintFeature beast_gift;
        public static BlueprintFeature harrowing_curse;
        public static BlueprintFeature ice_tomb;
        public static BlueprintFeature regenerative_sinew;
        public static BlueprintFeature retribution;
        public static BlueprintFeature restless_slumber;
        // grand hexes
        public static BlueprintFeature animal_servant;
        public static BlueprintFeature death_curse;
        public static BlueprintFeature lay_to_rest;
        public static BlueprintFeature life_giver;
        public static BlueprintFeature eternal_slumber;

        public static void createHexcrafter()
        {
            archetype = Helpers.Create<BlueprintArchetype>(a =>
            {
                a.name = "HexcrafterArchetype";
                a.LocalizedName = Helpers.CreateString($"{a.name}.Name", "Hexcrafter");
                a.LocalizedDescription = Helpers.CreateString($"{a.name}.Description", "A hexcrafter magus has uncovered the secret of using his arcane pool to recreate witch hexes. These magi can hex friend and foe, curse those they strike, and expand their spell selection to include many curses and harmful spells.");
            });

            Helpers.SetField(archetype, "m_ParentClass", magus);
            library.AddAsset(archetype, "d8c2c968311942d19df6e352a97d8428"); //GuidManager.NewGuid("HexcrafterArchetype")

            hex_engine = new HexEngine(new BlueprintCharacterClass[] { magus }, StatType.Intelligence, StatType.Charisma, archetype);

            //createHexes();
            createBasicHexes();
            createHexArcana();
            createFeatures();

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

        // work on hold. this is a bit too advanced for me. maybe I drop the idea
        static void createHexes()
        {
            // TODO: add functionality to accursed_strike_buff
            // substep: spells
            // substep: hexes

            var accursed_strike_buff = Helpers.CreateBuff(
                "AccursedStrikeBuff",
                "Accursed Strike",
                "Any prepared spell or hex with the curse descriptor can be delivered using the spellstrike ability, even if the spells are not touch attack spells.",
                "309c5ae6d76d4117a8945bd2a35504b8", //GuidManager.NewGuid("AccursedStrikeBuff")
                library.Get<BlueprintAbility>("0bd54216d38852947930320f6269a9d7").Icon, // TODO: replace Icon
                null
            //Common.createAddAreaEffect(area_effect)
            );

            var accursed_strike_activatable = Helpers.CreateActivatableAbility(
                "AccursedStrikeToggleAbility",
                accursed_strike_buff.Name,
                accursed_strike_buff.Description,
                "",//GuidManager.NewGuid("AccursedStrikeToggleAbility"), // TODO: new GUID
                library.Get<BlueprintAbility>("0bd54216d38852947930320f6269a9d7").Icon, // TODO: replace Icon
                accursed_strike_buff,
                AbilityActivationType.Immediately,
                CommandType.Free,
                null
            );

            accursed_strike = Helpers.CreateFeature(
                "AccursedStrikeFeature",
                accursed_strike_buff.Name,
                accursed_strike_buff.Description,
                "",//GuidManager.NewGuid("AccursedStrikeFeature"), // TODO: make new GUID
                library.Get<BlueprintAbility>("0bd54216d38852947930320f6269a9d7").Icon, // TODO: replace Icon
                FeatureGroup.None,
                Helpers.CreateAddFact(accursed_strike_activatable)
            );
            accursed_strike.Ranks = 1;
            accursed_strike.AddComponent(archetype.CreatePrerequisite(1, true));

            hex_arcana_selection.AllFeatures = hex_arcana_selection.AllFeatures.AddToArray(accursed_strike);
            hex_arcana_selection.Features = hex_arcana_selection.AllFeatures;
        }

        static void createBasicHexes()
        {
            Harmony12.AccessTools.Field(typeof(Helpers.GuidStorage), "allow_guid_generation").SetValue(null, true);

            healing = hex_engine.createHealing("MagusHealing", Witch.healing.Name, Witch.healing.Description, "d25a2dcd5bdf4198a4a6b191262b6fcf", "e2730c88a27d4fc8bc7992e1f1fc2092", "77d039f649de487e938899d1dafcff63", "7e87040afdd24a2485cf4a442ef147b6", "45facc55b76d4199b11a155c02aaaa81", "83a8c1580b004bd1842c9976425505c8");
            beast_of_ill_omen = hex_engine.createBeastOfIllOmen("MagusIllOmen", Witch.beast_of_ill_omen.Name, Witch.beast_of_ill_omen.Description, "bad0669bc6684df99da1d8bf51e32724", "ff57e85d9e2e44d8864af2a78a2e313d", "e08079b9580b43bc929338ca1a31383b");
            slumber_hex = Witch.slumber_hex;
            misfortune_hex = hex_engine.createMisfortune("MagusMisfortune", Witch.misfortune_hex.Name, Witch.misfortune_hex.Description, "0dad4fb01ef543f7aa08089340fe2da2", "7b4709310d3e48bf8d6df92f2563a29f", "a532fe49579443848a2583184751a58d", "93ddd3b8b16b4663ac2028087c249d73");
            fortune_hex = hex_engine.createFortuneHex("MagusFortune", Witch.fortune_hex.Name, Witch.fortune_hex.Description, "3a2784a3d7524082a9e78cea0f4b469c", "92e6a15e21974026b7b7f02f04f77328", "570ef638d18042fbab9d5c9bee57c30b", "69602a8cc858439caf4c8b97d23f56d2");
            flight_hex = hex_engine.CreateFlightHex("MagusFlight", Witch.flight_hex.Name, Witch.flight_hex.Description); flight_hex.AddComponent(Helpers.PrerequisiteClassLevel(library.Get<BlueprintCharacterClass>("45a4607686d96a1498891b3286121780"), 5));  // needs magus 5
            iceplant_hex = Witch.iceplant_hex;
            murksight_hex = Witch.murksight_hex;
            ameliorating = hex_engine.createAmeliorating("MagusAmeliorating", Witch.ameliorating.Name, Witch.ameliorating.Description, "1ff3e2ff03b84be28e2027af98d404c1", "b30a5d62a581460099dceb439e21a0fe", "be3d64e3410d498ea221692fe76a373b", "c6b160f952ff4c5baa6ad2c42f81680a", "9382e36d3fd8437b9b3d7e4ff54f9ce9", "a9deabb547ff4cb781287538bc2a57d8");
            evil_eye = hex_engine.createEvilEye("MagusEvilEye", Witch.evil_eye.Name, Witch.evil_eye.Description, "1022455534f94e21a0926d00b6ff89db", "45676581543a401d8190dcc3c9a1b9c1", "8d1f6ce954e3409fa1c0a2a3b17c88e6", "6dbafd10ed6d4c308305a3e0cef9927c", "b08382bb3c764ca397bf87d120d62d13", "a11af1376eb845aab9bed84676026acc", "98f5f81524c14fe89c2e3f0f2f40a8b8", "e81a5efc98d34d989fe57420e5ed7a48");
            summer_heat = hex_engine.createSummerHeat("MagusSummerHeat", Witch.summer_heat.Name, Witch.summer_heat.Description, "8820fbba3fe74299a696b8562b4326e5", "8454da1e2f1c414c972823a7deb73305", "0123cc13c80e45349a0700da54254e68", "4d27875df08045a19812011f6578ab32", "d74f5d033e294afabcd7e696cf92d60d");
            cackle = Witch.cackle;
            ward = hex_engine.createWardHex("MagusWard", Witch.ward.Name, Witch.ward.Description);
            swamps_grasp = Witch.swamps_grasp;
            //major hexes
            major_ameliorating = hex_engine.createMajorAmeliorating("MagusMajorAmeliorating", Witch.major_ameliorating.Name, Witch.major_ameliorating.Description, "412d8d23a212440c9b34d09de0b6c690", "e4eee63211274476b3e984d17521fa95", "ae82e7ff1687411e807e05bf40faf332", "f728dd1ac1874a16983f1d739ad4d10c", "ed98b109f14e4b37901e2708da90313a", "721c45b431094b9a94f1d64ce6beb034");
            major_healing = hex_engine.createMajorHealing("MagusMajorHealing", Witch.major_healing.Name, Witch.major_healing.Description, "20d7a72635f74eefb804f4017c0fcff9", "6e159eb5459345138500ab6d1fbd42d7", "9e80bc66b57949d0882f2c105823f2ce", "516faafc877842e4aa6d998977adbb38", "733a3613f3a347f3a59495a0db893284", "ffeb0385155d4bd5a3d8552e46d44603");
            animal_skin = hex_engine.createAnimalSkin("MagusAnimalSkin", Witch.animal_skin.Name, Witch.animal_skin.Description, "86699311d0e741babc06ce88d083ba0a", "d2feb588f4ae4caa96a2d237e664a8a1", "d2c9c8f70de54d70b1763cd63262f8c5", "03b905d23ed245cb9c0430c801092564");
            agony = hex_engine.createAgony("MagusAgony", Witch.agony.Name, Witch.agony.Description, "5b44402f5fcb4f71b635328abe4f4f9d", "5ce92cb9bc9c4f7c9ad02a8e08ed35bd", "4305f9af90274bbd8b523b775485009a", "0aebd73b37f7418c9915c78851d04394");
            beast_gift = hex_engine.createBeastGift("MagusBeastGift", Witch.beast_gift.Name, Witch.beast_gift.Description, "b7c6c6f3bad44706b9fd4460fe687c1d", "23b831bfd0d64f348c65ed609db2533f", "d251f08701674eb398f242ed740b1ba9", "bd1eb5798001438e8f3cbe726cf3d5f3");
            harrowing_curse = hex_engine.createHarrowingCurse("MagusHarrowingCurse", Witch.harrowing_curse.Name, Witch.harrowing_curse.Description, "acd1247ab64e427aba7de36e15d2f51a", "a15d4b83610e4720a2212fbfafb45ec4", "f9925cb0b28346999138892d4dca3b59");
            ice_tomb = hex_engine.createIceTomb("MagusIceTomb", Witch.ice_tomb.Name, Witch.ice_tomb.Description, "8db188c9d09c4d96a128e458ec8ae79e", "4f8757f0adbe462bb28257016b2be9ae", "96ff3efd93b648479f03149b52dc0bb7", "7e63048881b94462a13607b6922fba4b");
            regenerative_sinew = hex_engine.createRegenerativeSinew("MagusRegenerativeSinew", Witch.regenerative_sinew.Name, Witch.regenerative_sinew.Description, "016852e8263a4b01af1f8c33e7943a41", "3ecc3b396ad7462b933a744d2da59a91", "87ea5cee9d0b4d8f9a05960052cf88cd", "d394c88c317e47eba5fd2c0fca62c92d", "e9d6873106b0461faecb291f50b37335");
            retribution = hex_engine.createRetribution("MagusRetribution", Witch.retribution.Name, Witch.retribution.Description, "285dc89c35d846f5a77705529d05e4db", "b6061f9185fa4506930d5dc5de62cdf5", "8f18d3458c794358a8c42b8dc0576cc2");
            restless_slumber = hex_engine.createRestlessSlumber("MagusRestlessSlumber", Witch.restless_slumber.Name, Witch.restless_slumber.Description);
            // grand hexes
            animal_servant = hex_engine.createAnimalServant("MagusAnimalServant", Witch.animal_servant.Name, Witch.animal_servant.Description, "dc04e1689aa045daaad69ec251437038", "baeff3d6cdee48dca746ad098769ffbb", "987c833d19e74cb29e8018a556f82f51", "87c8c4f77c8c4852b107c9f5b3889409");
            death_curse = hex_engine.createDeathCurse("MagusDeathCurse", Witch.death_curse.Name, Witch.death_curse.Description, "2eff8cbde08e489286c4c2718e231ffa", "1fee31af354b46ca82cf1c7fc73c28e6", "6b7ce1c2d52e463c9b1f9f91bf94b9f7", "7639f04acaed4bf6a6d9ee3b0936efc5");
            lay_to_rest = hex_engine.createLayToRest("MagusLayToRest", Witch.lay_to_rest.Name, Witch.lay_to_rest.Description, "07d8f279f8784a879bf34102b01725bf", "c54163ee07004efa9c34c6184847a07e", "5a341a23d395411ab3ca103dcbdc5433");
            life_giver = hex_engine.createLifeGiver("MagusLifeGiver", Witch.life_giver.Name, Witch.life_giver.Description, "ba8d32c0401942ac9cb134c6c773c323", "c749acdfda594e73a7be949711a03255", "153f7f3897e941289b513125f1304357");
            eternal_slumber = hex_engine.createEternalSlumber("MagusEternalSlumber", Witch.eternal_slumber.Name, Witch.eternal_slumber.Description, "f9b03362f4b54e03be5f379c64a05531", "26acdee4106943fca4ebeb550aeb0653", "bc5096e35fe34187b9d6bf79a37b2d03", "25888bf0feec4100a2f9cd45ce977d67");

            CallOfTheWild.Helpers.GuidStorage.dump(@"./Mods/CallOfTheWild/blueprints.txt");
            Harmony12.AccessTools.Field(typeof(Helpers.GuidStorage), "allow_guid_generation").SetValue(null, false);
        }

        static void createHexArcana()
        {
            // add hexes to arcana selection
            //  later: fix major hexes 2 lvls later

            hex_arcana_selection = Helpers.CreateFeatureSelection(
                "HexArcanaFeat",
                "Hex Arcana",
                "You gain one hex. You must meet the prerequisites for this hex.\n"
                + "Special: You can take this arcana multiple times. Each time you do, you gain another hex.",
                "c12a85028ea843dcbcef31612fc8f3c4", //GuidManager.NewGuid("HexArcanaFeat")
                library.Get<BlueprintAbility>("0bd54216d38852947930320f6269a9d7").Icon,
                FeatureGroup.None,
                archetype.CreatePrerequisite(1, true)
            );

            hex_arcana_selection.AllFeatures = new BlueprintFeature[] {
                ameliorating, healing, beast_of_ill_omen, slumber_hex, misfortune_hex, fortune_hex, iceplant_hex, murksight_hex, evil_eye, summer_heat, cackle, ward, swamps_grasp, flight_hex,
                major_healing,  major_ameliorating, animal_skin, agony, beast_gift, harrowing_curse, ice_tomb, regenerative_sinew, retribution, restless_slumber,
                animal_servant, death_curse, lay_to_rest, life_giver, eternal_slumber
            };

            BlueprintFeature hex_arcana_feat = hex_arcana_selection;
            hex_arcana_feat.Ranks = 20;
            hex_arcana_feat.Groups = new FeatureGroup[] { FeatureGroup.None };
            //hex_arcana_feat.AddComponent(archetype.CreatePrerequisite(1, true));
            
            #region old
            //foreach (var feature in hex_arcana_selection.AllFeatures)
            //{
            //    Main.DebugLog("Feature: " + feature.Name);
            //    //var x = feature.GetComponent<ContextRankConfig>();
            //    var comps = feature.GetComponents<BlueprintComponent>();
            //    if (comps != null)
            //    {
            //        foreach (var comp in comps)
            //        {
            //            if (comp is AddFacts)
            //            {
            //                var facts = comp as AddFacts;
            //                foreach (var fact in facts.Facts)
            //                {
            //                    Main.DebugLog("AddFacts: " + fact.Name);
            //                }
            //            }
            //            else if (comp is PrerequisiteClassLevel)
            //            {
            //                var p_class = comp as PrerequisiteClassLevel;
            //                Main.DebugLog("PrerequisiteClassLevel: " + p_class.CharacterClass.Name);
            //                if (p_class.CharacterClass.Name == "Witch")
            //                    feature.AddComponent(archetype.CreatePrerequisite(p_class.Level == 10 ? 12 : p_class.Level, true));
            //            }
            //            else
            //                Main.DebugLog("Comp?: " + comp.ToString());
            //        }
            //    }
            //}
            #endregion
            
            magus_arcana_selection.AllFeatures = magus_arcana_selection.AllFeatures.AddToArray(hex_arcana_feat);
        }

        static void createFeatures()
        {
            // ! ExtraSpellList !
            // var spell_search = library.GetAllBlueprints().OfType<BlueprintAbility>().Where(b => b.IsSpell && (b.SpellDescriptor & SpellDescriptor.Fire) != 0).Cast<BlueprintAbility>().ToArray();
            var spell_guids = new string[] { "5590652e1c2225c4ca30c4a699ab3649", "9d5d2d3ffdd73c648af3eb3e585b1113" };  // TODO: find spell GUIDs
            var extra_spell_list = new Common.ExtraSpellList(spell_guids);
            //var learn_spell_list = extra_spell_list.createLearnSpellList("Witch" + name + "PatronSpellList", spell_list_guid, witch_class);

            hexcrafter_spells = Helpers.CreateFeature(
                "ExtendSpellListHexcrafter",
                "Spells",
                "A hexcrafter magus adds the following spells to his magus spell list: bestow curse, major curse, and all other spells of 6th level or lower that have the curse descriptor.",
                "dfe4149516374910b5aee14219ab02d3", //GuidManager.NewGuid("ExtendSpellListHexcrafter")
                library.Get<BlueprintAbility>("0bd54216d38852947930320f6269a9d7").Icon, // TODO: replace Icon
                FeatureGroup.None
            //extra_spell_list
            );

            arcana_hexes = Helpers.CreateFeature(
                "HexArcanaFeature",
                "Hex Arcana",
                "The hexcrafter may select any witch hex in place of a magus arcana. He gains the benefit of or uses that hex as if he were a witch of a level equal to his magus level. At 12th level, the hexcrafter may select a hex or major hex in place of a magus arcana. At 18th level, a hexcrafter can select a hex, major hex, or grand hex in place of a magus arcana.",
                "ae960b5eb06645dc887b40a247a758b4", //GuidManager.NewGuid("HexArcanaFeature")
                library.Get<BlueprintAbility>("0bd54216d38852947930320f6269a9d7").Icon,   // TODO: replace Icon
                FeatureGroup.None
            );

            extra_hex = Helpers.CreateFeatureSelection(
                "ExtraHexArcanaFeature",
                "Hex Magus",
                "At 4th level, the hexcrafter magus picks one hex from the witch’s hex class feature. This feature replaces spell recall.",
                "2344442d1c7f4675ac8855f9511ebd58", //GuidManager.NewGuid("ExtraHexArcanaFeature")
                library.Get<BlueprintAbility>("0bd54216d38852947930320f6269a9d7").Icon,   // TODO: replace Icon
                FeatureGroup.None
            );

            extra_hex.Features = hex_arcana_selection.AllFeatures;
            extra_hex.AllFeatures = extra_hex.Features;

        }
        
        // --- general feats ---

        public static void createExtraArcanaFeat()
        {
            var extra_arcana_feat_selection = Helpers.CreateFeatureSelection(
                "ExtraArcanaFeat",
                "Extra Arcana",
                "You gain one additional magus arcana. You must meet all the prerequisites for this magus arcana.\n"
                + "Special: You can gain this feat multiple times.Its effects stack, granting a new arcana each time you gain this feat.",
                "dbc7e543d8044952975f53f34310cee0",
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

        static void createHexStrikeFeat()
        {
            // future
        }
    }
}
