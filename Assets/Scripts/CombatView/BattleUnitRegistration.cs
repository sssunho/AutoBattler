using System;
using UnityEngine;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Unity
{
    [Serializable]
    public sealed class BattleUnitRegistration
    {
        [SerializeField]
        HexCharacterView _view;

        [SerializeField]
        BattleTeam _team;

        [SerializeField]
        MercenaryClass _unitClass;

        public HexCharacterView View => _view;
        public BattleTeam Team => _team;
        public MercenaryClass UnitClass => _unitClass;
    }
}
