using NUnit.Framework;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    static class BattleTestUtility
    {
        public static BattleTestUnit CreateStats(int maxHealth = 10, int attackDamage = 3,
            int movementPerTurn = 2, int attackRange = 1)
        {
            return new BattleTestUnit(MercenaryClass.Archer, maxHealth, attackDamage, movementPerTurn,
                attackRange, 0);
        }

        public static void StartTurn(BattleSimulation simulation, int unitId)
        {
            Assert.That(simulation.TryStartBattle(), Is.True);
            AdvanceToTurn(simulation, unitId);
        }

        public static void AdvanceToTurn(BattleSimulation simulation, int unitId)
        {
            for (long i = 0; i < simulation.Board.Units.Count * 2L; i++)
            {
                if (simulation.CurrentUnitId == unitId)
                {
                    return;
                }

                Assert.That(simulation.CurrentUnitId.HasValue, Is.True);
                Assert.That(simulation.TryEndTurn(simulation.CurrentUnitId.Value), Is.True);
            }

            Assert.That(simulation.CurrentUnitId, Is.EqualTo(unitId));
        }
    }
}
