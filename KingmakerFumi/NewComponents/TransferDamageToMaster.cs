using System;
using Kingmaker.Blueprints;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;

namespace FumisCodex.NewComponents
{
    public class TransferDamageToMaster : RuleTargetLogicComponent<RuleDealDamage>

    {
        public override void OnEventAboutToTrigger(RuleDealDamage evt)
        {
        }

        public override void OnEventDidTrigger(RuleDealDamage evt)
        {
            var master = evt?.Target?.Descriptor?.Master.Value;
            if (master == null || master.Descriptor.State.IsDead)
                return;

            Main.DebugLog($"Damage dealt {evt.Damage}, Eidolon total damage {evt.Target.Damage}, Eidolon HP left {evt.Target.HPLeft}, Master HP {master.HPLeft}");

            int need_HP = 1 - evt.Target.HPLeft;
            int can_transfer = master.HPLeft - 1;
            int transfer = Math.Min(need_HP, can_transfer);

            if (transfer > 0 && evt.Target.HPLeft + transfer > 0)
            {
                master.Damage += transfer;
                evt.Target.Damage -= transfer;

                Main.DebugLog("Transfered damage: " + transfer);
            }

            // evt.Target.Damage -= transfer_damage;
            // var damage_bundle = new DamageBundle(new DirectDamage(new DiceFormula(transfer_damage, DiceType.One), 0));
            // var rule = this.Fact.MaybeContext.TriggerRule(new RuleDealDamage(evt.Target, master, damage_bundle));
        }
    }
}
