using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace FumisCodex
{
    public class Settings
    {
        [JsonProperty]
        public int version = 7;

        [JsonProperty]
        public string CallOfTheWild = "ON"; // "ON" or "OFF" or "auto"

        [JsonProperty]
        public bool slumberHDrestriction = false;
        [JsonProperty]
        public bool auraOfDoomFx = true;
        [JsonProperty]
        public bool dazeIsNotStun = true;
        [JsonProperty]
        public bool eidolonLifeLink = true;

        [JsonProperty]
        public int magicItemBaseCost = 1000;
        [JsonProperty]
        public int pearlRunestoneDailyUses = 2;


        [JsonProperty]
        public bool extendSprayInfusion = true;
        [JsonProperty]
        public bool extraWildTalentFeat = true;
        [JsonProperty]
        public bool preciseBlastTalent = true;
        [JsonProperty]
        public bool mindShieldTalent = true;
        [JsonProperty]
        public bool fixShamblingMoundGrapple = false;
        
        [JsonProperty]
        public bool cheatCombineParametrizedFeats = false;
        
        [JsonProperty]
        public bool debugEnsureGuids = true;

        [JsonProperty]
        public List<string> doNotLoad = new List<string>();

        public static Config.Manager<Settings> StateManager = new Config.Manager<Settings>(Path.Combine(Main.ModPath, "settings.json"));
    }
}
