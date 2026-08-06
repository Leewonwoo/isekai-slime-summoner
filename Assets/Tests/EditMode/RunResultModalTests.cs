#if UNITY_EDITOR
using CrossDefense.Core;
using CrossDefense.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace CrossDefense.Tests.EditMode
{
    public sealed class RunResultModalTests
    {
        [Test]
        public void Defeat_ShowsRetryAndHidesContinue()
        {
            RunResultModalController controller = Create(out TemplateContainer root);

            Assert.That(
                root.Q<VisualElement>("run-result-overlay").pickingMode,
                Is.EqualTo(PickingMode.Ignore));
            controller.Show(RunPhase.Defeat, "Goblin Forest", 17, 50, 1234, true);

            Assert.That(controller.IsVisible, Is.True);
            Assert.That(
                root.Q<VisualElement>("run-result-overlay").pickingMode,
                Is.EqualTo(PickingMode.Position));
            Assert.That(root.Q<Label>("run-result-title").text, Is.EqualTo("소환사 패배"));
            Assert.That(root.Q<Button>("run-result-restart-button").text, Is.EqualTo("다시 시작"));
            Assert.That(
                root.Q<Button>("run-result-continue-button").style.display.value,
                Is.EqualTo(DisplayStyle.None));
            controller.Hide();
            Assert.That(
                root.Q<VisualElement>("run-result-overlay").pickingMode,
                Is.EqualTo(PickingMode.Ignore));
            controller.Dispose();
        }

        [Test]
        public void Victory_ShowsContinueOnlyWhenNextStageExists()
        {
            RunResultModalController controller = Create(out TemplateContainer root);

            controller.Show(RunPhase.Victory, "Goblin Forest", 50, 50, 4321, true);

            Assert.That(root.Q<Label>("run-result-title").text, Is.EqualTo("스테이지 클리어"));
            Assert.That(
                root.Q<Button>("run-result-continue-button").style.display.value,
                Is.EqualTo(DisplayStyle.Flex));

            controller.Show(RunPhase.Victory, "Goblin Forest", 50, 50, 4321, false);
            Assert.That(
                root.Q<Button>("run-result-continue-button").style.display.value,
                Is.EqualTo(DisplayStyle.None));
            controller.Dispose();
        }

        static RunResultModalController Create(out TemplateContainer root)
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/UI/UXML/RunResultModal.uxml");
            Assert.That(tree, Is.Not.Null);
            root = tree.CloneTree();
            return new RunResultModalController(root);
        }
    }
}
#endif
