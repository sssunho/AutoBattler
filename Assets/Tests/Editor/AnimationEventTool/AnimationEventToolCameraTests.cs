using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace RaggedBanner.AnimationTools.Tests
{
    public sealed class AnimationEventToolCameraTests
    {
        const BindingFlags _flags = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject _host;
        GameObject _cameraObject;
        AnimationEventToolController _controller;
        Camera _camera;
        Vector3 _initialPosition;
        Quaternion _initialRotation;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Camera tests");
            _controller = _host.AddComponent<AnimationEventToolController>();
            _cameraObject = new GameObject("Preview camera");
            _camera = _cameraObject.AddComponent<Camera>();
            _initialRotation = Quaternion.Euler(45f, 225f, 0f);
            _initialPosition = new Vector3(0f, 0.9f, 0f) - _initialRotation * Vector3.forward * 4.5f;
            _camera.transform.SetPositionAndRotation(_initialPosition, _initialRotation);
            Set("_previewCamera", _camera);
            Set("_previewRoot", _host.transform);
            Invoke("CaptureCameraView");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_cameraObject);
        }

        [Test]
        public void ZoomAndPitchAreClampedAndOrbitKeepsItsCenter()
        {
            Invoke("AdjustCameraView", new Vector2(10000f, -10000f), 1000f);
            Assert.That(Get<float>("_viewDistance"), Is.EqualTo(10f));
            Assert.That(Get<float>("_viewPitch"), Is.EqualTo(80f));
            Assert.That(Get<float>("_viewYaw"), Is.InRange(0f, 360f));
            Assert.That(Vector3.Distance(_camera.transform.position + _camera.transform.forward * 10f,
                new Vector3(0f, 0.9f, 0f)), Is.LessThan(0.001f));
            Invoke("AdjustCameraView", new Vector2(0f, 10000f), -1000f);
            Assert.That(Get<float>("_viewDistance"), Is.EqualTo(1.5f));
            Assert.That(Get<float>("_viewPitch"), Is.EqualTo(15f));
        }

        [Test]
        public void DisableRestoresOwnedPoseOnOriginalCameraEvenAfterReferenceReplacement()
        {
            Invoke("AdjustCameraView", new Vector2(30f, 10f), 1f);
            Set("_previewCamera", null);
            Invoke("OnDisable");
            Assert.That(Vector3.Distance(_camera.transform.position, _initialPosition), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(_camera.transform.rotation, _initialRotation), Is.LessThan(0.001f));
        }

        [Test]
        public void DisablePreservesExternalCameraPoseChange()
        {
            Invoke("AdjustCameraView", new Vector2(30f, 10f), 1f);
            Vector3 external = new Vector3(30f, 20f, 10f);
            _camera.transform.position = external;
            Invoke("OnDisable");
            Assert.That(_camera.transform.position, Is.EqualTo(external));
        }

        [Test]
        public void FocusLossAndBlockingCancelDrag()
        {
            Set("_dragControl", 123);
            Invoke("OnApplicationFocus", false);
            Assert.That(Get<int>("_dragControl"), Is.Zero);
            Set("_dragControl", 123);
            Invoke("Block", "test");
            Assert.That(Get<int>("_dragControl"), Is.Zero);
        }

        void Invoke(string method, params object[] arguments)
        {
            typeof(AnimationEventToolController).GetMethod(method, _flags).Invoke(_controller, arguments);
        }

        void Set(string name, object value)
        {
            typeof(AnimationEventToolController).GetField(name, _flags).SetValue(_controller, value);
        }

        T Get<T>(string name)
        {
            return (T)typeof(AnimationEventToolController).GetField(name, _flags).GetValue(_controller);
        }
    }
}
