using AutoBattler.HexGrid;

namespace RaggedBanner.Combat
{
    public sealed class HexCharacter
    {
        HexCoordinate _coordinate;

        public int Id { get; }
        public HexCoordinate Coordinate => _coordinate;

        internal HexCharacter(int id, HexCoordinate coordinate)
        {
            Id = id;
            _coordinate = coordinate;
        }

        internal void SetCoordinate(HexCoordinate coordinate)
        {
            _coordinate = coordinate;
        }
    }
}
