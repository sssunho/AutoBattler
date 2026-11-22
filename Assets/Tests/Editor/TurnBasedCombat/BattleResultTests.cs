using NUnit.Framework;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleResultTests
    {
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void MissingInitialTeamNeverProducesAResult(bool player, bool enemy)
        {
            BattleSimulation simulation = new BattleSimulation(4, 4, randomSeed: 3);

            if (player)
            {
                Spawn(simulation, BattleTeam.Player, 0, 0);
            }

            if (enemy)
            {
                Spawn(simulation, BattleTeam.Enemy, 3, 3);
            }

            Assert.That(simulation.TryFinalizeResult(out BattleResult beforeStart), Is.False);
            Assert.That(beforeStart, Is.Null);
            Assert.That(simulation.TryStartBattle(), Is.True);
            Assert.That(simulation.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(simulation.TryFinalizeResult(out BattleResult afterStart), Is.False);
            Assert.That(afterStart, Is.Null);
            Assert.That(simulation.Result, Is.Null);
        }

        [TestCase(BattleOutcome.PlayerVictory, false, true)]
        [TestCase(BattleOutcome.PlayerDefeat, true, false)]
        [TestCase(BattleOutcome.Draw, true, true)]
        public void OneFinalizationRecordsOutcomeRoundAndDeployedCounts(BattleOutcome outcome,
            bool removePlayers, bool removeEnemies)
        {
            BattleSimulation simulation = new BattleSimulation(4, 4, randomSeed: 7);
            HexCharacter player = Spawn(simulation, BattleTeam.Player, 0, 0);
            HexCharacter secondPlayer = Spawn(simulation, BattleTeam.Player, 0, 1);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 3, 3);
            HexCharacter secondEnemy = Spawn(simulation, BattleTeam.Enemy, 3, 2);
            Assert.That(simulation.TryStartBattle(), Is.True);
            Assert.That(simulation.TryFinalizeResult(out _), Is.False);

            if (removePlayers)
            {
                Assert.That(simulation.RemoveUnit(player.Id), Is.True);
                Assert.That(simulation.RemoveUnit(secondPlayer.Id), Is.True);
            }

            if (removeEnemies)
            {
                Assert.That(simulation.RemoveUnit(enemy.Id), Is.True);
                Assert.That(simulation.RemoveUnit(secondEnemy.Id), Is.True);
            }

            int round = simulation.RoundNumber;
            Assert.That(simulation.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(simulation.Result, Is.Null);
            SettleRemovedTurn(simulation);
            Assert.That(simulation.TryFinalizeResult(out BattleResult result), Is.True);
            Assert.That(result, Is.SameAs(simulation.Result));
            Assert.That(result.Outcome, Is.EqualTo(outcome));
            Assert.That(result.RoundNumber, Is.EqualTo(round));
            Assert.That(result.InitialPlayerCount, Is.EqualTo(2));
            Assert.That(result.InitialEnemyCount, Is.EqualTo(2));
            Assert.That(result.RemainingPlayerCount, Is.EqualTo(removePlayers ? 0 : 2));
            Assert.That(result.RemainingEnemyCount, Is.EqualTo(removeEnemies ? 0 : 2));
            Assert.That(simulation.State, Is.EqualTo(BattleState.Completed));
            Assert.That(simulation.CurrentUnitId, Is.Null);
            Assert.That(simulation.TryFinalizeResult(out BattleResult repeated), Is.False);
            Assert.That(repeated, Is.Null);
            Assert.That(simulation.Result, Is.SameAs(result));
        }

        [Test]
        public void InitialCountsExcludeParticipantsRemovedBeforeNormalStart()
        {
            BattleSimulation simulation = new BattleSimulation(4, 4, randomSeed: 1);
            Spawn(simulation, BattleTeam.Player, 0, 0);
            HexCharacter removedBeforeStart = Spawn(simulation, BattleTeam.Player, 1, 0);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 3, 3);
            Assert.That(simulation.RemoveUnit(removedBeforeStart.Id), Is.True);
            Assert.That(simulation.TryStartBattle(), Is.True);
            Assert.That(simulation.RemoveUnit(enemy.Id), Is.True);

            SettleRemovedTurn(simulation);
            Assert.That(simulation.TryFinalizeResult(out BattleResult result), Is.True);
            Assert.That(result.InitialPlayerCount, Is.EqualTo(1));
            Assert.That(result.InitialEnemyCount, Is.EqualTo(1));
        }

        [Test]
        public void IssuedMovementMustBeCompletedBeforeFinalization()
        {
            BattleSimulation simulation = new BattleSimulation(4, 4, randomSeed: 2);
            HexCharacter player = Spawn(simulation, BattleTeam.Player, 0, 0);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 3, 3);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.TryMove(player.Id, new HexCoordinate(1, 0)), Is.True);
            Assert.That(simulation.TryBeginMovementStep(out _), Is.True);
            Assert.That(simulation.RemoveUnit(enemy.Id), Is.True);

            Assert.That(simulation.IsWaitingForMovementCompletion, Is.True);
            Assert.That(simulation.TryFinalizeResult(out _), Is.False);
            Assert.That(simulation.TryCompleteMovementStep(), Is.True);
            Assert.That(simulation.TryFinalizeResult(out BattleResult result), Is.True);
            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void RemovedAttackTargetMustStillFinishItsIssuedActionBeforeFinalization()
        {
            BattleSimulation simulation = new BattleSimulation(4, 4, randomSeed: 2);
            HexCharacter player = Spawn(simulation, BattleTeam.Player, 0, 0);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 1, 0);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.TryAttack(player.Id, enemy.Id), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.RemoveUnit(enemy.Id), Is.True);

            Assert.That(simulation.State, Is.EqualTo(BattleState.Waiting));
            Assert.That(simulation.IsBusy, Is.True);
            Assert.That(simulation.TryFinalizeResult(out _), Is.False);
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
            Assert.That(simulation.TryFinalizeResult(out BattleResult result), Is.True);
            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void LethalAttackProducesOneResultOnlyWhenExplicitlyFinalized()
        {
            BattleSimulation simulation = new BattleSimulation(4, 4, randomSeed: 2);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(0, 0),
                BattleTestUtility.CreateStats(attackDamage: 10), HexDirection.East, out HexCharacter player), Is.True);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 1, 0);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.TryAttack(player.Id, enemy.Id), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult attack), Is.True);

            Assert.That(attack.TargetDied, Is.True);
            Assert.That(simulation.Result, Is.Null);
            Assert.That(simulation.TryFinalizeResult(out BattleResult result), Is.True);
            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(result.RemainingPlayerCount, Is.EqualTo(1));
            Assert.That(result.RemainingEnemyCount, Is.Zero);
            Assert.That(simulation.TryCompleteAttack(out _), Is.False);
        }

        [Test]
        public void CompletedBattleRejectsActionsAndCleanupCannotChangeItsResult()
        {
            BattleSimulation simulation = new BattleSimulation(4, 4, randomSeed: 2);
            HexCharacter player = Spawn(simulation, BattleTeam.Player, 0, 0);
            HexCharacter enemy = Spawn(simulation, BattleTeam.Enemy, 1, 0);
            BattleTestUtility.StartTurn(simulation, player.Id);
            Assert.That(simulation.RemoveUnit(enemy.Id), Is.True);
            Assert.That(simulation.TryFinalizeResult(out BattleResult result), Is.True);

            Assert.That(simulation.TryStartBattle(), Is.False);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 0),
                BattleTestUtility.CreateStats(), HexDirection.West, out _), Is.False);
            Assert.That(simulation.TryMove(player.Id, new HexCoordinate(1, 0)), Is.False);
            Assert.That(simulation.TryAttack(player.Id, enemy.Id), Is.False);
            Assert.That(simulation.TryEndTurn(player.Id), Is.False);
            Assert.That(simulation.PauseMovement(), Is.False);
            Assert.That(simulation.ResumeMovement(), Is.False);
            Assert.That(simulation.RemoveUnit(player.Id), Is.True);
            Assert.That(simulation.Result, Is.SameAs(result));
            Assert.That(result.RemainingPlayerCount, Is.EqualTo(1));
            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(simulation.State, Is.EqualTo(BattleState.Completed));
        }

        static void SettleRemovedTurn(BattleSimulation simulation)
        {
            if (simulation.IsBusy)
            {
                Assert.That(simulation.Phase, Is.EqualTo(BattleActionPhase.Settling));
                Assert.That(simulation.TryFinalizeResult(out _), Is.False);
                Assert.That(simulation.TrySettleAction(), Is.True);
            }
        }

        static HexCharacter Spawn(BattleSimulation simulation, BattleTeam team, int q, int r)
        {
            Assert.That(simulation.TrySpawnUnit(team, new HexCoordinate(q, r),
                BattleTestUtility.CreateStats(), HexDirection.East, out HexCharacter character), Is.True);
            return character;
        }
    }
}
