using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;

namespace FumisCodex.NewComponents
{
    public class ActivatableRestrictionPrerequisite : ActivatableAbilityRestriction
    {
        public BlueprintFeature Feature;

        public bool Not;

        public override bool IsAvailable()
        {
            bool flag = true;
            foreach (var preq in Feature.GetComponents<Prerequisite>())
            {
                try
                {
                    flag &= preq.Check(null, base.Owner, null); //this will fail for some components, noteably Alignment checks
                }
                catch (System.Exception)
                { 
                    FumisCodex.Main.DebugLog($"Failed ActivatableRestrictionPrerequisite on {preq.GetType()} for: {Feature.Name}"); 
                }
            }

            return flag;


        }
    }
}