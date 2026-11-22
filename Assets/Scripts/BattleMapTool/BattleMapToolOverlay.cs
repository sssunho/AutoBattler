#if UNITY_EDITOR
using System.Collections.Generic;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;

namespace RaggedBanner.BattleMaps.Authoring
{
    internal sealed class BattleMapToolOverlay
    {
        static readonly string[] _classSymbols = { "W", "R", "A" };
        static readonly Vector3[] _corners =
        {
            new Vector3(0f, 0f, 1f),
            new Vector3(-0.8660254f, 0f, 0.5f),
            new Vector3(-0.8660254f, 0f, -0.5f),
            new Vector3(0f, 0f, -1f),
            new Vector3(0.8660254f, 0f, -0.5f),
            new Vector3(0.8660254f, 0f, 0.5f)
        };

        GUIStyle _markerStyle;

        internal void Draw(Camera camera, HexMapView map, IReadOnlyList<BattleMapUnitPlacement> units,
            IReadOnlyList<BattleMapStartPoint> startPoints, bool hasSelection, HexCoordinate selected)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (_markerStyle == null)
            {
                _markerStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 15
                };
            }

            for (int i = 0; i < units.Count; i++)
            {
                BattleMapUnitPlacement unit = units[i];

                if (unit != null && map.IsValid(unit.Coordinate))
                {
                    int unitClass = (int)unit.UnitClass;
                    DrawMarker(camera, map, unit.Coordinate, unit.Facing,
                        unitClass >= 0 && unitClass < _classSymbols.Length ? _classSymbols[unitClass] : "?",
                        new Color(1f, 0.35f, 0.25f));
                }
            }

            for (int i = 0; i < startPoints.Count; i++)
            {
                BattleMapStartPoint point = startPoints[i];

                if (point != null && map.IsValid(point.Coordinate))
                {
                    DrawMarker(camera, map, point.Coordinate, point.Facing, "S" + point.Order,
                        new Color(0.25f, 0.9f, 1f));
                }
            }

            if (hasSelection && map.IsValid(selected))
            {
                Vector3 center = map.transform.InverseTransformPoint(map.AxialToWorld(selected));

                for (int i = 0; i < _corners.Length; i++)
                {
                    Vector3 a = map.transform.TransformPoint(center + _corners[i] * map.HexSize);
                    Vector3 b = map.transform.TransformPoint(center + _corners[(i + 1) % _corners.Length] * map.HexSize);

                    if (TryProject(camera, a, out Vector2 start) && TryProject(camera, b, out Vector2 end))
                    {
                        DrawLine(start, end, Color.yellow, 3f);
                    }
                }
            }
        }

        void DrawMarker(Camera camera, HexMapView map, HexCoordinate coordinate, HexDirection facing,
            string label, Color color)
        {
            Vector3 world = map.AxialToWorld(coordinate);

            if (!TryProject(camera, world, out Vector2 center))
            {
                return;
            }

            Color previous = GUI.color;
            GUI.color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
            float width = Mathf.Max(30f, _markerStyle.CalcSize(new GUIContent(label)).x + 8f);
            Rect rect = new Rect(center.x - width * 0.5f, center.y - 14f, width, 28f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.Box(rect, label, _markerStyle);
            GUI.color = previous;

            if ((int)facing >= 0 && (int)facing < 6)
            {
                Vector3 neighbor = map.AxialToWorld(coordinate.GetNeighbor(facing));
                Vector3 projected = camera.WorldToScreenPoint(Vector3.Lerp(world, neighbor, 0.42f));
                Vector2 delta = new Vector2(projected.x, Screen.height - projected.y) - center;

                if (projected.z > 0f && delta.sqrMagnitude > 0.0001f)
                {
                    Vector2 direction = delta.normalized;
                    Vector2 normal = new Vector2(-direction.y, direction.x);
                    float edge = 1f / Mathf.Max(Mathf.Abs(direction.x) / (width * 0.5f + 3f),
                        Mathf.Abs(direction.y) / 17f);
                    Vector2 tip = center + direction * Mathf.Max(delta.magnitude, edge + 14f);
                    DrawLine(center + direction * edge, tip, color, 3f);
                    DrawLine(tip, tip - direction * 7f + normal * 4f, color, 3f);
                    DrawLine(tip, tip - direction * 7f - normal * 4f, color, 3f);
                }
            }
        }

        static bool TryProject(Camera camera, Vector3 world, out Vector2 point)
        {
            Vector3 screen = camera.WorldToScreenPoint(world);
            point = new Vector2(screen.x, Screen.height - screen.y);
            return screen.z > 0f && camera.pixelRect.Contains(new Vector2(screen.x, screen.y));
        }

        static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 delta = end - start;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
            GUI.DrawTexture(new Rect(start.x, start.y - width * 0.5f, delta.magnitude, width), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }
    }
}
#endif
