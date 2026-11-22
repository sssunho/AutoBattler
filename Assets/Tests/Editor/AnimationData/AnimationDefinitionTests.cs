using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RaggedBanner.Animations.Tests
{
    public sealed class AnimationDefinitionTests
    {
        readonly List<Object> _objects = new List<Object>();
        AnimationClip _clip;

        [SetUp]
        public void SetUp()
        {
            _clip = CreateClip();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test]
        public void EmptyEventsAreValidAndDoNotRequireAnAnimatorOrController()
        {
            AnimationDefinition definition = CreateDefinition(_clip);

            Assert.That(definition.Validate(out string error), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(definition.Clip, Is.SameAs(_clip));
            Assert.That(definition.AuthoredFrameRate, Is.EqualTo(30f));
            Assert.That(definition.AuthoredLength, Is.EqualTo(1f));
            Assert.That(definition.Events, Is.Empty);
        }

        [Test]
        public void NewUnconfiguredDefinitionIsInvalid()
        {
            AnimationDefinition definition = ScriptableObject.CreateInstance<AnimationDefinition>();
            _objects.Add(definition);

            Assert.That(definition.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ValidationPreservesDuplicateFrameOrderAndInputObjects()
        {
            AnimationEventKey first = Key(0, AnimationAttackKind.Projectile);
            AnimationEventKey second = Key(0, AnimationAttackKind.Melee);
            AnimationEventKey last = Key(30, AnimationAttackKind.Melee);
            AnimationEventKey[] events = { first, second, last };

            Assert.That(Validate(events, out string error), Is.True);
            Assert.That(error, Is.Null);
            Assert.That(events[0], Is.SameAs(first));
            Assert.That(events[1], Is.SameAs(second));
            Assert.That(events[2], Is.SameAs(last));
            Assert.That(_clip.frameRate, Is.EqualTo(30f));
            Assert.That(_clip.length, Is.EqualTo(1f));
        }

        [Test]
        public void SerializedCopyRetainsClipMetadataAndSameFrameEventOrder()
        {
            AnimationDefinition definition = CreateDefinition(_clip,
                Key(18, AnimationAttackKind.Projectile), Key(18, AnimationAttackKind.Melee));
            AnimationDefinition restored = ScriptableObject.CreateInstance<AnimationDefinition>();
            _objects.Add(restored);

            EditorUtility.CopySerialized(definition, restored);

            Assert.That(restored.Validate(out string error), Is.True, error);
            Assert.That(restored.Clip, Is.SameAs(_clip));
            Assert.That(restored.AuthoredFrameRate, Is.EqualTo(definition.AuthoredFrameRate));
            Assert.That(restored.AuthoredLength, Is.EqualTo(definition.AuthoredLength));
            Assert.That(restored.Events.Count, Is.EqualTo(2));
            Assert.That(restored.Events[0].Frame, Is.EqualTo(18));
            Assert.That(restored.Events[0].EventKind, Is.EqualTo(AnimationEventKind.AttackHit));
            Assert.That(restored.Events[0].AttackKind, Is.EqualTo(AnimationAttackKind.Projectile));
            Assert.That(restored.Events[1].Frame, Is.EqualTo(18));
            Assert.That(restored.Events[1].AttackKind, Is.EqualTo(AnimationAttackKind.Melee));
        }

        [Test]
        public void MissingAndLegacyClipsAreRejected()
        {
            Assert.That(AnimationDefinition.ValidateData(null, 30f, 1f,
                Array.Empty<AnimationEventKey>(), out _), Is.False);

            _clip.legacy = true;

            Assert.That(Validate(Array.Empty<AnimationEventKey>(), out _), Is.False);
        }

        [Test]
        public void ZeroLengthClipsAreRejected()
        {
            AnimationClip empty = new AnimationClip();
            _objects.Add(empty);
            empty.SetCurve(string.Empty, typeof(Transform), "localScale.x", new AnimationCurve(new Keyframe(0f, 1f)));

            Assert.That(empty.length, Is.Zero);
            Assert.That(AnimationDefinition.ValidateData(empty, empty.frameRate, empty.length,
                Array.Empty<AnimationEventKey>(), out _), Is.False);
        }

        [TestCase(0f, 1f)]
        [TestCase(-30f, 1f)]
        [TestCase(float.NaN, 1f)]
        [TestCase(float.PositiveInfinity, 1f)]
        [TestCase(30f, 0f)]
        [TestCase(30f, -1f)]
        [TestCase(30f, float.NaN)]
        [TestCase(30f, float.PositiveInfinity)]
        public void InvalidAuthoredTimelineIsRejected(float frameRate, float length)
        {
            Assert.That(AnimationDefinition.ValidateData(_clip, frameRate, length,
                Array.Empty<AnimationEventKey>(), out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void SourceFrameRateChangesDoNotRetimeOrRewriteAuthoredData()
        {
            AnimationDefinition definition = CreateDefinition(_clip, Key(18, AnimationAttackKind.Melee));
            _clip.frameRate = 60f;

            Assert.That(definition.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(definition.AuthoredFrameRate, Is.EqualTo(30f));
            Assert.That(definition.Events[0].Frame, Is.EqualTo(18));
        }

        [Test]
        public void SourceLengthChangesDoNotRewriteAuthoredData()
        {
            AnimationDefinition definition = CreateDefinition(_clip, Key(18, AnimationAttackKind.Melee));
            _clip.SetCurve(string.Empty, typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 1f, 2f, 1f));

            Assert.That(definition.Validate(out _), Is.False);
            Assert.That(definition.AuthoredLength, Is.EqualTo(1f));
            Assert.That(definition.Events[0].Frame, Is.EqualTo(18));
        }

        [Test]
        public void NullEventListsAndEntriesAreRejected()
        {
            Assert.That(Validate(null, out _), Is.False);
            Assert.That(Validate(new AnimationEventKey[] { null }, out _), Is.False);
        }

        [TestCase(-1)]
        [TestCase(31)]
        public void EventFramesOutsideTheClipAreRejected(int frame)
        {
            Assert.That(Validate(new[] { Key(frame, AnimationAttackKind.Melee) }, out _), Is.False);
        }

        [Test]
        public void UnsortedEventsAreRejectedWithoutSortingTheInput()
        {
            AnimationEventKey first = Key(18, AnimationAttackKind.Melee);
            AnimationEventKey second = Key(12, AnimationAttackKind.Projectile);
            AnimationEventKey[] events = { first, second };

            Assert.That(Validate(events, out _), Is.False);
            Assert.That(events[0], Is.SameAs(first));
            Assert.That(events[1], Is.SameAs(second));
        }

        [TestCase(-1, 0)]
        [TestCase(1, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 2)]
        public void UndefinedEventAndAttackKindsAreRejected(int eventKind, int attackKind)
        {
            AnimationEventKey entry = new AnimationEventKey(18,
                (AnimationEventKind)eventKind, (AnimationAttackKind)attackKind);

            Assert.That(Validate(new[] { entry }, out _), Is.False);
        }

        [Test]
        public void DefinitionsSharingAClipKeepIndependentEvents()
        {
            AnimationDefinition melee = CreateDefinition(_clip, Key(18, AnimationAttackKind.Melee));
            AnimationDefinition projectile = CreateDefinition(_clip, Key(12, AnimationAttackKind.Projectile));

            SerializedObject edited = new SerializedObject(melee);
            edited.FindProperty("_events").GetArrayElementAtIndex(0).FindPropertyRelative("_frame").intValue = 20;
            edited.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(melee.Validate(out _), Is.True);
            Assert.That(projectile.Validate(out _), Is.True);
            Assert.That(melee.Events[0].Frame, Is.EqualTo(20));
            Assert.That(projectile.Events[0].Frame, Is.EqualTo(12));
            Assert.That(projectile.Events[0].AttackKind, Is.EqualTo(AnimationAttackKind.Projectile));
        }

        [Test]
        public void ProfilesCopyDefinitionDataIntoIndependentEntries()
        {
            AnimationDefinition definition = CreateDefinition(_clip, Key(18, AnimationAttackKind.Melee));
            CharacterAnimationProfile first = CreateProfile(definition, definition, definition);
            CharacterAnimationProfile second = CreateProfile(definition, definition, definition);

            Assert.That(first.Validate(out _), Is.True);
            Assert.That(second.Validate(out _), Is.True);
            Assert.That(first.TryGetAnimation("idle", out CharacterAnimationEntry idle), Is.True);
            Assert.That(first.TryGetAnimation("move", out CharacterAnimationEntry move), Is.True);
            Assert.That(second.TryGetAnimation("idle", out CharacterAnimationEntry otherIdle), Is.True);
            Assert.That(idle.Clip, Is.SameAs(definition.Clip));
            Assert.That(idle.Events[0], Is.Not.SameAs(move.Events[0]));
            Assert.That(idle.Events[0], Is.Not.SameAs(otherIdle.Events[0]));
            Assert.That(idle.Events[0], Is.Not.SameAs(definition.Events[0]));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EveryProfileRoleIsRequired(int missingRole)
        {
            AnimationDefinition definition = CreateDefinition(_clip);
            CharacterAnimationProfile profile = CreateProfile(missingRole == 0 ? null : definition,
                missingRole == 1 ? null : definition, missingRole == 2 ? null : definition);

            Assert.That(profile.Validate(out _), Is.True);
            Assert.That(profile.ValidateForPlayback(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EveryProfileRoleMustReferenceValidData(int invalidRole)
        {
            AnimationDefinition valid = CreateDefinition(_clip);
            AnimationDefinition invalid = CreateDefinition(_clip, Key(31, AnimationAttackKind.Melee));
            CharacterAnimationProfile profile = CreateProfile(invalidRole == 0 ? invalid : valid,
                invalidRole == 1 ? invalid : valid, invalidRole == 2 ? invalid : valid);

            Assert.That(profile.Validate(out _), Is.False);
        }

        [Test]
        public void LoopingClipsAreAllowedForIdleAndMoveButNotAttack()
        {
            AnimationClip loop = CreateClip();
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(loop);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(loop, settings);
            AnimationDefinition looping = CreateDefinition(loop);
            AnimationDefinition attack = CreateDefinition(_clip, Key(15, AnimationAttackKind.Melee));

            Assert.That(loop.isLooping, Is.True);
            Assert.That(looping.Validate(out _), Is.True);
            Assert.That(CreateProfile(looping, looping, attack).ValidateForPlayback(out _), Is.True);
            Assert.That(CreateProfile(attack, attack, looping).Validate(out _), Is.True);
            Assert.That(CreateProfile(attack, attack, looping).ValidateForPlayback(out _), Is.False);
        }

        bool Validate(IReadOnlyList<AnimationEventKey> events, out string error)
        {
            return AnimationDefinition.ValidateData(_clip, 30f, 1f, events, out error);
        }

        AnimationClip CreateClip()
        {
            AnimationClip clip = new AnimationClip { frameRate = 30f };
            _objects.Add(clip);
            clip.SetCurve(string.Empty, typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 1f, 1f, 1f));
            return clip;
        }

        AnimationDefinition CreateDefinition(AnimationClip clip, params AnimationEventKey[] events)
        {
            AnimationDefinition definition = ScriptableObject.CreateInstance<AnimationDefinition>();
            _objects.Add(definition);
            SerializedObject data = new SerializedObject(definition);
            data.FindProperty("_clip").objectReferenceValue = clip;
            data.FindProperty("_authoredFrameRate").floatValue = clip.frameRate;
            data.FindProperty("_authoredLength").floatValue = clip.length;
            SerializedProperty entries = data.FindProperty("_events");
            entries.arraySize = events.Length;

            for (int i = 0; i < events.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_frame").intValue = events[i].Frame;
                entry.FindPropertyRelative("_eventKind").intValue = (int)events[i].EventKind;
                entry.FindPropertyRelative("_attackKind").intValue = (int)events[i].AttackKind;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        CharacterAnimationProfile CreateProfile(AnimationDefinition idle, AnimationDefinition move, AnimationDefinition attack)
        {
            CharacterAnimationProfile profile = ScriptableObject.CreateInstance<CharacterAnimationProfile>();
            _objects.Add(profile);
            AnimationDefinition[] sources = { idle, move, attack };
            string[] keys = { "idle", "move", "attack1" };
            List<CharacterAnimationEntry> entries = new List<CharacterAnimationEntry>();

            for (int i = 0; i < sources.Length; i++)
            {
                AnimationDefinition source = sources[i];

                if (source != null)
                {
                    entries.Add(new CharacterAnimationEntry(keys[i], source.Clip, source.AuthoredFrameRate,
                        source.AuthoredLength, source.Events));
                }
            }

            typeof(CharacterAnimationProfile).GetField("_animations", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(profile, entries.ToArray());
            return profile;
        }

        static AnimationEventKey Key(int frame, AnimationAttackKind attackKind)
        {
            return new AnimationEventKey(frame, AnimationEventKind.AttackHit, attackKind);
        }
    }
}
