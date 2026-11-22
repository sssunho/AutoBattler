using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.Combat.Tests
{
    public sealed class HexCharacterOverlayViewTests
    {
        GameObject _root;
        HexCharacterOverlayView _view;
        RectTransform _healthRoot;
        Image _template;
        Image _marker;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Overlay", typeof(RectTransform));
            _view = _root.AddComponent<HexCharacterOverlayView>();
            _healthRoot = CreateRectTransform("Health", _root.transform);
            _template = CreateRectTransform("Template", _healthRoot).gameObject.AddComponent<Image>();
            _marker = CreateRectTransform("Marker", _root.transform).gameObject.AddComponent<Image>();
            SetField("_healthRoot", _healthRoot);
            SetField("_healthSegmentTemplate", _template);
            SetField("_turnMarker", _marker);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void InitializationShowsFullHealthWithoutInterceptingClicks()
        {
            Assert.That(ReadCombatTextTemplate(), Is.Null);
            Assert.That(TryInitialize(5), Is.True);
            Assert.That(_template.gameObject.activeSelf, Is.False);
            Assert.That(_marker.gameObject.activeSelf, Is.False);
            Assert.That(_marker.raycastTarget, Is.False);
            Image[] segments = ReadSegments();
            Assert.That(segments.Length, Is.EqualTo(5));

            for (int i = 0; i < segments.Length; i++)
            {
                Assert.That(segments[i].gameObject.activeSelf, Is.True);
                Assert.That(segments[i].raycastTarget, Is.False);
                Assert.That(segments[i].rectTransform.sizeDelta, Is.EqualTo(new Vector2(4f, 16f)));
            }

            Assert.That(segments[0].rectTransform.anchoredPosition.x, Is.EqualTo(-12f));
            Assert.That(segments[4].rectTransform.anchoredPosition.x, Is.EqualTo(12f));
            Assert.That(TryInitialize(5), Is.False);
            Assert.That(_healthRoot.childCount, Is.EqualTo(6));
        }

        [Test]
        public void OptionalCombatTextTemplateStaysHiddenAndDoesNotInterceptClicks()
        {
            Text combatText = CreateRectTransform("CombatText", _root.transform).gameObject.AddComponent<Text>();
            combatText.text = "치명타!\n차단!";
            combatText.raycastTarget = true;
            SetField("_combatTextTemplate", combatText);

            Assert.That(TryInitialize(5), Is.True);

            Assert.That(ReadCombatTextTemplate(), Is.SameAs(combatText));
            Assert.That(combatText.gameObject.activeSelf, Is.False);
            Assert.That(combatText.raycastTarget, Is.False);
            SetStatus(2, true);
            Assert.That(_marker.gameObject.activeSelf, Is.True);
            Assert.That(combatText.gameObject.activeSelf, Is.False);
            Assert.That(combatText.text, Is.EqualTo("치명타!\n차단!"));
            Assert.That(ReadSegments()[2].gameObject.activeSelf, Is.False);
        }

        [Test]
        public void DamageHidesRightmostSegmentsWithoutMovingSurvivors()
        {
            Assert.That(TryInitialize(5), Is.True);
            Image[] segments = ReadSegments();
            Vector2[] positions = new Vector2[segments.Length];

            for (int i = 0; i < segments.Length; i++)
            {
                positions[i] = segments[i].rectTransform.anchoredPosition;
            }

            SetStatus(2, true);

            for (int i = 0; i < segments.Length; i++)
            {
                Assert.That(segments[i].gameObject.activeSelf, Is.EqualTo(i < 2));
                Assert.That(segments[i].rectTransform.anchoredPosition, Is.EqualTo(positions[i]));
            }

            Assert.That(_marker.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void HealthAndTurnMarkerRestoreAfterOverlayIsHidden()
        {
            Assert.That(TryInitialize(5), Is.True);
            SetStatus(1, true);
            Image[] segments = ReadSegments();
            _root.SetActive(false);
            SetStatus(4, false);
            _root.SetActive(true);

            for (int i = 0; i < segments.Length; i++)
            {
                Assert.That(segments[i].gameObject.activeInHierarchy, Is.EqualTo(i < 4));
            }

            Assert.That(_marker.gameObject.activeSelf, Is.False);
            Assert.That(_healthRoot.childCount, Is.EqualTo(6));
        }

        bool TryInitialize(int count)
        {
            return (bool)typeof(HexCharacterOverlayView)
                .GetMethod("TryInitialize", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_view, new object[] { count });
        }

        void SetStatus(int health, bool isCurrentTurn)
        {
            typeof(HexCharacterOverlayView)
                .GetMethod("SetStatus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_view, new object[] { health, isCurrentTurn });
        }

        Text ReadCombatTextTemplate()
        {
            return (Text)typeof(HexCharacterOverlayView)
                .GetProperty("CombatTextTemplate", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_view);
        }

        Image[] ReadSegments()
        {
            return (Image[])typeof(HexCharacterOverlayView)
                .GetField("_segments", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_view);
        }

        void SetField(string name, object value)
        {
            typeof(HexCharacterOverlayView)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_view, value);
        }

        static RectTransform CreateRectTransform(string name, Transform parent)
        {
            RectTransform rectTransform = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }
    }
}
