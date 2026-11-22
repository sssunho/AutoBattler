#if UNITY_EDITOR
using System;
using UnityEditor;

namespace RaggedBanner.AnimationTools
{
    internal sealed class AnimationEventToolAssetPostprocessor : AssetPostprocessor
    {
        static Action _assetsChanged;

        internal static event Action AssetsChanged
        {
            add => _assetsChanged += value;
            remove => _assetsChanged -= value;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importedAssets.Length != 0 || deletedAssets.Length != 0
                || movedAssets.Length != 0 || movedFromAssetPaths.Length != 0)
            {
                _assetsChanged?.Invoke();
            }
        }
    }
}
#endif
