using RaggedBanner.HexGrid;

namespace AutoBattler.Combat
{
    public sealed class AutoBattlerBattleSimulation
    {
        readonly AutoBattlerBattleBoard _board;
        readonly AutoBattlerBattlePathfinder _pathfinder;
        readonly AutoBattlerBattleTargetingSystem _targetingSystem;
        readonly AutoBattlerBattleMovementSystem _movementSystem;
        readonly AutoBattlerBattleAttackSystem _attackSystem;

        int _nextUnitId = 1;

        public AutoBattlerBattleBoard Board => _board;

        public AutoBattlerBattleSimulation(int width, int height)
        {
            HexMap map = new HexMap(width, height);
            _board = new AutoBattlerBattleBoard(map);
            _pathfinder = new AutoBattlerBattlePathfinder();
            _targetingSystem = new AutoBattlerBattleTargetingSystem(_board, _pathfinder);
            _movementSystem = new AutoBattlerBattleMovementSystem(_board, _targetingSystem);
            _attackSystem = new AutoBattlerBattleAttackSystem(_board);
        }

        public bool TrySpawnUnit(
            AutoBattlerTeam team,
            HexCoordinate coordinate,
            AutoBattlerBattleUnitStat stat,
            out AutoBattlerBattleUnit unit)
        {
            unit = null;

            if (stat == null || !stat.IsValid())
            {
                return false;
            }

            AutoBattlerBattleUnit createdUnit = new AutoBattlerBattleUnit(
                _nextUnitId,
                team,
                coordinate,
                stat);

            if (!_board.TryPlace(createdUnit, coordinate))
            {
                return false;
            }

            _nextUnitId++;
            unit = createdUnit;
            return true;
        }

        public bool TrySpawnUnit(
            AutoBattlerTeam team,
            HexCoordinate coordinate,
            int attackRange,
            float moveSpeed,
            out AutoBattlerBattleUnit unit)
        {
            return TrySpawnUnit(
                team,
                coordinate,
                attackRange,
                moveSpeed,
                100,
                10,
                1.6f,
                0.4f,
                0.4f,
                out unit);
        }

        public bool TrySpawnUnit(
            AutoBattlerTeam team,
            HexCoordinate coordinate,
            int attackRange,
            float moveSpeed,
            int maxHealth,
            int attackDamage,
            out AutoBattlerBattleUnit unit)
        {
            return TrySpawnUnit(
                team,
                coordinate,
                attackRange,
                moveSpeed,
                maxHealth,
                attackDamage,
                1.6f,
                0.4f,
                0.4f,
                out unit);
        }

        public bool TrySpawnUnit(
            AutoBattlerTeam team,
            HexCoordinate coordinate,
            int attackRange,
            float moveSpeed,
            int maxHealth,
            int attackDamage,
            float attackAnimationDuration,
            float attackHitNormalizedTime,
            float attackCooldownDuration,
            out AutoBattlerBattleUnit unit)
        {
            unit = null;

            if (attackRange < 0
                || moveSpeed < 0f
                || float.IsNaN(moveSpeed)
                || float.IsInfinity(moveSpeed)
                || maxHealth <= 0
                || attackDamage < 0
                || attackAnimationDuration <= 0f
                || float.IsNaN(attackAnimationDuration)
                || float.IsInfinity(attackAnimationDuration)
                || attackHitNormalizedTime <= 0f
                || attackHitNormalizedTime > 1f
                || float.IsNaN(attackHitNormalizedTime)
                || float.IsInfinity(attackHitNormalizedTime)
                || attackCooldownDuration < 0f
                || float.IsNaN(attackCooldownDuration)
                || float.IsInfinity(attackCooldownDuration))
            {
                return false;
            }

            AutoBattlerBattleUnitStat stat = new AutoBattlerBattleUnitStat(
                maxHealth,
                0,
                moveSpeed,
                attackDamage,
                attackRange,
                attackAnimationDuration,
                attackHitNormalizedTime,
                attackCooldownDuration);
            return TrySpawnUnit(team, coordinate, stat, out unit);
        }

        public bool TryApplyBasicAttackDamage(AutoBattlerBattleUnit attacker, AutoBattlerBattleUnit target)
        {
            if (!IsRegistered(attacker)
                || !IsRegistered(target)
                || ReferenceEquals(attacker, target)
                || !attacker.IsAlive
                || !target.IsAlive
                || attacker.Team == target.Team)
            {
                return false;
            }

            return _board.TryApplyDamage(target, attacker.AttackDamage);
        }

        public bool TrySetTarget(AutoBattlerBattleUnit unit, AutoBattlerBattleUnit target)
        {
            if (!IsRegistered(unit)
                || !IsRegistered(target)
                || ReferenceEquals(unit, target)
                || !unit.IsAlive
                || !target.IsAlive
                || unit.Team == target.Team)
            {
                return false;
            }

            unit.Target = target;
            return true;
        }

        public void TickMovement(float tickDuration)
        {
            _movementSystem.Tick(tickDuration);
        }

        public void TickAttacks(float deltaTime)
        {
            _attackSystem.Tick(deltaTime);
        }

        bool IsRegistered(AutoBattlerBattleUnit unit)
        {
            return unit != null
                && _board.TryGetUnit(unit.Id, out AutoBattlerBattleUnit registeredUnit)
                && ReferenceEquals(registeredUnit, unit);
        }
    }
}
