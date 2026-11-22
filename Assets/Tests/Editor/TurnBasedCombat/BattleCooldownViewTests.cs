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
    public sealed class BattleCooldownViewTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        HexMapView _map;
        BattleRunner _runner;
        BattleInputController _input;
        BattleSimulation _simulation;
        HexCharacter _actor;
        HexCharacter _enemy;
        HexCharacterView _prefab;

        [SetUp]
        public void SetUp()
        {
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            SetField(_runner, "_initializationMode", BattleInitializationMode.External);
            Camera camera = CreateObject("Camera").AddComponent<Camera>();
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            SetField(_input, "_inputCamera", camera);
            _simulation = new BattleSimulation(_map.Width, _map.Height, new BattleRules(startingActionPoints: 8), 7);
            SkillSet skills = new SkillSet(new[]
            {
                new BattleSkillDefinition(SkillType.BasicAttack, SkillTargetType.Unit, SkillRangeSource.Own, 1, 8, 2, damage: 3),
                new BattleSkillDefinition(SkillType.SpiritImbuement, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 0, cooldownTurns: 2)
            }, new[] { 0, 3 });
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 2),
                new BattleUnitStats(MercenaryClass.Archer, 20, 5, 0, initiative: 20), HexDirection.East,
                out _actor, skills: skills), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(5, 2),
                new BattleTestUnit(MercenaryClass.Warrior, 20, 2, 3, 1, 0), HexDirection.East, out _enemy), Is.True);
            Assert.That(_simulation.TryStartBattle(), Is.True);
            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            _prefab = BattleTestViews.CreateCharacterView("Template", _objects, _animations);
            CreateView(_actor);
            CreateView(_enemy);
        }

        [TearDown]
        public void TearDown()
        {
            if (_input != null)
            {
                _input.CancelActionSelection();
            }
            if (_runner != null)
            {
                _runner.ShutdownBattle();
            }
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }
            _objects.Clear();
            BattleTestViews.DisposeFixtures(_animations);
        }

        [Test]
        public void InputRejectsCoolingSkillUntilSecondOwnerTurn()
        {
            Assert.That(_input.TryBeginSpiritImbuementSelection(), Is.True);
            Assert.That(_input.TryConfirmChoice(1), Is.True);
            Assert.That(_actor.ActionPoints, Is.EqualTo(8));
            Assert.That(_input.TryBeginSpiritImbuementSelection(), Is.False);
            AdvanceOwnerTurn();
            Assert.That(_actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.EqualTo(1));
            Assert.That(_input.TryBeginSpiritImbuementSelection(), Is.False);
            AdvanceOwnerTurn();
            Assert.That(_actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.Zero);
            Assert.That(_input.TryBeginSpiritImbuementSelection(), Is.True);
            _input.CancelActionSelection();
            Assert.That(_actor.PreparedSpirit, Is.EqualTo(SpiritType.Fire));
            Assert.That(_actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.Zero);
        }

        [Test]
        public void StatusRefreshesWhenOnlyCooldownChangesAfterZeroCostSameSpiritUse()
        {
            Assert.That(_simulation.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement, _actor.Id,
                spirit: SpiritType.Fire)), Is.True);
            AdvanceOwnerTurn();
            AdvanceOwnerTurn();
            Text text = CreateObject("Status", typeof(RectTransform)).AddComponent<Text>();
            BattleStatusView status = CreateObject("Status view").AddComponent<BattleStatusView>();
            SetField(status, "_runner", _runner);
            SetField(status, "_statusText", text);
            Invoke(status, "Refresh");
            Assert.That(text.text, Does.Contain("준비 정령: 불"));
            Assert.That(text.text, Does.Not.Contain("쿨타임"));
            int ap = _actor.ActionPoints;
            int round = _simulation.RoundNumber;
            Assert.That(_simulation.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement, _actor.Id,
                spirit: SpiritType.Fire)), Is.True);
            Assert.That(_actor.ActionPoints, Is.EqualTo(ap));
            Assert.That(_simulation.RoundNumber, Is.EqualTo(round));
            Assert.That(_actor.PreparedSpirit, Is.EqualTo(SpiritType.Fire));
            Invoke(status, "Refresh");
            Assert.That(text.text, Does.Contain("3 정령 부여 (쿨타임 2)"));
        }

        [Test]
        public void ReinitializationFromRestartSnapshotPreservesDefinitionButClearsRemainingCooldown()
        {
            SkillSet originalSkills = _actor.Skills;
            BattleSpawnInput[] inputs =
            {
                new BattleSpawnInput(_prefab, _actor.Team, _actor.Stats, _actor.Coordinate, _actor.Facing, skills: originalSkills),
                new BattleSpawnInput(_prefab, _enemy.Team, _enemy.Stats, _enemy.Coordinate, _enemy.Facing, skills: _enemy.Skills)
            };
            BattleRestartSnapshot snapshot = new BattleRestartSnapshot(_map, inputs, _simulation.Rules, 7, 0);
            Assert.That(_simulation.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement, _actor.Id,
                spirit: SpiritType.Fire)), Is.True);
            Assert.That(_actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.EqualTo(2));
            HexCharacter original = _actor;
            _runner.ShutdownBattle();
            Assert.That(_runner.TryInitializeBattle(snapshot.MapView, snapshot.Inputs, snapshot.Rules,
                snapshot.Seed, out string error), Is.True, error);
            Assert.That(_runner.TryGetCharacter(1, out HexCharacter restored), Is.True);
            Assert.That(restored, Is.Not.SameAs(original));
            Assert.That(restored.Skills, Is.SameAs(originalSkills));
            Assert.That(restored.Skills.Get(SkillType.SpiritImbuement).CooldownTurns, Is.EqualTo(2));
            Assert.That(restored.GetRemainingCooldown(SkillType.SpiritImbuement), Is.Zero);
            Assert.That(restored.PreparedSpirit, Is.EqualTo(SpiritType.None));
        }

        void AdvanceOwnerTurn()
        {
            Assert.That(_simulation.TryEndTurn(_actor.Id), Is.True);
            Assert.That(_simulation.TryEndTurn(_enemy.Id), Is.True);
            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_actor.Id));
        }

        void CreateView(HexCharacter character)
        {
            GameObject instance = Object.Instantiate(_prefab.gameObject);
            _objects.Add(instance);
            HexCharacterView view = instance.GetComponent<HexCharacterView>();
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