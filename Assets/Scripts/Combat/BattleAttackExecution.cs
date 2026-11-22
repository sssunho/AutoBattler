using System.Collections.Generic;
using RaggedBanner.HexGrid;

namespace RaggedBanner.Combat
{
    sealed class BattleAttackExecution
    {
        readonly List<HexCoordinate> _path = new List<HexCoordinate>();
        readonly List<HexCharacter> _targets = new List<HexCharacter>();
        readonly HashSet<HexCoordinate> _visited = new HashSet<HexCoordinate>();

        public BattleAttackContext Context { get; private set; }
        public HexCharacter SpiritActor { get; private set; }
        public SpiritType Spirit { get; private set; }
        public int FireDamage { get; private set; }
        public int ElectricDamage { get; private set; }
        public IReadOnlyList<HexCoordinate> Path => _path;
        public List<HexCharacter> Targets => _targets;
        public bool DamageResolved { get; set; }
        public bool Visit(HexCoordinate coordinate) => _visited.Add(coordinate);

        public bool Begin(BattleAttackContext context, HexCharacter actor, HexCharacter target)
        {
            Clear();
            Context = context;
            if (context.Kind == BattleAttackKind.Ambush || actor.PreparedSpirit == SpiritType.None)
            {
                return false;
            }

            SpiritActor = actor;
            Spirit = actor.PreparedSpirit;
            BattleSkillDefinition definition = actor.Skills.Get(SkillType.SpiritImbuement);
            FireDamage = definition.FireSpiritDamage;
            ElectricDamage = definition.ElectricSpiritDamage;
            BattleSpiritPath.Copy(actor.Coordinate, target.Coordinate, _path);
            return true;
        }

        public void Clear()
        {
            Context = default;
            SpiritActor = null;
            Spirit = SpiritType.None;
            FireDamage = 0;
            ElectricDamage = 0;
            _path.Clear();
            _targets.Clear();
            _visited.Clear();
            DamageResolved = false;
        }
    }
}
