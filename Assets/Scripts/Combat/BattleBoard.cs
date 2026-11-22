using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat
{
    public sealed class BattleBoard
    {
        readonly HexMap _map;
        readonly Dictionary<int, HexCharacter> _units = new Dictionary<int, HexCharacter>();
        readonly Dictionary<HexCoordinate, BattleEnvironmentState> _environment = new Dictionary<HexCoordinate, BattleEnvironmentState>();
        uint _occupancyVersion;
        uint _environmentVersion;

        public int Width => _map.Width;
        public int Height => _map.Height;
        public IReadOnlyCollection<HexCharacter> Units => _units.Values;
        public uint OccupancyVersion => _occupancyVersion;
        public uint EnvironmentVersion => _environmentVersion;

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

        public bool TryGetEnvironment(HexCoordinate coordinate, out BattleEnvironmentState state)
        {
            state = default;

            if (!_map.IsValid(coordinate))
            {
                return false;
            }

            _environment.TryGetValue(coordinate, out state);
            return true;
        }

        public void CopyEnvironment(List<BattleEnvironmentCell> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();

            foreach (KeyValuePair<HexCoordinate, BattleEnvironmentState> cell in _environment)
            {
                destination.Add(new BattleEnvironmentCell(cell.Key, cell.Value));
            }
        }

        internal bool TrySetEnvironment(HexCoordinate coordinate, BattleEnvironmentState state)
        {
            if (!TryGetEnvironment(coordinate, out BattleEnvironmentState current))
            {
                return false;
            }

            if (current.Ground == state.Ground && current.Air == state.Air)
            {
                return true;
            }

            if (state.IsEmpty)
            {
                _environment.Remove(coordinate);
            }
            else
            {
                _environment[coordinate] = state;
            }

            unchecked
            {
                _environmentVersion++;
            }

            return true;
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

        internal bool TryAdd(int id, BattleTeam team, HexCoordinate coordinate, BattleRules rules,
            BattleUnitStats stats, HexDirection facing, TraitCollection traits,
            BattleTraitSettings traitSettings, out HexCharacter unit,
            MercenarySpecialization specialization = MercenarySpecialization.None, SkillSet skills = null,
            BattleEquipmentLoadout equipment = null)
        {
            unit = null;

            if (id <= 0 || skills == null || _units.ContainsKey(id)
                || !_map.TryGetCell(coordinate, out HexCell cell) || cell.IsOccupied)
            {
                return false;
            }

            unit = new HexCharacter(id, team, coordinate, rules, stats, facing, traits, traitSettings, specialization, skills, equipment);
            _units.Add(id, unit);
            cell.OccupantUnitId = id;

            unchecked
            {
                _occupancyVersion++;
            }

            return true;
        }

        internal bool TryMove(int id, HexCoordinate destination)
        {
            if (!_units.TryGetValue(id, out HexCharacter unit)
                || unit.Coordinate.DistanceTo(destination) != 1)
            {
                return false;
            }

            return TryRelocate(id, destination);
        }

        internal bool TryRelocate(int id, HexCoordinate destination)
        {
            if (!_units.TryGetValue(id, out HexCharacter unit)
                || !_map.TryGetCell(unit.Coordinate, out HexCell origin) || origin.OccupantUnitId != id
                || !_map.TryGetCell(destination, out HexCell target) || target.IsOccupied)
            {
                return false;
            }

            origin.OccupantUnitId = null;
            target.OccupantUnitId = id;
            unit.SetCoordinate(destination);

            unchecked
            {
                _occupancyVersion++;
            }

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

            unchecked
            {
                _occupancyVersion++;
            }

            return true;
        }
    }
}
