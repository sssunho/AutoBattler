using System.Collections.Generic;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleSpiritImbuementTests
    {
        [Test]
        public void PreparationOverwritesAtZeroCostAndInvalidRequestKeepsIt()
        {
            BattleSimulation battle = Create(out HexCharacter actor, out _);
            int ap = actor.ActionPoints;
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement, actor.Id, spirit: SpiritType.Fire)), Is.True);
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement, actor.Id, spirit: SpiritType.Water)), Is.True);
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement, actor.Id)), Is.False);
            battle.CancelAction();
            Assert.That(actor.PreparedSpirit, Is.EqualTo(SpiritType.Water));
            Assert.That(actor.ActionPoints, Is.EqualTo(ap));
            Assert.That(battle.TryEndTurn(actor.Id), Is.True);
            Assert.That(actor.PreparedSpirit, Is.EqualTo(SpiritType.Water));
        }

        [Test]
        public void FireCombinesBeforeDefenseAndLeavesSteamOnWetGround()
        {
            BattleSimulation battle = Create(out HexCharacter actor, out HexCharacter target);
            Assert.That(battle.TryApplyGroundEffect(new HexCoordinate(1, 1), BattleGroundEffect.Wet, out _), Is.True);
            PrepareAndFire(battle, actor, target, SpiritType.Fire);
            Assert.That(actor.PreparedSpirit, Is.EqualTo(SpiritType.None));
            List<BattleAttackResult> results = new List<BattleAttackResult>();
            Assert.That(battle.TryCompleteAttackBatch(results), Is.True);
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].DamageByAttribute.Fire, Is.EqualTo(1));
            Assert.That(battle.Board.TryGetEnvironment(new HexCoordinate(1, 1), out BattleEnvironmentState state), Is.True);
            Assert.That(state.Ground, Is.EqualTo(BattleGroundEffect.None));
            Assert.That(state.Air, Is.EqualTo(BattleAirEffect.Steam));
            Assert.That(battle.Board.TryGetEnvironment(actor.Coordinate, out BattleEnvironmentState originState), Is.True);
            Assert.That(originState.IsEmpty, Is.True);
            Assert.That(battle.TryCompleteAttackBatch(results), Is.False);
        }

        [Test]
        public void MissingFinalTargetStopsEnvironmentalPath()
        {
            BattleSimulation battle = Create(out HexCharacter actor, out HexCharacter target);
            PrepareAndFire(battle, actor, target, SpiritType.Poison);
            Assert.That(battle.RemoveUnit(target.Id), Is.True);
            List<BattleAttackResult> results = new List<BattleAttackResult>();
            Assert.That(battle.TryCompleteAttackBatch(results), Is.True);
            Assert.That(results, Is.Empty);
            Assert.That(battle.Board.TryGetEnvironment(target.Coordinate, out BattleEnvironmentState state), Is.True);
            Assert.That(state.Ground, Is.EqualTo(BattleGroundEffect.None));
        }

        [Test]
        public void HitResolvesOnceAndWaitsForPresentationCompletion()
        {
            BattleSimulation battle = Create(out HexCharacter actor, out HexCharacter target);
            PrepareAndFire(battle, actor, target, SpiritType.Water);
            List<BattleAttackResult> results = new List<BattleAttackResult>();
            int health = target.Health;
            Assert.That(battle.TryResolveAttackDamage(results, true), Is.True);
            Assert.That(target.Health, Is.LessThan(health));
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Attacking));
            Assert.That(battle.IsAttackDamageResolved, Is.True);
            health = target.Health;
            Assert.That(battle.TryResolveAttackDamage(results, true), Is.False);
            Assert.That(target.Health, Is.EqualTo(health));
            Assert.That(battle.TryFinishAttackPresentation(), Is.True);
            Assert.That(battle.TryFinishAttackPresentation(), Is.False);
        }

        [Test]
        public void ProjectileVisitsOnlyReachedCellsAndAbortPreservesSpentCost()
        {
            BattleSimulation battle = Create(out HexCharacter actor, out HexCharacter target);
            PrepareAndFire(battle, actor, target, SpiritType.Poison);
            int ap = actor.ActionPoints;
            int health = target.Health;
            HexCoordinate reached = new HexCoordinate(1, 1);
            Assert.That(battle.TryVisitAttackCell(reached), Is.True);
            Assert.That(battle.TryVisitAttackCell(reached), Is.False);
            Assert.That(battle.Board.TryGetEnvironment(reached, out BattleEnvironmentState state), Is.True);
            Assert.That(state.Ground, Is.EqualTo(BattleGroundEffect.Poisoned));
            Assert.That(battle.Board.TryGetEnvironment(target.Coordinate, out state), Is.True);
            Assert.That(state.IsEmpty, Is.True);
            Assert.That(battle.TryAbortAttack(), Is.True);
            Assert.That(battle.TryAbortAttack(), Is.False);
            Assert.That(target.Health, Is.EqualTo(health));
            Assert.That(actor.ActionPoints, Is.EqualTo(ap));
            Assert.That(actor.PreparedSpirit, Is.EqualTo(SpiritType.None));
            Assert.That(battle.IsBusy, Is.False);
        }

        [Test]
        public void ElectricHitsEachEnemyOnceAndRemovesAllDeadAfterResults()
        {
            BattleSimulation battle = Create(out HexCharacter actor, out HexCharacter target, true);
            PrepareAndFire(battle, actor, target, SpiritType.Electricity);
            List<BattleAttackResult> results = new List<BattleAttackResult>();
            Assert.That(battle.TryCompleteAttackBatch(results), Is.True);
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].DamageByAttribute.Electricity, Is.EqualTo(1));
            Assert.That(results[1].DamageByAttribute.Electricity, Is.EqualTo(1));
            Assert.That(results[1].TargetDied, Is.True);
            Assert.That(battle.Board.TryGetUnit(results[1].TargetId, out _), Is.False);
        }

        [Test]
        public void CenterLineIsDeterministicAdjacentAndExcludesOrigin()
        {
            List<HexCoordinate> first = new List<HexCoordinate>();
            List<HexCoordinate> second = new List<HexCoordinate>();
            HexCoordinate origin = new HexCoordinate(0, 0), end = new HexCoordinate(3, 3);
            BattleSpiritPath.Copy(origin, end, first);
            BattleSpiritPath.Copy(origin, end, second);
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.Count, Is.EqualTo(origin.DistanceTo(end)));
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(first[i].DistanceTo(i == 0 ? origin : first[i - 1]), Is.EqualTo(1));
            }
            Assert.That(first[first.Count - 1], Is.EqualTo(end));
        }

        [Test]
        public void OverwatchConsumesPreparedSpiritAtReactionLaunchWithoutExtraCost()
        {
            BattleSimulation battle = Create(out HexCharacter actor, out HexCharacter target);
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement, actor.Id, spirit: SpiritType.Water)), Is.True);
            Assert.That(battle.TryPrepareOverwatch(actor.Id, HexDirection.East), Is.True);
            int ap = actor.ActionPoints;
            Assert.That(battle.TryMove(target.Id, new HexCoordinate(4, 1)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(actor.PreparedSpirit, Is.EqualTo(SpiritType.None));
            List<BattleAttackResult> results = new List<BattleAttackResult>();
            Assert.That(battle.TryCompleteAttackBatch(results), Is.True);
            Assert.That(actor.ActionPoints, Is.EqualTo(ap));
            Assert.That(battle.Board.TryGetEnvironment(target.Coordinate, out BattleEnvironmentState state), Is.True);
            Assert.That(state.Ground, Is.EqualTo(BattleGroundEffect.Wet));
        }
        static BattleSimulation Create(out HexCharacter actor, out HexCharacter target, bool intermediate = false)
        {
            BattleSimulation battle = new BattleSimulation(8, 4, new BattleRules(startingActionPoints: 8), 1);
            BattleSkillDefinition basic = new BattleSkillDefinition(SkillType.BasicAttack, SkillTargetType.Unit, SkillRangeSource.Own, 1, 6, 2, damage: 3);
            SkillSet skills = new SkillSet(basic, new BattleSkillDefinition(SkillType.SpiritImbuement, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 0), new BattleSkillDefinition(SkillType.Overwatch, SkillTargetType.Direction, SkillRangeSource.BasicAttack, 0, 0, 2));
            TraitCollection traits = new TraitCollection(new[] { new Trait(TraitType.FourElementBasicAttack, new TraitSource(TraitSourceKind.Specialization, 3)) });
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(0, 1), new BattleUnitStats(MercenaryClass.Archer, 20, 3, 0, initiative: 20), HexDirection.East, out actor, traits, skills: skills), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 1), new BattleUnitStats(MercenaryClass.Warrior, 20, 3, 0, fireDefense: 1, electricityDefense: 1), HexDirection.West, out target, skills: new SkillSet(basic)), Is.True);
            if (intermediate)
            {
                Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 1), new BattleUnitStats(MercenaryClass.Warrior, 1, 3, 0), HexDirection.West, out _, skills: new SkillSet(basic)), Is.True);
            }
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(actor.Id));
            return battle;
        }

        static void PrepareAndFire(BattleSimulation battle, HexCharacter actor, HexCharacter target, SpiritType spirit)
        {
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement, actor.Id, spirit: spirit)), Is.True);
            Assert.That(battle.TryAttack(actor.Id, target.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
        }
    }
}
