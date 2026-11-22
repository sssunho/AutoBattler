#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.BattleMaps.Authoring
{
    internal static class BattleMapToolAssetStore
    {
        const string _folder = "Assets/GameData/BattleMaps";

        internal static string ResolveScene(BattleMapDefinition definition)
        {
            if (!string.IsNullOrEmpty(definition.SceneGuid))
            {
                return AssetDatabase.GUIDToAssetPath(definition.SceneGuid);
            }

            return definition.ScenePath;
        }

        internal static bool TrySave(BattleMapDefinition current, string expectedSnapshot, string newName,
            string scenePath, IReadOnlyList<BattleMapUnitPlacement> units,
            IReadOnlyList<BattleMapStartPoint> startPoints, out BattleMapDefinition saved, out string error)
        {
            saved = current;
            error = null;
            string guid = AssetDatabase.AssetPathToGUID(scenePath);

            if (string.IsNullOrEmpty(guid) || AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                error = "The map scene no longer exists. Select the scene again.";
                return false;
            }

            if (current != null && (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(current))
                || EditorJsonUtility.ToJson(current) != expectedSnapshot))
            {
                error = "The asset changed outside this tool. Reload it or save a new copy.";
                return false;
            }

            bool creating = current == null;
            string previousJson = creating ? null : EditorJsonUtility.ToJson(current);

            try
            {
                string path = null;

                if (creating)
                {
                    string name = newName.Trim();

                    if (name.Length == 0 || name == "." || name == ".."
                        || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        error = "Enter a valid asset name without path separators.";
                        return false;
                    }

                    EnsureFolder(_folder);
                    path = AssetDatabase.GenerateUniqueAssetPath(_folder + "/" + name + ".asset");
                    saved = ScriptableObject.CreateInstance<BattleMapDefinition>();
                }
                else
                {
                    Undo.RecordObject(saved, "Save battle map placements");
                }

                SerializedObject data = new SerializedObject(saved);
                data.FindProperty("_sceneGuid").stringValue = guid;
                data.FindProperty("_scenePath").stringValue = scenePath;
                SerializedProperty unitArray = data.FindProperty("_units");
                unitArray.arraySize = units.Count;

                for (int i = 0; i < units.Count; i++)
                {
                    BattleMapUnitPlacement unit = units[i];
                    SerializedProperty item = unitArray.GetArrayElementAtIndex(i);
                    item.FindPropertyRelative("_q").intValue = unit.Coordinate.Q;
                    item.FindPropertyRelative("_r").intValue = unit.Coordinate.R;
                    item.FindPropertyRelative("_unitClass").intValue = (int)unit.UnitClass;
                    item.FindPropertyRelative("_team").intValue = (int)unit.Team;
                    item.FindPropertyRelative("_facing").intValue = (int)unit.Facing;
                }

                SerializedProperty startArray = data.FindProperty("_startPoints");
                startArray.arraySize = startPoints.Count;

                for (int i = 0; i < startPoints.Count; i++)
                {
                    BattleMapStartPoint point = startPoints[i];
                    SerializedProperty item = startArray.GetArrayElementAtIndex(i);
                    item.FindPropertyRelative("_q").intValue = point.Coordinate.Q;
                    item.FindPropertyRelative("_r").intValue = point.Coordinate.R;
                    item.FindPropertyRelative("_order").intValue = point.Order;
                    item.FindPropertyRelative("_facing").intValue = (int)point.Facing;
                }

                data.ApplyModifiedPropertiesWithoutUndo();

                if (creating)
                {
                    AssetDatabase.CreateAsset(saved, path);
                }

                EditorUtility.SetDirty(saved);
                AssetDatabase.SaveAssetIfDirty(saved);
                return true;
            }
            catch (Exception exception)
            {
                if (creating && saved != null && !AssetDatabase.Contains(saved))
                {
                    UnityEngine.Object.DestroyImmediate(saved);
                    saved = null;
                }
                if (!creating && saved != null)
                {
                    EditorJsonUtility.FromJsonOverwrite(previousJson, saved);
                }

                error = "Could not save: " + exception.Message;
                return false;
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
