using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RaggedBanner.Animations.Tests
{
    [Category("ProjectContent")]
    public sealed class CharacterAnimationPlayerHumanoidTests
    {
        const string _animationRoot = "Assets/Art/Character/Animation/KevinIglesias/Human Animations/Animations/Male/";

        readonly List<Object> _objects = new List<Object>();
        CharacterAnimationPlayer _player;
        Animator _animator;
        CharacterAnimationProfile _profile;

        [TearDown]
        public void TearDown()
        {
            _player?.Dispose();

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
        }

        [TestCase("Warrior")]
        [TestCase("Archer")]
        [TestCase("Rogue")]
        public void CombatPrefabSamplesHumanoidAttackAndHoldsEndWithoutRootMotion(string prefabName)
        {
            CreateFixture(prefabName);
            RuntimeAnimatorController controller = _animator.runtimeAnimatorController;
            Assert.That(_animator.avatar != null && _animator.avatar.isValid && _animator.avatar.isHuman, Is.True);
            Assert.That(_player.TryInitialize(_animator, _profile, out string error), Is.True, error);
            Assert.That(_animator.runtimeAnimatorController, Is.Null);
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);
            Quaternion[] initialPose = CapturePose();
            Vector3 rootPosition = _animator.transform.position;
            Quaternion rootRotation = _animator.transform.rotation;
            Assert.That(_profile.TryGetAnimation("attack1", out CharacterAnimationEntry attack), Is.True);
            float length = attack.Clip.length;

            Assert.That(_player.TryAdvance(length * 0.5f), Is.True);

            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Playing));
            Assert.That(MaximumPoseDifference(initialPose, CapturePose()), Is.GreaterThan(1f));
            Assert.That(_player.TryAdvance(length * 2f), Is.True);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Completed));
            Quaternion[] finalPose = CapturePose();
            Assert.That(_player.TryAdvance(length * 2f), Is.True);
            Assert.That(MaximumPoseDifference(finalPose, CapturePose()), Is.LessThan(0.1f));
            Assert.That(Vector3.Distance(_animator.transform.position, rootPosition), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(_animator.transform.rotation, rootRotation), Is.LessThan(0.1f));
            Assert.That(_player.TryPlayIdle(), Is.True);
            Assert.That(_player.TryAdvance(0.1f), Is.True);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Completed));
            _player.Dispose();
            Assert.That(_animator.runtimeAnimatorController, Is.SameAs(controller));
            Assert.That(_player.TryInitialize(_animator, _profile, out error), Is.True, error);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LostHumanoidAvatarInvalidatesAttackBeforeFurtherSampling(bool destroyAvatar)
        {
            CreateFixture("Warrior");
            Avatar avatar = Object.Instantiate(_animator.avatar);
            avatar.hideFlags = HideFlags.HideAndDontSave;
            _objects.Add(avatar);
            _animator.avatar = avatar;
            Assert.That(_player.TryInitialize(_animator, _profile, out string error), Is.True, error);
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);

            if (destroyAvatar)
            {
                Object.DestroyImmediate(avatar);
            }
            else
            {
                _animator.avatar = null;
            }

            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Invalid));
            Assert.That(_player.TryAdvance(0.1f), Is.False);
            _player.Dispose();
        }

        [Test]
        public void HumanoidClipsRequireAvatarBeforeTakingAnimatorOwnership()
        {
            CreateFixture("Warrior");
            RuntimeAnimatorController controller = _animator.runtimeAnimatorController;
            _animator.avatar = null;

            Assert.That(_player.TryInitialize(_animator, _profile, out string error), Is.False);

            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(_player.IsReady, Is.False);
            Assert.That(_animator.runtimeAnimatorController, Is.SameAs(controller));
        }

        void CreateFixture(string prefabName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/" + prefabName + ".prefab");
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            _objects.Add(instance);
            MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                behaviours[i].enabled = false;
            }

            _animator = instance.GetComponent<Animator>();
            Assert.That(_animator, Is.Not.Null);
            _profile = ScriptableObject.CreateInstance<CharacterAnimationProfile>();
            _profile.hideFlags = HideFlags.HideAndDontSave;
            _objects.Add(_profile);
            CharacterAnimationEntry[] entries =
            {
                CreateEntry("idle", "Combat/2H/HumanM@CombatIdle2H01.fbx"),
                CreateEntry("move", "Movement/Run/HumanM@Run01_Forward.fbx"),
                CreateEntry("attack1", "Combat/2H/HumanM@Attack2H01.fbx")
            };
            typeof(CharacterAnimationProfile).GetField("_animations", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_profile, entries);
            _player = new CharacterAnimationPlayer();
        }

        static CharacterAnimationEntry CreateEntry(string key, string relativePath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(_animationRoot + relativePath);
            AnimationClip clip = null;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip candidate && !candidate.name.StartsWith("__preview__"))
                {
                    Assert.That(clip, Is.Null);
                    clip = candidate;
                }
            }

            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.humanMotion, Is.True);
            AnimationEventKey[] events = key == "attack1"
                ? new[] { new AnimationEventKey(17, AnimationEventKind.AttackHit, AnimationAttackKind.Melee) }
                : Array.Empty<AnimationEventKey>();
            return new CharacterAnimationEntry(key, clip, clip.frameRate, clip.length, events);
        }

        Quaternion[] CapturePose()
        {
            Quaternion[] pose = new Quaternion[(int)HumanBodyBones.LastBone];

            for (int i = 0; i < pose.Length; i++)
            {
                Transform bone = _animator.GetBoneTransform((HumanBodyBones)i);
                pose[i] = bone != null ? bone.localRotation : Quaternion.identity;
                Assert.That(float.IsNaN(pose[i].x) || float.IsInfinity(pose[i].x), Is.False);
                Assert.That(float.IsNaN(pose[i].y) || float.IsInfinity(pose[i].y), Is.False);
                Assert.That(float.IsNaN(pose[i].z) || float.IsInfinity(pose[i].z), Is.False);
                Assert.That(float.IsNaN(pose[i].w) || float.IsInfinity(pose[i].w), Is.False);
            }

            return pose;
        }

        static float MaximumPoseDifference(Quaternion[] first, Quaternion[] second)
        {
            float maximum = 0f;

            for (int i = 0; i < first.Length; i++)
            {
                maximum = Mathf.Max(maximum, Quaternion.Angle(first[i], second[i]));
            }

            return maximum;
        }
    }
}
