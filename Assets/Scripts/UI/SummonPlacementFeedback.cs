using UnityEngine;

namespace CrossDefense.UI
{
    public static class SummonPlacementFeedback
    {
        public static string BuildNoSpaceMessage(string unitName, int failureCount)
        {
            int count = Mathf.Max(1, failureCount);
            string subject = count > 1
                ? $"{count:N0}마리"
                : string.IsNullOrWhiteSpace(unitName)
                    ? "소환수"
                    : unitName;
            return $"{subject} 자동 배치 실패\n" +
                   "필드 공간이 부족해 보유 용병에 보관했습니다.";
        }

        public static string BuildSpawnFailureMessage(string unitName)
        {
            string subject = string.IsNullOrWhiteSpace(unitName) ? "소환수" : unitName;
            return $"{subject} 배치에 실패했습니다\n보유 용병에서 다시 배치해주세요.";
        }
    }
}
