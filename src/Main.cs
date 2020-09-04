using System;
using System.Linq;
using UnityModManagerNet;
using Kingmaker.Blueprints;
using UnityEngine;
using Guid = FumisCodex.GuidManager;
using Kingmaker.Utility;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Enums;
using System.Reflection;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.EntitySystem.Stats;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;
using Kingmaker.Controllers.Brain.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;

namespace FumisCodex
{
    internal class Main
    {
        internal static HarmonyLib.Harmony harmony;
        internal static LibraryScriptableObject library;

        public static bool COTWpresent = false;

        /// <summary>True if mod is enabled. Doesn't do anything right now.</summary>
        internal static bool Enabled { get; set; } = true;
        /// <summary>Path of current mod.</summary>
        public static string ModPath { get; set; }

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

        /// <summary>Called when the mod is turned to on/off.
        /// With this function you control an operation of the mod and inform users whether it is enabled or not.</summary>
        /// <param name="value">true = mod to be turned on; false = mod to be turned off</param>
        /// <returns>Returns true, if state can be changed.</returns>
        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Main.Enabled = value;
            return true;
        }

        /// <summary>Draws the GUI</summary>
        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Label("Disclaimer: Remember that playing with mods makes them mandatory for your save game! If you want to uninstall anyway, then you need to remove all references to said mod. In my case respec all your characters, that should do the trick.");

            Checkbox(ref Settings.StateManager.State.slumberHDrestriction, "CotW - remove slumber HD restriction");
            Checkbox(ref Settings.StateManager.State.auraOfDoomFx, "CotW [*] - Aura of Doom visible area effect", CotW.modAuraOfDoomToogle);
            Checkbox(ref Settings.StateManager.State.dazeIsNotStun, "CotW [*] - Dazing Spell does not count as MindAffecting or Compulsion", CotW.modDazeToogle);
            Checkbox(ref Settings.StateManager.State.extendSprayInfusion, "Kineticist - Spray infusion may be used with cold blast");
            Checkbox(ref Settings.StateManager.State.extraWildTalentFeat, "Kineticist [F] - Feat for extra wild talents");
            Checkbox(ref Settings.StateManager.State.preciseBlastTalent, "Kineticist [F] - Utility talent for precise blasts (similiar to alchemist discovery, but for blasts)");
            Checkbox(ref Settings.StateManager.State.mindShieldTalent, "Kineticist [F] - Mind Shield: Utility talent to reduce Mind Burn's effect on Psychokineticist");
            Checkbox(ref Settings.StateManager.State.fixShamblingMoundGrapple, "Fix - Shambling Mound can act while grappling");
            Checkbox(ref Settings.StateManager.State.cheatCombineParametrizedFeats, "Cheat [*] - picking Weapon Focus (Greater etc.) will grant all in the same group");
            GUILayout.Label("Stuff in the options are either homebrew or. For a full list read the mods description."
                + "\nLegend: [F] This adds a feat. You still need to pick feats/talents for these effects. If you already picked these features, then they stay in effect regardless of the option above."
                + "\n[*] Option is active immediately.");
            if (GUILayout.Button("Save settings!"))
            {
                Settings.StateManager.TrySaveConfigurationState();
            }
            
        }
        private static void Checkbox(ref bool value, string label, Action<bool> action = null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"{(value ? "<color=green><b>✔</b></color>" : "<color=red><b>✖</b></color>")} {label}", GUILayout.ExpandWidth(false)))
            {
                value = !value;
                action?.Invoke(value);
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>Unused example.</summary>
        private static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
        {
            if (Input.GetKeyDown(KeyCode.F1)) { }
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.StateManager.TrySaveConfigurationState();
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
            modEntry.OnSaveGUI = OnSaveGUI;
            
            try
            {
                harmony = new HarmonyLib.Harmony(modEntry.Info.Id);
                harmony.PatchAll(typeof(Main).Assembly);
                Main.harmony.Patch(HarmonyLib.AccessTools.Method(typeof(EnumUtils), nameof(EnumUtils.GetMaxValue), null, new Type[] { typeof(ActivatableAbilityGroup) }),
                    postfix: new HarmonyLib.HarmonyMethod(typeof(Patch_ActivatableAbilityGroup).GetMethod("Postfix")));
            }
            catch (Exception ex)
            {
                DebugError(ex);
#if DEBUG
                throw ex;
#endif
            }


            return true;
        }

#endregion

        #region Load_Patch

        [HarmonyLib.HarmonyBefore(new string[] { "CallOfTheWild" })]
        [HarmonyLib.HarmonyPatch(typeof(LibraryScriptableObject), "LoadDictionary", new Type[0])]
        static class LibraryScriptableObject_LoadDictionary_Patch_Before
        {
            static bool Run = false;
            static void Postfix(LibraryScriptableObject __instance)
            {
                if (Run) return; Run = true;
                Main.library = __instance;
                Guid.library = __instance;
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

        [HarmonyLib.HarmonyAfter(new string[] { "CallOfTheWild" })]
        [HarmonyLib.HarmonyPatch(typeof(LibraryScriptableObject), "LoadDictionary", new Type[0])]
        static class LibraryScriptableObject_LoadDictionary_Patch_After
        {
            static bool Run = false;
            static void Postfix(LibraryScriptableObject __instance)
            {
                if (Run) return; Run = true;
                if (Main.library == null) Main.library = __instance;
                if (Guid.library == null) Guid.library = __instance;

                try
                {
                    COTWpresent = Main.CheckCOTW();
                }
                catch (Exception e)
                {
                    Main.DebugLogAlways(e.Message + "\n" + e.StackTrace);
                    COTWpresent = false;
                }

                try
                {
                    Main.DebugLogAlways("Loading Fumi's Codex");

                    //try {
                    //    Monk.MOMS_wildcardgroup = Main.Patch_ActivatableAbilityGroup.GetNewGroup();
                    //} catch (Exception) { Main.DebugLogAlways("failed GetNewGroup"); }

                    LoadSafe(Hexcrafter.createHexcrafter);
                    LoadSafe(Hexcrafter.createExtraArcanaFeat);
                    LoadSafe(Hexcrafter.createHexStrikeFeat);

                    LoadSafe(Rogue.createFlensingStrike);

                    LoadSafe(Kineticist.init);
                    LoadSafe(Kineticist.createImpaleInfusion);
                    LoadSafe(Kineticist.extendSprayInfusion, Settings.StateManager.State.extendSprayInfusion);
                    LoadSafe(Kineticist.createPreciseBlastTalent, Settings.StateManager.State.preciseBlastTalent);
                    LoadSafe(Kineticist.createMobileGatheringFeat);
                    LoadSafe(Kineticist.createHurricaneQueen);
                    LoadSafe(Kineticist.createMindShield, Settings.StateManager.State.mindShieldTalent);
                    LoadSafe(Kineticist.createFlight);
                    LoadSafe(Kineticist.createShiftEarth);
                    LoadSafe(Kineticist.createSparkofLife);
                    LoadSafe(Kineticist.fixWallInfusion);
                    LoadSafe(Kineticist.createMobileBlast);
                    LoadSafe(Kineticist.createWoodSoldiers);
                    LoadSafe(Kineticist.createExtraWildTalentFeat, Settings.StateManager.State.extraWildTalentFeat);//must be after new talents
                    LoadSafe(Kineticist.fixExpandElement);

                    LoadSafe(Monk.allowTWFwithFists);
                    LoadSafe(Monk.createMedusasWrath);
                    LoadSafe(Monk.createStyleMaster, true);
                    LoadSafe(Monk.createSnakeStyle);
                    LoadSafe(Monk.createBoarStyle);
                    LoadSafe(Monk.createWolfStyle);
                    LoadSafe(Monk.modKiPowers, true);
                    LoadSafe(Monk.createKiLeech);
                    LoadSafe(Monk.createOneTouch);
                    LoadSafe(Monk.createMasterOfManyStyles);//must be after new styles

                    LoadSafe(Fixes.fixShamblingMoundGrapple);

                    LoadSafe(CotW.modSlumber, Settings.StateManager.State.slumberHDrestriction);
                    LoadSafe(CotW.modAuraOfDoomToogle, Settings.StateManager.State.auraOfDoomFx);
                    LoadSafe(CotW.modDazeToogle, Settings.StateManager.State.dazeIsNotStun);

                    //if (Settings.StateManager.State.debugEnsureGuids) Guid.i.Ensure(); does not work... too bad
#if DEBUG
                    Main.DebugLog("Running in debug.");
                    Guid.i.WriteAll();
#endif
                }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                }
            }
        }

        #endregion

        #region Helper

        public static bool CheckCOTW()
        {
            if (Settings.StateManager.State.CallOfTheWild == "ON")
                return true;
            if (Settings.StateManager.State.CallOfTheWild == "OFF")
                return false;
            
            if (CallOfTheWild.Helpers.classes == null)
            {
            }
            return true;
        }

        public static bool ShouldLoadBackupCOTW()
        {
            return Settings.StateManager.State.CallOfTheWild != "ON";
        }

        public static bool LoadSafe(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (System.Exception e)
            {
                Main.DebugError(e);
                return false;
            }
        }

        public static bool LoadSafe(Action<bool> action, bool flag)
        {
            try
            {
                action(flag);
                return true;
            }
            catch (System.Exception e)
            {
                Main.DebugError(e);
                return false;
            }
        }

        #endregion

        #region Special Patches

        //[HarmonyLib.HarmonyPatch(typeof(EnumUtils), nameof(EnumUtils.GetMaxValue))] since this is a generic method, we need to patch this manually, see Main.Load
        public static class Patch_ActivatableAbilityGroup
        {
            public static int ExtraGroups = 0;
            public static bool GameAlreadyRunning = false;

            ///<summary>Calls this to register a new group. Returns your new enum.</summary>
            public static ActivatableAbilityGroup GetNewGroup()
            {
                if (GameAlreadyRunning)
                    return 0;
                
                ExtraGroups++;
                return (ActivatableAbilityGroup) (Enum.GetValues(typeof(ActivatableAbilityGroup)).Cast<int>().Max() + ExtraGroups);
            }

            public static void Postfix(ref int __result)
            {
                __result += ExtraGroups;
            }
        }

        #endregion

    }
}
