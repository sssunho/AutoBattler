using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Animations;
using RaggedBanner.Combat.Unity;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RaggedBanner.AnimationTools.Tests
{
    public sealed class AnimationEventToolControllerTests
    {
        const BindingFlags _flags = BindingFlags.Instance | BindingFlags.NonPublic;
        readonly List<string> _createdProfilePaths = new List<string>();
        string _folder;
        GameObject _host;
        AnimationEventToolController _controller;
        GameObject _prefab;
        GameObject _otherPrefab;
        AnimationClip _clip;
        CharacterAnimationProfile _profile;
        CharacterAnimationProfile _otherProfile;

        [SetUp]
        public void SetUp()
        {
            _folder = "Assets/__AnimationEventToolControllerTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", _folder.Substring("Assets/".Length));
            _clip = new AnimationClip { frameRate = 30f };
            _clip.SetCurve("Animated", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 1f, 1f, 2f));
            AssetDatabase.CreateAsset(_clip, _folder + "/Clip.anim");
            _profile = CreateProfile("Profile", "idle", "move", "attack1");
            _otherProfile = CreateProfile("OtherProfile", "idle", "move", "attack1");
            _prefab = CreatePrefab("Source", _profile);
            _otherPrefab = CreatePrefab("OtherSource", _otherProfile);

            _host = new GameObject("Tool Controller Test") { hideFlags = HideFlags.HideAndDontSave };
            _controller = _host.AddComponent<AnimationEventToolController>();
            SetField("_previewRoot", _host.transform);
            SetField("_configured", true);
            Invoke("OpenPrefab", _prefab);
            Assert.That(GetField<AnimationEventToolDocument>("_document"), Is.Not.Null, GetField<string>("_error"));
            Assert.That(GetField<bool>("_blocked"), Is.False);
            Assert.That(GetField<bool>("_ownsProfile"), Is.True, GetField<string>("_ownershipError"));
            Invoke("SubscribeSourceChanges");
            AnimationEventToolProfileOwnership.SetupTestHook = TrackCreatedProfile;
        }

        [TearDown]
        public void TearDown()
        {
            AnimationEventToolAssetStore.SaveTestHook = null;
            AnimationEventToolProfileOwnership.SetupTestHook = null;

            if (_controller != null)
            {
                Invoke("OnDisable");
            }

            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }

            if (!string.IsNullOrEmpty(_folder))
            {
                AssetDatabase.DeleteAsset(_folder);
            }

            for (int i = 0; i < _createdProfilePaths.Count; i++)
            {
                AssetDatabase.DeleteAsset(_createdProfilePaths[i]);
            }

            _createdProfilePaths.Clear();
        }

        [Test]
        public void DirtyCharacterSwitchWaitsForExplicitDiscardAndKeepsOriginalAsset()
        {
            AnimationEventToolDocument original = GetField<AnimationEventToolDocument>("_document");
            original.Add(4);

            Invoke("SelectPrefab", _otherPrefab);

            Assert.That(GetField<AnimationEventToolDocument>("_document"), Is.SameAs(original));
            Assert.That(GetField<Action>("_pendingAction"), Is.Not.Null);
            Assert.That(SourceEntry(_profile, "idle").Events, Is.Empty);
            Invoke("ResolvePending", false);
            Assert.That(GetField<AnimationEventToolDocument>("_document").Source, Is.SameAs(_otherProfile));
            Assert.That(SourceEntry(_profile, "idle").Events, Is.Empty);
        }

        [Test]
        public void SaveFailurePreservesPendingChangeAndDirtyWork()
        {
            AnimationEventToolDocument original = GetField<AnimationEventToolDocument>("_document");
            original.Add(7);
            Invoke("SelectPrefab", _otherPrefab);
            AnimationEventToolAssetStore.SaveTestHook = stage =>
            {
                if (stage == AnimationEventSaveStage.BeforeWrite)
                {
                    throw new InvalidOperationException("Injected pre-write failure");
                }
            };

            Invoke("ResolvePending", true);

            Assert.That(GetField<Action>("_pendingAction"), Is.Not.Null);
            Assert.That(GetField<AnimationEventToolDocument>("_document"), Is.SameAs(original));
            Assert.That(original.IsDirty, Is.True);
            Assert.That(SourceEntry(_profile, "idle").Events, Is.Empty);
            Assert.That(GetField<AnimationEventToolPreview>("_preview").IsReady, Is.True);
        }

        [Test]
        public void SuccessfulSelfSaveKeepsPreviewValidAcrossProjectNotification()
        {
            AnimationEventToolDocument document = GetField<AnimationEventToolDocument>("_document");
            document.Add(9);

            Assert.That((bool)Invoke("Save"), Is.True, GetField<string>("_error"));
            Invoke("OnProjectChanged");

            Assert.That(document.IsDirty, Is.False);
            Assert.That(SourceEntry(document.Source, "idle").Events.Count, Is.EqualTo(1));
            Assert.That(GetField<bool>("_blocked"), Is.False, GetField<string>("_error"));
            Assert.That(GetField<AnimationEventToolPreview>("_preview").IsReady, Is.True);
        }

        [Test]
        public void ContentOnlyReimportQueuesSourceCheckAndPreservesDirtyDraftUntilProcessed()
        {
            AnimationEventToolDocument original = GetField<AnimationEventToolDocument>("_document");
            original.Add(12);
            AnimationEventToolPreview preview = GetField<AnimationEventToolPreview>("_preview");
            Assert.That(preview.TrySetPlaying(true, out string error), Is.True, error);
            string path = AssetDatabase.GetAssetPath(_profile);

            File.AppendAllText(path, Environment.NewLine);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            Assert.That(GetField<bool>("_checkSourcesPending"), Is.True);
            Assert.That(GetField<bool>("_blocked"), Is.False);
            Assert.That(preview.IsReady, Is.True);
            Invoke("ProcessPendingSourceChanges");
            Assert.That(GetField<bool>("_checkSourcesPending"), Is.False);
            Assert.That(GetField<bool>("_blocked"), Is.True);
            Assert.That(preview.IsReady, Is.False);
            Assert.That(preview.IsPlaying, Is.False);
            Assert.That(GetField<AnimationEventToolDocument>("_document"), Is.SameAs(original));
            Assert.That(original.Keys.Count, Is.EqualTo(1));
            Assert.That(original.Keys[0].Frame, Is.EqualTo(12));
            Assert.That(original.IsDirty, Is.True);
            Assert.That(SourceEntry(_profile, "idle").Events, Is.Empty);
        }

        [Test]
        public void SelfSaveImportWaitsForCompletedSaveBeforeCheckingNewSnapshot()
        {
            AnimationEventToolDocument document = GetField<AnimationEventToolDocument>("_document");
            document.Add(9);
            bool checkedDuringSave = false;
            AnimationEventToolAssetStore.SaveTestHook = stage =>
            {
                if (stage == AnimationEventSaveStage.AfterReload)
                {
                    Assert.That(GetField<bool>("_checkSourcesPending"), Is.True);
                    Invoke("ProcessPendingSourceChanges");
                    Assert.That(GetField<bool>("_checkSourcesPending"), Is.True);
                    Assert.That(GetField<bool>("_blocked"), Is.False);
                    checkedDuringSave = true;
                }
            };

            Assert.That((bool)Invoke("Save"), Is.True, GetField<string>("_error"));

            Assert.That(checkedDuringSave, Is.True);
            Assert.That(GetField<bool>("_checkSourcesPending"), Is.True);
            Invoke("ProcessPendingSourceChanges");
            Assert.That(GetField<bool>("_checkSourcesPending"), Is.False);
            Assert.That(GetField<bool>("_blocked"), Is.False, GetField<string>("_error"));
            Assert.That(GetField<AnimationEventToolPreview>("_preview").IsReady, Is.True);
            Assert.That(document.IsDirty, Is.False);
            Assert.That(SourceEntry(document.Source, "idle").Events.Count, Is.EqualTo(1));
        }

        [Test]
        public void DisableUnsubscribesImportNotificationsAndClearsPendingCheck()
        {
            string path = AssetDatabase.GetAssetPath(_profile);
            Invoke("SubscribeSourceChanges");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Assert.That(GetField<bool>("_checkSourcesPending"), Is.True);

            Invoke("OnDisable");

            Assert.That(GetField<bool>("_checkSourcesPending"), Is.False);
            Assert.That(GetField<bool>("_sourceChangesSubscribed"), Is.False);
            SetField("_configured", true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Assert.That(GetField<bool>("_checkSourcesPending"), Is.False);
            Assert.That(GetField<AnimationEventToolDocument>("_document"), Is.Null);
        }

        [Test]
        public void ExternalSourceChangeStopsPreviewAndPreservesDirtyKeysUntilReload()
        {
            AnimationEventToolDocument original = GetField<AnimationEventToolDocument>("_document");
            original.Add(12);
            AnimationEventToolPreview preview = GetField<AnimationEventToolPreview>("_preview");
            Assert.That(preview.TrySetPlaying(true, out string error), Is.True, error);
            using (SerializedObject data = new SerializedObject(_profile))
            {
                data.FindProperty("_animations").GetArrayElementAtIndex(1)
                    .FindPropertyRelative("_authoredFrameRate").floatValue = 24f;
                data.ApplyModifiedPropertiesWithoutUndo();
            }

            Invoke("OnProjectChanged");

            Assert.That(GetField<bool>("_blocked"), Is.True);
            Assert.That(preview.IsReady, Is.False);
            Assert.That(preview.IsPlaying, Is.False);
            Assert.That(original.Keys.Count, Is.EqualTo(1));
            Assert.That(original.IsDirty, Is.True);
            Assert.That((bool)Invoke("Save"), Is.False);
            Invoke("RequestReload");
            Invoke("ResolvePending", false);
            Assert.That(GetField<AnimationEventToolDocument>("_document"), Is.Not.SameAs(original));
            Assert.That(GetField<AnimationEventToolDocument>("_document").Keys, Is.Empty);
        }

        [Test]
        public void UncertainSaveBlocksEditingAndExplicitReloadRecoversDiskResult()
        {
            AnimationEventToolDocument original = GetField<AnimationEventToolDocument>("_document");
            original.Add(15);
            AnimationEventToolAssetStore.SaveTestHook = stage =>
            {
                if (stage == AnimationEventSaveStage.AfterWrite)
                {
                    throw new InvalidOperationException("Injected post-write failure");
                }
            };

            Assert.That((bool)Invoke("Save"), Is.False);

            Assert.That(GetField<bool>("_blocked"), Is.True);
            Assert.That(original.IsSaveUncertain, Is.True);
            Assert.That(original.IsDirty, Is.True);
            Assert.That(GetField<AnimationEventToolPreview>("_preview").IsReady, Is.False);
            Assert.That((bool)Invoke("Save"), Is.False);
            AnimationEventToolAssetStore.SaveTestHook = null;
            Invoke("RequestReload");
            Invoke("ResolvePending", false);
            AnimationEventToolDocument restored = GetField<AnimationEventToolDocument>("_document");
            Assert.That(restored, Is.Not.SameAs(original));
            Assert.That(restored.IsSaveUncertain, Is.False);
            Assert.That(restored.IsDirty, Is.False);
            Assert.That(restored.Keys[0].Frame, Is.EqualTo(15));
            Assert.That(GetField<bool>("_blocked"), Is.False, GetField<string>("_error"));
        }

        [Test]
        public void SelectingKeyStopsPlaybackAndEditingFrameKeepsSelectionIdentity()
        {
            AnimationEventToolDocument document = GetField<AnimationEventToolDocument>("_document");
            AnimationEventToolKey first = document.Add(5);
            document.Add(10);
            AnimationEventToolPreview preview = GetField<AnimationEventToolPreview>("_preview");
            Assert.That(preview.TrySetPlaying(true, out string error), Is.True, error);

            Invoke("SelectKey", first);

            Assert.That(preview.IsPlaying, Is.False);
            Assert.That(preview.CurrentFrame, Is.EqualTo(5));
            Assert.That(document.TryEdit(first, 20, AnimationAttackKind.Projectile, out error), Is.True, error);
            Assert.That(GetField<AnimationEventToolKey>("_selectedKey"), Is.SameAs(first));
            Assert.That(document.Keys[1], Is.SameAs(first));
        }

        [TestCase("Warrior")]
        [TestCase("Archer")]
        [TestCase("Rogue")]
        [Category("ProjectContent")]
        public void CombatPrefabOpensWithSourceChecksAndScrubsWithoutChangingOriginalDefinition(string unitClass)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/" + unitClass + ".prefab");
            CharacterAnimationProfile profile = AnimationEventToolCatalog.GetProfile(prefab);
            string sourceBefore = AnimationEventToolSourceSnapshot.SerializeObject(profile);

            Invoke("OpenPrefab", prefab);

            AnimationEventToolDocument document = GetField<AnimationEventToolDocument>("_document");
            Assert.That(GetField<GameObject>("_prefab"), Is.SameAs(prefab), GetField<string>("_error"));
            Assert.That(GetField<bool>("_blocked"), Is.False, GetField<string>("_error"));
            Assert.That(document.Source, Is.SameAs(profile));
            AnimationEventToolKey key = document.Add(12);
            Invoke("SelectKey", key);
            Invoke("OnProjectChanged");
            Assert.That(GetField<bool>("_blocked"), Is.False, GetField<string>("_error"));
            Assert.That(GetField<AnimationEventToolPreview>("_preview").CurrentFrame, Is.EqualTo(12));
            Assert.That(GetField<AnimationEventToolPreview>("_preview").IsReady, Is.True);
            Assert.That(document.IsDirty, Is.True);
            Assert.That(AnimationEventToolSourceSnapshot.SerializeObject(profile), Is.EqualTo(sourceBefore));
        }

        [Test]
        public void DirtyEntrySwitchKeepsWholeProfileDraftWithoutPromptOrSourceChanges()
        {
            AnimationEventToolDocument document = Document;
            AnimationEventToolEntry first = document.SelectedEntry;
            AnimationEventToolEntry second = document.Entries[1];
            AnimationEventToolKey firstKey = document.Add(4);

            Invoke("SelectEntry", second);

            Assert.That(Document, Is.SameAs(document));
            Assert.That(document.SelectedEntry, Is.SameAs(second));
            Assert.That(GetField<Action>("_pendingAction"), Is.Null);
            AnimationEventToolKey secondKey = document.Add(8);
            Invoke("SelectEntry", first);
            Assert.That(document.Keys[0], Is.SameAs(firstKey));
            Assert.That(second.Keys[0], Is.SameAs(secondKey));
            Assert.That(document.IsDirty, Is.True);
            Assert.That(SourceEntry(_profile, "idle").Events, Is.Empty);
            Assert.That(SourceEntry(_profile, "move").Events, Is.Empty);
        }

        [Test]
        public void CharacterSwitchSavePersistsAllEditedEntriesBeforeOpeningNextCharacter()
        {
            Document.Add(4);
            Invoke("SelectEntry", Document.Entries[1]);
            Document.Add(8);
            Invoke("SelectPrefab", _otherPrefab);

            Invoke("ResolvePending", true);

            Assert.That(Document.Source, Is.SameAs(_otherProfile), GetField<string>("_error"));
            Assert.That(GetField<Action>("_pendingAction"), Is.Null);
            Assert.That(SourceEntry(_profile, "idle").Events[0].Frame, Is.EqualTo(4));
            Assert.That(SourceEntry(_profile, "move").Events[0].Frame, Is.EqualTo(8));
            Assert.That(SourceEntry(_otherProfile, "idle").Events, Is.Empty);
        }

        [Test]
        public void CancellingCharacterSwitchKeepsDraftAndAllowsAnotherSwitchRequest()
        {
            AnimationEventToolDocument original = Document;
            AnimationEventToolKey key = original.Add(6);
            Invoke("SelectPrefab", _otherPrefab);
            Assert.That(GetField<Action>("_pendingAction"), Is.Not.Null);

            SetField("_pendingAction", null);

            Assert.That(Document, Is.SameAs(original));
            Assert.That(original.Keys[0], Is.SameAs(key));
            Assert.That(original.IsDirty, Is.True);
            Assert.That(GetField<GameObject>("_prefab"), Is.SameAs(_prefab));
            Assert.That(Preview.IsReady, Is.True);
            Invoke("SelectPrefab", _otherPrefab);
            Assert.That(GetField<Action>("_pendingAction"), Is.Not.Null);
            Invoke("ResolvePending", false);
            Assert.That(Document.Source, Is.SameAs(_otherProfile));
        }

        [Test]
        public void SameClipCanBeAddedUnderAnotherKeyAndSavedWithIndependentEvents()
        {
            AnimationEventToolDocument document = Document;
            AnimationEventToolEntry idle = document.SelectedEntry;
            document.Add(3);
            SetField("_newEntryKey", " skill1 ");

            Invoke("AddClip", _clip);

            Assert.That(document.Entries.Count, Is.EqualTo(4), GetField<string>("_error"));
            AnimationEventToolEntry skill = document.SelectedEntry;
            Assert.That(skill.Key, Is.EqualTo("skill1"));
            Assert.That(skill.Clip, Is.SameAs(idle.Clip));
            Assert.That(skill.Keys, Is.Empty);
            AnimationEventToolKey hit = document.Add(12);
            Assert.That(document.TryEdit(hit, 12, AnimationAttackKind.Projectile, out string error), Is.True, error);
            Invoke("SelectEntry", idle);
            Assert.That(document.Keys[0].Frame, Is.EqualTo(3));
            Invoke("SelectEntry", skill);
            Assert.That((bool)Invoke("Save"), Is.True, GetField<string>("_error"));
            Assert.That(document.SelectedEntry, Is.SameAs(skill));
            Assert.That(SourceEntry(_profile, "idle").Events[0].Frame, Is.EqualTo(3));
            Assert.That(SourceEntry(_profile, "skill1").Events[0].Frame, Is.EqualTo(12));
            Assert.That(SourceEntry(_profile, "skill1").Events[0].AttackKind, Is.EqualTo(AnimationAttackKind.Projectile));
            Invoke("Reload");
            Assert.That(Document.SelectedEntry.Key, Is.EqualTo("skill1"));
            Assert.That(Document.Keys[0].Frame, Is.EqualTo(12));
        }

        [Test]
        public void DuplicateNormalizedKeyCannotAddOrRenameAnEntry()
        {
            AnimationEventToolDocument document = Document;
            AnimationEventToolEntry selected = document.SelectedEntry;
            SetField("_newEntryKey", " idle ");

            Invoke("AddClip", _clip);

            Assert.That(document.Entries.Count, Is.EqualTo(3));
            Assert.That(document.SelectedEntry, Is.SameAs(selected));
            Assert.That(document.IsDirty, Is.False);
            Assert.That(GetField<string>("_error"), Is.Not.Null.And.Not.Empty);
            SetField("_entryKeyInput", " move ");
            Invoke("RenameEntry");
            Assert.That(selected.Key, Is.EqualTo("idle"));
            Assert.That(document.IsDirty, Is.False);
            Assert.That(Preview.IsReady, Is.True);
        }

        [Test]
        public void RenamingPreservesSelectionAndRemovingSelectedEntryClearsEventSelection()
        {
            AnimationEventToolDocument document = Document;
            AnimationEventToolEntry original = document.SelectedEntry;
            AnimationEventToolKey hit = document.Add(5);
            Invoke("SelectKey", hit);
            SetField("_entryKeyInput", " attack2 ");

            Invoke("RenameEntry");

            Assert.That(document.SelectedEntry, Is.SameAs(original));
            Assert.That(original.Key, Is.EqualTo("attack2"));
            Assert.That(GetField<AnimationEventToolKey>("_selectedKey"), Is.SameAs(hit));
            Assert.That((bool)Invoke("Save"), Is.True, GetField<string>("_error"));
            Invoke("Reload");
            Assert.That(Document.SelectedEntry.Key, Is.EqualTo("attack2"));
            Invoke("SelectKey", Document.Keys[0]);
            Invoke("RemoveEntry");
            Assert.That(Document.Entries.Count, Is.EqualTo(2));
            Assert.That(Document.SelectedEntry.Key, Is.Not.EqualTo("attack2"));
            Assert.That(GetField<AnimationEventToolKey>("_selectedKey"), Is.Null);
            Assert.That(Preview.IsReady, Is.True);
        }

        [Test]
        public void RemovingEveryEntryLeavesRestPoseAndCanSaveAnEmptyCharacterList()
        {
            AnimationEventToolDocument document = Document;
            int count = document.Entries.Count;

            for (int i = 0; i < count; i++)
            {
                Invoke("RemoveEntry");
            }

            Assert.That(document.Entries, Is.Empty);
            Assert.That(document.SelectedEntry, Is.Null);
            Assert.That(document.Clip, Is.Null);
            Assert.That(Preview.IsReady, Is.True);
            Assert.That(Preview.Instance, Is.Not.Null);
            Assert.That(Preview.TrySetPlaying(true, out _), Is.False);
            Assert.That((bool)Invoke("Save"), Is.True, GetField<string>("_error"));
            Assert.That(_profile.Animations, Is.Empty);
            Assert.That(_profile.Validate(out _), Is.True);
            Assert.That(_profile.ValidateForPlayback(out _), Is.False);
        }

        [Test]
        public void EmptyProfileSpawnsRestPoseAndAcceptsItsFirstAssignedClip()
        {
            CharacterAnimationProfile empty = CreateProfile("EmptyProfile");
            GameObject prefab = CreatePrefab("EmptySource", empty);

            Invoke("OpenPrefab", prefab);

            Assert.That(Document.Source, Is.SameAs(empty));
            Assert.That(Document.Entries, Is.Empty);
            Assert.That(Document.SelectedEntry, Is.Null);
            Assert.That(Preview.IsReady, Is.True);
            Assert.That(Preview.Instance, Is.Not.Null);
            SetField("_newEntryKey", "idle");
            Invoke("AddClip", _clip);
            Assert.That(Document.SelectedEntry.Key, Is.EqualTo("idle"), GetField<string>("_error"));
            Assert.That(Preview.TrySetFrame(5, out string error), Is.True, error);
            Assert.That(empty.Animations, Is.Empty);
        }

        [Test]
        public void MissingProfileSpawnsRestPoseAndDedicatedSetupCreatesAnOwnedEmptyList()
        {
            GameObject prefab = CreatePrefab("MissingProfileSource", null);
            Invoke("OpenPrefab", prefab);
            Assert.That(Document, Is.Null);
            Assert.That(GetField<bool>("_ownsProfile"), Is.False);
            Assert.That(Preview.IsReady, Is.True);
            Assert.That(Preview.Instance, Is.Not.Null);
            Assert.That((bool)Invoke("Save"), Is.False);

            Invoke("CreateDedicatedProfile");

            Assert.That(Document, Is.Not.Null, GetField<string>("_error"));
            Assert.That(Document.Entries, Is.Empty);
            Assert.That(GetField<bool>("_ownsProfile"), Is.True, GetField<string>("_ownershipError"));
            Assert.That(AnimationEventToolCatalog.GetProfile(prefab), Is.SameAs(Document.Source));
            Assert.That(Preview.IsReady, Is.True);
            SetField("_newEntryKey", "idle");
            Invoke("AddClip", _clip);
            Document.Add(7);
            Assert.That((bool)Invoke("Save"), Is.True, GetField<string>("_error"));
            Assert.That(SourceEntry(Document.Source, "idle").Events[0].Frame, Is.EqualTo(7));
        }

        [Test]
        public void SharedProfileRequiresDedicatedCopyAndDoesNotChangeOtherCharacters()
        {
            GameObject shared = CreatePrefab("SharedSource", _profile);
            Invoke("OpenPrefab", shared);
            Assert.That(GetField<bool>("_ownsProfile"), Is.False);
            Assert.That((bool)Invoke("Save"), Is.False);
            string original = AnimationEventToolSourceSnapshot.SerializeObject(_profile);

            Invoke("CreateDedicatedProfile");

            Assert.That(GetField<bool>("_ownsProfile"), Is.True, GetField<string>("_error"));
            Assert.That(Document.Source, Is.Not.SameAs(_profile));
            Assert.That(AnimationEventToolCatalog.GetProfile(_prefab), Is.SameAs(_profile));
            Assert.That(AnimationEventToolCatalog.GetProfile(shared), Is.SameAs(Document.Source));
            Document.Add(11);
            Assert.That((bool)Invoke("Save"), Is.True, GetField<string>("_error"));
            Assert.That(SourceEntry(Document.Source, "idle").Events[0].Frame, Is.EqualTo(11));
            Assert.That(AnimationEventToolSourceSnapshot.SerializeObject(_profile), Is.EqualTo(original));
        }

        [Test]
        public void UncertainDedicatedSetupBlocksAutomaticRetryAndRecordsGeneratedAsset()
        {
            GameObject prefab = CreatePrefab("UncertainSetupSource", null);
            Invoke("OpenPrefab", prefab);
            int writes = 0;
            AnimationEventToolProfileOwnership.SetupTestHook = stage =>
            {
                TrackCreatedProfile(stage);

                if (stage == AnimationProfileSetupStage.AfterProfileWrite)
                {
                    writes++;
                    throw new InvalidOperationException("Injected profile setup failure");
                }
            };

            Invoke("CreateDedicatedProfile");

            Assert.That(GetField<bool>("_setupUncertain"), Is.True);
            Assert.That(GetField<bool>("_blocked"), Is.True);
            Assert.That(GetField<string>("_setupRecoveryPath"), Is.Not.Null.And.Not.Empty);
            Assert.That(Preview.IsReady, Is.False);
            Invoke("CreateDedicatedProfile");
            Assert.That(writes, Is.EqualTo(1));
            Assert.That(AnimationEventToolCatalog.GetProfile(prefab), Is.Null);
        }

        AnimationEventToolDocument Document => GetField<AnimationEventToolDocument>("_document");
        AnimationEventToolPreview Preview => GetField<AnimationEventToolPreview>("_preview");

        CharacterAnimationProfile CreateProfile(string name, params string[] keys)
        {
            CharacterAnimationProfile profile = ScriptableObject.CreateInstance<CharacterAnimationProfile>();
            CharacterAnimationEntry[] entries = new CharacterAnimationEntry[keys.Length];

            for (int i = 0; i < keys.Length; i++)
            {
                entries[i] = new CharacterAnimationEntry(keys[i], _clip, _clip.frameRate, _clip.length,
                    Array.Empty<AnimationEventKey>());
            }

            typeof(CharacterAnimationProfile).GetField("_animations", _flags).SetValue(profile, entries);
            AssetDatabase.CreateAsset(profile, _folder + "/" + name + ".asset");
            return profile;
        }

        GameObject CreatePrefab(string name, CharacterAnimationProfile profile)
        {
            GameObject source = new GameObject(name);

            try
            {
                source.AddComponent<Animator>();
                new GameObject("Animated").transform.SetParent(source.transform, false);
                HexCharacterView view = source.AddComponent<HexCharacterView>();

                using (SerializedObject data = new SerializedObject(view))
                {
                    data.FindProperty("_animationProfile").objectReferenceValue = profile;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }

                return PrefabUtility.SaveAsPrefabAsset(source, _folder + "/" + name + ".prefab");
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        static CharacterAnimationEntry SourceEntry(CharacterAnimationProfile profile, string key)
        {
            Assert.That(profile.TryGetAnimation(key, out CharacterAnimationEntry entry), Is.True);
            return entry;
        }

        void TrackCreatedProfile(AnimationProfileSetupStage stage)
        {
            if (stage == AnimationProfileSetupStage.AfterProfileWrite)
            {
                string path = GetField<string>("_setupRecoveryPath");

                if (!string.IsNullOrEmpty(path) && !_createdProfilePaths.Contains(path))
                {
                    _createdProfilePaths.Add(path);
                }
            }
        }

        object Invoke(string method, params object[] arguments)
        {
            return typeof(AnimationEventToolController).GetMethod(method, _flags).Invoke(_controller, arguments);
        }

        T GetField<T>(string name)
        {
            return (T)typeof(AnimationEventToolController).GetField(name, _flags).GetValue(_controller);
        }

        void SetField(string name, object value)
        {
            typeof(AnimationEventToolController).GetField(name, _flags).SetValue(_controller, value);
        }
    }
}
