using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Validation;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Newtonsoft.Json;

namespace FumisCodex.NewComponents
{
    [AllowedOn(typeof(BlueprintUnitFact))]
    public class AddPermanentWeaponFact : OwnedGameLogicComponent<UnitDescriptor>
    {
        public override void OnFactActivate()
        {
            base.OnFactActivate();
            this.m_Applied = this.Weapon.CreateEntity<ItemEntityWeapon>();
            this.m_Applied.MakeNotLootable();
            bool flag = true;
            if (base.Owner.Body.HandsEquipmentSets[0].PrimaryHand.HasItem)
            {
                flag = base.Owner.Body.HandsEquipmentSets[0].PrimaryHand.RemoveItem(true);
            }
            if (flag)
                ItemsCollection.DoWithoutEvents(delegate
                {
                    base.Owner.Body.HandsEquipmentSets[0].PrimaryHand.InsertItem(this.m_Applied);
                });
            else
                Main.DebugLog($"AddPermanentWeaponFact cannot remove item: {base.Owner.Body.HandsEquipmentSets[0].PrimaryHand.Item?.Name}");
        }

        public override void OnFactDeactivate()
        {
            base.OnFactDeactivate();
            if (this.m_Applied != null)
            {
                ItemSlot holdingSlot = this.m_Applied.HoldingSlot;
                if (holdingSlot != null)
                {
                    holdingSlot.RemoveItem(true);
                }
                ItemsCollection.DoWithoutEvents(delegate
                {
                    ItemsCollection collection = this.m_Applied.Collection;
                    if (collection == null)
                    {
                        return;
                    }
                    collection.Remove(this.m_Applied);
                });
                this.m_Applied = null;
            }
        }

        public override void OnTurnOn()
        {
            this.m_Applied?.HoldingSlot.Lock.Retain();
        }

        public override void OnTurnOff()
        {
            this.m_Applied?.HoldingSlot.Lock.Release();
        }

        public void OnAreaActivated()
        {
            if (this.m_Applied == null)
            {
                this.OnFactActivate();
                this.OnTurnOn();
            }
        }

        public BlueprintItemWeapon Weapon;

        [JsonProperty]
        private ItemEntityWeapon m_Applied;
    }
}
