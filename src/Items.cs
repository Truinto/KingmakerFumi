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
                    "This seemingly normal pearl of average size and luster is a potent aid to all spellcasters. Once per day on command, a pearl of power enables the possessor to recall any one spell that she had prepared and then cast. The spell is then prepared again, just as if it had not been cast. The spell must be of a particular level, depending on the pearl. Different pearls exist for recalling one spell per day of each level from 1st through 9th.",
                    Guid.i.Get(name),
                    Helper.Image2Sprite.Create($"pearl{i + 1}.png"),
                    Kingmaker.UnitLogic.Abilities.Blueprints.AbilityType.Supernatural,
                    Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                    Kingmaker.UnitLogic.Abilities.Blueprints.AbilityRange.Personal,
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
                    Kingmaker.UnitLogic.Abilities.Blueprints.AbilityType.Supernatural,
                    Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Free,
                    Kingmaker.UnitLogic.Abilities.Blueprints.AbilityRange.Personal,
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