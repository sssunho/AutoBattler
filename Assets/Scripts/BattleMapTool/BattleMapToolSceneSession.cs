#if UNITY_EDITOR
using System;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RaggedBanner.BattleMaps.Authoring
{
    internal sealed class BattleMapToolSceneSession : IDisposable
    {
        readonly Scene _ownerScene;
        Scene _loadedScene;
        Scene _fogScene;
        HexMapView _mapView;
        AsyncOperation _loadOperation;
        AsyncOperation _unloadOperation;
        string _requestedPath;
        string _queuedPath;
        string _status = "Select a map scene.";
        string _afterUnloadStatus = "Map unloaded.";
        bool _loading;
        bool _unloading;
        bool _cancelLoad;
        bool _disposed;
        bool _attached;
        bool _hasFogOverride;
        bool _originalFog;

        internal HexMapView MapView => _mapView;
        internal bool IsBusy => _loading || _unloading;
        internal string ScenePath => _loadedScene.IsValid() ? _loadedScene.path : string.Empty;
        internal string Status => _status;
        internal event Action Changed;

        internal BattleMapToolSceneSession(Scene ownerScene)
        {
            _ownerScene = ownerScene;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            _attached = true;
        }

        internal bool TryLoad(string scenePath, out string error)
        {
            if (_disposed || !Application.isPlaying || !_ownerScene.IsValid() || !_ownerScene.isLoaded)
            {
                error = "Open the BattleMapTool scene and enter Play mode first.";
                return false;
            }

            if (IsBusy)
            {
                error = "Wait for the current scene operation to finish.";
                return false;
            }

            SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath ?? string.Empty);

            if (asset == null)
            {
                error = "Select an existing scene asset.";
                return false;
            }

            scenePath = AssetDatabase.GetAssetPath(asset);

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (scene != _loadedScene && string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    error = "This scene is already open. The tool only loads and owns a separate map scene.";
                    return false;
                }
            }

            _cancelLoad = false;

            if (_loadedScene.IsValid() && _loadedScene.isLoaded)
            {
                _queuedPath = scenePath;
                _afterUnloadStatus = "Map unloaded.";
                return BeginUnload(out error);
            }

            return BeginLoad(scenePath, out error);
        }

        internal void Unload()
        {
            _queuedPath = null;
            _cancelLoad = true;
            _mapView = null;
            _afterUnloadStatus = "Map unloaded.";

            if (_loading)
            {
                _status = "Waiting for the pending scene load to finish before unloading.";
                NotifyChanged();
                return;
            }

            if (!_unloading)
            {
                BeginUnload(out _);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Changed = null;
            Unload();
            DetachWhenIdle();
        }

        bool BeginLoad(string scenePath, out string error)
        {
            _loading = true;
            _requestedPath = scenePath;
            _mapView = null;
            _status = "Loading map scene...";

            try
            {
                _loadOperation = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath,
                    new LoadSceneParameters(LoadSceneMode.Additive));

                if (_loadOperation == null)
                {
                    throw new InvalidOperationException("Unity did not start the scene load.");
                }

                _loadOperation.completed += OnLoadCompleted;
                error = null;
                NotifyChanged();
                return true;
            }
            catch (Exception exception)
            {
                _loading = false;
                _loadOperation = null;
                _requestedPath = null;
                _status = "Could not load the map scene: " + exception.Message;
                error = _status;
                NotifyChanged();
                DetachWhenIdle();
                return false;
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_loading || mode != LoadSceneMode.Additive
                || !string.Equals(scene.path, _requestedPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _loadedScene = scene;

            try
            {
                SuppressGameplay(scene);

                if (!TryFindMap(scene, out _mapView, out string error))
                {
                    _afterUnloadStatus = error;
                    _cancelLoad = true;
                }
            }
            catch (Exception exception)
            {
                _mapView = null;
                _afterUnloadStatus = "Could not prepare the map scene: " + exception.Message;
                _cancelLoad = true;
            }
        }

        void OnLoadCompleted(AsyncOperation operation)
        {
            operation.completed -= OnLoadCompleted;
            _loadOperation = null;
            _loading = false;
            _requestedPath = null;

            if (_disposed || _cancelLoad || _mapView == null)
            {
                if (!_cancelLoad && !_disposed)
                {
                    _afterUnloadStatus = "The loaded scene did not provide a valid HexMapView.";
                }

                BeginUnload(out _);
                return;
            }

            if (!SceneManager.SetActiveScene(_loadedScene) || SceneManager.GetActiveScene() != _loadedScene
                || _disposed || _cancelLoad)
            {
                _afterUnloadStatus = "Could not activate the map scene.";
                BeginUnload(out _);
                return;
            }

            _fogScene = _loadedScene;
            _originalFog = RenderSettings.fog;
            _hasFogOverride = true;
            RenderSettings.fog = false;
            _status = "Map ready: " + _loadedScene.name;
            NotifyChanged();
        }

        bool BeginUnload(out string error)
        {
            RestoreFog();
            _mapView = null;

            if (!_loadedScene.IsValid() || !_loadedScene.isLoaded || !Application.isPlaying)
            {
                _loadedScene = default;
                CompleteUnload();
                error = null;
                return true;
            }

            if (_ownerScene.IsValid() && _ownerScene.isLoaded
                && SceneManager.GetActiveScene() == _loadedScene)
            {
                SceneManager.SetActiveScene(_ownerScene);
            }

            _unloading = true;
            _status = "Unloading map scene...";

            try
            {
                _unloadOperation = SceneManager.UnloadSceneAsync(_loadedScene);

                if (_unloadOperation == null)
                {
                    throw new InvalidOperationException("Unity did not start the scene unload.");
                }

                _unloadOperation.completed += OnUnloadCompleted;
                error = null;
                NotifyChanged();
                return true;
            }
            catch (Exception exception)
            {
                _unloading = false;
                _unloadOperation = null;
                _queuedPath = null;
                _status = "Could not unload the map scene: " + exception.Message;
                error = _status;
                NotifyChanged();
                DetachWhenIdle();
                return false;
            }
        }

        void OnUnloadCompleted(AsyncOperation operation)
        {
            operation.completed -= OnUnloadCompleted;
            _unloadOperation = null;
            _unloading = false;
            _loadedScene = default;
            CompleteUnload();
        }

        void CompleteUnload()
        {
            _status = _afterUnloadStatus;
            string nextPath = _queuedPath;
            _queuedPath = null;

            if (!_disposed && !string.IsNullOrEmpty(nextPath))
            {
                _cancelLoad = false;
                BeginLoad(nextPath, out _);
                return;
            }

            NotifyChanged();
            DetachWhenIdle();
        }

        void OnSceneUnloaded(Scene scene)
        {
            if (scene == _ownerScene)
            {
                Dispose();
                return;
            }

            if (scene == _loadedScene)
            {
                RestoreFog();
                _loadedScene = default;
                _mapView = null;

                if (!IsBusy)
                {
                    _status = "The map scene was unloaded.";
                    NotifyChanged();
                }
            }
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode)
            {
                return;
            }

            RestoreFog();
            _disposed = true;
            Changed = null;

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
            _loading = false;
            _unloading = false;
            _mapView = null;
            _loadedScene = default;
            _queuedPath = null;
            DetachWhenIdle();
        }

        void RestoreFog()
        {
            if (!_hasFogOverride)
            {
                return;
            }

            Scene fogScene = _fogScene;
            bool originalFog = _originalFog;
            _hasFogOverride = false;
            _fogScene = default;

            if (!fogScene.IsValid() || !fogScene.isLoaded)
            {
                return;
            }

            Scene previousActive = SceneManager.GetActiveScene();

            if (previousActive != fogScene && !SceneManager.SetActiveScene(fogScene))
            {
                return;
            }

            try
            {
                if (fogScene.IsValid() && fogScene.isLoaded && SceneManager.GetActiveScene() == fogScene)
                {
                    RenderSettings.fog = originalFog;
                }
            }
            finally
            {
                if (previousActive != fogScene && previousActive.IsValid() && previousActive.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActive);
                }
            }
        }

        void NotifyChanged()
        {
            if (!_disposed)
            {
                Changed?.Invoke();
            }
        }

        void DetachWhenIdle()
        {
            if (!_disposed || IsBusy || !_attached)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            _attached = false;
        }

        static void SuppressGameplay(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);

                for (int j = 0; j < behaviours.Length; j++)
                {
                    MonoBehaviour behaviour = behaviours[j];

                    if (behaviour == null || behaviour is HexMapView)
                    {
                        continue;
                    }

                    Type type = behaviour.GetType();

                    if (type.FullName == "AutoBattler.Rendering.PainterlyAmbientLight"
                        || (type.Namespace != null && type.Namespace.StartsWith("UnityEngine.Rendering", StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    behaviour.enabled = false;
                }

                Camera[] cameras = root.GetComponentsInChildren<Camera>(true);

                for (int j = 0; j < cameras.Length; j++)
                {
                    cameras[j].enabled = false;
                }

                AudioListener[] listeners = root.GetComponentsInChildren<AudioListener>(true);

                for (int j = 0; j < listeners.Length; j++)
                {
                    listeners[j].enabled = false;
                }

                AudioSource[] sources = root.GetComponentsInChildren<AudioSource>(true);

                for (int j = 0; j < sources.Length; j++)
                {
                    sources[j].Stop();
                    sources[j].enabled = false;
                }

                Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);

                for (int j = 0; j < canvases.Length; j++)
                {
                    canvases[j].enabled = false;
                }

                HexCharacterView[] characters = root.GetComponentsInChildren<HexCharacterView>(true);

                for (int j = 0; j < characters.Length; j++)
                {
                    characters[j].gameObject.SetActive(false);
                }
            }
        }

        static bool TryFindMap(Scene scene, out HexMapView mapView, out string error)
        {
            mapView = null;
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                HexMapView[] maps = roots[i].GetComponentsInChildren<HexMapView>(true);

                for (int j = 0; j < maps.Length; j++)
                {
                    mapView = maps[j];
                    count++;
                }
            }

            if (count != 1)
            {
                mapView = null;
                error = "The map scene must contain exactly one HexMapView. Found: " + count + ".";
                return false;
            }

            if (!mapView.isActiveAndEnabled || mapView.Width <= 0 || mapView.Height <= 0
                || !IsFinite(mapView.HexSize) || mapView.HexSize <= 0f)
            {
                mapView = null;
                error = "HexMapView must be active, enabled, and have positive dimensions and a finite positive hex size.";
                return false;
            }

            Matrix4x4 matrix = mapView.transform.localToWorldMatrix;

            for (int i = 0; i < 16; i++)
            {
                if (!IsFinite(matrix[i]))
                {
                    mapView = null;
                    error = "HexMapView has a non-finite transform.";
                    return false;
                }
            }

            float determinant = matrix.determinant;

            if (!IsFinite(determinant) || determinant == 0f)
            {
                mapView = null;
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
#endif
