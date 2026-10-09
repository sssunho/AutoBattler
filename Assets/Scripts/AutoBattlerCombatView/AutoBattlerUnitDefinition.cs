using UnityEngine;

namespace AutoBattler.Combat.Unity
{
    [CreateAssetMenu(fileName = "AutoBattlerUnitDefinition", menuName = "AutoBattler/Unit Definition")]
    public sealed class AutoBattlerUnitDefinition : ScriptableObject
    {
        [SerializeField]
        string _id;

        [SerializeField]
        string _displayName;

        [SerializeField]
        AutoBattlerUnitStat _stat = new AutoBattlerUnitStat();

        [SerializeField]
        AutoBattlerBattleUnitView _viewPrefab;

        public string Id => _id;
        public string DisplayName => _displayName;
        public AutoBattlerUnitStat Stat => _stat;
        public AutoBattlerBattleUnitView ViewPrefab => _viewPrefab;

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
