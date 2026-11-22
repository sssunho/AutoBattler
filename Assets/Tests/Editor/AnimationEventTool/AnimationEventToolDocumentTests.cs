#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RaggedBanner.AnimationTools.Tests
{
    public sealed class AnimationEventToolDocumentTests
    {
        readonly List<Object> _objects = new List<Object>();

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
        public void EditingWorksOnIndependentKeysAndPreservesSelectionIdentityThroughSorting()
        {
            CharacterAnimationProfile profile = CreateProfile(CreateEntry("attack1", CreateClip(30f, 1f),
                new AnimationEventKey(4, AnimationEventKind.AttackHit, AnimationAttackKind.Melee)));
            AnimationEventToolDocument document = Open(profile);
            AnimationEventToolKey selected = document.Keys[0];
            AnimationEventToolKey added = document.Add(2);

            Assert.That(document.TryEdit(selected, 1, AnimationAttackKind.Projectile, out string error), Is.True, error);

            Assert.That(document.Keys[0], Is.SameAs(selected));
            Assert.That(document.Keys[1], Is.SameAs(added));
            Assert.That(document.IsDirty, Is.True);
            Assert.That(profile.Animations[0].Events.Count, Is.EqualTo(1));
            Assert.That(profile.Animations[0].Events[0].Frame, Is.EqualTo(4));
            Assert.That(profile.Animations[0].Events[0].AttackKind, Is.EqualTo(AnimationAttackKind.Melee));
            Assert.That(document.Remove(selected), Is.True);
            Assert.That(document.Keys.Count, Is.EqualTo(1));
            Assert.That(document.Keys[0], Is.SameAs(added));
        }

        [Test]
        public void SameFrameKeysKeepOriginalOrderAndAppendNewKeys()
        {
            AnimationEventToolDocument document = Open(CreateProfile(CreateEntry("attack1", CreateClip(30f, 1f),
                new AnimationEventKey(5, AnimationEventKind.AttackHit, AnimationAttackKind.Melee),
                new AnimationEventKey(5, AnimationEventKind.AttackHit, AnimationAttackKind.Projectile))));
            AnimationEventToolKey first = document.Keys[0];
            AnimationEventToolKey second = document.Keys[1];
            AnimationEventToolKey third = document.Add(5);
            Assert.That(document.TryEdit(second, 5, AnimationAttackKind.Melee, out string error), Is.True, error);

            Assert.That(document.Keys[0], Is.SameAs(first));
            Assert.That(document.Keys[1], Is.SameAs(second));
            Assert.That(document.Keys[2], Is.SameAs(third));
            AnimationEventKey[] exported = document.ExportKeys();
            Assert.That(exported.Length, Is.EqualTo(3));
            Assert.That(exported[0].Frame, Is.EqualTo(5));
            Assert.That(exported[2].EventKind, Is.EqualTo(AnimationEventKind.AttackHit));
        }

        [TestCase(-1)]
        [TestCase(31)]
        public void InvalidFrameEditsLeaveTheWorkingCopyUntouched(int frame)
        {
            AnimationEventToolDocument document = Open(CreateProfile(CreateEntry("attack1", CreateClip(30f, 1f),
                new AnimationEventKey(0, AnimationEventKind.AttackHit, AnimationAttackKind.Melee))));

            Assert.That(document.Add(frame), Is.Null);
            Assert.That(document.TryEdit(document.Keys[0], frame, AnimationAttackKind.Melee, out _), Is.False);

            Assert.That(document.Keys[0].Frame, Is.Zero);
            Assert.That(document.IsDirty, Is.False);
        }

        [Test]
        public void InvalidAttackKindAndForeignKeyAreRejected()
        {
            AnimationEventToolDocument document = Open(CreateProfile(CreateEntry("attack1", CreateClip(30f, 1f))));
            AnimationEventToolKey key = document.Add(0);

            Assert.That(document.TryEdit(key, 0, (AnimationAttackKind)99, out _), Is.False);
            Assert.That(document.TryEdit(new AnimationEventToolKey(0, AnimationAttackKind.Melee, 0), 0,
                AnimationAttackKind.Projectile, out _), Is.False);
            Assert.That(document.Remove(null), Is.False);
            Assert.That(key.AttackKind, Is.EqualTo(AnimationAttackKind.Melee));
        }

        [Test]
        public void MetadataMismatchPreservesKeysAndRequiresExplicitReauthoring()
        {
            AnimationClip clip = CreateClip(30f, 1f);
            CharacterAnimationProfile profile = CreateProfile(CreateEntry("attack1", clip,
                new AnimationEventKey(15, AnimationEventKind.AttackHit, AnimationAttackKind.Projectile)));
            clip.frameRate = 60f;
            AnimationEventToolDocument document = Open(profile);

            Assert.That(document.NeedsReauthor, Is.True);
            Assert.That(document.IsDirty, Is.False);
            Assert.That(document.Keys.Count, Is.EqualTo(1));
            Assert.That(document.Add(0), Is.Null);
            Assert.That(document.Remove(document.Keys[0]), Is.False);
            Assert.That(document.TryReauthor(clip, out string error), Is.True, error);
            Assert.That(document.NeedsReauthor, Is.False);
            Assert.That(document.FrameRate, Is.EqualTo(60f));
            Assert.That(document.Keys, Is.Empty);
            Assert.That(document.IsDirty, Is.True);
            Assert.That(profile.Animations[0].Events.Count, Is.EqualTo(1));
            Assert.That(profile.Animations[0].AuthoredFrameRate, Is.EqualTo(30f));
        }

        [Test]
        public void ClipReplacementClearsOnlySelectedWorkingKeys()
        {
            AnimationClip original = CreateClip(30f, 1f);
            CharacterAnimationProfile profile = CreateProfile(CreateEntry("attack1", original),
                CreateEntry("skill1", original, new AnimationEventKey(7, AnimationEventKind.AttackHit, AnimationAttackKind.Melee)));
            AnimationEventToolDocument document = Open(profile);
            document.Add(10);
            AnimationClip replacement = CreateClip(29.97f, 2f);

            Assert.That(document.TryReauthor(replacement, out string error), Is.True, error);

            Assert.That(document.Clip, Is.SameAs(replacement));
            Assert.That(document.Keys, Is.Empty);
            Assert.That(profile.Animations[0].Clip, Is.SameAs(original));
            Assert.That(document.LastFrame, Is.EqualTo(59));
            Assert.That(document.Entries[1].Clip, Is.SameAs(original));
            Assert.That(document.Entries[1].Keys[0].Frame, Is.EqualTo(7));
        }

        [Test]
        public void AClipShorterThanOneFrameAllowsOnlyFrameZero()
        {
            AnimationEventToolDocument document = Open(CreateProfile(CreateEntry("attack1", CreateClip(30f, 0.01f))));

            Assert.That(document.LastFrame, Is.Zero);
            Assert.That(document.Add(0), Is.Not.Null);
            Assert.That(document.Add(1), Is.Null);
        }

        [Test]
        public void UncertainSaveBlocksEventAndListEdits()
        {
            AnimationEventToolDocument document = Open(CreateProfile(CreateEntry("attack1", CreateClip(30f, 1f))));
            AnimationEventToolEntry entry = document.SelectedEntry;
            AnimationEventToolKey key = document.Add(0);
            document.MarkUncertain();

            Assert.That(document.IsSaveUncertain, Is.True);
            Assert.That(document.Add(1), Is.Null);
            Assert.That(document.Remove(key), Is.False);
            Assert.That(document.TryEdit(key, 1, AnimationAttackKind.Projectile, out _), Is.False);
            Assert.That(document.TryReauthor(document.Clip, out _), Is.False);
            Assert.That(document.TryAddEntry("skill1", document.Clip, out _, out _), Is.False);
            Assert.That(document.TryRenameEntry(entry, "other", out _), Is.False);
            Assert.That(document.RemoveEntry(entry), Is.False);
            Assert.That(document.TrySelectEntry(null), Is.False);
            Assert.That(document.TryExport(out _, out _), Is.False);
            Assert.That(document.Keys[0], Is.SameAs(key));
        }

        [Test]
        public void InvalidStoredEventKindIsRejectedWithoutModifyingTheSource()
        {
            CharacterAnimationProfile profile = CreateProfile(CreateEntry("attack1", CreateClip(30f, 1f),
                new AnimationEventKey(0, (AnimationEventKind)99, AnimationAttackKind.Melee)));

            Assert.That(AnimationEventToolDocument.TryOpen(profile, out _, out string error), Is.False);

            Assert.That(error, Is.Not.Empty);
            Assert.That((int)profile.Animations[0].Events[0].EventKind, Is.EqualTo(99));
        }

        [Test]
        public void EmptyProfileHasNoSelectionAndCanAddThenRemoveItsLastEntry()
        {
            CharacterAnimationProfile profile = CreateProfile();
            AnimationEventToolDocument document = Open(profile);

            Assert.That(document.SelectedEntry, Is.Null);
            Assert.That(document.Clip, Is.Null);
            Assert.That(document.Keys, Is.Empty);
            Assert.That(document.NeedsReauthor, Is.False);
            Assert.That(document.IsDirty, Is.False);
            Assert.That(document.Add(0), Is.Null);
            Assert.That(document.TryReauthor(CreateClip(30f, 1f), out _), Is.False);
            Assert.That(document.TryExport(out CharacterAnimationEntry[] empty, out _), Is.True);
            Assert.That(empty, Is.Empty);
            Assert.That(document.TryAddEntry(" idle ", CreateClip(30f, 1f), out AnimationEventToolEntry entry, out string error), Is.True, error);
            Assert.That(entry.Key, Is.EqualTo("idle"));
            Assert.That(document.SelectedEntry, Is.SameAs(entry));
            Assert.That(profile.Animations, Is.Empty);
            Assert.That(document.RemoveEntry(entry), Is.True);
            Assert.That(document.SelectedEntry, Is.Null);
            Assert.That(document.TryExport(out empty, out error), Is.True, error);
            Assert.That(empty, Is.Empty);
        }

        [Test]
        public void SameClipEntriesRetainIndependentEditsAcrossSelectionChanges()
        {
            AnimationClip clip = CreateClip(30f, 1f);
            CharacterAnimationProfile profile = CreateProfile(CreateEntry("attack1", clip));
            AnimationEventToolDocument document = Open(profile);
            AnimationEventToolEntry original = document.SelectedEntry;
            AnimationEventToolKey originalKey = document.Add(4);
            Assert.That(document.TryAddEntry("skill1", clip, out AnimationEventToolEntry added, out string error), Is.True, error);
            AnimationEventToolKey addedKey = document.Add(9);
            Assert.That(document.TryEdit(addedKey, 9, AnimationAttackKind.Projectile, out error), Is.True, error);

            Assert.That(document.TrySelectEntry(original), Is.True);

            Assert.That(document.Keys[0], Is.SameAs(originalKey));
            Assert.That(document.Keys[0].Frame, Is.EqualTo(4));
            Assert.That(document.TryRenameEntry(original, " attack2 ", out error), Is.True, error);
            Assert.That(document.SelectedEntry, Is.SameAs(original));
            Assert.That(document.TrySelectEntry(added), Is.True);
            Assert.That(document.Keys[0], Is.SameAs(addedKey));
            Assert.That(document.TryExport(out CharacterAnimationEntry[] exported, out error), Is.True, error);
            Assert.That(exported[0].Key, Is.EqualTo("attack2"));
            Assert.That(exported[0].Clip, Is.SameAs(exported[1].Clip));
            Assert.That(exported[0].Events[0].Frame, Is.EqualTo(4));
            Assert.That(exported[1].Events[0].Frame, Is.EqualTo(9));
            Assert.That(exported[0].Events[0], Is.Not.SameAs(exported[1].Events[0]));
            Assert.That(profile.Animations[0].Key, Is.EqualTo("attack1"));
            Assert.That(profile.Animations[0].Events, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t ")]
        [TestCase("attack1")]
        [TestCase(" attack1 ")]
        public void EmptyAndDuplicateKeysAreRejectedWithoutChangingTheList(string key)
        {
            AnimationClip clip = CreateClip(30f, 1f);
            AnimationEventToolDocument document = Open(CreateProfile(CreateEntry("attack1", clip), CreateEntry("skill1", clip)));

            Assert.That(document.TryAddEntry(key, clip, out _, out _), Is.False);
            Assert.That(document.TryRenameEntry(document.Entries[1], key, out _), Is.False);

            Assert.That(document.Entries.Count, Is.EqualTo(2));
            Assert.That(document.Entries[1].Key, Is.EqualTo("skill1"));
            Assert.That(document.IsDirty, Is.False);
        }

        [Test]
        public void KeysAreOrdinalCaseSensitiveAndNoOpRenameDoesNotDirtyTheDraft()
        {
            AnimationClip clip = CreateClip(30f, 1f);
            AnimationEventToolDocument document = Open(CreateProfile(CreateEntry("idle", clip)));

            Assert.That(document.TryRenameEntry(document.SelectedEntry, " idle ", out _), Is.True);
            Assert.That(document.IsDirty, Is.False);
            Assert.That(document.TryAddEntry("Idle", clip, out _, out _), Is.True);
            Assert.That(document.Entries.Count, Is.EqualTo(2));
        }

        [Test]
        public void UnselectedInvalidClipPreventsWholeProfileExport()
        {
            AnimationClip first = CreateClip(30f, 1f);
            AnimationClip second = CreateClip(30f, 1f);
            AnimationEventToolDocument document = Open(CreateProfile(CreateEntry("attack1", first), CreateEntry("skill1", second)));
            second.frameRate = 60f;

            Assert.That(document.NeedsReauthor, Is.False);
            Assert.That(document.TryExport(out _, out string error), Is.False);
            Assert.That(error, Does.StartWith("skill1:"));
            Assert.That(document.Entries.Count, Is.EqualTo(2));
        }

        [Test]
        public void ForeignEntriesCannotBeSelectedRenamedOrRemoved()
        {
            AnimationClip clip = CreateClip(30f, 1f);
            AnimationEventToolDocument first = Open(CreateProfile(CreateEntry("idle", clip)));
            AnimationEventToolDocument second = Open(CreateProfile(CreateEntry("idle", clip)));

            Assert.That(first.TrySelectEntry(second.SelectedEntry), Is.False);
            Assert.That(first.TryRenameEntry(second.SelectedEntry, "other", out _), Is.False);
            Assert.That(first.RemoveEntry(second.SelectedEntry), Is.False);
            Assert.That(first.IsDirty, Is.False);
        }

        [Test]
        public void MarkSavedPreservesEntryAndEventIdentity()
        {
            CharacterAnimationProfile profile = CreateProfile(CreateEntry("idle", CreateClip(30f, 1f)));
            AnimationEventToolDocument document = Open(profile);
            AnimationEventToolEntry entry = document.SelectedEntry;
            AnimationEventToolKey key = document.Add(4);

            document.MarkSaved(profile);

            Assert.That(document.SelectedEntry, Is.SameAs(entry));
            Assert.That(document.Keys[0], Is.SameAs(key));
            Assert.That(document.IsDirty, Is.False);
        }

        static AnimationEventToolDocument Open(CharacterAnimationProfile profile)
        {
            Assert.That(AnimationEventToolDocument.TryOpen(profile, out AnimationEventToolDocument document, out string error), Is.True, error);
            return document;
        }

        AnimationClip CreateClip(float fps, float length)
        {
            AnimationClip clip = new AnimationClip { frameRate = fps, hideFlags = HideFlags.HideAndDontSave };
            clip.SetCurve("", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 1f, length, 2f));
            _objects.Add(clip);
            return clip;
        }

        CharacterAnimationProfile CreateProfile(params CharacterAnimationEntry[] entries)
        {
            CharacterAnimationProfile profile = ScriptableObject.CreateInstance<CharacterAnimationProfile>();
            _objects.Add(profile);
            typeof(CharacterAnimationProfile).GetField("_animations", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(profile, entries);
            return profile;
        }

        static CharacterAnimationEntry CreateEntry(string key, AnimationClip clip, params AnimationEventKey[] events)
        {
            return new CharacterAnimationEntry(key, clip, clip.frameRate, clip.length, events);
        }
    }
}
#endif
