using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattlePioneerTests
    {
        [TestCase(SkillType.BasicAttack)]
        [TestCase(SkillType.Stealth)]
        [TestCase(SkillType.Ambush)]
        [TestCase(SkillType.Overwatch)]
        [TestCase(SkillType.Invisibility)]
        [TestCase(SkillType.AssassinLeap)]
        [TestCase(SkillType.SpiritImbuement)]
        [TestCase((SkillType)(-1))]
        [TestCase((SkillType)99)]
        public void PioneerEntryPointsRejectOtherSkillTypesWithoutChangingState(SkillType skill)
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 5), true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 5));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.CanUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id)).CanUse, Is.True);
            BattlePathLine line = new BattlePathLine((long)target.Coordinate.R - actor.Coordinate.R,
                (long)actor.Coordinate.Q - target.Coordinate.Q);
            List<HexCoordinate> path = new List<HexCoordinate> { new HexCoordinate(3, 5) };
            Assert.That(battle.CanUsePioneerSkill(actor.Id, target.Id, skill), Is.False);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill), Is.False);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill, line), Is.False);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill, path), Is.False);
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill, path), Is.False);
            Assert.That(path, Is.Empty);
            path.Add(new HexCoordinate(3, 5));
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill, line, path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(actor.ActionPoints, Is.EqualTo(8));
            Assert.That(actor.MovementRemaining, Is.EqualTo(2));
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(2, 5)));
            Assert.That(target.Coordinate, Is.EqualTo(new HexCoordinate(4, 5)));
            Assert.That(actor.HasUsedSkillMovementRecovery, Is.False);
            Assert.That(battle.IsBusy, Is.False);
        }

        [TestCase(SkillType.PullSelf, HexDirection.East)]
        [TestCase(SkillType.PullSelf, HexDirection.NorthEast)]
        [TestCase(SkillType.PullSelf, HexDirection.NorthWest)]
        [TestCase(SkillType.PullSelf, HexDirection.West)]
        [TestCase(SkillType.PullSelf, HexDirection.SouthWest)]
        [TestCase(SkillType.PullSelf, HexDirection.SouthEast)]
        [TestCase(SkillType.PullTarget, HexDirection.East)]
        [TestCase(SkillType.PullTarget, HexDirection.NorthEast)]
        [TestCase(SkillType.PullTarget, HexDirection.NorthWest)]
        [TestCase(SkillType.PullTarget, HexDirection.West)]
        [TestCase(SkillType.PullTarget, HexDirection.SouthWest)]
        [TestCase(SkillType.PullTarget, HexDirection.SouthEast)]
        public void BothSkillsMoveThreeStepsOnEveryAxisWithoutAttacking(SkillType skill, HexDirection direction)
        {
            BattleSimulation battle = NewBattle();
            HexCoordinate origin = new HexCoordinate(5, 5);
            HexCoordinate targetPosition = Advance(origin, direction, 4);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, origin, true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, targetPosition);
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill, path), Is.True);
            Assert.That(path.Count, Is.EqualTo(3));
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill), Is.True);
            Assert.That(actor.ActionPoints, Is.EqualTo(8));
            Assert.That(battle.ActingUnitId, Is.EqualTo(actor.Id));
            Assert.That(battle.MovingUnitId, Is.EqualTo(skill == SkillType.PullSelf ? actor.Id : target.Id));

            for (int i = 0; i < path.Count; i++)
            {
                Assert.That(battle.TryBeginMovementStep(out HexMovementStep step), Is.True);
                Assert.That(step.UnitId, Is.EqualTo(skill == SkillType.PullSelf ? actor.Id : target.Id));
                Assert.That(step.To, Is.EqualTo(path[i]));
                Assert.That(battle.TryCompleteMovementStep(), Is.True);
                Assert.That(battle.TryCompleteMovementStep(), Is.False);
            }

            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(battle.TryBeginAttack(out _), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(6));
            Assert.That(actor.MovementRemaining, Is.EqualTo(2));
            Assert.That(actor.HasUsedSkillMovementRecovery, Is.True);
            Assert.That(target.Health, Is.EqualTo(20));
            Assert.That(target.Facing, Is.EqualTo(HexDirection.East));
            Assert.That(actor.Facing, Is.EqualTo(direction));
            Assert.That(actor.Coordinate.DistanceTo(target.Coordinate), Is.EqualTo(1));
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(actor.Id));
            Assert.That(battle.IsBusy, Is.False);
        }

        [TestCase(3, 5)]
        [TestCase(9, 5)]
        [TestCase(5, 9)]
        public void InvalidRangeIsRejectedWithoutSpending(int q, int r)
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 5), true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(q, r));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.False);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(8));
            Assert.That(actor.HasUsedSkillMovementRecovery, Is.False);
            Assert.That(battle.IsBusy, Is.False);
        }

        [TestCase(SkillType.PullSelf, 2)]
        [TestCase(SkillType.PullSelf, 3)]
        [TestCase(SkillType.PullSelf, 4)]
        [TestCase(SkillType.PullSelf, 5)]
        [TestCase(SkillType.PullSelf, 6)]
        [TestCase(SkillType.PullTarget, 2)]
        [TestCase(SkillType.PullTarget, 3)]
        [TestCase(SkillType.PullTarget, 4)]
        [TestCase(SkillType.PullTarget, 5)]
        [TestCase(SkillType.PullTarget, 6)]
        public void NonAxisTargetsShareAReversiblePathAndExecuteThePreview(SkillType skill, int distance)
        {
            BattleSimulation battle = NewBattle();
            HexCoordinate origin = new HexCoordinate(2, 5);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, origin, true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(2 + distance - 1, 6));
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> forward = new List<HexCoordinate>();
            List<HexCoordinate> backward = new List<HexCoordinate>();
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, SkillType.PullSelf, forward), Is.True);
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, SkillType.PullTarget, backward), Is.True);
            Assert.That(forward.Count, Is.EqualTo(distance - 1));
            backward.Reverse();
            Assert.That(backward, Is.EqualTo(forward));
            List<HexCoordinate> repeated = new List<HexCoordinate>();
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, SkillType.PullSelf, repeated), Is.True);
            Assert.That(repeated, Is.EqualTo(forward));

            if (skill == SkillType.PullTarget)
            {
                forward.Reverse();
            }

            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill), Is.True);
            HexCharacter mover = skill == SkillType.PullSelf ? actor : target;
            HexCoordinate anchor = skill == SkillType.PullSelf ? target.Coordinate : actor.Coordinate;

            for (int i = 0; i < forward.Count; i++)
            {
                int previousDistance = mover.Coordinate.DistanceTo(anchor);
                Assert.That(battle.TryBeginMovementStep(out HexMovementStep step), Is.True);
                Assert.That(step.UnitId, Is.EqualTo(mover.Id));
                Assert.That(step.To, Is.EqualTo(forward[i]));
                Assert.That(mover.Coordinate.DistanceTo(anchor), Is.EqualTo(previousDistance - 1));
                Assert.That(battle.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(actor.ActionPoints, Is.EqualTo(6));
            Assert.That(actor.Coordinate.DistanceTo(target.Coordinate), Is.EqualTo(1));
            Assert.That(target.Facing, Is.EqualTo(HexDirection.East));
        }

        [Test]
        public void BfsFindsAnotherBranchWhenTheFirstCloserNeighborIsADeadEnd()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 5), true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 7));
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 5));
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(3, 6));
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, SkillType.PullSelf, path), Is.True);
            Assert.That(path, Is.EqualTo(new[]
            {
                new HexCoordinate(2, 6), new HexCoordinate(2, 7), new HexCoordinate(3, 7)
            }));
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.True);
            FinishSkill(battle);
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(3, 7)));
        }

        [Test]
        public void ALongerDetourIsRejectedWhenEveryCloserFirstStepIsOccupied()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 5), true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 7));
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(3, 5));
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(2, 6));
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> detour = new List<HexCoordinate>();
            Assert.That(new HexPathfinder().TryFindPathToAttackPosition(battle.Board, actor.Coordinate,
                target.Coordinate, 1, detour), Is.True);
            Assert.That(detour.Count, Is.GreaterThan(3));
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, SkillType.PullSelf, detour), Is.False);
            Assert.That(detour, Is.Empty);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(8));
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void FirstIssueRejectsABlockedFrozenPathEvenWhenAnotherPathExists(SkillType skill)
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 5), true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 7));
            HexCharacter blocker = Spawn(battle, BattleTeam.Enemy,
                skill == SkillType.PullSelf ? new HexCoordinate(5, 5) : new HexCoordinate(4, 4));
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> original = new List<HexCoordinate>();
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill, original), Is.True);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill), Is.True);
            MoveExternally(battle, blocker.Id, original[original.Count - 1]);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(2, 5)));
            Assert.That(target.Coordinate, Is.EqualTo(new HexCoordinate(4, 7)));
            Assert.That(actor.ActionPoints, Is.EqualTo(8));
            Assert.That(actor.HasUsedSkillMovementRecovery, Is.False);
            Assert.That(battle.TrySettleAction(), Is.True);
            List<HexCoordinate> alternative = new List<HexCoordinate>();
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill, alternative), Is.True);
            Assert.That(alternative, Is.Not.EqualTo(original));
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void AimLineSelectsDifferentPathsAndFrozenCommandOwnsItsCopy(SkillType skill)
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 5), true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 7));
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> horizontal = new List<HexCoordinate>();
            List<HexCoordinate> vertical = new List<HexCoordinate>();
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill,
                new BattlePathLine(0d, 1d), horizontal), Is.True);
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill,
                new BattlePathLine(1d, 0d), vertical), Is.True);
            List<HexCoordinate> expectedHorizontal = new List<HexCoordinate>
            {
                new HexCoordinate(3, 5), new HexCoordinate(4, 5), new HexCoordinate(4, 6)
            };
            List<HexCoordinate> expectedVertical = new List<HexCoordinate>
            {
                new HexCoordinate(2, 6), new HexCoordinate(2, 7), new HexCoordinate(3, 7)
            };

            if (skill == SkillType.PullTarget)
            {
                expectedHorizontal.Reverse();
                expectedVertical.Reverse();
            }

            Assert.That(horizontal, Is.EqualTo(expectedHorizontal));
            Assert.That(vertical, Is.EqualTo(expectedVertical));
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill, horizontal), Is.True);
            horizontal.Clear();
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill, vertical), Is.False);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill, new BattlePathLine(1d, 0d)), Is.False);

            for (int i = 0; i < expectedHorizontal.Count; i++)
            {
                Assert.That(battle.TryBeginMovementStep(out HexMovementStep step), Is.True);
                Assert.That(step.To, Is.EqualTo(expectedHorizontal[i]));
                Assert.That(battle.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(actor.ActionPoints, Is.EqualTo(6));
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void QueriesWithoutOutputPreserveEarlierPathsAndLeaveTheBoardUnchanged(SkillType skill)
        {
            BattleSimulation battle = NewBattle();
            HexCoordinate origin = new HexCoordinate(2, 5);
            HexCoordinate targetPosition = new HexCoordinate(4, 7);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, origin, true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, targetPosition);
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill,
                new BattlePathLine(0d, 1d), path), Is.True);
            List<HexCoordinate> expected = new List<HexCoordinate>(path);
            uint occupancyVersion = battle.Board.OccupancyVersion;

            Assert.That(battle.CanUsePioneerSkill(actor.Id, target.Id, skill), Is.True);
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill,
                new BattlePathLine(0d, 0d), null), Is.False);
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill,
                new BattlePathLine(1d, 0d), null), Is.True);
            Assert.That(path, Is.EqualTo(expected));
            Assert.That(battle.Board.OccupancyVersion, Is.EqualTo(occupancyVersion));
            Assert.That(actor.Coordinate, Is.EqualTo(origin));
            Assert.That(target.Coordinate, Is.EqualTo(targetPosition));
            Assert.That(actor.ActionPoints, Is.EqualTo(8));
            Assert.That(actor.MovementRemaining, Is.EqualTo(2));
            Assert.That(battle.IsBusy, Is.False);

            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill,
                new BattlePathLine(0d, 1d), path), Is.True);
            Assert.That(path, Is.EqualTo(expected));
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void LaterParentsImproveTheScoreBeforeTheNextLayerIsExpanded(SkillType skill)
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 5), true);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(5, 8));
            BattleTestUtility.StartTurn(battle, actor.Id);
            BattlePathLine line = new BattlePathLine(1d, -1d);
            List<HexCoordinate> path = new List<HexCoordinate>();
            List<HexCoordinate> expected = new List<HexCoordinate>
            {
                new HexCoordinate(3, 5), new HexCoordinate(3, 6), new HexCoordinate(4, 6),
                new HexCoordinate(4, 7), new HexCoordinate(5, 7)
            };

            if (skill == SkillType.PullTarget)
            {
                expected.Reverse();
            }

            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill, line, path), Is.True);
            Assert.That(path, Is.EqualTo(expected));
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, skill,
                new BattlePathLine(-double.MaxValue, double.MaxValue), path), Is.True);
            Assert.That(path, Is.EqualTo(expected));
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill, line), Is.True);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.That(battle.TryBeginMovementStep(out HexMovementStep step), Is.True);
                Assert.That(step.To, Is.EqualTo(expected[i]));
                Assert.That(battle.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(battle.TrySettleAction(), Is.True);
        }

        [TestCase(0d, 0d)]
        [TestCase(double.NaN, 1d)]
        [TestCase(1d, double.PositiveInfinity)]
        public void InvalidAimClearsPreviewAndCannotQueue(double qWeight, double rWeight)
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            BattlePathLine line = new BattlePathLine(qWeight, rWeight);
            List<HexCoordinate> path = new List<HexCoordinate> { actor.Coordinate };
            Assert.That(line.IsValid, Is.False);
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, SkillType.PullSelf, line, path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf, line), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(8));
            Assert.That(battle.IsBusy, Is.False);
        }

        [Test]
        public void MalformedOrOccupiedFrozenPathsAreRejectedWithoutCost()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            HexCharacter blocker = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 6));
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> path = new List<HexCoordinate>
            {
                new HexCoordinate(4, 5), new HexCoordinate(3, 5), new HexCoordinate(5, 5)
            };
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf, path), Is.False);
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, SkillType.PullSelf, path), Is.True);
            MoveExternally(battle, blocker.Id, path[path.Count - 1]);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf, path), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(8));
            Assert.That(actor.HasUsedSkillMovementRecovery, Is.False);
            Assert.That(battle.IsBusy, Is.False);
        }

        [Test]
        public void EntireBlockedRouteIsRejectedAndPreviewIsEmpty()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 5));
            BattleTestUtility.StartTurn(battle, actor.Id);
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(battle.TryCopyPioneerPath(actor.Id, target.Id, SkillType.PullSelf, path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(8));
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(2, 5)));
        }

        [Test]
        public void InitialRouteIsRevalidatedBeforeFirstCostAndMovement()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            HexCharacter blocker = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 6));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.True);
            MoveExternally(battle, blocker.Id, new HexCoordinate(4, 5));
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(8));
            Assert.That(actor.HasUsedSkillMovementRecovery, Is.False);
            Assert.That(battle.TrySettleAction(), Is.True);
        }

        [Test]
        public void LaterBlockedStepStopsWithoutRefundOrMovingThroughTheBlocker()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            HexCharacter blocker = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 6));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            MoveExternally(battle, blocker.Id, new HexCoordinate(4, 5));
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(3, 5)));
            Assert.That(actor.ActionPoints, Is.EqualTo(6));
            Assert.That(actor.HasUsedSkillMovementRecovery, Is.True);
            Assert.That(battle.TrySettleAction(), Is.True);
        }

        [Test]
        public void ZeroMovementCanUseSkillAndRecoveryOnlyTriggersOnceInTheTurn()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Invoke(actor, "SpendMovement", 2);
            Assert.That(actor.MovementRemaining, Is.Zero);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(actor.MovementRemaining, Is.EqualTo(1));
            battle.StopAction();
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TrySettleAction(), Is.True);
            Invoke(actor, "SpendMovement", 1);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.True);
            FinishSkill(battle);
            Assert.That(actor.MovementRemaining, Is.Zero);
            Assert.That(actor.ActionPoints, Is.EqualTo(4));
            Assert.That(battle.TryEndTurn(actor.Id), Is.True);
            Assert.That(battle.TryEndTurn(target.Id), Is.True);
            Assert.That(actor.HasUsedSkillMovementRecovery, Is.False);
            Assert.That(actor.MovementRemaining, Is.EqualTo(2));
        }

        [Test]
        public void MissingRecoveryTraitDoesNotGrantItImplicitly()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 5), true, recovery: false);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(6, 5));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Invoke(actor, "SpendMovement", 2);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.True);
            FinishSkill(battle);
            Assert.That(actor.MovementRemaining, Is.Zero);
            Assert.That(actor.HasUsedSkillMovementRecovery, Is.False);
        }

        [Test]
        public void StealthTurnAllowanceRemainsTheRecoveryCapAfterSkillReveals()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryEnterStealth(actor.Id), Is.True);
            Assert.That(battle.TryEndTurn(actor.Id), Is.True);
            Assert.That(battle.TryEndTurn(target.Id), Is.True);
            Assert.That(actor.TurnMovementAllowance, Is.EqualTo(1));
            Invoke(actor, "SpendMovement", 1);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.True);
            FinishSkill(battle);
            Assert.That(actor.IsStealthed, Is.False);
            Assert.That(actor.MovementRemaining, Is.EqualTo(1));
            Assert.That(actor.TurnMovementAllowance, Is.EqualTo(1));
        }

        [Test]
        public void UserCancelAndPauseCannotInterruptIssuedSkillButForcedStopCan()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            battle.CancelAction();
            Assert.That(battle.IsWaitingForMovementCompletion, Is.True);
            Assert.That(battle.PauseMovement(), Is.False);
            Assert.That(battle.ResumeMovement(), Is.False);
            battle.StopAction();
            battle.StopAction();
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(target.Coordinate, Is.EqualTo(new HexCoordinate(5, 5)));
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(6));
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.TrySettleAction(), Is.False);
        }

        [Test]
        public void CancellationBeforeFirstIssueDoesNotSpendOrReveal()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryEnterStealth(actor.Id), Is.True);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.True);
            battle.CancelAction();
            Assert.That(actor.ActionPoints, Is.EqualTo(6));
            Assert.That(actor.IsStealthed, Is.True);
            Assert.That(actor.HasUsedSkillMovementRecovery, Is.False);
            Assert.That(battle.IsBusy, Is.False);
        }

        [Test]
        public void RejectedCommandDuringMovementDoesNotEraseTheActivePath()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.True);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.False);
            FinishSkill(battle);
            Assert.That(target.Coordinate, Is.EqualTo(new HexCoordinate(3, 5)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RemovingActorOrMoverPreservesCommittedStepUntilCleanup(bool removeActor)
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            HexCharacter ally = Spawn(battle, BattleTeam.Player, new HexCoordinate(0, 0));
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(10, 10));
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.RemoveUnit(removeActor ? actor.Id : target.Id), Is.True);
            Assert.That(battle.IsWaitingForMovementCompletion, Is.True);
            Assert.That(battle.MovingUnitId, Is.EqualTo(target.Id));
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(removeActor ? ally.Id : actor.Id));
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void SkillAndForcedMovementDoNotReserveOpportunity(SkillType skill)
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            Spawn(battle, skill == SkillType.PullSelf ? BattleTeam.Enemy : BattleTeam.Player,
                skill == SkillType.PullSelf ? new HexCoordinate(1, 5) : new HexCoordinate(7, 5),
                opportunity: true);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, skill), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.HasPendingReactions, Is.False);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            FinishSkill(battle);
        }

        [Test]
        public void NonPioneerAndInvalidSpecializationCannotUseSkills()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 5));
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(6, 5));
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(0, 0),
                new BattleTestUnit(MercenaryClass.Rogue, 20, 3, 2, 1, 0), HexDirection.East,
                out _, specialization: MercenarySpecialization.Pioneer), Is.False);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.CanSelectPioneerSkill(actor.Id), Is.False);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.False);
        }

        [Test]
        public void ForcedObserverMovementRevealsAtStepCompletionWithFacingPreserved()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            HexCharacter hidden = Spawn(battle, BattleTeam.Player, new HexCoordinate(6, 4));
            BattleTestUtility.StartTurn(battle, hidden.Id);
            Assert.That(battle.TryEnterStealth(hidden.Id), Is.True);
            Assert.That(battle.TryEndTurn(hidden.Id), Is.True);
            BattleTestUtility.AdvanceToTurn(battle, actor.Id);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(hidden.IsStealthed, Is.True);
            Assert.That(target.Facing, Is.EqualTo(HexDirection.East));
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(hidden.IsStealthed, Is.False);
        }

        [Test]
        public void ForcedMovementClearsPreparedOverwatchWithoutConsumingMovementOrAnAttack()
        {
            BattleSimulation battle = NewBattle();
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 5),
                new BattleTestUnit(MercenaryClass.Archer, 20, 3, 2, 4, 0, initiative: 40),
                HexDirection.East, out HexCharacter archer), Is.True);
            HexCharacter actor = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(6, 5), true);
            BattleTestUtility.StartTurn(battle, archer.Id);
            Assert.That(battle.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(actor.Id));
            Assert.That(battle.TryPioneerSkill(actor.Id, archer.Id, SkillType.PullTarget), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.HasPendingReactions, Is.False);
            Assert.That(archer.IsOverwatching, Is.False);
            Assert.That(archer.Facing, Is.EqualTo(HexDirection.East));
            Assert.That(archer.MovementRemaining, Is.EqualTo(2));
            Assert.That(archer.ActionPoints, Is.EqualTo(6));
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            FinishSkill(battle);
            Assert.That(actor.Health, Is.EqualTo(20));
        }

        [Test]
        public void IssuedSkillStopRevealsCasterWhileUnissuedStopPreservesStealth()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter actor, out HexCharacter target);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryEnterStealth(actor.Id), Is.True);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.True);
            battle.StopAction();
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(actor.IsStealthed, Is.True);
            Assert.That(actor.ActionPoints, Is.EqualTo(6));
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullTarget), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(actor.IsStealthed, Is.True);
            battle.StopAction();
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(actor.IsStealthed, Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(4));
            Assert.That(battle.TrySettleAction(), Is.True);
        }

        static BattleSimulation NewBattle()
        {
            return new BattleSimulation(12, 12, new BattleRules(startingActionPoints: 8), 7);
        }

        static BattleSimulation CreateBattle(out HexCharacter actor, out HexCharacter target)
        {
            BattleSimulation battle = NewBattle();
            actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(2, 5), true);
            target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(6, 5));
            return battle;
        }

        static HexCharacter Spawn(BattleSimulation battle, BattleTeam team, HexCoordinate coordinate,
            bool pioneer = false, bool recovery = true, bool opportunity = false)
        {
            List<Trait> traits = new List<Trait>();

            if (pioneer && recovery)
            {
                traits.Add(new Trait(TraitType.SkillMovementRecovery,
                    new TraitSource(TraitSourceKind.Specialization, (int)MercenarySpecialization.Pioneer)));
            }

            if (opportunity)
            {
                traits.Add(new Trait(TraitType.OpportunityAttack, new TraitSource(TraitSourceKind.Class, 0)));
            }

            Assert.That(battle.TrySpawnUnit(team, coordinate,
                new BattleTestUnit(MercenaryClass.Warrior, 20, 3, 2, 1, 0,
                    initiative: pioneer ? 30 : team == BattleTeam.Player ? 20 : 10),
                HexDirection.East, out HexCharacter character, new TraitCollection(traits.ToArray()),
                specialization: pioneer ? MercenarySpecialization.Pioneer : MercenarySpecialization.None), Is.True);
            return character;
        }

        static HexCoordinate Advance(HexCoordinate origin, HexDirection direction, int count)
        {
            for (int i = 0; i < count; i++)
            {
                origin = origin.GetNeighbor(direction);
            }

            return origin;
        }

        static void FinishSkill(BattleSimulation battle)
        {
            for (int i = 0; i < 6 && battle.Phase == BattleActionPhase.Moving; i++)
            {
                Assert.That(battle.TryBeginMovementStep(out _), Is.True);
                Assert.That(battle.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(battle.TrySettleAction(), Is.True);
        }

        static void MoveExternally(BattleSimulation battle, int unitId, HexCoordinate coordinate)
        {
            Assert.That((bool)Invoke(battle.Board, "TryMove", unitId, coordinate), Is.True);
        }

        static object Invoke(object target, string method, params object[] arguments)
        {
            return target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, arguments);
        }
    }
}
