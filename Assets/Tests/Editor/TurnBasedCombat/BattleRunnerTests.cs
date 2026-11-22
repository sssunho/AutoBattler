using System.Collections.Generic;
using System.Reflection;
using RaggedBanner.Animations;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using UnityEngine;
using UnityEngine.TestTools;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleRunnerTests
    {
        readonly List<Object> _objects = new List<Object>();
        readonly List<BattleAnimationFixture> _animatorFixtures = new List<BattleAnimationFixture>();

        BattleRunner _runner;
        BattleSimulation _simulation;
        HexMapView _mapView;
        HexCharacterView _firstView;
        HexCharacter _first;
        HexCharacter _second;

        [SetUp]
        public void SetUp()
        {
            _mapView = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _simulation = new BattleSimulation(_mapView.Width, _mapView.Height,
                randomSeed: 0);
            _simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUtility.CreateStats(movementPerTurn: 8), HexDirection.East, out _first);
            _simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(0, 1),
                BattleTestUtility.CreateStats(movementPerTurn: 8), HexDirection.East, out _second);
            Assert.That(_simulation.TryStartBattle(), Is.True);

            if (_simulation.CurrentUnitId != _first.Id)
            {
                Assert.That(_simulation.TryEndTurn(_second.Id), Is.True);
            }

            Assert.That(_simulation.CurrentUnitId, Is.EqualTo(_first.Id));
            _firstView = CreateView(_first);
            HexCharacterView secondView = CreateView(_second);
            AddAnimator(secondView);

            SetField(_runner, "_simulation", _simulation);
            SetField(_runner, "_mapView", _mapView);
            SetField(_runner, "_hexSize", _mapView.HexSize);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            views.Add(_first.Id, _firstView);
            views.Add(_second.Id, secondView);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();

            for (int i = 0; i < _animatorFixtures.Count; i++)
            {
                _animatorFixtures[i].Dispose();
            }

            _animatorFixtures.Clear();
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void DisabledViewKeepsUnitOccupancyAndTurn(bool disableGameObject, bool runnerOwned)
        {
            if (runnerOwned)
            {
                ((HashSet<int>)GetField(_runner, "_spawnedViewIds")).Add(_first.Id);
            }

            if (disableGameObject)
            {
                _firstView.gameObject.SetActive(false);
            }
            else
            {
                _firstView.enabled = false;
            }

            TickRunner();

            Assert.That(_runner.TryGetCharacter(_first.Id, out HexCharacter retained), Is.True);
            Assert.That(retained, Is.SameAs(_first));
            Assert.That(_simulation.Board.IsOccupied(_first.Coordinate), Is.True);
            Assert.That(_runner.CurrentUnitId, Is.EqualTo(_first.Id));
            Assert.That(_firstView.UnitId, Is.EqualTo(_first.Id));
            Assert.That(_firstView.gameObject.activeSelf, Is.EqualTo(!disableGameObject));

            _firstView.gameObject.SetActive(true);
            _firstView.enabled = true;
            TickRunner();

            Assert.That(_firstView.IsInitialized, Is.True);
            Assert.That(_runner.TryGetCharacter(_first.Id, out retained), Is.True);
            Assert.That(retained, Is.SameAs(_first));
        }

        [Test]
        public void MissingAnimatorCompletesCommittedMovementAndDropsRemainingPath()
        {
            Assert.That(_simulation.TryMove(_first.Id, new HexCoordinate(2, 0)), Is.True);
            Assert.That(_simulation.TryBeginMovementStep(out HexMovementStep step), Is.True);
            SetField(BattlePresentationTestUtility.GetDriver(_runner), "_step", step);
            SetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep", true);
            SetField(_firstView, "_isPresenting", true);
            _firstView.transform.position = Vector3.Lerp(
                _mapView.AxialToWorld(step.From), _mapView.AxialToWorld(step.To), 0.5f);

            TickRunner();

            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_simulation.IsWaitingForMovementCompletion, Is.False);
            Assert.That(_simulation.TryCompleteMovementStep(), Is.False);
            Assert.That(_simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(_first.MovementRemaining, Is.EqualTo(7));
            Assert.That(_runner.TryGetCharacter(_first.Id, out HexCharacter retained), Is.True);
            Assert.That(retained, Is.SameAs(_first));
            Assert.That(retained.Coordinate, Is.EqualTo(step.To));
            Assert.That(_simulation.Board.IsOccupied(step.To), Is.True);
            Assert.That(_firstView.transform.position, Is.EqualTo(_mapView.AxialToWorld(step.To)));
            Assert.That(_firstView.IsPresenting, Is.False);
            Assert.That(_firstView.IsInitialized, Is.True);
            Assert.That(_firstView.gameObject.activeSelf, Is.True);
            Assert.That(_runner.CurrentUnitId, Is.EqualTo(_first.Id));
        }

        [Test]
        public void DestroyedLastPlayerViewRemovesUnitAndCompletesWithDefeat()
        {
            Object.DestroyImmediate(_firstView.gameObject);

            TickRunner();

            Assert.That(_runner.TryGetCharacter(_first.Id, out _), Is.False);
            Assert.That(_simulation.Board.IsOccupied(_first.Coordinate), Is.False);
            Assert.That(_runner.CurrentUnitId, Is.Null);
            Assert.That(_runner.State, Is.EqualTo(BattleState.Completed));
            Assert.That(_runner.Result.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(_runner.TryGetCharacter(_second.Id, out _), Is.True);
        }

        [Test]
        public void ExplicitRemovalStillUnbindsAndDeactivatesSceneView()
        {
            Assert.That(_runner.RemoveCharacter(_first.Id), Is.True);
            Assert.That(_runner.TryGetCharacter(_first.Id, out _), Is.False);
            Assert.That(_simulation.Board.IsOccupied(_first.Coordinate), Is.False);
            Assert.That(_firstView.IsInitialized, Is.False);
            Assert.That(_firstView.gameObject.activeSelf, Is.False);
            Assert.That(_runner.CurrentUnitId, Is.Null);
            Assert.That(_runner.State, Is.EqualTo(BattleState.Waiting));
        }

        [Test]
        public void RemovingApproachTargetPreservesCommittedMovementUntilCompletion()
        {
            Assert.That(_simulation.TryMove(_first.Id, new HexCoordinate(3, 0)), Is.True);

            while (_simulation.TryBeginMovementStep(out _))
            {
                _simulation.TryCompleteMovementStep();
            }

            Assert.That(_simulation.TryAttack(_first.Id, _second.Id), Is.True);
            Assert.That(_simulation.TryBeginMovementStep(out HexMovementStep move), Is.True);
            SetField(BattlePresentationTestUtility.GetDriver(_runner), "_step", move);
            SetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep", true);
            SetField(_firstView, "_step", move);
            SetField(_firstView, "_isPresenting", true);
            Vector3 midpoint = Vector3.Lerp(
                _mapView.AxialToWorld(move.From), _mapView.AxialToWorld(move.To), 0.5f);
            _firstView.transform.position = midpoint;

            Assert.That(_runner.RemoveCharacter(_second.Id), Is.True);
            Assert.That(_runner.IsBusy, Is.True);
            Assert.That(_simulation.IsWaitingForMovementCompletion, Is.True);
            Assert.That(GetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep"), Is.True);
            Assert.That(_firstView.IsPresenting, Is.True);
            Assert.That(_firstView.transform.position, Is.EqualTo(midpoint));

            AdvanceMovement(100f);

            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(GetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep"), Is.False);
            Assert.That(_firstView.IsPresenting, Is.False);
            Assert.That(_firstView.transform.position, Is.EqualTo(_mapView.AxialToWorld(move.To)));
            Assert.That(_runner.TryGetCharacter(_first.Id, out _), Is.True);
            Assert.That(_simulation.Board.IsOccupied(move.To), Is.True);
            Assert.That(_simulation.TryCompleteMovementStep(), Is.False);
            Assert.That(_simulation.TryBeginAttack(out _), Is.False);
            Assert.That(_first.ActionPoints, Is.EqualTo(4));
        }

        [Test]
        public void RemovingLastEnemyDuringOrdinaryMovePreservesCommittedMovementUntilCompletion()
        {
            Assert.That(_simulation.TryMove(_first.Id, new HexCoordinate(2, 0)), Is.True);
            Assert.That(_simulation.TryBeginMovementStep(out HexMovementStep move), Is.True);
            SetField(BattlePresentationTestUtility.GetDriver(_runner), "_step", move);
            SetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep", true);
            SetField(_firstView, "_step", move);
            SetField(_firstView, "_isPresenting", true);

            Assert.That(_runner.RemoveCharacter(_second.Id), Is.True);
            Assert.That(_runner.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(_runner.IsBusy, Is.True);
            Assert.That(_simulation.IsWaitingForMovementCompletion, Is.True);
            Assert.That(GetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep"), Is.True);
            Assert.That(_firstView.IsPresenting, Is.True);

            AdvanceMovement(100f);

            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(GetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep"), Is.False);
            Assert.That(_firstView.IsPresenting, Is.False);
            Assert.That(_firstView.transform.position, Is.EqualTo(_mapView.AxialToWorld(move.To)));
            Assert.That(_first.MovementRemaining, Is.EqualTo(7));
        }

        [Test]
        public void CancelActionFinishesCurrentMovementWithoutSnappingOrFollowingRemainingPath()
        {
            Assert.That(_simulation.TryMove(_first.Id, new HexCoordinate(5, 0)), Is.True);
            Assert.That(_simulation.TryBeginMovementStep(out HexMovementStep step), Is.True);
            SetField(BattlePresentationTestUtility.GetDriver(_runner), "_step", step);
            SetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep", true);
            SetField(_firstView, "_step", step);
            SetField(_firstView, "_isPresenting", true);
            Vector3 midpoint = Vector3.Lerp(
                _mapView.AxialToWorld(step.From), _mapView.AxialToWorld(step.To), 0.5f);
            _firstView.transform.position = midpoint;

            _runner.CancelAction();
            _runner.CancelAction();

            Assert.That(_runner.IsBusy, Is.True);
            Assert.That(_simulation.IsWaitingForMovementCompletion, Is.True);
            Assert.That(GetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep"), Is.True);
            Assert.That(_firstView.IsPresenting, Is.True);
            Assert.That(_firstView.transform.position, Is.EqualTo(midpoint));
            Assert.That(_first.Coordinate, Is.EqualTo(step.To));
            Assert.That(_first.MovementRemaining, Is.EqualTo(7));
            Assert.That(_simulation.TryEndTurn(_first.Id), Is.False);
            Assert.That(_simulation.TryMove(_first.Id, new HexCoordinate(2, 0)), Is.False);

            AdvanceMovement(100f);

            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_simulation.IsWaitingForMovementCompletion, Is.False);
            Assert.That(GetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep"), Is.False);
            Assert.That(_firstView.IsPresenting, Is.False);
            Assert.That(_firstView.transform.position, Is.EqualTo(_mapView.AxialToWorld(step.To)));
            Assert.That(_first.MovementRemaining, Is.EqualTo(7));
            Assert.That(_simulation.TryBeginMovementStep(out _), Is.False);
            Assert.That(_simulation.TryCompleteMovementStep(), Is.False);
            Assert.That(_simulation.TryMove(_first.Id, new HexCoordinate(2, 0)), Is.True);
        }

        [Test]
        public void RunnerDisableCompletesCommittedMovementAndClearsPresentation()
        {
            Assert.That(_simulation.TryMove(_first.Id, new HexCoordinate(2, 0)), Is.True);
            Assert.That(_simulation.TryBeginMovementStep(out HexMovementStep step), Is.True);
            SetField(BattlePresentationTestUtility.GetDriver(_runner), "_step", step);
            SetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep", true);
            SetField(_firstView, "_step", step);
            SetField(_firstView, "_isPresenting", true);
            _firstView.transform.position = Vector3.Lerp(
                _mapView.AxialToWorld(step.From), _mapView.AxialToWorld(step.To), 0.5f);

            typeof(BattleRunner).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_runner, null);

            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_simulation.IsWaitingForMovementCompletion, Is.False);
            Assert.That(GetField(BattlePresentationTestUtility.GetDriver(_runner), "_hasStep"), Is.False);
            Assert.That(_firstView.IsPresenting, Is.False);
            Assert.That(_firstView.transform.position, Is.EqualTo(_mapView.AxialToWorld(step.To)));
            Assert.That(_first.Coordinate, Is.EqualTo(step.To));
            Assert.That(_first.MovementRemaining, Is.EqualTo(7));
            Assert.That(_simulation.TryBeginMovementStep(out _), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FatalHitReleasesTargetViewAfterApplyingDamageOnce(bool disableTarget)
        {
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            HexCharacterView targetView = views[_second.Id];
            AddAnimator(_firstView);
            SetField(_second, "_health", _first.Skills.BasicAttack.Damage);

            if (disableTarget)
            {
                DisableView(targetView);
            }

            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);
            UpdateCurrentAction(0.1f);
            UpdateCurrentAction(0.6f);

            Assert.That(_second.Health, Is.Zero);
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
            Assert.That(_runner.TryGetCharacter(_second.Id, out _), Is.False);
            Assert.That(_runner.TryGetUnitId(targetView, out _), Is.False);
            Assert.That(_simulation.Board.IsOccupied(_second.Coordinate), Is.False);
            Assert.That(targetView.IsInitialized, Is.False);
            Assert.That(targetView.gameObject.activeSelf, Is.False);
            Assert.That(_firstView.IsPresenting, Is.True);
            UpdateCurrentAction(0.5f);
            Assert.That(_second.Health, Is.Zero);
            Assert.That(_firstView.IsPresenting, Is.False);
            Assert.That(_firstView.IsInitialized, Is.True);
            Assert.That(_runner.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(_simulation.TryCompleteAttack(out _), Is.False);
            int round = _runner.RoundNumber;
            TickRunner();
            Assert.That(_runner.RoundNumber, Is.EqualTo(round));
        }

        [Test]
        public void UnavailableAttackPresentationAbortsWithoutDamage()
        {
            Assert.That(_simulation.TryAttack(_first.Id, _second.Id), Is.True);
            Assert.That(_simulation.TryBeginAttack(out _), Is.True);
            SetField(_firstView, "_isAttacking", true);

            TickRunner();

            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
            Assert.That(_second.Health, Is.EqualTo(10));
            Assert.That(_runner.TryGetCharacter(_second.Id, out _), Is.True);
            Assert.That(_firstView.IsPresenting, Is.False);
            Assert.That(_simulation.TryCompleteAttack(out _), Is.False);
            TickRunner();
            Assert.That(_second.Health, Is.EqualTo(10));
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
        }

        [Test]
        public void MissingAnimatorAbortsIssuedAttackWithoutDamage()
        {
            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);

            UpdateCurrentAction(0.1f);

            AssertAttackAbortedWithoutDamage();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisabledAttackerBeforeCommandAbortsAndDisabledTargetStillTakesHit(bool disableTarget)
        {
            HexCharacterView targetView = GetTargetView();
            AddAnimator(_firstView);
            HexCharacterView disabledView = disableTarget ? targetView : _firstView;
            DisableView(disabledView);
            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);

            UpdateCurrentAction(0.1f);

            if (disableTarget)
            {
                AssertAttackWaitsForHitAndCompletion();
                AssertAttackCompletedOnce();
            }
            else
            {
                AssertAttackAbortedWithoutDamage();
            }

            Assert.That(_runner.TryGetUnitId(disabledView, out _), Is.True);
            EnableView(disabledView);
            Assert.That(disabledView.transform.position,
                Is.EqualTo(_mapView.AxialToWorld(disableTarget ? _second.Coordinate : _first.Coordinate)));
            Assert.That(disabledView.IsPresenting, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisableAndReenableBeforeAttackPresentationKeepsAttack(bool disableTarget)
        {
            AddAnimator(_firstView);
            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);
            HexCharacterView disabledView = disableTarget ? GetTargetView() : _firstView;
            DisableView(disabledView);
            EnableView(disabledView);

            UpdateCurrentAction(0.1f);

            AssertAttackWaitsForHitAndCompletion();
            AssertAttackCompletedOnce();
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void DisablingAttackerBeforeHitAbortsButDisablingTargetKeepsAttack(bool disableTarget, bool reenable)
        {
            AddAnimator(_firstView);
            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);
            UpdateCurrentAction(0.1f);
            Assert.That(_runner.IsBusy, Is.True);
            Assert.That(_second.Health, Is.EqualTo(10));
            HexCharacterView disabledView = disableTarget ? GetTargetView() : _firstView;
            DisableView(disabledView);

            if (reenable)
            {
                EnableView(disabledView);
            }

            UpdateCurrentAction(0f);

            if (disableTarget)
            {
                AssertAttackWaitsForHitAndCompletion();
                AssertAttackCompletedOnce();
            }
            else
            {
                AssertAttackAbortedWithoutDamage();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisabledViewDuringAttackApproachPreservesRemainingPath(bool disableTarget)
        {
            Assert.That(_simulation.TryMove(_first.Id, new HexCoordinate(3, 0)), Is.True);

            while (_simulation.TryBeginMovementStep(out _))
            {
                Assert.That(_simulation.TryCompleteMovementStep(), Is.True);
            }

            _firstView.transform.position = _mapView.AxialToWorld(_first.Coordinate);
            AddAnimator(_firstView);
            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);
            UpdateCurrentAction(0.01f);
            Assert.That(_simulation.IsWaitingForMovementCompletion, Is.True);
            DisableView(disableTarget ? GetTargetView() : _firstView);

            UpdateCurrentAction(100f);

            if (disableTarget)
            {
                AssertAttackWaitsForHitAndCompletion();
                AssertAttackCompletedOnce();
            }
            else
            {
                AssertAttackAbortedWithoutDamage();
            }

            Assert.That(_first.Coordinate.DistanceTo(_second.Coordinate), Is.EqualTo(1));
            Assert.That(_first.MovementRemaining, Is.EqualTo(3));
            EnableView(disableTarget ? GetTargetView() : _firstView);
            Assert.That(_firstView.transform.position, Is.EqualTo(_mapView.AxialToWorld(_first.Coordinate)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnemyRogueReachesSelectedRearBeforeAttacking(bool disableDuringMovement)
        {
            HexCharacterView enemyView = CreateEnemyTurn(
                new BattleTestUnit(MercenaryClass.Rogue, 10, 3, 4, 1, 0),
                new HexCoordinate(3, 2), new TraitCollection(new[]
                {
                    new Trait(TraitType.RearGuaranteedCritical,
                        new TraitSource(TraitSourceKind.Class, (int)MercenaryClass.Rogue))
                }));
            AddAnimator(enemyView);
            int resultCount = 0;
            BattleAttackResult completedResult = default;
            SubscribeRunnerEvent("AttackCompleted", new System.Action<BattleAttackResult, HexCharacterView, Vector3>(
                (result, view, position) =>
                {
                    resultCount++;
                    completedResult = result;
                }));
            Assert.That(_second.Coordinate.DistanceTo(_first.Coordinate), Is.EqualTo(1));

            TickRunner();

            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Moving));
            Assert.That(_simulation.TargetUnitId, Is.EqualTo(_first.Id));
            Assert.That(_second.Coordinate, Is.EqualTo(new HexCoordinate(3, 2)));
            Assert.That(_second.ActionPoints, Is.EqualTo(4));
            UpdateCurrentAction(0.01f);
            Assert.That(_simulation.IsWaitingForMovementCompletion, Is.True);
            Assert.That(enemyView.IsPresenting, Is.True);
            Assert.That(_first.Health, Is.EqualTo(10));

            if (disableDuringMovement)
            {
                DisableView(enemyView);
            }

            UpdateCurrentAction(100f);

            Assert.That(_second.Coordinate, Is.EqualTo(new HexCoordinate(1, 2)));
            Assert.That(_second.MovementRemaining, Is.EqualTo(1));
            Assert.That(_second.ActionPoints, Is.EqualTo(2));

            if (!disableDuringMovement)
            {
                Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
                Assert.That(_first.Health, Is.EqualTo(10));
                UpdateCurrentAction(1.1f);
            }

            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_runner.CurrentUnitId, Is.EqualTo(_second.Id));
            Assert.That(enemyView.IsPresenting, Is.False);
            Assert.That(enemyView.transform.position, Is.EqualTo(_mapView.AxialToWorld(_second.Coordinate)));

            if (disableDuringMovement)
            {
                Assert.That(_first.Health, Is.EqualTo(10));
                Assert.That(resultCount, Is.Zero);
            }
            else
            {
                Assert.That(_first.Health, Is.EqualTo(5));
                Assert.That(resultCount, Is.EqualTo(1));
                Assert.That(completedResult.TargetId, Is.EqualTo(_first.Id));
                Assert.That(completedResult.IsCritical, Is.True);
            }

            UpdateCurrentAction(100f);
            Assert.That(resultCount, Is.EqualTo(disableDuringMovement ? 0 : 1));
            Assert.That(_second.ActionPoints, Is.EqualTo(2));
        }

        [Test]
        public void EnemyApproachPresentationFailureEndsTurnWithoutRetryingRemainingMovement()
        {
            HexCharacterView enemyView = CreateEnemyTurn(
                new BattleTestUnit(MercenaryClass.Rogue, 10, 3, 2, 1, 0),
                new HexCoordinate(6, 2));
            AddAnimator(enemyView);

            TickRunner();

            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Moving));
            Assert.That(_simulation.TargetUnitId, Is.Zero);
            UpdateCurrentAction(0.01f);
            Assert.That(_simulation.IsWaitingForMovementCompletion, Is.True);
            HexCoordinate committedPosition = _second.Coordinate;
            DisableView(enemyView);
            UpdateCurrentAction(100f);
            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_second.MovementRemaining, Is.EqualTo(1));
            Assert.That(_runner.CurrentUnitId, Is.EqualTo(_second.Id));
            EnableView(enemyView);

            TickRunner();
            TickRunner();

            Assert.That(_runner.CurrentUnitId, Is.EqualTo(_first.Id));
            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_second.Coordinate, Is.EqualTo(committedPosition));
            Assert.That(_second.MovementRemaining, Is.EqualTo(1));
            Assert.That(_second.ActionPoints, Is.EqualTo(4));
            Assert.That(_first.Health, Is.EqualTo(10));
        }

        [Test]
        public void RejectedEnemyApproachCommandEndsTurnWithoutSpendingResources()
        {
            CreateEnemyTurn(new BattleTestUnit(MercenaryClass.Rogue, 10, 3, 2, 1, 0),
                new HexCoordinate(6, 2));

            TickRunner();
            TickRunner();

            Assert.That(_runner.CurrentUnitId, Is.EqualTo(_first.Id));
            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_second.Coordinate, Is.EqualTo(new HexCoordinate(6, 2)));
            Assert.That(_second.MovementRemaining, Is.EqualTo(2));
            Assert.That(_second.ActionPoints, Is.EqualTo(4));
            Assert.That(_first.Health, Is.EqualTo(10));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancellationOnlyStopsUnissuedAttack(bool issued)
        {
            AddAnimator(_firstView);
            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);

            if (issued)
            {
                UpdateCurrentAction(0.1f);
            }

            _runner.CancelAction();
            _runner.CancelAction();

            if (issued)
            {
                Assert.That(_runner.IsBusy, Is.True);
                Assert.That(_firstView.IsPresenting, Is.True);
                Assert.That(_simulation.TargetUnitId, Is.EqualTo(_second.Id));
                Assert.That(_runner.TryEndTurn(_first.Id), Is.False);
                Assert.That(_runner.TryMove(_first.Id, new HexCoordinate(2, 0)), Is.False);
                Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.False);
                AssertAttackWaitsForHitAndCompletion();
                AssertAttackCompletedOnce();
            }
            else
            {
                UpdateCurrentAction(0.1f);
                Assert.That(_runner.IsBusy, Is.False);
                Assert.That(_first.ActionPoints, Is.EqualTo(4));
                Assert.That(_second.Health, Is.EqualTo(10));
                Assert.That(_firstView.IsPresenting, Is.False);
                Assert.That(_simulation.TryBeginAttack(out _), Is.False);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RunnerDisableAbortsIssuedAttackBeforeHitWithoutDamage(bool fatal)
        {
            HexCharacterView targetView = GetTargetView();
            AddAnimator(_firstView);
            int resultCount = 0;
            int stoppedCount = 0;
            SubscribeRunnerEvent("AttackCompleted", new System.Action<BattleAttackResult, HexCharacterView, Vector3>(
                (result, view, position) => resultCount++));
            SubscribeRunnerEvent("PresentationStopped", new System.Action(() => stoppedCount++));

            if (fatal)
            {
                SetField(_second, "_health", _first.Skills.BasicAttack.Damage);
            }

            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);
            UpdateCurrentAction(0.1f);
            Assert.That(_firstView.IsPresenting, Is.True);

            MethodInfo onDisable = typeof(BattleRunner).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
            onDisable.Invoke(_runner, null);
            onDisable.Invoke(_runner, null);

            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
            Assert.That(_second.Health, Is.EqualTo(fatal ? _first.Skills.BasicAttack.Damage : 10));
            Assert.That(_firstView.IsPresenting, Is.False);
            Assert.That(_runner.TryGetCharacter(_second.Id, out _), Is.True);
            Assert.That(targetView.IsInitialized, Is.True);
            Assert.That(_simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(resultCount, Is.Zero);
            Assert.That(stoppedCount, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AttackHitPublishesOneResultWithTargetPositionSnapshot(bool fatal)
        {
            HexCharacterView targetView = GetTargetView();
            AddAnimator(_firstView);
            Vector3 overlayOffset = new Vector3(0.25f, 2.75f, -0.5f);
            SetField(targetView, "_overlayOffset", overlayOffset);
            targetView.transform.position = new Vector3(5f, 8f, 13f);
            Vector3 expectedPosition = targetView.transform.TransformPoint(overlayOffset);
            int resultCount = 0;
            BattleAttackResult receivedResult = default;
            HexCharacterView receivedView = null;
            Vector3 receivedPosition = default;
            bool busyDuringEvent = false;
            bool attackerPresentingDuringEvent = false;
            bool targetRegisteredDuringEvent = false;
            bool targetInitializedDuringEvent = false;
            bool targetActiveDuringEvent = false;
            SubscribeRunnerEvent("AttackCompleted", new System.Action<BattleAttackResult, HexCharacterView, Vector3>(
                (result, view, position) =>
                {
                    resultCount++;
                    receivedResult = result;
                    receivedView = view;
                    receivedPosition = position;
                    busyDuringEvent = _runner.IsBusy;
                    attackerPresentingDuringEvent = _firstView.IsPresenting;
                    targetRegisteredDuringEvent = _runner.TryGetCharacter(result.TargetId, out _);
                    targetInitializedDuringEvent = view.IsInitialized;
                    targetActiveDuringEvent = view.gameObject.activeSelf;
                }));

            if (fatal)
            {
                SetField(_second, "_health", _first.Skills.BasicAttack.Damage);
            }

            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);
            UpdateCurrentAction(0.1f);
            Assert.That(resultCount, Is.Zero);
            UpdateCurrentAction(0.4f);
            Assert.That(resultCount, Is.Zero);
            UpdateCurrentAction(0.2f);

            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(receivedResult.TargetId, Is.EqualTo(_second.Id));
            Assert.That(receivedResult.Damage, Is.EqualTo(3));
            Assert.That(receivedResult.RemainingHealth, Is.EqualTo(fatal ? 0 : 7));
            Assert.That(receivedResult.TargetDied, Is.EqualTo(fatal));
            Assert.That(receivedView, Is.SameAs(targetView));
            Assert.That(receivedPosition, Is.EqualTo(expectedPosition));
            Assert.That(busyDuringEvent, Is.True);
            Assert.That(attackerPresentingDuringEvent, Is.True);
            Assert.That(targetRegisteredDuringEvent, Is.EqualTo(!fatal));
            Assert.That(targetInitializedDuringEvent, Is.EqualTo(!fatal));
            Assert.That(targetActiveDuringEvent, Is.EqualTo(!fatal));

            if (fatal)
            {
                Assert.That(targetView.transform.TransformPoint(overlayOffset), Is.Not.EqualTo(expectedPosition));
            }

            UpdateCurrentAction(0.5f);
            Assert.That(_runner.IsBusy, Is.False);
            UpdateCurrentAction(0.1f);
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
            Assert.That(_second.Health, Is.EqualTo(fatal ? 0 : 7));
        }

        [Test]
        public void ReleaseBattleAbortsIssuedAttackBeforeHitAndStopsPresentation()
        {
            AddAnimator(_firstView);
            HexCharacterView targetView = GetTargetView();
            int resultCount = 0;
            int stoppedCount = 0;
            bool initializedAtStop = true;
            bool busyAtStop = true;
            SubscribeRunnerEvent("AttackCompleted", new System.Action<BattleAttackResult, HexCharacterView, Vector3>(
                (result, view, position) => resultCount++));
            SubscribeRunnerEvent("PresentationStopped", new System.Action(() =>
            {
                stoppedCount++;
                initializedAtStop = _runner.IsInitialized;
                busyAtStop = _runner.IsBusy;
            }));
            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);
            UpdateCurrentAction(0.1f);

            typeof(BattleRunner).GetMethod("ReleaseBattle", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_runner, null);

            Assert.That(resultCount, Is.Zero);
            Assert.That(stoppedCount, Is.EqualTo(1));
            Assert.That(initializedAtStop, Is.False);
            Assert.That(busyAtStop, Is.False);
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
            Assert.That(_second.Health, Is.EqualTo(10));
            Assert.That(_firstView.IsInitialized, Is.False);
            Assert.That(targetView.IsInitialized, Is.False);
            Assert.That(_simulation.TryCompleteAttack(out _), Is.False);
        }

        [Test]
        public void RemovingIssuedAttackTargetFinishesPresentationWithoutDamage()
        {
            AddAnimator(_firstView);
            int resultCount = 0;
            SubscribeRunnerEvent("AttackCompleted", new System.Action<BattleAttackResult, HexCharacterView, Vector3>(
                (result, view, position) => resultCount++));
            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);
            UpdateCurrentAction(0.1f);
            Assert.That(_runner.RemoveCharacter(_second.Id), Is.True);

            _runner.CancelAction();

            Assert.That(_runner.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(_runner.IsBusy, Is.True);
            Assert.That(_firstView.IsPresenting, Is.True);
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
            Assert.That(_second.Health, Is.EqualTo(10));

            UpdateCurrentAction(1.1f);

            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_firstView.IsPresenting, Is.False);
            Assert.That(_second.Health, Is.EqualTo(10));
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
            Assert.That(_simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(resultCount, Is.Zero);
        }

        [Test]
        public void LegacyRegistrationWithoutClassUsesWarriorPreset()
        {
            BattleUnitRegistration registration = JsonUtility.FromJson<BattleUnitRegistration>("{\"_team\":1}");
            BattleRunner runner = CreatePreparingRunner();
            HexCharacterView view = CreateRegistrationView(out Transform model);
            model.rotation = Quaternion.LookRotation(FacingDirection(HexDirection.East), _mapView.transform.up);
            Assert.That(registration.Team, Is.EqualTo(BattleTeam.Enemy));
            Assert.That(registration.UnitClass, Is.EqualTo(MercenaryClass.Warrior));

            Assert.That(runner.TryRegisterCharacter(view, registration.Team, registration.UnitClass, out int unitId), Is.True);

            Assert.That(runner.TryGetCharacter(unitId, out HexCharacter character), Is.True);
            Assert.That(character.Team, Is.EqualTo(BattleTeam.Enemy));
            Assert.That(character.Stats.UnitClass, Is.EqualTo(MercenaryClass.Warrior));
            Assert.That(character.MaxHealth, Is.EqualTo(10));
            Assert.That(character.Stats.PhysicalDefense, Is.EqualTo(1));
        }

        [Test]
        public void FailedViewBindingReleasesUnitAndHexForRegistrationRetry()
        {
            BattleRunner runner = CreatePreparingRunner();
            BattleSimulation simulation = (BattleSimulation)GetField(runner, "_simulation");
            HexCoordinate coordinate = new HexCoordinate(2, 2);
            HexCharacterView view = CreateRegistrationView(out Transform model);
            view.transform.position = _mapView.AxialToWorld(coordinate);
            model.rotation = Quaternion.LookRotation(FacingDirection(HexDirection.West), _mapView.transform.up);
            SetField(view, "_moveSpeed", 0f);
            LogAssert.Expect(LogType.Error,
                "HexCharacterView requires a model, Animator, animation profile, and a positive finite move speed.");

            Assert.That(runner.TryRegisterCharacter(view, BattleTeam.Player, MercenaryClass.Rogue, out int unitId), Is.False);

            Assert.That(unitId, Is.Zero);
            Assert.That(simulation.State, Is.EqualTo(BattleState.Preparing));
            Assert.That(simulation.Board.Units.Count, Is.Zero);
            Assert.That(simulation.Board.IsOccupied(coordinate), Is.False);
            Assert.That(view.IsInitialized, Is.False);
            Assert.That(view.UnitId, Is.Zero);
            Assert.That(runner.TryGetUnitId(view, out _), Is.False);
            Assert.That((Dictionary<int, HexCharacterView>)GetField(runner, "_views"), Is.Empty);
            SetField(view, "_moveSpeed", 3f);

            Assert.That(runner.TryRegisterCharacter(view, BattleTeam.Player, MercenaryClass.Rogue, out unitId), Is.True);

            Assert.That(simulation.Board.Units.Count, Is.EqualTo(1));
            Assert.That(simulation.Board.TryGetUnit(coordinate, out HexCharacter character), Is.True);
            Assert.That(character.Id, Is.EqualTo(unitId));
            Assert.That(character.Facing, Is.EqualTo(HexDirection.West));
            Assert.That(character.Stats.UnitClass, Is.EqualTo(MercenaryClass.Rogue));
            Assert.That(view.IsInitialized, Is.True);
            Assert.That(runner.TryGetUnitId(view, out int registeredId), Is.True);
            Assert.That(registeredId, Is.EqualTo(unitId));
        }

        [TestCase(MercenaryClass.Warrior, 10)]
        [TestCase(MercenaryClass.Rogue, 8)]
        [TestCase(MercenaryClass.Archer, 8)]
        public void RegistrationUsesSelectedClassAndSnapsPositionAndFacing(MercenaryClass unitClass, int maxHealth)
        {
            BattleRunner runner = CreatePreparingRunner();
            HexCoordinate coordinate = new HexCoordinate(2, 2);
            HexCharacterView view = CreateRegistrationView(out Transform model);
            view.transform.position = _mapView.AxialToWorld(coordinate) + new Vector3(0.05f, 0.2f, 0.03f);
            model.rotation = Quaternion.LookRotation(FacingDirection(HexDirection.West), _mapView.transform.up);

            Assert.That(runner.TryRegisterCharacter(view, BattleTeam.Player, unitClass, out int unitId), Is.True);

            Assert.That(runner.TryGetCharacter(unitId, out HexCharacter character), Is.True);
            Assert.That(character.Stats.UnitClass, Is.EqualTo(unitClass));
            Assert.That(character.Traits.Has(TraitType.RearGuaranteedCritical),
                Is.EqualTo(unitClass == MercenaryClass.Rogue));
            Assert.That(character.Traits.Has(TraitType.DirectionalPhysicalNegation), Is.False);
            Assert.That(character.Traits.Has(TraitType.OpportunityAttack),
                Is.EqualTo(unitClass == MercenaryClass.Warrior));
            Assert.That(character.Health, Is.EqualTo(maxHealth));
            Assert.That(character.MaxHealth, Is.EqualTo(maxHealth));
            Assert.That(character.Facing, Is.EqualTo(HexDirection.West));
            Assert.That(character.Coordinate, Is.EqualTo(coordinate));
            Assert.That(view.transform.position, Is.EqualTo(_mapView.AxialToWorld(coordinate)));
            Assert.That(Vector3.Angle(model.forward, FacingDirection(HexDirection.West)), Is.LessThan(0.1f));
        }

        [TestCase(HexDirection.East)]
        [TestCase(HexDirection.NorthEast)]
        [TestCase(HexDirection.NorthWest)]
        [TestCase(HexDirection.West)]
        [TestCase(HexDirection.SouthWest)]
        [TestCase(HexDirection.SouthEast)]
        public void InitialFacingTieChoosesClockwiseOnRotatedMapWithModelOffset(HexDirection clockwise)
        {
            _mapView.transform.rotation = Quaternion.Euler(17f, 38f, 11f);
            BattleRunner runner = CreatePreparingRunner();
            HexCharacterView view = CreateRegistrationView(out Transform model);
            view.transform.rotation = Quaternion.Euler(12f, 23f, 9f);
            HexDirection counterclockwise = (HexDirection)(((int)clockwise + 1) % 6);
            Vector3 middle = (FacingDirection(clockwise) + FacingDirection(counterclockwise)).normalized;
            model.rotation = Quaternion.LookRotation(middle, _mapView.transform.up);
            Quaternion localRotation = model.localRotation;

            Assert.That(runner.TryRegisterCharacter(view, BattleTeam.Player, MercenaryClass.Archer, out int unitId), Is.True);

            Assert.That(runner.TryGetCharacter(unitId, out HexCharacter character), Is.True);
            Assert.That(character.Facing, Is.EqualTo(clockwise));
            Assert.That(Quaternion.Angle(model.localRotation, localRotation), Is.LessThan(0.1f));
            Assert.That(Vector3.Angle(model.forward, FacingDirection(clockwise)), Is.LessThan(0.1f));
            DisableView(view);
            view.transform.rotation = Quaternion.identity;
            EnableView(view);
            Assert.That(Vector3.Angle(model.forward, FacingDirection(clockwise)), Is.LessThan(0.1f));
        }

        [TestCase(0.49f, HexDirection.East)]
        [TestCase(0.51f, HexDirection.NorthEast)]
        public void InitialFacingUsesNearestDirectionOnEitherSideOfTie(float blend, HexDirection expected)
        {
            BattleRunner runner = CreatePreparingRunner();
            HexCharacterView view = CreateRegistrationView(out Transform model);
            Vector3 direction = Vector3.Lerp(FacingDirection(HexDirection.East),
                FacingDirection(HexDirection.NorthEast), blend).normalized;
            model.rotation = Quaternion.LookRotation(direction, _mapView.transform.up);

            Assert.That(runner.TryRegisterCharacter(view, BattleTeam.Player, MercenaryClass.Warrior, out int unitId), Is.True);

            Assert.That(runner.TryGetCharacter(unitId, out HexCharacter character), Is.True);
            Assert.That(character.Facing, Is.EqualTo(expected));
        }

        [Test]
        public void RunnerUpdateSamplesIdleHonorsDisableAndStartsNewAttackAtZero()
        {
            AddAnimator(_firstView);
            Transform pose = _firstView.transform.Find("Animation Pose");
            pose.localScale = new Vector3(99f, 1f, 1f);
            float delta = Time.deltaTime;

            TickRunner();

            Assert.That(pose.localScale.x, Is.EqualTo(1f + delta % 1f).Within(0.001f));
            _runner.enabled = false;
            pose.localScale = new Vector3(99f, 1f, 1f);
            TickRunner();
            Assert.That(pose.localScale.x, Is.EqualTo(99f));
            UpdateCurrentAction(0.25f);
            Assert.That(pose.localScale.x, Is.EqualTo(99f));
            _runner.enabled = true;
            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);

            TickRunner();

            Assert.That(pose.localScale.x, Is.EqualTo(5f).Within(0.001f));
            Assert.That(_second.Health, Is.EqualTo(10));
            Assert.That(_firstView.TryReadAttackCompletion(out bool completed), Is.True);
            Assert.That(completed, Is.False);
        }

        [Test]
        public void AnimationTickAdvancesEveryIdleViewWithoutAnAction()
        {
            AddAnimator(_firstView);
            HexCharacterView target = GetTargetView();

            BattlePresentationTestUtility.AdvanceAnimations(_runner, 0.25f);

            Assert.That(_firstView.transform.Find("Animation Pose").localScale.x, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(target.transform.Find("Animation Pose").localScale.x, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(_runner.IsBusy, Is.False);
            UpdateCurrentAction(0.25f);
            Assert.That(_firstView.transform.Find("Animation Pose").localScale.x, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(target.transform.Find("Animation Pose").localScale.x, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void NewAttackStartsAtZeroAfterTickAndAppliesAnimatorSpeedOnlyOnce()
        {
            Animator animator = AddAnimator(_firstView);
            animator.speed = 2f;
            Transform pose = _firstView.transform.Find("Animation Pose");
            Assert.That(_runner.TryAttack(_first.Id, _second.Id), Is.True);

            UpdateCurrentAction(10f);

            Assert.That(pose.localScale.x, Is.EqualTo(5f).Within(0.001f));
            Assert.That(_second.Health, Is.EqualTo(10));
            Assert.That(_firstView.TryReadAttackCompletion(out bool completed), Is.True);
            Assert.That(completed, Is.False);
            UpdateCurrentAction(0.2f);
            Assert.That(pose.localScale.x, Is.EqualTo(5.4f).Within(0.001f));
            Assert.That(_second.Health, Is.EqualTo(10));
            UpdateCurrentAction(0.05f);
            Assert.That(pose.localScale.x, Is.EqualTo(5.5f).Within(0.001f));
            Assert.That(_runner.IsBusy, Is.True);
            Assert.That(_second.Health, Is.EqualTo(7));
            UpdateCurrentAction(0.25f);
            AssertAttackCompletedOnce();
        }

        [Test]
        public void UnbindReleasesAnimationOwnershipAndAllowsCleanRebinding()
        {
            Animator animator = AddAnimator(_firstView);
            CharacterAnimationProfile profile = (CharacterAnimationProfile)GetField(_firstView, "_animationProfile");
            CharacterAnimationPlayer player = (CharacterAnimationPlayer)GetField(_firstView, "_animationPlayer");
            Assert.That(player.IsReady, Is.True);

            _firstView.Unbind();
            _firstView.Unbind();

            Assert.That(player.IsReady, Is.False);
            using (CharacterAnimationPlayer other = new CharacterAnimationPlayer())
            {
                Assert.That(other.TryInitialize(animator, profile, out string error), Is.True, error);
            }

            Assert.That(_firstView.TryBind(_first, _mapView), Is.True);
            Assert.That(_firstView.IsReady, Is.True);
            Assert.That(_firstView.transform.Find("Animation Pose").localScale.x, Is.EqualTo(1f).Within(0.001f));
        }

        void AssertAttackWaitsForHitAndCompletion()
        {
            Assert.That(_runner.IsBusy, Is.True);
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
            Assert.That(_second.Health, Is.EqualTo(10));
            UpdateCurrentAction(0.4f);
            Assert.That(_runner.IsBusy, Is.True);
            Assert.That(_second.Health, Is.EqualTo(10));
            UpdateCurrentAction(0.2f);
            Assert.That(_runner.IsBusy, Is.True);
            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(_second.Health, Is.EqualTo(7));
            UpdateCurrentAction(0.5f);
        }

        void AssertAttackAbortedWithoutDamage()
        {
            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
            Assert.That(_second.Health, Is.EqualTo(10));
            Assert.That(_runner.TryGetCharacter(_first.Id, out HexCharacter retainedAttacker), Is.True);
            Assert.That(retainedAttacker, Is.SameAs(_first));
            Assert.That(_runner.TryGetCharacter(_second.Id, out HexCharacter retainedTarget), Is.True);
            Assert.That(retainedTarget, Is.SameAs(_second));
            Assert.That(_simulation.Board.IsOccupied(_second.Coordinate), Is.True);
            Assert.That(_firstView.IsPresenting, Is.False);
            UpdateCurrentAction(0.1f);
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
            Assert.That(_second.Health, Is.EqualTo(10));
        }

        void AssertAttackCompletedOnce()
        {
            Assert.That(_runner.IsBusy, Is.False);
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
            Assert.That(_second.Health, Is.EqualTo(7));
            Assert.That(_runner.TryGetCharacter(_first.Id, out HexCharacter retainedAttacker), Is.True);
            Assert.That(retainedAttacker, Is.SameAs(_first));
            Assert.That(_runner.TryGetCharacter(_second.Id, out HexCharacter retainedTarget), Is.True);
            Assert.That(retainedTarget, Is.SameAs(_second));
            Assert.That(_simulation.Board.IsOccupied(_second.Coordinate), Is.True);
            Assert.That(_firstView.IsPresenting, Is.False);
            Assert.That(Vector3.Angle(_firstView.transform.forward, FacingDirection(_first.Facing)), Is.LessThan(0.1f));
            UpdateCurrentAction(0.1f);
            Assert.That(_first.ActionPoints, Is.EqualTo(2));
            Assert.That(_second.Health, Is.EqualTo(7));
        }

        HexCharacterView GetTargetView()
        {
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            return views[_second.Id];
        }

        HexCharacterView CreateEnemyTurn(BattleTestUnit enemyStats, HexCoordinate enemyCoordinate,
            TraitCollection traits = null)
        {
            _simulation = new BattleSimulation(_mapView.Width, _mapView.Height, randomSeed: 0);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Enemy, enemyCoordinate, enemyStats,
                HexDirection.West, out _second, traits), Is.True);
            Assert.That(_simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 2),
                BattleTestUtility.CreateStats(), HexDirection.East, out _first), Is.True);
            BattleTestUtility.StartTurn(_simulation, _second.Id);
            SetField(_runner, "_simulation", _simulation);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            views.Clear();
            _firstView = CreateView(_first);
            HexCharacterView enemyView = CreateView(_second);
            views.Add(_first.Id, _firstView);
            views.Add(_second.Id, enemyView);
            return enemyView;
        }

        BattleRunner CreatePreparingRunner()
        {
            BattleRunner runner = CreateObject("Preparing Runner").AddComponent<BattleRunner>();
            SetField(runner, "_simulation", new BattleSimulation(_mapView.Width, _mapView.Height, randomSeed: 0));
            SetField(runner, "_mapView", _mapView);
            SetField(runner, "_hexSize", _mapView.HexSize);
            SetField(runner, "_classData", BattleSkillTestAssets.CreateCatalog(asset => _objects.Add(asset)));
            return runner;
        }

        HexCharacterView CreateRegistrationView(out Transform model)
        {
            HexCharacterView view = CreateObject("Registration Unit").AddComponent<HexCharacterView>();
            model = CreateObject("Registration Model").transform;
            model.SetParent(view.transform, false);
            BattleAnimationFixture fixture = new BattleAnimationFixture(model.gameObject);
            _animatorFixtures.Add(fixture);
            fixture.Attach(view);
            return view;
        }

        Vector3 FacingDirection(HexDirection direction)
        {
            return (_mapView.AxialToWorld(default(HexCoordinate).GetNeighbor(direction))
                - _mapView.AxialToWorld(default)).normalized;
        }

        static void DisableView(HexCharacterView view)
        {
            view.enabled = false;
            typeof(HexCharacterView).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, null);
        }

        static void EnableView(HexCharacterView view)
        {
            view.enabled = true;
            typeof(HexCharacterView).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, null);
        }

        Animator AddAnimator(HexCharacterView view)
        {
            BattleAnimationFixture fixture = new BattleAnimationFixture(view.gameObject);
            _animatorFixtures.Add(fixture);
            HexCharacter character = (HexCharacter)GetField(view, "_character");
            HexMapView mapView = (HexMapView)GetField(view, "_mapView");
            view.Unbind();
            fixture.Attach(view);
            Assert.That(view.TryBind(character, mapView), Is.True);
            return fixture.Animator;
        }

        GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        HexCharacterView CreateView(HexCharacter character)
        {
            HexCharacterView view = CreateObject("Unit").AddComponent<HexCharacterView>();
            SetField(view, "_character", character);
            SetField(view, "_mapView", _mapView);
            SetField(view, "_model", view.transform);
            view.transform.position = _mapView.AxialToWorld(character.Coordinate);
            return view;
        }

        void TickRunner()
        {
            typeof(BattleRunner).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_runner, null);
        }

        void AdvanceMovement(float deltaTime)
        {
            BattlePresentationTestUtility.Invoke(_runner, "AdvanceMovement", new object[] { _firstView, deltaTime });
        }

        void UpdateCurrentAction(float deltaTime)
        {
            BattlePresentationTestUtility.Invoke(_runner, "UpdateCurrentAction", new object[] { deltaTime });
        }

        void SubscribeRunnerEvent(string name, System.Delegate handler)
        {
            typeof(BattleRunner).GetEvent(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetAddMethod(true).Invoke(_runner, new object[] { handler });
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
