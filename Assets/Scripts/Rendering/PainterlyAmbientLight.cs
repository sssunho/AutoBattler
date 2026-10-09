using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace AutoBattler.Rendering
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PainterlyAmbientLight : MonoBehaviour
    {
        static readonly int _ambientColorId = Shader.PropertyToID("_PainterlyAmbientColor");
        static PainterlyAmbientLight _owner;

        [SerializeField]
        Color _color = new Color(0.06786223f, 0.154480457f, 0.235849082f, 1f);

        [SerializeField, Min(0f)]
        float _intensity;

        int _applyPending = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetGlobal()
        {
            _owner = null;
            Shader.SetGlobalVector(_ambientColorId, Vector4.zero);
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void InitializeEditor()
        {
            ResetGlobal();
            AssemblyReloadEvents.beforeAssemblyReload -= ResetGlobal;
            AssemblyReloadEvents.beforeAssemblyReload += ResetGlobal;
        }
#endif

        void OnEnable()
        {
            Interlocked.Exchange(ref _applyPending, 1);
            SceneManager.activeSceneChanged += OnActiveSceneChanged;

#if UNITY_EDITOR
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
#endif

            ApplyIfActiveScene();
        }

        void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;

#if UNITY_EDITOR
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
#endif

            if (_owner == this)
            {
                ResetGlobal();
            }
        }

        void OnValidate()
        {
            Interlocked.Exchange(ref _applyPending, 1);
        }

        void Update()
        {
            ApplyIfActiveScene();
        }

        void OnActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            Interlocked.Exchange(ref _applyPending, 1);
            ApplyIfActiveScene();
        }

        void ApplyIfActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            if (!isActiveAndEnabled || gameObject.scene != activeScene)
            {
                if (_owner == this)
                {
                    ResetGlobal();
                }

                return;
            }

            if (_owner != null && _owner != this && _owner.isActiveAndEnabled
                && _owner.gameObject.scene == activeScene)
            {
                return;
            }

            if (_owner != this)
            {
                _owner = this;
                Interlocked.Exchange(ref _applyPending, 1);
            }

            if (Interlocked.Exchange(ref _applyPending, 0) == 0)
            {
                return;
            }

            Color color = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? _color.linear
                : _color;
            float intensity = Mathf.Max(0f, _intensity);
            Shader.SetGlobalVector(_ambientColorId,
                new Vector4(color.r * intensity, color.g * intensity, color.b * intensity, 0f));
        }
    }
}
