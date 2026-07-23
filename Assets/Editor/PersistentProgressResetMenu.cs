using CrossDefense.Core;
using UnityEditor;
using UnityEngine;

namespace CrossDefense.Editor
{
    public static class PersistentProgressResetMenu
    {
        const string MenuPath = "Cross Defense/Debug/영구 진행 초기화";

        [MenuItem(MenuPath)]
        static void ResetPersistentProgress()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "영구 진행 초기화",
                "소환사 레벨·영구 특성·스킬 장착·몬스터 도감·장비를 초기화합니다.\n현재 런 데이터도 처음부터 다시 시작합니다.",
                "초기화",
                "취소");
            if (!confirmed)
                return;

            if (EditorApplication.isPlaying)
            {
                GameManager game = Object.FindFirstObjectByType<GameManager>();
                if (game != null)
                {
                    game.ResetAllProgressAndRestart();
                    Debug.Log("[CrossDefense] 영구 진행 초기화를 예약하고 현재 스테이지를 다시 시작합니다.");
                    return;
                }
            }

            int deletedCount = PersistentProgressReset.ResetNow();
            Debug.Log($"[CrossDefense] 영구 진행 초기화 완료 ({deletedCount}개 저장 키 삭제).");
        }
    }
}
