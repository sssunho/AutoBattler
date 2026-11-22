using System;
using System.Collections.Generic;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;
using RaggedBanner.MercenaryData;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleInitiativeTests
    {
        [Test]
        public void DefaultAndManualStatsUseZeroInitiativeAndNegativeValuesAreRejected()
        {
            Assert.That(new BattleUnitStats(MercenaryClass.Warrior, 10, 2, 0).Initiative, Is.Zero);

            for (int i = 0; i <= (int)MercenaryClass.Archer; i++)
            {
                Assert.That(BattleUnitStats.ForClass((MercenaryClass)i).Initiative, Is.Zero);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => CreateStats(-1));
        }

        [Test]
        public void ClassDefinitionRejectsNegativeInitiativeWithoutClampingIt()
        {
            ClassDefinition definition = ScriptableObject.CreateInstance<ClassDefinition>();
            SkillDefinition attack = BattleSkillTestAssets.CreateBasicAttack(_ => { });

            try
            {
                JsonUtility.FromJsonOverwrite("{\"_displayName\":\"Warrior\","
                    + "\"_baseStats\":{\"_maxHP\":10}}", definition);
                typeof(ClassDefinition).GetField("_basicAttack",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(definition, attack);
                Assert.That(definition.BaseStats.Initiative, Is.Zero);
                Assert.That(definition.Validate(out string error), Is.True, error);

                JsonUtility.FromJsonOverwrite("{\"_baseStats\":{\"_maxHP\":10,\"_initiative\":-1}}", definition);
                Assert.That(definition.Validate(out error), Is.False);
                Assert.That(error, Does.Contain("Initiative"));
                Assert.That(definition.BaseStats.Initiative, Is.EqualTo(-1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void EveryRoundVisitsEachParticipantOnceInDescendingInitiative()
        {
            BattleSimulation simulation = CreateBattle(7, 0, int.MaxValue, 7, 7);

            for (int round = 1; round <= 3; round++)
            {
                HashSet<int> visited = new HashSet<int>();
                int previousInitiative = int.MaxValue;

                for (int i = 0; i < 4; i++)
                {
                    Assert.That(simulation.RoundNumber, Is.EqualTo(round));
                    int unitId = simulation.CurrentUnitId.Value;
                    Assert.That(visited.Add(unitId), Is.True);
                    Assert.That(simulation.Board.TryGetUnit(unitId, out HexCharacter unit), Is.True);
                    Assert.That(unit.Stats.Initiative, Is.LessThanOrEqualTo(previousInitiative));
                    previousInitiative = unit.Stats.Initiative;
                    Assert.That(simulation.TryEndTurn(unitId), Is.True);
                }

                Assert.That(simulation.RoundNumber, Is.EqualTo(round + 1));
            }
        }

        [Test]
        public void SameSeedReproducesTieOrderAcrossRounds()
        {
            BattleSimulation first = CreateBattle(17, 4, 4, 0, 4, 0, 4);
            BattleSimulation second = CreateBattle(17, 4, 4, 0, 4, 0, 4);

            for (int i = 0; i < 30; i++)
            {
                Assert.That(first.CurrentUnitId, Is.EqualTo(second.CurrentUnitId));
                Assert.That(first.RoundNumber, Is.EqualTo(second.RoundNumber));
                Assert.That(first.TryEndTurn(first.CurrentUnitId.Value), Is.True);
                Assert.That(second.TryEndTurn(second.CurrentUnitId.Value), Is.True);
            }
        }

        [Test]
        public void TieOrderCanDifferBetweenSeedsWithoutMovingLowerInitiativeAhead()
        {
            HashSet<int> firstActors = new HashSet<int>();

            for (int seed = 0; seed < 16; seed++)
            {
                BattleSimulation simulation = CreateBattle(seed, 10, 10, 0);
                int first = simulation.CurrentUnitId.Value;
                firstActors.Add(first);
                Assert.That(first == 1 || first == 2, Is.True);
                Assert.That(simulation.TryEndTurn(first), Is.True);
                Assert.That(simulation.CurrentUnitId, Is.EqualTo(first == 1 ? 2 : 1));
                Assert.That(simulation.TryEndTurn(simulation.CurrentUnitId.Value), Is.True);
                Assert.That(simulation.CurrentUnitId, Is.EqualTo(3));
            }

            Assert.That(firstActors.Count, Is.EqualTo(2));
        }

        [Test]
        public void RemovingAnUpcomingUnitPreservesTheRemainingRoundOrder()
        {
            BattleSimulation simulation = CreateBattle(7, 30, 20, 10, 0);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(1));
            Assert.That(simulation.RemoveUnit(2), Is.True);
            Assert.That(simulation.TryEndTurn(1), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(3));
            Assert.That(simulation.RoundNumber, Is.EqualTo(1));
            Assert.That(simulation.TryEndTurn(3), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(4));
            Assert.That(simulation.TryEndTurn(4), Is.True);
            Assert.That(simulation.CurrentUnitId, Is.EqualTo(1));
            Assert.That(simulation.RoundNumber, Is.EqualTo(2));
        }

        static BattleSimulation CreateBattle(int seed, params int[] initiatives)
        {
            BattleSimulation simulation = new BattleSimulation(initiatives.Length, 1, randomSeed: seed);

            for (int i = 0; i < initiatives.Length; i++)
            {
                Assert.That(simulation.TrySpawnUnit(i == 0 ? BattleTeam.Player : BattleTeam.Enemy,
                    new HexCoordinate(i, 0), CreateStats(initiatives[i]), HexDirection.East, out _), Is.True);
            }

            Assert.That(simulation.TryStartBattle(), Is.True);
            return simulation;
        }

        static BattleTestUnit CreateStats(int initiative)
        {
            return new BattleTestUnit(MercenaryClass.Warrior, 10, 3, 2, 1, 0, initiative: initiative);
        }
    }
}
