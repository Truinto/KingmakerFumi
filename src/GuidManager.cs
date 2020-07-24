using Kingmaker.Blueprints;
using System;
using System.Collections.Generic;
using System.IO;
using CallOfTheWild;
using UnityEngine;
using Kingmaker.Blueprints.Facts;

namespace FumisCodex
{
    public class GuidManager
    {
        public static GuidManager i = new GuidManager();
        public static LibraryScriptableObject library;

#if DEBUG
        public bool allow_guid_generation = true;
#else
		public bool allow_guid_generation = false;
#endif

        public string filepath = Path.Combine(Main.ModPath, "blueprints.txt");
        public Dictionary<string, string> guid_list = new Dictionary<string, string>();
        public List<string> register = new List<string>();

        private bool loaded = false;
        public void TryLoad()
        {
            if (loaded) return;
            else loaded = true;

            if (!File.Exists(filepath)) return;

            try
            {
                string[] lines = File.ReadAllLines(filepath);
                foreach (string line in lines)
                {
                    string[] items = line.Split('\t');
                    if (items.Length >= 2)
                        guid_list[items[0]] = items[1];
                }
            } catch (Exception e) {
                Main.DebugLogAlways(e.ToString());
            }
        }

        private void Write(string key, string guid)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filepath, append: true))
                {
                    writer.WriteLine(key + '\t' + guid);
                }
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            }
        }

        ///<summary>Used to dump all guids to file. Use once when needed.</summary>
        public void WriteAll()
        {
            if (!allow_guid_generation) return;
            if (library == null) throw new InvalidOperationException("library must not be null when WriteAll is called");
            TryLoad();

            using (StreamWriter writer = new StreamWriter(filepath, append: false))
            {
                foreach (KeyValuePair<string, string> pair in guid_list)
                {
                    BlueprintScriptableObject obj = null;
                    try { obj = library.Get<BlueprintScriptableObject>(pair.Value); } catch (Exception) { }
                    if (obj != null)
                    {
                        writer.WriteLine(pair.Key + '\t' + pair.Value + '\t' + obj.GetType().FullName);
                        if (pair.Key != obj.name) Debug.LogError(pair.Key + " != " + obj.name);
                    }
                    else
                        Main.DebugLogAlways(pair.Value+" does not exist");
                }

                foreach (string guid in register)
                {
                    BlueprintScriptableObject obj = null;
                    try { obj = library.Get<BlueprintScriptableObject>(guid); } catch (Exception) { }
                    if (obj != null)
                        writer.WriteLine(obj.name + '\t' + guid + '\t' + obj.GetType().FullName);
                    else
                        Main.DebugLogAlways(guid+" does not exist");
                }
            }
        }

        ///<summary>When you already have a guid, but want it dumped.</summary>
        public string Reg(string guid)
        {
            if (allow_guid_generation)
                register.Add(guid);
            return guid;
        }

        ///<summary>Gets or makes a new guid.</summary>
        ///<key="key">Blueprint.name</key>
        public string Get(string key)
        {
            TryLoad();

            string result;
            guid_list.TryGetValue(key, out result);

            if (result == null)
            {
                if (!allow_guid_generation)
                    throw new Exception("Tried to generate a new GUID while not allowed! " + key);

                result = Guid.NewGuid().ToString("N");
                guid_list[key] = result;
                Write(key, result);
            }

            return result;
        }

        ///<summary>Generates dummy objects to ensure saves can be loaded.</summary>
        public void Ensure()
        {
            if (!File.Exists(filepath)) return;
            if (library == null) throw new InvalidOperationException("library must not be null when Ensure is called");

            try
            {
                string[] lines = File.ReadAllLines(filepath);
                foreach (string line in lines)
                {
                    string[] items = line.Split('\t');
                    if (items.Length >= 3)
                    {
                        guid_list[items[0]] = items[1];

                        BlueprintScriptableObject obj = null;
                        try { obj = library.Get<BlueprintScriptableObject>(items[1]); } catch (Exception) { }
                        if (obj == null)
                        {
                            Main.DebugLogAlways(items[1]+" not found");
                            //obj = (BlueprintScriptableObject)ScriptableObject.CreateInstance(Type.GetType(items[3]) ?? typeof(BlueprintScriptableObject));
                            obj = ScriptableObject.CreateInstance<BlueprintScriptableObject>();
                            obj.name = items[0];
                            library.AddAsset(obj, items[1]);
                        }
                        
                    }
                }
            } catch (Exception e) {
                Main.DebugLogAlways(e.ToString());
            }
        }

    }
}
