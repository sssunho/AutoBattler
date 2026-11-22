using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleEnvironmentTests
    {
        static readonly HexCoordinate _coordinate = new HexCoordinate(1, 1);

        [TestCase(BattleGroundEffect.Wet, BattleGroundEffect.None, BattleAirEffect.Steam, false)]
        [TestCase(BattleGroundEffect.Wet, BattleGroundEffect.None, BattleAirEffect.Steam, true)]
        [TestCase(BattleGroundEffect.Poisoned, BattleGroundEffect.Burning, BattleAirEffect.Smoke, false)]
        [TestCase(BattleGroundEffect.Poisoned, BattleGroundEffect.Burning, BattleAirEffect.Smoke, true)]
        [TestCase(BattleGroundEffect.Electrified, BattleGroundEffect.None, BattleAirEffect.ElectricCloud, false)]
        [TestCase(BattleGroundEffect.Electrified, BattleGroundEffect.None, BattleAirEffect.ElectricCloud, true)]
        public void BurningGroundReactionsAreSymmetricAndReplaceTheAir(BattleGroundEffect other,
            BattleGroundEffect expectedGround, BattleAirEffect expectedAir, bool reverse)
        {
            BattleSimulation battle = CreateBattle();
            Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.Steam, out _), Is.True);
            ApplyGround(battle, reverse ? other : BattleGroundEffect.Burning);
            uint before = battle.Board.EnvironmentVersion;
            ApplyGround(battle, reverse ? BattleGroundEffect.Burning : other);
            AssertState(battle, expectedGround, expectedAir);
            Assert.That(battle.Board.EnvironmentVersion, Is.EqualTo(before + 1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WetAndElectricGroundPreserveExistingAirInBothOrders(bool reverse)
        {
            BattleSimulation battle = CreateBattle();
            Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.PoisonCloud, out _), Is.True);
            ApplyGround(battle, reverse ? BattleGroundEffect.Electrified : BattleGroundEffect.Wet);
            ApplyGround(battle, reverse ? BattleGroundEffect.Wet : BattleGroundEffect.Electrified);
            AssertState(battle, BattleGroundEffect.Electrified, BattleAirEffect.PoisonCloud);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BurningGroundAndPoisonCloudReactInBothOrders(bool reverse)
        {
            BattleSimulation battle = CreateBattle();

            if (reverse)
            {
                Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.PoisonCloud, out _), Is.True);
                ApplyGround(battle, BattleGroundEffect.Burning);
            }
            else
            {
                ApplyGround(battle, BattleGroundEffect.Burning);
                Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.PoisonCloud, out _), Is.True);
            }

            AssertState(battle, BattleGroundEffect.Burning, BattleAirEffect.Smoke);
        }

        [Test]
        public void SameSlotReactionWinsOverThePoisonCloudReaction()
        {
            BattleSimulation battle = CreateBattle();
            ApplyGround(battle, BattleGroundEffect.Wet);
            Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.PoisonCloud, out _), Is.True);
            ApplyGround(battle, BattleGroundEffect.Burning);
            AssertState(battle, BattleGroundEffect.None, BattleAirEffect.Steam);
        }

        [Test]
        public void UndefinedCombinationsReplaceOnlyTheirTargetSlot()
        {
            BattleSimulation battle = CreateBattle();
            ApplyGround(battle, BattleGroundEffect.Poisoned);
            Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.Smoke, out _), Is.True);
            ApplyGround(battle, BattleGroundEffect.Wet);
            AssertState(battle, BattleGroundEffect.Wet, BattleAirEffect.Smoke);
            Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.ElectricCloud, out _), Is.True);
            AssertState(battle, BattleGroundEffect.Wet, BattleAirEffect.ElectricCloud);
        }

        [Test]
        public void StableReapplicationSucceedsWithoutChangingEitherVersion()
        {
            BattleSimulation battle = CreateBattle();
            uint occupancy = battle.Board.OccupancyVersion;
            ApplyGround(battle, BattleGroundEffect.Burning);
            Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.Smoke, out _), Is.True);
            uint version = battle.Board.EnvironmentVersion;
            ApplyGround(battle, BattleGroundEffect.Burning);
            Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.Smoke, out _), Is.True);
            Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.PoisonCloud, out _), Is.True);
            AssertState(battle, BattleGroundEffect.Burning, BattleAirEffect.Smoke);
            Assert.That(battle.Board.EnvironmentVersion, Is.EqualTo(version));
            Assert.That(battle.Board.OccupancyVersion, Is.EqualTo(occupancy));
        }

        [TestCase(true, false, BattleGroundEffect.None, BattleAirEffect.Smoke)]
        [TestCase(false, true, BattleGroundEffect.Wet, BattleAirEffect.None)]
        [TestCase(true, true, BattleGroundEffect.None, BattleAirEffect.None)]
        public void ClearRemovesOnlySelectedSlots(bool ground, bool air,
            BattleGroundEffect expectedGround, BattleAirEffect expectedAir)
        {
            BattleSimulation battle = CreateBattle();
            ApplyGround(battle, BattleGroundEffect.Wet);
            Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.Smoke, out _), Is.True);
            uint before = battle.Board.EnvironmentVersion;
            Assert.That(battle.TryClearEnvironment(_coordinate, ground, air, out _), Is.True);
            AssertState(battle, expectedGround, expectedAir);
            Assert.That(battle.Board.EnvironmentVersion, Is.EqualTo(before + 1));
            Assert.That(battle.TryClearEnvironment(_coordinate, ground, air, out _), Is.True);
            Assert.That(battle.Board.EnvironmentVersion, Is.EqualTo(before + 1));
        }

        [Test]
        public void EnvironmentCopiesContainOnlyNonemptyCellsAndDoNotExposeStoredState()
        {
            BattleSimulation battle = CreateBattle();
            ApplyGround(battle, BattleGroundEffect.Poisoned);
            List<BattleEnvironmentCell> cells = new List<BattleEnvironmentCell>
            {
                new BattleEnvironmentCell(default, default)
            };
            battle.Board.CopyEnvironment(cells);
            Assert.That(cells.Count, Is.EqualTo(1));
            Assert.That(cells[0].Coordinate, Is.EqualTo(_coordinate));
            BattleEnvironmentState snapshot = cells[0].State;
            Assert.That(battle.TryClearEnvironment(_coordinate, true, true, out _), Is.True);
            Assert.That(snapshot.Ground, Is.EqualTo(BattleGroundEffect.Poisoned));
            Assert.That(battle.Board.TryGetEnvironment(_coordinate, out BattleEnvironmentState empty), Is.True);
            Assert.That(empty.IsEmpty, Is.True);
            battle.Board.CopyEnvironment(cells);
            Assert.That(cells, Is.Empty);
            Assert.Throws<ArgumentNullException>(() => battle.Board.CopyEnvironment(null));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(4, 0)]
        [TestCase(0, 4)]
        public void OutOfBoundsRequestsPreserveEnvironment(int q, int r)
        {
            BattleSimulation battle = CreateBattle();
            ApplyGround(battle, BattleGroundEffect.Wet);
            uint before = battle.Board.EnvironmentVersion;
            HexCoordinate invalid = new HexCoordinate(q, r);
            Assert.That(battle.TryApplyGroundEffect(invalid, BattleGroundEffect.Burning, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(battle.TryApplyAirEffect(invalid, BattleAirEffect.Smoke, out _), Is.False);
            Assert.That(battle.TryClearEnvironment(invalid, true, true, out _), Is.False);
            Assert.That(battle.Board.TryGetEnvironment(invalid, out _), Is.False);
            Assert.That(battle.Board.EnvironmentVersion, Is.EqualTo(before));
            AssertState(battle, BattleGroundEffect.Wet, BattleAirEffect.None);
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(5)]
        public void InvalidAndEmptyEffectRequestsDoNotMutateTheBoard(int value)
        {
            BattleSimulation battle = CreateBattle();
            ApplyGround(battle, BattleGroundEffect.Wet);
            uint version = battle.Board.EnvironmentVersion;
            Assert.That(battle.TryApplyGroundEffect(_coordinate, (BattleGroundEffect)value, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(battle.TryApplyAirEffect(_coordinate, (BattleAirEffect)value, out error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(battle.TryClearEnvironment(_coordinate, false, false, out _), Is.False);
            Assert.That(battle.Board.EnvironmentVersion, Is.EqualTo(version));
        }

        [Test]
        public void PreparingWaitingAndBusyBattlesRejectEnvironmentChanges()
        {
            BattleSimulation preparing = new BattleSimulation(4, 4);
            Assert.That(preparing.TryApplyGroundEffect(_coordinate, BattleGroundEffect.Wet, out _), Is.False);
            Assert.That(preparing.TryStartBattle(), Is.True);
            Assert.That(preparing.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(preparing.TryApplyAirEffect(_coordinate, BattleAirEffect.Smoke, out _), Is.False);
            BattleSimulation battle = CreateBattle();
            Assert.That(battle.TryMove(battle.CurrentUnitId.Value, new HexCoordinate(0, 1)), Is.True);
            Assert.That(battle.IsBusy, Is.True);
            Assert.That(battle.TryApplyGroundEffect(_coordinate, BattleGroundEffect.Wet, out _), Is.False);
            Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.Smoke, out _), Is.False);
            Assert.That(battle.TryClearEnvironment(_coordinate, true, true, out _), Is.False);
            Assert.That(battle.Board.EnvironmentVersion, Is.Zero);
        }

        [Test]
        public void CompletionKeepsEnvironmentButNewBattleStartsEmpty()
        {
            BattleSimulation battle = CreateBattle();
            ApplyGround(battle, BattleGroundEffect.Burning);
            Assert.That(battle.RemoveUnit(2), Is.True);
            Assert.That(battle.TryFinalizeResult(out _), Is.True);
            Assert.That(battle.State, Is.EqualTo(BattleState.Completed));
            AssertState(battle, BattleGroundEffect.Burning, BattleAirEffect.None);
            Assert.That(battle.TryClearEnvironment(_coordinate, true, true, out _), Is.False);
            Assert.That(battle.TryApplyGroundEffect(_coordinate, BattleGroundEffect.Wet, out _), Is.False);
            Assert.That(battle.TryApplyAirEffect(_coordinate, BattleAirEffect.Smoke, out _), Is.False);
            AssertState(CreateBattle(), BattleGroundEffect.None, BattleAirEffect.None);
        }

        [Test]
        public void EnvironmentDealsCreationAndTurnDamageWithoutExpiring()
        {
            BattleSimulation battle = CreateBattle();
            Assert.That(battle.Board.TryGetUnit(1, out HexCharacter unit), Is.True);
            int health = unit.Health;
            int ap = unit.ActionPoints;
            int movement = unit.MovementRemaining;
            uint occupancy = battle.Board.OccupancyVersion;
            uint visibility = battle.VisibilityVersion;
            Assert.That(battle.TryApplyGroundEffect(unit.Coordinate, BattleGroundEffect.Burning, out _), Is.True);
            Assert.That(battle.TryApplyAirEffect(unit.Coordinate, BattleAirEffect.Smoke, out _), Is.True);
            Assert.That(unit.Health, Is.EqualTo(health - 1));
            Assert.That(unit.ActionPoints, Is.EqualTo(ap));
            Assert.That(unit.MovementRemaining, Is.EqualTo(movement));
            Assert.That(battle.Board.OccupancyVersion, Is.EqualTo(occupancy));
            Assert.That(battle.VisibilityVersion, Is.EqualTo(visibility));

            for (int i = 0; i < 6; i++)
            {
                Assert.That(battle.TryEndTurn(battle.CurrentUnitId.Value), Is.True);
            }

            Assert.That(battle.Board.TryGetEnvironment(unit.Coordinate, out BattleEnvironmentState state), Is.True);
            Assert.That(state.Ground, Is.EqualTo(BattleGroundEffect.Burning));
            Assert.That(state.Air, Is.EqualTo(BattleAirEffect.Smoke));
            Assert.That(unit.Health, Is.EqualTo(health - 4));
        }

        [Test]
        public void EveryValidStateAndIncomingEffectResolvesToAStablePair()
        {
            Type rules = typeof(BattleSimulation).Assembly.GetType("RaggedBanner.Combat.BattleEnvironmentRules");
            Assert.That(rules, Is.Not.Null);
            MethodInfo groundMethod = rules.GetMethod("TryApplyGround", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo airMethod = rules.GetMethod("TryApplyAir", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(groundMethod, Is.Not.Null);
            Assert.That(airMethod, Is.Not.Null);
            int checkedInputs = 0;

            for (int ground = 0; ground < 5; ground++)
            {
                for (int air = 0; air < 5; air++)
                {
                    BattleEnvironmentState current = new BattleEnvironmentState((BattleGroundEffect)ground, (BattleAirEffect)air);

                    for (int incoming = 1; incoming < 5; incoming++)
                    {
                        AssertStableResolution(groundMethod, current, (BattleGroundEffect)incoming);
                        AssertStableResolution(airMethod, current, (BattleAirEffect)incoming);
                        checkedInputs += 2;
                    }
                }
            }

            Assert.That(checkedInputs, Is.EqualTo(200));
        }

        [Test]
        public void EnvironmentStateRejectsUndefinedEnumValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleEnvironmentState((BattleGroundEffect)5, BattleAirEffect.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleEnvironmentState(BattleGroundEffect.None, (BattleAirEffect)(-1)));
        }

        static void AssertStableResolution(MethodInfo method, BattleEnvironmentState current, object incoming)
        {
            object[] arguments = { current, incoming, default(BattleEnvironmentState) };
            Assert.That((bool)method.Invoke(null, arguments), Is.True);
            BattleEnvironmentState result = (BattleEnvironmentState)arguments[2];
            Assert.That((uint)result.Ground, Is.LessThan(5u));
            Assert.That((uint)result.Air, Is.LessThan(5u));
            Assert.That(result.Ground == BattleGroundEffect.Burning && result.Air == BattleAirEffect.PoisonCloud, Is.False);
        }

        static BattleSimulation CreateBattle()
        {
            BattleSimulation battle = new BattleSimulation(4, 4, randomSeed: 21);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(0, 0),
                new BattleTestUnit(MercenaryClass.Warrior, 10, 3, 3, 1, 0, initiative: 20),
                HexDirection.East, out _), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 3),
                new BattleTestUnit(MercenaryClass.Warrior, 10, 3, 3, 1, 0),
                HexDirection.West, out _), Is.True);
            Assert.That(battle.TryStartBattle(), Is.True);
            return battle;
        }

        static void ApplyGround(BattleSimulation battle, BattleGroundEffect effect)
        {
            Assert.That(battle.TryApplyGroundEffect(_coordinate, effect, out string error), Is.True, error);
        }

        static void AssertState(BattleSimulation battle, BattleGroundEffect ground, BattleAirEffect air)
        {
            Assert.That(battle.Board.TryGetEnvironment(_coordinate, out BattleEnvironmentState state), Is.True);
            Assert.That(state.Ground, Is.EqualTo(ground));
            Assert.That(state.Air, Is.EqualTo(air));
        }
    }
}
