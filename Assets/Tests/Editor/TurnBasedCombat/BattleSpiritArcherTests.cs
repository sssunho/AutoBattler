using System;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleSpiritArcherTests
    {
        [TestCase(0, 4, 0)]
        [TestCase(100, 6, 2)]
        public void BasicAttackReplacesBaseDamageAndAddsCriticalOnlyOnce(int criticalChance, int total, int untyped)
        {
            BattleSimulation battle = CreateBattle(out HexCharacter archer, out HexCharacter target, criticalChance);
            BattleTestUtility.StartTurn(battle, archer.Id);
            BattleAttackResult result = Attack(battle, archer, target);
            Assert.That(result.AttackAttribute, Is.Null);
            Assert.That(result.DamageByAttribute.Physical, Is.Zero);
            Assert.That(result.DamageByAttribute.Water, Is.EqualTo(1));
            Assert.That(result.DamageByAttribute.Fire, Is.EqualTo(1));
            Assert.That(result.DamageByAttribute.Electricity, Is.EqualTo(1));
            Assert.That(result.DamageByAttribute.Poison, Is.EqualTo(1));
            Assert.That(result.DamageByAttribute.Untyped, Is.EqualTo(untyped));
            Assert.That(result.DamageByAttribute.Total, Is.EqualTo(total));
            Assert.That(result.Damage, Is.EqualTo(total));
            Assert.That(archer.ActionPoints, Is.EqualTo(6));
            Assert.That(battle.TryCompleteAttack(out _), Is.False);
        }

        [Test]
        public void EachDefenseReducesOnlyItsOwnComponentAndPhysicalNegationDoesNotApply()
        {
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Warrior, 30, 3, 8, 1, 0,
                physicalDefense: 100, waterDefense: 1, fireDefense: 3, electricityDefense: 0, poisonDefense: 1);
            BattleSimulation battle = CreateBattle(out HexCharacter archer, out HexCharacter target,
                targetStats: targetStats, targetTraits: Traits(TraitType.DirectionalPhysicalNegation),
                targetSettings: new BattleTraitSettings(1f, 1f, 1f));
            BattleTestUtility.StartTurn(battle, archer.Id);
            BattleAttackResult result = Attack(battle, archer, target);
            Assert.That(result.DamageByAttribute.Total, Is.EqualTo(1));
            Assert.That(result.DamageByAttribute.Water, Is.Zero);
            Assert.That(result.DamageByAttribute.Fire, Is.Zero);
            Assert.That(result.DamageByAttribute.Poison, Is.Zero);
            Assert.That(result.DamageByAttribute.Electricity, Is.EqualTo(1));
            Assert.That(result.WasNegated, Is.False);
        }

        [Test]
        public void PhysicalNegationDoesNotConsumeAnExtraCombatRandomRoll()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter archer, out HexCharacter target, 50,
                targetTraits: Traits(TraitType.DirectionalPhysicalNegation),
                targetSettings: new BattleTraitSettings(0.5f, 0.5f, 0.5f));
            BattleTestUtility.StartTurn(battle, archer.Id);
            Random expected = new Random(17);

            for (int i = 0; i < 4; i++)
            {
                BattleAttackResult result = Attack(battle, archer, target);
                Assert.That(result.IsCritical, Is.EqualTo(expected.Next(100) < 50));
                Assert.That(result.WasNegated, Is.False);
            }
        }

        [Test]
        public void OverkillPreservesComponentsButOnlySubtractsRemainingHealthOnce()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter archer, out HexCharacter target,
                targetStats: new BattleTestUnit(MercenaryClass.Warrior, 1, 3, 8, 1, 0));
            BattleTestUtility.StartTurn(battle, archer.Id);
            BattleAttackResult result = Attack(battle, archer, target);
            Assert.That(result.DamageByAttribute.Total, Is.EqualTo(4));
            Assert.That(result.DamageByAttribute.Poison, Is.EqualTo(1));
            Assert.That(result.Damage, Is.EqualTo(1));
            Assert.That(result.TargetDied, Is.True);
            Assert.That(target.Health, Is.Zero);
            Assert.That(battle.TryCompleteAttack(out _), Is.False);
        }

        [TestCase(0, 0, true)]
        [TestCase(100, 2, false)]
        public void FullyResistedAttackOnlyRevealsTargetWhenCriticalDealsDamage(int criticalChance, int damage, bool hidden)
        {
            BattleSimulation battle = CreateBattle(out HexCharacter archer, out HexCharacter target, criticalChance,
                new BattleTestUnit(MercenaryClass.Warrior, 30, 3, 8, 1, 0,
                    waterDefense: 1, fireDefense: 1, electricityDefense: 1, poisonDefense: 1));
            BattleTestUtility.StartTurn(battle, archer.Id);
            Assert.That(battle.TryAttack(archer.Id, target.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            typeof(HexCharacter).GetMethod("EnterInvisibility", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.EqualTo(damage));
            Assert.That(result.DamageByAttribute.Total, Is.EqualTo(damage));
            Assert.That(target.IsInvisible, Is.EqualTo(hidden));
            Assert.That(archer.ActionPoints, Is.EqualTo(6));
        }

        [Test]
        public void OverwatchUsesTheSameComponentsWithNoExtraAttackCost()
        {
            BattleSimulation battle = CreateBattle(out HexCharacter archer, out HexCharacter target, 100);
            BattleTestUtility.StartTurn(battle, archer.Id);
            Assert.That(battle.TryPrepareOverwatch(archer.Id, HexDirection.East), Is.True);
            Assert.That(battle.CurrentUnitId, Is.EqualTo(target.Id));
            Assert.That(battle.TryMove(target.Id, new HexCoordinate(7, 4)), Is.True);
            Assert.That(battle.TryBeginMovementStep(out _), Is.False);
            Assert.That(battle.Phase, Is.EqualTo(BattleActionPhase.ResolvingReactions));
            Assert.That(battle.TryBeginAttack(out HexAttackStep step), Is.True);
            Assert.That(step.AttackerId, Is.EqualTo(archer.Id));
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.DamageByAttribute.Total, Is.EqualTo(6));
            Assert.That(result.DamageByAttribute.Untyped, Is.EqualTo(2));
            Assert.That(archer.ActionPoints, Is.EqualTo(6));
            Assert.That(archer.IsOverwatching, Is.False);
            Assert.That(battle.TrySettleAction(), Is.True);
            Assert.That(target.Coordinate, Is.EqualTo(new HexCoordinate(6, 4)));
        }

        [TestCase(MercenaryClass.Warrior)]
        [TestCase(MercenaryClass.Rogue)]
        public void SpiritArcherSpecializationRejectsOtherClasses(MercenaryClass unitClass)
        {
            BattleSimulation battle = new BattleSimulation(4, 4);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, default,
                new BattleTestUnit(unitClass, 10, 3, 2, 1, 0), HexDirection.East, out _,
                specialization: MercenarySpecialization.SpiritArcher), Is.False);
        }

        [Test]
        public void AmbushRetainsSingleAttributeDamageEvenWhenTheAttackerHasTheTrait()
        {
            BattleSimulation battle = new BattleSimulation(8, 8, new BattleRules(startingActionPoints: 8), 17);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(4, 4),
                new BattleTestUnit(MercenaryClass.Rogue, 30, 7, 4, 1, 100, initiative: 30),
                HexDirection.East, out HexCharacter rogue, Traits(TraitType.FourElementBasicAttack)), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(5, 4),
                new BattleTestUnit(MercenaryClass.Warrior, 30, 3, 2, 1, 0, physicalDefense: 2),
                HexDirection.East, out HexCharacter target), Is.True);
            BattleTestUtility.StartTurn(battle, rogue.Id);
            typeof(HexCharacter).GetMethod("EnterInvisibility", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(rogue, null);
            Assert.That(battle.TryAmbush(rogue.Id, target.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.AttackAttribute, Is.EqualTo(AttackAttribute.Physical));
            Assert.That(result.DamageByAttribute.Physical, Is.EqualTo(9));
            Assert.That(result.DamageByAttribute.Total, Is.EqualTo(9));
            Assert.That(result.Damage, Is.EqualTo(9));
            Assert.That(rogue.HasUsedAmbush, Is.True);
            Assert.That(rogue.IsStealthed, Is.False);
        }

        [Test]
        public void DamageBreakdownRejectsNegativeValuesAndUnrepresentableTotals()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleDamageBreakdown(poison: -1));
            Assert.Throws<OverflowException>(() => new BattleDamageBreakdown(water: long.MaxValue, fire: 1));
            BattleDamageBreakdown values = new BattleDamageBreakdown(1, 2, 3, 4, 5, 6);
            Assert.That(values.Total, Is.EqualTo(21));

            for (int i = 0; i < 6; i++)
            {
                Assert.That(values.GetDamage((AttackAttribute)i), Is.EqualTo(i + 1));
            }
        }

        static BattleAttackResult Attack(BattleSimulation battle, HexCharacter archer, HexCharacter target)
        {
            Assert.That(battle.TryAttack(archer.Id, target.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            return result;
        }

        static TraitCollection Traits(TraitType type)
        {
            return new TraitCollection(new[] { new Trait(type, new TraitSource(TraitSourceKind.Specialization, 3)) });
        }

        static BattleSimulation CreateBattle(out HexCharacter archer, out HexCharacter target, int criticalChance = 0,
            BattleTestUnit targetStats = null, TraitCollection targetTraits = null, BattleTraitSettings targetSettings = null)
        {
            BattleSimulation battle = new BattleSimulation(12, 12, new BattleRules(startingActionPoints: 8), 17);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(4, 4),
                new BattleTestUnit(MercenaryClass.Archer, 30, 99, 8, 3, criticalChance, initiative: 30),
                HexDirection.East, out archer, Traits(TraitType.FourElementBasicAttack),
                specialization: MercenarySpecialization.SpiritArcher), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(6, 4),
                targetStats ?? new BattleTestUnit(MercenaryClass.Warrior, 30, 3, 8, 1, 0, initiative: 20),
                HexDirection.West, out target, targetTraits, targetSettings), Is.True);
            return battle;
        }
    }
}
