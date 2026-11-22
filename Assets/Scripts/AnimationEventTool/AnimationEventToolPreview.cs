#if UNITY_EDITOR
using System;
using RaggedBanner.Animations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace RaggedBanner.AnimationTools
{
    internal sealed class AnimationEventToolPreview : IDisposable
    {
        GameObject _sourcePrefab;
        AnimationClip _sourceClip;
        Transform _previewRoot;
        GameObject _container;
        GameObject _instance;
        AnimationClip _sampleClip;
        Animator _animator;
        Avatar _avatar;
        bool _hadAvatar;
        bool _requiresHumanAvatar;
        bool _hasClip;
        Vector3 _animatorPosition;
        Quaternion _animatorRotation;
        PlayableGraph _graph;
        AnimationClipPlayable _playable;
        float _length;
        float _frameRate;
        double _time;
        int _currentFrame;
        bool _isPlaying;

        public bool IsReady => HasValidContext();
        public GameObject Instance => _instance;
        public int CurrentFrame => _currentFrame;
        public bool IsPlaying => _isPlaying;

        public bool TryCreate(GameObject prefab, AnimationClip clip, Transform previewRoot, out string error)
        {
            Dispose();

            if (prefab == null || previewRoot == null || !previewRoot.gameObject.activeInHierarchy)
            {
                error = "A prefab and an active preview root are required.";
                return false;
            }

            if (clip != null && (clip.legacy
                || !AnimationFrameUtility.TryGetLastFrame(clip.length, clip.frameRate, out _)))
            {
                error = "A non-Legacy clip with a positive finite length and frame rate is required.";
                return false;
            }

            _sourcePrefab = prefab;
            _sourceClip = clip;
            _hasClip = clip != null;
            _previewRoot = previewRoot;
            _length = clip != null ? clip.length : 0f;
            _frameRate = clip != null ? clip.frameRate : 0f;

            try
            {
                _container = new GameObject("Animation Event Preview") { hideFlags = HideFlags.HideAndDontSave };
                _container.SetActive(false);
                _container.transform.SetParent(previewRoot, false);
                _instance = Object.Instantiate(prefab, _container.transform, false);
                _instance.hideFlags = HideFlags.HideAndDontSave;
                _instance.transform.localPosition = Vector3.zero;
                _instance.transform.localRotation = Quaternion.identity;
                _animator = _instance.GetComponent<Animator>();

                if (_animator == null)
                {
                    _animator = _instance.GetComponentInChildren<Animator>(true);
                }

                if (_animator == null)
                {
                    error = "The preview prefab requires an Animator.";
                    Dispose();
                    return false;
                }

                _avatar = _animator.avatar;
                _hadAvatar = _avatar != null;
                _requiresHumanAvatar = clip != null && clip.humanMotion;

                if (_requiresHumanAvatar && (_avatar == null || !_avatar.isValid || !_avatar.isHuman))
                {
                    error = "Humanoid clips require a valid Humanoid Avatar on the preview prefab.";
                    Dispose();
                    return false;
                }

                DisableAutomaticComponents();
                _animator.runtimeAnimatorController = null;
                _animator.applyRootMotion = false;
                _animator.fireEvents = false;
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _animator.speed = 1f;
                _animator.enabled = true;
                _animatorPosition = _animator.transform.localPosition;
                _animatorRotation = _animator.transform.localRotation;

                if (_hasClip)
                {
                    _sampleClip = Object.Instantiate(clip);
                    _sampleClip.name = "Animation Event Preview Clip";
                    _sampleClip.hideFlags = HideFlags.HideAndDontSave;
                    AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(_sampleClip);
                    settings.loopTime = false;
                    settings.loopBlend = false;
                    AnimationUtility.SetAnimationClipSettings(_sampleClip, settings);
                    _sampleClip.wrapMode = WrapMode.ClampForever;
                    AnimationUtility.SetAnimationEvents(_sampleClip, Array.Empty<AnimationEvent>());
                }

                _instance.SetActive(true);
                _container.SetActive(true);

                if (!_animator.isActiveAndEnabled)
                {
                    error = "The preview Animator must be in an active hierarchy.";
                    Dispose();
                    return false;
                }

                if (!_hasClip)
                {
                    FixRootTransform();
                    error = null;
                    return true;
                }

                _graph = PlayableGraph.Create("AnimationEventToolPreview");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                _playable = AnimationClipPlayable.Create(_graph, _sampleClip);
                _playable.SetApplyFootIK(false);
                _playable.SetApplyPlayableIK(false);
                _playable.SetSpeed(0d);
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Preview", _animator);
                output.SetSourcePlayable(_playable);
                _graph.Play();
                _time = 0d;
                _currentFrame = 0;
                _isPlaying = false;
                ApplyPose();
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                Dispose();
                error = "Could not create animation preview: " + exception.Message;
                return false;
            }
        }

        public bool TrySetFrame(int frame, out string error)
        {
            if (!EnsureReady(out error))
            {
                return false;
            }

            if (!_hasClip)
            {
                error = "Select an animation before changing the preview frame.";
                return false;
            }

            if (!AnimationFrameUtility.TryGetTime(frame, _length, _frameRate, out double time))
            {
                error = "The preview frame is outside the clip range.";
                return false;
            }

            _isPlaying = false;
            _currentFrame = frame;
            _time = time;
            return TryApplyPose(out error);
        }

        public bool TrySetPlaying(bool playing, out string error)
        {
            if (!EnsureReady(out error))
            {
                return false;
            }

            if (!_hasClip)
            {
                error = "Select an animation before playing the preview.";
                return false;
            }

            if (playing && _time >= _length)
            {
                _time = 0d;
                _currentFrame = 0;

                if (!TryApplyPose(out error))
                {
                    return false;
                }
            }

            _isPlaying = playing;
            error = null;
            return true;
        }

        public bool TryAdvance(float unscaledDeltaTime, out string error)
        {
            if (!EnsureReady(out error))
            {
                return false;
            }

            if (unscaledDeltaTime < 0f || float.IsNaN(unscaledDeltaTime) || float.IsInfinity(unscaledDeltaTime))
            {
                _isPlaying = false;
                error = "Preview elapsed time must be finite and non-negative.";
                return false;
            }

            if (!_isPlaying)
            {
                error = null;
                return true;
            }

            _time = Math.Min(_time + unscaledDeltaTime, _length);
            AnimationFrameUtility.TryGetFrame(_time, _length, _frameRate, out _currentFrame);
            _isPlaying = _time < _length;
            return TryApplyPose(out error);
        }

        public void Dispose()
        {
            _isPlaying = false;

            if (_graph.IsValid())
            {
                _graph.Destroy();
            }

            if (_container != null)
            {
                _container.SetActive(false);
                DestroyOwned(_container);
            }

            if (_sampleClip != null)
            {
                DestroyOwned(_sampleClip);
            }

            _sourcePrefab = null;
            _sourceClip = null;
            _previewRoot = null;
            _container = null;
            _instance = null;
            _sampleClip = null;
            _animator = null;
            _avatar = null;
            _hadAvatar = false;
            _requiresHumanAvatar = false;
            _hasClip = false;
            _graph = default;
            _playable = default;
            _time = 0d;
            _currentFrame = 0;
        }

        void DisableAutomaticComponents()
        {
            Behaviour[] behaviours = _instance.GetComponentsInChildren<Behaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null && behaviours[i] != _animator)
                {
                    behaviours[i].enabled = false;
                }
            }

            Collider[] colliders = _instance.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            Collider2D[] colliders2D = _instance.GetComponentsInChildren<Collider2D>(true);

            for (int i = 0; i < colliders2D.Length; i++)
            {
                colliders2D[i].enabled = false;
            }

            Rigidbody[] bodies = _instance.GetComponentsInChildren<Rigidbody>(true);

            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].isKinematic = true;
                bodies[i].useGravity = false;
                bodies[i].detectCollisions = false;
            }

            Rigidbody2D[] bodies2D = _instance.GetComponentsInChildren<Rigidbody2D>(true);

            for (int i = 0; i < bodies2D.Length; i++)
            {
                bodies2D[i].simulated = false;
            }

            ParticleSystem[] particles = _instance.GetComponentsInChildren<ParticleSystem>(true);

            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                main.playOnAwake = false;
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        bool EnsureReady(out string error)
        {
            if (HasValidContext())
            {
                error = null;
                return true;
            }

            Dispose();
            error = "The animation preview is no longer valid. Reload its source assets.";
            return false;
        }

        bool HasValidContext()
        {
            if (_sourcePrefab == null || _previewRoot == null
                || _container == null || !_container.activeInHierarchy || _instance == null
                || _instance.transform.parent != _container.transform || _container.transform.parent != _previewRoot
                || _animator == null || !_animator.isActiveAndEnabled || _animator.avatar != _avatar
                || _animator.runtimeAnimatorController != null || _animator.applyRootMotion || _animator.fireEvents
                || _animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                return false;
            }

            if (_hasClip && (_sourceClip == null || !_graph.IsValid() || !_playable.IsValid()
                || _sampleClip == null || _sourceClip.legacy || _sourceClip.length != _length
                || _sourceClip.frameRate != _frameRate))
            {
                return false;
            }

            return !(_hadAvatar && _avatar == null)
                && (!_requiresHumanAvatar || _avatar != null && _avatar.isValid && _avatar.isHuman);
        }

        bool TryApplyPose(out string error)
        {
            try
            {
                ApplyPose();
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                Dispose();
                error = "Could not sample animation preview: " + exception.Message;
                return false;
            }
        }

        void ApplyPose()
        {
            FixRootTransform();
            _playable.SetTime(_time);
            _graph.Evaluate(0f);
            FixRootTransform();
        }

        void FixRootTransform()
        {
            _container.transform.localPosition = Vector3.zero;
            _container.transform.localRotation = Quaternion.identity;
            _instance.transform.localPosition = Vector3.zero;
            _instance.transform.localRotation = Quaternion.identity;
            _animator.transform.localPosition = _animatorPosition;
            _animator.transform.localRotation = _animatorRotation;
        }

        static void DestroyOwned(Object target)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
