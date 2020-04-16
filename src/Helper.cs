using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Mechanics;
using System;
using System.IO;
using UnityEngine;

namespace FumisCodex
{
    public static class Extensions
    {
        public static PrerequisiteArchetypeLevel CreatePrerequisite(this BlueprintArchetype @class, int level, bool any = true)
        {
            var result = CallOfTheWild.Helpers.Create<PrerequisiteArchetypeLevel>();
            result.CharacterClass = @class.GetParentClass();
            result.Archetype = @class;
            result.Level = level;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }
        
        /// <returns>Action on index 0</returns>
        public static GameAction getAction(this BlueprintAbility @ability)
        {
            return ability.GetComponent<AbilityEffectRunAction>().Actions.Actions[0];
        }
        //public static GameAction getAction(this AbilityEffectRunAction effectAndRun) { return effectAndRun.Actions.Actions[0]; }

        /// <param name="savingThrow">SavingThrowType.Unknown means "None"</param>
        public static void setSavingThrow(this BlueprintAbility ability, SavingThrowType savingThrow)
        {
            ability.GetComponent<AbilityEffectRunAction>().SavingThrowType = savingThrow;
        }
        public static void setSavingThrow(this AbilityEffectRunAction effectAndRun, SavingThrowType savingThrow)
        {
            effectAndRun.SavingThrowType = savingThrow;
        }

        /// <summary>Clones ComponentsArray and overrides reference.
        /// This makes it so replacing components on a copy does not mutate the original.</summary>
        /// <returns>Reference to new array.</returns>
        public static BlueprintComponent[] DetachComponents(this BlueprintScriptableObject obj)
        {
            var result = new BlueprintComponent[obj.ComponentsArray.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = obj.ComponentsArray[i];
            obj.ComponentsArray = result;
            return result;
        }

        /// <summary>Looks for the first component of the replacement type and overrides the reference.
        /// Note that you need to detach the component first, otherwise the original gets mutated.</summary>
        /// <param name="replacement">The new component that should take it's place.</param>
        /// <returns>Itself for chaining.</returns>
        public static BlueprintScriptableObject ReplaceDirty(this BlueprintScriptableObject obj, BlueprintComponent replacement)
        {
            Type replacementType = replacement.GetType();
            for (int i = 0; i < obj.ComponentsArray.Length; i++)
            {
                if (obj.ComponentsArray[i].GetType() == replacementType)
                {
                    obj.ComponentsArray[i] = replacement;
                    return obj;
                }
            }
            return null;
        }

        /// <summary>Makes an copy of an already existing AbilityEffectRunAction component.</summary>
        /// <param name="detach">You only need to detach once per copy, this this might be unnecessary.</param>
        /// <returns></returns>
        public static AbilityEffectRunAction CloneAbilityActionList(this BlueprintAbility ability, bool detach = true)
        {
            //this might do something similar, I couldn't figure out what it does: ability.ReplaceComponent<AbilityEffectRunAction>( a => { } );
            var orig = ability.GetComponent<AbilityEffectRunAction>();

            var result = new AbilityEffectRunAction();
            result.SavingThrowType = orig.SavingThrowType;
            result.Actions = new ActionList();
            result.Actions.Actions = new GameAction[orig.Actions.Actions.Length];
            
            for (int i = 0; i < orig.Actions.Actions.Length; i++)
                result.Actions.Actions[i] = UnityEngine.Object.Instantiate(orig.Actions.Actions[i]);

            if (detach) ability.DetachComponents();
            ability.ReplaceDirty(result);
            return result;
        }

        public static T[] ToArray<T>(this T obj)
        {
            return new T[] { obj };
        }

        /// <summary>Appends objects on array. Does not change the original.</summary>
        public static T[] AppendRange<T>(this T[] orig, params T[] objs)
        {
            int i, j;
            T[] result = new T[orig.Length + objs.Length];
            for (i = 0; i < orig.Length; i++)
                result[i] = orig[i];
            for (j = 0; i < result.Length; i++)
                result[i] = objs[j++];
            return result;
        }

        //public static T CloneComponent<T>(this T orig) where T : class, new()
        //{
        //    Type type = orig.GetType();
        //    T result = new T();
        //    foreach (var fi in type.GetFields().Where(f => f.IsPublic || f.IsPrivate))
        //        fi.SetValue(result, fi.GetValue(orig));
        //    return result;
        //}
        //public static void CloneAndReplace<T> (this T orig, BlueprintScriptableObject parent) where T : BlueprintComponent
        //{
        //    parent.ReplaceComponent<T>(orig.CloneComponent());
        //}
    }

    public class Helper
    {
        /// <summary>Appends objects on array and overwrites the original.</summary>
        public static T[] AppendAndReplace<T>(ref T[] orig, params T[] objs)
        {
            int i, j;
            T[] result = new T[orig.Length + objs.Length];
            for (i = 0; i < orig.Length; i++)
                result[i] = orig[i];
            for (j = 0; i < result.Length; i++)
                result[i] = objs[j++];
            orig = result;
            return result;
        }

        /// <summary>AbilityRankType is only relevant for ContextValueType.Rank</summary> 
        public static ContextDiceValue CreateContextDiceValue(DiceType Dx, ContextValueType diceType = ContextValueType.Simple, AbilityRankType diceRank = AbilityRankType.Default, int diceValue = 0, ContextValueType bonusType = ContextValueType.Simple, AbilityRankType bonusRank = AbilityRankType.Default, int bonusValue = 0)
        {
            var result = new ContextDiceValue();
            result.DiceType = Dx;

            result.DiceCountValue = new ContextValue();
            result.DiceCountValue.ValueType = diceType;
            result.DiceCountValue.ValueRank = diceRank;
            result.DiceCountValue.Value = diceValue;

            result.BonusValue = new ContextValue();
            result.BonusValue.ValueType = bonusType;
            result.BonusValue.ValueRank = bonusRank;
            result.BonusValue.Value = bonusValue;

            return result;
        }

        public static AbilitySpawnFx CreateAbilitySpawnFx(string AssetId, AbilitySpawnFxTime spawnTime, AbilitySpawnFxAnchor position, AbilitySpawnFxAnchor orientation)
        {
            var spawnFx = ScriptableObject.CreateInstance<AbilitySpawnFx>();
            spawnFx.PrefabLink = new Kingmaker.ResourceLinks.PrefabLink();
            spawnFx.PrefabLink.AssetId = AssetId;
            spawnFx.Time = AbilitySpawnFxTime.OnPrecastStart;
            spawnFx.PositionAnchor = position;
            spawnFx.OrientationAnchor = orientation;
            return spawnFx;
        }

        public class Image2Sprite
        {
            public static Sprite Create(string filename)
            {
                var bytes = File.ReadAllBytes(Path.Combine(Main.ModPath, "Icons", filename));
                var texture = new Texture2D(64, 64);
                texture.LoadImage(bytes);
                return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0, 0));
            }
        }
    }
}
