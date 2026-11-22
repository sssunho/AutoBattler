using System;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleFacingTests
    {
        [TestCase(1, 0, HexDirection.East)]
        [TestCase(1, -1, HexDirection.NorthEast)]
        [TestCase(0, -1, HexDirection.NorthWest)]
        [TestCase(-1, 0, HexDirection.West)]
        [TestCase(-1, 1, HexDirection.SouthWest)]
        [TestCase(0, 1, HexDirection.SouthEast)]
        [TestCase(3, -1, HexDirection.East)]
        [TestCase(3, -2, HexDirection.NorthEast)]
        public void ClosestDirectionUsesActualDisplacement(int q, int r, HexDirection expected)
        {
            HexCoordinate origin = new HexCoordinate(7, 11);
            HexCoordinate offset = new HexCoordinate(q, r);
            Assert.That(BattleFacing.GetDirection(origin, origin + offset), Is.EqualTo(expected));
            Assert.That(BattleFacing.GetDirection(default, new HexCoordinate(q * 19, r * 19)), Is.EqualTo(expected));
        }

        [TestCase(2, -1, HexDirection.East)]
        [TestCase(1, -2, HexDirection.NorthEast)]
        [TestCase(-1, -1, HexDirection.NorthWest)]
        [TestCase(-2, 1, HexDirection.West)]
        [TestCase(-1, 2, HexDirection.SouthWest)]
        [TestCase(1, 1, HexDirection.SouthEast)]
        public void ExactMidpointChoosesClockwiseDirection(int q, int r, HexDirection expected)
        {
            Assert.That(BattleFacing.GetDirection(default, new HexCoordinate(q, r)), Is.EqualTo(expected));
            Assert.That(BattleFacing.GetDirection(default, new HexCoordinate(q * 1000, r * 1000)), Is.EqualTo(expected));
        }

        [TestCase(HexDirection.East)]
        [TestCase(HexDirection.NorthEast)]
        [TestCase(HexDirection.NorthWest)]
        [TestCase(HexDirection.West)]
        [TestCase(HexDirection.SouthWest)]
        [TestCase(HexDirection.SouthEast)]
        public void EveryFacingHasOneFrontOneRearAndFourSideAdjacentTiles(HexDirection facing)
        {
            HexCoordinate defender = new HexCoordinate(8, 12);

            for (int offset = 0; offset < 6; offset++)
            {
                HexDirection direction = (HexDirection)(((int)facing + offset) % 6);
                BattleRelativeDirection expected = offset == 0 ? BattleRelativeDirection.Front
                    : offset == 3 ? BattleRelativeDirection.Rear : BattleRelativeDirection.Side;
                Assert.That(BattleFacing.Classify(facing, defender, defender.GetNeighbor(direction)),
                    Is.EqualTo(expected));
            }
        }

        [TestCase(1, 0, BattleRelativeDirection.Front)]
        [TestCase(2, 0, BattleRelativeDirection.Front)]
        [TestCase(2, -1, BattleRelativeDirection.Front)]
        [TestCase(1, 1, BattleRelativeDirection.Front)]
        [TestCase(3, -2, BattleRelativeDirection.Front)]
        [TestCase(1, 2, BattleRelativeDirection.Front)]
        [TestCase(4, -2, BattleRelativeDirection.Front)]
        [TestCase(-1, 0, BattleRelativeDirection.Rear)]
        [TestCase(-2, 0, BattleRelativeDirection.Rear)]
        [TestCase(-2, 1, BattleRelativeDirection.Rear)]
        [TestCase(-1, -1, BattleRelativeDirection.Rear)]
        [TestCase(-3, 2, BattleRelativeDirection.Rear)]
        [TestCase(-1, -2, BattleRelativeDirection.Rear)]
        [TestCase(-4, 2, BattleRelativeDirection.Rear)]
        [TestCase(1, -1, BattleRelativeDirection.Side)]
        [TestCase(0, 1, BattleRelativeDirection.Side)]
        [TestCase(-1, 1, BattleRelativeDirection.Side)]
        [TestCase(0, -1, BattleRelativeDirection.Side)]
        [TestCase(2, -2, BattleRelativeDirection.Side)]
        [TestCase(0, 2, BattleRelativeDirection.Side)]
        [TestCase(0, -2, BattleRelativeDirection.Side)]
        [TestCase(-2, 2, BattleRelativeDirection.Side)]
        [TestCase(1, -2, BattleRelativeDirection.Side)]
        [TestCase(-1, 2, BattleRelativeDirection.Side)]
        [TestCase(2, -3, BattleRelativeDirection.Side)]
        [TestCase(1, -3, BattleRelativeDirection.Side)]
        [TestCase(-2, 3, BattleRelativeDirection.Side)]
        [TestCase(-1, 3, BattleRelativeDirection.Side)]
        public void AllFacingsUseShifted120DegreeSectorsAndIncludeTheirApexAndBoundaryTiles(
            int q, int r, BattleRelativeDirection expected)
        {
            HexCoordinate defender = new HexCoordinate(17, 23);
            HexCoordinate offset = new HexCoordinate(q, r);

            for (int direction = 0; direction < 6; direction++)
            {
                Assert.That(BattleFacing.Classify((HexDirection)direction, defender, defender + offset),
                    Is.EqualTo(expected));
                HexCoordinate distant = new HexCoordinate(offset.Q * 1000, offset.R * 1000);
                Assert.That(BattleFacing.Classify((HexDirection)direction, defender, defender + distant),
                    Is.EqualTo(expected));
                offset = new HexCoordinate(offset.Q + offset.R, -offset.Q);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(7)]
        [TestCase(31)]
        public void EveryFacingPartitionsEachDistanceRingWithShiftedSectorCounts(int distance)
        {
            HexCoordinate defender = new HexCoordinate(17, 23);

            for (int direction = 0; direction < 6; direction++)
            {
                int front = 0;
                int rear = 0;
                int side = 0;

                for (int q = -distance; q <= distance; q++)
                {
                    for (int r = -distance; r <= distance; r++)
                    {
                        if (Math.Max(Math.Abs(q), Math.Max(Math.Abs(r), Math.Abs(q + r))) != distance)
                        {
                            continue;
                        }

                        HexCoordinate attacker = defender + new HexCoordinate(q, r);

                        switch (BattleFacing.Classify((HexDirection)direction, defender, attacker))
                        {
                            case BattleRelativeDirection.Front:
                                front++;
                                break;
                            case BattleRelativeDirection.Rear:
                                rear++;
                                break;
                            case BattleRelativeDirection.Side:
                                side++;
                                break;
                            default:
                                Assert.Fail("Every distinct tile must have one relative direction.");
                                break;
                        }
                    }
                }

                Assert.That(front, Is.EqualTo(2 * distance - 1));
                Assert.That(rear, Is.EqualTo(2 * distance - 1));
                Assert.That(side, Is.EqualTo(2 * distance + 2));
                Assert.That(front + rear + side, Is.EqualTo(6 * distance));
            }
        }

        [TestCase(4294967295L, 0L, BattleRelativeDirection.Front)]
        [TestCase(4294967295L, -4294967294L, BattleRelativeDirection.Front)]
        [TestCase(1L, 4294967294L, BattleRelativeDirection.Front)]
        [TestCase(-4294967295L, 0L, BattleRelativeDirection.Rear)]
        [TestCase(-4294967295L, 4294967294L, BattleRelativeDirection.Rear)]
        [TestCase(-1L, -4294967294L, BattleRelativeDirection.Rear)]
        [TestCase(4294967295L, -4294967295L, BattleRelativeDirection.Side)]
        [TestCase(0L, 4294967295L, BattleRelativeDirection.Side)]
        [TestCase(1L, -4294967295L, BattleRelativeDirection.Side)]
        public void AllFacingsClassifyExtremeCoordinateDifferencesWithoutIntegerOverflow(
            long q, long r, BattleRelativeDirection expected)
        {
            for (int direction = 0; direction < 6; direction++)
            {
                int defenderQ = q >= 0 ? int.MinValue : int.MaxValue;
                int defenderR = r >= 0 ? int.MinValue : int.MaxValue;
                HexCoordinate defender = new HexCoordinate(defenderQ, defenderR);
                HexCoordinate attacker = new HexCoordinate(checked((int)(defenderQ + q)), checked((int)(defenderR + r)));

                Assert.That(BattleFacing.Classify((HexDirection)direction, defender, attacker), Is.EqualTo(expected));

                long nextQ = q + r;
                r = -q;
                q = nextQ;
            }
        }

        [TestCase(int.MinValue, int.MaxValue, BattleRelativeDirection.Front)]
        [TestCase(int.MaxValue, int.MinValue, BattleRelativeDirection.Rear)]
        public void ExtremeDiagonalUsesLongCoordinateSum(int defenderComponent, int attackerComponent,
            BattleRelativeDirection expected)
        {
            HexCoordinate defender = new HexCoordinate(defenderComponent, defenderComponent);
            HexCoordinate attacker = new HexCoordinate(attackerComponent, attackerComponent);

            Assert.That(BattleFacing.Classify(HexDirection.East, defender, attacker), Is.EqualTo(expected));
        }

        [Test]
        public void InvalidFacingAndCoincidentTilesAreRejected()
        {
            HexCoordinate coordinate = new HexCoordinate(2, 3);
            Assert.Throws<ArgumentException>(() => BattleFacing.GetDirection(coordinate, coordinate));
            Assert.Throws<ArgumentException>(() => BattleFacing.Classify(HexDirection.East, coordinate, coordinate));
            Assert.Throws<ArgumentOutOfRangeException>(() => BattleFacing.Classify((HexDirection)999,
                coordinate, coordinate.GetNeighbor(HexDirection.East)));
        }

        [Test]
        public void MovementChangesFacingWhenStepIsIssuedAndCancellationKeepsIssuedFacing()
        {
            BattleSimulation simulation = CreateBattle(out HexCharacter player, out _);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.TryMove(player.Id, new HexCoordinate(2, 0)), Is.True);
            Assert.That(player.Facing, Is.EqualTo(HexDirection.West));
            simulation.CancelAction();
            Assert.That(player.Facing, Is.EqualTo(HexDirection.West));
            Assert.That(player.MovementRemaining, Is.EqualTo(3));

            Assert.That(simulation.TryMove(player.Id, new HexCoordinate(2, 0)), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
            Assert.That(player.Facing, Is.EqualTo(HexDirection.East));
            Assert.That(player.MovementRemaining, Is.EqualTo(2));
            simulation.CancelAction();
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(player.Facing, Is.EqualTo(HexDirection.East));
            Assert.That(player.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(player.MovementRemaining, Is.EqualTo(2));
            Assert.That(simulation.TryBeginMovementStep(out _), Is.False);
        }

        [Test]
        public void AttackChangesFacingAtIssueUsingClockwiseTieAndDamageKeepsTargetFacing()
        {
            BattleSimulation simulation = CreateBattle(out HexCharacter player, out HexCharacter enemy);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.TryAttack(player.Id, enemy.Id), Is.True);
            Assert.That(player.Facing, Is.EqualTo(HexDirection.West));
            simulation.CancelAction();
            Assert.That(player.Facing, Is.EqualTo(HexDirection.West));
            Assert.That(player.ActionPoints, Is.EqualTo(4));

            Assert.That(simulation.TryAttack(player.Id, enemy.Id), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(player.Facing, Is.EqualTo(HexDirection.SouthEast));
            Assert.That(enemy.Facing, Is.EqualTo(HexDirection.NorthWest));
            Assert.That(enemy.Health, Is.EqualTo(20));
            simulation.CancelAction();
            Assert.That(simulation.TryCompleteAttack(out _), Is.True);
            Assert.That(player.Facing, Is.EqualTo(HexDirection.SouthEast));
            Assert.That(enemy.Facing, Is.EqualTo(HexDirection.NorthWest));
            Assert.That(enemy.Health, Is.EqualTo(17));
        }

        [Test]
        public void RejectedMovementAndAttackKeepFacingAndResources()
        {
            BattleSimulation simulation = CreateBattle(out HexCharacter player, out HexCharacter enemy);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.TryMove(player.Id, new HexCoordinate(4, 0)), Is.False);
            Assert.That(simulation.TryMove(player.Id, enemy.Coordinate), Is.False);
            Assert.That(simulation.TryAttack(player.Id, player.Id), Is.False);
            Assert.That(simulation.TryBeginAttack(out _), Is.False);
            Assert.That(player.Facing, Is.EqualTo(HexDirection.West));
            Assert.That(player.ActionPoints, Is.EqualTo(4));
            Assert.That(player.MovementRemaining, Is.EqualTo(3));
        }

        static BattleSimulation CreateBattle(out HexCharacter player, out HexCharacter enemy)
        {
            BattleSimulation simulation = new BattleSimulation(5, 3, randomSeed: 7);
            BattleTestUnit stats = new BattleTestUnit(MercenaryClass.Archer, 20, 3, 3, 3, 0);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default, stats, HexDirection.West, out player), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 1), stats,
                HexDirection.NorthWest, out enemy), Is.True);
            return simulation;
        }
    }
}
