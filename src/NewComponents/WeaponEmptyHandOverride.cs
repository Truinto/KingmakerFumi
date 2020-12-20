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

            int umax = base.Owner.Body.EmptyHandWeapon?.Damage.MaxValue(0) ?? 0;
            int wmax = evt.Weapon.Damage.MaxValue(0);

            if (umax > wmax)
            {
                evt.WeaponDamageDiceOverride = ChangeBy(base.Owner.Body.EmptyHandWeapon.Blueprint.BaseDamage, ScaleDice);
                Main.DebugLog($"WeaponEmptyHandOverride scaled to {evt.WeaponDamageDiceOverride.Value.Rolls}{evt.WeaponDamageDiceOverride.Value.Dice}");
            }
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt)
        {
        }

        public DiceFormula ChangeBy(DiceFormula dice, int amount)
        {
            for (int i = 0; i < DiceList.Length; i++)
            {
                if (DiceList[i] == dice)
                    return DiceList[(i + amount).MinMax(0, DiceList.Length-1)];
            }
            Main.DebugLog($"WeaponEmptyHandOverride: Couldn't find dice value {dice.Rolls}{dice.Dice}");
            return dice;
        }

        public bool WeaponMonk = true;

        public int ScaleDice = -1;

        public DiceFormula[] DiceList = new DiceFormula[]
        {
            new DiceFormula(1, DiceType.D6),
            new DiceFormula(1, DiceType.D8),
            new DiceFormula(1, DiceType.D10),
            new DiceFormula(2, DiceType.D6),
            new DiceFormula(2, DiceType.D8),
            new DiceFormula(2, DiceType.D10),
        };
    }
}