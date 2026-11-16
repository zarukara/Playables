using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UiSystem
{
    public sealed class SafeAreaFitterTests
    {
        private GameObject owner;
        private RectTransform panel;
        private SafeAreaFitter fitter;
        private static readonly MethodInfo ApplyMethod = typeof(SafeAreaFitter).GetMethod("Apply", BindingFlags.Instance | BindingFlags.NonPublic);
        [SetUp] public void Setup()
        {
            owner = new GameObject("SafeAreaPanel", typeof(RectTransform));
            panel = (RectTransform)owner.transform;
            fitter = owner.AddComponent<SafeAreaFitter>();
        }
        [TearDown] public void Teardown() => Object.DestroyImmediate(owner);
        private void Apply(Rect area, int width, int height, bool expectsLog)
        {
            if (expectsLog) LogAssert.Expect(LogType.Log, new Regex(@"\[SafeAreaFitter\] Screen.safeArea=.*resolution=" + width + "x" + height));
            ApplyMethod.Invoke(fitter, new object[] { area, width, height });
            LogAssert.NoUnexpectedReceived();
        }
        [Test] public void InsetsAreNormalizedAndOffsetsAreCleared()
        {
            panel.offsetMin = new Vector2(20, 30); panel.offsetMax = new Vector2(-20, -30);
            Apply(new Rect(50, 100, 900, 1700), 1000, 2000, true);
            Assert.That(panel.anchorMin.x, Is.EqualTo(.05f).Within(.00001f));
            Assert.That(panel.anchorMin.y, Is.EqualTo(.05f).Within(.00001f));
            Assert.That(panel.anchorMax.x, Is.EqualTo(.95f).Within(.00001f));
            Assert.That(panel.anchorMax.y, Is.EqualTo(.9f).Within(.00001f));
            Assert.That(panel.offsetMin, Is.EqualTo(Vector2.zero)); Assert.That(panel.offsetMax, Is.EqualTo(Vector2.zero));
        }
        [Test] public void ChangedSafeAreaAtSameResolutionIsApplied()
        {
            Apply(new Rect(0, 0, 1000, 2000), 1000, 2000, true);
            Apply(new Rect(0, 80, 1000, 1800), 1000, 2000, true);
            Assert.That(panel.anchorMin.y, Is.EqualTo(.04f).Within(.00001f));
            Assert.That(panel.anchorMax.y, Is.EqualTo(.94f).Within(.00001f));
        }
        [Test] public void ChangedResolutionWithSameSafeRectangleIsApplied()
        {
            Rect area = new Rect(0, 0, 800, 600);
            Apply(area, 800, 600, true); Apply(area, 1600, 1200, true);
            Assert.That(panel.anchorMax, Is.EqualTo(Vector2.one * .5f));
        }
        [Test] public void UnchangedDataDoesNotRewritePanelOrLog()
        {
            Rect area = new Rect(0, 0, 1000, 2000);
            Apply(area, 1000, 2000, true);
            panel.offsetMin = new Vector2(7, 9);
            Apply(area, 1000, 2000, false);
            Assert.That(panel.offsetMin, Is.EqualTo(new Vector2(7, 9)), "No second layout write should occur.");
        }
        [Test] public void ZeroSizeIsSkippedWithoutPoisoningNextFrame()
        {
            Apply(new Rect(0, 0, 1000, 2000), 0, 2000, false);
            Apply(new Rect(0, 0, 1000, 2000), 1000, 0, false);
            Apply(new Rect(0, 100, 1000, 1800), 1000, 2000, true);
            Assert.That(panel.anchorMin.y, Is.EqualTo(.05f).Within(.00001f));
            Assert.That(panel.anchorMax.y, Is.EqualTo(.95f).Within(.00001f));
        }
        [Test] public void LandscapeInsetsUseRotatedScreenDimensions()
        {
            Apply(new Rect(0, 80, 1080, 1780), 1080, 1920, true);
            Apply(new Rect(80, 30, 1780, 1050), 1920, 1080, true);
            Assert.That(panel.anchorMin.x, Is.EqualTo(80f / 1920).Within(.00001f));
            Assert.That(panel.anchorMin.y, Is.EqualTo(30f / 1080).Within(.00001f));
            Assert.That(panel.anchorMax.x, Is.EqualTo(1860f / 1920).Within(.00001f));
            Assert.That(panel.anchorMax.y, Is.EqualTo(1));
        }
    }
}
