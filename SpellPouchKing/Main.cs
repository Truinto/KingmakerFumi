using Kingmaker.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;
using HarmonyLib;

// TODO: make publicized dlls

namespace SpellPouchKingKing
{
    public class Main
    {
        public static Harmony harmony;
        public static LibraryScriptableObject library;
        public static string ModPath;


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

        internal static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModPath = modEntry.Path;
            logger = modEntry.Logger;

            try
            {
                harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(typeof(Main).Assembly);
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
    }
}

/*
     public partial class Main
    {
        private static GUIStyle StyleBox;
        private static GUIStyle StyleLine;

        static partial void OnLoad(UnityModManager.ModEntry modEntry)
        {
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnHideGUI = OnHideGUI;
        }

        public static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (StyleBox == null)
            {
                StyleBox = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter };
                StyleLine = new GUIStyle() { fixedHeight = 1, margin = new RectOffset(0, 0, 4, 4), };
                StyleLine.normal.background = new Texture2D(1, 1);
            }

            if (GUILayout.Button("Reload Ability Groups 'DefGroups.json'", GUILayout.ExpandWidth(false)))
                Patch_AbilityGroups.Reload();
            Checkbox(ref DefGroup.Unlocked, "Show all Ability Groups", b => DefGroup.RefreshUI());
        }

        public static void OnHideGUI(UnityModManager.ModEntry modEntry)
        {
        }

        public static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
        }

        static partial void OnBlueprintsLoaded()
        {
            using var scope = new Scope(Main.ModPath, Main.logger);

            PatchSafe(typeof(Patch_AbilityGroups));
            SubscribeSafe(typeof(Patch_AbilityGroups));
        }
    }
 */