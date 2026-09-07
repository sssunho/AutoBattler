using AutoBattler.HexGrid;

namespace AutoBattler.Combat
{
    public sealed class BattleSimulation
    {
        readonly BattleBoard _board;
        readonly BattlePathfinder _pathfinder;
        readonly BattleTargetingSystem _targetingSystem;
        readonly BattleMovementSystem _movementSystem;
        readonly BattleAttackSystem _attackSystem;

        int _nextUnitId = 1;

        public BattleBoard Board => _board;

        public BattleSimulation(int width, int height)
        {
            HexMap map = new HexMap(width, height);
            _board = new BattleBoard(map);
            _pathfinder = new BattlePathfinder();
            _targetingSystem = new BattleTargetingSystem(_board, _pathfinder);
            _movementSystem = new BattleMovementSystem(_board, _targetingSystem);
            _attackSystem = new BattleAttackSystem(_board);
        }

        public bool TrySpawnUnit(
            Team team,
            HexCoordinate coordinate,
            int attackRange,
            float moveSpeed,
            out BattleUnit unit)
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
            Team team,
            HexCoordinate coordinate,
            int attackRange,
            float moveSpeed,
            int maxHealth,
            int attackDamage,
            out BattleUnit unit)
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
            Team team,
            HexCoordinate coordinate,
            int attackRange,
            float moveSpeed,
            int maxHealth,
            int attackDamage,
            float attackAnimationDuration,
            float attackHitNormalizedTime,
            float attackCooldownDuration,
            out BattleUnit unit)
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

            BattleUnit createdUnit = new BattleUnit(
                _nextUnitId,
                team,
                coordinate,
                attackRange,
                moveSpeed,
                maxHealth,
                attackDamage,
                attackAnimationDuration,
                attackHitNormalizedTime,
                attackCooldownDuration);

            if (!_board.TryPlace(createdUnit, coordinate))
            {
                return false;
            }

            _nextUnitId++;
            unit = createdUnit;
            return true;
        }

        public bool TryApplyBasicAttackDamage(BattleUnit attacker, BattleUnit target)
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

        public bool TrySetTarget(BattleUnit unit, BattleUnit target)
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

        bool IsRegistered(BattleUnit unit)
        {
            return unit != null
                && _board.TryGetUnit(unit.Id, out BattleUnit registeredUnit)
                && ReferenceEquals(registeredUnit, unit);
        }
    }
}
