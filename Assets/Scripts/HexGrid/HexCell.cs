namespace RaggedBanner.HexGrid
{
    public sealed class HexCell
    {
        public int? OccupantUnitId { get; set; }
        public bool IsOccupied => OccupantUnitId.HasValue;
    }
}
