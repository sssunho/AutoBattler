using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleStealthTests
    {
        [Test]
        public void DetectionIncludesBothBoundariesAtTwentyCellsButNotTwentyOneOrRear()
        {
            HexCoordinate origin = new HexCoordinate(25, 25);

            for (int direction = 0; direction < 6; direction++)
            {
                for (int offset = 0; offset < 6; offset++)
                {
                    HexCoordinate vector = default(HexCoordinate).GetNeighbor((HexDirection)((direction + offset) % 6));
                    HexCoordinate edge = new HexCoordinate(origin.Q + vector.Q * 20, origin.R + vector.R * 20);
                    HexCoordinate outside = new HexCoordinate(origin.Q + vector.Q * 21, origin.R + vector.R * 21);
                    Assert.That(BattleDetectionArea.Contains(origin, (HexDirection)direction, edge),
                        Is.EqualTo(offset == 0 || offset == 1 || offset == 5));
                    Assert.That(BattleDetectionArea.Contains(origin, (HexDirection)direction, outside), Is.False);
                }

                Assert.That(BattleDetectionArea.Contains(origin, (HexDirection)direction, origin), Is.False);
            }

            Assert.That(BattleDetectionArea.Contains(new HexCoordinate(int.MinValue, int.MinValue),
                HexDirection.East, new HexCoordinate(int.MaxValue, int.MaxValue)), Is.False);
        }

        [Test]
        public void DetectionCellsMatchThePredicateAndStayInsideTheMap()
        {
            List<HexCoordinate> cells = new List<HexCoordinate>();
            HexCoordinate origin = new HexCoordinate(2, 2);
            BattleDetectionArea.CopyCells(25, 5, origin, HexDirection.East, cells);

            for (int r = 0; r < 5; r++)
            {
                for (int q = 0; q < 25; q++)
                {
                    HexCoordinate cell = new HexCoordinate(q, r);
                    Assert.That(cells.Contains(cell), Is.EqualTo(BattleDetectionArea.Contains(origin, HexDirection.East, cell)));
                }
            }

            Assert.That(new HashSet<HexCoordinate>(cells).Count, Is.EqualTo(cells.Count));
            BattleDetectionArea.CopyCells(25, 5, origin, (HexDirection)6, cells);
            Assert.That(cells, Is.Empty);
        }

        [Test]
        public void EntrySpendsBothResourcesOnceWithoutEndingTheTurnOrChangingOccupancy()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter rogue, out HexCharacter enemy);
            BattleTestUtility.StartTurn(battle, rogue.Id);
            uint version = battle.VisibilityVersion;
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            Assert.That(rogue.IsStealthed, Is.True);
            Assert.That(rogue.ActionPoints, Is.EqualTo(6));
            Assert.That(rogue.MovementRemaining, Is.EqualTo(6));
            Assert.That(battle.CurrentUnitId, Is.EqualTo(rogue.Id));
            Assert.That(battle.IsBusy, Is.False);
            Assert.That(battle.Board.IsOccupied(rogue.Coordinate), Is.True);
            Assert.That(battle.VisibilityVersion, Is.Not.EqualTo(version));
            Assert.That(battle.IsVisibleTo(enemy.Team, rogue.Id), Is.False);
            Assert.That(battle.IsVisibleTo(rogue.Team, rogue.Id), Is.True);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.False);
            Assert.That(rogue.ActionPoints, Is.EqualTo(6));
            Assert.That(rogue.MovementRemaining, Is.EqualTo(6));
        }

        [TestCase(1, 8, HexDirection.East)]
        [TestCase(8, 1, HexDirection.East)]
        [TestCase(8, 8, HexDirection.West)]
        public void InsufficientResourcesOrDetectionRejectEntryWithoutSpending(int points, int movement, HexDirection facing)
        {
            BattleSimulation battle = new BattleSimulation(12, 12,
                new BattleRules(startingActionPoints: points), 1);
            HexCharacter rogue = Spawn(battle, BattleTeam.Player, 4, 5, movement: movement);
            Spawn(battle, BattleTeam.Enemy, 5, 5, facing: facing);
            BattleTestUtility.StartTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.False);
            Assert.That(rogue.ActionPoints, Is.EqualTo(points));
            Assert.That(rogue.MovementRemaining, Is.EqualTo(movement));
            Assert.That(rogue.IsStealthed, Is.False);
        }

        [Test]
        public void OwnTurnMovementPenaltyIsNotRefundedWhenAnAttackRevealsTheUnit()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter rogue, out HexCharacter enemy);
            BattleTestUtility.StartTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            Assert.That(battle.TryEndTurn(rogue.Id), Is.True);
            Assert.That(battle.TryEndTurn(enemy.Id), Is.True);
            Assert.That(rogue.MovementRemaining, Is.EqualTo(7));
            Assert.That(battle.TryAttack(rogue.Id, enemy.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(rogue.IsStealthed, Is.True);
            Assert.That(battle.TryCompleteAttack(out _), Is.True);
            Assert.That(rogue.IsStealthed, Is.False);
            Assert.That(rogue.MovementRemaining, Is.EqualTo(7));
        }

        [Test]
        public void HiddenMovementDoesNotReserveOpportunityButRevealedNextDeclarationDoes()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter rogue, out HexCharacter enemy,
                enemyTraits: Traits(TraitType.OpportunityAttack));
            BattleTestUtility.StartTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            MoveOne(battle, rogue.Id, new HexCoordinate(4, 6));
            Assert.That(rogue.IsStealthed, Is.True);
            Assert.That(battle.TryMove(rogue.Id, new HexCoordinate(5, 6)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(rogue.IsStealthed, Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(rogue.IsStealthed, Is.False);
            Assert.That(battle.TryMove(rogue.Id, new HexCoordinate(6, 6)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.ResolvingReactions));
            Assert.That(battle.TryBeginAttack(out HexAttackStep attack), Is.True);
            Assert.That(attack.AttackerId, Is.EqualTo(enemy.Id));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CanceledOrStoppedCommittedStepStillDetectsAtCompletion(bool stop)
        {
            BattleSimulation battle = CreateBattle(out HexCharacter rogue, out _);
            BattleTestUtility.StartTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            MoveOne(battle, rogue.Id, new HexCoordinate(4, 6));
            Assert.That(battle.TryMove(rogue.Id, new HexCoordinate(5, 6)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);

            if (stop)
            {
                battle.StopAction();
            }
            else
            {
                battle.CancelAction();
            }

            Assert.That(rogue.IsStealthed, Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(rogue.IsStealthed, Is.False);
            Assert.That(battle.IsBusy, Is.False);
        }

        [Test]
        public void HiddenDepartureFromAnOpportunityOwnerIsAllowed()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter rogue, out _,
                enemyTraits: Traits(TraitType.OpportunityAttack));
            BattleTestUtility.StartTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            MoveOne(battle, rogue.Id, new HexCoordinate(3, 5));
            Assert.That(battle.HasPendingReactions, Is.False);
            Assert.That(rogue.IsStealthed, Is.True);
        }

        [Test]
        public void MovingObserverRevealsOnlyWhenItsStepCompletes()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter rogue, out HexCharacter enemy);
            BattleTestUtility.StartTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            Assert.That(battle.TryEndTurn(rogue.Id), Is.True);
            Assert.That(battle.TryMove(enemy.Id, new HexCoordinate(4, 6)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(rogue.IsStealthed, Is.True);
            Assert.That(battle.TryMove(enemy.Id, new HexCoordinate(4, 7)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TryMove(enemy.Id, new HexCoordinate(4, 6)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(rogue.IsStealthed, Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(rogue.IsStealthed, Is.False);
        }

        [Test]
        public void ObserverAttackFacingRevealsBeforeDamage()
        {
            BattleSimulation battle = new BattleSimulation(12, 12, new BattleRules(startingActionPoints: 8), 1);
            HexCharacter hidden = Spawn(battle, BattleTeam.Player, 4, 5);
            HexCharacter visible = Spawn(battle, BattleTeam.Player, 3, 5);
            HexCharacter enemy = Spawn(battle, BattleTeam.Enemy, 5, 5, range: 3);
            BattleTestUtility.StartTurn(battle, hidden.Id);
            Assert.That(battle.TryEnterStealth(hidden.Id), Is.True);
            Assert.That(battle.TryEndTurn(hidden.Id), Is.True);
            BattleTestUtility.AdvanceToTurn(battle, enemy.Id);
            Assert.That(battle.TryAttack(enemy.Id, visible.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(hidden.IsStealthed, Is.False);
            Assert.That(visible.Health, Is.EqualTo(30));
        }

        [Test]
        public void OnlyPositiveDamageRevealsTheVictim()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter rogue, out _);
            BattleTestUtility.StartTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            MethodInfo apply = typeof(HexCharacter).GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.NonPublic);
            apply.Invoke(rogue, new object[] { 0L });
            Assert.That(rogue.IsStealthed, Is.True);
            apply.Invoke(rogue, new object[] { 1L });
            Assert.That(rogue.IsStealthed, Is.False);
        }

        [Test]
        public void AmbushAddsAttackPowerAndRearCriticalThenConsumesOncePerOwnTurn()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter rogue, out HexCharacter enemy,
                playerTraits: Traits(TraitType.RearGuaranteedCritical), enemyDefense: 1);
            BattleTestUtility.StartTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            Assert.That(battle.CanAmbush(rogue.Id, enemy.Id), Is.True);
            Assert.That(battle.TryAmbush(rogue.Id, enemy.Id), Is.True);
            Assert.That(rogue.HasUsedAmbush, Is.False);
            Assert.That(rogue.ActionPoints, Is.EqualTo(6));
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(rogue.HasUsedAmbush, Is.True);
            Assert.That(rogue.IsStealthed, Is.True);
            Assert.That(rogue.ActionPoints, Is.EqualTo(4));
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.EqualTo(6));
            Assert.That(result.IsCritical, Is.True);
            Assert.That(rogue.IsStealthed, Is.False);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            Assert.That(battle.CanSelectAmbush(rogue.Id), Is.False);
            Assert.That(battle.TryEndTurn(rogue.Id), Is.True);
            Assert.That(battle.TryEndTurn(enemy.Id), Is.True);
            Assert.That(rogue.HasUsedAmbush, Is.False);
            Assert.That(battle.CanAmbush(rogue.Id, enemy.Id), Is.True);
        }

        [Test]
        public void AmbushDoesNotApproachAndCancelBeforeIssueDoesNotSpend()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter rogue, out HexCharacter enemy);
            HexCharacter distant = Spawn(battle, BattleTeam.Enemy, 8, 5);
            BattleTestUtility.StartTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            Assert.That(battle.TryAmbush(rogue.Id, distant.Id), Is.False);
            Assert.That(battle.TryAmbush(rogue.Id, enemy.Id), Is.True);
            battle.CancelAction();
            Assert.That(rogue.HasUsedAmbush, Is.False);
            Assert.That(rogue.IsStealthed, Is.True);
            Assert.That(rogue.ActionPoints, Is.EqualTo(6));
            Assert.That(battle.TryBeginAttack(out _), Is.False);
        }

        [Test]
        public void AmbushRechecksStealthBeforeIssuingWithoutSpending()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter rogue, out HexCharacter enemy);
            BattleTestUtility.StartTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            Assert.That(battle.TryAmbush(rogue.Id, enemy.Id), Is.True);
            typeof(HexCharacter).GetMethod("ClearStealth", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(rogue, null);
            Assert.That(battle.TryBeginAttack(out _), Is.False);
            Assert.That(rogue.HasUsedAmbush, Is.False);
            Assert.That(rogue.ActionPoints, Is.EqualTo(6));
        }

        [Test]
        public void IssuedAmbushKeepsItsBonusAfterRevealAndRevealsWhenTargetDisappears()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter rogue, out HexCharacter enemy);
            Spawn(battle, BattleTeam.Enemy, 30, 30);
            BattleTestUtility.StartTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            Assert.That(battle.TryAmbush(rogue.Id, enemy.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            typeof(HexCharacter).GetMethod("ClearStealth", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(rogue, null);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.EqualTo(5));

            Assert.That(battle.TryEndTurn(rogue.Id), Is.True);
            BattleTestUtility.AdvanceToTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            Assert.That(battle.TryAmbush(rogue.Id, enemy.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.RemoveUnit(enemy.Id), Is.True);
            Assert.That(battle.TryCompleteAttack(out _), Is.False);
            Assert.That(rogue.IsStealthed, Is.False);
            Assert.That(rogue.HasUsedAmbush, Is.True);
            Assert.That(rogue.ActionPoints, Is.EqualTo(4));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ZeroPointReactionRevealsOnlyIfActuallyIssuedEvenAtZeroDamage(bool issue)
        {
            BattleSimulation battle = CreateBattle(out HexCharacter rogue, out HexCharacter enemy,
                playerTraits: Traits(TraitType.OpportunityAttack), enemyDefense: 10);
            BattleTestUtility.StartTurn(battle, rogue.Id);
            Assert.That(battle.TryEnterStealth(rogue.Id), Is.True);
            Assert.That(battle.TryEndTurn(rogue.Id), Is.True);
            Assert.That(battle.TryMove(enemy.Id, new HexCoordinate(6, 5)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);

            if (issue)
            {
                Assert.That(battle.TryBeginAttack(out _), Is.True);
                Assert.That(rogue.IsStealthed, Is.True);
                Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
                Assert.That(result.Damage, Is.Zero);
            }
            else
            {
                battle.StopAction();
            }

            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(rogue.IsStealthed, Is.EqualTo(!issue));
            Assert.That(rogue.ActionPoints, Is.EqualTo(6));
        }

        [TestCase(MercenaryClass.Warrior)]
        [TestCase(MercenaryClass.Rogue)]
        [TestCase(MercenaryClass.Archer)]
        public void AiAndDirectCommandsIgnoreTheHiddenOnlyOpponent(MercenaryClass enemyClass)
        {
            BattleSimulation battle = new BattleSimulation(12, 12, new BattleRules(startingActionPoints: 8), 1);
            HexCharacter hidden = Spawn(battle, BattleTeam.Player, 4, 5);
            HexCharacter enemy = Spawn(battle, BattleTeam.Enemy, 5, 5, unitClass: enemyClass);
            BattleTestUtility.StartTurn(battle, hidden.Id);
            Assert.That(battle.TryEnterStealth(hidden.Id), Is.True);
            Assert.That(battle.TryEndTurn(hidden.Id), Is.True);
            BattleAiDecision decision = new BattleEnemyAI().Decide(battle);
            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.EndTurn));
            Assert.That(battle.TryAttack(enemy.Id, hidden.Id), Is.False);
            Assert.That(battle.TryAttack(enemy.Id, hidden.Id, enemy.Coordinate), Is.False);
            List<HexCoordinate> movement = new List<HexCoordinate>();
            List<BattleAttackTarget> targets = new List<BattleAttackTarget>();
            Assert.That(battle.TryGetActionRange(enemy.Id, movement, targets), Is.True);
            Assert.That(targets, Is.Empty);
        }

        [Test]
        public void OverwatchPreparationRevealsBeforeEndingTheTurn()
        {
            BattleSimulation battle = new BattleSimulation(12, 12, new BattleRules(startingActionPoints: 8), 1);
            HexCharacter archer = Spawn(battle, BattleTeam.Player, 4, 5, unitClass: MercenaryClass.Archer);
            HexCharacter enemy = Spawn(battle, BattleTeam.Enemy, 5, 5);
            BattleTestUtility.StartTurn(battle, archer.Id);
            Assert.That(battle.TryEnterStealth(archer.Id), Is.True);
            Assert.That(battle.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Assert.That(archer.IsStealthed, Is.False);
            Assert.That(archer.IsOverwatching, Is.True);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(enemy.Id));
        }

        [Test]
        public void HiddenTargetInsidePreparedAreaDoesNotTriggerOverwatch()
        {
            BattleSimulation battle = new BattleSimulation(12, 12, new BattleRules(startingActionPoints: 8), 1);
            HexCharacter archer = Spawn(battle, BattleTeam.Player, 4, 5,
                unitClass: MercenaryClass.Archer, range: 3);
            HexCharacter enemy = Spawn(battle, BattleTeam.Enemy, 5, 5);
            BattleTestUtility.StartTurn(battle, archer.Id);
            Assert.That(battle.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            typeof(HexCharacter).GetMethod("SetFacing", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(archer, new object[] { HexDirection.West, true });
            Assert.That(battle.TryEnterStealth(enemy.Id), Is.True);
            MoveOne(battle, enemy.Id, new HexCoordinate(6, 5));
            Assert.That(enemy.IsStealthed, Is.True);
            Assert.That(archer.IsOverwatching, Is.True);
            Assert.That(battle.HasPendingReactions, Is.False);
        }

        [Test]
        public void ReservedOverwatchRechecksVisibilityBeforeConsumingPreparation()
        {
            BattleSimulation battle = new BattleSimulation(12, 12, new BattleRules(startingActionPoints: 8), 1);
            HexCharacter archer = Spawn(battle, BattleTeam.Player, 4, 5,
                unitClass: MercenaryClass.Archer, range: 3);
            HexCharacter enemy = Spawn(battle, BattleTeam.Enemy, 5, 5);
            BattleTestUtility.StartTurn(battle, archer.Id);
            Assert.That(battle.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Assert.That(battle.TryMove(enemy.Id, new HexCoordinate(6, 5)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            typeof(HexCharacter).GetMethod("EnterStealth", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(enemy, null);
            Assert.That(battle.TryBeginAttack(out _), Is.False);
            Assert.That(archer.IsOverwatching, Is.True);
            Assert.That(enemy.Health, Is.EqualTo(30));
            Assert.That(battle.TrySettleAction(), Is.True);
        }

        static BattleSimulation CreateBattle(out HexCharacter rogue, out HexCharacter enemy,
            TraitCollection playerTraits = null, TraitCollection enemyTraits = null, int enemyDefense = 0)
        {
            BattleSimulation battle = new BattleSimulation(40, 40, new BattleRules(startingActionPoints: 8), 1);
            rogue = Spawn(battle, BattleTeam.Player, 4, 5, traits: playerTraits);
            enemy = Spawn(battle, BattleTeam.Enemy, 5, 5, traits: enemyTraits, defense: enemyDefense);
            return battle;
        }

        static HexCharacter Spawn(BattleSimulation battle, BattleTeam team, int q, int r,
            MercenaryClass unitClass = MercenaryClass.Rogue, int movement = 8, int range = 1,
            HexDirection facing = HexDirection.East, TraitCollection traits = null, int defense = 0)
        {
            BattleTestUnit stats = new BattleTestUnit(unitClass, 30, 3, movement, range, 0,
                physicalDefense: defense, initiative: team == BattleTeam.Player ? 30 : 20);
            Assert.That(battle.TrySpawnUnit(team, new HexCoordinate(q, r), stats, facing,
                out HexCharacter unit, traits), Is.True);
            return unit;
        }

        static TraitCollection Traits(TraitType type)
        {
            return new TraitCollection(new[] { new Trait(type, new TraitSource(TraitSourceKind.Selection, 1)) });
        }

        static void MoveOne(BattleSimulation battle, int unitId, HexCoordinate coordinate)
        {
            Assert.That(battle.TryMove(unitId, coordinate), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
        }
    }
}
