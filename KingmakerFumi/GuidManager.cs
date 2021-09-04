using Kingmaker.Blueprints;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;

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
                    try { obj = library.Get<BlueprintScriptableObject>(pair.Value); } catch (Exception e) { Main.DebugLogAlways("WriteAll guid_list: " + e.Message); }
                    if (obj != null)
                    {
                        writer.WriteLine(pair.Key + '\t' + pair.Value + '\t' + obj.GetType().FullName);
                        if (pair.Key != obj.name) Main.DebugLogAlways(pair.Key + " != " + obj.name);
                    }
                    else
                    {
                        Main.DebugLogAlways(pair.Value + " does not exist");
                        writer.WriteLine(pair.Key + '\t' + pair.Value + '\t' + "NULL");
                    }
                }

                foreach (string guid in register)
                {
                    if (guid_list.ContainsValue(guid))
                        continue;

                    BlueprintScriptableObject obj = null;
                    try { obj = library.Get<BlueprintScriptableObject>(guid); } catch (Exception e) { Main.DebugLogAlways("WriteAll register: " + e.Message); }
                    if (obj != null)
                    {
                        writer.WriteLine(obj.name + '\t' + guid + '\t' + obj.GetType().FullName);
                    }
                    else
                    {
                        Main.DebugLogAlways(guid + " does not exist");
                        writer.WriteLine("UNKNOWN" + '\t' + guid + '\t' + "NULL");
                    }
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

                Main.DebugLogAlways("Warning: Generating new GUID for " + key);
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
                            obj = (BlueprintScriptableObject)ScriptableObject.CreateInstance(Type.GetType(items[3]) ?? typeof(BlueprintScriptableObject));

                            if (items[3].EndsWith("BlueprintAbility"))
                                obj = HelperEA.CreateAbility(items[0], "NULL", "NULL", items[1], null, AbilityType.Extraordinary, UnitCommand.CommandType.Standard, AbilityRange.Personal, "", "");
                            else if (items[3].EndsWith("BlueprintBuff"))
                                obj = HelperEA.CreateBuff(items[0], "NULL", "NULL", items[1], null, Contexts.NullPrefabLink);
                            else if (items[3].EndsWith("BlueprintFeature"))
                                obj = HelperEA.CreateFeature(items[0], "NULL", "NULL", items[1], null, FeatureGroup.None);
                            else if (items[3].EndsWith("BlueprintFeatureSelection"))
                                obj = HelperEA.CreateFeatureSelection(items[0], "NULL", "NULL", items[1], null, FeatureGroup.None);
                            else if (items[3].EndsWith("BlueprintActivatableAbility"))
                                obj = HelperEA.CreateActivatableAbility(items[0], "NULL", "NULL", items[1], null, library.Get<BlueprintBuff>("5898bcf75a0942449a5dc16adc97b279"), AbilityActivationType.Immediately, UnitCommand.CommandType.Standard, null);
                            else if (items[3].EndsWith("BlueprintAbilityAreaEffect"))
                                obj = Helper.CreateBlueprintAbilityAreaEffect(items[0], items[1]);
                            else if (items[3].EndsWith("BlueprintSummonPool"))
                            {
                                obj = Helper.Create<BlueprintSummonPool>();
                                obj.name = items[0];
                                obj.AssetGuid = items[1];
                                HelperEA.AddAsset(library, obj, items[1]);
                            }
                            else if (items[3].EndsWith("BlueprintUnit"))
                            {
                                obj = Helper.Create<Kingmaker.Blueprints.BlueprintUnit>();
                                obj.name = items[0];
                                obj.AssetGuid = items[1];
                                HelperEA.AddAsset(library, obj, items[1]);
                            }
                            else if (items[3].EndsWith("BlueprintArchetype"))
                            {
                                obj = Helper.Create<Kingmaker.Blueprints.Classes.BlueprintArchetype>();
                                obj.name = items[0];
                                obj.AssetGuid = items[1];
                                HelperEA.AddAsset(library, obj, items[1]);
                            }
                            else if (items[3].EndsWith("BlueprintSpellList"))
                            {
                                obj = Helper.Create<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList>();
                                obj.name = items[0];
                                obj.AssetGuid = items[1];
                                HelperEA.AddAsset(library, obj, items[1]);
                            }
                            else if (items[3].EndsWith("BlueprintSpellbook"))
                            {
                                obj = Helper.Create<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellbook>();
                                obj.name = items[0];
                                obj.AssetGuid = items[1];
                                HelperEA.AddAsset(library, obj, items[1]);
                            }
                            else
                            {
                                obj = (BlueprintScriptableObject)ScriptableObject.CreateInstance(Type.GetType(items[3]) ?? typeof(BlueprintScriptableObject));
                                obj.name = items[0];
                                obj.AssetGuid = items[1];
                                HelperEA.AddAsset(library, obj, items[1]);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                Main.DebugLogAlways(e.ToString());
            }
        }

    }
}
