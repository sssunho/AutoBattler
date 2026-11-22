using System;
using RaggedBanner.HexGrid.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace RaggedBanner.Combat.Unity
{
    internal sealed class BattleMapSceneSession : IDisposable
    {
        readonly Scene _ownerScene;
        Scene _mapScene;
        Scene _previousActiveScene;
        AsyncOperation _loadOperation;
        AsyncOperation _unloadOperation;
        HexMapView _mapView;
        string _scenePath;
        string _validationError;
        string _cleanupError;
        bool _started;
        bool _disposed;
        bool _attached;

        internal event Action<HexMapView> Ready;
        internal event Action<string> Failed;

        internal string CleanupError => _cleanupError;

        internal BattleMapSceneSession(Scene ownerScene)
        {
            _ownerScene = ownerScene;
        }

        internal bool TryLoad(string scenePath, out string error)
        {
            if (_disposed || _started || !Application.isPlaying
                || !_ownerScene.IsValid() || !_ownerScene.isLoaded)
            {
                error = "This battle scene session cannot start another load.";
                return false;
            }

            if (string.IsNullOrEmpty(scenePath) || SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
            {
                error = "The battle map scene must be enabled in Build Settings: " + scenePath;
                return false;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (string.Equals(SceneManager.GetSceneAt(i).path, scenePath, StringComparison.Ordinal))
                {
                    error = "The map scene is already open. Start Battle with its map scene closed.";
                    return false;
                }
            }

            _started = true;
            _scenePath = scenePath;
            _previousActiveScene = SceneManager.GetActiveScene();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
            _attached = true;

            try
            {
                _loadOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);

                if (_loadOperation == null)
                {
                    throw new InvalidOperationException("Unity did not start loading the map scene.");
                }

                _loadOperation.completed += OnLoadCompleted;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                _loadOperation = null;
                error = "Could not load the battle map: " + exception.Message;
                Dispose();
                return false;
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Additive || _mapScene.IsValid()
                || !string.Equals(scene.path, _scenePath, StringComparison.Ordinal))
            {
                return;
            }

            _mapScene = scene;

            if (_disposed || !_ownerScene.IsValid() || !_ownerScene.isLoaded)
            {
                DeactivateScene(scene);
                return;
            }

            try
            {
                if (!TryFindMap(scene, out _mapView, out _validationError))
                {
                    _mapView = null;
                    DeactivateScene(scene);
                }
            }
            catch (Exception exception)
            {
                _mapView = null;
                _validationError = "Could not validate the battle map: " + exception.Message;
                DeactivateScene(scene);
            }
        }

        void OnLoadCompleted(AsyncOperation operation)
        {
            operation.completed -= OnLoadCompleted;
            _loadOperation = null;

            if (_disposed || !_ownerScene.IsValid() || !_ownerScene.isLoaded)
            {
                Dispose();
                BeginUnload();
                return;
            }

            if (_validationError != null || _mapView == null || !_mapScene.IsValid() || !_mapScene.isLoaded)
            {
                Fail(_validationError ?? "The loaded scene did not provide a valid HexMapView.");
                return;
            }

            if (!SceneManager.SetActiveScene(_mapScene) || SceneManager.GetActiveScene() != _mapScene)
            {
                Fail("Could not activate the battle map scene.");
                return;
            }

            if (_disposed)
            {
                BeginUnload();
                return;
            }

            Ready?.Invoke(_mapView);
        }

        void OnSceneUnloaded(Scene scene)
        {
            if (scene == _ownerScene)
            {
                Fail("The battle host scene was unloaded.");
                return;
            }

            if (scene != _mapScene)
            {
                return;
            }

            _mapScene = default;
            _mapView = null;

            if (!_disposed)
            {
                Fail("The battle map scene was unloaded.");
            }
        }

        void Fail(string error)
        {
            if (!_disposed)
            {
                Failed?.Invoke(error);
            }

            Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Ready = null;
            Failed = null;
            _mapView = null;

            if (_loadOperation == null)
            {
                BeginUnload();
            }
        }

        void BeginUnload()
        {
            if (_unloadOperation != null)
            {
                return;
            }

            if (!Application.isPlaying || !_mapScene.IsValid() || !_mapScene.isLoaded)
            {
                _mapScene = default;
                Detach();
                return;
            }

            if (SceneManager.GetActiveScene() == _mapScene)
            {
                Scene restoreScene = _previousActiveScene.IsValid() && _previousActiveScene.isLoaded
                    ? _previousActiveScene : _ownerScene;

                if (restoreScene.IsValid() && restoreScene.isLoaded)
                {
                    SceneManager.SetActiveScene(restoreScene);
                }
            }

            DeactivateScene(_mapScene);

            try
            {
                _unloadOperation = SceneManager.UnloadSceneAsync(_mapScene);

                if (_unloadOperation == null)
                {
                    throw new InvalidOperationException("Unity did not start unloading the map scene.");
                }

                _unloadOperation.completed += OnUnloadCompleted;
            }
            catch (Exception exception)
            {
                _cleanupError = "Could not unload the battle map: " + exception.Message;
                Detach();
            }
        }

        void OnUnloadCompleted(AsyncOperation operation)
        {
            operation.completed -= OnUnloadCompleted;
            _unloadOperation = null;
            _mapScene = default;
            Detach();
        }

        void Detach()
        {
            if (!_attached)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
            _attached = false;
        }

#if UNITY_EDITOR
        void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                return;
            }

            _disposed = true;
            Ready = null;
            Failed = null;

            if (_loadOperation != null)
            {
                _loadOperation.completed -= OnLoadCompleted;
            }

            if (_unloadOperation != null)
            {
                _unloadOperation.completed -= OnUnloadCompleted;
            }

            _loadOperation = null;
            _unloadOperation = null;
            Detach();
        }
#endif

        static void DeactivateScene(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                roots[i].SetActive(false);
            }
        }

        internal static bool TryFindMap(Scene scene, out HexMapView mapView, out string error)
        {
            mapView = null;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                error = "The map scene is not loaded.";
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            int mapCount = 0;

            for (int i = 0; i < roots.Length; i++)
            {
                MonoBehaviour[] behaviours = roots[i].GetComponentsInChildren<MonoBehaviour>(true);

                for (int j = 0; j < behaviours.Length; j++)
                {
                    MonoBehaviour behaviour = behaviours[j];

                    if (behaviour is HexMapView candidate)
                    {
                        mapView = candidate;
                        mapCount++;
                    }
                    else if (behaviour is BattleRunner || behaviour is BattleStartController || behaviour is HexCharacterView
                        || behaviour is BattleInputController || behaviour is BattleStatusView
                        || behaviour is BattleUnitOverlayController
                        || behaviour is EventSystem && behaviour.isActiveAndEnabled)
                    {
                        error = "The map scene must not contain battle units, battle controllers, or an active EventSystem.";
                        return false;
                    }
                }

                Camera[] cameras = roots[i].GetComponentsInChildren<Camera>(true);

                for (int j = 0; j < cameras.Length; j++)
                {
                    if (cameras[j].isActiveAndEnabled)
                    {
                        error = "The map scene must not contain an active camera. Battle owns the camera.";
                        return false;
                    }
                }

                AudioListener[] listeners = roots[i].GetComponentsInChildren<AudioListener>(true);

                for (int j = 0; j < listeners.Length; j++)
                {
                    if (listeners[j].isActiveAndEnabled)
                    {
                        error = "The map scene must not contain an active AudioListener.";
                        return false;
                    }
                }
            }

            if (mapCount != 1)
            {
                error = "The map scene must contain exactly one HexMapView. Found: " + mapCount + ".";
                return false;
            }

            if (!mapView.isActiveAndEnabled || mapView.Width <= 0 || mapView.Height <= 0
                || !IsFinite(mapView.HexSize) || mapView.HexSize <= 0f)
            {
                error = "HexMapView must be enabled with positive dimensions and a finite positive hex size.";
                return false;
            }

            Matrix4x4 matrix = mapView.transform.localToWorldMatrix;

            for (int i = 0; i < 16; i++)
            {
                if (!IsFinite(matrix[i]))
                {
                    error = "HexMapView has a non-finite transform.";
                    return false;
                }
            }

            if (!IsFinite(matrix.determinant) || matrix.determinant == 0f)
            {
                error = "HexMapView requires a finite, non-zero transform scale.";
                return false;
            }

            error = null;
            return true;
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
