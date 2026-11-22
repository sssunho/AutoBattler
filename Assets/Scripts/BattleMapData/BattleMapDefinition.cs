using System;
using System.Collections.Generic;
using RaggedBanner.Combat;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;
using UnityEngine;

namespace RaggedBanner.BattleMaps
{
    [CreateAssetMenu(menuName = "RaggedBanner/Battle Map", fileName = "BattleMapDefinition")]
    public sealed class BattleMapDefinition : ScriptableObject
    {
        [SerializeField]
        string _sceneGuid;

        [SerializeField]
        string _scenePath;

        [SerializeField]
        BattleMapUnitPlacement[] _units = Array.Empty<BattleMapUnitPlacement>();

        [SerializeField]
        BattleMapStartPoint[] _startPoints = Array.Empty<BattleMapStartPoint>();

        public string SceneGuid => _sceneGuid;
        public string ScenePath => _scenePath;
        public IReadOnlyList<BattleMapUnitPlacement> Units => _units;
        public IReadOnlyList<BattleMapStartPoint> StartPoints => _startPoints;

        public bool Validate(int width, int height, out string error)
        {
            return ValidatePlacements(width, height, _units, _startPoints, out error);
        }

        public static bool ValidatePlacements(int width, int height,
            IReadOnlyList<BattleMapUnitPlacement> units, IReadOnlyList<BattleMapStartPoint> startPoints,
            out string error)
        {
            if (width <= 0 || height <= 0)
            {
                error = "Map width and height must be greater than zero.";
                return false;
            }

            if (units == null || startPoints == null)
            {
                error = "Unit and start point lists must not be null.";
                return false;
            }

            HashSet<HexCoordinate> occupied = new HashSet<HexCoordinate>();

            for (int i = 0; i < units.Count; i++)
            {
                BattleMapUnitPlacement unit = units[i];

                if (unit == null)
                {
                    error = $"Unit {i + 1} is missing.";
                    return false;
                }

                if (!IsWithinMap(unit.Coordinate, width, height))
                {
                    error = $"Unit {i + 1} at {unit.Coordinate} is outside the map.";
                    return false;
                }

                if (unit.Team != BattleTeam.Enemy)
                {
                    error = $"Unit {i + 1} must belong to the Enemy team.";
                    return false;
                }

                if (unit.UnitClass < MercenaryClass.Warrior || unit.UnitClass > MercenaryClass.Archer)
                {
                    error = $"Unit {i + 1} has an invalid class.";
                    return false;
                }

                if (!IsValidFacing(unit.Facing))
                {
                    error = $"Unit {i + 1} has an invalid facing.";
                    return false;
                }

                if (!occupied.Add(unit.Coordinate))
                {
                    error = $"Hex {unit.Coordinate} contains more than one placement.";
                    return false;
                }
            }

            HashSet<int> orders = new HashSet<int>();

            for (int i = 0; i < startPoints.Count; i++)
            {
                BattleMapStartPoint point = startPoints[i];

                if (point == null)
                {
                    error = $"Start point {i + 1} is missing.";
                    return false;
                }

                if (!IsWithinMap(point.Coordinate, width, height))
                {
                    error = $"Start point {i + 1} at {point.Coordinate} is outside the map.";
                    return false;
                }

                if (point.Order < 0)
                {
                    error = $"Start point {i + 1} must have a nonnegative order.";
                    return false;
                }

                if (!orders.Add(point.Order))
                {
                    error = $"Start point order {point.Order} is used more than once.";
                    return false;
                }

                if (!IsValidFacing(point.Facing))
                {
                    error = $"Start point {i + 1} has an invalid facing.";
                    return false;
                }

                if (!occupied.Add(point.Coordinate))
                {
                    error = $"Hex {point.Coordinate} contains more than one placement.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        static bool IsWithinMap(HexCoordinate coordinate, int width, int height)
        {
            return coordinate.Q >= 0 && coordinate.Q < width
                && coordinate.R >= 0 && coordinate.R < height;
        }

        static bool IsValidFacing(HexDirection facing)
        {
            return facing >= HexDirection.East && facing <= HexDirection.SouthEast;
        }
    }
}
