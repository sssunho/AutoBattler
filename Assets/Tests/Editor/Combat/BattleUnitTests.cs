using RaggedBanner.HexGrid;
using NUnit.Framework;

namespace AutoBattler.Combat.Tests
{
    public sealed class BattleUnitTests
    {
        [Test]
        public void Constructor_CopiesStatAndInitializesResources()
        {
            AutoBattlerBattleUnitStat source = new AutoBattlerBattleUnitStat(
                250,
                80,
                2f,
                35,
                3,
                1.2f,
                0.5f,
                0.3f);

            AutoBattlerBattleUnit unit = new AutoBattlerBattleUnit(
                1,
                AutoBattlerTeam.Player,
                new HexCoordinate(0, 0),
                source);

            Assert.That(unit.Stat, Is.Not.SameAs(source));
            Assert.That(unit.MaxHealth, Is.EqualTo(250));
            Assert.That(unit.MaxMana, Is.EqualTo(80));
            Assert.That(unit.CurrentHealth, Is.EqualTo(250));
            Assert.That(unit.CurrentMana, Is.Zero);
            Assert.That(unit.MoveSpeed, Is.EqualTo(2f));
            Assert.That(unit.AttackDamage, Is.EqualTo(35));
            Assert.That(unit.AttackRange, Is.EqualTo(3));
            Assert.That(unit.AttackAnimationDuration, Is.EqualTo(1.2f));
            Assert.That(unit.AttackHitNormalizedTime, Is.EqualTo(0.5f));
            Assert.That(unit.AttackCooldownDuration, Is.EqualTo(0.3f));
        }

        [TestCase(AutoBattlerBattleAttackPhase.Animation)]
        [TestCase(AutoBattlerBattleAttackPhase.Cooldown)]
        public void IsAttacking_IsTrueForActiveAttackPhases(AutoBattlerBattleAttackPhase phase)
        {
            AutoBattlerBattleUnit unit = new AutoBattlerBattleUnit(
                1,
                AutoBattlerTeam.Player,
                new HexCoordinate(0, 0),
                1,
                1f,
                100,
                10);

            unit.AttackPhase = phase;

            Assert.That(unit.IsAttacking, Is.True);
        }
    }
}
