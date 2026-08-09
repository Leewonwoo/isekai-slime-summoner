#if UNITY_EDITOR
using System.IO;
using System.Reflection;
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
        public void DefaultBalance_MatchesApprovedComboAndOverdriveBuffValues()
        {
            Assert.That(_balance.ComboGraceSeconds, Is.EqualTo(2f));
            Assert.That(_balance.GaugePerKill(1), Is.EqualTo(3));
            Assert.That(_balance.GaugePerKill(10), Is.EqualTo(5));
            Assert.That(_balance.GaugePerKill(20), Is.EqualTo(8));
            Assert.That(_balance.GaugePerKill(30), Is.EqualTo(12));
            Assert.That(_balance.ComboCashoutMinimum, Is.EqualTo(2));
            Assert.That(_balance.ComboDamagePerCount, Is.EqualTo(0.1f));
            Assert.That(_balance.ComboGoldPerCount, Is.EqualTo(1));
            Assert.That(_balance.OverdriveDuration, Is.EqualTo(6f));
            Assert.That(_balance.OverdriveDamageMultiplier, Is.EqualTo(1.3f));
            Assert.That(_balance.OverdriveAttackSpeedMultiplier, Is.EqualTo(1.5f));
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
            int expiredCombo = 0;
            runtime.ComboExpired += combo => expiredCombo = combo;
            runtime.RegisterDefeat();
            runtime.RegisterDefeat();

            runtime.Tick(_balance.ComboGraceSeconds + 0.01f, false, null);

            Assert.That(runtime.Snapshot.Combo, Is.Zero);
            Assert.That(runtime.Snapshot.Gauge, Is.EqualTo(6));
            Assert.That(expiredCombo, Is.EqualTo(2));
        }

        [Test]
        public void ForcedComboReset_DoesNotTriggerCashout()
        {
            var runtime = new DopamineRuntime(_balance);
            int cashoutCount = 0;
            runtime.ComboExpired += _ => cashoutCount++;
            runtime.RegisterDefeat();
            runtime.RegisterDefeat();

            runtime.ResetCombo();

            Assert.That(runtime.Snapshot.Combo, Is.Zero);
            Assert.That(cashoutCount, Is.Zero);
        }

        [Test]
        public void Overdrive_ActivatesRelicOnceAndCannotRechargeWhileActive()
        {
            var runtime = CreateReadyRuntime();
            int activationCount = 0;

            runtime.Tick(0f, true, () =>
            {
                activationCount++;
                return true;
            });

            Assert.That(runtime.Snapshot.IsActive, Is.True);
            Assert.That(runtime.Snapshot.Gauge, Is.Zero);
            Assert.That(activationCount, Is.EqualTo(1));

            runtime.RegisterDefeat();
            Assert.That(runtime.Snapshot.Gauge, Is.Zero);

            runtime.Tick(_balance.OverdriveDuration + 0.01f, true, () => false);

            Assert.That(activationCount, Is.EqualTo(1));
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
        public void Overdrive_FrameHitchDoesNotRepeatRelicActivation()
        {
            var runtime = CreateReadyRuntime();
            int activationCount = 0;
            runtime.Tick(1f, true, () =>
            {
                activationCount++;
                return true;
            });
            runtime.Tick(1f, true, () =>
            {
                activationCount++;
                return true;
            });

            Assert.That(activationCount, Is.EqualTo(1));
            Assert.That(runtime.Snapshot.IsActive, Is.True);
        }

        [Test]
        public void DefaultBalanceAsset_ExistsAndUsesDopamineBalanceType()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DopamineBalanceData>(
                "Assets/Data/DopamineBalance_Default.asset");

            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.OverdriveDamageMultiplier, Is.EqualTo(1.3f));
            Assert.That(asset.OverdriveAttackSpeedMultiplier, Is.EqualTo(1.5f));
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
                new DopamineSnapshot(0, 0f, 50, 100, false, 0f),
                _balance);

            VisualElement fill = root.Q<VisualElement>("overdrive-gauge-fill");
            Assert.That(fill.style.height.value.unit, Is.EqualTo(LengthUnit.Percent));
            Assert.That(fill.style.height.value.value, Is.EqualTo(50f).Within(0.01f));
            Assert.That(
                root.Query<VisualElement>(className: "overdrive-gauge__tick").ToList().Count,
                Is.EqualTo(3));

            controller.Dispose();
        }

        [Test]
        public void FieldOverlayController_ReflectsSelectedGameplaySpeed()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/UI/UXML/FieldOverlay.uxml");
            TemplateContainer root = tree.CloneTree();
            var controller = new FieldOverlayController(root);

            controller.SetGameplaySpeed(1.5f);

            Button speedButton = root.Q<Button>("speed-toggle-button");
            Assert.That(speedButton.text, Is.EqualTo("×1.5"));
            Assert.That(
                speedButton.ClassListContains("speed-toggle-button--fast"),
                Is.True);
            controller.Dispose();
        }

        [Test]
        public void FieldOverlay_RightMenuUsesOnePointFiveScaleControls()
        {
            string variables = File.ReadAllText("Assets/UI/USS/variables.uss");
            string overlay = File.ReadAllText("Assets/UI/USS/FieldOverlay.uss");

            StringAssert.Contains("--field-settings-size: 96px", variables);
            StringAssert.Contains("--field-settings-icon-size: 66px", variables);
            StringAssert.Contains("--field-speed-width: 144px", variables);
            StringAssert.Contains("--codex-button-size: 96px", variables);
            StringAssert.Contains("--codex-button-icon-size: 90px", variables);
            StringAssert.Contains("width: var(--field-menu-width)", overlay);
            StringAssert.Contains("width: var(--field-speed-width)", overlay);
        }

        [Test]
        public void FieldOverlay_RightSkillControlsUseReadableScaleAndSpacing()
        {
            string variables = File.ReadAllText("Assets/UI/USS/variables.uss");

            StringAssert.Contains("--skill-button-size: 160px", variables);
            StringAssert.Contains("--skill-button-icon-size: 72px", variables);
            StringAssert.Contains("--skill-button-label-size: 28px", variables);
            StringAssert.Contains("--skill-button-cooldown-size: 36px", variables);
            StringAssert.Contains("--buff-skill-button-size: 120px", variables);
            StringAssert.Contains("--buff-skill-icon-size: 52px", variables);
            StringAssert.Contains("--buff-skill-cluster-bottom: 200px", variables);
            StringAssert.Contains("--overdrive-gauge-right: 200px", variables);
        }

        [Test]
        public void FieldOverlayController_ToastDoesNotCancelGoldenGoblinHide()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/UI/UXML/FieldOverlay.uxml");
            TemplateContainer root = tree.CloneTree();
            var controller = new FieldOverlayController(root);

            controller.SetGoldenGoblinState(new GoldenGoblinSnapshot(
                GoldenGoblinState.Defeated,
                0f,
                0f,
                50));
            FieldInfo resetField = typeof(FieldOverlayController).GetField(
                "_goldenGoblinReset",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var reset = (IVisualElementScheduledItem)resetField?.GetValue(controller);

            Assert.That(reset, Is.Not.Null);
            Assert.That(reset.isActive, Is.True);
            controller.ShowToast("새 슬라임 해금!");
            Assert.That(reset.isActive, Is.True);

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
