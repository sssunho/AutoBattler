using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RaggedBanner.Animations.Tests
{
    public sealed class CharacterAnimationProfileTests
    {
        readonly List<Object> _objects = new List<Object>();
        AnimationClip _clip;

        [SetUp]
        public void SetUp()
        {
            _clip = new AnimationClip { frameRate = 30f };
            _clip.SetCurve(string.Empty, typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 1f, 1f, 2f));
            _objects.Add(_clip);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void EmptyProfileCanBeAuthoredButCannotDriveGameplay()
        {
            CharacterAnimationProfile profile = CreateProfile();

            Assert.That(profile.Validate(out string error), Is.True, error);
            Assert.That(profile.ValidateForPlayback(out error), Is.False);
            Assert.That(error, Does.Contain("idle"));
            Assert.That(profile.TryGetAnimation("idle", out CharacterAnimationEntry missing), Is.False);
            Assert.That(missing, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t\r\n")]
        public void BlankKeysAreRejected(string key)
        {
            CharacterAnimationProfile profile = CreateProfile(Entry(key));

            Assert.That(profile.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ConstructorTrimsKeysAndDuplicateNormalizedKeysAreRejected()
        {
            CharacterAnimationEntry first = Entry(" attack1 ");
            CharacterAnimationProfile profile = CreateProfile(first, Entry("attack1"));

            Assert.That(first.Key, Is.EqualTo("attack1"));
            Assert.That(profile.Validate(out string error), Is.False);
            Assert.That(error, Does.Contain("unique"));
        }

        [Test]
        public void InspectorWhitespaceIsRejectedWithoutSilentlyChangingTheSource()
        {
            CharacterAnimationProfile profile = CreateProfile(Entry("idle"));
            SerializedObject data = new SerializedObject(profile);
            data.FindProperty("_animations").GetArrayElementAtIndex(0).FindPropertyRelative("_key").stringValue = " idle ";
            data.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(profile.Validate(out _), Is.False);
            Assert.That(profile.Animations[0].Key, Is.EqualTo(" idle "));
        }

        [Test]
        public void KeysUseOrdinalCaseSensitiveLookupAndDoNotNormalizeQueries()
        {
            CharacterAnimationProfile profile = CreateProfile(Entry("idle"), Entry("Idle"));

            Assert.That(profile.Validate(out string error), Is.True, error);
            Assert.That(profile.TryGetAnimation("idle", out CharacterAnimationEntry lower), Is.True);
            Assert.That(profile.TryGetAnimation("Idle", out CharacterAnimationEntry upper), Is.True);
            Assert.That(lower, Is.Not.SameAs(upper));
            Assert.That(profile.TryGetAnimation("IDLE", out _), Is.False);
            Assert.That(profile.TryGetAnimation(" idle ", out _), Is.False);
            Assert.That(profile.TryGetAnimation(null, out _), Is.False);
        }

        [TestCase("Idle", "move", "attack1")]
        [TestCase("idle", "Move", "attack1")]
        [TestCase("idle", "move", "attack")]
        public void GameplayRequiresTheExactReservedKeys(string idle, string move, string attack)
        {
            CharacterAnimationProfile profile = CreateProfile(Entry(idle), Entry(move), Entry(attack));

            Assert.That(profile.Validate(out _), Is.True);
            Assert.That(profile.ValidateForPlayback(out _), Is.False);
        }

        [Test]
        public void AdditionalEntriesAndListOrderDoNotChangeRequiredKeyValidation()
        {
            CharacterAnimationProfile profile = CreateProfile(Entry("skill1"), Entry("attack1"), Entry("move"), Entry("idle"));

            Assert.That(profile.ValidateForPlayback(out string error), Is.True, error);
            Assert.That(profile.TryGetAnimation("skill1", out CharacterAnimationEntry skill), Is.True);
            Assert.That(skill.Key, Is.EqualTo("skill1"));
        }

        [Test]
        public void SameClipAssignmentsOwnIndependentDeepCopiedEventData()
        {
            AnimationEventKey original = new AnimationEventKey(5, AnimationEventKind.AttackHit, AnimationAttackKind.Melee);
            AnimationEventKey[] source = { original };
            CharacterAnimationEntry first = new CharacterAnimationEntry("attack1", _clip, 30f, 1f, source);
            CharacterAnimationEntry second = new CharacterAnimationEntry("skill1", _clip, 30f, 1f, first.Events);
            CharacterAnimationProfile profile = CreateProfile(first, second);
            source[0] = new AnimationEventKey(20, AnimationEventKind.AttackHit, AnimationAttackKind.Projectile);
            typeof(AnimationEventKey).GetField("_frame", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(original, 15);
            SerializedObject data = new SerializedObject(profile);
            data.FindProperty("_animations").GetArrayElementAtIndex(0).FindPropertyRelative("_events")
                .GetArrayElementAtIndex(0).FindPropertyRelative("_frame").intValue = 10;
            data.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(profile.Validate(out string error), Is.True, error);
            Assert.That(profile.Animations[0].Clip, Is.SameAs(profile.Animations[1].Clip));
            Assert.That(profile.Animations[0].Events[0].Frame, Is.EqualTo(10));
            Assert.That(profile.Animations[1].Events[0].Frame, Is.EqualTo(5));
            Assert.That(profile.Animations[1].Events[0].AttackKind, Is.EqualTo(AnimationAttackKind.Melee));
        }

        [Test]
        public void ProfileSerializationPreservesKeysClipsAuthoredMetadataAndEventKinds()
        {
            CharacterAnimationEntry attack = new CharacterAnimationEntry("attack1", _clip, 30f, 1f,
                new[] { new AnimationEventKey(12, AnimationEventKind.AttackHit, AnimationAttackKind.Projectile) });
            CharacterAnimationProfile profile = CreateProfile(Entry("idle"), attack);
            CharacterAnimationProfile restored = CreateProfile();

            EditorUtility.CopySerialized(profile, restored);

            Assert.That(restored.Validate(out string error), Is.True, error);
            Assert.That(restored.Animations.Count, Is.EqualTo(2));
            Assert.That(restored.TryGetAnimation("attack1", out CharacterAnimationEntry entry), Is.True);
            Assert.That(entry.Clip, Is.SameAs(_clip));
            Assert.That(entry.AuthoredFrameRate, Is.EqualTo(30f));
            Assert.That(entry.AuthoredLength, Is.EqualTo(1f));
            Assert.That(entry.Events.Count, Is.EqualTo(1));
            Assert.That(entry.Events[0].Frame, Is.EqualTo(12));
            Assert.That(entry.Events[0].EventKind, Is.EqualTo(AnimationEventKind.AttackHit));
            Assert.That(entry.Events[0].AttackKind, Is.EqualTo(AnimationAttackKind.Projectile));
        }

        [Test]
        public void MissingEntriesAndCollectionsFailValidationWithoutThrowing()
        {
            CharacterAnimationProfile profile = CreateProfile(new CharacterAnimationEntry[] { null });
            Assert.That(profile.Validate(out _), Is.False);
            typeof(CharacterAnimationProfile).GetField("_animations", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(profile, null);
            Assert.That(profile.Validate(out _), Is.False);
            Assert.That(profile.TryGetAnimation("idle", out _), Is.False);
            CharacterAnimationEntry entry = new CharacterAnimationEntry("idle", _clip, 30f, 1f, null);
            Assert.That(entry.Validate(out _), Is.False);
        }

        [Test]
        public void EntryPreservesAuthoredMetadataMismatchForExplicitReauthoring()
        {
            CharacterAnimationEntry entry = new CharacterAnimationEntry("idle", _clip, 60f, 2f, Array.Empty<AnimationEventKey>());

            Assert.That(entry.Validate(out _), Is.False);
            Assert.That(entry.AuthoredFrameRate, Is.EqualTo(60f));
            Assert.That(entry.AuthoredLength, Is.EqualTo(2f));
        }

        [TestCase(0)]
        [TestCase(2)]
        public void GameplayRejectsMissingOrMultipleHitsButToolValidationAllowsThem(int count)
        {
            AnimationEventKey[] keys = new AnimationEventKey[count];
            for (int i = 0; i < count; i++)
            {
                keys[i] = new AnimationEventKey(i, AnimationEventKind.AttackHit, AnimationAttackKind.Melee);
            }

            CharacterAnimationEntry attack = new CharacterAnimationEntry("attack1", _clip, _clip.frameRate, _clip.length, keys);
            CharacterAnimationProfile profile = CreateProfile(Entry("idle"), Entry("move"), attack);
            Assert.That(profile.Validate(out _), Is.True);
            Assert.That(profile.ValidateForPlayback(out _), Is.False);
        }

        CharacterAnimationEntry Entry(string key)
        {
            AnimationEventKey[] events = key == "attack1"
                ? new[] { new AnimationEventKey(15, AnimationEventKind.AttackHit, AnimationAttackKind.Melee) }
                : Array.Empty<AnimationEventKey>();
            return new CharacterAnimationEntry(key, _clip, _clip.frameRate, _clip.length, events);
        }

        CharacterAnimationProfile CreateProfile(params CharacterAnimationEntry[] entries)
        {
            CharacterAnimationProfile profile = ScriptableObject.CreateInstance<CharacterAnimationProfile>();
            _objects.Add(profile);
            typeof(CharacterAnimationProfile).GetField("_animations", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(profile, entries);
            return profile;
        }
    }
}
