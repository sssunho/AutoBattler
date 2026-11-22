using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    public static class BattleSpiritPath
    {
        public static void Copy(HexCoordinate from, HexCoordinate to, List<HexCoordinate> output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            output.Clear();
            long dq = (long)to.Q - from.Q;
            long dr = (long)to.R - from.R;
            long distance = Math.Max(Math.Abs(dq), Math.Max(Math.Abs(dr), Math.Abs(dq + dr)));
            for (long i = 1; i <= distance; i++)
            {
                double t = (double)i / distance;
                double q = from.Q + dq * t + 0.000001;
                double r = from.R + dr * t + 0.000001;
                double s = -q - r;
                double rq = Math.Round(q), rr = Math.Round(r), rs = Math.Round(s);
                double eq = Math.Abs(rq - q), er = Math.Abs(rr - r), es = Math.Abs(rs - s);
                if (eq > er && eq > es)
                {
                    rq = -rr - rs;
                }
                else if (er > es)
                {
                    rr = -rq - rs;
                }
                HexCoordinate cell = new HexCoordinate((int)rq, (int)rr);
                if (output.Count == 0 || output[output.Count - 1] != cell)
                {
                    output.Add(cell);
                }
            }
        }
    }
}
