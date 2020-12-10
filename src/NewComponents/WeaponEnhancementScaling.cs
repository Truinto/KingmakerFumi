using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;

namespace FumisCodex.NewComponents
{
    public class WeaponEnhancementScaling : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RuleCalculateWeaponStats>, IRulebookHandler<RuleCalculateWeaponStats>, IInitiatorRulebookSubscriber, IInitiatorRulebookHandler<RuleCalculateAttackBonusWithoutTarget>, IRulebookHandler<RuleCalculateAttackBonusWithoutTarget>
    {
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (evt.Weapon == base.Owner)
            {
                if (base.Owner.Enchantments.SelectMany(p => p.SelectComponents<WeaponEnhancementScaling>()).FirstOrDefault() == this)
                {
                    int bonus = this.CalculateBonus(evt.Weapon.Wielder);
                    evt.AddBonusDamage(bonus);
                    evt.Enhancement += bonus;
                }
            }
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt)
        {
        }

        public void OnEventAboutToTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
            if (evt.Weapon == base.Owner)
            {
                if (base.Owner.Enchantments.SelectMany(p => p.SelectComponents<WeaponEnhancementScaling>()).FirstOrDefault() == this)
                {
                    evt.AddBonus(this.CalculateBonus(evt.Weapon.Wielder), base.Fact);
                }
            }
        }

        public void OnEventDidTrigger(RuleCalculateAttackBonusWithoutTarget evt)
        {
        }

        public int CalculateBonus(UnitDescriptor unit)
        {
			int result = this.BaseValue;
            int level_total = 0;
            foreach (BlueprintCharacterClass @class in this.Class)
            {
                bool flag2 = true;
                foreach (BlueprintArchetype archetype in this.Archetype)
                {
                    ClassData classData = unit.Progression.GetClassData(@class);
                    flag2 = (classData == null || !Enumerable.Contains<BlueprintArchetype>(@class.Archetypes, archetype) || classData.Archetypes.Contains(archetype));
                }
                level_total += (flag2 ? unit.Progression.GetClassLevel(@class) : 0);
            }
            level_total += (int)((float)(unit.Progression.CharacterLevel - level_total) * this.OtherClassesModifier);
            if (this.StartingLevel <= level_total)
            {
                result += Math.Max(this.StartValue + this.PerStepIncrease * (level_total - this.StartingLevel) / this.LevelStep, this.MinValue);
            }

			return Math.Min(result, 5);
        }

		[NotNull]
        public BlueprintCharacterClass[] Class;
		[NotNull]
        public BlueprintArchetype[] Archetype;
		public float OtherClassesModifier = 0f;
		public int BaseValue = 0;
		public int MinValue = 0;

		public int StartingLevel;
		public int StartValue;
		public int LevelStep;
		public int PerStepIncrease;
    }
}
