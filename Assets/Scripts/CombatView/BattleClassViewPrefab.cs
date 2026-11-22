using System;
using RaggedBanner.Mercenaries;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    [Serializable]
    public sealed class BattleClassViewPrefab
    {
        [SerializeField]
        MercenaryClass _unitClass;

        [SerializeField]
        HexCharacterView _prefab;

        public MercenaryClass UnitClass => _unitClass;
        public HexCharacterView Prefab => _prefab;

        public BattleClassViewPrefab(MercenaryClass unitClass, HexCharacterView prefab)
        {
            _unitClass = unitClass;
            _prefab = prefab;
        }
    }
}
