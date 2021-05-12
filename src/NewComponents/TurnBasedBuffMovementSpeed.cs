using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TurnBased.Controllers;

namespace FumisCodex.NewComponents
{
    [AllowedOn(typeof(BlueprintUnitFact))]
    [AllowMultipleComponents]
    public class TurnBasedBuffMovementSpeed : OwnedGameLogicComponent<UnitDescriptor>
    {
		public override void OnTurnOn()
		{
			if (CombatController.IsInTurnBasedCombat() ^ InTurnBasedCombat)
				return;

            int newSpeed = (int)(this.Owner.Stats.Speed.BaseValue * this.Multiplier + this.Bonus);
			newSpeed = newSpeed.MinMax(this.Min, this.Max);

			int bonus = newSpeed - this.Owner.Stats.Speed.BaseValue;

			m_Modifier = Owner.Stats.Speed.AddModifier(bonus, this, this.Descriptor);
		}

		public override void OnTurnOff()
		{
			ModifiableValue.Modifier modifier = this.m_Modifier;
			if (modifier != null)
			{
				modifier.Remove();
			}
			this.m_Modifier = null;
		}

		public bool InTurnBasedCombat;
		public float Multiplier = 1f;
		public int Bonus;
		public int Min = 0;
		public int Max = 60;

		public ModifierDescriptor Descriptor = ModifierDescriptor.Enhancement;
		private ModifiableValue.Modifier m_Modifier;
	}
}
