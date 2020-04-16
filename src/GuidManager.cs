using Kingmaker.Blueprints;
using System;
using System.Collections.Generic;
using System.IO;

namespace FumisCodex
{
    public class GuidManager
    {
        public static readonly string filepath = Path.Combine(Main.ModPath, "blueprints.txt");
#if DEBUG
        public static bool allow_guid_generation = true;
#else
        public static bool allow_guid_generation = false;
#endif

        public static Dictionary<string, string> guid_list = new Dictionary<string, string>();

        public static void Load()
        {
            if (!File.Exists(filepath))
                return;

            using (StringReader reader = new StringReader(filepath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] items = line.Split('\t');
                    guid_list.Add(items[0], items[1]);
                }
            }
        }

        public static void Write(string key, string guid, BlueprintScriptableObject blueprint = null)
        {
            using (StreamWriter writer = new StreamWriter(filepath, append: true))
            {
                writer.WriteLine(key + '\t' + guid); // + blueprint!=null ? '\t' + blueprint.GetType().FullName : "");
            }
        }

        public static string NewGuid(string key, BlueprintScriptableObject blueprint = null)
        {
            if (!allow_guid_generation)
                throw new Exception("Tried to generate a new GUID while not allowed! " + key);

            string guid = Guid.NewGuid().ToString("N");
            Write(key, guid, blueprint);
            return guid;
        }

    }
}
