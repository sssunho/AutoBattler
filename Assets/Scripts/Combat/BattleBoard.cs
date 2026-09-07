using System;
using System.Collections.Generic;
using AutoBattler.HexGrid;

namespace AutoBattler.Combat
{
    public sealed class BattleBoard
    {
        readonly HexMap _map;
        readonly Dictionary<int, BattleUnit> _units = new Dictionary<int, BattleUnit>();

        public int Width => _map.Width;
        public int Height => _map.Height;
        public IReadOnlyCollection<BattleUnit> Units => _units.Values;

        public BattleBoard(HexMap map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public bool IsValid(HexCoordinate coordinate)
        {
            return _map.IsValid(coordinate);
        }

        public bool IsOccupied(HexCoordinate coordinate)
        {
            return _map.TryGetCell(coordinate, out HexCell cell) && cell.IsOccupied;
        }

        public bool TryPlace(BattleUnit unit, HexCoordinate coordinate)
        {
            if (unit == null || _units.ContainsKey(unit.Id))
            {
                return false;
            }

            if (!_map.TryGetCell(coordinate, out HexCell cell) || cell.IsOccupied)
            {
                return false;
            }

            cell.OccupantUnitId = unit.Id;
            unit.Coordinate = coordinate;
            _units.Add(unit.Id, unit);
            return true;
        }

        public bool TryMove(BattleUnit unit, HexCoordinate destination)
        {
            if (!IsRegistered(unit))
            {
                return false;
            }

            HexCoordinate origin = unit.Coordinate;

            if (!_map.TryGetCell(origin, out HexCell originCell)
                || originCell.OccupantUnitId != unit.Id)
            {
                return false;
            }

            if (origin == destination)
            {
                return true;
            }

            if (!_map.TryGetCell(destination, out HexCell destinationCell)
                || destinationCell.IsOccupied)
            {
                return false;
            }

            destinationCell.OccupantUnitId = unit.Id;
            originCell.OccupantUnitId = null;
            unit.Coordinate = destination;
            return true;
        }

        public bool Remove(BattleUnit unit)
        {
            if (!IsRegistered(unit))
            {
                return false;
            }

            if (!_map.TryGetCell(unit.Coordinate, out HexCell cell)
                || cell.OccupantUnitId != unit.Id)
            {
                return false;
            }

            cell.OccupantUnitId = null;
            _units.Remove(unit.Id);
            return true;
        }

        public bool TryApplyDamage(BattleUnit unit, int damage)
        {
            if (damage < 0 || !IsRegistered(unit) || !unit.IsAlive)
            {
                return false;
            }

            if (!_map.TryGetCell(unit.Coordinate, out HexCell cell)
                || cell.OccupantUnitId != unit.Id)
            {
                return false;
            }

            unit.CurrentHealth = Math.Max(0, unit.CurrentHealth - damage);

            if (unit.CurrentHealth > 0)
            {
                return true;
            }

            unit.IsAlive = false;
            unit.ClearAttackState();
            unit.Target = null;
            cell.OccupantUnitId = null;
            _units.Remove(unit.Id);
            return true;
        }

        public bool TryGetUnit(int unitId, out BattleUnit unit)
        {
            return _units.TryGetValue(unitId, out unit);
        }

        public bool TryGetUnit(HexCoordinate coordinate, out BattleUnit unit)
        {
            unit = null;

            if (!_map.TryGetCell(coordinate, out HexCell cell)
                || !cell.OccupantUnitId.HasValue)
            {
                return false;
            }

            return _units.TryGetValue(cell.OccupantUnitId.Value, out unit);
        }

        public void CopyUnitsTo(List<BattleUnit> result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            result.Clear();

            foreach (BattleUnit unit in _units.Values)
            {
                result.Add(unit);
            }
        }

        bool IsRegistered(BattleUnit unit)
        {
            return unit != null
                && _units.TryGetValue(unit.Id, out BattleUnit registeredUnit)
                && ReferenceEquals(registeredUnit, unit);
        }
    }
}
