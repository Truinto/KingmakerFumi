using Newtonsoft.Json;
using System.IO;

namespace FumisCodex
{
    public class Settings
    {
        [JsonProperty]
        public int version = 5;
        [JsonProperty]
        public bool slumberHDrestriction = false;
        [JsonProperty]
        public bool auraOfDoomFx = true;
        [JsonProperty]
        public bool dazeIsNotStun = true;

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
        public bool debugEnsureGuids = false;

        public static Config.Manager<Settings> StateManager = new Config.Manager<Settings>(Path.Combine(Main.ModPath, "settings.json"));
    }
}
