namespace RaggedBanner.Combat
{
    static class BattleEnvironmentRules
    {
        internal static bool TryApplyGround(BattleEnvironmentState current, BattleGroundEffect incoming,
            out BattleEnvironmentState result)
        {
            result = current;

            if (incoming == BattleGroundEffect.None || (uint)incoming > (uint)BattleGroundEffect.Electrified)
            {
                return false;
            }

            if (!TryReactGround(current.Ground, incoming, current.Air, out BattleEnvironmentState next)
                && !TryReactAcross(incoming, current.Air, out next))
            {
                next = new BattleEnvironmentState(incoming, current.Air);
            }

            return TryStabilize(next, out result);
        }

        internal static bool TryApplyAir(BattleEnvironmentState current, BattleAirEffect incoming,
            out BattleEnvironmentState result)
        {
            result = current;

            if (incoming == BattleAirEffect.None || (uint)incoming > (uint)BattleAirEffect.ElectricCloud)
            {
                return false;
            }

            if (!TryReactAcross(current.Ground, incoming, out BattleEnvironmentState next))
            {
                next = new BattleEnvironmentState(current.Ground, incoming);
            }

            return TryStabilize(next, out result);
        }

        static bool TryReactGround(BattleGroundEffect first, BattleGroundEffect second, BattleAirEffect air,
            out BattleEnvironmentState result)
        {
            result = default;

            if (first == BattleGroundEffect.Burning || second == BattleGroundEffect.Burning)
            {
                BattleGroundEffect other = first == BattleGroundEffect.Burning ? second : first;

                switch (other)
                {
                    case BattleGroundEffect.Wet:
                        result = new BattleEnvironmentState(BattleGroundEffect.None, BattleAirEffect.Steam);
                        return true;
                    case BattleGroundEffect.Poisoned:
                        result = new BattleEnvironmentState(BattleGroundEffect.Burning, BattleAirEffect.Smoke);
                        return true;
                    case BattleGroundEffect.Electrified:
                        result = new BattleEnvironmentState(BattleGroundEffect.None, BattleAirEffect.ElectricCloud);
                        return true;
                }
            }

            if (first == BattleGroundEffect.Wet && second == BattleGroundEffect.Electrified
                || first == BattleGroundEffect.Electrified && second == BattleGroundEffect.Wet)
            {
                result = new BattleEnvironmentState(BattleGroundEffect.Electrified, air);
                return true;
            }

            return false;
        }

        static bool TryReactAcross(BattleGroundEffect ground, BattleAirEffect air, out BattleEnvironmentState result)
        {
            result = default;

            if (ground != BattleGroundEffect.Burning || air != BattleAirEffect.PoisonCloud)
            {
                return false;
            }

            result = new BattleEnvironmentState(BattleGroundEffect.Burning, BattleAirEffect.Smoke);
            return true;
        }

        static bool TryStabilize(BattleEnvironmentState current, out BattleEnvironmentState result)
        {
            result = default;
            uint visited = StateBit(current);

            while (TryReactAcross(current.Ground, current.Air, out BattleEnvironmentState next))
            {
                if (next.Ground == current.Ground && next.Air == current.Air)
                {
                    result = current;
                    return true;
                }

                uint bit = StateBit(next);

                if ((visited & bit) != 0)
                {
                    return false;
                }

                visited |= bit;
                current = next;
            }

            result = current;
            return true;
        }

        static uint StateBit(BattleEnvironmentState state)
        {
            return 1u << ((int)state.Ground * 5 + (int)state.Air);
        }
    }
}
