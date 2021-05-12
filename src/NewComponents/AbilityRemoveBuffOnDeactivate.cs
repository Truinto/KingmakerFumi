using Kingmaker.Blueprints;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex.NewComponents
{
    [AllowedOn(typeof(BlueprintAbility))]
    [AllowMultipleComponents]
    public class AbilityRemoveBuffOnDeactivate : OwnedGameLogicComponent<UnitDescriptor>
    {
        public override void OnFactDeactivate()
        {
            Main.DebugLog("Run AbilityRemoveBuffOnDeactivate OnFactDeactivate");
            if (Ability != null)
            {
                base.Owner.Get<UnitPartStoreBuffs>()?.Remove(Ability?.AssetGuid);
            }
        }

        public BlueprintAbility Ability;
    }
}
