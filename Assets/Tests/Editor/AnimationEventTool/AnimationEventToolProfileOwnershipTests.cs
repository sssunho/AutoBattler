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
    internal sealed class AnimationEventToolProfileTestAssets : IDisposable
    {
        readonly string _folder;

        internal string Folder => _folder;

        internal AnimationEventToolProfileTestAssets()
        {
            _folder = "Assets/__AnimationProfileTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(_folder));
        }

        internal AnimationClip CreateClip(string name)
        {
            AnimationClip clip = new AnimationClip { name = name, frameRate = 30f };
            clip.SetCurve("Animated", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 1f, 1f, 2f));
            AssetDatabase.CreateAsset(clip, _folder + "/" + name + ".anim");
            return clip;
        }

        internal CharacterAnimationProfile CreateProfile(string name, params CharacterAnimationEntry[] entries)
        {
            CharacterAnimationProfile profile = ScriptableObject.CreateInstance<CharacterAnimationProfile>();
            profile.name = name;
            typeof(CharacterAnimationProfile).GetField("_animations", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(profile, entries);
            AssetDatabase.CreateAsset(profile, _folder + "/" + name + ".asset");
            return profile;
        }

        internal GameObject CreatePrefab(string name, CharacterAnimationProfile profile)
        {
            GameObject temporary = new GameObject(name);
            temporary.SetActive(false);

            try
            {
                temporary.AddComponent<Animator>();
                Type viewType = Type.GetType("RaggedBanner.Combat.Unity.HexCharacterView, RaggedBanner.CombatView");
                Assert.That(viewType, Is.Not.Null);
                Component view = temporary.AddComponent(viewType);
                SetProfile(view, profile);
                return PrefabUtility.SaveAsPrefabAsset(temporary, _folder + "/" + name + ".prefab");
            }
            finally
            {
                Object.DestroyImmediate(temporary);
            }
        }

        internal GameObject CreateVariant(string name, GameObject source, bool assignProfile = false,
            CharacterAnimationProfile profile = null)
        {
            GameObject temporary = (GameObject)PrefabUtility.InstantiatePrefab(source);

            try
            {
                temporary.name = name;

                if (assignProfile)
                {
                    Component view = temporary.GetComponent(Type.GetType("RaggedBanner.Combat.Unity.HexCharacterView, RaggedBanner.CombatView"));
                    SetProfile(view, profile);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(view);
                }

                GameObject result = PrefabUtility.SaveAsPrefabAsset(temporary, _folder + "/" + name + ".prefab");
                Assert.That(PrefabUtility.GetPrefabAssetType(result), Is.EqualTo(PrefabAssetType.Variant));
                return result;
            }
            finally
            {
                Object.DestroyImmediate(temporary);
            }
        }

        internal static CharacterAnimationEntry Entry(string key, AnimationClip clip, int frame = 3)
        {
            return new CharacterAnimationEntry(key, clip, clip.frameRate, clip.length,
                new[] { new AnimationEventKey(frame, AnimationEventKind.AttackHit, AnimationAttackKind.Melee) });
        }

        internal static void SetProfile(Component view, CharacterAnimationProfile profile)
        {
            using (SerializedObject data = new SerializedObject(view))
            {
                data.FindProperty("_animationProfile").objectReferenceValue = profile;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public void Dispose()
        {
            AnimationEventToolProfileOwnership.SetupTestHook = null;

            if (_folder.StartsWith("Assets/__AnimationProfileTests_", StringComparison.Ordinal)
                && AssetDatabase.IsValidFolder(_folder))
            {
                AssetDatabase.DeleteAsset(_folder);
            }
        }
    }

    public sealed class AnimationEventToolProfileOwnershipTests
    {
        AnimationEventToolProfileTestAssets _assets;

        [SetUp]
        public void SetUp()
        {
            _assets = new AnimationEventToolProfileTestAssets();
        }

        [TearDown]
        public void TearDown()
        {
            _assets.Dispose();
        }

        [Test]
        public void MissingProfileCreatesAnEmptyOwnedListAndLinksOnlyTheSelectedPrefab()
        {
            GameObject selected = _assets.CreatePrefab("Selected", null);
            GameObject other = _assets.CreatePrefab("Other", null);
            Assert.That(AnimationEventToolProfileOwnership.TryValidateOwnership(selected, null, out _), Is.False);

            Assert.That(Create(selected, null, out CharacterAnimationProfile created, out string path,
                out bool uncertain, out string error), Is.True, error);

            Assert.That(uncertain, Is.False);
            Assert.That(path, Does.StartWith(_assets.Folder + "/"));
            Assert.That(created.Animations, Is.Empty);
            Assert.That(AnimationEventToolCatalog.GetProfile(selected), Is.SameAs(created));
            Assert.That(AnimationEventToolCatalog.GetProfile(other), Is.Null);
            Assert.That(AnimationEventToolProfileOwnership.TryValidateOwnership(selected, created, out error), Is.True, error);
        }

        [Test]
        public void SharedProfileCopyHasIndependentEntriesAndLeavesTheOtherConnectionUnchanged()
        {
            AnimationClip clip = _assets.CreateClip("Clip");
            CharacterAnimationProfile shared = _assets.CreateProfile("Shared",
                AnimationEventToolProfileTestAssets.Entry("attack1", clip),
                AnimationEventToolProfileTestAssets.Entry("skill1", clip, 7));
            GameObject selected = _assets.CreatePrefab("Selected", shared);
            GameObject other = _assets.CreatePrefab("Other", shared);
            Assert.That(AnimationEventToolProfileOwnership.TryValidateOwnership(selected, shared, out string error), Is.False);
            Assert.That(error, Does.Contain("Other.prefab"));

            Assert.That(Create(selected, shared, out CharacterAnimationProfile created, out _, out _, out error), Is.True, error);

            Assert.That(AnimationEventToolCatalog.GetProfile(selected), Is.SameAs(created));
            Assert.That(AnimationEventToolCatalog.GetProfile(other), Is.SameAs(shared));
            Assert.That(created.Animations.Count, Is.EqualTo(2));
            Assert.That(created.Animations[0], Is.Not.SameAs(shared.Animations[0]));
            Assert.That(created.Animations[0].Events[0], Is.Not.SameAs(shared.Animations[0].Events[0]));
            Assert.That(created.Animations[1].Key, Is.EqualTo("skill1"));
            Assert.That(created.Animations[1].Events[0].Frame, Is.EqualTo(7));
            Assert.That(created.Animations[0].Clip, Is.SameAs(created.Animations[1].Clip));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BaseWithInheritedDescendantsIsRejectedBeforeWritingEvenForNullProfile(bool hasProfile)
        {
            CharacterAnimationProfile profile = hasProfile ? _assets.CreateProfile("Profile") : null;
            GameObject selected = _assets.CreatePrefab("Base", profile);
            GameObject intermediate = _assets.CreateVariant("Intermediate", selected);
            GameObject leaf = _assets.CreateVariant("Leaf", intermediate);
            string[] before = AssetDatabase.FindAssets("t:CharacterAnimationProfile", new[] { _assets.Folder });

            Assert.That(Create(selected, profile, out CharacterAnimationProfile created, out string path,
                out bool uncertain, out string error), Is.False);

            Assert.That(created, Is.Null);
            Assert.That(path, Is.Null);
            Assert.That(uncertain, Is.False);
            Assert.That(error, Does.Contain("Intermediate.prefab").And.Contain("Leaf.prefab"));
            CollectionAssert.AreEquivalent(before,
                AssetDatabase.FindAssets("t:CharacterAnimationProfile", new[] { _assets.Folder }));
            Assert.That(AnimationEventToolCatalog.GetProfile(leaf), Is.SameAs(profile));
        }

        [Test]
        public void LeafThenIntermediateThenBaseCreatesExplicitIndependentVariantConnections()
        {
            GameObject root = _assets.CreatePrefab("Base", null);
            GameObject intermediate = _assets.CreateVariant("Intermediate", root);
            GameObject leaf = _assets.CreateVariant("Leaf", intermediate);

            Assert.That(Create(leaf, null, out CharacterAnimationProfile leafProfile, out _, out _, out string error), Is.True, error);
            Assert.That(AnimationEventToolCatalog.GetProfile(intermediate), Is.Null);
            Assert.That(Create(intermediate, null, out CharacterAnimationProfile intermediateProfile, out _, out _, out error), Is.True, error);
            Assert.That(AnimationEventToolCatalog.GetProfile(root), Is.Null);
            Assert.That(Create(root, null, out CharacterAnimationProfile rootProfile, out _, out _, out error), Is.True, error);

            Assert.That(AnimationEventToolCatalog.GetProfile(leaf), Is.SameAs(leafProfile));
            Assert.That(AnimationEventToolCatalog.GetProfile(intermediate), Is.SameAs(intermediateProfile));
            Assert.That(AnimationEventToolCatalog.GetProfile(root), Is.SameAs(rootProfile));
            Assert.That(rootProfile, Is.Not.SameAs(intermediateProfile));
            Assert.That(intermediateProfile, Is.Not.SameAs(leafProfile));
        }

        [Test]
        public void ExistingDescendantOverrideAllowsBaseCopyWithoutChangingTheDescendant()
        {
            CharacterAnimationProfile original = _assets.CreateProfile("Original");
            CharacterAnimationProfile own = _assets.CreateProfile("Own");
            GameObject root = _assets.CreatePrefab("Base", original);
            GameObject leaf = _assets.CreateVariant("Leaf", root, true, own);

            Assert.That(Create(root, original, out CharacterAnimationProfile created, out _, out _, out string error), Is.True, error);

            Assert.That(AnimationEventToolCatalog.GetProfile(root), Is.SameAs(created));
            Assert.That(AnimationEventToolCatalog.GetProfile(leaf), Is.SameAs(own));
        }

        [Test]
        public void SourceChangeBeforeWritingRejectsWithoutCreatingOrLinkingAnAsset()
        {
            AnimationClip clip = _assets.CreateClip("Clip");
            CharacterAnimationProfile profile = _assets.CreateProfile("Profile", AnimationEventToolProfileTestAssets.Entry("attack1", clip));
            GameObject selected = _assets.CreatePrefab("Selected", profile);
            AnimationEventToolProfileOwnership.SetupTestHook = stage =>
            {
                if (stage == AnimationProfileSetupStage.BeforeProfileWrite)
                {
                    clip.frameRate = 60f;
                }
            };

            Assert.That(Create(selected, profile, out _, out string path, out bool uncertain, out _), Is.False);

            Assert.That(path, Is.Null);
            Assert.That(uncertain, Is.False);
            Assert.That(AnimationEventToolCatalog.GetProfile(selected), Is.SameAs(profile));
            Assert.That(AssetDatabase.FindAssets("t:CharacterAnimationProfile", new[] { _assets.Folder }).Length, Is.EqualTo(1));
        }

        [Test]
        public void FailureAfterProfileWritePreservesRecoveryAssetAndDoesNotLinkOrRetry()
        {
            GameObject selected = _assets.CreatePrefab("Selected", null);
            int calls = 0;
            AnimationEventToolProfileOwnership.SetupTestHook = stage =>
            {
                if (stage == AnimationProfileSetupStage.AfterProfileWrite)
                {
                    calls++;
                    throw new InvalidOperationException("Injected write failure");
                }
            };

            Assert.That(Create(selected, null, out _, out string path, out bool uncertain, out string error), Is.False);

            Assert.That(uncertain, Is.True);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(AssetDatabase.LoadAssetAtPath<CharacterAnimationProfile>(path), Is.Not.Null);
            Assert.That(AnimationEventToolCatalog.GetProfile(selected), Is.Null);
            Assert.That(error, Does.Contain(path).And.Contain("was not attempted"));
        }

        [Test]
        public void SourceChangeAfterLinkIsUncertainAndPreservesConnectionForRecovery()
        {
            AnimationClip clip = _assets.CreateClip("Clip");
            CharacterAnimationProfile source = _assets.CreateProfile("Profile", AnimationEventToolProfileTestAssets.Entry("attack1", clip));
            GameObject selected = _assets.CreatePrefab("Selected", source);
            AnimationEventToolProfileOwnership.SetupTestHook = stage =>
            {
                if (stage == AnimationProfileSetupStage.AfterPrefabWrite)
                {
                    clip.frameRate = 60f;
                }
            };

            Assert.That(Create(selected, source, out _, out string path, out bool uncertain, out string error), Is.False);

            Assert.That(uncertain, Is.True);
            Assert.That(AnimationEventToolCatalog.GetProfile(selected),
                Is.SameAs(AssetDatabase.LoadAssetAtPath<CharacterAnimationProfile>(path)));
            Assert.That(error, Does.Contain("was attempted"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DiskOnlyChangeToCreatedProfileAfterLinkKeepsSetupUncertain(bool metadata)
        {
            GameObject selected = _assets.CreatePrefab("Selected", null);
            AnimationEventToolProfileOwnership.SetupTestHook = stage =>
            {
                if (stage == AnimationProfileSetupStage.AfterPrefabWrite)
                {
                    CharacterAnimationProfile linked = AnimationEventToolCatalog.GetProfile(selected);
                    string linkedPath = AssetDatabase.GetAssetPath(linked);
                    Assert.That(linkedPath, Is.Not.Empty);
                    File.AppendAllText(linkedPath + (metadata ? ".meta" : string.Empty), Environment.NewLine);
                }
            };

            Assert.That(Create(selected, null, out _, out string path, out bool uncertain, out string error), Is.False);

            Assert.That(uncertain, Is.True);
            Assert.That(error, Does.Contain("was attempted"));
            Assert.That(AssetDatabase.LoadAssetAtPath<CharacterAnimationProfile>(path), Is.Not.Null);
            Assert.That(AnimationEventToolCatalog.GetProfile(selected), Is.Not.Null);
        }

        bool Create(GameObject prefab, CharacterAnimationProfile source, out CharacterAnimationProfile created,
            out string path, out bool uncertain, out string error)
        {
            return AnimationEventToolProfileOwnership.TryCreateDedicated(prefab, source, out created, out path,
                out uncertain, out error, _assets.Folder);
        }
    }
}
#endif
