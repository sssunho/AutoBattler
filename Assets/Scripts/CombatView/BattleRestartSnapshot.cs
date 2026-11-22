using System;
using System.Collections.Generic;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RaggedBanner.Combat.Unity
{
    internal sealed class BattleRestartSnapshot
    {
        readonly Scene _scene;
        readonly int _width;
        readonly int _height;
        readonly float _hexSize;
        readonly Matrix4x4 _localToWorld;

        internal HexMapView MapView { get; }
        internal IReadOnlyList<BattleSpawnInput> Inputs { get; }
        internal BattleRules Rules { get; }
        internal int Seed { get; }
        internal int SkippedPlayerCount { get; }

        internal BattleRestartSnapshot(HexMapView mapView, BattleSpawnInput[] inputs,
            BattleRules rules, int seed, int skippedPlayerCount)
        {
            if (mapView == null || inputs == null || rules == null)
            {
                throw new ArgumentException("A battle snapshot requires a map, participant inputs, and rules.");
            }

            MapView = mapView;
            Inputs = Array.AsReadOnly((BattleSpawnInput[])inputs.Clone());
            Rules = rules;
            Seed = seed;
            SkippedPlayerCount = skippedPlayerCount;
            _scene = mapView.gameObject.scene;
            _width = mapView.Width;
            _height = mapView.Height;
            _hexSize = mapView.HexSize;
            _localToWorld = mapView.transform.localToWorldMatrix;
        }

        internal bool TryValidateMap(out string error)
        {
            if (MapView == null || !MapView.isActiveAndEnabled || !_scene.IsValid()
                || !_scene.isLoaded || MapView.gameObject.scene != _scene)
            {
                error = "최초 전장이 없어졌거나 비활성화되어 재시작할 수 없습니다.";
                return false;
            }

            if (MapView.Width != _width || MapView.Height != _height || MapView.HexSize != _hexSize)
            {
                error = "전장의 크기 또는 hex 크기가 변경되었습니다. Play를 다시 시작하세요.";
                return false;
            }

            Matrix4x4 matrix = MapView.transform.localToWorldMatrix;

            for (int i = 0; i < 16; i++)
            {
                if (matrix[i] != _localToWorld[i])
                {
                    error = "전장의 위치·회전·크기 변환이 변경되었습니다. Play를 다시 시작하세요.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
