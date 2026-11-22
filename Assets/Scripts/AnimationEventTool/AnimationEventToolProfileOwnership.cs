#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using RaggedBanner.Animations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RaggedBanner.AnimationTools
{
    internal enum AnimationProfileSetupStage
    {
        BeforeProfileWrite,
        AfterProfileWrite,
        BeforePrefabWrite,
        AfterPrefabWrite
    }

    internal static class AnimationEventToolProfileOwnership
    {
        const string _defaultFolder = "Assets/GameData/Animations/Profiles";
        static Action<AnimationProfileSetupStage> _setupTestHook;

        internal static Action<AnimationProfileSetupStage> SetupTestHook
        {
            get => _setupTestHook;
            set => _setupTestHook = value;
        }

        internal static bool TryValidateOwnership(GameObject prefab, CharacterAnimationProfile profile, out string error)
        {
            if (!TryValidateSelection(prefab, profile, out _, out error))
            {
                return false;
            }

            if (profile == null)
            {
                error = "Create a character-specific animation list before editing.";
                return false;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            List<string> shared = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject other = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (other != null && other != prefab && AnimationEventToolCatalog.GetProfile(other) == profile)
                {
                    shared.Add(path);
                }
            }

            if (shared.Count > 0)
            {
                shared.Sort(StringComparer.Ordinal);
                error = "This animation list is shared. Create a character-specific copy first. Other prefabs: "
                    + string.Join(", ", shared);
                return false;
            }

            error = null;
            return true;
        }

        internal static bool TryCreateDedicated(GameObject prefab, CharacterAnimationProfile source,
            out CharacterAnimationProfile created, out string recoveryPath, out bool uncertain, out string error,
            string destinationFolder = _defaultFolder)
        {
            created = null;
            recoveryPath = null;
            uncertain = false;

            if (!TryPreflight(prefab, source, out error)
                || !AnimationEventToolSourceSnapshot.TryCapture(prefab, source, out AnimationEventToolSourceSnapshot snapshot, out error))
            {
                return false;
            }

            if (!IsSafeFolder(destinationFolder))
            {
                error = "The animation list destination must be a folder inside Assets.";
                return false;
            }

            CharacterAnimationProfile target = null;
            GameObject contents = null;
            bool writeAttempted = false;
            bool linkAttempted = false;

            try
            {
                target = source != null ? Object.Instantiate(source) : ScriptableObject.CreateInstance<CharacterAnimationProfile>();
                target.name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(prefab)) + "AnimationProfile";
                target.hideFlags = HideFlags.None;
                string expected = AnimationEventToolSourceSnapshot.SerializeObject(target);
                _setupTestHook?.Invoke(AnimationProfileSetupStage.BeforeProfileWrite);

                if (!snapshot.Validate(out error) || !TryPreflight(prefab, source, out error))
                {
                    return false;
                }

                EnsureFolder(destinationFolder);
                recoveryPath = AssetDatabase.GenerateUniqueAssetPath(destinationFolder + "/" + target.name + ".asset");
                writeAttempted = true;
                AssetDatabase.CreateAsset(target, recoveryPath);
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
                _setupTestHook?.Invoke(AnimationProfileSetupStage.AfterProfileWrite);
                AssetDatabase.ImportAsset(recoveryPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                created = AssetDatabase.LoadAssetAtPath<CharacterAnimationProfile>(recoveryPath);

                if (created == null || AnimationEventToolSourceSnapshot.SerializeObject(created) != expected)
                {
                    throw new InvalidOperationException("The saved animation list did not match its source copy.");
                }

                string profileFingerprint = AnimationEventToolSourceSnapshot.ReadFingerprint(recoveryPath);
                string profileMetaFingerprint = AnimationEventToolSourceSnapshot.ReadFingerprint(recoveryPath + ".meta");

                if (!snapshot.Validate(out error) || !TryPreflight(prefab, source, out error))
                {
                    throw new InvalidOperationException(error);
                }

                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                Component view = FindView(contents);

                if (view == null || AnimationEventToolCatalog.GetProfile(contents) != source)
                {
                    throw new InvalidOperationException("The prefab connection changed before linking its new animation list.");
                }

                using (SerializedObject data = new SerializedObject(view))
                {
                    data.FindProperty("_animationProfile").objectReferenceValue = created;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }

                _setupTestHook?.Invoke(AnimationProfileSetupStage.BeforePrefabWrite);

                if (!snapshot.Validate(out error) || !TryPreflight(prefab, source, out error))
                {
                    throw new InvalidOperationException(error);
                }

                linkAttempted = true;
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(contents, prefabPath, out bool success);

                if (!success || saved == null)
                {
                    throw new InvalidOperationException("The selected prefab could not be saved.");
                }

                PrefabUtility.UnloadPrefabContents(contents);
                contents = null;
                _setupTestHook?.Invoke(AnimationProfileSetupStage.AfterPrefabWrite);
                AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                GameObject reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                created = AssetDatabase.LoadAssetAtPath<CharacterAnimationProfile>(recoveryPath);

                if (created == null || AnimationEventToolSourceSnapshot.SerializeObject(created) != expected
                    || !snapshot.ValidateAfterPrefabLink(out error)
                    || !TryValidateOwnership(reloaded, created, out error)
                    || AnimationEventToolSourceSnapshot.ReadFingerprint(recoveryPath) != profileFingerprint
                    || AnimationEventToolSourceSnapshot.ReadFingerprint(recoveryPath + ".meta") != profileMetaFingerprint)
                {
                    throw new InvalidOperationException(error ?? "The profile or prefab read-back did not match.");
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                uncertain = writeAttempted;
                error = writeAttempted
                    ? "Character list setup needs a reload before retrying. Generated asset: " + recoveryPath
                        + ". Prefab link " + (linkAttempted ? "was attempted" : "was not attempted") + ". " + exception.Message
                    : "Could not prepare a character-specific animation list: " + exception.Message;
                return false;
            }
            finally
            {
                if (contents != null)
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }

                if (target != null && !AssetDatabase.Contains(target))
                {
                    Object.DestroyImmediate(target);
                }
            }
        }

        static bool TryPreflight(GameObject prefab, CharacterAnimationProfile source, out string error)
        {
            if (!TryValidateSelection(prefab, source, out Component view, out error))
            {
                return false;
            }

            if (HasUnsavedChanges(prefab))
            {
                error = "Save the selected prefab's existing changes before creating its animation list.";
                return false;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            List<string> dependents = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject other = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (other != null && other != prefab && InheritsProfileFrom(FindView(other), view))
                {
                    dependents.Add(path);
                }
            }

            if (dependents.Count > 0)
            {
                dependents.Sort(StringComparer.Ordinal);
                error = "Create dedicated lists for these descendant prefabs first, starting with leaf variants: "
                    + string.Join(", ", dependents);
                return false;
            }

            error = null;
            return true;
        }

        static bool TryValidateSelection(GameObject prefab, CharacterAnimationProfile expected,
            out Component view, out string error)
        {
            view = FindView(prefab);
            string path = AssetDatabase.GetAssetPath(prefab);

            if (prefab == null || view == null || !AssetDatabase.Contains(prefab)
                || !path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                error = "Select a saved project character prefab with HexCharacterView.";
                return false;
            }

            if (AnimationEventToolCatalog.GetProfile(prefab) != expected)
            {
                error = "The selected prefab's animation list changed. Reload the character.";
                return false;
            }

            if (expected != null && (!AssetDatabase.Contains(expected)
                || !AssetDatabase.GetAssetPath(expected).StartsWith("Assets/", StringComparison.Ordinal)))
            {
                error = "The character animation list must be saved inside Assets.";
                return false;
            }

            error = null;
            return true;
        }

        static bool InheritsProfileFrom(Component candidate, Component source)
        {
            while (candidate != null)
            {
                using (SerializedObject data = new SerializedObject(candidate))
                {
                    SerializedProperty property = data.FindProperty("_animationProfile");

                    if (property == null || property.prefabOverride)
                    {
                        return false;
                    }
                }

                candidate = PrefabUtility.GetCorrespondingObjectFromSource(candidate);

                if (candidate == source)
                {
                    return true;
                }
            }

            return false;
        }

        static Component FindView(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours = prefab.GetComponents<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];

                if (behaviour != null && behaviour.GetType().FullName == "RaggedBanner.Combat.Unity.HexCharacterView")
                {
                    return behaviour;
                }
            }

            return null;
        }

        static bool HasUnsavedChanges(GameObject prefab)
        {
            Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < transforms.Length; i++)
            {
                if (EditorUtility.IsDirty(transforms[i].gameObject))
                {
                    return true;
                }

                Component[] components = transforms[i].GetComponents<Component>();

                for (int j = 0; j < components.Length; j++)
                {
                    if (components[j] != null && EditorUtility.IsDirty(components[j]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static bool IsSafeFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)
                || path.IndexOf('\\') >= 0)
            {
                return false;
            }

            try
            {
                string absolute = Path.GetFullPath(Path.Combine(Directory.GetParent(Application.dataPath).FullName, path));
                return absolute.StartsWith(Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
