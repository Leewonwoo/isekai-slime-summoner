#if UNITY_EDITOR
using CrossDefense.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CrossDefense.Tests.EditMode
{
    public sealed class DamageFloatingTextTests
    {
        GameObject _host;
        GameObject _cameraObject;
        DamageFloatingTextService _service;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("DamageTextTestCamera", typeof(Camera));
            Camera camera = _cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            _cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            _host = new GameObject("DamageTextTestHost");
            _service = _host.AddComponent<DamageFloatingTextService>();
            _service.Initialize(camera);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_cameraObject);
        }

        [Test]
        public void Overlay_UsesOneNonInteractiveCanvasAndSharedMaterial()
        {
            Canvas canvas = _service.OverlayCanvas;
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(canvas.sortingOrder, Is.EqualTo(1000));
            Assert.That(canvas.GetComponent<GraphicRaycaster>(), Is.Null);

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(scaler.matchWidthOrHeight, Is.Zero);

            Text[] texts = canvas.GetComponentsInChildren<Text>(true);
            Assert.That(texts.Length, Is.EqualTo(24));
            Font sharedFont = texts[0].font;
            Material sharedMaterial = texts[0].material;
            for (int i = 0; i < texts.Length; i++)
            {
                Assert.That(texts[i].font, Is.SameAs(sharedFont));
                Assert.That(texts[i].material, Is.SameAs(sharedMaterial));
                Assert.That(texts[i].raycastTarget, Is.False);
                Assert.That(texts[i].maskable, Is.False);
                Assert.That(texts[i].GetComponent<CanvasRenderer>().cullTransparentMesh, Is.True);
            }
        }

        [Test]
        public void Show_ReusesPoolAndNeverExceedsHardCapacity()
        {
            for (int i = 0; i < 80; i++)
                Assert.That(_service.Show(Vector3.zero, i + 1f, DamageTextKind.Dealt), Is.True);

            Assert.That(_service.ActiveCount, Is.EqualTo(_service.Capacity));
            Assert.That(_service.CreatedCount, Is.EqualTo(_service.Capacity));
        }

        [Test]
        public void Show_BindsActualDamageTextWithoutRaycast()
        {
            Assert.That(_service.Show(Vector3.zero, 12f, DamageTextKind.Received), Is.True);

            Text[] texts = _service.OverlayCanvas.GetComponentsInChildren<Text>(true);
            Text active = null;
            for (int i = 0; i < texts.Length; i++)
            {
                if (!texts[i].gameObject.activeSelf) continue;
                active = texts[i];
                break;
            }

            Assert.That(active, Is.Not.Null);
            Assert.That(active.text, Is.EqualTo("12"));
            Color expected = new Color32(255, 82, 82, 255);
            Assert.That(active.color.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(active.color.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(active.color.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(active.color.a, Is.EqualTo(expected.a).Within(0.001f));
            Assert.That(active.raycastTarget, Is.False);
        }
    }
}
#endif
