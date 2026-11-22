using System.Collections.Generic;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [DefaultExecutionOrder(100)]
    public sealed partial class BattleUnitOverlayController : MonoBehaviour
    {
        [SerializeField]
        BattleRunner _runner;

        [SerializeField]
        Camera _worldCamera;

        [SerializeField]
        HexCharacterOverlayView _overlayPrefab;

        [Header("Damage Text Settings")]
        [SerializeField]
        Vector2 _textOffset = Vector2.zero;

        [SerializeField, Min(0.01f)]
        float _spreadDuration = 0.5f;

        [SerializeField, Min(0f)]
        float _spreadDistance = 36f;

        [SerializeField, Min(0f)]
        float _riseDistance = 8f;

        [SerializeField, Min(0.02f)]
        float _displayDuration = 1.2f;

        [SerializeField, Min(0f)]
        float _fadeDuration = 0.3f;

        [Header("Damage Text Colors")]
        [SerializeField]
        Color _physicalColor = Color.white;

        [SerializeField]
        Color _waterColor = new Color(0.48f, 0.8f, 1f);

        [SerializeField]
        Color _fireColor = new Color(1f, 0.27f, 0.07f);

        [SerializeField]
        Color _electricityColor = new Color(0.48f, 0.36f, 1f);

        [SerializeField]
        Color _poisonColor = new Color(0.12f, 0.48f, 0.2f);

        [SerializeField]
        Color _untypedColor = new Color(1f, 0.86f, 0.5f);

        readonly Dictionary<int, HexCharacterOverlayView> _overlays = new Dictionary<int, HexCharacterOverlayView>();
        readonly List<HexCharacterView> _views = new List<HexCharacterView>();
        readonly HashSet<int> _registeredIds = new HashSet<int>();
        readonly List<int> _removedIds = new List<int>();
        Canvas _canvas;
        RectTransform _canvasRect;
        BattleRunner _boundRunner;
        HexCharacterOverlayView _boundPrefab;
        bool _isListening;

        void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvasRect = transform as RectTransform;
        }

        void OnEnable()
        {
            BindPresentation();
        }

        void OnValidate()
        {
            ValidateTextSettings();
        }

        void ValidateTextSettings()
        {
            _textOffset.x = FiniteOrDefault(_textOffset.x, 0f);
            _textOffset.y = FiniteOrDefault(_textOffset.y, 0f);
            _spreadDuration = Mathf.Max(0.01f, FiniteOrDefault(_spreadDuration, 0.5f));
            _spreadDistance = Mathf.Max(0f, FiniteOrDefault(_spreadDistance, 36f));
            _riseDistance = Mathf.Max(0f, FiniteOrDefault(_riseDistance, 8f));
            _displayDuration = Mathf.Max(_spreadDuration + 0.01f, FiniteOrDefault(_displayDuration, 1.2f));
            _fadeDuration = Mathf.Clamp(FiniteOrDefault(_fadeDuration, 0.3f), 0f, _displayDuration);
        }

        static float FiniteOrDefault(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
        }

        void LateUpdate()
        {
            BindPresentation();

            if (_canvas == null || _canvasRect == null || !_canvas.isActiveAndEnabled
                || _runner == null || !_runner.isActiveAndEnabled || !_runner.IsInitialized
                || _overlayPrefab == null)
            {
                HideOverlays();
                ReleaseCombatTexts();
                return;
            }

            if (_worldCamera == null || !_worldCamera.isActiveAndEnabled)
            {
                HideOverlays();
                UpdateCombatTexts(Time.unscaledTime, false, null);
                return;
            }

            Canvas rootCanvas = _canvas.rootCanvas;

            if (rootCanvas.renderMode == RenderMode.WorldSpace
                || rootCanvas.renderMode == RenderMode.ScreenSpaceCamera && rootCanvas.worldCamera == null)
            {
                HideOverlays();
                UpdateCombatTexts(Time.unscaledTime, false, null);
                return;
            }

            Camera canvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            _runner.CopyRegisteredViews(_views);
            _registeredIds.Clear();
            int? currentUnitId = _runner.CurrentUnitId;

            for (int i = 0; i < _views.Count; i++)
            {
                HexCharacterView view = _views[i];

                if (view == null || !_runner.TryGetUnitId(view, out int unitId)
                    || !_runner.TryGetCharacter(unitId, out HexCharacter character))
                {
                    continue;
                }

                _registeredIds.Add(unitId);

                if (!_overlays.TryGetValue(unitId, out HexCharacterOverlayView overlay))
                {
                    overlay = CreateOverlay(character.MaxHealth);
                    _overlays.Add(unitId, overlay);
                }

                if (overlay == null)
                {
                    continue;
                }

                if (!view.isActiveAndEnabled || !TryGetCanvasPosition(view.OverlayWorldPosition, canvasCamera, out Vector2 position))
                {
                    SetOverlayActive(overlay, false);
                    continue;
                }

                RectTransform overlayRect = (RectTransform)overlay.transform;
                overlayRect.anchorMin = _canvasRect.pivot;
                overlayRect.anchorMax = _canvasRect.pivot;
                overlayRect.anchoredPosition = position;
                overlay.SetStatus(character.Health, currentUnitId == unitId);
                overlay.SetStealthStatus(character.Team == BattleTeam.Player && character.IsStealthed, character.IsInvisible);
                SetOverlayActive(overlay, true);
            }

            RemoveUnregisteredOverlays();
            UpdateCombatTexts(Time.unscaledTime, true, canvasCamera);
        }

        void BindPresentation()
        {
            if (_boundRunner != _runner || _boundPrefab != _overlayPrefab)
            {
                UnsubscribePresentation();
                ReleaseOverlays();
                ReleaseCombatTexts();
                _boundRunner = _runner;
                _boundPrefab = _overlayPrefab;
            }

            if (!_isListening && _boundRunner != null)
            {
                _boundRunner.DamageResolved += ReceiveDamageResult;
                _boundRunner.PresentationStopped += ReleaseCombatTexts;
                _isListening = true;
            }
        }

        void UnsubscribePresentation()
        {
            if (_isListening && _boundRunner != null)
            {
                _boundRunner.DamageResolved -= ReceiveDamageResult;
                _boundRunner.PresentationStopped -= ReleaseCombatTexts;
            }

            _isListening = false;
        }

        BattleCombatTextPresentation _textPresentation;
        BattleCombatTextPresentation TextPresentation => _textPresentation ?? (_textPresentation = new BattleCombatTextPresentation(this));

        void ReceiveDamageResult(BattleDamageEvent damage, Vector3 position)
        {
            ReceiveAttackResult(damage.Result, null, position);
        }

        void ReceiveAttackResult(BattleAttackResult result, HexCharacterView target, Vector3 position)
        {
            TextPresentation.ReceiveAttackResult(result, target, position);
        }

        void UpdateCombatTexts(float time, bool canDisplay, Camera canvasCamera)
        {
            TextPresentation.UpdateCombatTexts(time, canDisplay, canvasCamera);
        }

        void ReleaseCombatTexts()
        {
            if (_textPresentation != null)
            {
                _textPresentation.ReleaseCombatTexts();
            }
        }

        HexCharacterOverlayView CreateOverlay(int maxHealth)
        {
            HexCharacterOverlayView overlay = Instantiate(_overlayPrefab, _canvasRect, false);
            SetOverlayActive(overlay, false);

            if (!(overlay.transform is RectTransform overlayRect) || !overlay.TryInitialize(maxHealth))
            {
                DestroyUIObject(overlay.gameObject);
                return null;
            }

            overlayRect.SetAsFirstSibling();
            overlayRect.localScale = Vector3.one;
            overlayRect.localRotation = Quaternion.identity;
            overlayRect.anchoredPosition3D = Vector3.zero;
            return overlay;
        }

        bool TryGetCanvasPosition(Vector3 worldPosition, Camera canvasCamera, out Vector2 position)
        {
            position = default;
            Vector3 screenPosition = _worldCamera.WorldToScreenPoint(worldPosition);

            if (!(screenPosition.z >= _worldCamera.nearClipPlane && screenPosition.z <= _worldCamera.farClipPlane)
                || !_worldCamera.pixelRect.Contains(new Vector2(screenPosition.x, screenPosition.y)))
            {
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPosition, canvasCamera, out position)
                && !float.IsNaN(position.x) && !float.IsInfinity(position.x)
                && !float.IsNaN(position.y) && !float.IsInfinity(position.y);
        }

        void RemoveUnregisteredOverlays()
        {
            _removedIds.Clear();

            foreach (KeyValuePair<int, HexCharacterOverlayView> entry in _overlays)
            {
                if (!_registeredIds.Contains(entry.Key))
                {
                    _removedIds.Add(entry.Key);
                }
            }

            for (int i = 0; i < _removedIds.Count; i++)
            {
                int unitId = _removedIds[i];
                DestroyOverlay(_overlays[unitId]);
                _overlays.Remove(unitId);
            }
        }

        void HideOverlays()
        {
            foreach (KeyValuePair<int, HexCharacterOverlayView> entry in _overlays)
            {
                if (entry.Value != null)
                {
                    SetOverlayActive(entry.Value, false);
                }
            }
        }

        void ReleaseOverlays()
        {
            foreach (KeyValuePair<int, HexCharacterOverlayView> entry in _overlays)
            {
                DestroyOverlay(entry.Value);
            }

            _overlays.Clear();
            _views.Clear();
            _registeredIds.Clear();
            _removedIds.Clear();
        }

        internal void ResetPresentation()
        {
            ReleaseOverlays();
            ReleaseCombatTexts();
        }

        void DestroyOverlay(HexCharacterOverlayView overlay)
        {
            if (overlay != null)
            {
                SetOverlayActive(overlay, false);
                DestroyUIObject(overlay.gameObject);
            }
        }

        static void DestroyUIObject(GameObject gameObject)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        static void SetOverlayActive(HexCharacterOverlayView overlay, bool active)
        {
            if (overlay.gameObject.activeSelf != active)
            {
                overlay.gameObject.SetActive(active);
            }
        }

        void OnDisable()
        {
            UnsubscribePresentation();
            HideOverlays();
            ReleaseCombatTexts();
        }

        void OnDestroy()
        {
            UnsubscribePresentation();
            ReleaseOverlays();
            ReleaseCombatTexts();
        }
    }
}
