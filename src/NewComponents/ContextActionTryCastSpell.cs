using System;
using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;

namespace FumisCodex.NewComponents
{
	/// Checks that target is valid before casting the spell
	public class ContextActionTryCastSpell : ContextAction
	{
		public override string GetCaption()
		{
			return $"Try casting spell {Spell} DC: {(OverrideDC ? DC : 0)} SL: {(OverrideSpellLevel ? SpellLevel : 0)}";
		}

		public override void RunAction()
		{
			if (base.Context.MaybeCaster == null)
				return;

			AbilityData abilityData = new AbilityData(this.Spell, base.Context.MaybeCaster.Descriptor);
			if (this.OverrideDC)
				abilityData.OverrideDC = new int?(this.DC.Calculate(base.Context));

			if (this.OverrideSpellLevel)
				abilityData.OverrideSpellLevel = new int?(this.SpellLevel.Calculate(base.Context));

			if (abilityData.CanTarget(base.Target))
			{
				Rulebook.Trigger<RuleCastSpell>(new RuleCastSpell(abilityData, base.Target));
				this.Succeed?.Run();
			}
			else
			{
				Main.DebugLog("ContextActionTryCastSpell: Invalid target for spell " + Spell.name);
				this.Failed?.Run();
			}
		}

		public BlueprintAbility Spell;

		public bool OverrideDC;

		[ShowIf("OverrideDC")]
		public ContextValue DC;

		public bool OverrideSpellLevel;

		[ShowIf("OverrideSpellLevel")]
		public ContextValue SpellLevel;

		public ActionList Succeed;
		public ActionList Failed;
	}
}
