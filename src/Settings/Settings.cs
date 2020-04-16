using Newtonsoft.Json;
using System.IO;

namespace FumisCodex
{
    public class Settings
    {
        [JsonProperty]
        public int version = 1;
        [JsonProperty]
        public bool slumberHDrestriction = false;
        
        public static Config.Manager<Settings> StateManager = new Config.Manager<Settings>(Path.Combine(Main.ModPath, "settings.json"));
    }
}
