#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Animations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace RaggedBanner.AnimationTools.Tests
{
    public sealed class AnimationEventToolPreviewTests
    {
        readonly List<Object> _objects = new List<Object>();
        AnimationEventToolPreview _preview;
        GameObject _prefab;
        AnimationClip _clip;
        Transform _root;

        [SetUp]
        public void SetUp()
        {
            _preview = new AnimationEventToolPreview();
            _root = CreateObject("Preview Root").transform;
            _prefab = CreateObject("Preview Source");
            _prefab.SetActive(false);
            _prefab.AddComponent<Animator>();
            GameObject animated = CreateObject("Animated");
            animated.transform.SetParent(_prefab.transform, false);
            _clip = CreateClip(1f);
        }

        [TearDown]
        public void TearDown()
        {
            _preview.Dispose();

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
        public void UninitializedPreviewRejectsCommandsAndRepeatedDisposeIsSafe()
        {
            Assert.That(_preview.IsReady, Is.False);
            Assert.That(_preview.TrySetFrame(0, out _), Is.False);
            Assert.That(_preview.TrySetPlaying(true, out _), Is.False);
            Assert.That(_preview.TryAdvance(0.1f, out _), Is.False);
            _preview.Dispose();
            _preview.Dispose();
            Assert.That(_preview.Instance, Is.Null);
        }

        [Test]
        public void MissingClipKeepsRestPoseAndRejectsTimelineWithoutDestroyingThePreview()
        {
            Assert.That(_preview.TryCreate(_prefab, null, _root, out string error), Is.True, error);
            GameObject instance = _preview.Instance;

            Assert.That(_preview.IsReady, Is.True);
            AssertPose(1f);
            Assert.That(GetOwned<AnimationClip>("_sampleClip"), Is.Null);
            Assert.That(GetOwned<PlayableGraph>("_graph").IsValid(), Is.False);
            Assert.That(_preview.TrySetFrame(0, out _), Is.False);
            Assert.That(_preview.TrySetPlaying(true, out _), Is.False);
            Assert.That(_preview.TryAdvance(0.1f, out error), Is.True, error);
            Assert.That(_preview.Instance, Is.SameAs(instance));
            Assert.That(_preview.IsReady, Is.True);
            Assert.That(_preview.IsPlaying, Is.False);
            _preview.Dispose();
            Assert.That(instance == null, Is.True);
            Assert.That(_root.childCount, Is.Zero);
        }

        [Test]
        public void RestPoseAndClipPreviewsCanReplaceEachOtherWithoutRetainingGraphResources()
        {
            CreatePreview();
            PlayableGraph graph = GetOwned<PlayableGraph>("_graph");
            AnimationClip sample = GetOwned<AnimationClip>("_sampleClip");

            Assert.That(_preview.TryCreate(_prefab, null, _root, out string error), Is.True, error);

            Assert.That(graph.IsValid(), Is.False);
            Assert.That(sample == null, Is.True);
            Assert.That(_preview.IsReady, Is.True);
            CreatePreview();
            SetFrame(15);
            AssertPose(2f);
            Assert.That(_root.childCount, Is.EqualTo(1));
        }

        [Test]
        public void CreateSamplesZeroAndReverseScrubbingUsesExactFrameTime()
        {
            CreatePreview();

            Assert.That(_preview.CurrentFrame, Is.Zero);
            Assert.That(_preview.IsPlaying, Is.False);
            AssertPose(1f);
            SetFrame(30);
            AssertPose(3f);
            SetFrame(15);
            AssertPose(2f);
            SetFrame(3);
            AssertPose(1.2f);
            SetFrame(0);
            AssertPose(1f);
            Assert.That(_prefab.activeSelf, Is.False);
            Assert.That(_prefab.transform.Find("Animated").localScale.x, Is.EqualTo(1f));
        }

        [Test]
        public void LoopClipCopyKeepsEndPoseAndPreservesSourceLoopAndEvents()
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(_clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(_clip, settings);
            AnimationUtility.SetAnimationEvents(_clip, new[]
            {
                new AnimationEvent { time = 0f, functionName = "AttackHit" },
                new AnimationEvent { time = 0.5f, functionName = "AttackHit" }
            });
            Assert.That(_clip.isLooping, Is.True);
            string before = EditorJsonUtility.ToJson(_clip);

            CreatePreview();
            SetFrame(30);

            AssertPose(3f);
            AnimationClip sample = GetOwned<AnimationClip>("_sampleClip");
            Assert.That(sample, Is.Not.SameAs(_clip));
            Assert.That(sample.isLooping, Is.False);
            Assert.That(AnimationUtility.GetAnimationEvents(sample), Is.Empty);
            Assert.That(_preview.Instance.GetComponent<Animator>().fireEvents, Is.False);
            Assert.That(EditorJsonUtility.ToJson(_clip), Is.EqualTo(before));
            Assert.That(_preview.TrySetPlaying(true, out string error), Is.True, error);
            AssertPose(1f);
            Assert.That(_preview.TryAdvance(5f, out error), Is.True, error);
            AssertPose(3f);
            Assert.That(_preview.IsPlaying, Is.False);
        }

        [Test]
        public void PlaybackSamplesSubframesAtOneTimesSpeedAndStopsAtEnd()
        {
            _prefab.GetComponent<Animator>().speed = 4f;
            CreatePreview();
            Assert.That(_preview.Instance.GetComponent<Animator>().speed, Is.EqualTo(1f));
            float previousTimeScale = Time.timeScale;

            try
            {
                Time.timeScale = 0f;
                Assert.That(_preview.TrySetPlaying(true, out string error), Is.True, error);
                Assert.That(_preview.TryAdvance(0.25f, out error), Is.True, error);
                AssertPose(1.5f);
                Assert.That(_preview.CurrentFrame, Is.EqualTo(7));
                Assert.That(_preview.TryAdvance(0f, out error), Is.True, error);
                AssertPose(1.5f);
                Assert.That(_preview.IsPlaying, Is.True);
                Assert.That(_preview.TryAdvance(float.MaxValue, out error), Is.True, error);
                AssertPose(3f);
                Assert.That(_preview.CurrentFrame, Is.EqualTo(30));
                Assert.That(_preview.IsPlaying, Is.False);
                Assert.That(_preview.TryAdvance(1f, out error), Is.True, error);
                AssertPose(3f);
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }
        }

        [Test]
        public void PauseAndScrubbingStopPlaybackWithoutMovingThePose()
        {
            CreatePreview();
            Assert.That(_preview.TrySetPlaying(true, out _), Is.True);
            Assert.That(_preview.TryAdvance(0.25f, out _), Is.True);
            Assert.That(_preview.TrySetPlaying(false, out _), Is.True);
            Assert.That(_preview.TryAdvance(1f, out _), Is.True);
            AssertPose(1.5f);
            Assert.That(_preview.TrySetPlaying(true, out _), Is.True);

            SetFrame(6);

            Assert.That(_preview.IsPlaying, Is.False);
            Assert.That(_preview.TryAdvance(1f, out _), Is.True);
            AssertPose(1.4f);
        }

        [Test]
        public void ShortClipWithOnlyFrameZeroSupportsScrubAndPlaybackEnd()
        {
            AnimationClip shortClip = CreateClip(0.01f);
            Assert.That(AnimationFrameUtility.TryGetLastFrame(shortClip.length, shortClip.frameRate, out int last), Is.True);
            Assert.That(last, Is.Zero);
            Assert.That(_preview.TryCreate(_prefab, shortClip, _root, out string error), Is.True, error);
            SetFrame(0);
            AssertPose(1f);
            Assert.That(_preview.TrySetFrame(1, out error), Is.False);

            Assert.That(_preview.TrySetPlaying(true, out error), Is.True, error);
            Assert.That(_preview.TryAdvance(1f, out error), Is.True, error);

            Assert.That(_preview.CurrentFrame, Is.Zero);
            Assert.That(_preview.IsPlaying, Is.False);
            AssertPose(3f);
        }

        [TestCase(-1)]
        [TestCase(31)]
        public void InvalidFrameKeepsTheExistingPreview(int frame)
        {
            CreatePreview();
            SetFrame(15);

            Assert.That(_preview.TrySetFrame(frame, out string error), Is.False);

            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(_preview.IsReady, Is.True);
            Assert.That(_preview.CurrentFrame, Is.EqualTo(15));
            AssertPose(2f);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDeltaStopsPlaybackAndLeavesTheCurrentPose(float delta)
        {
            CreatePreview();
            SetFrame(15);
            Assert.That(_preview.TrySetPlaying(true, out _), Is.True);

            Assert.That(_preview.TryAdvance(delta, out string error), Is.False);

            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(_preview.IsPlaying, Is.False);
            Assert.That(_preview.IsReady, Is.True);
            AssertPose(2f);
        }

        [Test]
        public void AnimatedRootCannotMoveOrRotateThePreviewInstance()
        {
            _root.SetPositionAndRotation(new Vector3(2f, 3f, 4f), Quaternion.Euler(0f, 45f, 0f));
            _prefab.transform.localPosition = new Vector3(20f, 0f, 0f);
            _prefab.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            _clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 50f));
            _clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.y", AnimationCurve.Linear(0f, 0f, 1f, 120f));
            CreatePreview();

            SetFrame(30);

            Assert.That(Vector3.Distance(_preview.Instance.transform.position, _root.position), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(_preview.Instance.transform.rotation, _root.rotation), Is.LessThan(0.1f));
            Assert.That(_prefab.transform.localPosition.x, Is.EqualTo(20f));
            AssertPose(3f);
        }

        [Test]
        public void AutomaticComponentsAreDisabledOnlyOnThePreviewClone()
        {
            _prefab.AddComponent<BoxCollider>();
            _prefab.AddComponent<AudioSource>();
            GameObject legacyObject = CreateObject("Legacy Animation");
            legacyObject.transform.SetParent(_prefab.transform, false);
            legacyObject.AddComponent<Animation>();
            Rigidbody body = _prefab.AddComponent<Rigidbody>();
            body.useGravity = true;
            CreatePreview();
            Animator animator = _preview.Instance.GetComponent<Animator>();

            Assert.That(animator.enabled, Is.True);
            Assert.That(animator.runtimeAnimatorController, Is.Null);
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(animator.fireEvents, Is.False);
            Assert.That(_preview.Instance.GetComponent<BoxCollider>().enabled, Is.False);
            Assert.That(_preview.Instance.GetComponent<AudioSource>().enabled, Is.False);
            Assert.That(_preview.Instance.GetComponentInChildren<Animation>(true).enabled, Is.False);
            Assert.That(_preview.Instance.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(_preview.Instance.GetComponent<Rigidbody>().detectCollisions, Is.False);
            Assert.That(_prefab.GetComponent<BoxCollider>().enabled, Is.True);
            Assert.That(_prefab.GetComponent<AudioSource>().enabled, Is.True);
            Assert.That(body.useGravity, Is.True);
        }

        [Test]
        public void ReplacementAndRepeatedDisposeReleaseOnlyOwnedResources()
        {
            CreatePreview();
            GameObject first = _preview.Instance;
            AnimationClip firstCopy = GetOwned<AnimationClip>("_sampleClip");
            PlayableGraph firstGraph = GetOwned<PlayableGraph>("_graph");

            CreatePreview();

            Assert.That(first == null, Is.True);
            Assert.That(firstCopy == null, Is.True);
            Assert.That(firstGraph.IsValid(), Is.False);
            Assert.That(_root.childCount, Is.EqualTo(1));
            GameObject second = _preview.Instance;
            AnimationClip secondCopy = GetOwned<AnimationClip>("_sampleClip");
            PlayableGraph secondGraph = GetOwned<PlayableGraph>("_graph");
            _preview.Dispose();
            _preview.Dispose();
            Assert.That(second == null, Is.True);
            Assert.That(secondCopy == null, Is.True);
            Assert.That(secondGraph.IsValid(), Is.False);
            Assert.That(_root.childCount, Is.Zero);
            Assert.That(_prefab != null && _clip != null, Is.True);
            Assert.That(_preview.Instance, Is.Null);
        }

        [Test]
        public void CreationFailureReleasesPreviousAndPartialSessions()
        {
            CreatePreview();
            GameObject previous = _preview.Instance;
            Object.DestroyImmediate(_prefab.GetComponent<Animator>());

            Assert.That(_preview.TryCreate(_prefab, _clip, _root, out string error), Is.False);

            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(previous == null, Is.True);
            Assert.That(_preview.IsReady, Is.False);
            Assert.That(_preview.Instance, Is.Null);
            Assert.That(_root.childCount, Is.Zero);
            _prefab.AddComponent<Animator>();
            CreatePreview();
            AssertPose(1f);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void LostSourceOrPreviewResourceInvalidatesAndReleasesSession(int lostResource)
        {
            CreatePreview();
            AnimationClip sample = GetOwned<AnimationClip>("_sampleClip");
            PlayableGraph graph = GetOwned<PlayableGraph>("_graph");

            switch (lostResource)
            {
                case 0:
                    Object.DestroyImmediate(_prefab);
                    break;
                case 1:
                    Object.DestroyImmediate(_clip);
                    break;
                case 2:
                    Object.DestroyImmediate(_root.gameObject);
                    break;
                default:
                    Object.DestroyImmediate(_preview.Instance.GetComponent<Animator>());
                    break;
            }

            Assert.That(_preview.IsReady, Is.False);
            Assert.That(_preview.TryAdvance(0.1f, out _), Is.False);
            Assert.That(graph.IsValid(), Is.False);
            Assert.That(sample == null, Is.True);
            Assert.That(_preview.Instance, Is.Null);
        }

        [Test]
        public void ChangedSourceMetadataAndDisabledPreviewRootRejectFurtherSampling()
        {
            CreatePreview();
            _clip.frameRate = 60f;
            Assert.That(_preview.TrySetFrame(1, out _), Is.False);
            Assert.That(_preview.Instance, Is.Null);
            CreatePreview();
            _root.gameObject.SetActive(false);

            Assert.That(_preview.TrySetPlaying(true, out _), Is.False);

            Assert.That(_preview.IsReady, Is.False);
            Assert.That(_preview.Instance, Is.Null);
        }

        [TestCase("Warrior")]
        [TestCase("Archer")]
        [TestCase("Rogue")]
        [Category("ProjectContent")]
        public void CombatPrefabScrubsHumanoidPoseWithGameScriptsAndCollidersDisabled(string unitClass)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/" + unitClass + ".prefab");
            CharacterAnimationProfile profile = AnimationEventToolCatalog.GetProfile(prefab);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.TryGetAnimation("attack1", out CharacterAnimationEntry animation), Is.True);
            Assert.That(_preview.TryCreate(prefab, animation.Clip, _root, out string error), Is.True, error);
            Animator animator = _preview.Instance.GetComponent<Animator>();
            Quaternion[] first = CapturePose(animator);
            Assert.That(AnimationFrameUtility.TryGetLastFrame(animation.Clip.length, animation.Clip.frameRate,
                out int last), Is.True);
            SetFrame(last / 2);
            Quaternion[] middle = CapturePose(animator);
            float maximum = 0f;

            for (int i = 0; i < first.Length; i++)
            {
                maximum = Mathf.Max(maximum, Quaternion.Angle(first[i], middle[i]));
            }

            Assert.That(maximum, Is.GreaterThan(1f));
            SetFrame(last);
            SetFrame(0);
            Quaternion[] reset = CapturePose(animator);

            for (int i = 0; i < first.Length; i++)
            {
                Assert.That(Quaternion.Angle(first[i], reset[i]), Is.LessThan(0.1f));
            }

            MonoBehaviour[] behaviours = _preview.Instance.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.That(behaviours.Length, Is.GreaterThan(0));

            for (int i = 0; i < behaviours.Length; i++)
            {
                Assert.That(behaviours[i].enabled, Is.False);
            }

            foreach (Collider collider in _preview.Instance.GetComponentsInChildren<Collider>(true))
            {
                Assert.That(collider.enabled, Is.False);
            }

            Assert.That(prefab.GetComponent<MonoBehaviour>().enabled, Is.True);
            Assert.That(prefab.GetComponent<Collider>().enabled, Is.True);
        }

        void CreatePreview()
        {
            Assert.That(_preview.TryCreate(_prefab, _clip, _root, out string error), Is.True, error);
            Assert.That(_preview.IsReady, Is.True);
        }

        void SetFrame(int frame)
        {
            Assert.That(_preview.TrySetFrame(frame, out string error), Is.True, error);
            Assert.That(_preview.CurrentFrame, Is.EqualTo(frame));
        }

        void AssertPose(float expected)
        {
            Assert.That(_preview.Instance.transform.Find("Animated").localScale.x,
                Is.EqualTo(expected).Within(0.001f));
        }

        GameObject CreateObject(string name)
        {
            GameObject value = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            _objects.Add(value);
            return value;
        }

        AnimationClip CreateClip(float length)
        {
            AnimationClip clip = new AnimationClip { name = "Preview Test Clip", frameRate = 30f, hideFlags = HideFlags.HideAndDontSave };
            _objects.Add(clip);
            clip.SetCurve("Animated", typeof(Transform), "localScale.x", AnimationCurve.Linear(0f, 1f, length, 3f));
            return clip;
        }

        T GetOwned<T>(string field)
        {
            return (T)typeof(AnimationEventToolPreview).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(_preview);
        }

        static Quaternion[] CapturePose(Animator animator)
        {
            Quaternion[] result = new Quaternion[(int)HumanBodyBones.LastBone];

            for (int i = 0; i < result.Length; i++)
            {
                Transform bone = animator.GetBoneTransform((HumanBodyBones)i);
                result[i] = bone != null ? bone.localRotation : Quaternion.identity;
            }

            return result;
        }
    }
}
#endif
