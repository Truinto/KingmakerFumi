using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex.NewComponents
{
    public class ContextActionConcentration : OwnedGameLogicComponent<UnitDescriptor>, ITargetRulebookSubscriber, ITargetRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>
    {
        [NotNull]
        public ActionList FailedAction;

        [NotNull]
        public BlueprintAbility Ability;

        private void RunAction()
        {
            if (this.FailedAction.HasActions)
                (this.Fact as IFactContextOwner)?.RunActionInContext(this.FailedAction, this.Owner.Unit);
        }

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            AbilityData data = new AbilityData(Ability, this.Owner);
            bool saved = Rulebook.Trigger(new RuleCheckConcentration(this.Owner.Unit, data, evt)).Success;

            if (!saved)
                this.RunAction();
        }
    }
}
