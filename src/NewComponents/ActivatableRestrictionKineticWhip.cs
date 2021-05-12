using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex.NewComponents
{
    public class ActivatableRestrictionKineticWhip : ActivatableAbilityRestriction
    {
        public override bool IsAvailable()
        {
            UnitDescriptor unit = this.Owner;
			UnitBody body = unit.Body;

			// false if hands not useable
			if (body.IsPolymorphed || !body.HandsAreEnabled)
				return false;

			// false if not kineticist
			UnitPartKineticist unitPartKineticist = unit.Get<UnitPartKineticist>();
			if (!unitPartKineticist)
				return false;

			// false if weapon not kinetic blade
			if (body.PrimaryHand.MaybeWeapon?.Blueprint?.GetComponent<WeaponKineticBlade>() == null)
				return false;

			return true;
		}
    }
}
