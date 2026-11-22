using System;
using System.Collections.Generic;
using UnityEngine;

namespace RaggedBanner.Animations
{
    [CreateAssetMenu(menuName = "RaggedBanner/Character Animation Profile", fileName = "CharacterAnimationProfile")]
    public sealed class CharacterAnimationProfile : ScriptableObject
    {
        [SerializeField]
        CharacterAnimationEntry[] _animations = Array.Empty<CharacterAnimationEntry>();

        public IReadOnlyList<CharacterAnimationEntry> Animations => _animations;

        public bool TryGetAnimation(string key, out CharacterAnimationEntry animation)
        {
            if (_animations != null && !string.IsNullOrEmpty(key))
            {
                for (int i = 0; i < _animations.Length; i++)
                {
                    CharacterAnimationEntry entry = _animations[i];

                    if (entry != null && string.Equals(entry.Key, key, StringComparison.Ordinal))
                    {
                        animation = entry;
                        return true;
                    }
                }
            }

            animation = null;
            return false;
        }

        public bool Validate(out string error)
        {
            if (_animations == null)
            {
                error = "The character animation list must not be null.";
                return false;
            }

            for (int i = 0; i < _animations.Length; i++)
            {
                CharacterAnimationEntry entry = _animations[i];

                if (entry == null)
                {
                    error = "A character animation entry is missing.";
                    return false;
                }

                if (!entry.Validate(out error))
                {
                    error = entry.Key + ": " + error;
                    return false;
                }

                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(_animations[j].Key, entry.Key, StringComparison.Ordinal))
                    {
                        error = "Animation keys must be unique within a character: " + entry.Key;
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        public bool ValidateForPlayback(out string error)
        {
            if (!Validate(out error))
            {
                return false;
            }

            if (!TryGetAnimation("idle", out _) || !TryGetAnimation("move", out _)
                || !TryGetAnimation("attack1", out CharacterAnimationEntry attack))
            {
                error = "Gameplay requires animations with the exact keys idle, move, and attack1.";
                return false;
            }

            if (attack.Clip.isLooping)
            {
                error = "The attack1 animation clip must not loop.";
                return false;
            }

            if (attack.Events.Count != 1 || attack.Events[0].EventKind != AnimationEventKind.AttackHit)
            {
                error = "Gameplay attack1 requires exactly one AttackHit event.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
