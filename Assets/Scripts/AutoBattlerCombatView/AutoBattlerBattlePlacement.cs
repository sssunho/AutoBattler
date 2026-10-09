using AutoBattler.HexGrid;

namespace AutoBattler.Combat.Unity
{
    internal readonly struct AutoBattlerBattlePlacement
    {
        internal AutoBattlerUnitDefinition Definition { get; }
        internal AutoBattlerTeam Team { get; }
        internal HexCoordinate Coordinate { get; }

        internal AutoBattlerBattlePlacement(
            AutoBattlerUnitDefinition definition,
            AutoBattlerTeam team,
            HexCoordinate coordinate)
        {
            Definition = definition;
            Team = team;
            Coordinate = coordinate;
        }
    }
}
