using CallOfTheWild;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.Utility;
using Newtonsoft.Json;

namespace FumisCodex
{
    public class CotW
    {
        static LibraryScriptableObject library => Main.library;

        public static void modSlumber()
        {
            var slumber = library.Get<BlueprintAbility>("31f0fa4235ad435e95ebc89d8549c2ce");
            slumber?.RemoveComponents<CallOfTheWild.NewMechanics.AbilityTargetCasterHDDifference>();
            var restless_slumber = library.Get<BlueprintAbility>("e845d92965544e2ba9ca7ab5b1b246ca");
            restless_slumber?.RemoveComponents<CallOfTheWild.NewMechanics.AbilityTargetCasterHDDifference>();

            Main.DebugLog("Removed level cap of slumber.");
        }
        
        //[Harmony12.HarmonyPatch(typeof(CallOfTheWild.Rebalance), "fixMagicVestmentArmor")]
        class fixMagicVestmentArmorPatch
        {
            static bool Prefix()
            {
                Main.DebugLog("CotW skipped magic vestment armor patch.");
                return false;
            }
        }

        // ---------- Experiments ----------
        // playing around with armor values, does not work
        public static void modMagicVestment()
        {
            var magic_vestment_armor_spell = library.Get<BlueprintAbility>("956309af83352714aa7ee89fb4ecf201");
            magic_vestment_armor_spell.RemoveComponents<CallOfTheWild.NewMechanics.AbilitTargetHasArmor>();

            var magic_vestement_armor_buff = Main.library.Get<BlueprintBuff>("9e265139cf6c07c4fb8298cb8b646de9");
            var armor_enchant = Helpers.Create<BuffContextEnchantArmor>();
            armor_enchant.value = Helpers.CreateContextValue(AbilityRankType.StatBonus);
            armor_enchant.enchantments = ArmorEnchantments.temporary_armor_enchantments;

            magic_vestement_armor_buff.Stacking = StackingType.Replace;
            magic_vestement_armor_buff.ComponentsArray = new BlueprintComponent[] {
                armor_enchant,
                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.CasterLevel,
                    progression: ContextRankProgression.DivStep, startLevel: 4, min:1, stepLevel: 4, max: 5,
                    type: AbilityRankType.StatBonus)
            };

            // var magic_vestment_shield_spell = library.Get<BlueprintAbility>("adcda176d1756eb45bd5ec9592073b09"); magic_vestment_shield_spell.RemoveComponents<NewMechanics.AbilitTargetHasShield>();

            Main.DebugLog("Magic Vestment works on either armor or wrist items.");
        }
    }

    // playing around with armor values, does not work
    public class BuffContextEnchantArmor : BuffLogic
    {
        public BlueprintArmorEnchantment[] enchantments;
        public ContextValue value;
        public bool lock_slot = false;
        public bool only_non_magical = false;
        [JsonProperty]
        private bool m_unlock;
        [JsonProperty]
        private ItemEnchantment m_Enchantment;
        [JsonProperty]
        private ItemEntity m_Armor;

        public override void OnFactActivate()
        {
            m_unlock = false;
            var unit = this.Owner;
            if (unit == null) return;

            Main.DebugLog(unit.Body.Wrist.MaybeItem?.ToString() ?? "wrist is null");

            ItemEntity armor = unit.Body.Armor.MaybeArmor;
            Main.DebugLog("1: " + armor?.ToString() ?? "armor is null");
            if (armor == null) armor = unit.Body.Wrist.MaybeItem;
            Main.DebugLog("2: " + armor?.ToString() ?? "wrist-armor is null");
            if (armor == null) return;

            int bonus = value.Calculate(Context) - 1;
            Main.DebugLog("3: " + bonus);
            if (bonus < 0)
            {
                bonus = 0;
            }
            if (bonus >= enchantments.Length)
            {
                bonus = enchantments.Length - 1;
            }
            Main.DebugLog("4: " + bonus);
            Main.DebugLog("5: " + enchantments.Length);

            if (armor.Enchantments.HasFact(enchantments[bonus]))
            {
                Main.DebugLog("6: Enchantments.HasFact");
                return;
            }

            m_Enchantment = armor.AddEnchantment(enchantments[bonus], Context, new Rounds?());

            //armor.RecalculateStats();
            m_Armor = armor;
            if (lock_slot && !armor.IsNonRemovable)
            {
                armor.IsNonRemovable = true;
                m_unlock = true;
            }

            Main.DebugLog("x: ");
        }

        public override void OnFactDeactivate()
        {
            if (this.m_Enchantment == null)
                return;
            this.m_Enchantment.Owner?.RemoveEnchantment(this.m_Enchantment);
            if (m_Armor != null)
            {
                //m_Armor.RecalculateStats();
            }
            else
            {
                return;
            }
            if (m_unlock)
            {
                m_Armor.IsNonRemovable = false;
            }
        }
    }

}
