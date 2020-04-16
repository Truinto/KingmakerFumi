
//------------------ earth - as new
//// unfortunately this does not work. at least I got more familiar with the code. I keep this here, maybe I can recycle something later on

//var earth_impale_ability = Helpers.CreateAbility(
//        "EarthImpaleAbility",
//        infusion_impale_feature.Name,
//        infusion_impale_feature.Description,
//        "adcf52d3bc874d9a94250053b7ebf6e4",
//        icon,
//        AbilityType.SpellLike,
//        CommandType.Standard,
//        AbilityRange.Close,
//        "",
//        Helpers.savingThrowNone
//    );
//earth_impale_ability.CanTargetEnemies = true;
//earth_impale_ability.CanTargetPoint = true;
//earth_impale_ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
//earth_impale_ability.Parent = earth_base;
//earth_impale_ability.Animation = Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell.CastAnimationStyle.Kineticist;
////earth_impale_ability.AnimationStyle = Kingmaker.View.Animation.CastAnimationStyle.CastActionOmni;
//earth_impale_ability.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten | Metamagic.Reach;
////earth_impale_ability.MaterialComponent = new BlueprintAbility.MaterialComponentData() { Count = 1 };
//earth_impale_ability.ResourceAssetIds = new string[] {"69a83b56c1265464f8626a2ab414364a", "852b687aad7863e438c61339dd35d85d", "7bc4c143b08b5f9458bfc9745199c722", "1472ed032d040af48a3d8a710cd19f1c", "87d38ebf311f6da4a85414572dba3e45"};

//var abilityActions = ScriptableObject.CreateInstance<AbilityEffectRunAction>();
//abilityActions.Actions = new Kingmaker.ElementsSystem.ActionList();
//abilityActions.SavingThrowType = SavingThrowType.Unknown;
//var damage_dice = Helper.CreateContextDiceValue(
//        Dx: DiceType.D6,
//        diceType: ContextValueType.Rank,
//        diceRank: AbilityRankType.DamageDice,
//        bonusType: ContextValueType.Shared
//    );
//var damage_roll = Helpers.CreateActionDealDamage(PhysicalDamageForm.Piercing, damage_dice, isAoE: true, halfIfSaved: false, IgnoreCritical: false);
//abilityActions.Actions.Actions = new Kingmaker.ElementsSystem.GameAction[] { damage_roll };

//var rank = Helpers.CreateContextRankConfig(
//        baseValueType: ContextRankBaseValueType.FeatureRank, 
//        type: AbilityRankType.DamageDice,
//        feature: kinetic_blast_feature
//    );

//var projectile = ScriptableObject.CreateInstance<AbilityDeliverProjectile>();
//projectile.Projectiles = library.Get<BlueprintProjectile>("5d66a6c3cac5124469b2d0474e53ecab").ToArray();  //Kinetic_EarthBlastLine00 --Kinetic_EarthBlast00_Projectile:c28e153e8c212c1458ec2ee4092a794f
//projectile.LineWidth = new Feet(5f);
//projectile.Length = new Feet(30f);
//projectile.NeedAttackRoll = true;
//projectile.Weapon = weapon_blast_physical;

//var damage_bonus = ScriptableObject.CreateInstance<ContextCalculateSharedValue>();
//damage_bonus.Modifier = 1f;
//damage_bonus.Value = Helper.CreateContextDiceValue(
//        Dx: DiceType.One,
//        diceType: ContextValueType.Rank,
//        diceRank: AbilityRankType.DamageDice,
//        bonusType: ContextValueType.Rank,
//        bonusRank: AbilityRankType.DamageBonus
//    );

//var context_damage_score = Helpers.CreateContextRankConfig(
//        type: AbilityRankType.DamageBonus,
//        baseValueType: ContextRankBaseValueType.CustomProperty,
//        stat: StatType.Constitution,
//        customProperty: kineticist_primary_score
//    );

//var spell_descriptor = Helpers.CreateSpellDescriptor((SpellDescriptor)1125899906842624);

//var kineticist_comp = ScriptableObject.CreateInstance<AbilityKineticist>();
//kineticist_comp.Amount = 1;

//var preFx = Helper.CreateAbilitySpawnFx("69a83b56c1265464f8626a2ab414364a", AbilitySpawnFxTime.OnPrecastStart, AbilitySpawnFxAnchor.None, AbilitySpawnFxAnchor.None);
//var startFx = Helper.CreateAbilitySpawnFx("852b687aad7863e438c61339dd35d85d", AbilitySpawnFxTime.OnStart, AbilitySpawnFxAnchor.None, AbilitySpawnFxAnchor.None);

//earth_impale_ability.AddComponents(abilityActions, rank, projectile, damage_bonus, context_damage_score, spell_descriptor, kineticist_comp, preFx, startFx);