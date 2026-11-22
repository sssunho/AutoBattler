using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using UnityEngine;
using UnityEngine.UI;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleSpiritInputTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        HexMapView _map;
        Camera _camera;
        BattleRunner _runner;
        BattleInputController _input;
        BattleSimulation _simulation;
        HexCharacter _archer;
        HexCharacter _enemy;
        BattleRangeController _ranges;
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        Material _material;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _camera = CreateObject("Camera").AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 12f;
            _camera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            _camera.transform.position = new Vector3(0f, 10f, 0f);
            _camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            SetField(_input, "_inputCamera", _camera);
            SetField(_input, "_characterLayerMask", (LayerMask)0);
            _simulation = new BattleSimulation(_map.Width, _map.Height, new BattleRules(startingActionPoints: 6), 7);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 2),
                new BattleUnitStats(MercenaryClass.Archer, 20, 5, 0, initiative: 20), HexDirection.East,
                out _archer, skills: new SkillSet(new[]
                {
                    new BattleSkillDefinition(SkillType.BasicAttack, SkillTargetType.Unit, SkillRangeSource.Own, 1, 8, 2, damage: 3),
                    new BattleSkillDefinition(SkillType.SpiritImbuement, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 0)
                }, new[] { 0, 3 })), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(5, 2),
                new BattleTestUnit(MercenaryClass.Warrior, 20, 2, 3, 1, 0), HexDirection.East, out _enemy), Is.True);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            CreateView(_archer);
            CreateView(_enemy);
        }

        [TearDown]
        public void TearDown()
        {
            _input.CancelActionSelection();
            _runner.ShutdownBattle();

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            BattleTestViews.DisposeFixtures(_animations);

            if (_material != null)
            {
                Object.DestroyImmediate(_material);
            }
        }

        [Test]
        public void ChoiceModeUsesTheSharedOptionIdsAndRejectsUnknownOptions()
        {
            Assert.That(_input.TryBeginSkillSelection(SkillType.SpiritImbuement), Is.True);
            Assert.That(_input.SelectedInputMode, Is.EqualTo(SkillInputMode.Choice));
            Assert.That(_input.SelectedSkillChoices, Is.SameAs(BattleSkillChoices.Get(SkillType.SpiritImbuement)));
            Assert.That(_input.TryConfirmChoice(99), Is.False);
            Assert.That(_input.IsSelectingAction, Is.True);
            Assert.That(_input.TryConfirmChoice(_input.SelectedSkillChoices[0].Id), Is.True);
            Assert.That(_input.IsSelectingAction, Is.False);
        }

        [Test]
        public void SpiritSelectionOverwritesWithoutCostAndCancellationPreservesPreparation()
        {
            Assert.That(_input.TryBeginSpiritImbuementSelection(), Is.True);
            Assert.That(_runner.TryEndTurn(_archer.Id), Is.False);
            Assert.That(_input.TryConfirmChoice(1), Is.True);
            Assert.That(_archer.PreparedSpirit, Is.EqualTo(SpiritType.Fire));
            Assert.That(_archer.ActionPoints, Is.EqualTo(6));
            Assert.That(_input.IsSelectingAction, Is.False);
            Assert.That(_input.TryBeginSpiritImbuementSelection(), Is.True);
            _input.CancelActionSelection();
            Assert.That(_archer.PreparedSpirit, Is.EqualTo(SpiritType.Fire));
            Assert.That(_input.TryBeginSpiritImbuementSelection(), Is.True);
            Assert.That(_input.TryConfirmChoice(2), Is.True);
            Assert.That(_archer.PreparedSpirit, Is.EqualTo(SpiritType.Water));
            Assert.That(_archer.ActionPoints, Is.EqualTo(6));
        }

        [Test]
        public void SpiritPreviewUsesTheSharedStraightLineAndDisappearsOnSelection()
        {
            Assert.That(_input.TryBeginSpiritImbuementSelection(), Is.True);
            Assert.That(_input.TryConfirmChoice(1), Is.True);
            Assert.That(_input.TryUpdateMovementPreview(Screen(_enemy.Coordinate)), Is.True);
            List<HexCoordinate> path = new List<HexCoordinate>();
            List<HexCoordinate> expected = new List<HexCoordinate>();
            BattleSpiritPath.Copy(_archer.Coordinate, _enemy.Coordinate, expected);
            Assert.That(_input.TryCopySpiritPreview(_runner, path), Is.True);
            Assert.That(path, Is.EqualTo(expected));
            CreateRanges();
            Invoke(_ranges, "LateUpdate");
            BattleRangeMeshView view = ((BattleRangePresentationLayer)GetField(_ranges, "_spiritPathLayer")).View;
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(_input.TryBeginSpiritImbuementSelection(), Is.True);
            Invoke(_ranges, "LateUpdate");
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
        }

        [Test]
        public void StatusRefreshesAfterZeroCostOverwrite()
        {
            Text text = CreateObject("Status", typeof(RectTransform)).AddComponent<Text>();
            BattleStatusView status = CreateObject("Status view").AddComponent<BattleStatusView>();
            SetField(status, "_runner", _runner);
            SetField(status, "_statusText", text);
            Assert.That(_input.TryBeginSpiritImbuementSelection(), Is.True);
            Invoke(status, "Refresh");
            Assert.That(text.text, Does.Contain("정령 부여 선택"));
            Assert.That(_input.TryConfirmChoice(1), Is.True);
            Invoke(status, "Refresh");
            Assert.That(text.text, Does.Contain("준비 정령: 불"));
            Assert.That(_input.TryBeginSpiritImbuementSelection(), Is.True);
            Assert.That(_input.TryConfirmChoice(4), Is.True);
            Invoke(status, "Refresh");
            Assert.That(text.text, Does.Contain("준비 정령: 독"));
        }
        void CreateRanges()
        {
            _material = BattleTestViews.CreateRangeMaterial();
            _ranges = CreateObject("Ranges").AddComponent<BattleRangeController>();
            SetField(_ranges, "_runner", _runner);
            SetField(_ranges, "_input", _input);
            SetField(_ranges, "_material", _material);
        }

        Vector2 Screen(HexCoordinate coordinate)
        {
            Vector3 screen = _camera.WorldToScreenPoint(_map.AxialToWorld(coordinate));
            return new Vector2(screen.x, screen.y);
        }

        void CreateView(HexCharacter character)
        {
            HexCharacterView view = BattleTestViews.CreateCharacterView("Unit " + character.Id, _objects, _animations);
            Assert.That(view.TryBind(character, _map), Is.True);
            ((Dictionary<int, HexCharacterView>)GetField(_runner, "_views")).Add(character.Id, view);
        }

        GameObject CreateObject(string name, params System.Type[] components)
        {
            GameObject instance = new GameObject(name, components);
            _objects.Add(instance);
            return instance;
        }

        static object Invoke(object target, string name)
        {
            return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }

        static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
