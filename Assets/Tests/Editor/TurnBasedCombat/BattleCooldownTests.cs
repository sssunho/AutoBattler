using System;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleCooldownTests
    {
        [TestCase(-1)]
        [TestCase(1)]
        public void BasicAttackRejectsNonzeroCooldown(int cooldown)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Basic(cooldown));
        }

        [Test]
        public void NonbasicRejectsNegativeCooldown()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Preparation(-1));
        }

        [Test]
        public void ZeroCooldownAllowsRepeatedPreparation()
        {
            BattleSimulation battle = Create(0, out HexCharacter actor, out _);
            Assert.That(Prepare(battle, actor, SpiritType.Fire), Is.True);
            Assert.That(Prepare(battle, actor, SpiritType.Water), Is.True);
            Assert.That(actor.PreparedSpirit, Is.EqualTo(SpiritType.Water));
            Assert.That(actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.Zero);
            Assert.That(actor.CooldownVersion, Is.Zero);
        }

        [Test]
        public void ZeroApPreparationBlocksReuseAndOnlyOwnTurnReducesCooldown()
        {
            BattleSimulation battle = Create(2, out HexCharacter actor, out HexCharacter enemy);
            int ap = actor.ActionPoints;
            Assert.That(Prepare(battle, actor, SpiritType.Fire), Is.True);
            Assert.That(actor.ActionPoints, Is.EqualTo(ap));
            Assert.That(actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.EqualTo(2));
            Assert.That(actor.CooldownVersion, Is.EqualTo(1));
            Assert.That(enemy.GetRemainingCooldown(SkillType.SpiritImbuement), Is.Zero);
            Assert.That(battle.CanSelectSkill(actor.Id, SkillType.SpiritImbuement).CanUse, Is.False);
            Assert.That(Prepare(battle, actor, SpiritType.Water), Is.False);
            Assert.That(actor.PreparedSpirit, Is.EqualTo(SpiritType.Fire));
            Assert.That(battle.TryEndTurn(actor.Id), Is.True);
            Assert.That(actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.EqualTo(2));
            Assert.That(battle.TryEndTurn(enemy.Id), Is.True);
            Assert.That(actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.EqualTo(1));
            Assert.That(actor.CooldownVersion, Is.EqualTo(2));
            Assert.That(Prepare(battle, actor, SpiritType.Water), Is.False);
            Assert.That(battle.TryEndTurn(actor.Id), Is.True);
            Assert.That(battle.TryEndTurn(enemy.Id), Is.True);
            Assert.That(actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.Zero);
            Assert.That(battle.CanSelectSkill(actor.Id, SkillType.SpiritImbuement).CanUse, Is.True);
            uint version = actor.CooldownVersion;
            Assert.That(battle.TryEndTurn(actor.Id), Is.True);
            Assert.That(battle.TryEndTurn(enemy.Id), Is.True);
            Assert.That(actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.Zero);
            Assert.That(actor.CooldownVersion, Is.EqualTo(version));
        }

        [Test]
        public void NewUnitUsingSameSnapshotStartsReady()
        {
            BattleSimulation first = Create(3, out HexCharacter actor, out _);
            Assert.That(Prepare(first, actor, SpiritType.Fire), Is.True);
            BattleSimulation restarted = new BattleSimulation(8, 5);
            Assert.That(restarted.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(0, 2), actor.Stats,
                HexDirection.East, out HexCharacter replacement, skills: actor.Skills), Is.True);
            Assert.That(replacement.GetRemainingCooldown(SkillType.SpiritImbuement), Is.Zero);
            Assert.That(actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.EqualTo(3));
        }

        [Test]
        public void CancelBeforeFirstPullStepDoesNotStartCooldownAndFollowingStepsDoNotRestartIt()
        {
            BattleSimulation battle = Create(3, out HexCharacter actor, out HexCharacter target);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.True);
            battle.CancelAction();
            Assert.That(actor.GetRemainingCooldown(SkillType.PullSelf), Is.Zero);
            Assert.That(actor.CooldownVersion, Is.Zero);
            Assert.That(battle.TryPioneerSkill(actor.Id, target.Id, SkillType.PullSelf), Is.True);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(battle.TryBeginMovementStep(out _), Is.True);
                Assert.That(battle.TryCompleteMovementStep(), Is.True);
                Assert.That(actor.GetRemainingCooldown(SkillType.PullSelf), Is.EqualTo(3));
                Assert.That(actor.CooldownVersion, Is.EqualTo(1));
            }
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.CanSelectSkill(actor.Id, SkillType.PullSelf).CanUse, Is.False);
        }

        [Test]
        public void CommittedLeapStillAttacksWithoutRechargingOrPayingAgain()
        {
            BattleSimulation battle = Create(3, out HexCharacter actor, out HexCharacter target, 3);
            Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out AssassinLeapPreview preview), Is.True);
            Assert.That(battle.TryAssassinLeap(actor.Id, target.Id, preview), Is.True);
            Assert.That(actor.GetRemainingCooldown(SkillType.AssassinLeap), Is.Zero);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(actor.ActionPoints, Is.Zero);
            Assert.That(actor.GetRemainingCooldown(SkillType.AssassinLeap), Is.EqualTo(3));
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.GreaterThan(0));
            Assert.That(actor.GetRemainingCooldown(SkillType.AssassinLeap), Is.EqualTo(3));
            Assert.That(actor.CooldownVersion, Is.EqualTo(1));
        }

        [Test]
        public void PreparedOverwatchAndSpiritReactDespiteCooldownWithoutRestartingIt()
        {
            BattleSimulation battle = Create(3, out HexCharacter actor, out HexCharacter enemy);
            Assert.That(Prepare(battle, actor, SpiritType.Water), Is.True);
            Assert.That(battle.TryPrepareOverwatch(actor.Id, HexDirection.East), Is.True);
            Assert.That(actor.GetRemainingCooldown(SkillType.Overwatch), Is.EqualTo(3));
            Assert.That(actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.EqualTo(3));
            uint version = actor.CooldownVersion;
            int ap = actor.ActionPoints;
            Assert.That(battle.TryMove(enemy.Id, new HexCoordinate(5, 2)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.TryCompleteAttack(out _), Is.True);
            Assert.That(actor.PreparedSpirit, Is.EqualTo(SpiritType.None));
            Assert.That(actor.ActionPoints, Is.EqualTo(ap));
            Assert.That(actor.CooldownVersion, Is.EqualTo(version));
            Assert.That(actor.GetRemainingCooldown(SkillType.Overwatch), Is.EqualTo(3));
            Assert.That(actor.GetRemainingCooldown(SkillType.SpiritImbuement), Is.EqualTo(3));
        }

        static bool Prepare(BattleSimulation battle, HexCharacter actor, SpiritType spirit)
        {
            return battle.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement, actor.Id, spirit: spirit));
        }

        static BattleSkillDefinition Basic(int cooldown = 0)
        {
            return new BattleSkillDefinition(SkillType.BasicAttack, SkillTargetType.Unit, SkillRangeSource.Own,
                1, 6, 2, damage: 3, cooldownTurns: cooldown);
        }

        static BattleSkillDefinition Preparation(int cooldown)
        {
            return new BattleSkillDefinition(SkillType.SpiritImbuement, SkillTargetType.Self, SkillRangeSource.Own,
                0, 0, 0, cooldownTurns: cooldown);
        }

        static BattleSimulation Create(int cooldown, out HexCharacter actor, out HexCharacter target, int ap = 8)
        {
            BattleSimulation battle = new BattleSimulation(8, 5, new BattleRules(startingActionPoints: ap), 1);
            SkillSet skills = new SkillSet(Basic(), Preparation(cooldown),
                new BattleSkillDefinition(SkillType.PullSelf, SkillTargetType.Unit, SkillRangeSource.Own, 2, 6, 2, cooldownTurns: cooldown),
                new BattleSkillDefinition(SkillType.AssassinLeap, SkillTargetType.Unit, SkillRangeSource.Own, 1, 4, 3, cooldownTurns: cooldown),
                new BattleSkillDefinition(SkillType.Overwatch, SkillTargetType.Direction, SkillRangeSource.BasicAttack, 0, 0, 2, cooldownTurns: cooldown));
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(0, 2),
                new BattleUnitStats(MercenaryClass.Archer, 30, 4, 0, initiative: 20), HexDirection.East, out actor, skills: skills), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(4, 2),
                new BattleUnitStats(MercenaryClass.Warrior, 30, 4, 0), HexDirection.East, out target, skills: skills), Is.True);
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(actor.Id));
            return battle;
        }
    }
}