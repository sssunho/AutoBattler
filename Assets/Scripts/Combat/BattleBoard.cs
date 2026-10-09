using System.Collections.Generic;
using AutoBattler.HexGrid;

namespace RaggedBanner.Combat
{
    public sealed class BattleBoard
    {
        readonly HexMap _map;
        readonly Dictionary<int, HexCharacter> _units = new Dictionary<int, HexCharacter>();

        public int Width => _map.Width;
        public int Height => _map.Height;
        public IReadOnlyCollection<HexCharacter> Units => _units.Values;

        public BattleBoard(int width, int height)
        {
            _map = new HexMap(width, height);
        }

        public bool IsValid(HexCoordinate coordinate)
        {
            return _map.IsValid(coordinate);
        }

        public bool IsOccupied(HexCoordinate coordinate)
        {
            return _map.TryGetCell(coordinate, out HexCell cell) && cell.IsOccupied;
        }

        public bool TryGetUnit(int id, out HexCharacter unit)
        {
            return _units.TryGetValue(id, out unit);
        }

        public bool TryGetUnit(HexCoordinate coordinate, out HexCharacter unit)
        {
            unit = null;

            if (!_map.TryGetCell(coordinate, out HexCell cell) || !cell.OccupantUnitId.HasValue)
            {
                return false;
            }

            return _units.TryGetValue(cell.OccupantUnitId.Value, out unit);
        }

        internal bool TryAdd(int id, HexCoordinate coordinate, out HexCharacter unit)
        {
            unit = null;

            if (id <= 0 || _units.ContainsKey(id)
                || !_map.TryGetCell(coordinate, out HexCell cell) || cell.IsOccupied)
            {
                return false;
            }

            unit = new HexCharacter(id, coordinate);
            _units.Add(id, unit);
            cell.OccupantUnitId = id;
            return true;
        }

        internal bool TryMove(int id, HexCoordinate destination)
        {
            if (!_units.TryGetValue(id, out HexCharacter unit)
                || unit.Coordinate.DistanceTo(destination) != 1
                || !_map.TryGetCell(unit.Coordinate, out HexCell origin) || origin.OccupantUnitId != id
                || !_map.TryGetCell(destination, out HexCell target) || target.IsOccupied)
            {
                return false;
            }

            origin.OccupantUnitId = null;
            target.OccupantUnitId = id;
            unit.SetCoordinate(destination);
            return true;
        }

        internal bool Remove(int id)
        {
            if (!_units.TryGetValue(id, out HexCharacter unit))
            {
                return false;
            }

            _map[unit.Coordinate].OccupantUnitId = null;
            _units.Remove(id);
            return true;
        }
    }
}
