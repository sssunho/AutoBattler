using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using UnityEngine;
using UnityEngine.TestTools;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleMapRunnerTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _animators = new List<BattleAnimationFixture>();
        readonly List<BattleRunner> _runners = new List<BattleRunner>();

        HexMapView _mapView;
        HexCharacterView _prefab;

        [SetUp]
        public void SetUp()
        {
            _mapView = CreateObject("Map").AddComponent<HexMapView>();
            _prefab = CreatePrefab(true);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _runners.Count; i++)
            {
                if (_runners[i] != null)
                {
                    _runners[i].ShutdownBattle();
                }
            }

            _runners.Clear();

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();

            for (int i = 0; i < _animators.Count; i++)
            {
                _animators[i].Dispose();
            }

            _animators.Clear();
        }

        [Test]
        public void ExternalStartWaitsForItsExplicitInput()
        {
            BattleRunner runner = CreateRunner();

            InvokeStart(runner);

            Assert.That(runner.IsInitialized, Is.False);
            Assert.That(runner.MapView, Is.Null);
            Assert.That(runner.transform.childCount, Is.Zero);
        }

        [Test]
        public void ExternalInputsCarryTraitsAndSettingsWithoutClassDefaults()
        {
            BattleRunner runner = CreateRunner();
            BattleSpawnInput[] inputs = CreateInputs();
            TraitCollection traits = new TraitCollection(new[]
            {
                new Trait(TraitType.DirectionalPhysicalNegation,
                    new TraitSource(TraitSourceKind.Specialization, 2))
            });
            BattleTraitSettings settings = new BattleTraitSettings(0.9f, 0.4f, 0.05f);
            BattleSpawnInput first = inputs[0];
            inputs[0] = BattleTestUnitExtensions.CreateSpawnInput(first.Prefab, first.Team, first.Stats,
                first.Coordinate, first.Facing, traits, settings, skills: first.Skills);

            Assert.That(runner.TryInitializeBattle(_mapView, inputs, 187, out string error), Is.True, error);

            Assert.That(runner.TryGetCharacter(1, out HexCharacter player), Is.True);
            Assert.That(player.Traits, Is.SameAs(traits));
            Assert.That(player.TraitSettings, Is.SameAs(settings));
            Assert.That(player.Traits.Has(TraitType.RearGuaranteedCritical), Is.False);
            Assert.That(runner.TryGetCharacter(2, out HexCharacter enemy), Is.True);
            Assert.That(enemy.Traits.Has(TraitType.RearGuaranteedCritical), Is.False);
            Assert.That(enemy.Traits.Has(TraitType.DirectionalPhysicalNegation), Is.False);
        }

        [Test]
        public void MissingTraitSettingsRejectInputsBeforeCreatingViews()
        {
            BattleRunner runner = CreateRunner();
            BattleSpawnInput[] inputs = CreateInputs();
            BattleSpawnInput first = inputs[0];
            TraitCollection traits = new TraitCollection(new[]
            {
                new Trait(TraitType.DirectionalPhysicalNegation,
                    new TraitSource(TraitSourceKind.Specialization, 2))
            });
            inputs[0] = BattleTestUnitExtensions.CreateSpawnInput(first.Prefab, first.Team, first.Stats,
                first.Coordinate, first.Facing, traits, skills: first.Skills);

            Assert.That(runner.TryInitializeBattle(_mapView, inputs, 187, out string error), Is.False);
            Assert.That(error, Does.Contain("negation settings"));
            Assert.That(runner.IsInitialized, Is.False);
            Assert.That(runner.transform.childCount, Is.Zero);
        }

        [Test]
        public void ManualStartStillUsesItsSceneRegistrationsOnlyOnce()
        {
            BattleRunner runner = CreateRunner(BattleInitializationMode.Manual);
            HexCharacterView enemy = CreatePrefab(true);
            _prefab.transform.position = _mapView.AxialToWorld(new HexCoordinate(1, 1));
            enemy.transform.position = _mapView.AxialToWorld(new HexCoordinate(3, 2));
            SetField(runner, "_mapView", _mapView);
            SetField(runner, "_initialUnits", new[]
            {
                CreateRegistration(_prefab, BattleTeam.Player),
                CreateRegistration(enemy, BattleTeam.Enemy)
            });

            InvokeStart(runner);

            Assert.That(runner.State, Is.EqualTo(BattleState.Running));
            Assert.That(runner.TryGetCharacter(1, out HexCharacter player), Is.True);
            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(1, 1)));
            Assert.That(player.Stats.UnitClass, Is.EqualTo(MercenaryClass.Rogue));
            Assert.That(runner.TryGetUnitId(_prefab, out int playerId), Is.True);
            Assert.That(playerId, Is.EqualTo(1));

            InvokeStart(runner);

            Assert.That(runner.TryGetCharacter(1, out HexCharacter retained), Is.True);
            Assert.That(retained, Is.SameAs(player));
        }

        [Test]
        public void ExternalInputPreservesExactStatsCoordinatesFacingAndSeed()
        {
            _mapView.transform.SetPositionAndRotation(new Vector3(8f, 3f, -5f), Quaternion.Euler(12f, 42f, 5f));
            _prefab.transform.rotation = Quaternion.Euler(0f, 17f, 0f);
            BattleRunner firstRunner = CreateRunner();
            BattleRunner secondRunner = CreateRunner();
            BattleSpawnInput[] inputs = CreateInputs();

            Assert.That(firstRunner.TryInitializeBattle(_mapView, inputs, 187, out string firstError), Is.True, firstError);
            Assert.That(secondRunner.TryInitializeBattle(_mapView, inputs, 187, out string secondError), Is.True, secondError);
            Assert.That(_prefab.IsInitialized, Is.False);

            List<HexCharacterView> views = new List<HexCharacterView>();
            firstRunner.CopyRegisteredViews(views);
            Assert.That(views.Count, Is.EqualTo(inputs.Length));

            for (int i = 0; i < inputs.Length; i++)
            {
                Assert.That(firstRunner.TryGetCharacter(i + 1, out HexCharacter character), Is.True);
                Assert.That(character.Stats, Is.SameAs(inputs[i].Stats));
                Assert.That(character.Health, Is.EqualTo(inputs[i].Stats.MaxHealth));
                Assert.That(character.Coordinate, Is.EqualTo(inputs[i].Coordinate));
                Assert.That(character.Facing, Is.EqualTo(inputs[i].Facing));
                HexCharacterView view = views.Find(candidate => candidate.UnitId == character.Id);
                Assert.That(view, Is.Not.Null);
                Assert.That(view.transform.position, Is.EqualTo(_mapView.AxialToWorld(inputs[i].Coordinate)));
                Transform model = (Transform)GetField(view, "_model");
                Vector3 direction = _mapView.AxialToWorld(inputs[i].Coordinate.GetNeighbor(inputs[i].Facing))
                    - _mapView.AxialToWorld(inputs[i].Coordinate);
                Assert.That(Vector3.Angle(model.forward, direction), Is.LessThan(0.1f));
            }

            for (int i = 0; i < 12; i++)
            {
                Assert.That(firstRunner.CurrentUnitId, Is.EqualTo(secondRunner.CurrentUnitId));
                Assert.That(firstRunner.RoundNumber, Is.EqualTo(secondRunner.RoundNumber));
                Assert.That(firstRunner.TryEndTurn(firstRunner.CurrentUnitId.Value), Is.True);
                Assert.That(secondRunner.TryEndTurn(secondRunner.CurrentUnitId.Value), Is.True);
            }
        }

        [TestCase("overlap")]
        [TestCase("outside")]
        [TestCase("one-team")]
        [TestCase("player-limit")]
        public void InvalidFinalPlacementCreatesNoPartialBattle(string invalidCase)
        {
            BattleRunner runner = CreateRunner();
            BattleSpawnInput[] inputs = CreateInputs();

            switch (invalidCase)
            {
                case "overlap":
                {
                    inputs[1] = CreateInput(BattleTeam.Enemy, inputs[0].Coordinate);
                    break;
                }
                case "outside":
                {
                    inputs[1] = CreateInput(BattleTeam.Enemy, new HexCoordinate(_mapView.Width, 0));
                    break;
                }
                case "one-team":
                {
                    inputs[1] = CreateInput(BattleTeam.Player, inputs[1].Coordinate);
                    break;
                }
                case "player-limit":
                {
                    SetField(runner, "_maxPlayerUnits", 1);
                    inputs = new[] { inputs[0], inputs[1], CreateInput(BattleTeam.Player, new HexCoordinate(4, 4)) };
                    break;
                }
            }

            Assert.That(runner.TryInitializeBattle(_mapView, inputs, 1, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(runner.IsInitialized, Is.False);
            Assert.That(runner.MapView, Is.Null);
            Assert.That(runner.transform.childCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InvalidAnimationRollsBackEarlierViewsAndAllowsCleanRetry(bool missingAttack)
        {
            BattleRunner runner = CreateRunner();
            HexCharacterView invalidPrefab = missingAttack ? CreatePrefab(true, false) : CreatePrefab(false);
            BattleSpawnInput[] inputs = CreateInputs();
            inputs[1] = BattleTestUnitExtensions.CreateSpawnInput(invalidPrefab, BattleTeam.Enemy, inputs[1].Stats,
                inputs[1].Coordinate, inputs[1].Facing, skills: inputs[1].Skills);
            LogAssert.Expect(LogType.Error, missingAttack
                ? "HexCharacterView could not initialize its animation profile."
                : "HexCharacterView requires a model, Animator, animation profile, and a positive finite move speed.");

            Assert.That(runner.TryInitializeBattle(_mapView, inputs, 1, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(runner.IsInitialized, Is.False);
            Assert.That(runner.transform.childCount, Is.Zero);
            Assert.That(runner.MapView, Is.Null);
            List<HexCharacterView> views = new List<HexCharacterView>();
            runner.CopyRegisteredViews(views);
            Assert.That(views, Is.Empty);
            Assert.That(_prefab.IsInitialized, Is.False);
            Assert.That(invalidPrefab.IsInitialized, Is.False);

            Assert.That(runner.TryInitializeBattle(_mapView, CreateInputs(), 1, out error), Is.True, error);
            Assert.That(runner.TryGetCharacter(1, out HexCharacter first), Is.True);
            Assert.That(first.Health, Is.EqualTo(first.MaxHealth));
            Assert.That(runner.transform.childCount, Is.EqualTo(2));
        }

        [Test]
        public void ExistingBattleRejectsInitializationWithoutChangingItsState()
        {
            BattleRunner runner = CreateRunner();
            Assert.That(runner.TryInitializeBattle(_mapView, CreateInputs(), 1, out string error), Is.True, error);
            Assert.That(runner.TryGetCharacter(1, out HexCharacter original), Is.True);
            int? currentId = runner.CurrentUnitId;

            Assert.That(runner.TryInitializeBattle(null, null, 99, out error), Is.False);
            InvokeStart(runner);

            Assert.That(runner.TryGetCharacter(1, out HexCharacter retained), Is.True);
            Assert.That(retained, Is.SameAs(original));
            Assert.That(runner.MapView, Is.SameAs(_mapView));
            Assert.That(runner.CurrentUnitId, Is.EqualTo(currentId));
            Assert.That(runner.transform.childCount, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExternalInitializationRejectsManualModeOrSceneRegistrations(bool hasSceneRegistrations)
        {
            BattleRunner runner = CreateRunner(hasSceneRegistrations
                ? BattleInitializationMode.External : BattleInitializationMode.Manual);

            if (hasSceneRegistrations)
            {
                SetField(runner, "_initialUnits", new[] { CreateRegistration(_prefab, BattleTeam.Player) });
            }

            Assert.That(runner.TryInitializeBattle(_mapView, CreateInputs(), 1, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(runner.IsInitialized, Is.False);
            Assert.That(_prefab.IsInitialized, Is.False);
            Assert.That(runner.transform.childCount, Is.Zero);
        }

        [Test]
        public void ShutdownRejectsReentrantInitializationAndAllowsTheNextAttempt()
        {
            BattleRunner runner = CreateRunner();
            BattleSpawnInput[] inputs = CreateInputs();
            Assert.That(runner.TryInitializeBattle(_mapView, inputs, 1, out string error), Is.True, error);
            Assert.That(runner.TryGetCharacter(1, out HexCharacter original), Is.True);
            SetField(original, "_health", 1);
            bool? reentrantResult = null;
            System.Action handler = () => reentrantResult = runner.TryInitializeBattle(_mapView, inputs, 1, out _);
            runner.PresentationStopped += handler;

            runner.ShutdownBattle();
            runner.PresentationStopped -= handler;

            Assert.That(reentrantResult, Is.False);
            Assert.That(runner.IsInitialized, Is.False);
            Assert.That(runner.MapView, Is.Null);
            Assert.That(runner.transform.childCount, Is.Zero);
            Assert.That(runner.TryInitializeBattle(_mapView, inputs, 1, out error), Is.True, error);
            Assert.That(runner.TryGetCharacter(1, out HexCharacter replacement), Is.True);
            Assert.That(replacement, Is.Not.SameAs(original));
            Assert.That(replacement.Health, Is.EqualTo(replacement.MaxHealth));
        }

        [Test]
        public void SnapshotRulesIgnoreChangedInspectorRulesDuringValidationAndRestart()
        {
            BattleRunner runner = CreateRunner();
            SetField(runner, "_maxPlayerUnits", 2);
            SetField(runner, "_startingActionPoints", 6);
            SetField(runner, "_maxActionPoints", 9);
            SetField(runner, "_actionPointsPerTurn", 3);
            BattleSpawnInput[] inputs =
            {
                CreateInput(BattleTeam.Player, new HexCoordinate(1, 1)),
                CreateInput(BattleTeam.Player, new HexCoordinate(2, 1)),
                CreateInput(BattleTeam.Enemy, new HexCoordinate(3, 2))
            };
            Assert.That(runner.TryInitializeBattle(_mapView, inputs, 187, out string error), Is.True, error);
            BattleRules rules = runner.Rules;
            Assert.That(runner.TryGetCharacter(1, out HexCharacter original), Is.True);
            int? firstUnit = runner.CurrentUnitId;
            SetField(runner, "_maxPlayerUnits", 0);
            SetField(runner, "_startingActionPoints", 99);
            SetField(runner, "_maxActionPoints", -1);
            SetField(runner, "_actionPointsPerTurn", -1);

            Assert.That(runner.ValidateRestartInputs(_mapView, inputs, rules, out error), Is.True, error);
            Assert.That(runner.TryGetCharacter(1, out HexCharacter retained), Is.True);
            Assert.That(retained, Is.SameAs(original));
            runner.ShutdownBattle();
            Assert.That(runner.TryInitializeBattle(_mapView, inputs, rules, 187, out error), Is.True, error);

            Assert.That(runner.Rules, Is.SameAs(rules));
            Assert.That(runner.Rules.MaxPlayerUnits, Is.EqualTo(2));
            Assert.That(runner.Rules.StartingActionPoints, Is.EqualTo(6));
            Assert.That(runner.Rules.MaxActionPoints, Is.EqualTo(9));
            Assert.That(runner.Rules.ActionPointsPerTurn, Is.EqualTo(3));
            Assert.That(runner.CurrentUnitId, Is.EqualTo(firstUnit));
            Assert.That(runner.Result, Is.Null);
            Assert.That(runner.TryGetCharacter(1, out HexCharacter replacement), Is.True);
            Assert.That(replacement, Is.Not.SameAs(original));
            Assert.That(replacement.Health, Is.EqualTo(replacement.MaxHealth));
        }

        [TestCase("overlap")]
        [TestCase("disabled-prefab")]
        [TestCase("null-rules")]
        [TestCase("snapshot-limit")]
        public void FailedRestartPreflightLeavesCurrentBattleAndViewsUntouched(string invalidCase)
        {
            BattleRunner runner = CreateRunner();
            BattleSpawnInput[] inputs = CreateInputs();
            Assert.That(runner.TryInitializeBattle(_mapView, inputs, 1, out string error), Is.True, error);
            Assert.That(runner.TryGetCharacter(1, out HexCharacter original), Is.True);
            int? currentId = runner.CurrentUnitId;
            BattleRules rules = runner.Rules;

            switch (invalidCase)
            {
                case "overlap":
                {
                    inputs[1] = CreateInput(BattleTeam.Enemy, inputs[0].Coordinate);
                    break;
                }
                case "disabled-prefab":
                {
                    _prefab.enabled = false;
                    break;
                }
                case "null-rules":
                {
                    rules = null;
                    break;
                }
                case "snapshot-limit":
                {
                    rules = new BattleRules(maxPlayerUnits: 1);
                    inputs = new[] { inputs[0], inputs[1], CreateInput(BattleTeam.Player, new HexCoordinate(4, 4)) };
                    break;
                }
            }

            Assert.That(runner.ValidateRestartInputs(_mapView, inputs, rules, out error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(runner.TryGetCharacter(1, out HexCharacter retained), Is.True);
            Assert.That(retained, Is.SameAs(original));
            Assert.That(runner.CurrentUnitId, Is.EqualTo(currentId));
            Assert.That(runner.State, Is.EqualTo(BattleState.Running));
            Assert.That(runner.transform.childCount, Is.EqualTo(2));
        }

        [Test]
        public void CompletionCallbackCanReplaceTheBattleWithoutAdvancingTheReplacement()
        {
            BattleRunner runner = CreateRunner();
            BattleSpawnInput[] inputs = CreateInputs();
            Assert.That(runner.TryInitializeBattle(_mapView, inputs, 187, out string error), Is.True, error);
            BattleRules rules = runner.Rules;
            int? initialUnitId = runner.CurrentUnitId;
            int notifications = 0;
            runner.BattleCompleted += result =>
            {
                notifications++;
                runner.ShutdownBattle();
                Assert.That(runner.TryInitializeBattle(_mapView, inputs, rules, 187, out string restartError),
                    Is.True, restartError);
            };
            Assert.That(runner.RemoveCharacter(2), Is.True);

            typeof(BattleRunner).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(runner, null);

            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(runner.State, Is.EqualTo(BattleState.Running));
            Assert.That(runner.Result, Is.Null);
            Assert.That(runner.CurrentUnitId, Is.EqualTo(initialUnitId));
            Assert.That(runner.IsBusy, Is.False);
            Assert.That(runner.transform.childCount, Is.EqualTo(2));
        }

        BattleSpawnInput[] CreateInputs()
        {
            return new[]
            {
                CreateInput(BattleTeam.Player, new HexCoordinate(1, 1)),
                CreateInput(BattleTeam.Enemy, new HexCoordinate(3, 2))
            };
        }

        BattleSpawnInput CreateInput(BattleTeam team, HexCoordinate coordinate)
        {
            return BattleTestUnitExtensions.CreateSpawnInput(_prefab, team,
                new BattleTestUnit(MercenaryClass.Rogue, 27, 6, 5, 2, 41), coordinate, HexDirection.SouthWest);
        }

        BattleRunner CreateRunner(BattleInitializationMode mode = BattleInitializationMode.External)
        {
            BattleRunner runner = CreateObject("Runner").AddComponent<BattleRunner>();
            SetField(runner, "_initializationMode", mode);
            _runners.Add(runner);
            return runner;
        }

        HexCharacterView CreatePrefab(bool withAnimator, bool withAttack = true)
        {
            HexCharacterView view = CreateObject("Unit Source").AddComponent<HexCharacterView>();
            Transform model = CreateObject("Model").transform;
            model.SetParent(view.transform, false);
            SetField(view, "_model", model);

            if (withAnimator)
            {
                BattleAnimationFixture fixture = new BattleAnimationFixture(model.gameObject, withAttack);
                _animators.Add(fixture);
                fixture.Attach(view);
            }

            return view;
        }

        static BattleUnitRegistration CreateRegistration(HexCharacterView view, BattleTeam team)
        {
            BattleUnitRegistration registration = new BattleUnitRegistration();
            SetField(registration, "_view", view);
            SetField(registration, "_team", team);
            SetField(registration, "_unitClass", MercenaryClass.Rogue);
            return registration;
        }

        GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        static void InvokeStart(BattleRunner runner)
        {
            typeof(BattleRunner).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(runner, null);
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
