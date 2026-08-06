#if UNITY_EDITOR
using CrossDefense.UI;
using NUnit.Framework;

namespace CrossDefense.Tests.EditMode
{
    public sealed class SummonPlacementFeedbackTests
    {
        [Test]
        public void NoSpaceMessage_NamesSingleUnitAndExplainsBenchFallback()
        {
            string message = SummonPlacementFeedback.BuildNoSpaceMessage("주먹 슬라임", 1);

            Assert.That(message, Does.Contain("주먹 슬라임"));
            Assert.That(message, Does.Contain("필드 공간"));
            Assert.That(message, Does.Contain("보유 용병"));
        }

        [Test]
        public void NoSpaceMessage_GroupsRapidMultipleFailures()
        {
            string message = SummonPlacementFeedback.BuildNoSpaceMessage("주먹 슬라임", 3);

            Assert.That(message, Does.StartWith("3마리"));
            Assert.That(message, Does.Contain("자동 배치 실패"));
        }
    }
}
#endif
