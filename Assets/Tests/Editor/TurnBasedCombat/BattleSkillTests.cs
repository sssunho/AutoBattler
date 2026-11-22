using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleSkillTests
    {
        [Test]
        public void InputModesValidateAndChoiceRemainsSelfTargeted()
        {
            Assert.That(BasicAttack(1, 1).InputMode, Is.EqualTo(SkillInputMode.Unit));
            BattleSkillDefinition spirit = new BattleSkillDefinition(SkillType.SpiritImbuement,
                SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 0, inputMode: SkillInputMode.Choice);
            Assert.That(spirit.TargetType, Is.EqualTo(SkillTargetType.Self));
            Assert.That(spirit.InputMode, Is.EqualTo(SkillInputMode.Choice));
            Assert.Throws<ArgumentException>(() => new BattleSkillDefinition(SkillType.SpiritImbuement,
                SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 0, inputMode: SkillInputMode.Self));
            Assert.Throws<ArgumentException>(() => new BattleSkillDefinition(SkillType.BasicAttack,
                SkillTargetType.Unit, SkillRangeSource.Own, 1, 1, 2, damage: 3, inputMode: (SkillInputMode)99));
        }

        [Test]
        public void InputPreviewOwnsItsPathAndExecutionCopiesItBeforeCleanup()
        {
            BattleSimulation battle = CreateBattle(1, 1, 3, new HexCoordinate(4, 1),
                out HexCharacter actor, out HexCharacter target);
            List<HexCoordinate> path = new List<HexCoordinate>();
            SkillInputPreview preview = battle.PreviewSkill(new SkillInputQuery(SkillType.BasicAttack, actor.Id, target.Id), path);
            Assert.That(preview.CanUse, Is.True);
            Assert.That(preview.Request.Path, Is.SameAs(path));
            HexCoordinate first = path[0];
            Assert.That(battle.CanUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id)).CanUse, Is.True);
            Assert.That(path[0], Is.EqualTo(first));
            Assert.That(battle.TryUseSkill(preview.Request), Is.True);
            path.Clear();
            Assert.That(battle.TryBeginMovementStep(out HexMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(first));
        }

        [Test]
        public void InvalidInputPreviewClearsPreviousPathAndCannotConfirm()
        {
            BattleSimulation battle = CreateBattle(1, 1, 3, new HexCoordinate(4, 1),
                out HexCharacter actor, out HexCharacter target);
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(battle.PreviewSkill(new SkillInputQuery(SkillType.BasicAttack, actor.Id, target.Id), path).CanUse, Is.True);
            SkillInputPreview invalid = battle.PreviewSkill(new SkillInputQuery(SkillType.BasicAttack, actor.Id, actor.Id), path);
            Assert.That(invalid.CanUse, Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(battle.TryUseSkill(invalid.Request), Is.False);
        }

        [TestCase(1, SpiritType.Fire)]
        [TestCase(2, SpiritType.Water)]
        [TestCase(3, SpiritType.Electricity)]
        [TestCase(4, SpiritType.Poison)]
        public void ChoiceIdsResolveToEffectsOnlyInCombat(int optionId, SpiritType expected)
        {
            BattleSimulation battle = new BattleSimulation(3, 3);
            SkillSet skills = new SkillSet(BasicAttack(1, 1), new BattleSkillDefinition(SkillType.SpiritImbuement,
                SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 0));
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(0, 1),
                new BattleUnitStats(MercenaryClass.Archer, 30, 3, 0, initiative: 20), HexDirection.East,
                out HexCharacter actor, skills: skills), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 30, 0, 0), HexDirection.West,
                out _, skills: new SkillSet(BasicAttack(1, 1))), Is.True);
            Assert.That(battle.TryStartBattle(), Is.True);
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(battle.PreviewSkill(new SkillInputQuery(SkillType.SpiritImbuement, actor.Id, optionId: 5), path).CanUse, Is.False);
            SkillInputPreview preview = battle.PreviewSkill(new SkillInputQuery(SkillType.SpiritImbuement, actor.Id, optionId: optionId), path);
            Assert.That(preview.CanUse, Is.True);
            Assert.That(BattleSkillChoices.Get(SkillType.SpiritImbuement)[optionId - 1].Id, Is.EqualTo(optionId));
            Assert.That(battle.TryUseSkill(preview.Request), Is.True);
            Assert.That(actor.PreparedSpirit, Is.EqualTo(expected));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void ApproachRequestChecksItsDestinationAndCopiesThePreviewPath(int cost)
        {
            BattleSimulation battle = CreateBattle(1, 1, 3, new HexCoordinate(4, 1),
                out HexCharacter actor, out HexCharacter target, cost);
            List<HexCoordinate> path = new List<HexCoordinate>
            {
                new HexCoordinate(1, 1), new HexCoordinate(2, 1), new HexCoordinate(3, 1)
            };
            SkillUseRequest request = new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id, path: path);
            int ap = actor.ActionPoints;
            Assert.That(battle.CanUseSkill(request).CanUse, Is.True);
            Assert.That(battle.IsBusy, Is.False);
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(0, 1)));
            Assert.That(actor.ActionPoints, Is.EqualTo(ap));
            Assert.That(battle.TryUseSkill(request), Is.True);
            Assert.That(battle.TryUseSkill(request), Is.False);
            path.Clear();

            for (int q = 1; q <= 3; q++)
            {
                Assert.That(battle.TryBeginMovementStep(out HexMovementStep step), Is.True);
                Assert.That(step.To, Is.EqualTo(new HexCoordinate(q, 1)));
                Assert.That(battle.TryCompleteMovementStep(), Is.True);
                Assert.That(actor.ActionPoints, Is.EqualTo(ap));
            }

            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(actor.ActionPoints, Is.EqualTo(ap - actor.Skills.BasicAttack.ActionPointCost));
            Assert.That(battle.TryBeginAttack(out _), Is.False);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.EqualTo(5));
            Assert.That(battle.TryCompleteAttack(out _), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(ap - actor.Skills.BasicAttack.ActionPointCost));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AttackRechecksActualRangeAtStartAndAtImpact(bool afterStart)
        {
            BattleSimulation battle = CreateBattle(1, 1, 0, new HexCoordinate(1, 1),
                out HexCharacter actor, out HexCharacter target);
            int ap = actor.ActionPoints;
            int health = target.Health;
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id)), Is.True);

            if (afterStart)
            {
                Assert.That(battle.TryBeginAttack(out _), Is.True);
            }

            MoveExternally(battle, target.Id, new HexCoordinate(2, 1));

            if (afterStart)
            {
                Assert.That(battle.TryCompleteAttack(out _), Is.False);
            }
            else
            {
                Assert.That(battle.TryBeginAttack(out _), Is.False);
            }

            Assert.That(target.Health, Is.EqualTo(health));
            Assert.That(actor.ActionPoints, Is.EqualTo(ap - (afterStart ? actor.Skills.BasicAttack.ActionPointCost : 0)));
            Assert.That(battle.IsBusy, Is.False);
        }

        [Test]
        public void MinimumRangeIsUsedByRequestsAndAttackRangeQueries()
        {
            BattleSimulation battle = CreateBattle(2, 3, 0, new HexCoordinate(1, 1),
                out HexCharacter actor, out HexCharacter target);
            SkillUseRequest request = new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id);
            Assert.That(battle.CanUseSkill(request).CanUse, Is.False);
            Assert.That(battle.TryUseSkill(request), Is.False);
            List<HexCoordinate> movement = new List<HexCoordinate>();
            List<BattleAttackTarget> attacks = new List<BattleAttackTarget>();
            Assert.That(battle.TryGetActionRange(actor.Id, movement, attacks), Is.True);
            Assert.That(attacks, Is.Empty);
            MoveExternally(battle, target.Id, new HexCoordinate(2, 1));
            Assert.That(battle.CanUseSkill(request).CanUse, Is.True);
        }

        [Test]
        public void MinimumRangeAllowsRetreatToAValidAttackPosition()
        {
            BattleSimulation battle = CreateBattle(2, 2, 2, new HexCoordinate(1, 1),
                out HexCharacter actor, out HexCharacter target);
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id)), Is.True);
            int steps = 0;

            while (battle.Phase == BattleActionPhase.Moving && steps < 3)
            {
                Assert.That(battle.TryBeginMovementStep(out _), Is.True);
                Assert.That(battle.TryCompleteMovementStep(), Is.True);
                steps++;
            }

            Assert.That(steps, Is.InRange(1, 2));
            Assert.That(actor.Coordinate.DistanceTo(target.Coordinate), Is.EqualTo(2));
            Assert.That(battle.TryBeginAttack(out _), Is.True);
        }

        [Test]
        public void SkillOwnershipIsRequiredEvenWhenTheClassCouldNormallyUseIt()
        {
            BattleSimulation battle = CreateBattle(1, 1, 3, new HexCoordinate(4, 1),
                out HexCharacter actor, out _);
            Assert.That(actor.Skills.Contains(SkillType.Stealth), Is.False);
            Assert.That(battle.CanSelectSkill(actor.Id, SkillType.Stealth).CanUse, Is.False);
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.Stealth, actor.Id)), Is.False);
            Assert.That(actor.IsStealthed, Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(battle.Rules.StartingActionPoints));
        }

        [Test]
        public void CancelingAnApproachBeforeMovementDoesNotSpendAttackCost()
        {
            BattleSimulation battle = CreateBattle(1, 1, 3, new HexCoordinate(4, 1),
                out HexCharacter actor, out HexCharacter target);
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id)), Is.True);
            battle.CancelAction();
            Assert.That(actor.ActionPoints, Is.EqualTo(battle.Rules.StartingActionPoints));
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(0, 1)));
            Assert.That(battle.IsBusy, Is.False);
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id)), Is.True);
        }

        [Test]
        public void ReferencedRangeAndBasicAttackCostHaveSingleSources()
        {
            BattleSkillDefinition basic = BasicAttack(2, 8, 3);
            SkillSet skills = BattleTestSkills.ForClass(MercenaryClass.Archer, basicAttack: basic);
            skills.GetRange(SkillType.Overwatch, out int min, out int max);
            Assert.That(min, Is.EqualTo(2));
            Assert.That(max, Is.EqualTo(8));
            Assert.That(basic.ActionPointCost, Is.EqualTo(3));
            Assert.That(skills.Get(SkillType.Overwatch).ActionPointCost, Is.EqualTo(2));
            List<HexCoordinate> cells = new List<HexCoordinate>();
            BattleOverwatchArea.CopyCells(12, 5, new HexCoordinate(1, 2), HexDirection.East, max, cells, min);
            Assert.That(cells, Has.No.Member(new HexCoordinate(2, 2)));
            Assert.That(cells, Has.Member(new HexCoordinate(3, 2)));
        }

        [Test]
        public void SkillSetCopiesItsDefinitionArrayAndRejectsAmbiguousOwnership()
        {
            BattleSkillDefinition original = BasicAttack(1, 3);
            BattleSkillDefinition[] definitions = { original };
            SkillSet skills = new SkillSet(definitions);
            definitions[0] = BasicAttack(1, 8);
            Assert.That(skills.BasicAttack, Is.SameAs(original));
            Assert.Throws<ArgumentException>(() => new SkillSet(original, original));
            Assert.Throws<ArgumentException>(() => new SkillSet());
            Assert.Throws<ArgumentNullException>(() => new SkillSet(null));
        }

        [Test]
        public void EnemyAiUsesTheSkillsMinimumAndMaximumRange()
        {
            BattleSimulation battle = new BattleSimulation(6, 3, randomSeed: 1);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(0, 1),
                new BattleUnitStats(MercenaryClass.Archer, 30, 3, 0, initiative: 20), HexDirection.East,
                out HexCharacter actor, skills: new SkillSet(BasicAttack(2, 2))), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 30, 0, 0), HexDirection.West,
                out HexCharacter target, skills: new SkillSet(BasicAttack(1, 1))), Is.True);
            Assert.That(battle.TryStartBattle(), Is.True);
            BattleAiDecision decision = new BattleEnemyAI().Decide(battle);
            Assert.That(decision.Kind, Is.EqualTo(BattleAiDecisionKind.Attack));
            Assert.That(decision.Destination.DistanceTo(target.Coordinate), Is.EqualTo(2));
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id,
                target.Id, attackPosition: decision.Destination)), Is.True);
        }

        [Test]
        public void OpportunityReactionCannotIgnoreTheBasicAttacksMinimumRange()
        {
            BattleSimulation battle = new BattleSimulation(6, 3, randomSeed: 1);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 30, 2, 0, initiative: 20),
                HexDirection.East, out HexCharacter mover, skills: new SkillSet(BasicAttack(1, 1))), Is.True);
            TraitCollection traits = new TraitCollection(new[]
            {
                new Trait(TraitType.OpportunityAttack, new TraitSource(TraitSourceKind.Class, 0))
            });
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 30, 2, 0), HexDirection.East, out _, traits,
                skills: new SkillSet(BasicAttack(2, 3))), Is.True);
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(battle.TryMove(mover.Id, new HexCoordinate(3, 1)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out HexMovementStep step), Is.True);
            Assert.That(step.To, Is.EqualTo(new HexCoordinate(3, 1)));
            Assert.That(battle.HasPendingReactions, Is.False);
            Assert.That(mover.Health, Is.EqualTo(30));
        }

        [TestCase(BattleTeam.Player, 1, true)]
        [TestCase(BattleTeam.Player, 3, false)]
        [TestCase(BattleTeam.Enemy, 1, true)]
        [TestCase(BattleTeam.Enemy, 3, false)]
        public void SkillCostControlsSelectionTargetsAndEnemyDecisions(BattleTeam team, int cost, bool canAttack)
        {
            BattleSimulation battle = new BattleSimulation(6, 3, new BattleRules(startingActionPoints: 2), 1);
            Assert.That(battle.TrySpawnUnit(team, new HexCoordinate(0, 1),
                new BattleUnitStats(MercenaryClass.Archer, 30, 0, 0, initiative: 20), HexDirection.East,
                out HexCharacter actor, skills: new SkillSet(BasicAttack(1, 1, cost))), Is.True);
            BattleTeam opponent = team == BattleTeam.Player ? BattleTeam.Enemy : BattleTeam.Player;
            Assert.That(battle.TrySpawnUnit(opponent, new HexCoordinate(1, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 30, 0, 0), HexDirection.West,
                out HexCharacter target, skills: new SkillSet(BasicAttack(1, 1))), Is.True);
            Assert.That(battle.TryStartBattle(), Is.True);
            SkillUseRequest request = new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id);
            Assert.That(battle.CanSelectSkill(actor.Id, SkillType.BasicAttack).CanUse, Is.EqualTo(canAttack));
            Assert.That(battle.CanUseSkill(request).CanUse, Is.EqualTo(canAttack));
            List<HexCoordinate> movement = new List<HexCoordinate>();
            List<BattleAttackTarget> attacks = new List<BattleAttackTarget>();
            Assert.That(battle.TryGetActionRange(actor.Id, movement, attacks), Is.True);
            Assert.That(attacks.Count, Is.EqualTo(canAttack ? 1 : 0));

            if (team == BattleTeam.Enemy)
            {
                BattleAiDecision decision = new BattleEnemyAI().Decide(battle);
                Assert.That(decision.Kind, Is.EqualTo(canAttack ? BattleAiDecisionKind.Attack : BattleAiDecisionKind.EndTurn));
            }

            Assert.That(battle.TryUseSkill(request), Is.EqualTo(canAttack));

            if (canAttack)
            {
                Assert.That(battle.TryBeginAttack(out _), Is.True);
                Assert.That(actor.ActionPoints, Is.EqualTo(2 - cost));
                Assert.That(battle.TryBeginAttack(out _), Is.False);
                Assert.That(battle.TryCompleteAttack(out _), Is.True);
                Assert.That(actor.ActionPoints, Is.EqualTo(2 - cost));
            }
            else
            {
                Assert.That(actor.ActionPoints, Is.EqualTo(2));
                Assert.That(battle.IsBusy, Is.False);
            }
        }

        [Test]
        public void OpportunityAttackDoesNotChargeItsConfiguredBasicCost()
        {
            BattleSimulation battle = new BattleSimulation(6, 3, new BattleRules(startingActionPoints: 0), 1);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(2, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 30, 2, 0, initiative: 20), HexDirection.East,
                out HexCharacter mover, skills: new SkillSet(BasicAttack(1, 1))), Is.True);
            TraitCollection traits = new TraitCollection(new[]
            {
                new Trait(TraitType.OpportunityAttack, new TraitSource(TraitSourceKind.Class, 0))
            });
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(1, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 30, 2, 0), HexDirection.East,
                out HexCharacter reactor, traits, skills: new SkillSet(BasicAttack(1, 1, 3))), Is.True);
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(battle.TryMove(mover.Id, new HexCoordinate(3, 1)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(battle.HasPendingReactions, Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.IsReactionAttack, Is.True);
            Assert.That(reactor.ActionPoints, Is.Zero);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.EqualTo(5));
            Assert.That(reactor.ActionPoints, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void BasicAttackRequiresAPositiveConfiguredCost(int cost)
        {
            Assert.Catch<ArgumentException>(() => BasicAttack(1, 1, cost));
        }

        [Test]
        public void ApproachRechecksItsSkillCostBeforeTheFirstStep()
        {
            BattleSimulation battle = CreateBattle(1, 1, 3, new HexCoordinate(4, 1),
                out HexCharacter actor, out HexCharacter target, 3);
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.BasicAttack, actor.Id, target.Id)), Is.True);
            typeof(HexCharacter).GetField("_actionPoints", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(actor, 2);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(actor.Coordinate, Is.EqualTo(new HexCoordinate(0, 1)));
            Assert.That(actor.ActionPoints, Is.EqualTo(2));
            Assert.That(battle.IsBusy, Is.False);
        }

        static BattleSimulation CreateBattle(int min, int max, int movement, HexCoordinate targetCoordinate,
            out HexCharacter actor, out HexCharacter target, int cost = 2)
        {
            BattleSimulation battle = new BattleSimulation(6, 3, randomSeed: 1);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(0, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 30, movement, 0, initiative: 20),
                HexDirection.East, out actor, skills: new SkillSet(BasicAttack(min, max, cost))), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, targetCoordinate,
                new BattleUnitStats(MercenaryClass.Warrior, 30, 0, 0), HexDirection.West, out target, skills: new SkillSet(BasicAttack(1, 1))), Is.True);
            Assert.That(battle.TryStartBattle(), Is.True);
            return battle;
        }

        [Test]
        public void SkillSlotsAreExplicitImmutableAndRejectCollisions()
        {
            BattleSkillDefinition basic = BasicAttack(1, 1);
            BattleSkillDefinition stealth = new BattleSkillDefinition(SkillType.Stealth, SkillTargetType.Self,
                SkillRangeSource.Own, 0, 0, 2, movementCost: 2);
            BattleSkillDefinition invisibility = new BattleSkillDefinition(SkillType.Invisibility, SkillTargetType.Self,
                SkillRangeSource.Own, 0, 0, 2);
            BattleSkillDefinition[] definitions = { basic, stealth, invisibility };
            int[] slots = { 0, 9, 5 };
            SkillSet skills = new SkillSet(definitions, slots);
            slots[1] = 1;
            definitions[1] = invisibility;
            Assert.That(skills.TryGetSlot(9, out BattleSkillDefinition found), Is.True);
            Assert.That(found, Is.SameAs(stealth));
            Assert.That(skills.GetSlot(SkillType.Invisibility), Is.EqualTo(5));
            Assert.That(skills.TryGetSlot(1, out _), Is.False);
            Assert.That(skills.TryGetSlot(0, out _), Is.False);
            Assert.That(skills.TryGetSlot(10, out _), Is.False);
            Assert.Throws<ArgumentException>(() => new SkillSet(new[] { basic, stealth, invisibility }, new[] { 0, 1, 1 }));
            Assert.Throws<ArgumentException>(() => new SkillSet(new[] { basic, stealth }, new[] { 0, 0 }));
            Assert.Throws<ArgumentException>(() => new SkillSet(new[] { basic, stealth }, new[] { 0, 10 }));
            Assert.Throws<ArgumentException>(() => new SkillSet(new[] { basic }, new[] { 1 }));
        }

        [Test]
        public void MissingSkillSnapshotDoesNotOccupyCellOrConsumeId()
        {
            BattleSimulation battle = new BattleSimulation(3, 3);
            BattleUnitStats stats = BattleUnitStats.ForClass(MercenaryClass.Warrior);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, default, stats, HexDirection.East, out _), Is.False);
            Assert.That(battle.Board.IsOccupied(default), Is.False);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, default, stats, HexDirection.East,
                out HexCharacter actor, skills: new SkillSet(BasicAttack(1, 1))), Is.True);
            Assert.That(actor.Id, Is.EqualTo(1));
        }

        [TestCase(SkillType.Ambush)]
        [TestCase(SkillType.Overwatch)]
        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        [TestCase(SkillType.Invisibility)]
        [TestCase(SkillType.AssassinLeap)]
        public void GrantedSkillCanExecuteWithoutItsOriginalClassOrSpecialization(SkillType type)
        {
            BattleSkillDefinition skill;

            if (type == SkillType.Ambush || type == SkillType.Invisibility || type == SkillType.AssassinLeap)
            {
                skill = BattleTestSkills.ForClass(MercenaryClass.Rogue, MercenarySpecialization.Assassin).Get(type);
            }
            else if (type == SkillType.Overwatch)
            {
                skill = BattleTestSkills.ForClass(MercenaryClass.Archer).Get(type);
            }
            else
            {
                skill = BattleTestSkills.ForClass(MercenaryClass.Warrior, MercenarySpecialization.Pioneer).Get(type);
            }

            BattleSimulation battle = new BattleSimulation(8, 3);
            MercenaryClass actorClass = type == SkillType.PullSelf || type == SkillType.PullTarget
                ? MercenaryClass.Archer : MercenaryClass.Warrior;
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1),
                new BattleUnitStats(actorClass, 20, 4, 0, initiative: 100), HexDirection.East,
                out HexCharacter actor, skills: new SkillSet(new[] { BasicAttack(1, 1), skill }, new[] { 0, 7 })), Is.True);
            HexCoordinate targetCoordinate = new HexCoordinate(type == SkillType.Ambush ? 2 : 4, 1);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, targetCoordinate,
                BattleUnitStats.ForClass(MercenaryClass.Warrior), HexDirection.East, out HexCharacter target,
                skills: new SkillSet(BasicAttack(1, 1))), Is.True);
            Assert.That(battle.TryStartBattle(), Is.True);

            if (type == SkillType.Ambush)
            {
                typeof(HexCharacter).GetField("_isStealthed", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(actor, true);
            }

            Assert.That(battle.CanSelectSkill(actor.Id, type).CanUse, Is.True);
            Assert.That(actor.Traits.Has(TraitType.RearGuaranteedCritical), Is.False);
            Assert.That(actor.Traits.Has(TraitType.SkillMovementRecovery), Is.False);
            Assert.That(actor.Specialization, Is.EqualTo(MercenarySpecialization.None));

            if (type == SkillType.AssassinLeap)
            {
                Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out AssassinLeapPreview preview), Is.True);
                Assert.That(battle.TryAssassinLeap(actor.Id, target.Id, preview), Is.True);
            }
            else
            {
                int targetId = skill.TargetType == SkillTargetType.Unit ? target.Id : 0;
                Assert.That(battle.TryUseSkill(new SkillUseRequest(type, actor.Id, targetId)), Is.True);
            }

            if (type == SkillType.PullSelf || type == SkillType.PullTarget || type == SkillType.AssassinLeap)
            {
                Assert.That(battle.TryBeginMovementStep(out _), Is.True);
                Assert.That(actor.ActionPoints, Is.EqualTo(4 - skill.ActionPointCost));
            }
            else if (type == SkillType.Ambush)
            {
                Assert.That(battle.TryBeginAttack(out _), Is.True);
                Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
                Assert.That(result.IsCritical, Is.False);
            }
        }

        static BattleSkillDefinition BasicAttack(int min, int max, int cost = 2)
        {
            return new BattleSkillDefinition(SkillType.BasicAttack, SkillTargetType.Unit,
                SkillRangeSource.Own, min, max, cost, damage: 5);
        }

        static void MoveExternally(BattleSimulation battle, int unitId, HexCoordinate coordinate)
        {
            MethodInfo method = typeof(BattleBoard).GetMethod("TryMove", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That((bool)method.Invoke(battle.Board, new object[] { unitId, coordinate }), Is.True);
        }
    }
}
