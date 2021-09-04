using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Kingmaker.UnitLogic.Abilities.Components;
using FumisCodex.NewComponents;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items;
using Guid = FumisCodex.GuidManager;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands.Base;
using System.Reflection.Emit;
using System.Reflection;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;

namespace FumisCodex
{
    public class Items
    {
        static LibraryScriptableObject library => Main.library;

        public static BlueprintItemEquipmentUsable[] Pearls = Array.Empty<BlueprintItemEquipmentUsable>();
        public static BlueprintItemEquipmentUsable[] RuneStones = Array.Empty<BlueprintItemEquipmentUsable>();

        public static void createPearlOfPower(bool enabled = true)
        {
            var reference = library.Get<BlueprintItemEquipmentUsable>("fedfe06fec617c8429b2db25eb584cd6");  //FaerieDragon_01_Item

            Pearls = new BlueprintItemEquipmentUsable[9];
            for (int i = 0; i < Pearls.Length; i++)
            {
                string name = "PearlOfPower" + (i + 1) + "Ability";
                var pearl_ability = HelperEA.CreateAbility(
                    name,
                    "Pearl of Power Level " + (i + 1),
                    "This seemingly normal pearl of average size and luster is a potent aid to all spellcasters who prepare spells. Once per day on command, a pearl of power enables the possessor to recall any one spell that she had prepared and then cast. The spell is then prepared again, just as if it had not been cast. The spell must be of a particular level, depending on the pearl. Different pearls exist for recalling one spell per day of each level from 1st through 9th.",
                    Guid.i.Get(name),
                    Helper.Image2Sprite.Create($"pearl{i + 1}.png"),
                    AbilityType.Supernatural,
                    UnitCommand.CommandType.Standard,
                    AbilityRange.Personal,
                    Strings.Empty,
                    Strings.Empty,
                    Helper.CreateAbilityRestoreSpellSlot(i + 1)
                );
                pearl_ability.SetMiscAbilityParametersSelfOnly();

                name = "PearlOfPower" + (i + 1);
                Pearls[i] = library.CopyAndAdd(reference, name, Guid.i.Get(name));
                Pearls[i].CasterLevel = i * 2 + 1;
                Pearls[i].SpellLevel = i + 1;
                Pearls[i].ActivatableAbility = null;
                Pearls[i].Ability = pearl_ability;
                Pearls[i].Charges = Settings.StateManager.State.pearlRunestoneDailyUses;
                Access.m_Cost(Pearls[i]) = (int)(Math.Pow(i + 1, 2) * Settings.StateManager.State.magicItemBaseCost);
                Access.BlueprintItem_Weight(Pearls[i]) = 0f;
                Access.m_BeltItemPrefab(Pearls[i]) = null;

                Pearls[i].SetItemNameDescriptionFlavorIcon(
                    displayName: pearl_ability.Name,
                    description: pearl_ability.Description,
                    flavorText: "Shiny.",
                    icon: pearl_ability.Icon
                );
            }

            if (enabled)
            {
                // add items to vendors
                var vendor_scrollsI = library.Get<BlueprintSharedVendorTable>("5450d563aab78134196ee9a932e88671");
                var vendor_dragon = library.TryGet<BlueprintSharedVendorTable>("08e090bb2038e3d47be56d8752d5dcaf");
                foreach (var pearl in Pearls)
                {
                    vendor_scrollsI.AddItemToSpecifiedVendorTable(pearl, 10);
                    vendor_dragon?.AddItemToSpecifiedVendorTable(pearl, 10);
                }
            }
        }

        public static void createRuneStoneOfPower(bool enabled = true)
        {
            var reference = library.Get<BlueprintItemEquipmentUsable>("fedfe06fec617c8429b2db25eb584cd6");  //FaerieDragon_01_Item

            RuneStones = new BlueprintItemEquipmentUsable[9];
            for (int i = 0; i < RuneStones.Length; i++)
            {
                string name = "RuneStoneOfPower" + (i + 1) + "Ability";
                var runestone_ability = HelperEA.CreateAbility(
                    name,
                    "Runestone of Power Level " + (i + 1),
                    "A runestone of power is a small chip of polished stone etched with a rune. Once per day, a runestone of power enables any spontaneous spellcaster (like sorcerers) to regain one spell per day. The spell must be of a particular level, depending on the runestone. Different runestones exist for regaining one spell per day of each level from 1st through 9th. The spellcaster can also directly draw the energy from the runestone to cast the spell�doing so is part of the spellcasting action (a free action).",
                    Guid.i.Get(name),
                    Helper.Image2Sprite.Create($"stone{i + 1}.png"),
                    AbilityType.Supernatural,
                    UnitCommand.CommandType.Free,
                    AbilityRange.Personal,
                    Strings.Empty,
                    Strings.Empty,
                    Helper.CreateAbilityRestoreSpontaneousSpell(i + 1)
                );
                runestone_ability.SetMiscAbilityParametersSelfOnly();

                name = "RuneStoneOfPower" + (i + 1);
                RuneStones[i] = library.CopyAndAdd(reference, name, Guid.i.Get(name));
                RuneStones[i].CasterLevel = i * 2 + 1;
                RuneStones[i].SpellLevel = i + 1;
                RuneStones[i].ActivatableAbility = null;
                RuneStones[i].Ability = runestone_ability;
                //RuneStones[i].Type = UsableItemType.Potion;
                RuneStones[i].Charges = Settings.StateManager.State.pearlRunestoneDailyUses;
                Access.m_Cost(RuneStones[i]) = (int)(Math.Pow(i + 1, 2) * 2 * Settings.StateManager.State.magicItemBaseCost);
                Access.BlueprintItem_Weight(RuneStones[i]) = 0f;

                RuneStones[i].SetItemNameDescriptionFlavorIcon(
                    displayName: runestone_ability.Name,
                    description: runestone_ability.Description,
                    flavorText: "Ancient.",
                    icon: runestone_ability.Icon
                );
            }

            if (enabled)
            {
                // add items to vendors
                var vendor_scrollsI = library.Get<BlueprintSharedVendorTable>("5450d563aab78134196ee9a932e88671");
                var vendor_dragon = library.TryGet<BlueprintSharedVendorTable>("08e090bb2038e3d47be56d8752d5dcaf");
                foreach (var runestone in RuneStones)
                {
                    vendor_scrollsI.AddItemToSpecifiedVendorTable(runestone, 10);
                    vendor_dragon?.AddItemToSpecifiedVendorTable(runestone, 10);
                }
            }
        }

        public static void createDebugItem()
        {
            string name = "FUMI_RingOfPower";
            string displayName = "Ring of Power";
            string desc = "The wearer of this ring can bestow powerful buffs.\nIf the wearer of this ring is a kineticist, the bonus given to them by their gather power ability is increased by 1.";
            string flavor = "This artifact overflows with magical energy.";

            var itemRing = library.CopyAndAdd<BlueprintItemEquipmentRing>("ec4247c5f7731524b81191815c2a8d75", name + "Item", "bbfe969c65a24855b3977b8ecf4e0c9b");
            itemRing.SetItemNameDescriptionFlavorIcon(displayName, desc, flavor);
            itemRing.CasterLevel = 20;
            itemRing.SpellLevel = 9;
            itemRing.DC = 50;
            itemRing.SpendCharges = false;
            Access.BlueprintItemEquipmentSimple_Enchantments(itemRing) = new BlueprintEquipmentEnchantment[] {
                library.Get<BlueprintEquipmentEnchantment>("3dbc31019a444944ea587e756c4f40a7")
            };

            var abilityparent = itemRing.Ability = HelperEA.CreateAbility(
                name + "Ability",
                displayName,
                desc,
                "a7da3ba438104506b788249f024361fd",
                itemRing.Icon,
                AbilityType.Extraordinary,
                UnitCommand.CommandType.Free,
                AbilityRange.Long,
                "",
                "",
                Contexts.SFXTransmutationBuff00
            );
            abilityparent.CanTargetFriends = true;
            abilityparent.CanTargetEnemies = true;
            abilityparent.CanTargetSelf = true;
            abilityparent.CanTargetPoint = false;
            abilityparent.EffectOnAlly = AbilityEffectOnUnit.Helpful;
            abilityparent.EffectOnEnemy = AbilityEffectOnUnit.Harmful;

            string[] str_buffs = new string[] {
                "4f139d125bb602f48bfaec3d3e1937cb", //Person vergrößern
                "b0793973c61a19744a8630468e8f4174", //Person verkleinern
                "a92acdf18049d784eaa8f2004f5d2304", //Magierrüstung
                "9c0fa9b438ada3f43864be8dd8b3e741", //Schild
                "98dc7e7cc6ef59f4abe20c65708ac623", //Spiegelbilder
                "03464790f40c3c24aa684b57155f3280", //Hast
                "e6b35473a237a6045969253beb09777c", //Mächtige Unsicherbarkeit
                "00402bae4442a854081264e498e7a833", //Standort vortäuschen
                "1533e782fca42b84ea370fc1dcbf4fc1", //Bewegungsfreiheit
                "b0253e57a75b621428c1b89de5a937d1", //Todesschutz
                "09b4b69169304474296484c74aa12027", //Wahrer Blick
                "906262fda0fbda442b27f9b0a04e5aa0", //Furchtauslösende Erscheinung
                "8037ba3917974656bdf44b3e438a5481", //*Blade Tutor
                "bc91b41bb7ff4e7b886dc96c171dd5fb", //*Fly
                "8e5dba1e6c544d15b4c6309fc3993d06", //*Stunning Barrier, Greater
                "50a77710a7c4914499d0254e76a808e5", //SpellResistanceBuff
                "f107df846998ba24ca92cd4b18d8c282", //RegenerationColdIron10
                "3bc40c9cbf9a0db4b8b43d8eedf2e6ec", //LuckDomainBaseBuff
                "96bbd279e0bed0f4fb208a1761f566b5", //ChaosDomainBaseBuff
                "09d39b38bb7c6014394b6daced9bacd3", //Stunned
                "4814db563c105e64d948161162715661", //FalseLifeGreaterBuff

                "7c5d556b9a5883048bf030e20daebe31", //StoneskinCommunal
                "4ac47ddb9fa1eaf43a1b6809980cfbd2", //MagicMissile
                "ff8f1534f66559c478448723e16b6624", //Heal
                "cc09224ecc9af79449816c45bc5be218", //HarmCast
                "80a1a388ee938aa4e90d427ce9a7a3e9", //Resurrection
            };

            List<BlueprintAbility> abilityapply = new List<BlueprintAbility>(str_buffs.Length);
            for (int i = 0; i < str_buffs.Length; i++)
            {
                var obj = library.TryGet<BlueprintScriptableObject>(str_buffs[i]);
                var buff = obj as BlueprintBuff;
                var ability = obj as BlueprintAbility;
                if (buff != null)
                {
                    string buff_name = buff.Name;
                    if (buff_name == "")
                    {
                        buff_name = buff.name.Replace("Buff", "");
                    }

                    var variant = library.CopyAndAdd(abilityparent, name + buff.name, buff.AssetGuid, abilityparent.AssetGuid);
                    variant.SetNameDescriptionIcon(buff_name, buff.Description, buff.Icon);
                    variant.AddComponent(HelperEA.CreateRunActions(Helper.Create<ContextActionApplyAndStoreBuff>(a => { a.Buffs = buff.ObjToArray(); })));
                    abilityapply.Add(variant);
                }
                else if (ability != null)
                {
                    abilityapply.Add(ability);
                }
            }

            abilityparent.AddComponent(HelperEA.CreateAbilityVariants(abilityparent, abilityapply.ToArray()));
            abilityparent.AddComponent(Helper.Create<AbilityRemoveBuffOnDeactivate>(a => a.Ability = abilityparent));
        }

        #region CMI Patches

        private static bool _patchedCMIignoreCL = false;
        public static void patchCraftMagicItemIgnoreCLToogle(bool enabled = true)
        {
            if (COM.RenderCraftingSkillInformation == null || COM.WorkOnProjects == null || COM.CasterLevelIsSinglePrerequisite == null)
                return;

            if (enabled)
            {
                _patchedCMIignoreCL = true;
                Main.harmony.Patch(COM.RenderCraftingSkillInformation, prefix: new HarmonyLib.HarmonyMethod(typeof(Items).GetMethod(nameof(PrefixCraftMagicItemIgnoreCL))));
                Main.harmony.Patch(COM.WorkOnProjects, transpiler: new HarmonyLib.HarmonyMethod(typeof(Items).GetMethod(nameof(TranspilerCraftMagicItemIgnoreCL))));
                Main.DebugLog("Patched patchCraftMagicItemIgnoreCLToogle");
            }
            else if (_patchedCMIignoreCL)
            {
                Main.harmony.Unpatch(COM.RenderCraftingSkillInformation, HarmonyLib.HarmonyPatchType.All, Main.harmony.Id);
                Main.harmony.Unpatch(COM.WorkOnProjects, HarmonyLib.HarmonyPatchType.All, Main.harmony.Id);
                Main.DebugLog("Unpatched patchCraftMagicItemIgnoreCLToogle");
            }
        }

        public static IEnumerable<HarmonyLib.CodeInstruction> TranspilerCraftMagicItemIgnoreCL(IEnumerable<HarmonyLib.CodeInstruction> instr)
        {
            int index = 0;
            int flag = -1;
            foreach (var line in instr)
            {
                if (line.opcode == OpCodes.Ldfld && line.operand as FieldInfo == COM.CasterLevelIsSinglePrerequisite)
                    flag = index;

                if (flag > 0 && index < flag + 12 && line.opcode == OpCodes.Ldc_I4_5)
                {
                    line.opcode = OpCodes.Ldc_I4_0;
                    Main.DebugLog("TranspilerCraftMagicItemIgnoreCL line: " + index);
                }

                index++;
            }

            return instr;
        }

        public static void PrefixCraftMagicItemIgnoreCL(ref int casterLevel)
        {
            casterLevel = 0;
        }

        #endregion

        [HarmonyLib.HarmonyPatch(typeof(AbilityData), "ActionType", HarmonyLib.MethodType.Getter)]
        [HarmonyLib.HarmonyPriority(HarmonyLib.Priority.HigherThanNormal)]
        public class AbilityData_Patch
        {
            public static bool Prefix(AbilityData __instance, ref UnitCommand.CommandType __result)
            {
                var sourceItem = __instance.SourceItemUsableBlueprint;
                if (sourceItem != null && sourceItem.Ability != null && sourceItem.Type == UsableItemType.Other)
                {
                    //if (sourceItem.Ability.ActionType != UnitCommand.CommandType.Standard) Main.DebugLog($"Item {__instance.Blueprint.Name} returned as {__instance.Blueprint.ActionType} action.");
                    __result = sourceItem.Ability.ActionType;
                    return false;
                }
                return true;
            }
        }
    }
}