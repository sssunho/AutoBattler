using System;
using System.Collections.Generic;
using NUnit.Framework;
using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleEquipmentTests
    {
        [Test]
        public void LoadoutCopiesDuplicateItemsAndAppliesDefensiveBonusesOnceAtSpawn()
        {
            BattleEquipmentItem[] items =
            {
                new BattleEquipmentItem(EquipmentType.Armor, maxHealthBonus: 2),
                new BattleEquipmentItem(EquipmentType.Armor, maxHealthBonus: 2),
                new BattleEquipmentItem(EquipmentType.Shield, physicalDefenseBonus: 1)
            };
            BattleEquipmentLoadout equipment = new BattleEquipmentLoadout(items);
            items[0] = default;
            BattleUnitStats original = new BattleUnitStats(MercenaryClass.Warrior, 10, 2, 0, physicalDefense: 1);
            BattleUnitStats adjusted = equipment.ApplyTo(original);
            Assert.That(original.MaxHealth, Is.EqualTo(10));
            Assert.That(adjusted.MaxHealth, Is.EqualTo(14));
            Assert.That(adjusted.PhysicalDefense, Is.EqualTo(2));
            Assert.That(equipment.Items.Count, Is.EqualTo(3));
            Assert.That(equipment.Items[0].MaxHealthBonus, Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(() => ((IList<BattleEquipmentItem>)equipment.Items)[0] = default);
            for (int i = 0; i < 2; i++)
            {
                BattleSimulation battle = new BattleSimulation(4, 4);
                Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1), adjusted,
                    HexDirection.East, out HexCharacter actor, skills: Skills(), equipment: equipment), Is.True);
                Assert.That(actor.Health, Is.EqualTo(14));
                Assert.That(actor.Stats.PhysicalDefense, Is.EqualTo(2));
                Assert.That(actor.Equipment, Is.SameAs(equipment));
            }
        }

        [Test]
        public void InvalidOrOverflowingEquipmentIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new BattleEquipmentLoadout(new BattleEquipmentItem[4]));
            Assert.Throws<ArgumentNullException>(() => new BattleEquipmentLoadout(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleEquipmentItem(EquipmentType.Dagger, criticalDamageBonus: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BattleEquipmentItem((EquipmentType)99));
            BattleEquipmentItem large = new BattleEquipmentItem(EquipmentType.Armor, maxHealthBonus: int.MaxValue);
            Assert.Throws<OverflowException>(() => new BattleEquipmentLoadout(new[] { large, large }));
            Assert.Throws<OverflowException>(() => new BattleEquipmentLoadout(new[] { large })
                .ApplyTo(new BattleUnitStats(MercenaryClass.Warrior, 10, 2, 0)));
        }

        [TestCase(false, 5)]
        [TestCase(true, 7)]
        public void ThreeLongswordsAddOnlyToPhysicalBeforeDefense(bool critical, int expected)
        {
            BattleEquipmentItem sword = new BattleEquipmentItem(EquipmentType.Longsword, physicalAttackBonus: 1);
            BattleEquipmentLoadout loadout = new BattleEquipmentLoadout(new[] { sword, sword, sword });
            BattleSimulation battle = CreateBattle(loadout, critical, false, AttackAttribute.Physical,
                out HexCharacter actor, out HexCharacter target);
            Assert.That(battle.TryAttack(actor.Id, target.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.Damage, Is.EqualTo(expected));
            Assert.That(actor.Skills.BasicAttack.Damage, Is.EqualTo(3));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FourElementAttackIgnoresSwordAndAddsDaggerOnceOnCritical(bool critical)
        {
            BattleEquipmentLoadout loadout = new BattleEquipmentLoadout(new[]
            {
                new BattleEquipmentItem(EquipmentType.Longsword, physicalAttackBonus: 1),
                new BattleEquipmentItem(EquipmentType.Dagger, criticalDamageBonus: 1),
                new BattleEquipmentItem(EquipmentType.Dagger, criticalDamageBonus: 1)
            });
            BattleSimulation battle = CreateBattle(loadout, critical, true, AttackAttribute.Physical,
                out HexCharacter actor, out HexCharacter target);
            Assert.That(battle.TryAttack(actor.Id, target.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.DamageByAttribute.Physical, Is.Zero);
            Assert.That(result.DamageByAttribute.Untyped, Is.EqualTo(critical ? 4 : 0));
            Assert.That(result.Damage, Is.EqualTo(critical ? 8 : 4));
        }

        [Test]
        public void ElementalPrimaryDoesNotGainSwordPhysicalComponent()
        {
            BattleEquipmentLoadout loadout = new BattleEquipmentLoadout(new[]
            {
                new BattleEquipmentItem(EquipmentType.Longsword, physicalAttackBonus: 1),
                new BattleEquipmentItem(EquipmentType.Dagger, criticalDamageBonus: 1)
            });
            BattleSimulation battle = CreateBattle(loadout, true, false, AttackAttribute.Fire,
                out HexCharacter actor, out HexCharacter target);
            Assert.That(battle.TryAttack(actor.Id, target.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            Assert.That(battle.TryCompleteAttack(out BattleAttackResult result), Is.True);
            Assert.That(result.DamageByAttribute.Physical, Is.Zero);
            Assert.That(result.DamageByAttribute.Fire, Is.EqualTo(6));
        }

        [Test]
        public void EquipmentDoesNotModifyElectricSecondaryOrEnvironmentDamage()
        {
            BattleEquipmentLoadout equipment = new BattleEquipmentLoadout(new[]
            {
                new BattleEquipmentItem(EquipmentType.Longsword, physicalAttackBonus: 1),
                new BattleEquipmentItem(EquipmentType.Dagger, criticalDamageBonus: 1)
            });
            BattleSimulation battle = new BattleSimulation(6, 4, randomSeed: 9);
            SkillSet skills = new SkillSet(Skills().BasicAttack,
                new BattleSkillDefinition(SkillType.SpiritImbuement, SkillTargetType.Self, SkillRangeSource.Own, 0, 0, 0));
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1),
                new BattleUnitStats(MercenaryClass.Archer, 20, 2, 100, initiative: 100), HexDirection.East,
                out HexCharacter actor, skills: skills, equipment: equipment), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 20, 2, 0), HexDirection.West,
                out HexCharacter secondary, skills: Skills()), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 20, 2, 0), HexDirection.West,
                out HexCharacter primary, skills: Skills()), Is.True);
            BattleTestUtility.StartTurn(battle, actor.Id);
            Assert.That(battle.TryUseSkill(new SkillUseRequest(SkillType.SpiritImbuement, actor.Id,
                spirit: SpiritType.Electricity)), Is.True);
            Assert.That(battle.TryAttack(actor.Id, primary.Id), Is.True);
            Assert.That(battle.TryBeginAttack(out _), Is.True);
            List<BattleAttackResult> results = new List<BattleAttackResult>();
            Assert.That(battle.TryCompleteAttackBatch(results), Is.True);
            Assert.That(secondary.Health, Is.EqualTo(19));
            Assert.That(primary.Health, Is.EqualTo(12));
            Assert.That(battle.TryApplyGroundEffect(actor.Coordinate, BattleGroundEffect.Burning, out _), Is.True);
            Assert.That(actor.Health, Is.EqualTo(19));
        }

        static SkillSet Skills(AttackAttribute attribute = AttackAttribute.Physical)
        {
            return new SkillSet(new BattleSkillDefinition(SkillType.BasicAttack, SkillTargetType.Unit,
                SkillRangeSource.Own, 1, 3, 1, damage: 3, attackAttribute: attribute));
        }

        static BattleSimulation CreateBattle(BattleEquipmentLoadout equipment, bool critical, bool fourElement,
            AttackAttribute attribute, out HexCharacter actor, out HexCharacter target)
        {
            BattleSimulation battle = new BattleSimulation(5, 5, randomSeed: 7);
            TraitCollection traits = fourElement ? new TraitCollection(new[]
            {
                new Trait(TraitType.FourElementBasicAttack, new TraitSource(TraitSourceKind.Specialization, 0))
            }) : TraitCollection.Empty;
            BattleUnitStats stats = new BattleUnitStats(MercenaryClass.Warrior, 20, 2, critical ? 100 : 0, initiative: 100);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1), equipment.ApplyTo(stats),
                HexDirection.East, out actor, traits: traits, skills: Skills(attribute), equipment: equipment), Is.True);
            Assert.That(battle.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 1),
                new BattleUnitStats(MercenaryClass.Warrior, 100, 2, 0, physicalDefense: 1), HexDirection.West,
                out target, skills: Skills()), Is.True);
            BattleTestUtility.StartTurn(battle, actor.Id);
            return battle;
        }
    }
}
