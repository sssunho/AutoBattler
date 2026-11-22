using System;
using UnityEngine;

namespace RaggedBanner.MercenaryData
{
    [Serializable]
    public struct DirectionalPhysicalNegationSettings
    {
        [SerializeField]
        float _frontChance;

        [SerializeField]
        float _sideChance;

        [SerializeField]
        float _rearChance;

        public float FrontChance => _frontChance;
        public float SideChance => _sideChance;
        public float RearChance => _rearChance;

        internal bool Validate(out string error)
        {
            if (!(_frontChance >= 0f && _frontChance <= 1f))
            {
                error = "DirectionalPhysicalNegation.FrontChance must be a finite value between 0 and 1.";
                return false;
            }

            if (!(_sideChance >= 0f && _sideChance <= 1f))
            {
                error = "DirectionalPhysicalNegation.SideChance must be a finite value between 0 and 1.";
                return false;
            }

            if (!(_rearChance >= 0f && _rearChance <= 1f))
            {
                error = "DirectionalPhysicalNegation.RearChance must be a finite value between 0 and 1.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
