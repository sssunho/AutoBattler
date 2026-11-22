using RaggedBanner.HexGrid;
using UnityEngine;

namespace RaggedBanner.Combat.Unity
{
    [DisallowMultipleComponent]
    public sealed class BattleEnvironmentDebugController : MonoBehaviour
    {
        enum EnvironmentSlot
        {
            Ground,
            Air
        }

        [SerializeField]
        BattleRunner _runner;

        [Header("Target Hex")]
        [SerializeField]
        int _q;

        [SerializeField]
        int _r;

        [Header("Environment Effect")]
        [SerializeField]
        EnvironmentSlot _slot;

        [SerializeField]
        BattleGroundEffect _groundEffect = BattleGroundEffect.Burning;

        [SerializeField]
        BattleAirEffect _airEffect = BattleAirEffect.Smoke;

        [Header("Last Result")]
        [SerializeField, TextArea]
        string _lastResult = "Play 모드에서 컴포넌트 메뉴의 Apply Selected Effect 또는 Clear 명령을 사용하세요.";

        [ContextMenu("Apply Selected Effect")]
        void ApplySelectedEffect()
        {
            if (!CanEdit())
            {
                return;
            }

            HexCoordinate coordinate = new HexCoordinate(_q, _r);
            bool applied;
            string error;

            if (_slot == EnvironmentSlot.Ground)
            {
                applied = _runner.TryApplyGroundEffect(coordinate, _groundEffect, out error);
            }
            else
            {
                applied = _runner.TryApplyAirEffect(coordinate, _airEffect, out error);
            }

            _lastResult = applied ? $"{coordinate}: 환경효과를 적용했습니다." : error;
        }

        [ContextMenu("Clear Selected Slot")]
        void ClearSelectedSlot()
        {
            Clear(_slot == EnvironmentSlot.Ground, _slot == EnvironmentSlot.Air);
        }

        [ContextMenu("Clear Both Slots")]
        void ClearBothSlots()
        {
            Clear(true, true);
        }

        void Clear(bool ground, bool air)
        {
            if (!CanEdit())
            {
                return;
            }

            HexCoordinate coordinate = new HexCoordinate(_q, _r);
            _lastResult = _runner.TryClearEnvironment(coordinate, ground, air, out string error)
                ? $"{coordinate}: 선택한 환경 슬롯을 비웠습니다." : error;
        }

        bool CanEdit()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || _runner == null)
            {
                _lastResult = "Play 모드의 활성 컴포넌트와 BattleRunner 연결이 필요합니다.";
                return false;
            }

            if (_slot != EnvironmentSlot.Ground && _slot != EnvironmentSlot.Air)
            {
                _lastResult = "올바른 환경 슬롯을 선택하세요.";
                return false;
            }

            return true;
        }
    }
}
