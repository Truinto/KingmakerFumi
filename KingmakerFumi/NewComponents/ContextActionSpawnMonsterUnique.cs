using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Mechanics.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Buffs;
using Newtonsoft.Json;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.AreaLogic.SummonPool;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Parts;

namespace FumisCodex.NewComponents
{
    //same as spawnmonster, but kills off all monsters of specified pool
    public class ContextActionSpawnMonsterUnique : ContextActionSpawnMonster
    {
        public override void RunAction()
        {
            foreach (UnitEntityData unit in Game.Instance.SummonPools.GetPool(this.SummonPool).Units)
                if (this.Context.MaybeOwner.UniqueId == unit.Get<UnitPartSummonedMonster>().Summoner.UniqueId)
                    unit.Buffs.RemoveFact(Game.Instance.BlueprintRoot.SystemMechanics.SummonedUnitBuff);
            
            base.RunAction();
        }
    }
}