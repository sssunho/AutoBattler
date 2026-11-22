using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.Combat.Unity
{
    public sealed partial class BattleUnitOverlayController
    {
        sealed class BattleCombatTextPresentation
        {
            sealed class CombatTextItem
            {
                internal Text _text;
                internal Vector2 _size;
                internal Vector2 _variation;
                internal Vector2 _destination;
                internal Color _color;
            }

            sealed class CombatTextEntry
            {
                internal readonly List<CombatTextItem> _items = new List<CombatTextItem>(7);
                internal Vector3 _position;
                internal Vector2 _origin;
                internal Rect _reservation;
                internal float _startedAt = -1f;
                internal float _rise;
                internal Vector2 _textOffset;
                internal float _spreadDuration;
                internal float _spreadDistance;
                internal float _riseDistance;
                internal float _displayDuration;
                internal float _fadeDuration;
                internal int _nextItem;
                internal int _activeCount;
                internal bool _placed;
            }

            readonly List<CombatTextEntry> _combatTexts = new List<CombatTextEntry>();
            readonly System.Random _presentationRandom = new System.Random();
            readonly Vector2[] _textSizes = new Vector2[7];
            readonly Vector2[] _textVariations = new Vector2[7];
            readonly Vector2[] _textDestinations = new Vector2[7];
            Rect _textViewport;
            float _textScaleFactor;
            bool _hasTextViewport;

            readonly BattleUnitOverlayController _owner;

            public BattleCombatTextPresentation(BattleUnitOverlayController owner)
            {
                _owner = owner;
            }

            public void ReceiveAttackResult(BattleAttackResult result, HexCharacterView target, Vector3 position)
            {
                if (!_owner.isActiveAndEnabled || _owner._canvas == null || !_owner._canvas.isActiveAndEnabled
                    || _owner._boundRunner != _owner._runner || _owner._boundPrefab != _owner._overlayPrefab
                    || _owner._runner == null || !_owner._runner.isActiveAndEnabled
                    || _owner._overlayPrefab == null || _owner._overlayPrefab.CombatTextTemplate == null
                    || !TryGetTextCamera(out Camera canvasCamera)
                    || !_owner.TryGetCanvasPosition(position, canvasCamera, out _))
                {
                    return;
                }

                _owner.ValidateTextSettings();
                CombatTextEntry entry = new CombatTextEntry
                {
                    _position = position,
                    _textOffset = _owner._textOffset,
                    _spreadDuration = _owner._spreadDuration,
                    _spreadDistance = _owner._spreadDistance,
                    _riseDistance = _owner._riseDistance,
                    _displayDuration = _owner._displayDuration,
                    _fadeDuration = _owner._fadeDuration
                };

                for (int i = 0; i <= (int)AttackAttribute.Untyped; i++)
                {
                    AttackAttribute attribute = (AttackAttribute)i;
                    long damage = result.DamageByAttribute.GetDamage(attribute);

                    if (damage > 0)
                    {
                        AddCombatText(entry, damage.ToString(CultureInfo.InvariantCulture), GetDamageColor(attribute));
                    }
                }

                if (entry._items.Count == 0)
                {
                    AddCombatText(entry, result.WasNegated ? "차단!" : "피해 없음", Color.white);
                }

                if (result.IsCritical)
                {
                    AddCombatText(entry, "치명타!", new Color(1f, 0.85f, 0.45f));
                }

                _combatTexts.Add(entry);
            }

            void AddCombatText(CombatTextEntry entry, string message, Color color)
            {
                Text text = Instantiate(_owner._overlayPrefab.CombatTextTemplate, _owner._canvasRect, false);
                text.gameObject.SetActive(false);
                text.raycastTarget = false;
                text.alignment = TextAnchor.MiddleCenter;
                text.resizeTextForBestFit = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.text = message;
                text.color = color;
                RectTransform rect = text.rectTransform;
                rect.SetAsFirstSibling();
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchorMin = _owner._canvasRect.pivot;
                rect.anchorMax = _owner._canvasRect.pivot;
                Vector2 size = new Vector2(Mathf.Max(1f, text.preferredWidth) + 4f,
                    Mathf.Max(text.fontSize, text.preferredHeight) + 4f);
                rect.sizeDelta = size;
                Outline outline = text.GetComponent<Outline>();

                if (outline == null)
                {
                    outline = text.gameObject.AddComponent<Outline>();
                }

                outline.effectColor = new Color(0.03f, 0.03f, 0.04f, 0.9f);
                outline.effectDistance = new Vector2(1f, -1f);
                outline.useGraphicAlpha = true;
                entry._items.Add(new CombatTextItem
                {
                    _text = text,
                    _size = size,
                    _color = color,
                    _variation = new Vector2((float)_presentationRandom.NextDouble() * 4f - 2f,
                        0.96f + (float)_presentationRandom.NextDouble() * 0.08f)
                });
            }

            Color GetDamageColor(AttackAttribute attribute)
            {
                switch (attribute)
                {
                    case AttackAttribute.Fire:
                        return _owner._fireColor;
                    case AttackAttribute.Water:
                        return _owner._waterColor;
                    case AttackAttribute.Electricity:
                        return _owner._electricityColor;
                    case AttackAttribute.Poison:
                        return _owner._poisonColor;
                    case AttackAttribute.Untyped:
                        return _owner._untypedColor;
                    case AttackAttribute.Physical:
                        return _owner._physicalColor;
                    default:
                        return Color.white;
                }
            }

            bool TryGetTextCamera(out Camera canvasCamera)
            {
                canvasCamera = null;

                if (_owner._worldCamera == null || !_owner._worldCamera.isActiveAndEnabled || _owner._canvas == null
                    || !_owner._canvas.isActiveAndEnabled || _owner._canvasRect == null)
                {
                    return false;
                }

                Canvas root = _owner._canvas.rootCanvas;

                if (root.renderMode == RenderMode.WorldSpace
                    || root.renderMode == RenderMode.ScreenSpaceCamera
                    && (root.worldCamera == null || !root.worldCamera.isActiveAndEnabled))
                {
                    return false;
                }

                canvasCamera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
                return true;
            }

            bool TryGetTextViewport(Camera canvasCamera, out Rect viewport)
            {
                viewport = default;
                Rect pixels = _owner._worldCamera.pixelRect;
                Rect display = _owner._canvas.rootCanvas.pixelRect;
                pixels = Rect.MinMaxRect(Mathf.Max(display.xMin, pixels.xMin), Mathf.Max(display.yMin, pixels.yMin),
                    Mathf.Min(display.xMax, pixels.xMax), Mathf.Min(display.yMax, pixels.yMax));

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_owner._canvasRect, pixels.min,
                    canvasCamera, out Vector2 minimum)
                    || !RectTransformUtility.ScreenPointToLocalPointInRectangle(_owner._canvasRect, pixels.max,
                        canvasCamera, out Vector2 maximum))
                {
                    return false;
                }

                Rect canvasBounds = _owner._canvasRect.rect;
                viewport = Rect.MinMaxRect(Mathf.Max(canvasBounds.xMin, minimum.x), Mathf.Max(canvasBounds.yMin, minimum.y),
                    Mathf.Min(canvasBounds.xMax, maximum.x), Mathf.Min(canvasBounds.yMax, maximum.y));
                return viewport.width > 0f && viewport.height > 0f;
            }

            public void UpdateCombatTexts(float time, bool canDisplay, Camera canvasCamera)
            {
                if (!canDisplay || !TryGetTextCamera(out canvasCamera)
                    || !TryGetTextViewport(canvasCamera, out Rect viewport))
                {
                    ReleaseCombatTexts();
                    return;
                }

                bool resized = _hasTextViewport && (_textViewport != viewport || _textScaleFactor != _owner._canvas.scaleFactor);
                _textViewport = viewport;
                _textScaleFactor = _owner._canvas.scaleFactor;
                _hasTextViewport = true;

                for (int i = _combatTexts.Count - 1; i >= 0; i--)
                {
                    CombatTextEntry entry = _combatTexts[i];

                    if (entry._activeCount > 0 && time - entry._startedAt >= entry._displayDuration)
                    {
                        for (int j = entry._nextItem; j < entry._nextItem + entry._activeCount; j++)
                        {
                            DestroyCombatText(entry._items[j]);
                        }

                        entry._nextItem += entry._activeCount;
                        entry._activeCount = 0;
                        entry._startedAt = -1f;
                        entry._placed = false;
                    }

                    if (entry._nextItem == entry._items.Count)
                    {
                        _combatTexts.RemoveAt(i);
                        continue;
                    }

                    if (resized)
                    {
                        entry._placed = false;
                        SetCombatTextActive(entry, false);
                    }
                }

                if (resized)
                {
                    return;
                }

                for (int i = 0; i < _combatTexts.Count; i++)
                {
                    CombatTextEntry entry = _combatTexts[i];

                    if (entry._placed)
                    {
                        if (entry._nextItem + entry._activeCount < entry._items.Count)
                        {
                            break;
                        }

                        continue;
                    }

                    if (!_owner.TryGetCanvasPosition(entry._position, canvasCamera, out Vector2 desired))
                    {
                        DestroyCombatText(entry);
                        _combatTexts.RemoveAt(i--);
                        continue;
                    }

                    int count = entry._activeCount > 0 ? entry._activeCount : entry._items.Count - entry._nextItem;
                    bool hasReservations = HasCombatTextReservations();
                    bool placed = TryPlaceCombatText(entry, count, desired + entry._textOffset, viewport);

                    if (!placed && !hasReservations && entry._activeCount == 0)
                    {
                        while (--count > 0)
                        {
                            if (TryPlaceCombatText(entry, count, desired + entry._textOffset, viewport))
                            {
                                placed = true;
                                break;
                            }
                        }
                    }

                    if (!placed)
                    {
                        if (!hasReservations && entry._activeCount == 0)
                        {
                            DestroyCombatText(entry);
                            _combatTexts.RemoveAt(i--);
                            continue;
                        }

                        break;
                    }

                    if (entry._activeCount == 0)
                    {
                        entry._activeCount = count;
                        entry._startedAt = time;
                    }

                    entry._placed = true;

                    if (entry._nextItem + entry._activeCount < entry._items.Count)
                    {
                        break;
                    }
                }

                for (int i = 0; i < _combatTexts.Count; i++)
                {
                    CombatTextEntry entry = _combatTexts[i];

                    if (!entry._placed)
                    {
                        continue;
                    }

                    float elapsed = time - entry._startedAt;
                    float fade = entry._fadeDuration > 0f
                        ? Mathf.Clamp01((entry._displayDuration - elapsed) / entry._fadeDuration) : 1f;
                    float appear = Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(elapsed / entry._spreadDuration));

                    for (int j = entry._nextItem; j < entry._nextItem + entry._activeCount; j++)
                    {
                        CombatTextItem item = entry._items[j];
                        item._text.rectTransform.anchoredPosition = BattleDamageFanLayout.Evaluate(entry._origin,
                            item._destination, elapsed, entry._rise, entry._spreadDuration, entry._displayDuration);
                        Color color = item._color;
                        color.a *= fade * appear;
                        item._text.color = color;
                        item._text.gameObject.SetActive(true);
                    }
                }
            }

            bool TryPlaceCombatText(CombatTextEntry entry, int count, Vector2 desired, Rect viewport)
            {
                for (int i = 0; i < count; i++)
                {
                    CombatTextItem item = entry._items[entry._nextItem + i];
                    _textSizes[i] = item._size;
                    _textVariations[i] = item._variation;
                }

                if (!BattleDamageFanLayout.TryArrange(_textSizes, _textVariations, count, viewport, desired,
                    _textDestinations, entry._spreadDistance, entry._riseDistance,
                    out Vector2 origin, out Rect reservation, out float rise))
                {
                    return false;
                }

                for (int i = 0; i < _combatTexts.Count; i++)
                {
                    CombatTextEntry other = _combatTexts[i];

                    if (other != entry && other._placed && other._reservation.Overlaps(reservation))
                    {
                        return false;
                    }
                }

                entry._origin = origin;
                entry._reservation = reservation;
                entry._rise = rise;

                for (int i = 0; i < count; i++)
                {
                    entry._items[entry._nextItem + i]._destination = _textDestinations[i];
                }

                return true;
            }

            bool HasCombatTextReservations()
            {
                for (int i = 0; i < _combatTexts.Count; i++)
                {
                    if (_combatTexts[i]._placed)
                    {
                        return true;
                    }
                }

                return false;
            }

            public void ReleaseCombatTexts()
            {
                for (int i = 0; i < _combatTexts.Count; i++)
                {
                    DestroyCombatText(_combatTexts[i]);
                }

                _combatTexts.Clear();
                _hasTextViewport = false;
            }

            void DestroyCombatText(CombatTextEntry entry)
            {
                for (int i = 0; i < entry._items.Count; i++)
                {
                    DestroyCombatText(entry._items[i]);
                }
            }

            void DestroyCombatText(CombatTextItem item)
            {
                if (item._text != null)
                {
                    item._text.gameObject.SetActive(false);
                    DestroyUIObject(item._text.gameObject);
                    item._text = null;
                }
            }

            static void SetCombatTextActive(CombatTextEntry entry, bool active)
            {
                for (int i = 0; i < entry._items.Count; i++)
                {
                    Text text = entry._items[i]._text;

                    if (text != null && text.gameObject.activeSelf != active)
                    {
                        text.gameObject.SetActive(active);
                    }
                }
            }

        }
    }
}
