using UnityEngine;

namespace RaggedBanner.MercenaryData
{
    [CreateAssetMenu(menuName = "RaggedBanner/Trait Settings", fileName = "TraitSettings")]
    public sealed class TraitSettings : ScriptableObject
    {
        [SerializeField]
        DirectionalPhysicalNegationSettings _directionalPhysicalNegation;

        public DirectionalPhysicalNegationSettings DirectionalPhysicalNegation => _directionalPhysicalNegation;

        public bool Validate(out string error)
        {
            if (!_directionalPhysicalNegation.Validate(out string settingsError))
            {
                error = $"TraitSettings '{name}': {settingsError}";
                return false;
            }

            error = null;
            return true;
        }
    }
}
