using System.Collections.Generic;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleSmokeTests
    {
        [TestCase(1, true)]
        [TestCase(3, false)]
        public void TargetSmokeUsesSkillMaximumRangeEvenForAdjacentTarget(int maximumRange, bool allowed)
        {
            BattleSimulation battle = CreateBattle(maximumRange, 0, new HexCoordinate(1, 1),
                out HexCharacter actor, out HexCharacter target);
            ApplySmoke(battle, target.Coordinate);
            Assert.That(battle.HasSkillLineOfSight(actor, SkillType.BasicAttack, actor.Coordinate, target.Coordinate), Is.EqualTo(allowed));
            Assert.That(battle.CanUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id)).CanUse, Is.EqualTo(allowed));
        }

        [Test]
        public void SourceSmokeIsIgnoredButIntermediateAndTargetSmokeBlock()
        {
            BattleSimulation battle = CreateBattle(3, 0, new HexCoordinate(3, 1),
                out HexCharacter actor, out HexCharacter target);
            ApplySmoke(battle, actor.Coordinate);
            Assert.That(battle.CanUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id)).CanUse, Is.True);
            ApplySmoke(battle, new HexCoordinate(1, 1));
            Assert.That(battle.CanUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id)).CanUse, Is.False);
            Assert.That(battle.TryClearEnvironment(new HexCoordinate(1, 1), false, true, out _), Is.True);
            ApplySmoke(battle, target.Coordinate);
            Assert.That(battle.CanUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id)).CanUse, Is.False);
        }

        [Test]
        public void ApproachPreviewFindsAnotherLegalFiringPosition()
        {
            BattleSimulation battle = CreateBattle(3, 1, new HexCoordinate(3, 1),
                out HexCharacter actor, out HexCharacter target);
            ApplySmoke(battle, new HexCoordinate(1, 1));
            List<HexCoordinate> path = new List<HexCoordinate>();
            SkillInputPreview preview = battle.PreviewSkill(new SkillInputQuery(SkillType.BasicAttack, actor.Id, target.Id), path);
            Assert.That(preview.CanUse, Is.True);
            Assert.That(path.Count, Is.EqualTo(1));
            Assert.That(battle.HasSkillLineOfSight(actor, SkillType.BasicAttack, path[0], target.Coordinate), Is.True);
            Assert.That(battle.CanUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id,
                attackPosition: actor.Coordinate)).CanUse, Is.False);
            Assert.That(battle.TryUseSkill(preview.Request), Is.True);
        }

        [Test]
        public void SmokeAtTargetInvalidatesSavedPreviewWithoutSpendingResources()
        {
            BattleSimulation battle = CreateBattle(3, 1, new HexCoordinate(3, 1),
                out HexCharacter actor, out HexCharacter target);
            List<HexCoordinate> path = new List<HexCoordinate>();
            SkillInputPreview preview = battle.PreviewSkill(new SkillInputQuery(SkillType.BasicAttack, actor.Id, target.Id), path);
            Assert.That(preview.CanUse, Is.True);
            int ap = actor.ActionPoints;
            ApplySmoke(battle, target.Coordinate);
            Assert.That(battle.TryUseSkill(preview.Request), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(ap));
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void PullSkillsCannotTargetThroughSmoke(SkillType skill)
        {
            BattleSimulation battle = new BattleSimulation(6, 4, randomSeed: 7);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, MercenaryClass.Warrior,
                new HexCoordinate(0, 1), 1, 3, 100, MercenarySpecialization.Pioneer);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, MercenaryClass.Warrior,
                new HexCoordinate(3, 1), 1, 3, 0);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.CanUseSkill(new SkillUseRequest(skill, actor.Id, target.Id)).CanUse, Is.True);
            ApplySmoke(battle, new HexCoordinate(1, 1));
            Assert.That(battle.CanUseSkill(new SkillUseRequest(skill, actor.Id, target.Id)).CanUse, Is.False);
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(battle.PreviewSkill(new SkillInputQuery(skill, actor.Id, target.Id), path).CanUse, Is.False);
            Assert.That(path, Is.Empty);
        }

        [Test]
        public void AssassinLeapCannotTargetSmokeEvenWithMeleeBasicAttack()
        {
            BattleSimulation battle = new BattleSimulation(6, 4, randomSeed: 7);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, MercenaryClass.Rogue,
                new HexCoordinate(0, 1), 1, 3, 100, MercenarySpecialization.Assassin);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, MercenaryClass.Warrior,
                new HexCoordinate(3, 1), 1, 3, 0);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out _), Is.True);
            ApplySmoke(battle, target.Coordinate);
            Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out _), Is.False);
        }

        [Test]
        public void EnemyChoosesALegalAlternativeInsteadOfAttackingThroughSmoke()
        {
            BattleSimulation battle = new BattleSimulation(6, 4, randomSeed: 7);
            HexCharacter actor = Spawn(battle, BattleTeam.Enemy, MercenaryClass.Archer,
                new HexCoordinate(0, 1), 3, 1, 100);
            HexCharacter target = Spawn(battle, BattleTeam.Player, MercenaryClass.Warrior,
                new HexCoordinate(3, 1), 1, 0, 0);
            BattleTestUtility.StartTurn(battle, actor.Id);
            ApplySmoke(battle, new HexCoordinate(1, 1));
            BattleAiDecision decision = new BattleEnemyAI().Decide(battle);
            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(decision.Destination, Is.Not.EqualTo(actor.Coordinate));
            Assert.That(battle.HasSkillLineOfSight(actor, SkillType.BasicAttack, decision.Destination, target.Coordinate), Is.True);
        }

        [Test]
        public void RangedOpportunityAttackCannotTargetMoverInsideSmoke()
        {
            BattleSimulation battle = new BattleSimulation(6, 4, randomSeed: 7);
            HexCharacter mover = Spawn(battle, BattleTeam.Player, MercenaryClass.Warrior,
                new HexCoordinate(1, 1), 1, 3, 100);
            TraitCollection traits = new TraitCollection(new[] { new Trait(TraitType.OpportunityAttack,
                new TraitSource(TraitSourceKind.Class, (int)MercenaryClass.Archer)) });
            Spawn(battle, BattleTeam.Enemy, MercenaryClass.Archer, new HexCoordinate(0, 1), 3, 3, 0, traits: traits);
            BattleTestUtility.StartTurn(battle, mover.Id);
            ApplySmoke(battle, mover.Coordinate);
            Assert.That(battle.TryMove(mover.Id, new HexCoordinate(2, 1)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.HasPendingReactions, Is.False);
        }

        [Test]
        public void OverwatchPreparationIsAllowedButReactionCannotTargetSmoke()
        {
            BattleSimulation battle = new BattleSimulation(6, 4, randomSeed: 7);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, MercenaryClass.Archer,
                new HexCoordinate(0, 1), 3, 3, 100);
            HexCharacter mover = Spawn(battle, BattleTeam.Enemy, MercenaryClass.Warrior,
                new HexCoordinate(2, 1), 1, 3, 0);
            BattleTestUtility.StartTurn(battle, actor.Id);
            ApplySmoke(battle, mover.Coordinate);
            Assert.That(battle.TryPrepareOverwatch(actor.Id, HexDirection.East), Is.True);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(mover.Id));
            Assert.That(battle.TryMove(mover.Id, new HexCoordinate(3, 1)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.HasPendingReactions, Is.False);
            Assert.That(actor.IsOverwatching, Is.True);
        }

        [Test]
        public void AmbushUsesInheritedBasicAttackRangeForSmoke()
        {
            BattleSimulation battle = new BattleSimulation(6, 4, randomSeed: 7);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, MercenaryClass.Rogue,
                new HexCoordinate(0, 1), 3, 3, 100);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, MercenaryClass.Warrior,
                new HexCoordinate(2, 1), 1, 3, 0);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryEnterStealth(actor.Id), Is.True);
            Assert.That(battle.CanAmbush(actor.Id, target.Id), Is.True);
            ApplySmoke(battle, target.Coordinate);
            Assert.That(battle.CanAmbush(actor.Id, target.Id), Is.False);
        }

        [Test]
        public void SmokeAddedAfterAttackIssueDoesNotCancelTheAttack()
        {
            BattleSimulation battle = CreateBattle(3, 0, new HexCoordinate(3, 1),
                out HexCharacter actor, out HexCharacter target);
            Assert.That(battle.TryAttack(actor.Id, target.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            int health = target.Health;
            var setter = typeof(BattleBoard).GetMethod("TrySetEnvironment",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            setter.Invoke(battle.Board, new object[] { target.Coordinate,
                new BattleEnvironmentState(BattleGroundEffect.None, BattleAirEffect.Smoke) });
            Assert.That(battle.TryCompleteAttack(out _), Is.True);
            Assert.That(target.Health, Is.LessThan(health));
        }

        static BattleSimulation CreateBattle(int maximumRange, int movement, HexCoordinate targetPosition,
            out HexCharacter actor, out HexCharacter target)
        {
            BattleSimulation battle = new BattleSimulation(6, 4, randomSeed: 7);
            actor = Spawn(battle, BattleTeam.Player, MercenaryClass.Archer, new HexCoordinate(0, 1), maximumRange, movement, 100);
            target = Spawn(battle, BattleTeam.Enemy, MercenaryClass.Warrior, targetPosition, 1, 0, 0);
            BattleTestUtility.StartTurn(battle, actor.Id);
            return battle;
        }

        static HexCharacter Spawn(BattleSimulation battle, BattleTeam team, MercenaryClass unitClass,
            HexCoordinate coordinate, int maximumRange, int movement, int initiative,
            MercenarySpecialization specialization = MercenarySpecialization.None, TraitCollection traits = null)
        {
            BattleSkillDefinition basic = new BattleSkillDefinition(SkillType.BasicAttack, SkillTargetType.Unit,
                SkillRangeSource.Own, 1, maximumRange, 2, damage: 3);
            Assert.That(battle.TrySpawnUnit(team, coordinate, new BattleUnitStats(unitClass, 30, movement, 0, initiative: initiative),
                HexDirection.East, out HexCharacter unit, traits: traits, specialization: specialization,
                skills: BattleTestSkills.ForClass(unitClass, specialization, basic)), Is.True);
            return unit;
        }

        static void ApplySmoke(BattleSimulation battle, HexCoordinate coordinate)
        {
            Assert.That(battle.TryApplyAirEffect(coordinate, BattleAirEffect.Smoke, out _), Is.True);
        }
    }
}
