using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;

namespace FumisCodex.NewComponents
{
    [AllowedOn(typeof(BlueprintAbility))]
    public class AbilityRequirementActionAvailable : BlueprintComponent, IAbilityAvailabilityProvider
    {
        public bool Not;
        public ActionType Action;

        public bool IsAvailableFor(AbilityData ability)
        {
            bool hasUsedAction;
            //hasMoved = ability.Caster.Unit.CombatState.IsFullAttackRestrictedBecauseOfMoveAction;
            //hasMoved = (Kingmaker.Game.Instance.TimeController.GameTime - ability.Caster.Unit.LastMoveTime) < Rounds.Rounds().Seconds;

            if (Action == ActionType.FullRound)
                hasUsedAction = ability.Caster.Unit.CombatState.IsFullAttackRestrictedBecauseOfMoveAction;
            else
                hasUsedAction = ability.Caster.Unit.CombatState.HasCooldownForCommand((UnitCommand.CommandType)Action);
            
            return !hasUsedAction && !Not || hasUsedAction && Not;
        }

        public string GetReason()
        {
            return LocalizedTexts.Instance.Reasons.NoRequiredCondition;
        }
    }

    public enum ActionType
    {
        Free = UnitCommand.CommandType.Free,
        Swift = UnitCommand.CommandType.Swift,
        Move = UnitCommand.CommandType.Move,
        Standard = UnitCommand.CommandType.Standard,
        FullRound = 4
    }
}
