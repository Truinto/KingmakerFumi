using Kingmaker;
using Kingmaker.AreaLogic.SummonPool;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex.NewComponents
{
    public class ContextActionKillSummons : ContextAction
    {
        public BlueprintSummonPool SummonPool;
        public BlueprintBuff[] Buffs;

        public override string GetCaption()
        {
            return "Kill Summon Pool";
        }
		
		public List<UnitEntityData> GetPoolForSummoner(ISummonPool pool)
		{
			List<UnitEntityData> list = new List<UnitEntityData>();
			
            foreach (UnitEntityData unit in pool.Units)
            {
                if (this.Context.MaybeOwner.UniqueId == unit.Get<UnitPartSummonedMonster>().Summoner.UniqueId)
                {
					list.Add(unit);
                }
            }
			return list;
		}
		
		public int GetBuffCount()
		{
			if (this.Buffs == null || this.Buffs.Count() < 1 || this.Buffs[0] == null)
				return 0;
			
			int count = 0;
			
			foreach(var buff in this.Context.MaybeOwner.Buffs)
			{
				if (this.Buffs.Contains(buff.Blueprint))
					count++;
			}
			
			return count;
		}

        public override void RunAction()
        {
            try
            {
                ISummonPool pool = Game.Instance.SummonPools.GetPool(this.SummonPool);
                if (pool == null)
                    return;

                var list = GetPoolForSummoner(pool);
                int diff = list.Count - GetBuffCount();

                Main.DebugLog("ContextActionKillSummons pool=" + list.Count + " buffcount=" + (GetBuffCount()-1) + " diff=" + diff);
                
                for (; diff >= 0; diff--)
                    list[diff].Buffs.RemoveFact(Game.Instance.BlueprintRoot.SystemMechanics.SummonedUnitBuff);

            }
            catch (Exception e)
            {
                Main.DebugLog(e.ToString());
            }
			
        }
    }
}
