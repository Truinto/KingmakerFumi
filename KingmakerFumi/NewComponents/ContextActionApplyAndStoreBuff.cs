using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Blueprints;
using Kingmaker.Controllers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;

namespace FumisCodex.NewComponents
{
    public class ContextActionApplyAndStoreBuff : ContextAction
    {
        public override string GetCaption()
        {
            return "ContextActionApplyAndStoreBuff";
        }

		public override void RunAction()
		{
			MechanicsContext.Data data = ElementsContext.GetData<MechanicsContext.Data>();
			MechanicsContext context = (data != null) ? data.Context : null;
			if (context == null)
				return;

			TimeSpan? duration = this.Permanent ? null : new TimeSpan?(this.UseDurationSeconds ? this.DurationSeconds.Seconds() : this.DurationValue.Calculate(context).Seconds);
			UnitEntityData unitEntityData = this.ToCaster ? context.MaybeCaster : base.Target.Unit;
			if (context.MaybeCaster == null || unitEntityData == null)
				return;

			foreach (var Buff in Buffs)
			{
				var storeBuffs = context.MaybeCaster.Ensure<UnitPartStoreBuffs>();

				if (Toggle && storeBuffs.Remove(unitEntityData.Buffs.GetBuff(Buff)))  // if buff was already applied, remove it and skip rest
                {
					Main.DebugLog("Removed buff " + Buff.name);
					continue;
                }

				Main.DebugLog("Appling buff " + Buff.name);
				Buff buff = unitEntityData.Descriptor.AddBuff(Buff, context, duration);
				if (buff == null)
					return;

				var source = Context.SourceAbility?.Parent ?? Context.SourceAbility;
				storeBuffs.Add(buff, source?.AssetGuid);

				AreaEffectContextData data2 = ElementsContext.GetData<AreaEffectContextData>();
				AreaEffectEntityData areaEffectEntityData = (data2 != null) ? data2.Entity : null;
				if (areaEffectEntityData != null)
				{
					buff.SourceAreaEffectId = areaEffectEntityData.UniqueId;
				}
				buff.IsFromSpell = this.IsFromSpell;
				buff.IsNotDispelable = this.IsNotDispelable;
			}

		}

		public BlueprintBuff[] Buffs;

		public bool Toggle = true;

		public bool Permanent = true;

		public bool UseDurationSeconds;

		[HideIf("UseDurationSeconds")]
		public ContextDurationValue DurationValue;

		[ShowIf("UseDurationSeconds")]
		public float DurationSeconds;

		public bool IsFromSpell;

		public bool IsNotDispelable;

		public bool ToCaster;
	}
}
