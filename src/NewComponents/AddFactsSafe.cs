using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FumisCodex.NewComponents
{
    [AllowedOn(typeof(BlueprintUnitFact))]
    public class AddFactsSafe : OwnedGameLogicComponent<UnitDescriptor>, IUnitDisableFeaturesBeforeLevelUpHandler
    {
        public override void OnFactActivate()
        {
            if (!base.IsReapplying)
            {
                this.m_AppliedFacts.Clear();

                foreach (BlueprintUnitFact f in this.Facts)
                {
                    if (!base.Owner.HasFact(f))
                        this.m_AppliedFacts.Add(base.Owner.AddFact(f, null, null));
                }
            }
            else
            {
                foreach (Fact f in this.m_AppliedFacts)
                {
                    if (f != null)
                        f.Recalculate();
                }
            }
        }
        
        public override void OnFactDeactivate()
        {
            if (!base.IsReapplying)
            {
                foreach (Fact f in this.m_AppliedFacts)
                    base.Owner.RemoveFact(f);
                this.m_AppliedFacts.Clear();
            }
        }

        public override void OnRecalculate()
        {
            foreach (Fact fact in this.m_AppliedFacts)
            {
                if (fact != null)
                    fact.Recalculate();
            }
        }

        public override void PostLoad()
        {
            base.PostLoad();
            if (base.Fact.Active && this.RestoreMissingFacts && (this.m_AppliedFacts.Count != this.Facts.Length || this.m_AppliedFacts.HasItem((Fact i) => !base.Owner.HasFact(i)) || this.Facts.HasItem((BlueprintUnitFact f) => !this.m_AppliedFacts.HasItem((Fact i) => i.Blueprint == f))))
            {
                this.OnFactDeactivate();
                this.OnFactActivate();
                Main.DebugLogAlways($"Reapply component {base.GetType().Name} of fact {base.Fact}");
            }
        }

        public void HandleUnitDisableFeaturesBeforeLevelUp()
        {
            this.OnFactDeactivate();
            if (this.Activatable != null)
            {
                foreach (var act in base.Owner.ActivatableAbilities)
                {
                    if (act.Blueprint == this.Activatable)
                        act.IsOn = false;
                }
            }
        }

        [NotNull]
        public BlueprintUnitFact[] Facts;
        
        public bool RestoreMissingFacts;

        [CanBeNull]
        public BlueprintActivatableAbility Activatable;
        
        [JsonProperty]
        private List<Fact> m_AppliedFacts = new List<Fact>();
    }
}
