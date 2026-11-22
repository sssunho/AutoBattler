using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleEnvironmentDamageTests
    {
        [TestCase(BattleTeam.Player)]
        [TestCase(BattleTeam.Enemy)]
        public void CreationDamagesBothTeamsButIdenticalReapplicationDoesNot(BattleTeam team)
        {
            BattleSimulation battle = NewBattle();
            HexCharacter player = Spawn(battle, BattleTeam.Player, new HexCoordinate(0, 1), initiative: 20);
            HexCharacter enemy = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 1));
            HexCharacter unit = team == BattleTeam.Player ? player : enemy;
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(battle.TryApplyGroundEffect(unit.Coordinate, BattleGroundEffect.Burning, out _), Is.True);
            Assert.That(unit.Health, Is.EqualTo(9));
            Assert.That(battle.TryApplyGroundEffect(unit.Coordinate, BattleGroundEffect.Burning, out _), Is.True);
            Assert.That(unit.Health, Is.EqualTo(9));
            Assert.That(battle.TryDequeueDamage(out BattleDamageEvent damage), Is.True);
            Assert.That(damage.Origin, Is.EqualTo(BattleDamageOrigin.Environment));
            Assert.That(damage.Coordinate, Is.EqualTo(unit.Coordinate));
            Assert.That(damage.Result.IsCritical, Is.False);
            Assert.That(damage.Result.WasNegated, Is.False);
            Assert.That(battle.HasPendingDamage, Is.False);
        }

        [Test]
        public void CreationOnlyDamagesChangedSlotsAndTurnDamageCombinesMatchingAttributes()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter player = Spawn(battle, BattleTeam.Player, new HexCoordinate(0, 1), poisonDefense: 1, initiative: 20);
            HexCharacter enemy = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 1));
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(battle.TryApplyGroundEffect(player.Coordinate, BattleGroundEffect.Poisoned, out _), Is.True);
            Assert.That(battle.TryApplyAirEffect(player.Coordinate, BattleAirEffect.PoisonCloud, out _), Is.True);
            Assert.That(player.Health, Is.EqualTo(10));
            battle.DiscardPendingDamage();
            Assert.That(battle.TryEndTurn(player.Id), Is.True);
            Assert.That(battle.TryEndTurn(enemy.Id), Is.True);
            Assert.That(player.Health, Is.EqualTo(9));
            Assert.That(battle.TryDequeueDamage(out BattleDamageEvent damage), Is.True);
            Assert.That(damage.Result.DamageByAttribute.Poison, Is.EqualTo(1));
            Assert.That(battle.HasPendingDamage, Is.False);
        }

        [TestCase(BattleGroundEffect.Wet, BattleAirEffect.Steam, 0)]
        [TestCase(BattleGroundEffect.None, BattleAirEffect.Smoke, 0)]
        [TestCase(BattleGroundEffect.Burning, BattleAirEffect.None, 1)]
        [TestCase(BattleGroundEffect.Poisoned, BattleAirEffect.None, 1)]
        [TestCase(BattleGroundEffect.None, BattleAirEffect.PoisonCloud, 1)]
        [TestCase(BattleGroundEffect.Electrified, BattleAirEffect.None, 1)]
        [TestCase(BattleGroundEffect.None, BattleAirEffect.ElectricCloud, 1)]
        public void TurnStartAppliesOnlyTheDefinedDamagingEffects(BattleGroundEffect ground, BattleAirEffect air, int expected)
        {
            BattleSimulation battle = NewBattle();
            HexCharacter player = Spawn(battle, BattleTeam.Player, new HexCoordinate(0, 1), initiative: 20);
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 1));
            SetEnvironment(battle, player.Coordinate, ground, air);
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(player.Health, Is.EqualTo(10 - expected));
            Assert.That(battle.HasPendingDamage, Is.EqualTo(expected > 0));
        }

        [Test]
        public void OnlyActualEnvironmentDamageRevealsInvisibility()
        {
            BattleSimulation battle = NewBattle();
            TraitCollection traits = new TraitCollection(new[]
            {
                new Trait(TraitType.BattleStartInvisibility, new TraitSource(TraitSourceKind.Specialization, 2))
            });
            HexCharacter player = Spawn(battle, BattleTeam.Player, new HexCoordinate(0, 1), poisonDefense: 1,
                initiative: 20, traits: traits);
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(4, 1));
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(player.IsInvisible, Is.True);
            uint visibility = battle.VisibilityVersion;
            Assert.That(battle.TryApplyGroundEffect(player.Coordinate, BattleGroundEffect.Poisoned, out _), Is.True);
            Assert.That(player.IsInvisible, Is.True);
            Assert.That(battle.VisibilityVersion, Is.EqualTo(visibility));
            Assert.That(battle.TryApplyGroundEffect(player.Coordinate, BattleGroundEffect.Burning, out _), Is.True);
            Assert.That(player.IsStealthed, Is.False);
            Assert.That(battle.VisibilityVersion, Is.EqualTo(visibility + 1));
        }

        [Test]
        public void EntryDamageOccursOnlyWhenTheIssuedStepCompletes()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter player = Spawn(battle, BattleTeam.Player, new HexCoordinate(0, 1), initiative: 20);
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(5, 1));
            Assert.That(battle.TryStartBattle(), Is.True);
            HexCoordinate destination = new HexCoordinate(1, 1);
            Assert.That(battle.TryApplyGroundEffect(destination, BattleGroundEffect.Burning, out _), Is.True);
            Assert.That(battle.TryMove(player.Id, destination), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(player.Health, Is.EqualTo(10));
            Assert.That(battle.HasPendingDamage, Is.False);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(player.Health, Is.EqualTo(9));
            Assert.That(battle.TryCompleteMovementStep(), Is.False);
            Assert.That(player.Health, Is.EqualTo(9));
        }

        [TestCase(SkillType.PullSelf)]
        [TestCase(SkillType.PullTarget)]
        public void LethalPullEntryStopsTheRemainingPathWithoutRefund(SkillType skill)
        {
            BattleSimulation battle = NewBattle();
            SkillSet skills = BattleTestSkills.ForClass(MercenaryClass.Warrior, MercenarySpecialization.Pioneer);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(0, 1),
                health: skill == SkillType.PullSelf ? 1 : 10, initiative: 20, skills: skills,
                specialization: MercenarySpecialization.Pioneer);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(3, 1),
                health: skill == SkillType.PullTarget ? 1 : 10);
            Assert.That(battle.TryStartBattle(), Is.True);
            HexCoordinate first = skill == SkillType.PullSelf ? new HexCoordinate(1, 1) : new HexCoordinate(2, 1);
            Assert.That(battle.TryApplyGroundEffect(first, BattleGroundEffect.Burning, out _), Is.True);
            Assert.That(battle.TryPullSkill(actor.Id, target.Id, skill), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            int spentAp = actor.ActionPoints;
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            HexCharacter moved = skill == SkillType.PullSelf ? actor : target;
            Assert.That(moved.Coordinate, Is.EqualTo(first));
            Assert.That(moved.Health, Is.Zero);
            Assert.That(battle.Board.TryGetUnit(moved.Id, out _), Is.False);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(actor.ActionPoints, Is.EqualTo(spentAp));
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LeapOnlyReceivesLandingDamageAndCannotAttackAfterLethalLanding(bool lethal)
        {
            BattleSimulation battle = NewBattle();
            SkillSet skills = BattleTestSkills.ForClass(MercenaryClass.Rogue, MercenarySpecialization.Assassin);
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(0, 1), health: lethal ? 1 : 10,
                initiative: 20, unitClass: MercenaryClass.Rogue, skills: skills, specialization: MercenarySpecialization.Assassin);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(3, 1));
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(battle.TryApplyGroundEffect(new HexCoordinate(1, 1), BattleGroundEffect.Burning, out _), Is.True);
            Assert.That(battle.TryApplyGroundEffect(new HexCoordinate(2, 1), BattleGroundEffect.Poisoned, out _), Is.True);
            Assert.That(battle.TryGetAssassinLeapPreview(actor.Id, target.Id, out AssassinLeapPreview preview), Is.True);
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.AssassinLeap, actor.Id, target.Id, leapPreview: preview)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.True);
            Assert.That(battle.TryCompleteMovementStep(), Is.True);
            Assert.That(actor.Health, Is.EqualTo(lethal ? 0 : 9));
            Assert.That(battle.TryDequeueDamage(out BattleDamageEvent damage), Is.True);
            Assert.That(damage.Result.DamageByAttribute.Poison, Is.EqualTo(1));
            Assert.That(battle.HasPendingDamage, Is.False);
            Assert.That(battle.Phase, Is.EqualTo(lethal ? BattleActionPhase.Settling : BattleActionPhase.ReadyToAttack));
            if (lethal)
            {
                Assert.That(battle.TryBeginAttack(out _), Is.False);
            }
        }

        [Test]
        public void ConsecutiveTurnStartDeathsAdvanceIterativelyAndBeginEachTurnOnce()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter first = Spawn(battle, BattleTeam.Player, new HexCoordinate(0, 1), health: 1, initiative: 30);
            HexCharacter second = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(2, 1), health: 1, initiative: 20);
            HexCharacter survivor = Spawn(battle, BattleTeam.Player, new HexCoordinate(4, 1), initiative: 10);
            Spawn(battle, BattleTeam.Enemy, new HexCoordinate(6, 1));
            SetEnvironment(battle, first.Coordinate, BattleGroundEffect.Burning);
            SetEnvironment(battle, second.Coordinate, BattleGroundEffect.Burning);
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(survivor.Id));
            Assert.That(first.Health, Is.Zero);
            Assert.That(second.Health, Is.Zero);
            Assert.That(survivor.ActionPoints, Is.EqualTo(battle.Rules.StartingActionPoints));
            Assert.That(battle.IsBusy, Is.False);
            Assert.That(battle.TryDequeueDamage(out BattleDamageEvent firstDamage), Is.True);
            Assert.That(battle.TryDequeueDamage(out BattleDamageEvent secondDamage), Is.True);
            Assert.That(firstDamage.Result.TargetId, Is.EqualTo(first.Id));
            Assert.That(secondDamage.Result.TargetId, Is.EqualTo(second.Id));
            Assert.That(battle.HasPendingDamage, Is.False);
        }

        [Test]
        public void LethalCreationOnTheCurrentUnitSettlesBeforeTheNextTurn()
        {
            BattleSimulation battle = NewBattle();
            HexCharacter first = Spawn(battle, BattleTeam.Player, new HexCoordinate(0, 1), health: 1, initiative: 30);
            HexCharacter enemy = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(2, 1), initiative: 20);
            Spawn(battle, BattleTeam.Player, new HexCoordinate(4, 1));
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(battle.TryApplyGroundEffect(first.Coordinate, BattleGroundEffect.Burning, out _), Is.True);
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.Settling));
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(enemy.Id));
            Assert.That(enemy.ActionPoints, Is.EqualTo(battle.Rules.StartingActionPoints));
            Assert.That(battle.TrySettleAction(), Is.False);
        }

        [Test]
        public void SpiritEnvironmentCanKillTheTargetBeforeAttackWithoutConsumingCombatRandom()
        {
            BattleSimulation battle = NewBattle();
            SkillSet skills = new SkillSet(Basic(), new BattleSkillDefinition(SkillType.SpiritImbuement,
                SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 0));
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(0, 1), initiative: 20, skills: skills, criticalChance: 37);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(3, 1), health: 1);
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement, actor.Id, spirit: SpiritType.Poison)), Is.True);
            Assert.That(battle.TryAttack(actor.Id, target.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            List<BattleAttackResult> attacks = new List<BattleAttackResult>();
            Assert.That(battle.TryCompleteAttackBatch(attacks), Is.True);
            Assert.That(attacks, Is.Empty);
            Assert.That(battle.Board.TryGetUnit(target.Id, out _), Is.False);
            Assert.That(battle.TryDequeueDamage(out BattleDamageEvent damage), Is.True);
            Assert.That(damage.Origin, Is.EqualTo(BattleDamageOrigin.Environment));
            Assert.That(damage.Result.TargetDied, Is.True);
            Assert.That(battle.HasPendingDamage, Is.False);
            Random combatRandom = (Random)typeof(BattleSimulation).GetField("_combatRandom", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(battle);
            Assert.That(combatRandom.Next(), Is.EqualTo(new Random(19).Next()));
        }

        [Test]
        public void FireSpiritCommitsItsFinalEnvironmentOnceAndAttackIgnoresItsOwnSmoke()
        {
            BattleSimulation battle = NewBattle();
            SkillSet skills = new SkillSet(Basic(), new BattleSkillDefinition(SkillType.SpiritImbuement,
                SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 0));
            HexCharacter actor = Spawn(battle, BattleTeam.Player, new HexCoordinate(0, 1), initiative: 20, skills: skills);
            HexCharacter target = Spawn(battle, BattleTeam.Enemy, new HexCoordinate(1, 1));
            Assert.That(battle.TryStartBattle(), Is.True);
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement, actor.Id, spirit: SpiritType.Fire)), Is.True);
            Assert.That(battle.TryAttack(actor.Id, target.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            uint version = battle.Board.EnvironmentVersion;
            List<BattleAttackResult> attacks = new List<BattleAttackResult>();
            Assert.That(battle.TryCompleteAttackBatch(attacks), Is.True);
            Assert.That(battle.Board.EnvironmentVersion, Is.EqualTo(version + 1));
            Assert.That(attacks.Count, Is.EqualTo(1));
            Assert.That(battle.TryDequeueDamage(out BattleDamageEvent environment), Is.True);
            Assert.That(battle.TryDequeueDamage(out BattleDamageEvent attack), Is.True);
            Assert.That(environment.Origin, Is.EqualTo(BattleDamageOrigin.Environment));
            Assert.That(attack.Origin, Is.EqualTo(BattleDamageOrigin.Attack));
            Assert.That(environment.Result.Damage, Is.EqualTo(1));
            Assert.That(attack.Result.Damage, Is.EqualTo(2));
            Assert.That(target.Health, Is.EqualTo(7));
            Assert.That(battle.HasPendingDamage, Is.False);
        }

        static void SetEnvironment(BattleSimulation battle, HexCoordinate coordinate, BattleGroundEffect ground,
            BattleAirEffect air = BattleAirEffect.None)
        {
            MethodInfo method = typeof(BattleBoard).GetMethod("TrySetEnvironment", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That((bool)method.Invoke(battle.Board,
                new object[] { coordinate, new BattleEnvironmentState(ground, air) }), Is.True);
        }

        static BattleSimulation NewBattle()
        {
            return new BattleSimulation(8, 4, randomSeed: 19);
        }

        static BattleSkillDefinition Basic()
        {
            return new BattleSkillDefinition(SkillType.BasicAttack, SkillTargetType.Unit,
                SkillRangeSource.Own, 1, 6, 2, damage: 1);
        }

        static HexCharacter Spawn(BattleSimulation battle, BattleTeam team, HexCoordinate coordinate,
            int health = 10, int poisonDefense = 0, int initiative = 0, MercenaryClass unitClass = MercenaryClass.Warrior,
            SkillSet skills = null, MercenarySpecialization specialization = MercenarySpecialization.None, TraitCollection traits = null, int criticalChance = 0)
        {
            Assert.That(battle.TrySpawnUnit(team, coordinate,
                new BattleUnitStats(unitClass, health, 4, criticalChance, poisonDefense: poisonDefense, initiative: initiative),
                HexDirection.East, out HexCharacter unit, traits: traits, specialization: specialization,
                skills: skills ?? new SkillSet(Basic())), Is.True);
            return unit;
        }
    }
}
