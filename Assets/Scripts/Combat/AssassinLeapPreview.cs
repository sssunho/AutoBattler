using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public readonly struct AssassinLeapPreview
    {
        public int ActorId { get; }
        public int TargetId { get; }
        public HexCoordinate ActorOrigin { get; }
        public HexCoordinate TargetOrigin { get; }
        public HexDirection TargetFacing { get; }
        public HexCoordinate Landing { get; }

        public AssassinLeapPreview(int actorId, int targetId, HexCoordinate actorOrigin,
            HexCoordinate targetOrigin, HexDirection targetFacing, HexCoordinate landing)
        {
            ActorId = actorId;
            TargetId = targetId;
            ActorOrigin = actorOrigin;
            TargetOrigin = targetOrigin;
            TargetFacing = targetFacing;
            Landing = landing;
        }
    }
}
