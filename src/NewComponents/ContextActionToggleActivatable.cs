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

namespace FumisCodex.NewComponents
{
    public class ContextActionToggleActivatable : ContextAction
    {
        public BlueprintActivatableAbility Activatable;
        public bool Toggle = false;
        public bool TurnOn = true;

        [CanBeNull]
        public ActionList OnSuccess;
        [CanBeNull]
        public ActionList OnFailure;

        public override string GetCaption()
        {
            return "Toggle Activatable";
        }

        public override void RunAction()
        {
            if (this.Context.MaybeOwner == null)
            {
                Main.DebugLogAlways("ActionToggleActivatable Owner is null");
                return;
            }
            
            foreach (var fact in this.Context.MaybeOwner.ActivatableAbilities)
            {
                if (fact.Blueprint == this.Activatable)
                {
                    if (this.Toggle)
                        fact.IsOn = !fact.IsOn;
                    else if (fact.IsOn == this.TurnOn)
                        OnFailure?.Run();
                    else
                    {
                        fact.IsOn = this.TurnOn;
                        OnSuccess?.Run();
                    }
                    break;
                }
            }
        }
        
    }
}
