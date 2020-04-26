using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityModManagerNet;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Blueprints.Items;
using UnityEngine;

namespace FumisCodex
{
    internal class Main
    {
        internal static Harmony12.HarmonyInstance harmony;
        internal static LibraryScriptableObject library;

        /// <summary>True if mod is enabled.</summary>
        internal static bool Enabled { get; set; } = true;
        /// <summary>Path of current mod.</summary>
        internal static string ModPath { get; set; }

        #region logging

        static UnityModManager.ModEntry.ModLogger logger;

        /// <summary>Only prints message, if compiled on DEBUG.</summary>
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void DebugLog(string msg)
        {
            logger?.Log(msg);
        }

        internal static void DebugLogAlways(string msg)
        {
            logger?.Log(msg);
        }

        internal static void DebugError(Exception ex)
        {
            logger?.LogException(ex);
        }

        #endregion
        
        #region GUI

        /// <summary>
        /// Called when the mod is turned to on/off.
        /// With this function you control an operation of the mod and inform users whether it is enabled or not.
        /// </summary>
        /// <param name="value">true = mod to be turned on; false = mod to be turned off</param>
        /// <returns>Returns true, if state can be changed.</returns>
        internal static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Main.Enabled = value;
            return true;
        }
        
        public static string text1 = "Example Text.";
        /// <summary>Draws the GUI</summary>
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Label("Disclaimer: Remember that playing with mods makes them mandatory for your save game! If you want to uninstall anyway, then you need to remove all references to said mod. In my case respec all your characters, that should do the trick.");

            Checkbox(ref Settings.StateManager.State.slumberHDrestriction, "CotW - remove slumber HD restriction");
            Checkbox(ref Settings.StateManager.State.extendSprayInfusion, "Kineticist - Spray infusion may be used with cold blast");
            Checkbox(ref Settings.StateManager.State.extraWildTalentFeat, "Kineticist [F] - Feat for extra wild talents");
            Checkbox(ref Settings.StateManager.State.preciseBlastTalent, "Kineticist [F] - Utility talent for precise blasts (similiar to alchemist discovery, but for blasts)");
            Checkbox(ref Settings.StateManager.State.mindShieldTalent, "Kineticist [F] - Mind Shield: Utility talent to reduce Mind Burn's effect on Psychokineticist");
            GUILayout.Label("Stuff in the options are homebrew. For a full list read the mods description.\n"
                + "Legend: [F] This adds a talent. You still need to pick feats/talents for these effects. If you already picked these features, then they stay in effect regardless of the option above.");
            if (GUILayout.Button("Save settings. All changes require a restart to take effect!"))
            {
                Settings.StateManager.TrySaveConfigurationState();
            }

            GUILayout.Space(5);
            GUILayout.Label("Options below are effective immediately without restart.");
            Checkbox(ref Settings.StateManager.State.cheatCombineParametrizedFeats, "Cheat - picking Weapon Focus (Greater etc.) will grant all in the same group");
        }
        private static void Checkbox(ref bool value, string label)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"{(value ? "<color=green><b>✔</b></color>" : "<color=red><b>✖</b></color>")} {label}", GUILayout.ExpandWidth(false)))
            {
                value = !value;
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>Unused example.</summary>
        static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            if (Input.GetKeyDown(KeyCode.F1)) { }
        }

        #endregion

        #region Load

        /// <summary>Loads on game start.</summary>
        /// <param name="modEntry.Info">Contains all fields from the 'Info.json' file.</param>
        /// <param name="modEntry.Path">The path to the mod folder e.g. '\Steam\steamapps\common\YourGame\Mods\TestMod\'.</param>
        /// <param name="modEntry.Active">Active or inactive.</param>
        /// <param name="modEntry.Logger">Writes logs to the 'Log.txt' file.</param>
        /// <param name="modEntry.OnToggle">The presence of this function will let the mod manager know that the mod can be safely disabled during the game.</param>
        /// <param name="modEntry.OnGUI">Called to draw UI.</param>
        /// <param name="modEntry.OnSaveGUI">Called while saving.</param>
        /// <param name="modEntry.OnUpdate">Called by MonoBehaviour.Update.</param>
        /// <param name="modEntry.OnLateUpdate">Called by MonoBehaviour.LateUpdate.</param>
        /// <param name="modEntry.OnFixedUpdate">Called by MonoBehaviour.FixedUpdate.</param>
        /// <returns>Returns true, if no error occurred.</returns>
        internal static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModPath = modEntry.Path;
            logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;

            try
            {
                harmony = Harmony12.HarmonyInstance.Create(modEntry.Info.Id);
                harmony.PatchAll(typeof(Main).Assembly);
            }
            catch (Exception ex)
            {
                DebugError(ex);
                throw ex;
            }


            return true;
        }

        #endregion

        #region Load_Patch

        [Harmony12.HarmonyBefore(new string[] { "CallOfTheWild" })]
        [Harmony12.HarmonyPatch(typeof(LibraryScriptableObject), "LoadDictionary")]
        [Harmony12.HarmonyPatch(typeof(LibraryScriptableObject), "LoadDictionary", new Type[0])]
        static class LibraryScriptableObject_LoadDictionary_Patch_Before
        {
            static bool Run = false;
            static void Postfix(LibraryScriptableObject __instance)
            {
                if (Run) return; Run = true;
                Main.library = __instance;
                try
                {
                    Main.DebugLog("Pre-loading Fumi's Codex");
                }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                }
            }
        }

        [Harmony12.HarmonyAfter(new string[] { "CallOfTheWild" })]
        [Harmony12.HarmonyPatch(typeof(LibraryScriptableObject), "LoadDictionary")]
        [Harmony12.HarmonyPatch(typeof(LibraryScriptableObject), "LoadDictionary", new Type[0])]
        static class LibraryScriptableObject_LoadDictionary_Patch_After
        {
            static bool Run = false;
            static void Postfix(LibraryScriptableObject __instance)
            {
                if (Run) return; Run = true;
                if (Main.library == null) Main.library = __instance;
                try
                {
                    Main.DebugLogAlways("Loading Fumi's Codex");

                    //Hexcrafter.createHexcrafter();
                    Hexcrafter.createExtraArcanaFeat();
                    Kineticist.createImpaleInfusion();
                    Kineticist.extendSprayInfusion(Settings.StateManager.State.extendSprayInfusion);
                    Kineticist.createPreciseBlastTalent(Settings.StateManager.State.preciseBlastTalent);
                    Kineticist.createMobileGatheringFeat();
                    Kineticist.createHurricaneQueen();
                    Kineticist.createMindShield(Settings.StateManager.State.mindShieldTalent);
                    Kineticist.createExtraWildTalentFeat(Settings.StateManager.State.extraWildTalentFeat);
                    //Kineticist.createExpandElementalFocus();
                    if (Settings.StateManager.State.slumberHDrestriction) CotW.modSlumber();
                }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                }
            }
        }

        #endregion

    }
}
