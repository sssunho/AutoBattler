using System;
using UnityEngine;

namespace RaggedBanner.MercenaryData
{
    [Serializable]
    public struct ClassBaseStats
    {
        [SerializeField]
        int _maxHP;

        [SerializeField]
        int _movement;

        [SerializeField]
        int _initiative;

        [SerializeField]
        float _criticalChance;

        [SerializeField]
        int _physicalDefense;

        [SerializeField]
        int _waterDefense;

        [SerializeField]
        int _fireDefense;

        [SerializeField]
        int _lightningDefense;

        [SerializeField]
        int _poisonDefense;

        public int MaxHP => _maxHP;
        public int Movement => _movement;
        public int Initiative => _initiative;
        public float CriticalChance => _criticalChance;
        public int PhysicalDefense => _physicalDefense;
        public int WaterDefense => _waterDefense;
        public int FireDefense => _fireDefense;
        public int LightningDefense => _lightningDefense;
        public int PoisonDefense => _poisonDefense;

        internal bool Validate(out string error)
        {
            if (_maxHP < 1)
            {
                error = "BaseStats.MaxHP must be at least 1.";
                return false;
            }

            if (_movement < 0)
            {
                error = "BaseStats.Movement must be non-negative.";
                return false;
            }

            if (_initiative < 0)
            {
                error = "BaseStats.Initiative must be non-negative.";
                return false;
            }

            if (!(_criticalChance >= 0f && _criticalChance <= 1f))
            {
                error = "BaseStats.CriticalChance must be a finite value between 0 and 1.";
                return false;
            }

            if (_physicalDefense < 0)
            {
                error = "BaseStats.PhysicalDefense must be non-negative.";
                return false;
            }

            if (_waterDefense < 0)
            {
                error = "BaseStats.WaterDefense must be non-negative.";
                return false;
            }

            if (_fireDefense < 0)
            {
                error = "BaseStats.FireDefense must be non-negative.";
                return false;
            }

            if (_lightningDefense < 0)
            {
                error = "BaseStats.LightningDefense must be non-negative.";
                return false;
            }

            if (_poisonDefense < 0)
            {
                error = "BaseStats.PoisonDefense must be non-negative.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
