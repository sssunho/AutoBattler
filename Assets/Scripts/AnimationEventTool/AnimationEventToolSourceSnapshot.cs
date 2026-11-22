#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using RaggedBanner.Animations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RaggedBanner.AnimationTools
{
    internal sealed class AnimationEventToolSourceSnapshot
    {
        sealed class AssetState
        {
            internal string _path;
            internal string _fileHash;
            internal string _metaHash;
            internal Hash128 _dependencyHash;
            internal string _memory;
            internal string[] _dependencies;
        }

        sealed class ClipState
        {
            internal AnimationClip _clip;
            internal string _path;
            internal string _memory;
        }

        static readonly Regex _instanceId = new Regex("\\\"instanceID\\\"\\s*:\\s*(-?[0-9]+)");
        readonly List<AssetState> _assets = new List<AssetState>();
        readonly List<ClipState> _clips = new List<ClipState>();
        readonly GameObject _prefab;
        readonly CharacterAnimationProfile _profile;
        readonly bool _hadProfile;
        readonly string _profilePath;

        internal GameObject Prefab => _prefab;

        AnimationEventToolSourceSnapshot(GameObject prefab, CharacterAnimationProfile profile)
        {
            _prefab = prefab;
            _profile = profile;
            _hadProfile = profile != null;
            _profilePath = AssetDatabase.GetAssetPath(profile);
        }

        internal static bool TryCapture(GameObject prefab, CharacterAnimationProfile profile,
            out AnimationEventToolSourceSnapshot snapshot, out string error)
        {
            snapshot = null;

            if (prefab == null || !IsProjectAsset(prefab) || (profile != null && !IsProjectAsset(profile)))
            {
                error = "Preview sources must be saved project assets.";
                return false;
            }

            try
            {
                AnimationEventToolSourceSnapshot candidate = new AnimationEventToolSourceSnapshot(prefab, profile);
                HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
                AddDependencies(prefab, paths);
                AddDependencies(profile, paths);

                if (profile != null)
                {
                    for (int i = 0; i < profile.Animations.Count; i++)
                    {
                        CharacterAnimationEntry entry = profile.Animations[i];

                        if (entry != null && entry.Clip != null)
                        {
                            candidate.AddClip(entry.Clip, paths);
                        }
                    }
                }

                candidate.CapturePaths(paths);
                snapshot = candidate;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "Could not inspect preview sources: " + exception.Message;
                return false;
            }
        }

        internal bool Validate(out string error)
        {
            return ValidateExceptProfile(null, out error);
        }

        internal bool ValidateAfterPrefabLink(out string error)
        {
            return ValidateExceptProfile(AssetDatabase.GetAssetPath(_prefab), out error);
        }

        internal bool TryWithClip(AnimationClip clip, out AnimationEventToolSourceSnapshot snapshot, out string error)
        {
            snapshot = null;

            if (!Validate(out error))
            {
                return false;
            }

            try
            {
                AnimationEventToolSourceSnapshot candidate = new AnimationEventToolSourceSnapshot(_prefab, _profile);
                candidate._assets.AddRange(_assets);
                candidate._clips.AddRange(_clips);
                HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
                candidate.AddClip(clip, paths);
                candidate.CapturePaths(paths);

                if (!candidate.Validate(out error))
                {
                    return false;
                }

                snapshot = candidate;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "Could not add a preview source: " + exception.Message;
                return false;
            }
        }

        internal bool MatchesDocument(AnimationEventToolDocument document)
        {
            if (document == null || document.Source != _profile)
            {
                return false;
            }

            for (int i = 0; i < document.Entries.Count; i++)
            {
                AnimationClip clip = document.Entries[i].Clip;

                if (clip != null && !ContainsClip(clip))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool TryAfterSave(CharacterAnimationProfile saved, out AnimationEventToolSourceSnapshot snapshot, out string error)
        {
            snapshot = null;

            if (saved == null || !_hadProfile || AssetDatabase.GetAssetPath(saved) != _profilePath
                || !ValidateExceptProfile(_profilePath, out error))
            {
                error = "A preview source changed while saving. Reload the sources before continuing.";
                return false;
            }

            return TryCapture(_prefab, saved, out snapshot, out error);
        }

        void AddClip(AnimationClip clip, HashSet<string> paths)
        {
            if (clip == null || !IsProjectAsset(clip))
            {
                throw new InvalidOperationException("Animation clips must be saved project assets.");
            }

            if (ContainsClip(clip))
            {
                return;
            }

            _clips.Add(new ClipState
            {
                _clip = clip,
                _path = AssetDatabase.GetAssetPath(clip),
                _memory = SerializeObject(clip)
            });
            AddDependencies(clip, paths);
        }

        bool ContainsClip(AnimationClip clip)
        {
            for (int i = 0; i < _clips.Count; i++)
            {
                if (_clips[i]._clip == clip)
                {
                    return true;
                }
            }

            return false;
        }

        void CapturePaths(HashSet<string> paths)
        {
            for (int i = 0; i < _assets.Count; i++)
            {
                paths.Remove(_assets[i]._path);
            }

            List<string> sorted = new List<string>(paths);
            sorted.Sort(StringComparer.Ordinal);

            for (int i = 0; i < sorted.Count; i++)
            {
                string path = sorted[i];

                if (AssetDatabase.IsValidFolder(path))
                {
                    continue;
                }

                string fileHash = ReadFingerprint(path);
                string metaHash = ReadFingerprint(path + ".meta");

                if (path.StartsWith("Assets/", StringComparison.Ordinal) && (fileHash == "missing" || metaHash == "missing"))
                {
                    throw new FileNotFoundException("A preview source or its metadata is missing: " + path);
                }

                _assets.Add(new AssetState
                {
                    _path = path,
                    _fileHash = fileHash,
                    _metaHash = metaHash,
                    _dependencyHash = AssetDatabase.GetAssetDependencyHash(path),
                    _memory = ReadAssetMemory(path),
                    _dependencies = AssetDatabase.GetDependencies(path, true)
                });
            }
        }

        internal static string SerializeObject(Object value)
        {
            if (value == null)
            {
                return "null";
            }

            return _instanceId.Replace(EditorJsonUtility.ToJson(value), match =>
            {
                int instanceId = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                Object reference = EditorUtility.InstanceIDToObject(instanceId);

                if (reference != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(reference, out string guid, out long localId))
                {
                    return "\"assetReference\":\"" + guid + ":" + localId.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\"";
                }

                return match.Value;
            });
        }

        bool ValidateExceptProfile(string savedPath, out string error)
        {
            if (_prefab == null || (_hadProfile && _profile == null))
            {
                error = "A preview source was removed.";
                return false;
            }

            try
            {
                for (int i = 0; i < _clips.Count; i++)
                {
                    ClipState clip = _clips[i];

                    if (clip._clip == null || AssetDatabase.GetAssetPath(clip._clip) != clip._path
                        || SerializeObject(clip._clip) != clip._memory)
                    {
                        error = "An animation clip changed. Reload it before editing: " + clip._path;
                        return false;
                    }
                }

                for (int i = 0; i < _assets.Count; i++)
                {
                    AssetState state = _assets[i];
                    bool target = !string.IsNullOrEmpty(savedPath) && state._path == savedPath;
                    bool dependsOnTarget = !string.IsNullOrEmpty(savedPath) && Array.IndexOf(state._dependencies, savedPath) >= 0;

                    if ((!target && (ReadFingerprint(state._path) != state._fileHash
                        || ReadAssetMemory(state._path) != state._memory))
                        || ReadFingerprint(state._path + ".meta") != state._metaHash
                        || (!dependsOnTarget && AssetDatabase.GetAssetDependencyHash(state._path) != state._dependencyHash))
                    {
                        error = "A preview source changed: " + state._path;
                        return false;
                    }
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "Could not verify preview sources: " + exception.Message;
                return false;
            }
        }

        static void AddDependencies(Object asset, HashSet<string> paths)
        {
            if (asset == null)
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            paths.Add(path);
            string[] dependencies = AssetDatabase.GetDependencies(path, true);

            for (int i = 0; i < dependencies.Length; i++)
            {
                paths.Add(dependencies[i]);
            }
        }

        static string ReadAssetMemory(string path)
        {
            Object main = AssetDatabase.LoadMainAssetAtPath(path);

            if (main == null)
            {
                return "null";
            }

            if (main is GameObject prefab)
            {
                StringBuilder builder = new StringBuilder();
                Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);

                for (int i = 0; i < transforms.Length; i++)
                {
                    builder.Append(SerializeObject(transforms[i].gameObject));
                    Component[] components = transforms[i].GetComponents<Component>();

                    for (int j = 0; j < components.Length; j++)
                    {
                        builder.Append(SerializeObject(components[j]));

                        if (components[j] is Animator animator)
                        {
                            builder.Append(SerializeObject(animator.avatar));
                        }
                    }
                }

                return builder.ToString();
            }

            if (main is ScriptableObject || main is AnimationClip || main is Avatar || main is Material)
            {
                return SerializeObject(main);
            }

            return main.GetType().FullName;
        }

        internal static string ReadFingerprint(string path)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return string.Empty;
            }

            string project = Directory.GetParent(Application.dataPath).FullName;
            string absolute = Path.GetFullPath(Path.Combine(project, path));
            string assetRoot = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;

            if (!absolute.StartsWith(assetRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Source path is outside Assets.");
            }

            if (!File.Exists(absolute))
            {
                return "missing";
            }

            using (SHA256 hash = SHA256.Create())
            using (FileStream stream = File.OpenRead(absolute))
            {
                return Convert.ToBase64String(hash.ComputeHash(stream));
            }
        }

        static bool IsProjectAsset(Object value)
        {
            string path = AssetDatabase.GetAssetPath(value);
            return AssetDatabase.Contains(value) && path.StartsWith("Assets/", StringComparison.Ordinal);
        }
    }
}
#endif
