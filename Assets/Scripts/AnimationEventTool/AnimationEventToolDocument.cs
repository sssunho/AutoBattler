#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using RaggedBanner.Animations;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.AnimationTools
{
    internal sealed class AnimationEventToolKey
    {
        readonly long _order;
        int _frame;
        AnimationAttackKind _attackKind;

        internal int Frame => _frame;
        internal AnimationAttackKind AttackKind => _attackKind;
        internal long Order => _order;

        internal AnimationEventToolKey(int frame, AnimationAttackKind attackKind, long order)
        {
            _frame = frame;
            _attackKind = attackKind;
            _order = order;
        }

        internal void Set(int frame, AnimationAttackKind attackKind)
        {
            _frame = frame;
            _attackKind = attackKind;
        }
    }

    internal sealed class AnimationEventToolEntry
    {
        readonly List<AnimationEventToolKey> _keys = new List<AnimationEventToolKey>();
        string _key;
        AnimationClip _clip;
        float _frameRate;
        float _length;
        int _lastFrame;
        long _nextOrder;

        internal string Key => _key;
        internal AnimationClip Clip => _clip;
        internal float FrameRate => _frameRate;
        internal float Length => _length;
        internal int LastFrame => _lastFrame;
        internal IReadOnlyList<AnimationEventToolKey> Keys => _keys;
        internal bool NeedsReauthor => _clip == null || _clip.legacy || _clip.frameRate != _frameRate
            || _clip.length != _length;

        internal AnimationEventToolEntry(string key, AnimationClip clip, float frameRate, float length,
            IReadOnlyList<AnimationEventKey> events)
        {
            _key = key;
            _clip = clip;
            _frameRate = frameRate;
            _length = length;
            AnimationFrameUtility.TryGetLastFrame(length, frameRate, out _lastFrame);

            for (int i = 0; i < events.Count; i++)
            {
                AnimationEventKey value = events[i];
                _keys.Add(new AnimationEventToolKey(value.Frame, value.AttackKind, _nextOrder++));
            }
        }

        internal void Rename(string key)
        {
            _key = key;
        }

        internal AnimationEventToolKey Add(int frame)
        {
            AnimationEventToolKey key = new AnimationEventToolKey(frame, AnimationAttackKind.Melee, _nextOrder++);
            _keys.Add(key);
            _keys.Sort(CompareKeys);
            return key;
        }

        internal bool Remove(AnimationEventToolKey key)
        {
            return _keys.Remove(key);
        }

        internal bool Contains(AnimationEventToolKey key)
        {
            return _keys.Contains(key);
        }

        internal void Edit(AnimationEventToolKey key, int frame, AnimationAttackKind attackKind)
        {
            key.Set(frame, attackKind);
            _keys.Sort(CompareKeys);
        }

        internal void Reauthor(AnimationClip clip)
        {
            _clip = clip;
            _frameRate = clip.frameRate;
            _length = clip.length;
            AnimationFrameUtility.TryGetLastFrame(_length, _frameRate, out _lastFrame);
            _keys.Clear();
            _nextOrder = 0;
        }

        internal AnimationEventKey[] ExportKeys()
        {
            AnimationEventKey[] events = new AnimationEventKey[_keys.Count];

            for (int i = 0; i < _keys.Count; i++)
            {
                AnimationEventToolKey key = _keys[i];
                events[i] = new AnimationEventKey(key.Frame, AnimationEventKind.AttackHit, key.AttackKind);
            }

            return events;
        }

        static int CompareKeys(AnimationEventToolKey first, AnimationEventToolKey second)
        {
            int byFrame = first.Frame.CompareTo(second.Frame);
            return byFrame != 0 ? byFrame : first.Order.CompareTo(second.Order);
        }
    }

    internal sealed class AnimationEventToolDocument
    {
        readonly List<AnimationEventToolEntry> _entries = new List<AnimationEventToolEntry>();
        CharacterAnimationProfile _source;
        AnimationEventToolEntry _selectedEntry;
        string _sourceSnapshot;
        string _recoveryPath;
        bool _isDirty;
        bool _isSaveUncertain;

        internal CharacterAnimationProfile Source => _source;
        internal IReadOnlyList<AnimationEventToolEntry> Entries => _entries;
        internal AnimationEventToolEntry SelectedEntry => _selectedEntry;
        internal AnimationClip Clip => _selectedEntry?.Clip;
        internal float FrameRate => _selectedEntry?.FrameRate ?? 0f;
        internal float Length => _selectedEntry?.Length ?? 0f;
        internal int LastFrame => _selectedEntry?.LastFrame ?? 0;
        internal IReadOnlyList<AnimationEventToolKey> Keys => _selectedEntry == null
            ? Array.Empty<AnimationEventToolKey>() : _selectedEntry.Keys;
        internal string RecoveryPath => _recoveryPath;
        internal bool IsDirty => _isDirty;
        internal bool IsSaveUncertain => _isSaveUncertain;
        internal bool NeedsReauthor => _selectedEntry != null && _selectedEntry.NeedsReauthor;

        internal static bool TryOpen(CharacterAnimationProfile source,
            out AnimationEventToolDocument document, out string error)
        {
            document = null;

            if (source == null || source.Animations == null)
            {
                error = "The character animation profile is missing or has no animation list.";
                return false;
            }

            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < source.Animations.Count; i++)
            {
                CharacterAnimationEntry entry = source.Animations[i];

                if (entry == null || string.IsNullOrWhiteSpace(entry.Key)
                    || !string.Equals(entry.Key, entry.Key.Trim(), StringComparison.Ordinal) || !names.Add(entry.Key)
                    || entry.Events == null || !AnimationFrameUtility.TryGetLastFrame(entry.AuthoredLength,
                        entry.AuthoredFrameRate, out int lastFrame))
                {
                    error = "The profile contains a missing entry, invalid key, or invalid authored metadata.";
                    return false;
                }

                int previousFrame = -1;

                for (int j = 0; j < entry.Events.Count; j++)
                {
                    AnimationEventKey key = entry.Events[j];

                    if (key == null || key.Frame < previousFrame || key.Frame < 0 || key.Frame > lastFrame
                        || key.EventKind != AnimationEventKind.AttackHit || !IsValidKind(key.AttackKind))
                    {
                        error = "The profile contains invalid or unordered event keys: " + entry.Key;
                        return false;
                    }

                    previousFrame = key.Frame;
                }
            }

            document = new AnimationEventToolDocument
            {
                _source = source,
                _sourceSnapshot = AnimationEventToolSourceSnapshot.SerializeObject(source),
                _recoveryPath = AssetDatabase.GetAssetPath(source)
            };

            for (int i = 0; i < source.Animations.Count; i++)
            {
                CharacterAnimationEntry entry = source.Animations[i];
                document._entries.Add(new AnimationEventToolEntry(entry.Key, entry.Clip,
                    entry.AuthoredFrameRate, entry.AuthoredLength, entry.Events));
            }

            if (document._entries.Count > 0)
            {
                document._selectedEntry = document._entries[0];
            }

            error = null;
            return true;
        }

        internal bool TrySelectEntry(AnimationEventToolEntry entry)
        {
            if (_isSaveUncertain || (entry != null && !_entries.Contains(entry)))
            {
                return false;
            }

            _selectedEntry = entry;
            return true;
        }

        internal bool TryAddEntry(string key, AnimationClip clip, out AnimationEventToolEntry entry, out string error)
        {
            entry = null;

            if (!CanChangeList(out error) || !TryNormalizeKey(key, null, out string normalized, out error))
            {
                return false;
            }

            if (clip == null || !AnimationDefinition.ValidateData(clip, clip.frameRate, clip.length,
                Array.Empty<AnimationEventKey>(), out error))
            {
                error = "Choose a non-Legacy clip with a valid length and frame rate.";
                return false;
            }

            entry = new AnimationEventToolEntry(normalized, clip, clip.frameRate, clip.length, Array.Empty<AnimationEventKey>());
            _entries.Add(entry);
            _selectedEntry = entry;
            _isDirty = true;
            error = null;
            return true;
        }

        internal bool TryRenameEntry(AnimationEventToolEntry entry, string key, out string error)
        {
            if (!CanChangeList(out error))
            {
                return false;
            }

            if (entry == null || !_entries.Contains(entry))
            {
                error = "Choose an animation from this character's list.";
                return false;
            }

            if (!TryNormalizeKey(key, entry, out string normalized, out error))
            {
                return false;
            }

            if (!string.Equals(entry.Key, normalized, StringComparison.Ordinal))
            {
                entry.Rename(normalized);
                _isDirty = true;
            }

            error = null;
            return true;
        }

        internal bool RemoveEntry(AnimationEventToolEntry entry)
        {
            if (_isSaveUncertain || entry == null || !_entries.Remove(entry))
            {
                return false;
            }

            if (_selectedEntry == entry)
            {
                _selectedEntry = _entries.Count == 0 ? null : _entries[0];
            }

            _isDirty = true;
            return true;
        }

        internal AnimationEventToolKey Add(int frame)
        {
            if (!CanEditFrame(frame))
            {
                return null;
            }

            _isDirty = true;
            return _selectedEntry.Add(frame);
        }

        internal bool Remove(AnimationEventToolKey key)
        {
            if (_isSaveUncertain || _selectedEntry == null || NeedsReauthor || !_selectedEntry.Remove(key))
            {
                return false;
            }

            _isDirty = true;
            return true;
        }

        internal bool TryEdit(AnimationEventToolKey key, int frame, AnimationAttackKind attackKind, out string error)
        {
            if (key == null || !CanEditFrame(frame) || !_selectedEntry.Contains(key) || !IsValidKind(attackKind))
            {
                error = "Select an editable key, a valid frame, and a supported attack kind.";
                return false;
            }

            if (key.Frame != frame || key.AttackKind != attackKind)
            {
                _selectedEntry.Edit(key, frame, attackKind);
                _isDirty = true;
            }

            error = null;
            return true;
        }

        internal bool TryReauthor(AnimationClip clip, out string error)
        {
            if (!CanChangeList(out error))
            {
                return false;
            }

            if (_selectedEntry == null)
            {
                error = "Choose an animation entry before replacing its clip.";
                return false;
            }

            if (clip == null || !AnimationDefinition.ValidateData(clip, clip.frameRate, clip.length,
                Array.Empty<AnimationEventKey>(), out error))
            {
                error = "Choose a non-Legacy clip with a valid length and frame rate.";
                return false;
            }

            _selectedEntry.Reauthor(clip);
            _isDirty = true;
            error = null;
            return true;
        }

        internal AnimationEventKey[] ExportKeys()
        {
            return _selectedEntry == null ? Array.Empty<AnimationEventKey>() : _selectedEntry.ExportKeys();
        }

        internal bool TryExport(out CharacterAnimationEntry[] entries, out string error)
        {
            entries = null;

            if (!CanChangeList(out error))
            {
                return false;
            }

            CharacterAnimationEntry[] result = new CharacterAnimationEntry[_entries.Count];
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < _entries.Count; i++)
            {
                AnimationEventToolEntry entry = _entries[i];

                if (string.IsNullOrWhiteSpace(entry.Key) || !names.Add(entry.Key)
                    || !string.Equals(entry.Key, entry.Key.Trim(), StringComparison.Ordinal))
                {
                    error = "Animation keys must be non-empty and unique within the character.";
                    return false;
                }

                CharacterAnimationEntry exported = new CharacterAnimationEntry(entry.Key, entry.Clip,
                    entry.FrameRate, entry.Length, entry.ExportKeys());

                if (!exported.Validate(out error))
                {
                    error = entry.Key + ": " + error;
                    return false;
                }

                result[i] = exported;
            }

            entries = result;
            error = null;
            return true;
        }

        internal bool SourceMatches()
        {
            return _source != null && AnimationEventToolSourceSnapshot.SerializeObject(_source) == _sourceSnapshot
                && AssetDatabase.GetAssetPath(_source) == _recoveryPath;
        }

        internal void MarkSaved(CharacterAnimationProfile source)
        {
            _source = source;
            _sourceSnapshot = AnimationEventToolSourceSnapshot.SerializeObject(source);
            _recoveryPath = AssetDatabase.GetAssetPath(source);
            _isDirty = false;
            _isSaveUncertain = false;
        }

        internal void MarkWriteAttempt(string path)
        {
            _recoveryPath = path;
        }

        internal void MarkUncertain()
        {
            _isSaveUncertain = true;
            _isDirty = true;
        }

        bool CanChangeList(out string error)
        {
            if (_isSaveUncertain)
            {
                error = "Reload the uncertain save result before editing.";
                return false;
            }

            error = null;
            return true;
        }

        bool CanEditFrame(int frame)
        {
            return !_isSaveUncertain && _selectedEntry != null && !NeedsReauthor && frame >= 0 && frame <= LastFrame;
        }

        bool TryNormalizeKey(string key, AnimationEventToolEntry except, out string normalized, out string error)
        {
            normalized = key == null ? string.Empty : key.Trim();

            if (normalized.Length == 0)
            {
                error = "Enter an animation key.";
                return false;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i] != except && string.Equals(_entries[i].Key, normalized, StringComparison.Ordinal))
                {
                    error = "This character already has an animation with that key: " + normalized;
                    return false;
                }
            }

            error = null;
            return true;
        }

        static bool IsValidKind(AnimationAttackKind kind)
        {
            return kind == AnimationAttackKind.Melee || kind == AnimationAttackKind.Projectile;
        }
    }
}
#endif
