using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace FumisCodex.NewComponents
{
    public class ContextActionSpawnMonsterLeveled : ContextActionSpawnMonster
    {
        [NotNull]
        public int[] LevelThreshold;
        [NotNull]
        public BlueprintUnit[] BlueprintPool;

        public override void RunAction()
        {
            if (LevelThreshold == null || BlueprintPool == null || LevelThreshold.Length != BlueprintPool.Length)
                throw new ArgumentException("ContextActionSpawnMonsterLeveled must have arrays of the same length.");

            for (int i = LevelThreshold.Length - 1; i >= 0; i--)
            {
                if (this.Context.Params.CasterLevel >= LevelThreshold[i])
                {
                    this.Blueprint = BlueprintPool[i];
                    break;
                }
            }

            base.RunAction();
        }
    }
}
