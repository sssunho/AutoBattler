using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaggedBanner.Animations
{
    [Serializable]
    public sealed class CharacterAnimationEntry
    {
        [SerializeField]
        string _key;

        [SerializeField]
        AnimationClip _clip;

        [SerializeField]
        float _authoredFrameRate;

        [SerializeField]
        float _authoredLength;

        [SerializeField]
        AnimationEventKey[] _events;

        public string Key => _key;
        public AnimationClip Clip => _clip;
        public float AuthoredFrameRate => _authoredFrameRate;
        public float AuthoredLength => _authoredLength;
        public IReadOnlyList<AnimationEventKey> Events => _events;

        public CharacterAnimationEntry(string key, AnimationClip clip, float authoredFrameRate, float authoredLength,
            IReadOnlyList<AnimationEventKey> events)
        {
            _key = key == null ? string.Empty : key.Trim();
            _clip = clip;
            _authoredFrameRate = authoredFrameRate;
            _authoredLength = authoredLength;

            if (events != null)
            {
                _events = new AnimationEventKey[events.Count];

                for (int i = 0; i < events.Count; i++)
                {
                    AnimationEventKey source = events[i];
                    _events[i] = source == null ? null : new AnimationEventKey(source.Frame, source.EventKind, source.AttackKind);
                }
            }
        }

        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(_key) || !string.Equals(_key, _key.Trim(), StringComparison.Ordinal))
            {
                error = "An animation key must be nonempty and have no surrounding whitespace.";
                return false;
            }

            return AnimationDefinition.ValidateData(_clip, _authoredFrameRate, _authoredLength, _events, out error);
        }
    }
}
