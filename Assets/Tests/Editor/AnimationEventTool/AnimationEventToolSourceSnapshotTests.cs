#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Animations;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.AnimationTools.Tests
{
    public sealed class AnimationEventToolSourceSnapshotTests
    {
        AnimationEventToolProfileTestAssets _assets;
        AnimationClip _first;
        AnimationClip _second;
        CharacterAnimationProfile _profile;
        GameObject _prefab;
        AnimationEventToolSourceSnapshot _snapshot;

        [SetUp]
        public void SetUp()
        {
            _assets = new AnimationEventToolProfileTestAssets();
            _first = _assets.CreateClip("First");
            _second = _assets.CreateClip("Second");
            _profile = _assets.CreateProfile("Profile", AnimationEventToolProfileTestAssets.Entry("attack1", _first),
                AnimationEventToolProfileTestAssets.Entry("skill1", _second));
            _prefab = _assets.CreatePrefab("Character", _profile);
            Assert.That(AnimationEventToolSourceSnapshot.TryCapture(_prefab, _profile, out _snapshot, out string error), Is.True, error);
        }

        [TearDown]
        public void TearDown()
        {
            _assets.Dispose();
        }

        [Test]
        public void EmptyAndMissingProfilesCanCaptureRestPoseSources()
        {
            CharacterAnimationProfile empty = _assets.CreateProfile("Empty");
            GameObject emptyPrefab = _assets.CreatePrefab("EmptyCharacter", empty);
            GameObject missingPrefab = _assets.CreatePrefab("MissingCharacter", null);

            Assert.That(AnimationEventToolSourceSnapshot.TryCapture(emptyPrefab, empty, out AnimationEventToolSourceSnapshot emptySnapshot,
                out string error), Is.True, error);
            Assert.That(AnimationEventToolSourceSnapshot.TryCapture(missingPrefab, null, out AnimationEventToolSourceSnapshot missingSnapshot,
                out error), Is.True, error);

            Assert.That(emptySnapshot.Validate(out error), Is.True, error);
            Assert.That(missingSnapshot.Validate(out error), Is.True, error);
        }

        [Test]
        public void UnselectedClipMemoryChangeInvalidatesTheEntireProfileSnapshot()
        {
            Assert.That(AnimationEventToolDocument.TryOpen(_profile, out AnimationEventToolDocument document, out string error), Is.True, error);
            Assert.That(document.TrySelectEntry(document.Entries[0]), Is.True);
            _second.SetCurve("Animated", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 5f, 1f, 6f));

            Assert.That(_snapshot.Validate(out error), Is.False);
            Assert.That(error, Does.Contain("Second.anim"));
            Assert.That(_second.length, Is.EqualTo(1f));
            Assert.That(_second.frameRate, Is.EqualTo(30f));
        }

        [Test]
        public void AddingNewClipCannotRebaseAnExistingSourceChange()
        {
            AnimationClip third = _assets.CreateClip("Third");
            File.AppendAllText(AssetDatabase.GetAssetPath(_second), Environment.NewLine);

            Assert.That(_snapshot.TryWithClip(third, out AnimationEventToolSourceSnapshot extended, out string error), Is.False);

            Assert.That(extended, Is.Null);
            Assert.That(error, Does.Contain("Second.anim"));
            Assert.That(_snapshot.Validate(out _), Is.False);
        }

        [Test]
        public void AddingAnotherClipPreservesTheOldSnapshotAndTracksEveryDraftEntry()
        {
            AnimationClip third = _assets.CreateClip("Third");
            Assert.That(AnimationEventToolDocument.TryOpen(_profile, out AnimationEventToolDocument document, out string error), Is.True, error);
            Assert.That(document.TryAddEntry("skill2", third, out _, out error), Is.True, error);
            Assert.That(_snapshot.MatchesDocument(document), Is.False);

            Assert.That(_snapshot.TryWithClip(third, out AnimationEventToolSourceSnapshot extended, out error), Is.True, error);

            Assert.That(extended.MatchesDocument(document), Is.True);
            Assert.That(_snapshot.MatchesDocument(document), Is.False);
            third.SetCurve("Animated", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 5f, 1f, 6f));
            Assert.That(extended.Validate(out _), Is.False);
            Assert.That(_snapshot.Validate(out error), Is.True, error);
        }

        [Test]
        public void RemovingAnEntryDoesNotStopWatchingItsOriginalClipBeforeSave()
        {
            Assert.That(AnimationEventToolDocument.TryOpen(_profile, out AnimationEventToolDocument document, out string error), Is.True, error);
            Assert.That(document.RemoveEntry(document.Entries[1]), Is.True);
            Assert.That(_snapshot.MatchesDocument(document), Is.True);
            File.AppendAllText(AssetDatabase.GetAssetPath(_second), Environment.NewLine);

            Assert.That(_snapshot.Validate(out error), Is.False);

            Assert.That(error, Does.Contain("Second.anim"));
        }

        [Test]
        public void RemovedClipChangesDuringSaveCannotBeHiddenByRebasingTheTargetProfile()
        {
            SaveEntries(AnimationEventToolProfileTestAssets.Entry("attack1", _first));
            _second.SetCurve("Animated", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 5f, 1f, 6f));

            Assert.That(_snapshot.TryAfterSave(_profile, out AnimationEventToolSourceSnapshot next, out _), Is.False);

            Assert.That(next, Is.Null);
        }

        [Test]
        public void SuccessfulSaveRebasesOnlyTheProfileAndThenReleasesRemovedClipDependencies()
        {
            SaveEntries(AnimationEventToolProfileTestAssets.Entry("attack1", _first, 9));

            Assert.That(_snapshot.TryAfterSave(_profile, out AnimationEventToolSourceSnapshot next, out string error), Is.True, error);

            Assert.That(next.Validate(out error), Is.True, error);
            _second.SetCurve("Animated", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 5f, 1f, 6f));
            Assert.That(next.Validate(out error), Is.True, error);
            Assert.That(_snapshot.Validate(out _), Is.False);
        }

        [Test]
        public void SameFrameRateAndLengthReimportStillInvalidatesTheSnapshot()
        {
            _second.SetCurve("Animated", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 5f, 1f, 6f));
            EditorUtility.SetDirty(_second);
            AssetDatabase.SaveAssetIfDirty(_second);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_second), ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            Assert.That(_second.length, Is.EqualTo(1f));
            Assert.That(_second.frameRate, Is.EqualTo(30f));
            Assert.That(_snapshot.Validate(out _), Is.False);
        }

        [Test]
        public void ReplacingMetadataDuringSaveCannotBeAcceptedAsTheToolsOwnWrite()
        {
            SaveEntries(AnimationEventToolProfileTestAssets.Entry("attack1", _first, 9));
            File.AppendAllText(AssetDatabase.GetAssetPath(_profile) + ".meta", Environment.NewLine);

            Assert.That(_snapshot.TryAfterSave(_profile, out _, out _), Is.False);
        }

        void SaveEntries(params CharacterAnimationEntry[] entries)
        {
            typeof(CharacterAnimationProfile).GetField("_animations", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_profile, entries);
            EditorUtility.SetDirty(_profile);
            AssetDatabase.SaveAssetIfDirty(_profile);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_profile), ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }
    }
}
#endif
