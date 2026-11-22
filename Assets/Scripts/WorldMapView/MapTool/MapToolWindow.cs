#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.WorldMap.Unity
{
    sealed class MapToolWindow : EditorWindow
    {
        MapToolController _tool;
        int[] _ids = Array.Empty<int>();
        string[] _names = Array.Empty<string>();
        int _choice;
        Sprite _icon;
        Vector2 _size;
        string _status = "Click an empty map area to create a node, or select an existing node.";
        bool _editing;
        bool _editingRoad;
        bool _roadValid = true;
        int _roadChoice;
        Vector2 _startOffset;
        Vector2 _endOffset;
        Vector2 _controlOffsetA;
        Vector2 _controlOffsetB;
        float _roadWidth;

        internal void Initialize(MapToolController tool)
        {
            _tool = tool;
            titleContent = new GUIContent("MapTool");
            minSize = new Vector2(400f, 440f);
        }

        internal void Edit(int[] ids, string[] names, int id, Sprite icon, Vector2 size)
        {
            _ids = ids;
            _names = names;
            _choice = Array.IndexOf(ids, id);
            _icon = icon;
            _size = size;
            _editing = true;
            _editingRoad = false;
            _status = "Apply saves edits. Delete Node immediately removes the node and its roads.";
            Repaint();
        }

        internal void ClearSelection()
        {
            _editing = false;
            _editingRoad = false;
            _roadChoice = 0;
            Repaint();
        }

        internal void EditRoad(int index, WorldMapRoadDefinition road)
        {
            _editing = false;
            _editingRoad = true;
            _roadChoice = index + 1;
            UpdateRoadAppearance(road);
            _status = "Drag Start / End to set attachments, or Control A / B to bend the road. Apply saves the preview.";
            Repaint();
        }

        internal void UpdateRoadAppearance(WorldMapRoadDefinition road)
        {
            _startOffset = road.StartOffset;
            _endOffset = road.EndOffset;
            _controlOffsetA = road.ControlOffsetA;
            _controlOffsetB = road.ControlOffsetB;
            _roadWidth = road.Width;
            _roadValid = MapToolData.IsValidRoadAppearance(_startOffset, _endOffset,
                _controlOffsetA, _controlOffsetB, _roadWidth);
            Repaint();
        }

        internal void SetStatus(string status)
        {
            _status = status;
            Repaint();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("MapTool - Locations and Roads", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_status, MessageType.Info);
            if (_tool == null || !Application.isPlaying)
            {
                EditorGUILayout.LabelField("Open MapTool scene and enter Play mode.");
                return;
            }
            int roadChoice = EditorGUILayout.Popup("Road", _roadChoice, _tool.RoadNames);
            if (roadChoice != _roadChoice)
            {
                _tool.SelectRoad(roadChoice - 1);
            }
            if (_editingRoad)
            {
                DrawRoadEditor();
                return;
            }
            if (!_editing)
            {
                EditorGUILayout.LabelField("Click a location or an empty area in Game view.");
                return;
            }
            EditorGUI.BeginChangeCheck();
            _choice = EditorGUILayout.Popup("Map ID", _choice, _names);
            _icon = (Sprite)EditorGUILayout.ObjectField("Icon Sprite", _icon, typeof(Sprite), false);
            _size = EditorGUILayout.Vector2Field("Icon Size (map units)", _size);
            if (EditorGUI.EndChangeCheck() && _size.x > 0f && _size.y > 0f
                && !float.IsInfinity(_size.x) && !float.IsInfinity(_size.y))
            {
                _tool.Preview(_icon, _size);
            }
            using (new EditorGUI.DisabledScope(_choice < 0 || _choice >= _ids.Length))
            {
                if (GUILayout.Button("Apply and Save"))
                {
                    if (_tool.Apply(_ids[_choice], _icon, _size, out string message))
                    {
                        _editing = false;
                    }
                    _status = message;
                }
            }
            if (_editing && _tool.HasSelectedNode)
            {
                if (GUILayout.Button("Delete Node"))
                {
                    if (_tool.DeleteSelectedNode(out string message))
                    {
                        _editing = false;
                    }
                    _status = message;
                }
            }
            if (GUILayout.Button("Cancel"))
            {
                _tool.CancelSelection();
                _editing = false;
                _status = "Changes cancelled.";
            }
        }

        void DrawRoadEditor()
        {
            EditorGUI.BeginChangeCheck();
            _startOffset = EditorGUILayout.Vector2Field("Start Offset (normalized)", _startOffset);
            _endOffset = EditorGUILayout.Vector2Field("End Offset (normalized)", _endOffset);
            _controlOffsetA = EditorGUILayout.Vector2Field("Control A Offset (normalized)", _controlOffsetA);
            _controlOffsetB = EditorGUILayout.Vector2Field("Control B Offset (normalized)", _controlOffsetB);
            _roadWidth = EditorGUILayout.FloatField("Width (map units)", _roadWidth);
            if (EditorGUI.EndChangeCheck())
            {
                _roadValid = _tool.PreviewRoad(_startOffset, _endOffset, _controlOffsetA,
                    _controlOffsetB, _roadWidth);
            }
            if (!_roadValid)
            {
                EditorGUILayout.HelpBox("Use finite offsets and a finite, positive width.", MessageType.Warning);
            }
            using (new EditorGUI.DisabledScope(!_roadValid))
            {
                if (GUILayout.Button("Apply and Save Road"))
                {
                    _tool.ApplyRoad(out string message);
                    _status = message;
                }
            }
            if (GUILayout.Button("Cancel"))
            {
                _tool.CancelSelection();
                _status = "Road changes cancelled.";
            }
        }

        internal void Detach()
        {
            _tool = null;
        }

        void OnDestroy()
        {
            if (_tool != null)
            {
                _tool.CancelSelection();
            }
        }
    }
}
#endif
