using Kingmaker.PubSubSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex
{
    public interface IUnitDisableFeaturesBeforeLevelUpHandler : IUnitSubscriber
    {
        void HandleUnitDisableFeaturesBeforeLevelUp();
    }
}
