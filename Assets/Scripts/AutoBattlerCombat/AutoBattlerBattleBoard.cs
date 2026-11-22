using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace AutoBattler.Combat
{
    public sealed class AutoBattlerBattleBoard
    {
        readonly HexMap _map;
        readonly Dictionary<int, AutoBattlerBattleUnit> _units = new Dictionary<int, AutoBattlerBattleUnit>();

        public int Width => _map.Width;
        public int Height => _map.Height;
        public IReadOnlyCollection<AutoBattlerBattleUnit> Units => _units.Values;

        public AutoBattlerBattleBoard(HexMap map)
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

        public bool TryPlace(AutoBattlerBattleUnit unit, HexCoordinate coordinate)
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

        public bool TryMove(AutoBattlerBattleUnit unit, HexCoordinate destination)
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

        public bool Remove(AutoBattlerBattleUnit unit)
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

        public bool TryApplyDamage(AutoBattlerBattleUnit unit, int damage)
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

        public bool TryGetUnit(int unitId, out AutoBattlerBattleUnit unit)
        {
            return _units.TryGetValue(unitId, out unit);
        }

        public bool TryGetUnit(HexCoordinate coordinate, out AutoBattlerBattleUnit unit)
        {
            unit = null;

            if (!_map.TryGetCell(coordinate, out HexCell cell)
                || !cell.OccupantUnitId.HasValue)
            {
                return false;
            }

            return _units.TryGetValue(cell.OccupantUnitId.Value, out unit);
        }

        public void CopyUnitsTo(List<AutoBattlerBattleUnit> result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            result.Clear();

            foreach (AutoBattlerBattleUnit unit in _units.Values)
            {
                result.Add(unit);
            }
        }

        bool IsRegistered(AutoBattlerBattleUnit unit)
        {
            return unit != null
                && _units.TryGetValue(unit.Id, out AutoBattlerBattleUnit registeredUnit)
                && ReferenceEquals(registeredUnit, unit);
        }
    }
}
