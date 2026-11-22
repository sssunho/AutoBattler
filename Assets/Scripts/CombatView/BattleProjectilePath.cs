using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    internal sealed class BattleProjectilePath
    {
        const double _sqrt3 = 1.7320508075688772;
        readonly List<CellEntry> _entries = new List<CellEntry>();

        readonly struct CellEntry : IComparable<CellEntry>
        {
            internal readonly HexCoordinate _coordinate;
            readonly double _time;

            internal CellEntry(HexCoordinate coordinate, double time)
            {
                _coordinate = coordinate;
                _time = time;
            }

            public int CompareTo(CellEntry other)
            {
                int result = _time.CompareTo(other._time);
                if (result == 0)
                {
                    result = _coordinate.R.CompareTo(other._coordinate.R);
                }
                return result == 0 ? _coordinate.Q.CompareTo(other._coordinate.Q) : result;
            }
        }

        internal void Copy(HexMapView map, Vector3 worldFrom, Vector3 worldTo, List<HexCoordinate> result)
        {
            result.Clear();
            _entries.Clear();
            if (map == null)
            {
                return;
            }

            Vector3 from = map.transform.InverseTransformPoint(worldFrom);
            Vector3 to = map.transform.InverseTransformPoint(worldTo);
            double size = map.HexSize;
            if (!Finite(from.x) || !Finite(from.z) || !Finite(to.x) || !Finite(to.z) || size <= 0d)
            {
                return;
            }

            double fromQ = (_sqrt3 * from.x + from.z) / (3d * size);
            double toQ = (_sqrt3 * to.x + to.z) / (3d * size);
            double fromR = -2d * from.z / (3d * size);
            double toR = -2d * to.z / (3d * size);
            int minQ = (int)Math.Max(0d, Math.Min(map.Width, Math.Floor(Math.Min(fromQ, toQ) - 1d)));
            int maxQ = (int)Math.Max(-1d, Math.Min(map.Width - 1d, Math.Ceiling(Math.Max(fromQ, toQ) + 1d)));
            int minR = (int)Math.Max(0d, Math.Min(map.Height, Math.Floor(Math.Min(fromR, toR) - 1d)));
            int maxR = (int)Math.Max(-1d, Math.Min(map.Height - 1d, Math.Ceiling(Math.Max(fromR, toR) + 1d)));
            for (int r = minR; r <= maxR; r++)
            {
                for (int q = minQ; q <= maxQ; q++)
                {
                    HexCoordinate coordinate = new HexCoordinate(q, r);
                    if (Intersects(coordinate, map.HexSize, from, to, out double time))
                    {
                        _entries.Add(new CellEntry(coordinate, time));
                    }
                }
            }

            _entries.Sort();
            for (int i = 0; i < _entries.Count; i++)
            {
                result.Add(_entries[i]._coordinate);
            }
        }

        static bool Intersects(HexCoordinate coordinate, float size, Vector3 from, Vector3 to, out double enter)
        {
            enter = 0d;
            double exit = 1d;
            double tolerance = size * size * 0.000001d;
            for (int i = 0; i < 6; i++)
            {
                BattleRangeOutline.GetLocalEdge(new BattleHexEdge(coordinate, (HexDirection)i), size,
                    out Vector3 a, out Vector3 b);
                double edgeX = (double)b.x - a.x;
                double edgeZ = (double)b.z - a.z;
                double distance = edgeX * ((double)from.z - a.z) - edgeZ * ((double)from.x - a.x);
                double change = edgeX * ((double)to.z - from.z) - edgeZ * ((double)to.x - from.x);
                if (Math.Abs(change) <= tolerance)
                {
                    if (distance < -tolerance)
                    {
                        return false;
                    }
                    continue;
                }

                double time = (-tolerance - distance) / change;
                if (change > 0d)
                {
                    enter = Math.Max(enter, time);
                }
                else
                {
                    exit = Math.Min(exit, time);
                }
                if (enter > exit)
                {
                    return false;
                }
            }
            return true;
        }

        static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
