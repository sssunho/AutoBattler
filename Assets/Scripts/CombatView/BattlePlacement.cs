using AutoBattler.HexGrid;

namespace AutoBattler.Combat.Unity
{
    internal readonly struct BattlePlacement
    {
        internal UnitDefinition Definition { get; }
        internal Team Team { get; }
        internal HexCoordinate Coordinate { get; }

        internal BattlePlacement(
            UnitDefinition definition,
            Team team,
            HexCoordinate coordinate)
        {
            Definition = definition;
            Team = team;
            Coordinate = coordinate;
        }
    }
}
