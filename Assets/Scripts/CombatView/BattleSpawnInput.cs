using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat.Unity
{
    internal sealed class BattleSpawnInput
    {
        public HexCharacterView Prefab { get; }
        public BattleTeam Team { get; }
        public BattleUnitStats Stats { get; }
        public SkillSet Skills { get; }
        public BattleEquipmentLoadout Equipment { get; }
        public TraitCollection Traits { get; }
        public BattleTraitSettings TraitSettings { get; }
        public MercenarySpecialization Specialization { get; }
        public HexCoordinate Coordinate { get; }
        public HexDirection Facing { get; }

        public BattleSpawnInput(HexCharacterView prefab, BattleTeam team, BattleUnitStats stats,
            HexCoordinate coordinate, HexDirection facing, TraitCollection traits = null,
            BattleTraitSettings traitSettings = null, MercenarySpecialization specialization = MercenarySpecialization.None,
            SkillSet skills = null, BattleEquipmentLoadout equipment = null)
        {
            Prefab = prefab;
            Team = team;
            Stats = stats;
            Skills = skills;
            Equipment = equipment ?? BattleEquipmentLoadout.Empty;
            Traits = traits ?? TraitCollection.Empty;
            TraitSettings = traitSettings;
            Specialization = specialization;
            Coordinate = coordinate;
            Facing = facing;
        }

    }
}
