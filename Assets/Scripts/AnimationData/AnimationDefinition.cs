using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaggedBanner.Animations
{
    [CreateAssetMenu(menuName = "RaggedBanner/Animation Definition", fileName = "AnimationDefinition")]
    public sealed class AnimationDefinition : ScriptableObject
    {
        [SerializeField]
        AnimationClip _clip;

        [SerializeField]
        float _authoredFrameRate;

        [SerializeField]
        float _authoredLength;

        [SerializeField]
        AnimationEventKey[] _events = Array.Empty<AnimationEventKey>();

        public AnimationClip Clip => _clip;
        public float AuthoredFrameRate => _authoredFrameRate;
        public float AuthoredLength => _authoredLength;
        public IReadOnlyList<AnimationEventKey> Events => _events;

        public bool Validate(out string error)
        {
            return ValidateData(_clip, _authoredFrameRate, _authoredLength, _events, out error);
        }

        public static bool ValidateData(AnimationClip clip, float authoredFrameRate, float authoredLength,
            IReadOnlyList<AnimationEventKey> events, out string error)
        {
            if (clip == null)
            {
                error = "An animation clip is required.";
                return false;
            }

            if (clip.legacy)
            {
                error = "Legacy animation clips are not supported.";
                return false;
            }

            if (!AnimationFrameUtility.TryGetLastFrame(clip.length, clip.frameRate, out int lastFrame))
            {
                error = "The clip must have a positive finite length and frame rate within the supported frame range.";
                return false;
            }

            if (!AnimationFrameUtility.TryGetLastFrame(authoredLength, authoredFrameRate, out _)
                || authoredFrameRate != clip.frameRate || authoredLength != clip.length)
            {
                error = "The authored frame rate and length must match the current animation clip.";
                return false;
            }

            if (events == null)
            {
                error = "The animation event list must not be null.";
                return false;
            }

            int previousFrame = -1;

            for (int i = 0; i < events.Count; i++)
            {
                AnimationEventKey key = events[i];

                if (key == null)
                {
                    error = $"Animation event {i + 1} is missing.";
                    return false;
                }

                if (key.Frame < 0 || key.Frame > lastFrame)
                {
                    error = $"Animation event {i + 1} is outside the clip frame range.";
                    return false;
                }

                if (key.EventKind != AnimationEventKind.AttackHit)
                {
                    error = $"Animation event {i + 1} has an invalid event kind.";
                    return false;
                }

                if (key.AttackKind != AnimationAttackKind.Melee && key.AttackKind != AnimationAttackKind.Projectile)
                {
                    error = $"Animation event {i + 1} has an invalid attack kind.";
                    return false;
                }

                if (key.Frame < previousFrame)
                {
                    error = "Animation events must be ordered by frame.";
                    return false;
                }

                previousFrame = key.Frame;
            }

            error = null;
            return true;
        }
    }
}
