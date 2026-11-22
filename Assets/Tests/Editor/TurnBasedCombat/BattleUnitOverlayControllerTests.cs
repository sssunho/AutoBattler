using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using UnityEngine;
using UnityEngine.UI;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleUnitOverlayControllerTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();

        BattleUnitOverlayController _controller;
        Camera _camera;
        Camera _uiCamera;
        Canvas _canvas;
        HexCharacterView _characterView;
        Transform _headAnchor;
        HexCharacter _character;
        Text _combatTextTemplate;

        [SetUp]
        public void SetUp()
        {
            _camera = CreateObject("World Camera").AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
            _camera.nearClipPlane = 0.3f;
            _camera.farClipPlane = 100f;
            _camera.pixelRect = new Rect(0f, 0f, 800f, 600f);

            BattleSimulation simulation = new BattleSimulation(2, 1, randomSeed: 0);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(), HexDirection.East, out _character), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 0),
                BattleTestUtility.CreateStats(), HexDirection.East, out _), Is.True);
            Assert.That(simulation.TryStartBattle(), Is.True);

            if (simulation.CurrentUnitId != _character.Id)
            {
                Assert.That(simulation.TryEndTurn(simulation.CurrentUnitId.Value), Is.True);
            }

            BattleRunner runner = CreateObject("Runner").AddComponent<BattleRunner>();
            SetField(runner, "_simulation", simulation);
            _characterView = CreateObject("Character").AddComponent<HexCharacterView>();
            SetField(_characterView, "_character", _character);
            _characterView.transform.position = new Vector3(0f, 0f, 10f);
            _headAnchor = new GameObject("Head Anchor").transform;
            _headAnchor.SetParent(_characterView.transform, false);
            _headAnchor.localPosition = new Vector3(0f, 2.1f, 0f);
            SetField(_characterView, "_overlayAnchor", _headAnchor);
            ReadField<Dictionary<int, HexCharacterView>>(runner, "_views").Add(_character.Id, _characterView);

            HexCharacterOverlayView prefab = CreateOverlay();
            _canvas = CreateObject("Canvas", typeof(RectTransform)).AddComponent<Canvas>();
            _uiCamera = CreateObject("UI Camera").AddComponent<Camera>();
            _uiCamera.transform.position = new Vector3(0f, 0f, -100f);
            _uiCamera.orthographic = true;
            _uiCamera.cullingMask = 0;
            _uiCamera.pixelRect = _camera.pixelRect;
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _uiCamera;
            _canvas.planeDistance = 1f;
            _canvas.scaleFactor = 2f;
            ((RectTransform)_canvas.transform).pivot = new Vector2(0.2f, 0.7f);
            Canvas.ForceUpdateCanvases();

            _controller = _canvas.gameObject.AddComponent<BattleUnitOverlayController>();
            SetField(_controller, "_runner", runner);
            SetField(_controller, "_worldCamera", _camera);
            SetField(_controller, "_overlayPrefab", prefab);
            SetField(_controller, "_textOffset", new Vector2(0f, 56f));
            SetField(_controller, "_spreadDuration", 0.2f);
            SetField(_controller, "_spreadDistance", 36f);
            SetField(_controller, "_riseDistance", 18f);
            SetField(_controller, "_displayDuration", 0.8f);
            SetField(_controller, "_fadeDuration", 0.25f);
            Invoke(_controller, "Awake");
        }

        [TearDown]
        public void TearDown()
        {
            if (_controller != null)
            {
                ReadField<Dictionary<int, HexCharacterOverlayView>>(_controller, "_overlays").Clear();
                Invoke(_controller, "ReleaseCombatTexts");
            }

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void OverlayTracksPresentedPositionOnScaledCanvasWithNoncentralPivot()
        {
            _characterView.transform.position = new Vector3(1.25f, 0.5f, 10f);
            Refresh();
            HexCharacterOverlayView overlay = ReadOverlay();
            AssertProjectedPosition(overlay);

            _characterView.transform.position = new Vector3(-1f, 1f, 10f);
            Refresh();

            Assert.That(ReadOverlay(), Is.SameAs(overlay));
            Assert.That(_character.Coordinate, Is.EqualTo(default(HexCoordinate)));
            Assert.That(_canvas.scaleFactor, Is.EqualTo(2f));
            Assert.That(((RectTransform)overlay.transform).anchorMin, Is.EqualTo(new Vector2(0.2f, 0.7f)));
            AssertProjectedPosition(overlay);
        }

        [Test]
        public void BehindCameraOverlayHidesAndRestoresWithoutRecreation()
        {
            Refresh();
            HexCharacterOverlayView overlay = ReadOverlay();
            Assert.That(overlay.gameObject.activeSelf, Is.True);

            _characterView.transform.position = new Vector3(0f, 0f, -1f);
            Refresh();
            Assert.That(overlay.gameObject.activeSelf, Is.False);

            _characterView.transform.position = new Vector3(0f, 0f, 10f);
            Refresh();
            Assert.That(ReadOverlay(), Is.SameAs(overlay));
            Assert.That(overlay.gameObject.activeSelf, Is.True);
            Assert.That(ReadField<Image>(overlay, "_turnMarker").gameObject.activeInHierarchy, Is.True);

            overlay.gameObject.SetActive(false);
            Refresh();
            Assert.That(overlay.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void DifferentClassesCreateIndependentHealthSegmentCounts()
        {
            BattleSimulation simulation = new BattleSimulation(3, 1, randomSeed: 0);
            BattleRunner runner = ReadField<BattleRunner>(_controller, "_runner");
            SetField(runner, "_simulation", simulation);
            Dictionary<int, HexCharacterView> views = ReadField<Dictionary<int, HexCharacterView>>(runner, "_views");
            views.Clear();
            MercenaryClass[] classes = { MercenaryClass.Warrior, MercenaryClass.Rogue, MercenaryClass.Archer };
            int[] expectedHealth = { 10, 8, 8 };
            HexCharacter[] characters = new HexCharacter[classes.Length];

            for (int i = 0; i < classes.Length; i++)
            {
                Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(i, 0),
                    BattleTestUnit.ForClass(classes[i]), HexDirection.East, out characters[i]), Is.True);
                HexCharacterView view = CreateObject("Class Character").AddComponent<HexCharacterView>();
                SetField(view, "_character", characters[i]);
                view.transform.position = new Vector3(i, 0f, 10f);
                views.Add(characters[i].Id, view);
            }

            Refresh();
            Dictionary<int, HexCharacterOverlayView> overlays =
                ReadField<Dictionary<int, HexCharacterOverlayView>>(_controller, "_overlays");

            for (int i = 0; i < characters.Length; i++)
            {
                Image[] segments = ReadField<Image[]>(overlays[characters[i].Id], "_segments");
                Assert.That(segments.Length, Is.EqualTo(expectedHealth[i]));

                for (int j = 0; j < segments.Length; j++)
                {
                    Assert.That(segments[j].gameObject.activeSelf, Is.True);
                }
            }

            SetField(characters[1], "_health", 3);
            Refresh();
            Image[] damagedSegments = ReadField<Image[]>(overlays[characters[1].Id], "_segments");
            Assert.That(damagedSegments.Length, Is.EqualTo(8));

            for (int i = 0; i < damagedSegments.Length; i++)
            {
                Assert.That(damagedSegments[i].gameObject.activeSelf, Is.EqualTo(i < 3));
            }
        }

        [TestCase(false, false, 3, "3", 1)]
        [TestCase(false, false, 0, "피해 없음", 1)]
        [TestCase(false, true, 0, "차단!", 1)]
        [TestCase(true, false, 3, "3", 2)]
        [TestCase(true, true, 0, "차단!", 2)]
        public void DamageAndResultMessagesHaveIndependentTextItems(
            bool critical, bool negated, int damage, string expected, int count)
        {
            Refresh();
            ReceiveResult(CreateResult(_character.Id, damage, 10,
                AttackAttribute.Physical, new BattleDamageBreakdown(physical: negated ? 0 : damage), critical, negated));
            UpdateCombatTexts(10f);
            IList items = ReadItems(ReadCombatEntry());
            Assert.That(items.Count, Is.EqualTo(count));
            Assert.That(ReadText(items[0]).text, Is.EqualTo(expected));
            Assert.That(ReadText(items[0]).raycastTarget, Is.False);
            Assert.That(ReadText(items[0]).GetComponent<Outline>(), Is.Not.Null);

            if (critical)
            {
                Assert.That(ReadText(items[1]).text, Is.EqualTo("치명타!"));
            }
        }

        [Test]
        public void CompositeDamageUsesColoredNumbersFromOneOriginThenSeparates()
        {
            Refresh();
            ReceiveResult(CompositeResult());
            UpdateCombatTexts(10f);
            object entry = ReadCombatEntry();
            IList items = ReadItems(entry);
            Assert.That(items.Count, Is.EqualTo(4));
            Vector2 origin = ReadText(items[0]).rectTransform.anchoredPosition;
            Color[] colors =
            {
                new Color(0.48f, 0.8f, 1f), new Color(1f, 0.27f, 0.07f),
                new Color(0.48f, 0.36f, 1f), new Color(0.12f, 0.48f, 0.2f)
            };

            for (int i = 0; i < items.Count; i++)
            {
                Text text = ReadText(items[i]);
                Assert.That(text.text, Is.EqualTo("1"));
                Assert.That(text.rectTransform.anchoredPosition, Is.EqualTo(origin));
                Assert.That(text.color.r, Is.EqualTo(colors[i].r).Within(0.001f));
                Assert.That(text.color.g, Is.EqualTo(colors[i].g).Within(0.001f));
                Assert.That(text.color.b, Is.EqualTo(colors[i].b).Within(0.001f));
            }

            UpdateCombatTexts(10.21f);

            for (int i = 0; i < items.Count; i++)
            {
                Text text = ReadText(items[i]);
                Assert.That(text.rectTransform.anchoredPosition.y, Is.GreaterThan(origin.y));
                Rect bounds = TextBounds(text);

                for (int j = 0; j < i; j++)
                {
                    Assert.That(bounds.Overlaps(TextBounds(ReadText(items[j]))), Is.False);
                }
            }
        }

        [Test]
        public void CriticalOverkillKeepsEachCalculatedComponentAndOneUntypedBonus()
        {
            Refresh();
            ReceiveResult(CreateResult(_character.Id, 1, 0, null,
                new BattleDamageBreakdown(water: 1, fire: 1, electricity: 1, poison: 1, untyped: 2), true, false));
            IList items = ReadItems(ReadCombatEntry());
            Assert.That(items.Count, Is.EqualTo(6));
            Assert.That(ReadText(items[4]).text, Is.EqualTo("2"));
            Assert.That(ReadText(items[5]).text, Is.EqualTo("치명타!"));
            Assert.That(ReadText(items[4]).color, Is.EqualTo(new Color(1f, 0.86f, 0.5f)));
        }

        [Test]
        public void ConsecutiveHitsKeepSeparateGroupsAndWaitForSpace()
        {
            Refresh();
            ReceiveResult(CompositeResult());
            ReceiveResult(CompositeResult());
            Assert.That(ReadCombatEntries().Count, Is.EqualTo(2));
            object first = ReadCombatEntries()[0];
            object second = ReadCombatEntries()[1];
            UpdateCombatTexts(10f);
            Assert.That(ReadField<float>(first, "_startedAt"), Is.EqualTo(10f));
            Assert.That(ReadField<float>(second, "_startedAt"), Is.LessThan(0f));
            Assert.That(ReadText(ReadItems(second)[0]).gameObject.activeSelf, Is.False);
            UpdateCombatTexts(10.81f);
            Assert.That(ReadCombatEntries().Count, Is.EqualTo(1));
            Assert.That(ReadCombatEntry(), Is.SameAs(second));
            Assert.That(ReadField<float>(second, "_startedAt"), Is.EqualTo(10.81f));
            Assert.That(ReadText(ReadItems(second)[0]).gameObject.activeSelf, Is.True);
        }

        [Test]
        public void DisplayUsesPositionSnapshotAfterTargetRemovalAndCameraMovement()
        {
            Refresh();
            ReceiveResult(CompositeResult());
            UpdateCombatTexts(10f);
            object entry = ReadCombatEntry();
            Vector2 origin = ReadField<Vector2>(entry, "_origin");
            BattleRunner runner = ReadField<BattleRunner>(_controller, "_runner");
            ReadField<Dictionary<int, HexCharacterView>>(runner, "_views").Clear();
            Object.DestroyImmediate(_characterView.gameObject);
            _camera.transform.position += Vector3.right;
            UpdateCombatTexts(10.3f);
            Assert.That(ReadCombatEntries().Count, Is.EqualTo(1));
            Assert.That(ReadField<Vector2>(entry, "_origin"), Is.EqualTo(origin));
            Assert.That(ReadText(ReadItems(entry)[0]).gameObject.activeSelf, Is.True);
            UpdateCombatTexts(10.81f);
            Assert.That(ReadCombatEntries().Count, Is.Zero);
        }

        [Test]
        public void ResizeHidesForOneUpdateAndDoesNotRestartLifetime()
        {
            Refresh();
            ReceiveResult(CompositeResult());
            UpdateCombatTexts(10f);
            object entry = ReadCombatEntry();
            _canvas.scaleFactor = 1f;
            Canvas.ForceUpdateCanvases();
            UpdateCombatTexts(10.3f);
            Assert.That(ReadText(ReadItems(entry)[0]).gameObject.activeSelf, Is.False);
            UpdateCombatTexts(10.31f);
            Assert.That(ReadField<float>(entry, "_startedAt"), Is.EqualTo(10f));
            UpdateCombatTexts(10.81f);
            Assert.That(ReadCombatEntries().Count, Is.Zero);
        }

        [TestCase("stop")]
        [TestCase("disable")]
        [TestCase("reset")]
        [TestCase("prefab")]
        [TestCase("runner")]
        [TestCase("camera")]
        public void PresentationChangesReleaseActiveAndPendingText(string change)
        {
            Refresh();
            ReceiveResult(CompositeResult());
            ReceiveResult(CompositeResult());
            UpdateCombatTexts(10f);
            Text active = ReadText(ReadItems(ReadCombatEntries()[0])[0]);
            Text pending = ReadText(ReadItems(ReadCombatEntries()[1])[0]);

            switch (change)
            {
                case "stop":
                    BattleRunner runner = ReadField<BattleRunner>(_controller, "_runner");
                    ReadField<System.Action>(runner, "PresentationStopped").Invoke();
                    break;
                case "disable":
                    _controller.enabled = false;
                    Invoke(_controller, "OnDisable");
                    break;
                case "reset":
                    _controller.ResetPresentation();
                    break;
                case "prefab":
                    SetField(_controller, "_overlayPrefab", CreateOverlay());
                    Refresh();
                    break;
                case "runner":
                    SetField(_controller, "_runner", null);
                    Refresh();
                    break;
                case "camera":
                    _camera.enabled = false;
                    Refresh();
                    break;
            }

            Assert.That(ReadCombatEntries().Count, Is.Zero);
            Assert.That(active == null, Is.True);
            Assert.That(pending == null, Is.True);
        }

        [Test]
        public void RunnerSubscriptionRemainsSingleAcrossDisableAndReenable()
        {
            Refresh();
            Refresh();
            BattleRunner runner = ReadField<BattleRunner>(_controller, "_runner");
            Assert.That(ReadField<System.Action>(runner, "PresentationStopped").GetInvocationList().Length,
                Is.EqualTo(1));
            _controller.enabled = false;
            Invoke(_controller, "OnDisable");
            Assert.That(ReadField<System.Action>(runner, "PresentationStopped"), Is.Null);
            _controller.enabled = true;
            Invoke(_controller, "OnEnable");
            Refresh();
            Assert.That(ReadField<System.Action>(runner, "PresentationStopped").GetInvocationList().Length,
                Is.EqualTo(1));
            System.Action<BattleDamageEvent, Vector3> callback =
                ReadField<System.Action<BattleDamageEvent, Vector3>>(runner, "DamageResolved");
            BattleDamageEvent damage = (BattleDamageEvent)System.Activator.CreateInstance(typeof(BattleDamageEvent),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { BattleDamageOrigin.Attack, default(RaggedBanner.HexGrid.HexCoordinate), CompositeResult() }, null);
            callback.Invoke(damage, _headAnchor.position);
            Assert.That(ReadCombatEntries().Count, Is.EqualTo(1));
        }

        [Test]
        public void OffscreenAndUnavailableCameraDoNotAccumulatePendingText()
        {
            Refresh();
            _characterView.transform.position = new Vector3(0f, 0f, -10f);
            ReceiveResult(CompositeResult());
            Assert.That(ReadCombatEntries().Count, Is.Zero);
            _camera.enabled = false;
            ReceiveResult(CompositeResult());
            Assert.That(ReadCombatEntries().Count, Is.Zero);
        }

        [Test]
        public void InspectorChangesApplyOnlyToSubsequentAttackGroups()
        {
            Refresh();
            ReceiveResult(CompositeResult());
            ReceiveResult(CompositeResult());
            UpdateCombatTexts(10f);
            object active = ReadCombatEntries()[0];
            object pending = ReadCombatEntries()[1];
            Vector2 originalOrigin = ReadField<Vector2>(active, "_origin");
            SetField(_controller, "_textOffset", new Vector2(10f, -20f));
            SetField(_controller, "_spreadDuration", 0.5f);
            SetField(_controller, "_spreadDistance", 50f);
            SetField(_controller, "_riseDistance", 8f);
            SetField(_controller, "_displayDuration", 1.2f);
            SetField(_controller, "_fadeDuration", 0.3f);
            ReceiveResult(CompositeResult());
            object next = ReadCombatEntries()[2];
            UpdateCombatTexts(10.3f);
            Assert.That(ReadField<Vector2>(active, "_origin"), Is.EqualTo(originalOrigin));
            Assert.That(ReadField<float>(active, "_spreadDuration"), Is.EqualTo(0.2f));
            Assert.That(ReadField<float>(pending, "_displayDuration"), Is.EqualTo(0.8f));
            Assert.That(ReadField<Vector2>(next, "_textOffset"), Is.EqualTo(new Vector2(10f, -20f)));
            Assert.That(ReadField<float>(next, "_spreadDuration"), Is.EqualTo(0.5f));
            Assert.That(ReadField<float>(next, "_spreadDistance"), Is.EqualTo(50f));
            Assert.That(ReadField<float>(next, "_riseDistance"), Is.EqualTo(8f));
            Assert.That(ReadField<float>(next, "_displayDuration"), Is.EqualTo(1.2f));
            Assert.That(ReadField<float>(next, "_fadeDuration"), Is.EqualTo(0.3f));
        }

        [Test]
        public void InvalidInspectorValuesAreNormalizedBeforeTheyReachAnAttack()
        {
            Refresh();
            SetField(_controller, "_textOffset", new Vector2(float.NaN, float.PositiveInfinity));
            SetField(_controller, "_spreadDuration", 0f);
            SetField(_controller, "_spreadDistance", -1f);
            SetField(_controller, "_riseDistance", float.NaN);
            SetField(_controller, "_displayDuration", -1f);
            SetField(_controller, "_fadeDuration", 10f);
            ReceiveResult(CompositeResult());
            object entry = ReadCombatEntry();
            Assert.That(ReadField<Vector2>(entry, "_textOffset"), Is.EqualTo(Vector2.zero));
            Assert.That(ReadField<float>(entry, "_spreadDuration"), Is.EqualTo(0.01f));
            Assert.That(ReadField<float>(entry, "_spreadDistance"), Is.Zero);
            Assert.That(ReadField<float>(entry, "_riseDistance"), Is.EqualTo(8f));
            Assert.That(ReadField<float>(entry, "_displayDuration"), Is.EqualTo(0.02f));
            Assert.That(ReadField<float>(entry, "_fadeDuration"), Is.EqualTo(0.02f));
        }

        [Test]
        public void ZeroFadeKeepsTextVisibleUntilItsConfiguredLifetimeEnds()
        {
            Refresh();
            SetField(_controller, "_spreadDuration", 0.5f);
            SetField(_controller, "_displayDuration", 1.2f);
            SetField(_controller, "_fadeDuration", 0f);
            ReceiveResult(CompositeResult());
            UpdateCombatTexts(10f);
            UpdateCombatTexts(11.1f);
            Assert.That(ReadText(ReadItems(ReadCombatEntry())[0]).color.a, Is.EqualTo(1f));
            UpdateCombatTexts(11.21f);
            Assert.That(ReadCombatEntries().Count, Is.Zero);
        }

        BattleAttackResult CompositeResult()
        {
            return CreateResult(_character.Id, 4, 6, null,
                new BattleDamageBreakdown(water: 1, fire: 1, electricity: 1, poison: 1), false, false);
        }

        static BattleAttackResult CreateResult(int targetId, int damage, int remainingHealth,
            AttackAttribute? attribute, BattleDamageBreakdown breakdown, bool critical, bool negated)
        {
            ConstructorInfo constructor = typeof(BattleAttackResult).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(int), typeof(int), typeof(int), typeof(AttackAttribute?),
                    typeof(BattleDamageBreakdown), typeof(bool), typeof(bool) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (BattleAttackResult)constructor.Invoke(
                new object[] { targetId, damage, remainingHealth, attribute, breakdown, critical, negated });
        }

        static Rect TextBounds(Text text)
        {
            return new Rect(text.rectTransform.anchoredPosition - text.rectTransform.sizeDelta * 0.5f,
                text.rectTransform.sizeDelta);
        }

        static Text ReadText(object item)
        {
            return ReadField<Text>(item, "_text");
        }

        static IList ReadItems(object entry)
        {
            return ReadField<IList>(entry, "_items");
        }

        IList ReadCombatEntries()
        {
            return ReadField<IList>(ReadField<object>(_controller, "_textPresentation"), "_combatTexts");
        }

        object ReadCombatEntry()
        {
            Assert.That(ReadCombatEntries().Count, Is.GreaterThan(0));
            return ReadCombatEntries()[0];
        }

        void ReceiveResult(BattleAttackResult result)
        {
            Invoke(_controller, "ReceiveAttackResult", result, _characterView, _headAnchor.position);
        }

        void UpdateCombatTexts(float time)
        {
            Invoke(_controller, "UpdateCombatTexts", time, true, null);
        }
        void AssertProjectedPosition(HexCharacterOverlayView overlay)
        {
            Vector2 expected = _camera.WorldToScreenPoint(_headAnchor.position);
            Vector2 actual = RectTransformUtility.WorldToScreenPoint(_uiCamera, overlay.transform.position);
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(0.01f));
        }

        HexCharacterOverlayView CreateOverlay()
        {
            GameObject root = CreateObject("Overlay Prefab", typeof(RectTransform));
            HexCharacterOverlayView overlay = root.AddComponent<HexCharacterOverlayView>();
            RectTransform healthRoot = CreateChild("Health", root.transform);
            Image template = CreateChild("Template", healthRoot).gameObject.AddComponent<Image>();
            Image marker = CreateChild("Marker", root.transform).gameObject.AddComponent<Image>();
            _combatTextTemplate = CreateChild("Combat Text Template", root.transform).gameObject.AddComponent<Text>();
            _combatTextTemplate.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _combatTextTemplate.fontSize = 24;
            _combatTextTemplate.rectTransform.sizeDelta = new Vector2(200f, 80f);
            _combatTextTemplate.rectTransform.pivot = new Vector2(0.5f, 0f);
            _combatTextTemplate.color = new Color(1f, 0.8f, 0.2f, 1f);
            _combatTextTemplate.raycastTarget = true;
            _combatTextTemplate.gameObject.SetActive(false);
            SetField(overlay, "_healthRoot", healthRoot);
            SetField(overlay, "_healthSegmentTemplate", template);
            SetField(overlay, "_turnMarker", marker);
            SetField(overlay, "_combatTextTemplate", _combatTextTemplate);
            root.SetActive(false);
            return overlay;
        }

        HexCharacterOverlayView ReadOverlay()
        {
            return ReadField<Dictionary<int, HexCharacterOverlayView>>(_controller, "_overlays")[_character.Id];
        }

        void Refresh()
        {
            Invoke(_controller, "LateUpdate");
        }

        GameObject CreateObject(string name, params System.Type[] components)
        {
            GameObject gameObject = new GameObject(name, components);
            _objects.Add(gameObject);
            return gameObject;
        }

        static RectTransform CreateChild(string name, Transform parent)
        {
            RectTransform child = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            child.SetParent(parent, false);
            return child;
        }

        static T ReadField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        static void Invoke(object target, string name, params object[] arguments)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
        }
    }
}
