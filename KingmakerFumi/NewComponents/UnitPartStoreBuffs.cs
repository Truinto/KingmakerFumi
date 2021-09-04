using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FumisCodex.NewComponents
{
    public class UnitPartStoreBuffs : UnitPart
    {
        public Dictionary<string, List<Buff>> AppliedBuffs 
        {
            get => m_AppliedBuffs;
        }

        public void Add(Buff buff, string source = null)
        {
            Main.DebugLog("Run UnitPartStoreBuffs Add " + source);
            source = source ?? "";

            m_AppliedBuffs.TryGetValue(source, out List<Buff> buffs);
            if (buffs == null)
            {
                buffs = new List<Buff>();
                m_AppliedBuffs[source] = buffs;
            }
            buffs.Add(buff);
        }

        // remove all granted from source ability
        public void Remove(string source = null)
        {
            Main.DebugLog("Run UnitPartStoreBuffs Remove " + source);
            source = source ?? "";

            m_AppliedBuffs.TryGetValue(source, out List<Buff> buffs);

            if (buffs != null)
            {
                foreach (var buff in buffs)
                {
                    Main.DebugLog("Buff is " + (buff?.Name ?? "null") + " and " + buff?.Active);
                    if (buff != null && buff.Active)
                    {
                        Main.DebugLog("Removing buff " + buff.Name);
                        buff.Remove();
                    }
                }
            }
            else
                Main.DebugLog("List<Buff> buffs is null");

            m_AppliedBuffs.Remove(source);
        }

        // remove single buff
        public bool Remove(Buff buff)
        {
            if (buff == null)
                return false;

            if (buff.Active)
                buff.Remove();

            foreach(var list in m_AppliedBuffs)
            {
                if (list.Value.Remove(buff))
                    return true;
            }
            return false;
        }

        // remove all buffs matching a blueprint
        public bool Remove(BlueprintBuff blueprint)
        {
            if (blueprint == null)
                return false;

            bool flag = false;
            foreach (var list in m_AppliedBuffs)
            {
                for (int i = list.Value.Count-1; i >= 0; i--)
                {
                    if (list.Value[i].Blueprint == blueprint)
                    {
                        list.Value[i].Remove();
                        list.Value.RemoveAt(i);
                        flag = true;
                    }
                }
            }
            return flag;
        }

        public override void OnRemove()
        {
            Main.DebugLog("Run UnitPartStoreBuffs OnRemove");
            if (m_AppliedBuffs == null)
                return;
            foreach (var buffs in m_AppliedBuffs.Values)
            {
                if (buffs != null)
                    foreach (var buff in buffs)
                    {
                        if (buff != null && buff.Active)
                            buff.Remove();
                    }
            }
            m_AppliedBuffs.Clear();
        }

        public override void PreSave()
        {
            try
            {
                Main.DebugLog("Run UnitPartStoreBuffs PreSave");
                List<string> toRemove = new List<string>();

                foreach (var list in m_AppliedBuffs)
                {
                    var buffs = list.Value;

                    for (int i = buffs.Count-1; i >= 0; i--)
                    {
                        if (buffs[i] == null || !buffs[i].Active)
                            buffs.RemoveAt(i);
                    }

                    if (buffs.Count == 0)
                        toRemove.Add(list.Key);
                }

                foreach (var key in toRemove)
                    m_AppliedBuffs.Remove(key);
            }
            catch (Exception e)
            {
                Main.DebugError(e);
            }
        }

        [JsonProperty]
        Dictionary<string, List<Buff>> m_AppliedBuffs = new Dictionary<string, List<Buff>>();
    }
}
