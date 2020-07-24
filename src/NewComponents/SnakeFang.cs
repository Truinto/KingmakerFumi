

using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using Newtonsoft.Json;

namespace FumisCodex.NewComponents
{
    [AllowedOn(typeof(BlueprintUnitFact))]
    [AllowMultipleComponents]
    public class SnakeFang : OwnedGameLogicComponent<UnitDescriptor>, IGlobalRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>, IGlobalRulebookSubscriber
    {
        // [JsonProperty]
        // private TimeSpan m_LastUseTime;

        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            // if (this.m_LastUseTime + 1.Rounds().Seconds > Game.Instance.TimeController.GameTime)
            //     return;
            // this.m_LastUseTime = Game.Instance.TimeController.GameTime;

            if (evt.Target == this.Owner.Unit && !evt.IsHit)
            {
                Game.Instance.CombatEngagementController.ForceAttackOfOpportunity(this.Owner.Unit, evt.Initiator);
            }
        }
    }
}