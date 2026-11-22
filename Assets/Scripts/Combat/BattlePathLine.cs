using System;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public readonly struct BattlePathLine
    {
        public double QWeight { get; }
        public double RWeight { get; }
        public bool IsValid => QWeight != 0d || RWeight != 0d;

        internal static BattlePathLine Between(HexCoordinate start, HexCoordinate target)
        {
            return new BattlePathLine((long)target.R - start.R, (long)start.Q - target.Q);
        }

        public BattlePathLine(double qWeight, double rWeight)
        {
            QWeight = 0d;
            RWeight = 0d;

            if (double.IsNaN(qWeight) || double.IsInfinity(qWeight)
                || double.IsNaN(rWeight) || double.IsInfinity(rWeight))
            {
                return;
            }

            double scale = Math.Max(Math.Abs(qWeight), Math.Abs(rWeight));

            if (scale > 0d)
            {
                QWeight = qWeight / scale;
                RWeight = rWeight / scale;
            }
        }
    }
}
