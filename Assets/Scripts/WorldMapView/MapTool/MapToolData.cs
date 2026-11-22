#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RaggedBanner.WorldMap.Unity
{
    [InitializeOnLoad]
    static class MapToolData
    {
        static MapToolData()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MapToolController tool in root.GetComponentsInChildren<MapToolController>(true))
                {
                    try
                    {
                        Dictionary<int, string> catalog = LoadCatalog(tool.CatalogPath);
                        int removed = Validate(tool.Definition, catalog);
                        if (removed > 0 || !HasStart(tool.Definition))
                        {
                            EditorUtility.DisplayDialog("MapTool", Status(tool.Definition, removed), "OK");
                        }
                    }
                    catch (Exception exception)
                    {
                        EditorUtility.DisplayDialog("MapTool validation failed", exception.Message, "OK");
                    }
                }
            }
        }

        internal static Dictionary<int, string> LoadCatalog(string relativePath)
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
            List<List<string>> rows = ParseCsv(File.ReadAllText(path));
            if (rows.Count == 0 || rows[0].Count < 2 || rows[0][0].Trim().TrimStart('\uFEFF') != "ID" || rows[0][1] != "Name")
            {
                throw new InvalidDataException("The catalog must contain ID and Name columns.");
            }

            Dictionary<int, string> catalog = new Dictionary<int, string>();
            for (int i = 1; i < rows.Count; i++)
            {
                List<string> row = rows[i];
                if (row.Count != rows[0].Count || !int.TryParse(row[0], out int id) || id <= 0
                    || string.IsNullOrWhiteSpace(row[1]) || catalog.ContainsKey(id))
                {
                    throw new InvalidDataException("Invalid or duplicate catalog entry at row " + (i + 1));
                }
                catalog.Add(id, row[1]);
            }
            if (catalog.Count == 0)
            {
                throw new InvalidDataException("The catalog is empty. Existing nodes were not removed.");
            }
            return catalog;
        }

        static List<List<string>> ParseCsv(string text)
        {
            List<List<string>> rows = new List<List<string>>();
            List<string> row = new List<string>();
            StringBuilder field = new StringBuilder();
            bool quoted = false;
            bool closed = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                            closed = true;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }
                    continue;
                }
                if (c == ',' || c == '\r' || c == '\n')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    closed = false;
                    if (c != ',')
                    {
                        if (row.Count != 1 || row[0].Length != 0)
                        {
                            rows.Add(row);
                        }
                        row = new List<string>();
                        if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        {
                            i++;
                        }
                    }
                }
                else if (c == '"' && field.Length == 0 && !closed)
                {
                    quoted = true;
                }
                else
                {
                    if (closed || c == '"')
                    {
                        throw new InvalidDataException("Malformed CSV quoting.");
                    }
                    field.Append(c);
                }
            }
            if (quoted)
            {
                throw new InvalidDataException("Unclosed CSV quote.");
            }
            if (field.Length > 0 || row.Count > 0 || closed)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }
            return rows;
        }

        internal static int Validate(WorldMapDefinition definition, Dictionary<int, string> catalog)
        {
            if (definition == null || !AssetDatabase.Contains(definition))
            {
                throw new InvalidOperationException("Assign a saved WorldMapDefinition asset.");
            }
            SerializedObject data = new SerializedObject(definition);
            SerializedProperty locations = data.FindProperty("_locations");
            HashSet<int> removedIds = new HashSet<int>();
            int removed = 0;
            for (int i = locations.arraySize - 1; i >= 0; i--)
            {
                int id = locations.GetArrayElementAtIndex(i).FindPropertyRelative("_id").intValue;
                if (!catalog.ContainsKey(id))
                {
                    removedIds.Add(id);
                    locations.DeleteArrayElementAtIndex(i);
                    removed++;
                }
            }
            if (removed > 0)
            {
                SerializedProperty roads = data.FindProperty("_roads");
                for (int i = roads.arraySize - 1; i >= 0; i--)
                {
                    SerializedProperty road = roads.GetArrayElementAtIndex(i);
                    if (removedIds.Contains(road.FindPropertyRelative("_locationAId").intValue)
                        || removedIds.Contains(road.FindPropertyRelative("_locationBId").intValue))
                    {
                        roads.DeleteArrayElementAtIndex(i);
                    }
                }
                data.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(definition);
            }
            return removed;
        }

        internal static bool HasStart(WorldMapDefinition definition)
        {
            for (int i = 0; i < definition.Locations.Count; i++)
            {
                if (definition.Locations[i].Id == definition.StartLocationId)
                {
                    return true;
                }
            }
            return false;
        }

        internal static string Status(WorldMapDefinition definition, int removed)
        {
            return "Removed invalid nodes: " + removed
                + (HasStart(definition) ? "" : "\nStarting location is missing. Restore that ID or set Start Location Id on the asset.");
        }

        internal static void SavePosition(WorldMapDefinition definition, int index, int expectedId, Vector2 position)
        {
            SerializedObject data = new SerializedObject(definition);
            SerializedProperty nodes = data.FindProperty("_locations");
            if (index < 0 || index >= nodes.arraySize
                || nodes.GetArrayElementAtIndex(index).FindPropertyRelative("_id").intValue != expectedId)
            {
                throw new InvalidOperationException("The asset changed externally. Select the node again.");
            }
            nodes.GetArrayElementAtIndex(index).FindPropertyRelative("_position").vector2Value = position;
            data.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(definition);
        }

        internal static bool IsValidRoadAppearance(Vector2 startOffset, Vector2 endOffset,
            Vector2 controlOffsetA, Vector2 controlOffsetB, float width)
        {
            return IsFinite(startOffset.x) && IsFinite(startOffset.y) && IsFinite(endOffset.x)
                && IsFinite(endOffset.y) && IsFinite(controlOffsetA.x) && IsFinite(controlOffsetA.y)
                && IsFinite(controlOffsetB.x) && IsFinite(controlOffsetB.y) && IsFinite(width) && width > 0f;
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal static void SaveRoad(WorldMapDefinition definition, int index, int expectedId,
            int expectedLocationAId, int expectedLocationBId, WorldMapRoadDefinition original,
            WorldMapRoadDefinition edited)
        {
            if (!IsValidRoadAppearance(edited.StartOffset, edited.EndOffset, edited.ControlOffsetA,
                edited.ControlOffsetB, edited.Width))
            {
                throw new InvalidOperationException("Road offsets and width must be finite, and width must be positive.");
            }
            SerializedObject data = new SerializedObject(definition);
            SerializedProperty roads = data.FindProperty("_roads");
            if (index < 0 || index >= roads.arraySize)
            {
                throw new InvalidOperationException("The asset changed externally. Select the road again.");
            }
            SerializedProperty road = roads.GetArrayElementAtIndex(index);
            if (road.FindPropertyRelative("_id").intValue != expectedId
                || road.FindPropertyRelative("_locationAId").intValue != expectedLocationAId
                || road.FindPropertyRelative("_locationBId").intValue != expectedLocationBId
                || !road.FindPropertyRelative("_startOffset").vector2Value.Equals(original.StartOffset)
                || !road.FindPropertyRelative("_endOffset").vector2Value.Equals(original.EndOffset)
                || !road.FindPropertyRelative("_controlOffsetA").vector2Value.Equals(original.ControlOffsetA)
                || !road.FindPropertyRelative("_controlOffsetB").vector2Value.Equals(original.ControlOffsetB)
                || !road.FindPropertyRelative("_width").floatValue.Equals(original.Width))
            {
                throw new InvalidOperationException("The asset changed externally. Select the road again.");
            }
            road.FindPropertyRelative("_startOffset").vector2Value = edited.StartOffset;
            road.FindPropertyRelative("_endOffset").vector2Value = edited.EndOffset;
            road.FindPropertyRelative("_controlOffsetA").vector2Value = edited.ControlOffsetA;
            road.FindPropertyRelative("_controlOffsetB").vector2Value = edited.ControlOffsetB;
            road.FindPropertyRelative("_width").floatValue = edited.Width;
            data.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(definition);
        }

        internal static void DeleteNode(WorldMapDefinition definition, int index, int expectedId)
        {
            SerializedObject data = new SerializedObject(definition);
            SerializedProperty nodes = data.FindProperty("_locations");
            if (index < 0 || index >= nodes.arraySize
                || nodes.GetArrayElementAtIndex(index).FindPropertyRelative("_id").intValue != expectedId)
            {
                throw new InvalidOperationException("The asset changed externally. Select the node again.");
            }

            nodes.DeleteArrayElementAtIndex(index);
            SerializedProperty roads = data.FindProperty("_roads");
            for (int i = roads.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty road = roads.GetArrayElementAtIndex(i);
                if (road.FindPropertyRelative("_locationAId").intValue == expectedId
                    || road.FindPropertyRelative("_locationBId").intValue == expectedId)
                {
                    roads.DeleteArrayElementAtIndex(i);
                }
            }
            data.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(definition);
        }

        internal static void SaveNode(WorldMapDefinition definition, int index, int id, string name,
            Vector2 position, Sprite icon, Vector2 size)
        {
            SerializedObject data = new SerializedObject(definition);
            SerializedProperty nodes = data.FindProperty("_locations");
            int oldId = 0;
            if (index < 0)
            {
                index = nodes.arraySize;
                nodes.arraySize++;
            }
            else
            {
                oldId = nodes.GetArrayElementAtIndex(index).FindPropertyRelative("_id").intValue;
            }
            SerializedProperty node = nodes.GetArrayElementAtIndex(index);
            node.FindPropertyRelative("_id").intValue = id;
            node.FindPropertyRelative("_displayName").stringValue = name;
            node.FindPropertyRelative("_position").vector2Value = position;
            node.FindPropertyRelative("_icon").objectReferenceValue = icon;
            node.FindPropertyRelative("_iconSize").vector2Value = size;
            if (oldId != 0 && oldId != id)
            {
                SerializedProperty roads = data.FindProperty("_roads");
                for (int i = 0; i < roads.arraySize; i++)
                {
                    SerializedProperty road = roads.GetArrayElementAtIndex(i);
                    SerializedProperty a = road.FindPropertyRelative("_locationAId");
                    SerializedProperty b = road.FindPropertyRelative("_locationBId");
                    if (a.intValue == oldId)
                    {
                        a.intValue = id;
                    }
                    if (b.intValue == oldId)
                    {
                        b.intValue = id;
                    }
                }
                SerializedProperty start = data.FindProperty("_startLocationId");
                if (start.intValue == oldId)
                {
                    start.intValue = id;
                }
            }
            data.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(definition);
        }
    }
}
#endif
