using AutoBattler.HexGrid;
using NUnit.Framework;

namespace AutoBattler.Combat.Tests
{
    public sealed class BattleUnitTests
    {
        [TestCase(BattleAttackPhase.Animation)]
        [TestCase(BattleAttackPhase.Cooldown)]
        public void IsAttacking_IsTrueForActiveAttackPhases(BattleAttackPhase phase)
        {
            BattleUnit unit = new BattleUnit(
                1,
                Team.Player,
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
