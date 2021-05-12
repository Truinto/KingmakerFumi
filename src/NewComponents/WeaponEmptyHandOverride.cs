using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;

namespace FumisCodex.NewComponents
{
    public class WeaponEmptyHandOverride : OwnedGameLogicComponent<UnitDescriptor>, IInitiatorRulebookHandler<RuleCalculateWeaponStats>, IRulebookHandler<RuleCalculateWeaponStats>, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (WeaponMonk && !evt.Weapon.Blueprint.IsMonk)
                return;

            DiceFormula weaponmax = evt.Weapon.Blueprint.BaseDamage;
            DiceFormula unarmedmax = base.Owner.Body.EmptyHandWeapon?.Blueprint.BaseDamage ?? DiceFormula.Zero;
            DiceFormula monkmax = MonkStrikeLevel(evt.Initiator.Descriptor.Progression.CharacterLevel + CharacterScaling);

            if (monkmax.MaxValue(0) > unarmedmax.MaxValue(0))
                unarmedmax = monkmax;

            if (unarmedmax.MaxValue(0) > weaponmax.MaxValue(0))
            {
                evt.WeaponDamageDiceOverride = unarmedmax;
                Main.DebugLog($"WeaponEmptyHandOverride scaled to {evt.WeaponDamageDiceOverride.Value.Rolls}{evt.WeaponDamageDiceOverride.Value.Dice}");
            }
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt)
        {
        }

        public DiceFormula MonkStrikeLevel(int level)
        {
            level = (level - 1).MinMax(0, 19);
            return DiceList[level];
        }

        public DiceFormula ChangeBy(DiceFormula dice, int amount)
        {
            for (int i = 0; i < DiceList.Length; i++)
            {
                if (DiceList[i] == dice)
                    return DiceList[(i + amount).MinMax(0, 19)];
            }
            Main.DebugLog($"WeaponEmptyHandOverride: Couldn't find dice value {dice.Rolls}{dice.Dice}");
            return dice;
        }

        public bool WeaponMonk = true;

        public int CharacterScaling = -4;

        public static DiceFormula[] DiceList = new DiceFormula[]
        {
            new DiceFormula(1, DiceType.D6),
            new DiceFormula(1, DiceType.D6),
            new DiceFormula(1, DiceType.D6),
            new DiceFormula(1, DiceType.D8),
            new DiceFormula(1, DiceType.D8),
            new DiceFormula(1, DiceType.D8),
            new DiceFormula(1, DiceType.D8),
            new DiceFormula(1, DiceType.D10),
            new DiceFormula(1, DiceType.D10),
            new DiceFormula(1, DiceType.D10),
            new DiceFormula(1, DiceType.D10),
            new DiceFormula(2, DiceType.D6),
            new DiceFormula(2, DiceType.D6),
            new DiceFormula(2, DiceType.D6),
            new DiceFormula(2, DiceType.D6),
            new DiceFormula(2, DiceType.D8),
            new DiceFormula(2, DiceType.D8),
            new DiceFormula(2, DiceType.D8),
            new DiceFormula(2, DiceType.D8),
            new DiceFormula(2, DiceType.D10),
        };
    }
}