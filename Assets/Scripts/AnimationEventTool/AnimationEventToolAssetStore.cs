#if UNITY_EDITOR
using System;
using System.IO;
using RaggedBanner.Animations;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.AnimationTools
{
    internal enum AnimationEventSaveResult
    {
        Saved,
        Rejected,
        Uncertain
    }

    internal enum AnimationEventSaveStage
    {
        BeforeWrite,
        AfterWrite,
        AfterReload
    }

    internal static class AnimationEventToolAssetStore
    {
        static Action<AnimationEventSaveStage> _saveTestHook;

        internal static Action<AnimationEventSaveStage> SaveTestHook
        {
            get => _saveTestHook;
            set => _saveTestHook = value;
        }

        internal static AnimationEventSaveResult TrySave(AnimationEventToolDocument document,
            AnimationEventToolSourceSnapshot snapshot, out AnimationEventToolSourceSnapshot newSnapshot, out string error)
        {
            newSnapshot = null;

            if (document == null || document.Source == null || snapshot == null || document.IsSaveUncertain)
            {
                error = "Reload the character animation list before saving.";
                return AnimationEventSaveResult.Rejected;
            }

            if (!snapshot.MatchesDocument(document) || !document.SourceMatches())
            {
                error = "The loaded character animation list or its sources changed. Reload before saving.";
                return AnimationEventSaveResult.Rejected;
            }

            if (!snapshot.Validate(out error)
                || !AnimationEventToolProfileOwnership.TryValidateOwnership(snapshot.Prefab, document.Source, out error)
                || !document.TryExport(out CharacterAnimationEntry[] entries, out error))
            {
                return AnimationEventSaveResult.Rejected;
            }

            CharacterAnimationProfile target = document.Source;
            string path = AssetDatabase.GetAssetPath(target);

            if (!IsSafeAssetPath(path) || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                error = "The character animation list must be a saved project .asset file.";
                return AnimationEventSaveResult.Rejected;
            }

            string previousJson = EditorJsonUtility.ToJson(target);
            bool previouslyDirty = EditorUtility.IsDirty(target);
            bool writeAttempted = false;

            try
            {
                WriteEntries(target, entries);
                _saveTestHook?.Invoke(AnimationEventSaveStage.BeforeWrite);
                writeAttempted = true;
                document.MarkWriteAttempt(path);
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
                _saveTestHook?.Invoke(AnimationEventSaveStage.AfterWrite);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                CharacterAnimationProfile reloaded = AssetDatabase.LoadAssetAtPath<CharacterAnimationProfile>(path);

                if (!MatchesEntries(reloaded, entries, out error))
                {
                    document.MarkUncertain();
                    return AnimationEventSaveResult.Uncertain;
                }

                string verifiedFingerprint = AnimationEventToolSourceSnapshot.ReadFingerprint(path);

                if (verifiedFingerprint == "missing")
                {
                    document.MarkUncertain();
                    error = "The saved character animation list is missing from disk. Reload before continuing.";
                    return AnimationEventSaveResult.Uncertain;
                }

                _saveTestHook?.Invoke(AnimationEventSaveStage.AfterReload);

                if (!MatchesEntries(reloaded, entries, out error)
                    || !AnimationEventToolProfileOwnership.TryValidateOwnership(snapshot.Prefab, reloaded, out error)
                    || !snapshot.TryAfterSave(reloaded, out newSnapshot, out error))
                {
                    newSnapshot = null;
                    document.MarkUncertain();
                    return AnimationEventSaveResult.Uncertain;
                }

                if (AnimationEventToolSourceSnapshot.ReadFingerprint(path) != verifiedFingerprint)
                {
                    newSnapshot = null;
                    document.MarkUncertain();
                    error = "The saved character animation list changed on disk during verification. Reload before continuing.";
                    return AnimationEventSaveResult.Uncertain;
                }

                if (!MatchesEntries(reloaded, entries, out error))
                {
                    newSnapshot = null;
                    document.MarkUncertain();
                    return AnimationEventSaveResult.Uncertain;
                }

                document.MarkSaved(reloaded);
                error = null;
                return AnimationEventSaveResult.Saved;
            }
            catch (Exception exception)
            {
                if (writeAttempted)
                {
                    newSnapshot = null;
                    document.MarkUncertain();
                    error = "Save result is uncertain. Reload the disk result before saving again: " + exception.Message;
                    return AnimationEventSaveResult.Uncertain;
                }

                if (target != null)
                {
                    EditorJsonUtility.FromJsonOverwrite(previousJson, target);

                    if (previouslyDirty)
                    {
                        EditorUtility.SetDirty(target);
                    }
                    else
                    {
                        EditorUtility.ClearDirty(target);
                    }
                }

                error = "Could not save before writing the character animation list: " + exception.Message;
                return AnimationEventSaveResult.Rejected;
            }
        }

        internal static bool TryReload(AnimationEventToolDocument document,
            out AnimationEventToolDocument reloadedDocument, out string error)
        {
            reloadedDocument = null;

            if (document == null || !IsSafeAssetPath(document.RecoveryPath))
            {
                error = "There is no saved character animation list to reload.";
                return false;
            }

            try
            {
                string path = document.RecoveryPath;
                string selectedKey = document.SelectedEntry?.Key;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                CharacterAnimationProfile source = AssetDatabase.LoadAssetAtPath<CharacterAnimationProfile>(path);

                if (!AnimationEventToolDocument.TryOpen(source, out reloadedDocument, out error))
                {
                    return false;
                }

                for (int i = 0; i < reloadedDocument.Entries.Count; i++)
                {
                    if (string.Equals(reloadedDocument.Entries[i].Key, selectedKey, StringComparison.Ordinal))
                    {
                        reloadedDocument.TrySelectEntry(reloadedDocument.Entries[i]);
                        break;
                    }
                }

                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = "Could not reload the disk character animation list: " + exception.Message;
                return false;
            }
        }

        static bool MatchesEntries(CharacterAnimationProfile saved, CharacterAnimationEntry[] entries, out string error)
        {
            if (saved == null || !saved.Validate(out error) || saved.Animations.Count != entries.Length)
            {
                error = "The reloaded character animation list did not match the working copy.";
                return false;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                CharacterAnimationEntry expected = entries[i];
                CharacterAnimationEntry actual = saved.Animations[i];

                if (actual == null || !string.Equals(actual.Key, expected.Key, StringComparison.Ordinal)
                    || actual.Clip != expected.Clip || actual.AuthoredFrameRate != expected.AuthoredFrameRate
                    || actual.AuthoredLength != expected.AuthoredLength || actual.Events.Count != expected.Events.Count)
                {
                    error = "The reloaded animation entry did not match the working copy: " + expected.Key;
                    return false;
                }

                for (int j = 0; j < expected.Events.Count; j++)
                {
                    AnimationEventKey expectedKey = expected.Events[j];
                    AnimationEventKey actualKey = actual.Events[j];

                    if (actualKey.Frame != expectedKey.Frame || actualKey.EventKind != expectedKey.EventKind
                        || actualKey.AttackKind != expectedKey.AttackKind)
                    {
                        error = "The reloaded event keys did not match the working copy: " + expected.Key;
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        static void WriteEntries(CharacterAnimationProfile target, CharacterAnimationEntry[] entries)
        {
            SerializedObject data = new SerializedObject(target);
            SerializedProperty animations = data.FindProperty("_animations");
            animations.arraySize = entries.Length;

            for (int i = 0; i < entries.Length; i++)
            {
                CharacterAnimationEntry entry = entries[i];
                SerializedProperty animation = animations.GetArrayElementAtIndex(i);
                animation.FindPropertyRelative("_key").stringValue = entry.Key;
                animation.FindPropertyRelative("_clip").objectReferenceValue = entry.Clip;
                animation.FindPropertyRelative("_authoredFrameRate").floatValue = entry.AuthoredFrameRate;
                animation.FindPropertyRelative("_authoredLength").floatValue = entry.AuthoredLength;
                SerializedProperty events = animation.FindPropertyRelative("_events");
                events.arraySize = entry.Events.Count;

                for (int j = 0; j < entry.Events.Count; j++)
                {
                    SerializedProperty item = events.GetArrayElementAtIndex(j);
                    item.FindPropertyRelative("_frame").intValue = entry.Events[j].Frame;
                    item.FindPropertyRelative("_eventKind").intValue = (int)entry.Events[j].EventKind;
                    item.FindPropertyRelative("_attackKind").intValue = (int)entry.Events[j].AttackKind;
                }
            }

            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool IsSafeAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)
                || path.IndexOf('\\') >= 0)
            {
                return false;
            }

            string project = Directory.GetParent(Application.dataPath).FullName;
            string absolute = Path.GetFullPath(Path.Combine(project, path));
            string root = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            return absolute.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
