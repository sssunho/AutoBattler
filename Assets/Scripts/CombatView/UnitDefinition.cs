using UnityEngine;

namespace AutoBattler.Combat.Unity
{
    [CreateAssetMenu(fileName = "UnitDefinition", menuName = "AutoBattler/Unit Definition")]
    public sealed class UnitDefinition : ScriptableObject
    {
        [SerializeField]
        string _id;

        [SerializeField]
        string _displayName;

        [SerializeField]
        UnitStat _stat = new UnitStat();

        [SerializeField]
        BattleUnitView _viewPrefab;

        public string Id => _id;
        public string DisplayName => _displayName;
        public UnitStat Stat => _stat;
        public BattleUnitView ViewPrefab => _viewPrefab;

        internal bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(_id)
                && !string.IsNullOrWhiteSpace(_displayName)
                && _stat != null
                && _stat.IsValid()
                && _viewPrefab != null;
        }
    }
}
