
using System;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using Newtonsoft.Json;

namespace FumisCodex.NewComponents
{
    public class MedusasWrath : OwnedGameLogicComponent<UnitDescriptor>, IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>, IInitiatorRulebookSubscriber
    {
        [JsonProperty]
        private TimeSpan m_LastUseTime;
        
        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            if (this.m_LastUseTime + 1.Rounds().Seconds <= Game.Instance.TimeController.GameTime &&
                !evt.Initiator.CombatState.IsFullAttackRestrictedBecauseOfMoveAction &&
                COM.CheckWeaponOverride(evt.Initiator, evt.Weapon, WeaponCategory.UnarmedStrike) &&
                (evt.Target.Descriptor.State.HasCondition(UnitCondition.Dazed) ||
                 evt.Target.Descriptor.State.HasCondition(UnitCondition.Paralyzed) ||
                 evt.Target.Descriptor.State.HasCondition(UnitCondition.Staggered) ||
                 evt.Target.Descriptor.State.HasCondition(UnitCondition.Stunned) ||
                 evt.Target.Descriptor.State.HasCondition(UnitCondition.Unconscious) ||
                 Rulebook.Trigger(new RuleCheckTargetFlatFooted(evt.Initiator, evt.Target)).IsFlatFooted))
            {
                    this.m_LastUseTime = Game.Instance.TimeController.GameTime;
                    Rulebook.Trigger(new RuleAttackWithWeapon(evt.Initiator, evt.Target, evt.Weapon, 0));
                    Rulebook.Trigger(new RuleAttackWithWeapon(evt.Initiator, evt.Target, evt.Weapon, 0));
            }
        }
    }
}