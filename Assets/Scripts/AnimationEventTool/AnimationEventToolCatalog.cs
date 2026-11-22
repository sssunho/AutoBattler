#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using RaggedBanner.Animations;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.AnimationTools
{
    internal sealed class AnimationEventToolCatalog
    {
        internal sealed class Entry<T> where T : UnityEngine.Object
        {
            internal T Asset { get; }
            internal GUIContent Label { get; }
            internal string SearchText { get; }

            internal Entry(T asset, string path, string suffix = "")
            {
                Asset = asset;
                string text = asset.name + " — " + path + suffix;
                Label = new GUIContent(text, text);
                SearchText = text;
            }
        }

        Entry<GameObject>[] _prefabs = Array.Empty<Entry<GameObject>>();
        Entry<AnimationClip>[] _clips = Array.Empty<Entry<AnimationClip>>();

        internal Entry<GameObject>[] Prefabs => _prefabs;
        internal Entry<AnimationClip>[] Clips => _clips;

        internal void Refresh()
        {
            _prefabs = FindMainAssets<GameObject>("t:Prefab", "Assets/Prefabs/Combat");
            List<Entry<AnimationClip>> clips = new List<Entry<AnimationClip>>();
            HashSet<AnimationClip> seen = new HashSet<AnimationClip>();
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Art/Character/Animation" });

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

                for (int j = 0; j < assets.Length; j++)
                {
                    if (assets[j] is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal)
                        && seen.Add(clip))
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string _, out long localId);
                        clips.Add(new Entry<AnimationClip>(clip, path, " #" + localId));
                    }
                }
            }

            clips.Sort((first, second) => StringComparer.OrdinalIgnoreCase.Compare(first.SearchText, second.SearchText));
            _clips = clips.ToArray();
        }

        internal static CharacterAnimationProfile GetProfile(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours = prefab.GetComponents<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];

                if (behaviour == null || behaviour.GetType().FullName != "RaggedBanner.Combat.Unity.HexCharacterView")
                {
                    continue;
                }

                using (SerializedObject data = new SerializedObject(behaviour))
                {
                    SerializedProperty property = data.FindProperty("_animationProfile");
                    return property != null ? property.objectReferenceValue as CharacterAnimationProfile : null;
                }
            }

            return null;
        }

        static Entry<T>[] FindMainAssets<T>(string filter, string folder) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets(filter, new[] { folder });
            List<Entry<T>> entries = new List<Entry<T>>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);

                if (asset != null)
                {
                    entries.Add(new Entry<T>(asset, path));
                }
            }

            entries.Sort((first, second) => StringComparer.OrdinalIgnoreCase.Compare(first.SearchText, second.SearchText));
            return entries.ToArray();
        }
    }
}
#endif
