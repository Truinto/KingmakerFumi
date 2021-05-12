using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex.NewComponents
{
    // NOTE: doesn't seem to work
    public class WeaponCalculateStatBonus : OwnedGameLogicComponent<UnitDescriptor>, IInitiatorRulebookHandler<RuleCalculateWeaponStats>, IRulebookHandler<RuleCalculateWeaponStats>, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (this.Weapon != null && !this.Weapon.HasItem(evt.Weapon.Blueprint))
                return;

            var inc_reach = new ModifiableValue.Modifier() 
            { 
                ModDescriptor = Descriptor,
                ModValue = this.Value,
                AppliedTo = new ModifiableValue(this.StatType)
            };
            
            evt.AddTemporaryModifier(inc_reach);
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt)
        {
        }

        public ModifierDescriptor Descriptor = ModifierDescriptor.Enhancement;
        public StatType StatType;
        public int Value;
        public BlueprintItemWeapon[] Weapon;
    }
}
