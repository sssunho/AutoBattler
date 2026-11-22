using System;
using RaggedBanner.Mercenaries;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    [Serializable]
    public struct BattlePartySpecialization
    {
        [SerializeField, Min(0)]
        int _partyIndex;

        [SerializeField]
        MercenarySpecialization _specialization;

        public int PartyIndex => _partyIndex;
        public MercenarySpecialization Specialization => _specialization;

        public BattlePartySpecialization(int partyIndex, MercenarySpecialization specialization)
        {
            _partyIndex = partyIndex;
            _specialization = specialization;
        }
    }
}
