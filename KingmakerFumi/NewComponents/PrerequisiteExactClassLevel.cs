using System;
using System.Text;
using JetBrains.Annotations;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace FumisCodex.NewComponents
{
    public class PrerequisiteExactClassLevel : Prerequisite
	{
		public override bool Check(FeatureSelectionState selectionState, UnitDescriptor unit, LevelUpState state)
		{
			int num = 0;
			foreach (ClassLevelsForPrerequisites classLevelsForPrerequisites in unit.Progression.Features.SelectFactComponents<ClassLevelsForPrerequisites>())
			{
				if (classLevelsForPrerequisites.FakeClass == this.CharacterClass)
				{
					num += (int)(classLevelsForPrerequisites.Modifier * (double)unit.Progression.GetClassLevel(classLevelsForPrerequisites.ActualClass) + (double)classLevelsForPrerequisites.Summand);
				}
			}
			return unit.Progression.GetClassLevel(this.CharacterClass) + num == this.Level;
		}

		public override string GetUIText()
		{
			return $"{this.CharacterClass.Name} {UIStrings.Instance.Tooltips.Level}: exactly {this.Level}";
		}

		[NotNull]
		public BlueprintCharacterClass CharacterClass;

		public int Level;
	}
}
