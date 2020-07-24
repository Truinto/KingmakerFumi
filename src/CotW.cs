using CallOfTheWild;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.ResourceLinks;
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

        public static string[] guids = new string[] { "31f0fa4235ad435e95ebc89d8549c2ce", "b03f4347c1974e38acff99a2af092461", "15d3a2eef8ac43dd886d2bae83be35eb",
            "c04cde18e91e4f84898de92a372bc1e0", "6535cf6ab2c143079468edb7e1cd2b86", "e845d92965544e2ba9ca7ab5b1b246ca", "656b4f5990f14f29b0e2c262a39d274f" };

        public static void modSlumber(bool enabled = true)
        {
            if (!enabled) return;

            try {
                foreach (string guid in guids)
                {
                    library.Get<BlueprintAbility>(guid)?.RemoveComponents<CallOfTheWild.NewMechanics.AbilityTargetCasterHDDifference>();
                }
            } catch (System.Exception) {
                Main.DebugLogAlways("Error: guids for slumber wrong.");
                return;
            }

            Main.DebugLogAlways("Removed level cap of slumber.");
        }

        public static void modAuraOfDoomToogle(bool enable = true)
        {
            var area = library.Get<BlueprintAbilityAreaEffect>("711e28b2b57c4318805b723f0f441701");//AuraOfDoomArea
            if (enable)
                area.Fx = Common.createPrefabLink("bbd6decdae32bce41ae8f06c6c5eb893");//Holy00_Alignment_Aoe_20Feet
            else
                area.Fx = new PrefabLink();
        }

        public static void modDazeToogle(bool enable = true)
        {
            var daze_buff = library.Get<BlueprintBuff>("9934fedff1b14994ea90205d189c8759");

            if (enable)
                daze_buff.ReplaceComponent<SpellDescriptorComponent>(Helpers.CreateSpellDescriptor(SpellDescriptor.Daze));
            else
                daze_buff.ReplaceComponent<SpellDescriptorComponent>(Helpers.CreateSpellDescriptor(SpellDescriptor.MindAffecting | SpellDescriptor.Compulsion | SpellDescriptor.Daze | SpellDescriptor.Stun));
        }

        public static void modFrostbite()
        {
            //CallOfTheWild.NewSpells.frost_bite
            // remove level cap of frostbite
        }


    }



}
