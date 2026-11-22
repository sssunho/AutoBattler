using System;
using System.Collections.Generic;
using System.Reflection;
using RaggedBanner.Animations;
using RaggedBanner.Combat.Unity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RaggedBanner.Combat.Tests
{
    sealed class BattleAnimationFixture : IDisposable
    {
        readonly List<Object> _assets = new List<Object>();
        readonly AnimationAttackKind _attackKind;

        public Animator Animator { get; }
        public CharacterAnimationProfile Profile { get; }
        public Transform Pose { get; }

        public BattleAnimationFixture(GameObject model, bool withAttack = true, AnimationAttackKind attackKind = AnimationAttackKind.Melee)
        {
            _attackKind = attackKind;
            Animator = model.AddComponent<Animator>();
            Pose = new GameObject("Animation Pose").transform;
            Pose.SetParent(model.transform, false);
            Profile = ScriptableObject.CreateInstance<CharacterAnimationProfile>();
            Profile.hideFlags = HideFlags.HideAndDontSave;
            _assets.Add(Profile);
            List<CharacterAnimationEntry> entries = new List<CharacterAnimationEntry>
            {
                CreateEntry("idle", 1f),
                CreateEntry("move", 3f)
            };

            if (withAttack)
            {
                entries.Add(CreateEntry("attack1", 5f));
            }

            SetField(Profile, "_animations", entries.ToArray());
        }

        public void Attach(HexCharacterView view)
        {
            SetField(view, "_model", Animator.transform);
            SetField(view, "_animator", Animator);
            SetField(view, "_animationProfile", Profile);
        }

        public void Dispose()
        {
            for (int i = _assets.Count - 1; i >= 0; i--)
            {
                if (_assets[i] != null)
                {
                    Object.DestroyImmediate(_assets[i]);
                }
            }

            _assets.Clear();
        }

        CharacterAnimationEntry CreateEntry(string role, float from)
        {
            AnimationClip clip = new AnimationClip
            {
                name = "Test " + role,
                frameRate = 30f,
                hideFlags = HideFlags.HideAndDontSave
            };
            clip.SetCurve("Animation Pose", typeof(Transform), "m_LocalScale.x",
                AnimationCurve.Linear(0f, from, 1f, from + 1f));
            _assets.Add(clip);
            AnimationEventKey[] events = role == "attack1"
                ? new[] { new AnimationEventKey(15, AnimationEventKind.AttackHit, _attackKind) }
                : Array.Empty<AnimationEventKey>();
            return new CharacterAnimationEntry(role, clip, clip.frameRate, clip.length, events);
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
