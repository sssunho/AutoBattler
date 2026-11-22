using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    internal readonly struct BattleHexEdge
    {
        internal HexCoordinate Coordinate { get; }
        internal HexDirection Direction { get; }

        internal BattleHexEdge(HexCoordinate coordinate, HexDirection direction)
        {
            Coordinate = coordinate;
            Direction = direction;
        }
    }

    internal sealed class BattleRangeOutline
    {
        const float _halfSqrt3 = 0.8660254037844386f;

        static readonly Vector3[] _cornerDirections =
        {
            new Vector3(0f, 0f, 1f),
            new Vector3(-_halfSqrt3, 0f, 0.5f),
            new Vector3(-_halfSqrt3, 0f, -0.5f),
            new Vector3(0f, 0f, -1f),
            new Vector3(_halfSqrt3, 0f, -0.5f),
            new Vector3(_halfSqrt3, 0f, 0.5f)
        };

        readonly HashSet<HexCoordinate> _region = new HashSet<HexCoordinate>();
        readonly List<HexCoordinate> _regionOrder = new List<HexCoordinate>();
        readonly HashSet<HexCoordinate> _exterior = new HashSet<HexCoordinate>();
        readonly Queue<HexCoordinate> _frontier = new Queue<HexCoordinate>();

        internal void Build(int width, int height, HexCoordinate origin,
            IReadOnlyList<HexCoordinate> movementDestinations, List<BattleHexEdge> result)
        {
            _region.Clear();
            _regionOrder.Clear();
            _exterior.Clear();
            _frontier.Clear();

            if (result == null)
            {
                return;
            }

            result.Clear();

            if (width <= 0 || height <= 0 || !IsValid(origin, width, height)
                || movementDestinations == null || movementDestinations.Count == 0)
            {
                return;
            }

            _region.Add(origin);
            _regionOrder.Add(origin);
            int minQ = origin.Q;
            int maxQ = origin.Q;
            int minR = origin.R;
            int maxR = origin.R;

            for (int i = 0; i < movementDestinations.Count; i++)
            {
                HexCoordinate coordinate = movementDestinations[i];

                if (!IsValid(coordinate, width, height) || !_region.Add(coordinate))
                {
                    continue;
                }

                _regionOrder.Add(coordinate);
                minQ = Math.Min(minQ, coordinate.Q);
                maxQ = Math.Max(maxQ, coordinate.Q);
                minR = Math.Min(minR, coordinate.R);
                maxR = Math.Max(maxR, coordinate.R);
            }

            if (_regionOrder.Count == 1)
            {
                return;
            }

            minQ--;
            maxQ++;
            minR--;
            maxR++;
            HexCoordinate outside = new HexCoordinate(minQ, minR);
            _exterior.Add(outside);
            _frontier.Enqueue(outside);

            while (_frontier.Count > 0)
            {
                HexCoordinate current = _frontier.Dequeue();

                for (int i = 0; i < 6; i++)
                {
                    HexCoordinate neighbor = current.GetNeighbor((HexDirection)i);

                    if (neighbor.Q < minQ || neighbor.Q > maxQ || neighbor.R < minR || neighbor.R > maxR
                        || _region.Contains(neighbor) || !_exterior.Add(neighbor))
                    {
                        continue;
                    }

                    _frontier.Enqueue(neighbor);
                }
            }

            for (int i = 0; i < _regionOrder.Count; i++)
            {
                HexCoordinate coordinate = _regionOrder[i];

                for (int direction = 0; direction < 6; direction++)
                {
                    HexDirection facing = (HexDirection)direction;

                    if (_exterior.Contains(coordinate.GetNeighbor(facing)))
                    {
                        result.Add(new BattleHexEdge(coordinate, facing));
                    }
                }
            }
        }

        internal static void GetLocalEdge(BattleHexEdge edge, float hexSize, out Vector3 start, out Vector3 end)
        {
            if ((uint)edge.Direction >= 6)
            {
                throw new ArgumentOutOfRangeException(nameof(edge));
            }

            Vector2 position = HexLayout.AxialToLocal(edge.Coordinate, hexSize);
            Vector3 center = new Vector3(position.x, 0f, position.y);
            int corner = ((int)edge.Direction + 4) % 6;
            start = center + _cornerDirections[corner] * hexSize;
            end = center + _cornerDirections[(corner + 1) % 6] * hexSize;
        }

        static bool IsValid(HexCoordinate coordinate, int width, int height)
        {
            return coordinate.Q >= 0 && coordinate.Q < width && coordinate.R >= 0 && coordinate.R < height;
        }
    }
}
