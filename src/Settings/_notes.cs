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

// playing around with armor values, does not work

//public class BuffContextEnchantArmor : BuffLogic
//{
//    public BlueprintArmorEnchantment[] enchantments;
//    public ContextValue value;
//    public bool lock_slot = false;
//    public bool only_non_magical = false;
//    [JsonProperty]
//    private bool m_unlock;
//    [JsonProperty]
//    private ItemEnchantment m_Enchantment;
//    [JsonProperty]
//    private ItemEntity m_Armor;

//    public override void OnFactActivate()
//    {
//        m_unlock = false;
//        var unit = this.Owner;
//        if (unit == null) return;

//        Main.DebugLog(unit.Body.Wrist.MaybeItem?.ToString() ?? "wrist is null");

//        ItemEntity armor = unit.Body.Armor.MaybeArmor;
//        Main.DebugLog("1: " + armor?.ToString() ?? "armor is null");
//        if (armor == null) armor = unit.Body.Wrist.MaybeItem;
//        Main.DebugLog("2: " + armor?.ToString() ?? "wrist-armor is null");
//        if (armor == null) return;

//        int bonus = value.Calculate(Context) - 1;
//        Main.DebugLog("3: " + bonus);
//        if (bonus < 0)
//        {
//            bonus = 0;
//        }
//        if (bonus >= enchantments.Length)
//        {
//            bonus = enchantments.Length - 1;
//        }
//        Main.DebugLog("4: " + bonus);
//        Main.DebugLog("5: " + enchantments.Length);

//        if (armor.Enchantments.HasFact(enchantments[bonus]))
//        {
//            Main.DebugLog("6: Enchantments.HasFact");
//            return;
//        }

//        m_Enchantment = armor.AddEnchantment(enchantments[bonus], Context, new Rounds?());

//        //armor.RecalculateStats();
//        m_Armor = armor;
//        if (lock_slot && !armor.IsNonRemovable)
//        {
//            armor.IsNonRemovable = true;
//            m_unlock = true;
//        }

//        Main.DebugLog("x: ");
//    }

//    public override void OnFactDeactivate()
//    {
//        if (this.m_Enchantment == null)
//            return;
//        this.m_Enchantment.Owner?.RemoveEnchantment(this.m_Enchantment);
//        if (m_Armor != null)
//        {
//            //m_Armor.RecalculateStats();
//        }
//        else
//        {
//            return;
//        }
//        if (m_unlock)
//        {
//            m_Armor.IsNonRemovable = false;
//        }
//    }
//}

////[Harmony12.HarmonyPatch(typeof(CallOfTheWild.Rebalance), "fixMagicVestmentArmor")]
//class fixMagicVestmentArmorPatch
//{
//    static bool Prefix()
//    {
//        Main.DebugLog("CotW skipped magic vestment armor patch.");
//        return false;
//    }
//}

//// ---------- Experiments ----------
//// playing around with armor values, does not work
//public static void modMagicVestment()
//{
//    var magic_vestment_armor_spell = library.Get<BlueprintAbility>("956309af83352714aa7ee89fb4ecf201");
//    magic_vestment_armor_spell.RemoveComponents<CallOfTheWild.NewMechanics.AbilitTargetHasArmor>();
//    var magic_vestement_armor_buff = Main.library.Get<BlueprintBuff>("9e265139cf6c07c4fb8298cb8b646de9");
//    var armor_enchant = Helpers.Create<BuffContextEnchantArmor>();
//    armor_enchant.value = Helpers.CreateContextValue(AbilityRankType.StatBonus);
//    armor_enchant.enchantments = ArmorEnchantments.temporary_armor_enchantments;
//    magic_vestement_armor_buff.Stacking = StackingType.Replace;
//    magic_vestement_armor_buff.ComponentsArray = new BlueprintComponent[] {
//                armor_enchant,
//                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.CasterLevel,
//                    progression: ContextRankProgression.DivStep, startLevel: 4, min:1, stepLevel: 4, max: 5,
//                    type: AbilityRankType.StatBonus)
//            };
//    // var magic_vestment_shield_spell = library.Get<BlueprintAbility>("adcda176d1756eb45bd5ec9592073b09"); magic_vestment_shield_spell.RemoveComponents<NewMechanics.AbilitTargetHasShield>();
//    Main.DebugLog("Magic Vestment works on either armor or wrist items.");
//}