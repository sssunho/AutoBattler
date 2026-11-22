using System;

namespace RaggedBanner.Combat
{
    public readonly struct BattleEnvironmentState
    {
        public BattleGroundEffect Ground { get; }
        public BattleAirEffect Air { get; }
        public bool IsEmpty => Ground == BattleGroundEffect.None && Air == BattleAirEffect.None;

        public BattleEnvironmentState(BattleGroundEffect ground, BattleAirEffect air)
        {
            if ((uint)ground > (uint)BattleGroundEffect.Electrified)
            {
                throw new ArgumentOutOfRangeException(nameof(ground));
            }

            if ((uint)air > (uint)BattleAirEffect.ElectricCloud)
            {
                throw new ArgumentOutOfRangeException(nameof(air));
            }

            Ground = ground;
            Air = air;
        }
    }
}
