using RaggedBanner.HexGrid;
using RaggedBanner.Mercenaries;

namespace RaggedBanner.Combat
{
    public sealed class HexCharacter
    {
        readonly int[] _cooldowns = new int[SkillTypeRules.Capacity];
        uint _cooldownVersion;
        SpiritType _preparedSpirit;
        HexCoordinate _coordinate;
        HexDirection _facing;
        int _health;
        int _actionPoints;
        int _movementRemaining;
        bool _hasStartedTurn;
        bool _isOverwatching;
        bool _isStealthed;
        bool _isInvisible;
        bool _hasUsedAmbush;
        bool _hasUsedSkillMovementRecovery;
        int _turnMovementAllowance;
        HexDirection _overwatchDirection;

        public SpiritType PreparedSpirit => _preparedSpirit;
        public uint CooldownVersion => _cooldownVersion;
        public int Id { get; }
        public BattleTeam Team { get; }
        public BattleUnitStats Stats { get; }
        public SkillSet Skills { get; }
        public BattleEquipmentLoadout Equipment { get; }
        public TraitCollection Traits { get; }
        public BattleTraitSettings TraitSettings { get; }
        public MercenarySpecialization Specialization { get; }
        public int MaxHealth => Stats.MaxHealth;
        public HexDirection Facing => _facing;
        public HexCoordinate Coordinate => _coordinate;
        public int Health => _health;
        public int ActionPoints => _actionPoints;
        public int MovementRemaining => _movementRemaining;
        public bool IsOverwatching => _isOverwatching;
        public bool IsStealthed => _isStealthed;
        public bool IsInvisible => _isInvisible;
        public bool HasUsedAmbush => _hasUsedAmbush;
        public bool HasUsedSkillMovementRecovery => _hasUsedSkillMovementRecovery;
        public int TurnMovementAllowance => _turnMovementAllowance;
        public HexDirection OverwatchDirection => _overwatchDirection;

        internal HexCharacter(int id, BattleTeam team, HexCoordinate coordinate, BattleRules rules,
            BattleUnitStats stats, HexDirection facing, TraitCollection traits, BattleTraitSettings traitSettings,
            MercenarySpecialization specialization = MercenarySpecialization.None, SkillSet skills = null,
            BattleEquipmentLoadout equipment = null)
        {
            Id = id;
            Team = team;
            Stats = stats;
            Skills = skills ?? throw new System.ArgumentNullException(nameof(skills));
            Equipment = equipment ?? BattleEquipmentLoadout.Empty;
            Traits = traits;
            TraitSettings = traitSettings;
            Specialization = specialization;
            _coordinate = coordinate;
            _facing = facing;
            _health = stats.MaxHealth;
            _actionPoints = rules.StartingActionPoints;
        }

        internal void PrepareSpirit(SpiritType spirit)
        {
            _preparedSpirit = spirit;
        }

        public int GetRemainingCooldown(SkillType skill)
        {
            Skills.Get(skill);
            return _cooldowns[(int)skill];
        }

        internal void StartCooldown(SkillType skill)
        {
            int turns = Skills.Get(skill).CooldownTurns;
            if (_cooldowns[(int)skill] != turns)
            {
                _cooldowns[(int)skill] = turns;
                _cooldownVersion++;
            }
        }

        internal void SetCoordinate(HexCoordinate coordinate)
        {
            if (_coordinate != coordinate)
            {
                ClearOverwatch();
            }

            _coordinate = coordinate;
        }

        internal void SetFacing(HexDirection facing, bool preserveOverwatch = false)
        {
            if (_facing != facing && !preserveOverwatch)
            {
                ClearOverwatch();
            }

            _facing = facing;
        }

        internal void PrepareOverwatch(HexDirection direction)
        {
            _overwatchDirection = direction;
            _isOverwatching = true;
        }

        internal void ClearOverwatch()
        {
            _isOverwatching = false;
        }

        internal void BeginTurn(BattleRules rules)
        {
            bool cooldownChanged = false;
            for (int i = 0; i < _cooldowns.Length; i++)
            {
                if (_cooldowns[i] > 0)
                {
                    _cooldowns[i]--;
                    cooldownChanged = true;
                }
            }

            if (cooldownChanged)
            {
                _cooldownVersion++;
            }

            ClearOverwatch();
            _hasUsedAmbush = false;
            _hasUsedSkillMovementRecovery = false;

            if (_hasStartedTurn)
            {
                _actionPoints += System.Math.Min(rules.ActionPointsPerTurn, rules.MaxActionPoints - _actionPoints);
            }

            _hasStartedTurn = true;
            _turnMovementAllowance = System.Math.Max(0, Stats.MovementPerTurn - (_isStealthed && !_isInvisible ? 1 : 0));
            _movementRemaining = _turnMovementAllowance;
        }

        internal void ApplySkillMovementRecovery()
        {
            if (_hasUsedSkillMovementRecovery || !Traits.Has(TraitType.SkillMovementRecovery))
            {
                return;
            }

            _hasUsedSkillMovementRecovery = true;

            if (_movementRemaining < _turnMovementAllowance)
            {
                _movementRemaining++;
            }
        }

        internal void EnterStealth()
        {
            _isStealthed = true;
            _isInvisible = false;
        }

        internal void EnterInvisibility()
        {
            _isStealthed = true;
            _isInvisible = true;
        }

        internal bool ClearStealth()
        {
            bool wasStealthed = _isStealthed;
            _isStealthed = false;
            _isInvisible = false;
            return wasStealthed;
        }

        internal void UseAmbush()
        {
            _hasUsedAmbush = true;
        }

        internal void SpendMovement(int amount = 1)
        {
            _movementRemaining -= amount;
        }

        internal void SpendActionPoints(int amount)
        {
            _actionPoints -= amount;
        }

        internal int ApplyDamage(long amount)
        {
            int damage = (int)System.Math.Min(_health, System.Math.Max(0L, amount));
            _health -= damage;

            if (damage > 0)
            {
                ClearStealth();
            }

            if (_health == 0)
            {
                ClearOverwatch();
            }

            return damage;
        }
    }
}
