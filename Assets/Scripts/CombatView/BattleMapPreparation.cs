using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RaggedBanner.BattleMaps;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;
using RaggedBanner.MercenaryData;

[assembly: InternalsVisibleTo("RaggedBanner.Combat.Tests")]

namespace RaggedBanner.Combat.Unity
{
    internal static class BattleMapPreparation
    {
        internal static bool TryPrepare(BattleMapDefinition definition, IReadOnlyList<MercenaryClass> party,
            MercenaryDataList classData, IReadOnlyList<BattleClassViewPrefab> prefabs,
            int width, int height, int maxPlayerUnits, out BattleSpawnInput[] inputs,
            out int skippedPlayerCount, out string error, IReadOnlyList<BattlePartySpecialization> specializations = null,
            IReadOnlyList<BattleEquipmentLoadout> equipment = null)
        {
            inputs = Array.Empty<BattleSpawnInput>();
            skippedPlayerCount = 0;

            if (!TryValidateConfiguration(definition, party, classData, prefabs, maxPlayerUnits, out error, specializations)
                || !definition.Validate(width, height, out error)
                || !TryValidateEquipment(party, classData, equipment, out error))
            {
                return false;
            }

            BattleMapStartPoint[] starts = new BattleMapStartPoint[definition.StartPoints.Count];

            for (int i = 0; i < starts.Length; i++)
            {
                starts[i] = definition.StartPoints[i];
            }

            Array.Sort(starts, (left, right) => left.Order.CompareTo(right.Order));
            int playerCount = Math.Min(party.Count, starts.Length);

            if ((long)playerCount + definition.Units.Count > int.MaxValue)
            {
                error = "The battle contains too many participants.";
                return false;
            }

            BattleSpawnInput[] prepared = new BattleSpawnInput[playerCount + definition.Units.Count];

            for (int i = 0; i < playerCount; i++)
            {
                BattleMapStartPoint start = starts[i];

                if (!TryCreateInput(classData, prefabs, party[i], BattleTeam.Player,
                    start.Coordinate, start.Facing, out prepared[i], out error, GetSpecialization(specializations, i),
                    equipment == null ? BattleEquipmentLoadout.Empty : equipment[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < definition.Units.Count; i++)
            {
                BattleMapUnitPlacement unit = definition.Units[i];

                if (!TryCreateInput(classData, prefabs, unit.UnitClass, BattleTeam.Enemy,
                    unit.Coordinate, unit.Facing, out prepared[playerCount + i], out error))
                {
                    return false;
                }
            }

            inputs = prepared;
            skippedPlayerCount = party.Count - playerCount;
            error = null;
            return true;
        }

        internal static bool TryValidateConfiguration(BattleMapDefinition definition,
            IReadOnlyList<MercenaryClass> party, MercenaryDataList classData,
            IReadOnlyList<BattleClassViewPrefab> prefabs, int maxPlayerUnits, out string error,
            IReadOnlyList<BattlePartySpecialization> specializations = null)
        {
            if (definition == null)
            {
                error = "A battle map definition is required.";
                return false;
            }

            if (maxPlayerUnits <= 0)
            {
                error = "The player unit limit must be greater than zero.";
                return false;
            }

            if (party == null || party.Count == 0)
            {
                error = "The test party must contain at least one player unit.";
                return false;
            }

            if (party.Count > maxPlayerUnits)
            {
                error = $"The test party exceeds the player unit limit of {maxPlayerUnits}.";
                return false;
            }

            if (definition.Units == null || definition.Units.Count == 0)
            {
                error = "The battle map must contain at least one enemy unit.";
                return false;
            }

            if (definition.StartPoints == null || definition.StartPoints.Count == 0)
            {
                error = "The battle map must contain at least one player start point.";
                return false;
            }

            if (classData == null)
            {
                error = "Mercenary class data is required.";
                return false;
            }

            if (prefabs == null)
            {
                error = "Class view prefab mappings are required.";
                return false;
            }

            for (int i = 0; i < party.Count; i++)
            {
                if (!IsValidClass(party[i]))
                {
                    error = $"Player unit {i + 1} has an invalid class.";
                    return false;
                }
            }

            if (specializations != null)
            {
                HashSet<int> specializedIndices = new HashSet<int>();

                for (int i = 0; i < specializations.Count; i++)
                {
                    BattlePartySpecialization entry = specializations[i];

                    if ((uint)entry.PartyIndex >= (uint)party.Count || !specializedIndices.Add(entry.PartyIndex))
                    {
                        error = $"Specialization entry {i + 1} requires a unique party index within the test party.";
                        return false;
                    }

                    if (!MercenarySpecializationRules.IsCompatible(party[entry.PartyIndex], entry.Specialization))
                    {
                        error = $"Player unit {entry.PartyIndex + 1} has an invalid specialization for its class.";
                        return false;
                    }
                }
            }

            for (int i = 0; i < definition.Units.Count; i++)
            {
                BattleMapUnitPlacement unit = definition.Units[i];

                if (unit == null || !IsValidClass(unit.UnitClass) || unit.Team != BattleTeam.Enemy)
                {
                    error = $"Fixed unit {i + 1} must have a valid class and belong to the Enemy team.";
                    return false;
                }
            }

            for (int i = 0; i < definition.StartPoints.Count; i++)
            {
                if (definition.StartPoints[i] == null)
                {
                    error = $"Player start point {i + 1} is missing.";
                    return false;
                }
            }

            HashSet<MercenaryClass> mappedClasses = new HashSet<MercenaryClass>();

            for (int i = 0; i < prefabs.Count; i++)
            {
                BattleClassViewPrefab mapping = prefabs[i];

                if (mapping == null || !IsValidClass(mapping.UnitClass))
                {
                    error = $"Class view mapping {i + 1} has an invalid class.";
                    return false;
                }

                if (!mappedClasses.Add(mapping.UnitClass))
                {
                    error = $"Class {mapping.UnitClass} has more than one view prefab mapping.";
                    return false;
                }
            }

            if (!classData.Validate(out error))
            {
                return false;
            }

            int playerCount = Math.Min(party.Count, definition.StartPoints.Count);

            for (int i = 0; i < playerCount; i++)
            {
                if (!TryResolveClass(classData, prefabs, party[i], out _, out _, out error))
                {
                    return false;
                }
            }

            for (int i = 0; i < definition.Units.Count; i++)
            {
                if (!TryResolveClass(classData, prefabs, definition.Units[i].UnitClass,
                    out _, out _, out error))
                {
                    return false;
                }
            }

            error = null;
            return true;
        }

        internal static bool TryValidateEquipment(IReadOnlyList<MercenaryClass> party, MercenaryDataList classData,
            IReadOnlyList<BattleEquipmentLoadout> equipment, out string error)
        {
            if (equipment == null)
            {
                error = null;
                return true;
            }

            if (party == null || classData == null || equipment.Count != party.Count)
            {
                error = "Equipment snapshots must match the party.";
                return false;
            }

            for (int i = 0; i < equipment.Count; i++)
            {
                if (equipment[i] == null || !classData.TryGetDefinition(party[i], out ClassDefinition definition))
                {
                    error = "Each party member requires valid class and equipment data.";
                    return false;
                }

                if ((long)definition.BaseStats.MaxHP + equipment[i].MaxHealthBonus > int.MaxValue
                    || (long)definition.BaseStats.PhysicalDefense + equipment[i].PhysicalDefenseBonus > int.MaxValue)
                {
                    error = "Equipped stats exceed the supported range.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        static bool TryCreateInput(MercenaryDataList classData, IReadOnlyList<BattleClassViewPrefab> prefabs,
            MercenaryClass unitClass, BattleTeam team, HexCoordinate coordinate, HexDirection facing,
            out BattleSpawnInput input, out string error, MercenarySpecialization specialization = MercenarySpecialization.None,
            BattleEquipmentLoadout equipment = null)
        {
            input = null;

            if (!TryResolveClass(classData, prefabs, unitClass, out ClassDefinition definition,
                out HexCharacterView prefab, out error))
            {
                return false;
            }

            ClassBaseStats baseStats = definition.BaseStats;

            if (!classData.TryCreateSkillSet(unitClass, specialization, out SkillSet skills, out error))
            {
                return false;
            }

            int criticalChance = (int)Math.Floor((decimal)baseStats.CriticalChance * 100m + 0.5m);
            BattleUnitStats stats = new BattleUnitStats(unitClass, baseStats.MaxHP,
                baseStats.Movement, criticalChance,
                baseStats.PhysicalDefense, baseStats.WaterDefense, baseStats.FireDefense,
                baseStats.LightningDefense, baseStats.PoisonDefense, baseStats.Initiative);
            if (!classData.TryCreateTraits(unitClass, specialization, out TraitCollection traits, out error))
            {
                return false;
            }

            BattleTraitSettings traitSettings = BattleTraitSettings.None;

            if (traits.Has(TraitType.DirectionalPhysicalNegation))
            {
                DirectionalPhysicalNegationSettings negation = classData.TraitSettings.DirectionalPhysicalNegation;
                traitSettings = new BattleTraitSettings(negation.FrontChance, negation.SideChance, negation.RearChance);
            }

            equipment = equipment ?? BattleEquipmentLoadout.Empty;
            stats = equipment.ApplyTo(stats);
            input = new BattleSpawnInput(prefab, team, stats, coordinate, facing, traits, traitSettings, specialization, skills, equipment);
            return true;
        }

        static MercenarySpecialization GetSpecialization(IReadOnlyList<BattlePartySpecialization> specializations,
            int partyIndex)
        {
            if (specializations != null)
            {
                for (int i = 0; i < specializations.Count; i++)
                {
                    if (specializations[i].PartyIndex == partyIndex)
                    {
                        return specializations[i].Specialization;
                    }
                }
            }

            return MercenarySpecialization.None;
        }

        static bool TryResolveClass(MercenaryDataList classData, IReadOnlyList<BattleClassViewPrefab> prefabs,
            MercenaryClass unitClass, out ClassDefinition definition, out HexCharacterView prefab,
            out string error)
        {
            prefab = null;

            if (!classData.TryGetDefinition(unitClass, out definition))
            {
                error = $"No valid class definition exists for {unitClass}.";
                return false;
            }

            for (int i = 0; i < prefabs.Count; i++)
            {
                if (prefabs[i].UnitClass == unitClass)
                {
                    prefab = prefabs[i].Prefab;
                    break;
                }
            }

            if (prefab == null)
            {
                error = $"A view prefab is required for class {unitClass}.";
                return false;
            }

            error = null;
            return true;
        }

        static bool IsValidClass(MercenaryClass unitClass)
        {
            return unitClass >= MercenaryClass.Warrior && unitClass <= MercenaryClass.Archer;
        }
    }
}
