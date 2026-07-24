#if UNITY_EDITOR
using CrossDefense.Core;
using CrossDefense.Data;
using CrossDefense.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.Tests.EditMode
{
    public sealed class DopamineSystemTests
    {
        DopamineBalanceData _balance;

        [SetUp]
        public void SetUp()
        {
            _balance = DopamineBalanceData.CreateRuntimeDefault();
        }

        [TearDown]
        public void TearDown()
        {
            if (_balance != null)
                Object.DestroyImmediate(_balance);
        }

        [Test]
        public void DefaultBalance_MatchesApprovedComboAndMeteorValues()
        {
            Assert.That(_balance.ComboGraceSeconds, Is.EqualTo(2f));
            Assert.That(_balance.GaugePerKill(1), Is.EqualTo(3));
            Assert.That(_balance.GaugePerKill(10), Is.EqualTo(5));
            Assert.That(_balance.GaugePerKill(20), Is.EqualTo(8));
            Assert.That(_balance.GaugePerKill(30), Is.EqualTo(12));
            Assert.That(_balance.OverdriveDuration, Is.EqualTo(6f));
            Assert.That(_balance.MeteorInterval, Is.EqualTo(0.3f));
            Assert.That(_balance.MeteorCount, Is.EqualTo(20));
            Assert.That(_balance.MeteorDamageMultiplier, Is.EqualTo(0.55f));
            Assert.That(_balance.MeteorRadius, Is.EqualTo(1.35f));
        }

        [Test]
        public void UnbrokenCombo_ChargesOverdriveOnTwentySecondDefeat()
        {
            var runtime = new DopamineRuntime(_balance);

            for (int i = 0; i < 21; i++)
                runtime.RegisterDefeat();

            Assert.That(runtime.Snapshot.Combo, Is.EqualTo(21));
            Assert.That(runtime.Snapshot.Gauge, Is.EqualTo(93));
            Assert.That(runtime.Snapshot.IsReady, Is.False);

            runtime.RegisterDefeat();

            Assert.That(runtime.Snapshot.Combo, Is.EqualTo(22));
            Assert.That(runtime.Snapshot.Gauge, Is.EqualTo(100));
            Assert.That(runtime.Snapshot.IsReady, Is.True);
        }

        [Test]
        public void ComboTimeout_ResetsComboButPreservesGauge()
        {
            var runtime = new DopamineRuntime(_balance);
            runtime.RegisterDefeat();

            runtime.Tick(_balance.ComboGraceSeconds + 0.01f, false, null);

            Assert.That(runtime.Snapshot.Combo, Is.Zero);
            Assert.That(runtime.Snapshot.Gauge, Is.EqualTo(3));
        }

        [Test]
        public void Overdrive_DropsTwentyMeteorsAndCannotRechargeWhileActive()
        {
            var runtime = CreateReadyRuntime();
            int meteorCount = 0;

            runtime.Tick(0f, true, () =>
            {
                meteorCount++;
                return true;
            });

            Assert.That(runtime.Snapshot.IsActive, Is.True);
            Assert.That(runtime.Snapshot.Gauge, Is.Zero);
            Assert.That(meteorCount, Is.EqualTo(1));

            runtime.RegisterDefeat();
            Assert.That(runtime.Snapshot.Gauge, Is.Zero);

            for (int i = 0; i < 19; i++)
                runtime.Tick(_balance.MeteorInterval, true, () =>
                {
                    meteorCount++;
                    return true;
                });
            runtime.Tick(_balance.MeteorInterval + 0.01f, true, () =>
            {
                meteorCount++;
                return true;
            });

            Assert.That(meteorCount, Is.EqualTo(20));
            Assert.That(runtime.Snapshot.IsActive, Is.False);
            Assert.That(runtime.Snapshot.Gauge, Is.Zero);
        }

        [Test]
        public void Overdrive_PausesDurationWhenNoEnemyIsAlive()
        {
            var runtime = CreateReadyRuntime();
            runtime.Tick(0f, true, () => true);
            float activeTime = runtime.Snapshot.ActiveTimeRemaining;

            runtime.Tick(1f, false, () => true);

            Assert.That(runtime.Snapshot.ActiveTimeRemaining, Is.EqualTo(activeTime));
            Assert.That(runtime.Snapshot.IsActive, Is.True);
        }

        [Test]
        public void Overdrive_FrameHitchCapsCatchUpDropsAtTwo()
        {
            var runtime = CreateReadyRuntime();
            int meteorCount = 0;
            runtime.Tick(1f, true, () =>
            {
                meteorCount++;
                return true;
            });

            Assert.That(meteorCount, Is.EqualTo(2));
            Assert.That(runtime.Snapshot.IsActive, Is.True);
        }

        [Test]
        public void DefaultBalanceAsset_ExistsAndUsesDopamineBalanceType()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DopamineBalanceData>(
                "Assets/Data/DopamineBalance_Default.asset");

            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.MeteorCount, Is.EqualTo(20));
        }

        [Test]
        public void FieldOverlayController_ChargesVerticalGaugeByHeight()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/UI/UXML/FieldOverlay.uxml");
            Assert.That(tree, Is.Not.Null);

            TemplateContainer root = tree.CloneTree();
            var controller = new FieldOverlayController(root);
            controller.SetDopamineState(
                new DopamineSnapshot(0, 0f, 50, 100, false, 0f, 0),
                _balance);

            VisualElement fill = root.Q<VisualElement>("overdrive-gauge-fill");
            Assert.That(fill.style.height.value.unit, Is.EqualTo(LengthUnit.Percent));
            Assert.That(fill.style.height.value.value, Is.EqualTo(50f).Within(0.01f));
            Assert.That(
                root.Query<VisualElement>(className: "overdrive-gauge__tick").ToList().Count,
                Is.EqualTo(3));

            controller.Dispose();
        }

        DopamineRuntime CreateReadyRuntime()
        {
            var runtime = new DopamineRuntime(_balance);
            for (int i = 0; i < 22; i++)
                runtime.RegisterDefeat();
            return runtime;
        }
    }
}
#endif
