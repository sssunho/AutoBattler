using System;
using System.Reflection;
using RaggedBanner.MercenaryData;
using RaggedBanner.Mercenaries;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    static class BattleSkillTestAssets
    {
        public static SkillDefinition CreateBasicAttack(Action<ScriptableObject> track,
            int damage = 3, int range = 1, AttackAttribute attribute = AttackAttribute.Physical)
        {
            SkillDefinition definition = Create(track, SkillType.BasicAttack, SkillTargetType.Unit,
                SkillRangeSource.Own, 1, range, 2);
            SetField(definition, "_damage", damage);
            SetField(definition, "_attackAttribute", attribute);
            return definition;
        }

        public static void SetCatalog(MercenaryDataList data, Action<ScriptableObject> track)
        {
            SkillDefinition[] definitions =
            {
                Create(track, SkillType.Stealth, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 2, 2),
                Create(track, SkillType.Ambush, SkillTargetType.Unit, SkillRangeSource.BasicAttack, 0, 0, 2, bonus: 2),
                Create(track, SkillType.Overwatch, SkillTargetType.Direction, SkillRangeSource.BasicAttack, 0, 0, 2),
                Create(track, SkillType.PullSelf, SkillTargetType.Unit, SkillRangeSource.Own, 2, 6, 2),
                Create(track, SkillType.PullTarget, SkillTargetType.Unit, SkillRangeSource.Own, 2, 6, 2),
                Create(track, SkillType.Invisibility, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 2),
                Create(track, SkillType.AssassinLeap, SkillTargetType.Unit, SkillRangeSource.Own, 1, 4, 3)
            };
            SetField(data, "_commonSkills", new[] { Grant(definitions[0], 2) });
            SerializedObject serialized = new SerializedObject(data);
            SerializedProperty classes = serialized.FindProperty("_classes").FindPropertyRelative("_entries");

            for (int i = 0; i < classes.arraySize; i++)
            {
                SerializedProperty entry = classes.GetArrayElementAtIndex(i);
                ClassDefinition definition = (ClassDefinition)entry.FindPropertyRelative("_value").objectReferenceValue;
                MercenaryClass unitClass = (MercenaryClass)entry.FindPropertyRelative("_key").intValue;
                SkillGrant[] grants = unitClass == MercenaryClass.Rogue ? new[] { Grant(definitions[1], 1) }
                    : unitClass == MercenaryClass.Archer ? new[] { Grant(definitions[2], 1) }
                    : Array.Empty<SkillGrant>();
                SetField(definition, "_grantedSkills", grants);
            }

            SerializedProperty specializations = serialized.FindProperty("_specializations").FindPropertyRelative("_entries");
            specializations.arraySize = 3;

            for (int i = 0; i < 3; i++)
            {
                SpecializationDefinition definition = ScriptableObject.CreateInstance<SpecializationDefinition>();
                track(definition);
                SkillGrant[] grants = i == 0 ? new[] { Grant(definitions[3], 1), Grant(definitions[4], 3) }
                    : i == 1 ? new[] { Grant(definitions[5], 4), Grant(definitions[6], 3) }
                    : Array.Empty<SkillGrant>();
                SetField(definition, "_grantedSkills", grants);
                TraitType trait = i == 0 ? TraitType.SkillMovementRecovery
                    : i == 1 ? TraitType.BattleStartInvisibility : TraitType.FourElementBasicAttack;
                SetField(definition, "_traits", new[] { trait });
                SerializedProperty entry = specializations.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_key").intValue = i + 1;
                entry.FindPropertyRelative("_value").objectReferenceValue = definition;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static MercenaryDataList CreateCatalog(Action<ScriptableObject> track)
        {
            MercenaryDataList data = ScriptableObject.CreateInstance<MercenaryDataList>();
            track(data);
            SerializedObject serialized = new SerializedObject(data);
            SerializedProperty entries = serialized.FindProperty("_classes").FindPropertyRelative("_entries");
            entries.arraySize = 3;

            for (int i = 0; i < 3; i++)
            {
                MercenaryClass unitClass = (MercenaryClass)i;
                BattleUnitStats stats = BattleUnitStats.ForClass(unitClass);
                ClassDefinition definition = ScriptableObject.CreateInstance<ClassDefinition>();
                track(definition);
                SetField(definition, "_displayName", unitClass.ToString());
                SetField(definition, "_basicAttack", CreateBasicAttack(track, range: unitClass == MercenaryClass.Archer ? 3 : 1));
                SerializedObject classData = new SerializedObject(definition);
                SerializedProperty baseStats = classData.FindProperty("_baseStats");
                baseStats.FindPropertyRelative("_maxHP").intValue = stats.MaxHealth;
                baseStats.FindPropertyRelative("_movement").intValue = stats.MovementPerTurn;
                baseStats.FindPropertyRelative("_physicalDefense").intValue = stats.PhysicalDefense;
                baseStats.FindPropertyRelative("_criticalChance").floatValue = stats.CriticalChance / 100f;
                classData.ApplyModifiedPropertiesWithoutUndo();
                SetField(definition, "_traits", unitClass == MercenaryClass.Warrior ? new[] { TraitType.OpportunityAttack }
                    : unitClass == MercenaryClass.Rogue ? new[] { TraitType.RearGuaranteedCritical } : Array.Empty<TraitType>());
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_key").intValue = i;
                entry.FindPropertyRelative("_value").objectReferenceValue = definition;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetCatalog(data, track);
            return data;
        }

        static SkillGrant Grant(SkillDefinition skill, int slot)
        {
            object grant = default(SkillGrant);
            SetField(grant, "_skill", skill);
            SetField(grant, "_slot", slot);
            return (SkillGrant)grant;
        }

        static SkillDefinition Create(Action<ScriptableObject> track, SkillType type, SkillTargetType target,
            SkillRangeSource source, int min, int max, int cost, int movement = 0, int bonus = 0)
        {
            SkillDefinition definition = ScriptableObject.CreateInstance<SkillDefinition>();
            track(definition);
            SetField(definition, "_type", type);
            SetField(definition, "_targetType", target);
            SetField(definition, "_inputMode", type == SkillType.SpiritImbuement ? SkillInputMode.Choice
                : target == SkillTargetType.Self ? SkillInputMode.Self
                : target == SkillTargetType.Direction ? SkillInputMode.Direction : SkillInputMode.Unit);
            SetField(definition, "_rangeSource", source);
            SetField(definition, "_minRange", min);
            SetField(definition, "_maxRange", max);
            SetField(definition, "_actionPointCost", cost);
            SetField(definition, "_movementCost", movement);
            SetField(definition, "_damage", 0);
            SetField(definition, "_damageBonus", bonus);
            return definition;
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
