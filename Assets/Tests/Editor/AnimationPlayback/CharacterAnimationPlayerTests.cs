using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RaggedBanner.Animations.Tests
{
    public sealed class CharacterAnimationPlayerTests
    {
        readonly List<Object> _objects = new List<Object>();
        readonly List<CharacterAnimationPlayer> _players = new List<CharacterAnimationPlayer>();
        CharacterAnimationPlayer _player;
        Animator _animator;
        Transform _animated;
        CharacterAnimationEntry _idle;
        CharacterAnimationEntry _move;
        CharacterAnimationEntry _attack;
        CharacterAnimationProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _animator = CreateAnimator(out _animated);
            _idle = CreateEntry("idle", CreateClip(1f, 2f));
            _move = CreateEntry("move", CreateClip(10f, 20f));
            _attack = CreateEntry("attack1", CreateClip(30f, 40f));
            _profile = CreateProfile(_idle, _move, _attack);
            _player = CreatePlayer();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _players.Count - 1; i >= 0; i--)
            {
                _players[i].Dispose();
            }

            _players.Clear();

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
        public void UninitializedPlayerRejectsCommandsAndDisposesRepeatedly()
        {
            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.TryPlayIdle(), Is.False);
            Assert.That(_player.TryPlayMove(), Is.False);
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.False);
            Assert.That(requestId, Is.Zero);
            Assert.That(_player.TryAdvance(0.1f), Is.False);
            Assert.That(_player.GetAttackStatus(0), Is.EqualTo(AnimationPlaybackStatus.Invalid));
            _player.Dispose();
            _player.Dispose();
        }

        [Test]
        public void InitializationWithoutControllerEvaluatesIdleImmediately()
        {
            Assert.That(_animator.runtimeAnimatorController, Is.Null);
            _animated.localScale = new Vector3(7f, 1f, 1f);

            Initialize();

            Assert.That(_player.IsReady, Is.True);
            Assert.That(_animator.runtimeAnimatorController, Is.Null);
            Assert.That(_animator.applyRootMotion, Is.False);
            Assert.That(_animator.fireEvents, Is.False);
            Assert.That(_animator.cullingMode, Is.EqualTo(AnimatorCullingMode.AlwaysAnimate));
            AssertPose(1f);
        }

        [Test]
        public void AttackStartsAtZeroCompletesAtEndAndKeepsCompletionAndEndPose()
        {
            Initialize();
            Assert.That(_player.TryAdvance(0.4f), Is.True);

            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);

            Assert.That(requestId, Is.Not.Zero);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Playing));
            AssertPose(30f);
            Assert.That(_player.TryAdvance(0.5f), Is.True);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Playing));
            AssertPose(35f);
            Assert.That(_player.TryAdvance(0.5f), Is.True);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Completed));
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Completed));
            AssertPose(40f);
            Assert.That(_player.TryAdvance(100f), Is.True);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Completed));
            AssertPose(40f);
        }

        [Test]
        public void LargeDeltaClampsAttackToLastPose()
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);

            Assert.That(_player.TryAdvance(float.MaxValue), Is.True);

            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Completed));
            AssertPose(40f);
        }

        [Test]
        public void ZeroDeltaFreezesAttackAndBlend()
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);
            Assert.That(_player.TryAdvance(0.25f), Is.True);
            Assert.That(_player.TryAdvance(0f), Is.True);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Playing));
            AssertPose(32.5f);
            Assert.That(_player.TryPlayIdle(), Is.True);
            float before = _animated.localScale.x;

            Assert.That(_player.TryAdvance(0f), Is.True);

            AssertPose(before);
        }

        [Test]
        public void AnimatorSpeedIsAppliedExactlyOnceToAttackAndBlend()
        {
            _animator.speed = 2f;
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);

            Assert.That(_player.TryAdvance(0.25f), Is.True);

            AssertPose(35f);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Playing));
            Assert.That(_player.TryAdvance(0.25f), Is.True);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Completed));
            Assert.That(_player.TryPlayIdle(), Is.True);
            Assert.That(_player.TryAdvance(0.025f), Is.True);
            AssertPose((40f + 1.05f) * 0.5f);
            Assert.That(_player.TryAdvance(0.025f), Is.True);
            AssertPose(1.1f);
            Assert.That(_animator.speed, Is.EqualTo(2f));
        }

        [Test]
        public void AttackRestartGetsFreshRequestAndResetsClockAndPose()
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong first), Is.True);
            Assert.That(_player.TryAdvance(0.75f), Is.True);

            Assert.That(_player.TryPlayAttack(out ulong second), Is.True);

            Assert.That(second, Is.Not.Zero.And.Not.EqualTo(first));
            Assert.That(_player.GetAttackStatus(first), Is.EqualTo(AnimationPlaybackStatus.Interrupted));
            Assert.That(_player.GetAttackStatus(second), Is.EqualTo(AnimationPlaybackStatus.Playing));
            Assert.That(_player.GetAttackStatus(0), Is.EqualTo(AnimationPlaybackStatus.Invalid));
            AssertPose(30f);
            Assert.That(_player.TryAdvance(0.5f), Is.True);
            Assert.That(_player.GetAttackStatus(second), Is.EqualTo(AnimationPlaybackStatus.Playing));
            AssertPose(35f);
        }

        [Test]
        public void NewAttackInvalidatesPreviousCompletion()
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong first), Is.True);
            Assert.That(_player.TryAdvance(1f), Is.True);

            Assert.That(_player.TryPlayAttack(out ulong second), Is.True);

            Assert.That(_player.GetAttackStatus(first), Is.EqualTo(AnimationPlaybackStatus.Interrupted));
            Assert.That(_player.GetAttackStatus(second), Is.EqualTo(AnimationPlaybackStatus.Playing));
            AssertPose(30f);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IdleOrMoveInterruptsPlayingAttack(bool move)
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);
            Assert.That(_player.TryAdvance(0.25f), Is.True);

            Assert.That(move ? _player.TryPlayMove() : _player.TryPlayIdle(), Is.True);

            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Interrupted));
            Assert.That(_player.TryAdvance(2f), Is.True);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Interrupted));
        }

        [Test]
        public void ReturningToIdlePreservesCompletedRequestAndBlendsFromAttackEnd()
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);
            Assert.That(_player.TryAdvance(1f), Is.True);

            Assert.That(_player.TryPlayIdle(), Is.True);

            AssertPose(40f);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Completed));
            Assert.That(_player.TryAdvance(0.05f), Is.True);
            AssertPose((40f + 1.05f) * 0.5f);
            Assert.That(_player.TryAdvance(0.05f), Is.True);
            AssertPose(1.1f);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Completed));
        }

        [Test]
        public void IdleAndMoveLoopWithoutDependingOnClipLoopSettings()
        {
            Initialize();
            Assert.That(_player.TryAdvance(1.25f), Is.True);
            AssertPose(1.25f);

            Assert.That(_player.TryPlayMove(), Is.True);
            Assert.That(_player.TryAdvance(1.25f), Is.True);

            AssertPose(12.5f);
            Assert.That(_player.TryAdvance(1f), Is.True);
            AssertPose(12.5f);
        }

        [Test]
        public void RepeatedRoleRequestDoesNotRestartLocalTimeOrBlend()
        {
            Initialize();
            Assert.That(_player.TryAdvance(0.25f), Is.True);
            Assert.That(_player.TryPlayIdle(), Is.True);
            Assert.That(_player.TryAdvance(0.25f), Is.True);
            AssertPose(1.5f);
            Assert.That(_player.TryPlayMove(), Is.True);
            Assert.That(_player.TryAdvance(0.05f), Is.True);
            AssertPose((1.55f + 10.5f) * 0.5f);

            Assert.That(_player.TryPlayMove(), Is.True);
            Assert.That(_player.TryAdvance(0.05f), Is.True);

            AssertPose(11f);
            Assert.That(_player.TryPlayMove(), Is.True);
            Assert.That(_player.TryAdvance(0.15f), Is.True);
            AssertPose(12.5f);
        }

        [Test]
        public void SwitchingBackDuringBlendPreservesPoseAndResumesActiveIdleTime()
        {
            Initialize();
            Assert.That(_player.TryAdvance(0.2f), Is.True);
            Assert.That(_player.TryPlayMove(), Is.True);
            Assert.That(_player.TryAdvance(0.05f), Is.True);
            AssertPose((1.25f + 10.5f) * 0.5f);
            float before = _animated.localScale.x;

            Assert.That(_player.TryPlayIdle(), Is.True);

            AssertPose(before);
            Assert.That(_player.TryAdvance(0.05f), Is.True);
            AssertPose(1.3f * 0.75f + 11f * 0.25f);
            Assert.That(_player.TryAdvance(0.05f), Is.True);
            AssertPose(1.35f);
        }

        [Test]
        public void RoleWithZeroWeightRestartsAtZeroWhenSelectedAgain()
        {
            Initialize();
            Assert.That(_player.TryAdvance(0.6f), Is.True);
            Assert.That(_player.TryPlayMove(), Is.True);
            Assert.That(_player.TryAdvance(0.2f), Is.True);

            Assert.That(_player.TryPlayIdle(), Is.True);
            Assert.That(_player.TryAdvance(0.1f), Is.True);

            AssertPose(1.1f);
        }

        [Test]
        public void NewBlendRetainsAllThreeCurrentWeights()
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out _), Is.True);
            Assert.That(_player.TryAdvance(1f), Is.True);
            Assert.That(_player.TryPlayIdle(), Is.True);
            Assert.That(_player.TryAdvance(0.05f), Is.True);
            float before = _animated.localScale.x;

            Assert.That(_player.TryPlayMove(), Is.True);

            AssertPose(before);
            Assert.That(_player.TryAdvance(0.05f), Is.True);
            AssertPose(1.1f * 0.25f + 10.5f * 0.5f + 40f * 0.25f);
            Assert.That(_player.TryAdvance(0.05f), Is.True);
            AssertPose(11f);
        }

        [Test]
        public void DisposeRestoresOwnedAnimatorSettingsAndKeepsBorrowedSources()
        {
            AnimatorController controller = CreateController();
            _animator.runtimeAnimatorController = controller;
            _animator.applyRootMotion = true;
            _animator.fireEvents = true;
            _animator.cullingMode = AnimatorCullingMode.CullCompletely;
            _animator.speed = 1.5f;
            Initialize();
            Assert.That(_animator.runtimeAnimatorController, Is.Null);
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);

            _player.Dispose();
            _player.Dispose();

            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Interrupted));
            AssertAnotherPlayerCanBind();
            Assert.That(_animator.runtimeAnimatorController, Is.SameAs(controller));
            Assert.That(_animator.applyRootMotion, Is.True);
            Assert.That(_animator.fireEvents, Is.True);
            Assert.That(_animator.cullingMode, Is.EqualTo(AnimatorCullingMode.CullCompletely));
            Assert.That(_animator.speed, Is.EqualTo(1.5f));
            Assert.That(_profile != null, Is.True);
            Assert.That(_idle != null && _move != null && _attack != null, Is.True);
            Assert.That(_idle.Clip != null && _move.Clip != null && _attack.Clip != null, Is.True);
            Assert.That(_profile.Validate(out string error), Is.True, error);
        }

        [Test]
        public void DisposePreservesAnimatorSettingsChangedByOtherOwner()
        {
            _animator.applyRootMotion = false;
            _animator.fireEvents = false;
            _animator.cullingMode = AnimatorCullingMode.CullCompletely;
            Initialize();
            AnimatorController replacement = CreateController();
            _animator.runtimeAnimatorController = replacement;
            _animator.applyRootMotion = true;
            _animator.fireEvents = true;
            _animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            _player.Dispose();

            Assert.That(_animator.runtimeAnimatorController, Is.SameAs(replacement));
            Assert.That(_animator.applyRootMotion, Is.True);
            Assert.That(_animator.fireEvents, Is.True);
            Assert.That(_animator.cullingMode, Is.EqualTo(AnimatorCullingMode.CullUpdateTransforms));
        }

        [Test]
        public void FailedInitializationLeavesTargetSettingsUntouched()
        {
            AnimatorController controller = CreateController();
            _animator.runtimeAnimatorController = controller;
            _animator.applyRootMotion = true;
            _animator.fireEvents = true;
            _animator.cullingMode = AnimatorCullingMode.CullCompletely;
            CharacterAnimationProfile invalid = CreateProfile(_idle, null, _attack);

            Assert.That(_player.TryInitialize(_animator, invalid, out string error), Is.False);

            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(_player.IsReady, Is.False);
            AssertAnotherPlayerCanBind();
            Assert.That(_animator.runtimeAnimatorController, Is.SameAs(controller));
            Assert.That(_animator.applyRootMotion, Is.True);
            Assert.That(_animator.fireEvents, Is.True);
            Assert.That(_animator.cullingMode, Is.EqualTo(AnimatorCullingMode.CullCompletely));
            Initialize();
            AssertPose(1f);
        }

        [Test]
        public void FailedReinitializationDisposesPreviousSessionBeforeValidatingNewTarget()
        {
            _animator.fireEvents = true;
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);

            Assert.That(_player.TryInitialize(null, _profile, out string error), Is.False);

            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Interrupted));
            AssertAnotherPlayerCanBind();
            Assert.That(_animator.fireEvents, Is.True);
            Assert.That(_player.TryAdvance(1f), Is.False);
            Initialize();
            AssertPose(1f);
        }

        [Test]
        public void ReinitializationRestartsIdleAndNeverReusesOldRequestId()
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong first), Is.True);
            Assert.That(_player.TryAdvance(1f), Is.True);

            Initialize();

            AssertPose(1f);
            Assert.That(_player.TryPlayAttack(out ulong second), Is.True);
            Assert.That(second, Is.Not.Zero.And.Not.EqualTo(first));
            Assert.That(_player.GetAttackStatus(first), Is.EqualTo(AnimationPlaybackStatus.Interrupted));
            Assert.That(_player.GetAttackStatus(second), Is.EqualTo(AnimationPlaybackStatus.Playing));
        }

        [Test]
        public void ProfileReferenceChangesTakeEffectOnlyAfterReinitialization()
        {
            Initialize();
            CharacterAnimationEntry replacement = CreateEntry("attack1", CreateClip(50f, 60f));
            SetAnimations(_profile, new[] { _idle, _move, replacement });

            Assert.That(_player.IsReady, Is.True);
            Assert.That(_player.TryPlayAttack(out _), Is.True);

            AssertPose(30f);
            Assert.That(_player.TryAdvance(0.5f), Is.True);
            AssertPose(35f);
            Initialize();
            Assert.That(_player.TryPlayAttack(out _), Is.True);
            AssertPose(50f);
        }

        [Test]
        public void GameplayBindsReservedKeysRegardlessOfListOrderOrAdditionalEntries()
        {
            CharacterAnimationEntry skill = CreateEntry("skill1", CreateClip(70f, 80f));
            SetAnimations(_profile, new[] { skill, _attack, _move, _idle });

            Initialize();

            AssertPose(1f);
            Assert.That(_player.TryPlayMove(), Is.True);
            Assert.That(_player.TryAdvance(0.1f), Is.True);
            AssertPose(11f);
            Assert.That(_player.TryPlayAttack(out _), Is.True);
            AssertPose(30f);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidAnimatorSpeedInvalidatesActiveAttackAndRequiresReinitialization(float speed)
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);
            _animator.speed = speed;

            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.TryAdvance(0.1f), Is.False);

            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Invalid));
            _animator.speed = 1f;
            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.TryPlayAttack(out ulong rejected), Is.False);
            Assert.That(rejected, Is.Zero);
            Initialize();
            AssertPose(1f);
        }

        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidDeltaInvalidatesActiveAttackUntilReinitialized(float delta)
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);

            Assert.That(_player.TryAdvance(delta), Is.False);

            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Invalid));
            Assert.That(_player.TryAdvance(0.1f), Is.False);
            Initialize();
            AssertPose(1f);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisabledAnimatorOrObjectInvalidatesAttackAndAllowsReinitialization(bool disableObject)
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);

            if (disableObject)
            {
                _animator.gameObject.SetActive(false);
            }
            else
            {
                _animator.enabled = false;
            }

            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.TryAdvance(0.1f), Is.False);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Invalid));
            _animator.gameObject.SetActive(true);
            _animator.enabled = true;
            Assert.That(_player.IsReady, Is.False);
            Initialize();
            AssertPose(1f);
        }

        [Test]
        public void ExternalControllerTakeoverInvalidatesAttackWithoutOverwritingController()
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);
            AnimatorController replacement = CreateController();
            _animator.runtimeAnimatorController = replacement;

            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.TryPlayIdle(), Is.False);

            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Invalid));
            Assert.That(_animator.runtimeAnimatorController, Is.SameAs(replacement));
            Initialize();
            Assert.That(_animator.runtimeAnimatorController, Is.Null);
            _player.Dispose();
            Assert.That(_animator.runtimeAnimatorController, Is.SameAs(replacement));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ExternallyChangedAnimatorFlagsInvalidateAttackAndRemainAfterCleanup(int changedSetting)
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);

            switch (changedSetting)
            {
                case 0:
                    _animator.applyRootMotion = true;
                    break;
                case 1:
                    _animator.fireEvents = true;
                    break;
                default:
                    _animator.cullingMode = AnimatorCullingMode.CullCompletely;
                    break;
            }

            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Invalid));
            AssertAnotherPlayerCanBind();

            switch (changedSetting)
            {
                case 0:
                    Assert.That(_animator.applyRootMotion, Is.True);
                    break;
                case 1:
                    Assert.That(_animator.fireEvents, Is.True);
                    break;
                default:
                    Assert.That(_animator.cullingMode, Is.EqualTo(AnimatorCullingMode.CullCompletely));
                    break;
            }
        }

        [Test]
        public void ChangedClipMetadataInvalidatesActiveAttack()
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);
            _attack.Clip.frameRate = 60f;

            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.TryAdvance(0.1f), Is.False);

            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Invalid));
            _attack.Clip.frameRate = 30f;
            Assert.That(_player.IsReady, Is.False);
            AssertAnotherPlayerCanBind();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void DestroyedBorrowedSourceOrAnimatorInvalidatesActiveAttack(int destroyedSource)
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);

            switch (destroyedSource)
            {
                case 0:
                    Object.DestroyImmediate(_attack.Clip);
                    break;
                case 1:
                    Object.DestroyImmediate(_idle.Clip);
                    break;
                case 2:
                    Object.DestroyImmediate(_profile);
                    break;
                default:
                    Object.DestroyImmediate(_animator);
                    break;
            }

            Assert.That(_player.IsReady, Is.False);
            Assert.That(_player.TryAdvance(0.1f), Is.False);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Invalid));
            _player.Dispose();
            _player.Dispose();
        }

        [Test]
        public void SecondPlayerCannotTakeAnimatorOwnedByAnotherPlayer()
        {
            Initialize();
            Assert.That(_player.TryPlayAttack(out ulong requestId), Is.True);
            CharacterAnimationPlayer other = CreatePlayer();

            Assert.That(other.TryInitialize(_animator, _profile, out string error), Is.False);

            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(other.IsReady, Is.False);
            Assert.That(_player.IsReady, Is.True);
            Assert.That(_player.TryAdvance(0.5f), Is.True);
            Assert.That(_player.GetAttackStatus(requestId), Is.EqualTo(AnimationPlaybackStatus.Playing));
            AssertPose(35f);
        }

        [Test]
        public void FireEventsRemainsDisabledDuringManualPlaybackAndRestoresOnDispose()
        {
            _animator.fireEvents = true;
            Initialize();

            Assert.That(_player.TryPlayAttack(out _), Is.True);
            Assert.That(_player.TryAdvance(0.75f), Is.True);

            Assert.That(_animator.fireEvents, Is.False);
            _player.Dispose();
            Assert.That(_animator.fireEvents, Is.True);
        }

        void Initialize()
        {
            Assert.That(_player.TryInitialize(_animator, _profile, out string error), Is.True, error);
            Assert.That(_player.IsReady, Is.True);
        }

        void AssertAnotherPlayerCanBind()
        {
            CharacterAnimationPlayer other = CreatePlayer();
            Assert.That(other.TryInitialize(_animator, _profile, out string error), Is.True, error);
            Assert.That(other.IsReady, Is.True);
            other.Dispose();
        }

        [TestCase(0)]
        [TestCase(15)]
        [TestCase(30)]
        public void AttackHitCanBeConsumedOnceEvenWhenAdvancePassesTheClipEnd(int frame)
        {
            CharacterAnimationEntry entry = new CharacterAnimationEntry("attack1", _attack.Clip, 30f, 1f,
                new[] { new AnimationEventKey(frame, AnimationEventKind.AttackHit, AnimationAttackKind.Projectile) });
            SetAnimations(_profile, new[] { _idle, _move, entry });
            Assert.That(_player.TryInitialize(_animator, _profile, out string error), Is.True, error);
            Assert.That(_player.TryPlayAttack(out ulong request), Is.True);
            Assert.That(_player.TryConsumeAttackHit(request, out _), Is.EqualTo(frame == 0));
            Assert.That(_player.TryAdvance(2f), Is.True);
            Assert.That(_player.TryConsumeAttackHit(request, out AnimationAttackKind kind), Is.EqualTo(frame != 0));
            if (frame != 0)
            {
                Assert.That(kind, Is.EqualTo(AnimationAttackKind.Projectile));
            }

            Assert.That(_player.GetAttackStatus(request), Is.EqualTo(AnimationPlaybackStatus.Completed));
            Assert.That(_player.TryConsumeAttackHit(request, out _), Is.False);
        }

        [Test]
        public void InterruptedAndPreviousRequestsCannotDeliverHits()
        {
            Assert.That(_player.TryInitialize(_animator, _profile, out string error), Is.True, error);
            Assert.That(_player.TryPlayAttack(out ulong first), Is.True);
            Assert.That(_player.TryAdvance(0.6f), Is.True);
            Assert.That(_player.TryPlayIdle(), Is.True);
            Assert.That(_player.TryConsumeAttackHit(first, out _), Is.False);
            Assert.That(_player.TryPlayAttack(out ulong second), Is.True);
            Assert.That(_player.TryConsumeAttackHit(first, out _), Is.False);
            Assert.That(_player.TryConsumeAttackHit(second, out _), Is.False);
            Assert.That(_player.TryAdvance(0.6f), Is.True);
            Assert.That(_player.TryConsumeAttackHit(second, out AnimationAttackKind kind), Is.True);
            Assert.That(kind, Is.EqualTo(AnimationAttackKind.Melee));
            _player.Dispose();
            Assert.That(_player.TryConsumeAttackHit(second, out _), Is.False);
        }

        void AssertPose(float expected)
        {
            Assert.That(_animated.localScale.x, Is.EqualTo(expected).Within(0.001f));
        }

        CharacterAnimationPlayer CreatePlayer()
        {
            CharacterAnimationPlayer player = new CharacterAnimationPlayer();
            _players.Add(player);
            return player;
        }

        Animator CreateAnimator(out Transform animated)
        {
            GameObject root = new GameObject("Animation Playback Test") { hideFlags = HideFlags.HideAndDontSave };
            _objects.Add(root);
            GameObject child = new GameObject("Animated") { hideFlags = HideFlags.HideAndDontSave };
            child.transform.SetParent(root.transform, false);
            animated = child.transform;
            return root.AddComponent<Animator>();
        }

        AnimationClip CreateClip(float from, float to)
        {
            AnimationClip clip = new AnimationClip
            {
                frameRate = 30f,
                hideFlags = HideFlags.HideAndDontSave
            };
            _objects.Add(clip);
            clip.SetCurve("Animated", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, from, 1f, to));
            return clip;
        }

        static CharacterAnimationEntry CreateEntry(string key, AnimationClip clip)
        {
            AnimationEventKey[] events = key == "attack1"
                ? new[] { new AnimationEventKey(15, AnimationEventKind.AttackHit, AnimationAttackKind.Melee) }
                : Array.Empty<AnimationEventKey>();
            return new CharacterAnimationEntry(key, clip, clip.frameRate, clip.length, events);
        }

        CharacterAnimationProfile CreateProfile(CharacterAnimationEntry idle, CharacterAnimationEntry move, CharacterAnimationEntry attack)
        {
            CharacterAnimationProfile profile = ScriptableObject.CreateInstance<CharacterAnimationProfile>();
            profile.hideFlags = HideFlags.HideAndDontSave;
            _objects.Add(profile);
            List<CharacterAnimationEntry> entries = new List<CharacterAnimationEntry>();

            if (idle != null)
            {
                entries.Add(idle);
            }

            if (move != null)
            {
                entries.Add(move);
            }

            if (attack != null)
            {
                entries.Add(attack);
            }

            SetAnimations(profile, entries.ToArray());
            return profile;
        }

        AnimatorController CreateController()
        {
            AnimatorController controller = new AnimatorController { hideFlags = HideFlags.HideAndDontSave };
            _objects.Add(controller);
            controller.AddLayer("Base Layer");
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            stateMachine.hideFlags = HideFlags.HideAndDontSave;
            _objects.Add(stateMachine);
            AnimatorState state = stateMachine.AddState("Idle");
            state.hideFlags = HideFlags.HideAndDontSave;
            _objects.Add(state);
            state.motion = _idle.Clip;
            stateMachine.defaultState = state;
            return controller;
        }

        static void SetAnimations(CharacterAnimationProfile profile, CharacterAnimationEntry[] entries)
        {
            typeof(CharacterAnimationProfile).GetField("_animations", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(profile, entries);
        }
    }
}
