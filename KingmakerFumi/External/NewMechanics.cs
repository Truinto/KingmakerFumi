using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Parts;
using UnityEngine;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.Visual.HitSystem;
using System;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Newtonsoft.Json;
using Kingmaker.Utility;
using Kingmaker.UI.GenericSlot;
using Kingmaker.Items;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.EntitySystem.Entities;
using System.Collections.Generic;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.Blueprints.Validation;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.ElementsSystem;
using Kingmaker.Controllers;
using Kingmaker;
using static Kingmaker.UnitLogic.Abilities.Components.AbilityCustomMeleeAttack;
using Kingmaker.UnitLogic.Mechanics.ContextData;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.EntitySystem;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Mechanics.Properties;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.EntitySystem.Persistence.Versioning;
using JetBrains.Annotations;
using Kingmaker.Enums.Damage;
using Kingmaker.Inspect;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Designers.EventConditionActionSystem.ContextData;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.Items.Slots;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.AreaLogic.Cutscenes.Commands;
using Pathfinding;
using Kingmaker.Controllers.Combat;
using Kingmaker.UnitLogic.Abilities.Components.AreaEffects;
using System.Text;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Blueprints.Items.Armors;

namespace CopyOf.CallOfTheWild.NewMechanics
{
    [AllowedOn(typeof(BlueprintBuff))]
    [ComponentName("Buffs/Damage bonus for specific weapon types")]
    public class ContextWeaponTypeDamageBonus : RuleInitiatorLogicComponent<RuleCalculateWeaponStats>
    {
        public BlueprintWeaponType[] weapon_types;
        public ContextValue Value;

        public override void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            int num = Value.Calculate(this.Fact.MaybeContext);
            foreach (var w in weapon_types)
            {
                if (evt.Weapon.Blueprint.Type == w)
                {
                    evt.AddBonusDamage(num);
                    return;
                }
            }

        }

        public override void OnEventDidTrigger(RuleCalculateWeaponStats evt)
        {
        }
    }
}