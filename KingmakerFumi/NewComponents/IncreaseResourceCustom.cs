using System;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;

namespace FumisCodex.NewComponents
{
    public class IncreaseResourceCustom : OwnedGameLogicComponent<UnitDescriptor>, IResourceAmountBonusHandler, IUnitSubscriber
    {
        public void CalculateMaxResourceAmount(BlueprintAbilityResource resource, ref int bonus)
        {
            if (base.Fact.Active && resource == this.Resource)
            {
                int index = 0;
                foreach (BlueprintCharacterClass @class in this.Classes)
                {
                    ClassData player_class = base.Owner.Progression.GetClassData(@class);
                    if (player_class == null) continue; // if the player doesn't have this class, skip it

                    // check for archetypes; passes if all listed archetypes don't fit; passes if one archetype fits and player has it
                    bool eligible = true;
                    foreach (BlueprintArchetype archetype in this.Archetypes)   
                    {
                        if (@class.Archetypes.Contains(archetype) && !player_class.Archetypes.Contains(archetype))
                        {
                            eligible = false;
                            break;
                        }
                    }
                    
                    if (eligible)
                        index += base.Owner.Progression.GetClassLevel(@class);
                }

                if (Invert)
                    index = base.Owner.Progression.CharacterLevel - index;

                bonus += Bonus[index.MinMax(0, Bonus.Length-1)];   // adds a bonus based on an array of int; values can be negative; index is capped
            }
        }

        public int[] Bonus; //important! index 0 is special, since it's basically NO-MATCH

        public bool Invert; //only count levels that don't fit into Classes/Archetypes

        public BlueprintCharacterClass[] Classes;
        public BlueprintArchetype[] Archetypes;

        public BlueprintAbilityResource Resource;
    }
}