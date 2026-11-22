using System;
using RaggedBanner.Combat;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;
using UnityEngine;

namespace RaggedBanner.BattleMaps
{
    [Serializable]
    public sealed class BattleMapUnitPlacement
    {
        [SerializeField]
        int _q;

        [SerializeField]
        int _r;

        [SerializeField]
        MercenaryClass _unitClass;

        [SerializeField]
        BattleTeam _team = BattleTeam.Enemy;

        [SerializeField]
        HexDirection _facing;

        public HexCoordinate Coordinate => new HexCoordinate(_q, _r);
        public MercenaryClass UnitClass => _unitClass;
        public BattleTeam Team => _team;
        public HexDirection Facing => _facing;

        public BattleMapUnitPlacement(HexCoordinate coordinate, MercenaryClass unitClass,
            BattleTeam team, HexDirection facing)
        {
            _q = coordinate.Q;
            _r = coordinate.R;
            _unitClass = unitClass;
            _team = team;
            _facing = facing;
        }
    }
}
