using Newtonsoft.Json;
using System.IO;

namespace FumisCodex
{
    public class Settings
    {
        [JsonProperty]
        public int version = 2;
        [JsonProperty]
        public bool slumberHDrestriction = false;
        [JsonProperty]
        public bool extendSprayInfusion = true;
        [JsonProperty]
        public bool extraWildTalentFeat = true;
        [JsonProperty]
        public bool preciseBlastTalent = true;
        [JsonProperty]
        public bool mindShieldTalent = true;

        [JsonProperty]
        public bool cheatCombineParametrizedFeats = false;

        public static Config.Manager<Settings> StateManager = new Config.Manager<Settings>(Path.Combine(Main.ModPath, "settings.json"));
    }
}
