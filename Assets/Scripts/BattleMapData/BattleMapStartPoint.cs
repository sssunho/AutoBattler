using System;
using RaggedBanner.HexGrid;
using UnityEngine;

namespace RaggedBanner.BattleMaps
{
    [Serializable]
    public sealed class BattleMapStartPoint
    {
        [SerializeField]
        int _q;

        [SerializeField]
        int _r;

        [SerializeField]
        int _order;

        [SerializeField]
        HexDirection _facing;

        public HexCoordinate Coordinate => new HexCoordinate(_q, _r);
        public int Order => _order;
        public HexDirection Facing => _facing;

        public BattleMapStartPoint(HexCoordinate coordinate, int order, HexDirection facing)
        {
            _q = coordinate.Q;
            _r = coordinate.R;
            _order = order;
            _facing = facing;
        }
    }
}
