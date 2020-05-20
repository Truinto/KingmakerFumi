using Kingmaker;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;
using Newtonsoft.Json;
using Kingmaker.UI.SettingsUI;

namespace FirstAzlanti
{
    public static class Settings
    {
        [JsonProperty]
        public static bool EnableKeepAzlanti = true;
        [JsonProperty]
        public static bool BackupAzlantiOnAutoSave = true;
    }

    public static class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger;
        private static Harmony12.HarmonyInstance harmony;

        internal static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;

            harmony = Harmony12.HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(typeof(Main).Assembly);

            return true;
        }
    }

    [Harmony12.HarmonyPatch(typeof(GameOverIronmanController), nameof(GameOverIronmanController.Activate))]
    public static class KeepSavePatch
    {
        private static bool Prefix()
        {
            if (Settings.EnableKeepAzlanti)
            {
                Main.Logger.Log("Prevented Iron Man loss.");
                LoadingProcess.Instance.ResetManualLoadingScreen();
                return false;
            }
            return true;
        }
    }

    [Harmony12.HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveRoutine))]
    public static class BackupSavePatch
    {
        private static void Prefix(SaveInfo saveInfo)
        {
            //Main.Logger.Log("Saving game... " + (saveInfo.Type == SaveInfo.SaveType.IronMan).ToString() + ":" + SettingsRoot.Instance.OnlyOneSave.CurrentValue.ToString());
            if (Settings.BackupAzlantiOnAutoSave && (saveInfo.Type == SaveInfo.SaveType.IronMan || SettingsRoot.Instance.OnlyOneSave.CurrentValue))
            {
                
                string copy = saveInfo.FolderName.Substring(0, saveInfo.FolderName.Length-4);
                try
                {

                    System.IO.File.Copy(saveInfo.FolderName, copy, true);
                    Main.Logger.Log("Backuped Iron Man save: " + copy);
                }
                catch (Exception)
                {
                    Main.Logger.Log("Save backup failed.");
                }
            }
        }

    }


}
