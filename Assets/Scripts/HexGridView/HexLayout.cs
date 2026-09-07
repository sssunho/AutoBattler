using System;
using UnityEngine;

namespace AutoBattler.HexGrid.Unity
{
    public static class HexLayout
    {
        const float _sqrt3 = 1.7320508075688772f;

        public static Vector2 AxialToLocal(HexCoordinate coordinate, float hexSize)
        {
            ValidateHexSize(hexSize);

            float x = _sqrt3 * hexSize * (coordinate.Q + coordinate.R * 0.5f);
            float z = -1.5f * hexSize * coordinate.R;
            return new Vector2(x, z);
        }

        public static HexCoordinate LocalToAxial(Vector2 positionXZ, float hexSize)
        {
            ValidateHexSize(hexSize);

            float q = (_sqrt3 / 3f * positionXZ.x + positionXZ.y / 3f) / hexSize;
            float r = -2f / 3f * positionXZ.y / hexSize;
            return Round(q, r);
        }

        static HexCoordinate Round(float q, float r)
        {
            float s = -q - r;
            int roundedQ = Mathf.RoundToInt(q);
            int roundedR = Mathf.RoundToInt(r);
            int roundedS = Mathf.RoundToInt(s);
            float qDifference = Mathf.Abs(roundedQ - q);
            float rDifference = Mathf.Abs(roundedR - r);
            float sDifference = Mathf.Abs(roundedS - s);

            if (qDifference > rDifference && qDifference > sDifference)
            {
                roundedQ = -roundedR - roundedS;
            }
            else if (rDifference > sDifference)
            {
                roundedR = -roundedQ - roundedS;
            }

            return new HexCoordinate(roundedQ, roundedR);
        }

        static void ValidateHexSize(float hexSize)
        {
            if (hexSize <= 0f || float.IsNaN(hexSize) || float.IsInfinity(hexSize))
            {
                throw new ArgumentOutOfRangeException(nameof(hexSize));
            }
        }
    }
}
