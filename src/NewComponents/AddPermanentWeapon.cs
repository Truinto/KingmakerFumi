using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Validation;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Newtonsoft.Json;

namespace FumisCodex.NewComponents
{
    [AllowedOn(typeof(BlueprintBuff))]
    public class AddPermanentWeapon : BuffLogic, IAreaActivationHandler, IGlobalSubscriber
    {
        public override void OnFactActivate()
        {
            base.OnFactActivate();
            this.m_Applied = this.Blade.CreateEntity<ItemEntityWeapon>();
            this.m_Applied.MakeNotLootable();
            if (base.Owner.Body.PrimaryHand.HasItem)
            {
                base.Owner.Body.PrimaryHand.RemoveItem(true);
            }
            ItemsCollection.DoWithoutEvents(delegate
            {
                base.Owner.Body.PrimaryHand.InsertItem(this.m_Applied);
            });
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

        public BlueprintItemWeapon Blade;

        [JsonProperty]
        private ItemEntityWeapon m_Applied;
    }
}
