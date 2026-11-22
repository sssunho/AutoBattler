using System;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleTraitTests
    {
        [Test]
        public void MissingTraitInputsCreateEmptyOwnershipAndSettings()
        {
            BattleSimulation simulation = new BattleSimulation(2, 1);

            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default,
                BattleTestUnit.ForClass(MercenaryClass.Rogue), HexDirection.East, out HexCharacter unit), Is.True);

            Assert.That(unit.Traits, Is.SameAs(TraitCollection.Empty));
            Assert.That(unit.TraitSettings, Is.SameAs(BattleTraitSettings.None));
        }

        [Test]
        public void RogueWithoutRearCriticalTraitUsesOrdinaryChanceFromRear()
        {
            BattleSimulation simulation = CreateBattle(MercenaryClass.Rogue, MercenaryClass.Archer,
                new HexCoordinate(-1, 0), null, null, null, 7, out HexCharacter attacker, out HexCharacter target);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.IsCritical, Is.False);
            Assert.That(result.Damage, Is.EqualTo(5));
        }

        [TestCase(MercenaryClass.Warrior)]
        [TestCase(MercenaryClass.Rogue)]
        [TestCase(MercenaryClass.Archer)]
        public void RearCriticalDependsOnTraitInsteadOfClass(MercenaryClass unitClass)
        {
            TraitCollection traits = CreateTraits(TraitType.RearGuaranteedCritical, 2);
            BattleSimulation simulation = CreateBattle(unitClass, MercenaryClass.Archer,
                new HexCoordinate(-1, 0), traits, null, null, 7, out HexCharacter attacker, out HexCharacter target);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(attacker.Traits, Is.SameAs(traits));
            Assert.That(attacker.Traits.GetCount(TraitType.RearGuaranteedCritical), Is.EqualTo(2));
            Assert.That(result.IsCritical, Is.True);
            Assert.That(result.Damage, Is.EqualTo(7));
        }

        [Test]
        public void WarriorWithoutNegationTraitDoesNotBlockEvenWithGuaranteedSettings()
        {
            BattleSimulation simulation = CreateBattle(MercenaryClass.Archer, MercenaryClass.Warrior,
                new HexCoordinate(1, 0), null, null, new BattleTraitSettings(1f, 1f, 1f), 7,
                out HexCharacter attacker, out HexCharacter target);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.WasNegated, Is.False);
            Assert.That(result.Damage, Is.EqualTo(5));
        }

        [TestCase(MercenaryClass.Warrior)]
        [TestCase(MercenaryClass.Rogue)]
        [TestCase(MercenaryClass.Archer)]
        public void DirectionalNegationDependsOnTraitInsteadOfClass(MercenaryClass unitClass)
        {
            TraitCollection traits = CreateTraits(TraitType.DirectionalPhysicalNegation);
            BattleTraitSettings settings = new BattleTraitSettings(1f, 0f, 0f);
            BattleSimulation simulation = CreateBattle(MercenaryClass.Archer, unitClass,
                new HexCoordinate(1, 0), null, traits, settings, 7,
                out HexCharacter attacker, out HexCharacter target);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(target.Traits, Is.SameAs(traits));
            Assert.That(target.TraitSettings, Is.SameAs(settings));
            Assert.That(result.WasNegated, Is.True);
            Assert.That(result.Damage, Is.Zero);
        }

        [TestCase(1, 0, 1f, 0f, 0f, true)]
        [TestCase(1, -1, 1f, 0f, 0f, false)]
        [TestCase(-1, 0, 1f, 0f, 0f, false)]
        [TestCase(1, 0, 0f, 1f, 0f, false)]
        [TestCase(1, -1, 0f, 1f, 0f, true)]
        [TestCase(-1, 0, 0f, 1f, 0f, false)]
        [TestCase(1, 0, 0f, 0f, 1f, false)]
        [TestCase(1, -1, 0f, 0f, 1f, false)]
        [TestCase(-1, 0, 0f, 0f, 1f, true)]
        public void DirectionalNegationUsesConfiguredChanceForEachDirection(
            int q, int r, float front, float side, float rear, bool expected)
        {
            BattleSimulation simulation = CreateBattle(MercenaryClass.Archer, MercenaryClass.Archer,
                new HexCoordinate(q, r), null, CreateTraits(TraitType.DirectionalPhysicalNegation),
                new BattleTraitSettings(front, side, rear), 7, out HexCharacter attacker, out HexCharacter target);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.WasNegated, Is.EqualTo(expected));
            Assert.That(result.Damage, Is.EqualTo(expected ? 0 : 5));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FractionalNegationChanceIsNotRoundedToWholePercent(bool expected)
        {
            int seed = FindFractionalSeed(expected ? 0.001 : 0.006, expected ? 0.004 : 0.009);
            BattleSimulation simulation = CreateBattle(MercenaryClass.Archer, MercenaryClass.Archer,
                new HexCoordinate(1, 0), null, CreateTraits(TraitType.DirectionalPhysicalNegation),
                new BattleTraitSettings(0.005f, 0f, 0f), seed, out HexCharacter attacker, out HexCharacter target);

            BattleAttackResult result = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(result.WasNegated, Is.EqualTo(expected));
        }

        [Test]
        public void MultipleNegationSourcesDoNotRepeatRollOrConsumeAdditionalRandomValues()
        {
            int seed = FindNegationSequenceSeed();
            TraitCollection traits = CreateTraits(TraitType.DirectionalPhysicalNegation, 2);
            BattleSimulation simulation = CreateBattle(MercenaryClass.Archer, MercenaryClass.Archer,
                new HexCoordinate(1, 0), null, traits, new BattleTraitSettings(0.5f, 0f, 0f), seed,
                out HexCharacter attacker, out HexCharacter target);

            BattleAttackResult first = CompleteAttack(simulation, attacker.Id, target.Id);
            BattleAttackResult second = CompleteAttack(simulation, attacker.Id, target.Id);

            Assert.That(target.Traits.GetCount(TraitType.DirectionalPhysicalNegation), Is.EqualTo(2));
            Assert.That(first.WasNegated, Is.False);
            Assert.That(first.Damage, Is.EqualTo(5));
            Assert.That(second.WasNegated, Is.True);
            Assert.That(second.Damage, Is.Zero);
        }

        [TestCase(0f)]
        [TestCase(1f)]
        public void NegationEndpointsDoNotConsumeRandomRoll(float chance)
        {
            int seed = FindNegationSequenceSeed();
            BattleSimulation simulation = new BattleSimulation(3, 1, randomSeed: seed);
            BattleTestUnit attackerStats = new BattleTestUnit(MercenaryClass.Archer, 100, 5, 2, 3, 50);
            BattleTestUnit targetStats = new BattleTestUnit(MercenaryClass.Archer, 100, 3, 2, 3, 0);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 0), attackerStats,
                HexDirection.East, out HexCharacter attacker, CreateTraits(TraitType.RearGuaranteedCritical)), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 0), targetStats,
                HexDirection.East, out HexCharacter rearTarget, CreateTraits(TraitType.DirectionalPhysicalNegation),
                new BattleTraitSettings(chance, chance, chance)), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, default, targetStats,
                HexDirection.East, out HexCharacter frontTarget), Is.True);
            BattleTestUtility.StartTurn(simulation, attacker.Id);

            BattleAttackResult first = CompleteAttack(simulation, attacker.Id, rearTarget.Id);
            BattleAttackResult second = CompleteAttack(simulation, attacker.Id, frontTarget.Id);

            Assert.That(first.IsCritical, Is.True);
            Assert.That(first.WasNegated, Is.EqualTo(chance == 1f));
            Assert.That(second.IsCritical, Is.False);
        }

        [Test]
        public void MissingNegationSettingsRejectSpawnWithoutOccupyingTileOrConsumingId()
        {
            BattleSimulation simulation = new BattleSimulation(2, 1);
            TraitCollection traits = CreateTraits(TraitType.DirectionalPhysicalNegation);
            BattleTestUnit stats = BattleTestUnit.ForClass(MercenaryClass.Warrior);
            uint occupancyVersion = simulation.Board.OccupancyVersion;

            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default, stats, HexDirection.East,
                out HexCharacter rejected, traits), Is.False);
            Assert.That(rejected, Is.Null);
            Assert.That(simulation.Board.IsOccupied(default), Is.False);
            Assert.That(simulation.Board.Units.Count, Is.Zero);
            Assert.That(simulation.Board.OccupancyVersion, Is.EqualTo(occupancyVersion));
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, default, stats, HexDirection.East,
                out HexCharacter accepted, traits, BattleTraitSettings.None), Is.True);
            Assert.That(accepted.Id, Is.EqualTo(1));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-0.001f)]
        [TestCase(1.001f)]
        public void TraitSettingsRejectInvalidChances(float chance)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleTraitSettings(chance, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleTraitSettings(0f, chance, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleTraitSettings(0f, 0f, chance));
        }

        static TraitCollection CreateTraits(TraitType type, int count = 1)
        {
            Trait[] traits = new Trait[count];

            for (int i = 0; i < count; i++)
            {
                traits[i] = new Trait(type, new TraitSource(TraitSourceKind.Selection, i));
            }

            return new TraitCollection(traits);
        }

        static BattleSimulation CreateBattle(MercenaryClass attackerClass, MercenaryClass targetClass,
            HexCoordinate offset, TraitCollection attackerTraits, TraitCollection targetTraits,
            BattleTraitSettings targetSettings, int seed, out HexCharacter attacker, out HexCharacter target)
        {
            BattleSimulation simulation = new BattleSimulation(7, 7, randomSeed: seed);
            BattleTestUnit attackerStats = new BattleTestUnit(attackerClass, 100, 5, 2, 3, 0);
            BattleTestUnit targetStats = new BattleTestUnit(targetClass, 100, 3, 2, 3, 0);
            HexCoordinate targetCoordinate = new HexCoordinate(3, 3);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, targetCoordinate + offset, attackerStats,
                HexDirection.West, out attacker, attackerTraits), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, targetCoordinate, targetStats,
                HexDirection.East, out target, targetTraits, targetSettings), Is.True);
            BattleTestUtility.StartTurn(simulation, attacker.Id);
            return simulation;
        }

        static BattleAttackResult CompleteAttack(BattleSimulation simulation, int attackerId, int targetId)
        {
            Assert.That(simulation.TryAttack(attackerId, targetId), Is.True);
            Assert.That(simulation.TryBeginAttack(out _), Is.True);
            Assert.That(simulation.TryCompleteAttack(out BattleAttackResult result), Is.True);
            return result;
        }

        static int FindFractionalSeed(double minimum, double maximum)
        {
            for (int seed = 0; seed < 100000; seed++)
            {
                double value = new Random(seed).NextDouble();

                if (value >= minimum && value <= maximum)
                {
                    return seed;
                }
            }

            Assert.Fail("No seed found for the requested fractional probability.");
            return 0;
        }

        static int FindNegationSequenceSeed()
        {
            for (int seed = 0; seed < 100000; seed++)
            {
                Random random = new Random(seed);

                if (random.NextDouble() >= 0.5 && random.NextDouble() < 0.5)
                {
                    return seed;
                }
            }

            Assert.Fail("No seed found for the requested negation sequence.");
            return 0;
        }
    }
}
