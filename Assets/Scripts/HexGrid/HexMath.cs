namespace AutoBattler.HexGrid
{
    public static class HexMath
    {
        public static double Dot(HexCoordinate left, HexCoordinate right)
        {
            double leftQ = left.Q;
            double leftR = left.R;
            double rightQ = right.Q;
            double rightR = right.R;

            return leftQ * rightQ
                + leftR * rightR
                + (leftQ + leftR) * (rightQ + rightR);
        }
    }
}
