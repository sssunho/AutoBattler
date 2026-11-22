using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace RaggedBanner.Animations
{
    public sealed class CharacterAnimationPlayer : IDisposable
    {
        const int _idle = 0;
        const int _move = 1;
        const int _attack = 2;
        const int _roleCount = 3;
        const double _blendDuration = 0.1d;

        static readonly HashSet<Animator> _boundAnimators = new HashSet<Animator>();

        readonly AnimationClip[] _clips = new AnimationClip[_roleCount];
        readonly AnimationClipPlayable[] _playables = new AnimationClipPlayable[_roleCount];
        readonly double[] _times = new double[_roleCount];
        readonly float[] _lengths = new float[_roleCount];
        readonly float[] _frameRates = new float[_roleCount];
        readonly float[] _weights = new float[_roleCount];
        readonly float[] _blendStartWeights = new float[_roleCount];

        Animator _animator;
        Avatar _avatar;
        CharacterAnimationProfile _profile;
        RuntimeAnimatorController _previousController;
        AnimatorCullingMode _previousCullingMode;
        bool _previousApplyRootMotion;
        bool _previousFireEvents;
        bool _ownsAnimatorSettings;
        bool _hadAvatar;
        bool _requiresHumanAvatar;
        PlayableGraph _graph;
        AnimationMixerPlayable _mixer;
        int _currentRole;
        double _blendElapsed;
        bool _isBlending;
        ulong _requestSequence;
        ulong _attackRequestId;
        AnimationPlaybackStatus _attackStatus;
        double _attackHitTime;
        AnimationAttackKind _attackKind;
        bool _attackHitReached;
        bool _attackHitConsumed;

        public bool IsReady => HasValidContext();

        public bool TryInitialize(Animator animator, CharacterAnimationProfile profile, out string error)
        {
            Dispose();

            if (animator == null || !animator.isActiveAndEnabled)
            {
                error = "An active Animator is required.";
                return false;
            }

            if (!IsPositiveFinite(animator.speed))
            {
                error = "Animator speed must be positive and finite.";
                return false;
            }

            if (_boundAnimators.Contains(animator))
            {
                error = "The Animator is already controlled by a CharacterAnimationPlayer.";
                return false;
            }

            if (profile == null)
            {
                error = "A character animation profile is required.";
                return false;
            }

            if (!profile.ValidateForPlayback(out error))
            {
                return false;
            }

            profile.TryGetAnimation("idle", out CharacterAnimationEntry idle);
            profile.TryGetAnimation("move", out CharacterAnimationEntry move);
            profile.TryGetAnimation("attack1", out CharacterAnimationEntry attack);
            bool requiresHumanAvatar = idle.Clip.humanMotion || move.Clip.humanMotion || attack.Clip.humanMotion;

            if (requiresHumanAvatar && (animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman))
            {
                error = "Humanoid clips require a valid Humanoid Avatar.";
                return false;
            }

            _animator = animator;
            _avatar = animator.avatar;
            _hadAvatar = _avatar != null;
            _requiresHumanAvatar = requiresHumanAvatar;
            _profile = profile;
            _clips[_idle] = idle.Clip;
            _clips[_move] = move.Clip;
            _clips[_attack] = attack.Clip;
            _attackHitTime = Math.Min((double)attack.Events[0].Frame / attack.AuthoredFrameRate, attack.Clip.length);
            _attackKind = attack.Events[0].AttackKind;
            _previousController = animator.runtimeAnimatorController;
            _previousApplyRootMotion = animator.applyRootMotion;
            _previousFireEvents = animator.fireEvents;
            _previousCullingMode = animator.cullingMode;
            _ownsAnimatorSettings = true;
            _boundAnimators.Add(animator);

            try
            {
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                animator.fireEvents = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _graph = PlayableGraph.Create("CharacterAnimationPlayer");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                _mixer = AnimationMixerPlayable.Create(_graph, _roleCount);

                for (int i = 0; i < _roleCount; i++)
                {
                    AnimationClip clip = _clips[i];
                    _lengths[i] = clip.length;
                    _frameRates[i] = clip.frameRate;
                    _times[i] = 0d;
                    _weights[i] = i == _idle ? 1f : 0f;
                    _playables[i] = AnimationClipPlayable.Create(_graph, clip);
                    _playables[i].SetApplyFootIK(false);
                    _playables[i].SetApplyPlayableIK(false);
                    _playables[i].SetSpeed(0d);
                    _graph.Connect(_playables[i], 0, _mixer, i);
                }

                AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
                output.SetSourcePlayable(_mixer);
                _currentRole = _idle;
                _isBlending = false;
                _blendElapsed = 0d;
                _attackRequestId = 0;
                _attackStatus = AnimationPlaybackStatus.Invalid;
                _graph.Play();
                ApplyPose();
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                Invalidate();
                error = "Could not initialize animation playback: " + exception.Message;
                return false;
            }
        }

        public bool TryPlayIdle()
        {
            return TryPlayLoop(_idle);
        }

        public bool TryPlayMove()
        {
            return TryPlayLoop(_move);
        }

        public bool TryPlayAttack(out ulong requestId)
        {
            requestId = 0;

            if (!EnsureReady())
            {
                return false;
            }

            _requestSequence = unchecked(_requestSequence + 1);

            if (_requestSequence == 0)
            {
                _requestSequence = 1;
            }

            _attackRequestId = _requestSequence;
            _attackStatus = AnimationPlaybackStatus.Playing;
            _attackHitReached = false;
            _attackHitConsumed = false;
            _currentRole = _attack;
            _times[_attack] = 0d;
            _isBlending = false;

            for (int i = 0; i < _roleCount; i++)
            {
                _weights[i] = i == _attack ? 1f : 0f;
            }

            if (!TryApplyPose())
            {
                return false;
            }

            requestId = _attackRequestId;
            _attackHitReached = _attackHitTime <= 0d;
            return true;
        }

        public bool TryAdvance(float deltaTime)
        {
            if (!EnsureReady())
            {
                return false;
            }

            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                Invalidate();
                return false;
            }

            double elapsed = (double)deltaTime * _animator.speed;

            for (int i = 0; i < _roleCount; i++)
            {
                if (_weights[i] <= 0f && i != _currentRole)
                {
                    continue;
                }

                double time = _times[i] + elapsed;
                _times[i] = i == _attack ? Math.Min(time, _lengths[i]) : time % _lengths[i];
            }

            if (_isBlending)
            {
                _blendElapsed = Math.Min(_blendElapsed + elapsed, _blendDuration);
                float blend = (float)(_blendElapsed / _blendDuration);

                for (int i = 0; i < _roleCount; i++)
                {
                    _weights[i] = Mathf.Lerp(_blendStartWeights[i], i == _currentRole ? 1f : 0f, blend);
                }

                _isBlending = _blendElapsed < _blendDuration;
            }

            if (!TryApplyPose())
            {
                return false;
            }

            if (_attackStatus == AnimationPlaybackStatus.Playing && _times[_attack] >= _attackHitTime)
            {
                _attackHitReached = true;
            }

            if (_attackStatus == AnimationPlaybackStatus.Playing && _times[_attack] >= _lengths[_attack])
            {
                _attackStatus = AnimationPlaybackStatus.Completed;
            }

            return true;
        }

        public bool TryConsumeAttackHit(ulong requestId, out AnimationAttackKind attackKind)
        {
            attackKind = default;
            AnimationPlaybackStatus status = GetAttackStatus(requestId);
            if (requestId != _attackRequestId || !_attackHitReached || _attackHitConsumed
                || status != AnimationPlaybackStatus.Playing && status != AnimationPlaybackStatus.Completed)
            {
                return false;
            }

            _attackHitConsumed = true;
            attackKind = _attackKind;
            return true;
        }

        public AnimationPlaybackStatus GetAttackStatus(ulong requestId)
        {
            if (requestId == 0 || requestId > _requestSequence)
            {
                return AnimationPlaybackStatus.Invalid;
            }

            if (requestId != _attackRequestId)
            {
                return AnimationPlaybackStatus.Interrupted;
            }

            if (_attackStatus == AnimationPlaybackStatus.Playing && !HasValidContext())
            {
                Invalidate();
            }

            return _attackStatus;
        }

        public void Dispose()
        {
            _attackHitReached = false;
            _attackHitConsumed = false;
            if (_attackStatus == AnimationPlaybackStatus.Playing)
            {
                _attackStatus = AnimationPlaybackStatus.Interrupted;
            }

            if (_graph.IsValid())
            {
                _graph.Destroy();
            }

            if (_ownsAnimatorSettings && !ReferenceEquals(_animator, null))
            {
                _boundAnimators.Remove(_animator);
            }

            if (_ownsAnimatorSettings && _animator != null)
            {
                if (_animator.runtimeAnimatorController == null)
                {
                    _animator.runtimeAnimatorController = _previousController;
                }

                if (!_animator.applyRootMotion)
                {
                    _animator.applyRootMotion = _previousApplyRootMotion;
                }

                if (!_animator.fireEvents)
                {
                    _animator.fireEvents = _previousFireEvents;
                }

                if (_animator.cullingMode == AnimatorCullingMode.AlwaysAnimate)
                {
                    _animator.cullingMode = _previousCullingMode;
                }
            }

            _ownsAnimatorSettings = false;
            _hadAvatar = false;
            _requiresHumanAvatar = false;
            _animator = null;
            _avatar = null;
            _profile = null;
            _previousController = null;
            _graph = default;
            _mixer = default;
            _isBlending = false;

            for (int i = 0; i < _roleCount; i++)
            {
                _clips[i] = null;
                _playables[i] = default;
                _times[i] = 0d;
                _weights[i] = 0f;
                _blendStartWeights[i] = 0f;
            }
        }

        bool TryPlayLoop(int role)
        {
            if (!EnsureReady())
            {
                return false;
            }

            if (_currentRole == role)
            {
                return true;
            }

            _attackHitReached = false;
            if (_attackStatus == AnimationPlaybackStatus.Playing)
            {
                _attackStatus = AnimationPlaybackStatus.Interrupted;
            }

            if (_weights[role] <= 0f)
            {
                _times[role] = 0d;
            }

            for (int i = 0; i < _roleCount; i++)
            {
                _blendStartWeights[i] = _weights[i];
            }

            _currentRole = role;
            _blendElapsed = 0d;
            _isBlending = true;
            return TryApplyPose();
        }

        bool EnsureReady()
        {
            if (HasValidContext())
            {
                return true;
            }

            Invalidate();
            return false;
        }

        bool HasValidContext()
        {
            if (!_ownsAnimatorSettings || !_graph.IsValid() || _animator == null || !_animator.isActiveAndEnabled
                || _profile == null || _animator.avatar != _avatar || !IsPositiveFinite(_animator.speed)
                || _animator.runtimeAnimatorController != null || _animator.applyRootMotion || _animator.fireEvents
                || _animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                return false;
            }

            if ((_hadAvatar && _avatar == null)
                || (_requiresHumanAvatar && (_avatar == null || !_avatar.isValid || !_avatar.isHuman)))
            {
                return false;
            }

            for (int i = 0; i < _roleCount; i++)
            {
                if (_clips[i] == null || !_playables[i].IsValid()
                    || _clips[i].legacy || _clips[i].length != _lengths[i] || _clips[i].frameRate != _frameRates[i])
                {
                    return false;
                }
            }

            return !_clips[_attack].isLooping;
        }

        bool TryApplyPose()
        {
            try
            {
                ApplyPose();
                return true;
            }
            catch (Exception)
            {
                Invalidate();
                return false;
            }
        }

        void ApplyPose()
        {
            for (int i = 0; i < _roleCount; i++)
            {
                _playables[i].SetTime(_times[i]);
                _mixer.SetInputWeight(i, _weights[i]);
            }

            _graph.Evaluate(0f);
        }

        void Invalidate()
        {
            if (_attackStatus == AnimationPlaybackStatus.Playing)
            {
                _attackStatus = AnimationPlaybackStatus.Invalid;
            }

            Dispose();
        }

        static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
