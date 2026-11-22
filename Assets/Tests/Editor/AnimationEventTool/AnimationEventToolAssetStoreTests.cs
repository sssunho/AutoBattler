#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Animations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RaggedBanner.AnimationTools.Tests
{
    public sealed class AnimationEventToolAssetStoreTests
    {
        string _folder;
        AnimationClip _clip;
        AnimationClip _otherClip;
        CharacterAnimationProfile _profile;
        GameObject _prefab;
        AnimationEventToolDocument _document;
        AnimationEventToolSourceSnapshot _snapshot;

        [SetUp]
        public void SetUp()
        {
            _folder = "Assets/__AnimationEventToolTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
            _clip = CreateClip("Clip", 30f);
            _otherClip = CreateClip("OtherClip", 30f);
            _profile = ScriptableObject.CreateInstance<CharacterAnimationProfile>();
            SetEntries(_profile, CreateEntry("attack1", _clip), CreateEntry("skill1", _otherClip));
            AssetDatabase.CreateAsset(_profile, _folder + "/Profile.asset");
            GameObject temporary = new GameObject("Store Test Prefab");
            temporary.SetActive(false);

            try
            {
                temporary.AddComponent<Animator>();
                Type viewType = Type.GetType("RaggedBanner.Combat.Unity.HexCharacterView, RaggedBanner.CombatView");
                Assert.That(viewType, Is.Not.Null);
                Component view = temporary.AddComponent(viewType);
                SerializedObject viewData = new SerializedObject(view);
                viewData.FindProperty("_animationProfile").objectReferenceValue = _profile;
                viewData.ApplyModifiedPropertiesWithoutUndo();
                _prefab = PrefabUtility.SaveAsPrefabAsset(temporary, _folder + "/Preview.prefab");
            }
            finally
            {
                Object.DestroyImmediate(temporary);
            }

            OpenSession();
        }

        [TearDown]
        public void TearDown()
        {
            AnimationEventToolAssetStore.SaveTestHook = null;

            if (!string.IsNullOrEmpty(_folder) && _folder.StartsWith("Assets/__AnimationEventToolTests_", StringComparison.Ordinal)
                && AssetDatabase.IsValidFolder(_folder))
            {
                AssetDatabase.DeleteAsset(_folder);
            }
        }

        [Test]
        public void SavingProfileReloadsAllEntriesAndPreservesDraftSelectionIdentity()
        {
            AnimationEventToolKey first = _document.Add(5);
            AnimationEventToolKey second = _document.Add(5);
            AnimationEventToolEntry selected = _document.SelectedEntry;
            Assert.That(_document.TryEdit(second, 5, AnimationAttackKind.Projectile, out _), Is.True);
            Assert.That(_profile.Animations[0].Events, Is.Empty);

            AnimationEventSaveResult result = Save(out AnimationEventToolSourceSnapshot next, out string error);

            Assert.That(result, Is.EqualTo(AnimationEventSaveResult.Saved), error);
            Assert.That(next.Validate(out error), Is.True, error);
            Assert.That(_document.IsDirty, Is.False);
            Assert.That(_document.SelectedEntry, Is.SameAs(selected));
            Assert.That(_document.Keys[0], Is.SameAs(first));
            Assert.That(_document.Keys[1], Is.SameAs(second));
            Assert.That(_document.Source.Animations[0].Events.Count, Is.EqualTo(2));
            Assert.That(_document.Source.Animations[0].Events[1].AttackKind, Is.EqualTo(AnimationAttackKind.Projectile));
            Assert.That(_document.Source.Animations[1].Events, Is.Empty);
            Assert.That(_document.Source, Is.SameAs(_profile));
            Assert.That(AnimationEventToolAssetStore.TryReload(_document, out AnimationEventToolDocument reloaded, out error), Is.True, error);
            Assert.That(reloaded.Keys.Count, Is.EqualTo(2));
            Assert.That(reloaded.Keys[1].AttackKind, Is.EqualTo(AnimationAttackKind.Projectile));
            Assert.That(reloaded.IsDirty, Is.False);
        }

        [Test]
        public void SameClipCanBeSavedTwiceWithIndependentKeysAndEvents()
        {
            _document.Add(2);
            Assert.That(_document.TryAddEntry("skill2", _clip, out AnimationEventToolEntry entry, out string error), Is.True, error);
            _document.Add(11);
            Assert.That(_document.TryRenameEntry(entry, " attack2 ", out error), Is.True, error);

            Assert.That(Save(out _, out error), Is.EqualTo(AnimationEventSaveResult.Saved), error);

            Assert.That(_profile.Animations.Count, Is.EqualTo(3));
            Assert.That(_profile.Animations[0].Clip, Is.SameAs(_profile.Animations[2].Clip));
            Assert.That(_profile.Animations[0].Events[0].Frame, Is.EqualTo(2));
            Assert.That(_profile.Animations[2].Key, Is.EqualTo("attack2"));
            Assert.That(_profile.Animations[2].Events[0].Frame, Is.EqualTo(11));
            Assert.That(_profile.Animations[0].Events[0], Is.Not.SameAs(_profile.Animations[2].Events[0]));
            Assert.That(AnimationEventToolAssetStore.TryReload(_document, out AnimationEventToolDocument reloaded, out error), Is.True, error);
            Assert.That(reloaded.SelectedEntry.Key, Is.EqualTo("attack2"));
            Assert.That(reloaded.Keys[0].Frame, Is.EqualTo(11));
        }

        [Test]
        public void RemovingEveryEntrySavesAnEmptyAuthoringList()
        {
            Assert.That(_document.RemoveEntry(_document.Entries[1]), Is.True);
            Assert.That(_document.RemoveEntry(_document.Entries[0]), Is.True);

            Assert.That(Save(out AnimationEventToolSourceSnapshot next, out string error), Is.EqualTo(AnimationEventSaveResult.Saved), error);

            Assert.That(_profile.Animations, Is.Empty);
            Assert.That(_document.SelectedEntry, Is.Null);
            Assert.That(next.Validate(out error), Is.True, error);
            Assert.That(AnimationEventToolAssetStore.TryReload(_document, out AnimationEventToolDocument reloaded, out error), Is.True, error);
            Assert.That(reloaded.Entries, Is.Empty);
        }

        [Test]
        public void ExternalUnsavedProfileChangeRejectsSaveWithoutOverwritingIt()
        {
            _document.Add(3);
            SetSingleKey(0, 9, AnimationAttackKind.Projectile);

            Assert.That(Save(out _, out string error), Is.EqualTo(AnimationEventSaveResult.Rejected));

            Assert.That(error, Is.Not.Empty);
            Assert.That(_profile.Animations[0].Events[0].Frame, Is.EqualTo(9));
            Assert.That(_document.Keys[0].Frame, Is.EqualTo(3));
            Assert.That(_document.IsDirty, Is.True);
        }

        [Test]
        public void ExternalDiskChangeRejectsSaveEvenBeforeAssetDatabaseRefresh()
        {
            _document.Add(3);
            File.AppendAllText(_folder + "/Profile.asset", Environment.NewLine);

            Assert.That(Save(out _, out _), Is.EqualTo(AnimationEventSaveResult.Rejected));

            Assert.That(_profile.Animations[0].Events, Is.Empty);
            Assert.That(_document.IsDirty, Is.True);
        }

        [Test]
        public void MissingDiskSourceCannotBecomeANewValidSnapshotWhileItsObjectIsCached()
        {
            File.Delete(_folder + "/Clip.anim");

            Assert.That(_clip != null, Is.True);
            Assert.That(AnimationEventToolSourceSnapshot.TryCapture(_prefab, _profile, out _, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void SameLengthAndFrameRateClipReimportInvalidatesThePreviewSnapshot()
        {
            float length = _clip.length;
            float rate = _clip.frameRate;
            ChangeClipCurve(_clip);

            Assert.That(_clip.length, Is.EqualTo(length));
            Assert.That(_clip.frameRate, Is.EqualTo(rate));
            Assert.That(_snapshot.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void UnsavedUnselectedEntryChangeInvalidatesThePreviewSnapshot()
        {
            SetSingleKey(1, 7, AnimationAttackKind.Melee);

            Assert.That(_snapshot.Validate(out _), Is.False);
            Assert.That(Save(out _, out _), Is.EqualTo(AnimationEventSaveResult.Rejected));
        }

        [Test]
        public void UnselectedClipChangeIsWatchedAcrossTheWholeProfile()
        {
            Assert.That(_document.Clip, Is.SameAs(_clip));
            ChangeClipCurve(_otherClip);

            Assert.That(_snapshot.Validate(out _), Is.False);
            Assert.That(Save(out _, out _), Is.EqualTo(AnimationEventSaveResult.Rejected));
        }

        [Test]
        public void DeletedEntryDependenciesRemainWatchedUntilSuccessfulSave()
        {
            Assert.That(_document.RemoveEntry(_document.Entries[1]), Is.True);
            ChangeClipCurve(_otherClip);

            Assert.That(Save(out _, out _), Is.EqualTo(AnimationEventSaveResult.Rejected));

            Assert.That(_document.Entries.Count, Is.EqualTo(1));
            Assert.That(_profile.Animations.Count, Is.EqualTo(2));
            Assert.That(_document.IsDirty, Is.True);
        }

        [Test]
        public void AddingAClipCannotRebaseAnAlreadyChangedSource()
        {
            AnimationClip added = CreateClip("Added", 60f);
            File.AppendAllText(_folder + "/OtherClip.anim", Environment.NewLine);

            Assert.That(_snapshot.TryWithClip(added, out _, out string error), Is.False);

            Assert.That(error, Is.Not.Empty);
            Assert.That(_document.Entries.Count, Is.EqualTo(2));
        }

        [Test]
        public void AddedClipRequiresSnapshotExtensionBeforeSaving()
        {
            AnimationClip added = CreateClip("Added", 60f);
            Assert.That(_document.TryAddEntry("skill2", added, out _, out string error), Is.True, error);

            Assert.That(Save(out _, out _), Is.EqualTo(AnimationEventSaveResult.Rejected));

            Assert.That(_snapshot.TryWithClip(added, out _snapshot, out error), Is.True, error);
            Assert.That(Save(out AnimationEventToolSourceSnapshot next, out error), Is.EqualTo(AnimationEventSaveResult.Saved), error);
            Assert.That(next.Validate(out error), Is.True, error);
            Assert.That(_profile.Animations[2].Clip, Is.SameAs(added));
        }

        [Test]
        public void UnsavedPrefabComponentChangeInvalidatesThePreviewSnapshot()
        {
            Animator animator = _prefab.GetComponent<Animator>();
            animator.applyRootMotion = !animator.applyRootMotion;

            Assert.That(_snapshot.Validate(out _), Is.False);
        }

        [Test]
        public void BasePrefabChangeInvalidatesVariantSnapshot()
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(_prefab);
            GameObject variant;

            try
            {
                variant = PrefabUtility.SaveAsPrefabAsset(instance, _folder + "/Variant.prefab");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }

            Assert.That(AnimationEventToolSourceSnapshot.TryCapture(variant, _profile,
                out AnimationEventToolSourceSnapshot snapshot, out string error), Is.True, error);
            File.AppendAllText(AssetDatabase.GetAssetPath(_prefab), Environment.NewLine);

            Assert.That(snapshot.Validate(out _), Is.False);
        }

        [Test]
        public void SharedProfileDiscoveredAfterOpeningRejectsSave()
        {
            Assert.That(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(_prefab), _folder + "/AnotherCharacter.prefab"), Is.True);
            AssetDatabase.ImportAsset(_folder + "/AnotherCharacter.prefab", ImportAssetOptions.ForceSynchronousImport);
            _document.Add(4);

            Assert.That(Save(out _, out string error), Is.EqualTo(AnimationEventSaveResult.Rejected));

            Assert.That(error, Is.Not.Empty);
            Assert.That(_profile.Animations[0].Events, Is.Empty);
            Assert.That(_document.IsDirty, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailureBeforeWriteRestoresMemoryAndPreservesDiskDraftAndDirtyState(bool dirty)
        {
            if (dirty)
            {
                EditorUtility.SetDirty(_profile);
            }

            string before = File.ReadAllText(_folder + "/Profile.asset");
            bool wasDirty = EditorUtility.IsDirty(_profile);
            _document.Add(7);
            AnimationEventToolAssetStore.SaveTestHook = stage =>
            {
                if (stage == AnimationEventSaveStage.BeforeWrite)
                {
                    throw new IOException("Injected before write");
                }
            };

            Assert.That(Save(out _, out _), Is.EqualTo(AnimationEventSaveResult.Rejected));

            Assert.That(_profile.Animations[0].Events, Is.Empty);
            Assert.That(EditorUtility.IsDirty(_profile), Is.EqualTo(wasDirty));
            Assert.That(File.ReadAllText(_folder + "/Profile.asset"), Is.EqualTo(before));
            Assert.That(_document.IsDirty, Is.True);
            Assert.That(_document.IsSaveUncertain, Is.False);
            Assert.That(_snapshot.Validate(out string error), Is.True, error);
            AnimationEventToolAssetStore.SaveTestHook = null;
            Assert.That(Save(out _, out error), Is.EqualTo(AnimationEventSaveResult.Saved), error);
        }

        [TestCase((int)AnimationEventSaveStage.AfterWrite)]
        [TestCase((int)AnimationEventSaveStage.AfterReload)]
        public void FailureAfterWritePreservesDraftAndBlocksFurtherSaveUntilExplicitReload(int failureStage)
        {
            AnimationEventToolKey key = _document.Add(8);
            AnimationEventToolAssetStore.SaveTestHook = stage =>
            {
                if ((int)stage == failureStage)
                {
                    throw new IOException("Injected after write");
                }
            };

            Assert.That(Save(out _, out string error), Is.EqualTo(AnimationEventSaveResult.Uncertain));

            Assert.That(error, Is.Not.Empty);
            Assert.That(_document.IsSaveUncertain, Is.True);
            Assert.That(_document.IsDirty, Is.True);
            Assert.That(_document.Keys[0], Is.SameAs(key));
            Assert.That(_document.RecoveryPath, Is.EqualTo(_folder + "/Profile.asset"));
            AnimationEventToolAssetStore.SaveTestHook = null;
            Assert.That(Save(out _, out _), Is.EqualTo(AnimationEventSaveResult.Rejected));
            Assert.That(AnimationEventToolAssetStore.TryReload(_document, out AnimationEventToolDocument reloaded, out error), Is.True, error);
            Assert.That(reloaded.Keys[0].Frame, Is.EqualTo(8));
            Assert.That(reloaded.IsSaveUncertain, Is.False);
            Assert.That(_document.IsSaveUncertain, Is.True);
        }

        [Test]
        public void IndependentClipChangeDuringOwnSaveIsNotRebasedAsSuccess()
        {
            _document.Add(6);
            AnimationEventToolAssetStore.SaveTestHook = stage =>
            {
                if (stage == AnimationEventSaveStage.AfterReload)
                {
                    ChangeClipCurve(_otherClip);
                }
            };

            Assert.That(Save(out AnimationEventToolSourceSnapshot next, out _), Is.EqualTo(AnimationEventSaveResult.Uncertain));

            Assert.That(next, Is.Null);
            Assert.That(_document.IsSaveUncertain, Is.True);
            Assert.That(_document.IsDirty, Is.True);
            Assert.That(_document.Keys[0].Frame, Is.EqualTo(6));
        }

        [Test]
        public void TargetMutationAfterReloadCannotBecomeSuccessfulSave()
        {
            _document.Add(6);
            AnimationEventToolAssetStore.SaveTestHook = stage =>
            {
                if (stage == AnimationEventSaveStage.AfterReload)
                {
                    SetSingleKey(1, 12, AnimationAttackKind.Projectile);
                }
            };

            Assert.That(Save(out AnimationEventToolSourceSnapshot next, out _), Is.EqualTo(AnimationEventSaveResult.Uncertain));

            Assert.That(next, Is.Null);
            Assert.That(_document.IsSaveUncertain, Is.True);
            Assert.That(_document.Entries[1].Keys, Is.Empty);
        }

        [Test]
        public void DiskOnlyTargetMutationAfterReloadPreservesDraftAndBlocksFurtherSave()
        {
            AnimationEventToolKey key = _document.Add(6);
            string path = _folder + "/Profile.asset";
            AnimationEventToolAssetStore.SaveTestHook = stage =>
            {
                if (stage == AnimationEventSaveStage.AfterReload)
                {
                    string yaml = File.ReadAllText(path);
                    File.WriteAllText(path, yaml.Replace("_frame: 6", "_frame: 12"));
                }
            };

            Assert.That(Save(out AnimationEventToolSourceSnapshot next, out string error), Is.EqualTo(AnimationEventSaveResult.Uncertain));

            Assert.That(error, Is.Not.Empty);
            Assert.That(next, Is.Null);
            Assert.That(File.ReadAllText(path), Does.Contain("_frame: 12"));
            Assert.That(_document.IsSaveUncertain, Is.True);
            Assert.That(_document.IsDirty, Is.True);
            Assert.That(_document.Keys[0], Is.SameAs(key));
            Assert.That(_document.Keys[0].Frame, Is.EqualTo(6));
            Assert.That(_document.RecoveryPath, Is.EqualTo(path));
            AnimationEventToolAssetStore.SaveTestHook = null;
            Assert.That(Save(out _, out _), Is.EqualTo(AnimationEventSaveResult.Rejected));
            Assert.That(AnimationEventToolAssetStore.TryReload(_document, out AnimationEventToolDocument reloaded, out error), Is.True, error);
            Assert.That(reloaded.Keys[0].Frame, Is.EqualTo(12));
            Assert.That(reloaded.IsSaveUncertain, Is.False);
        }

        [Test]
        public void ReauthoringToAnotherClipCanSaveItsExpectedDependencyChange()
        {
            AnimationClip replacement = CreateClip("Replacement", 60f);
            Assert.That(_snapshot.TryWithClip(replacement, out _snapshot, out string error), Is.True, error);
            Assert.That(_document.TryReauthor(replacement, out error), Is.True, error);
            _document.Add(60);

            Assert.That(Save(out AnimationEventToolSourceSnapshot next, out error), Is.EqualTo(AnimationEventSaveResult.Saved), error);

            Assert.That(_document.Source.Animations[0].Clip, Is.SameAs(replacement));
            Assert.That(next.Validate(out error), Is.True, error);
            Assert.That(_document.Source.Animations[0].Events[0].Frame, Is.EqualTo(60));
        }

        [Test]
        public void MetadataMismatchAndMissingSnapshotCannotSave()
        {
            _otherClip.frameRate = 60f;

            Assert.That(Save(out _, out _), Is.EqualTo(AnimationEventSaveResult.Rejected));
            Assert.That(AnimationEventToolAssetStore.TrySave(_document, null, out _, out _), Is.EqualTo(AnimationEventSaveResult.Rejected));
            Assert.That(_profile.Animations[0].Events, Is.Empty);
        }

        void OpenSession()
        {
            Assert.That(AnimationEventToolDocument.TryOpen(_profile, out _document, out string error), Is.True, error);
            Assert.That(AnimationEventToolSourceSnapshot.TryCapture(_prefab, _profile, out _snapshot, out error), Is.True, error);
        }

        AnimationEventSaveResult Save(out AnimationEventToolSourceSnapshot snapshot, out string error)
        {
            return AnimationEventToolAssetStore.TrySave(_document, _snapshot, out snapshot, out error);
        }

        AnimationClip CreateClip(string name, float frameRate)
        {
            AnimationClip clip = new AnimationClip { name = name, frameRate = frameRate };
            clip.SetCurve("Animated", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 1f, 1f, 2f));
            AssetDatabase.CreateAsset(clip, _folder + "/" + name + ".anim");
            return clip;
        }

        static CharacterAnimationEntry CreateEntry(string key, AnimationClip clip)
        {
            return new CharacterAnimationEntry(key, clip, clip.frameRate, clip.length, Array.Empty<AnimationEventKey>());
        }

        static void SetEntries(CharacterAnimationProfile profile, params CharacterAnimationEntry[] entries)
        {
            typeof(CharacterAnimationProfile).GetField("_animations", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(profile, entries);
        }

        void SetSingleKey(int animationIndex, int frame, AnimationAttackKind attackKind)
        {
            SerializedObject data = new SerializedObject(_profile);
            SerializedProperty entry = data.FindProperty("_animations").GetArrayElementAtIndex(animationIndex);
            SerializedProperty keys = entry.FindPropertyRelative("_events");
            keys.arraySize = 1;
            SerializedProperty item = keys.GetArrayElementAtIndex(0);
            item.FindPropertyRelative("_frame").intValue = frame;
            item.FindPropertyRelative("_eventKind").intValue = (int)AnimationEventKind.AttackHit;
            item.FindPropertyRelative("_attackKind").intValue = (int)attackKind;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ChangeClipCurve(AnimationClip clip)
        {
            clip.SetCurve("Animated", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 4f, 1f, 5f));
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(clip), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
#endif
