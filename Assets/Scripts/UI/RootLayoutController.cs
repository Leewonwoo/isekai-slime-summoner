using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>UI 진입점 — 유일한 UI MonoBehaviour (ui-guidelines §7-1).
    /// UIDocument 하나에 RootLayout을 태우고 서브 컨트롤러를 생성한다.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class RootLayoutController : MonoBehaviour
    {
        [SerializeField] VisualTreeAsset upgradeRowTemplate;

        public TopHUDController TopHUD { get; private set; }
        public FieldOverlayController FieldOverlay { get; private set; }
        public BottomPanelController BottomPanel { get; private set; }

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            ApplySafeArea(root);
            TopHUD = new TopHUDController(root);
            FieldOverlay = new FieldOverlayController(root);
            BottomPanel = new BottomPanelController(root, upgradeRowTemplate);
            ApplyScaffoldDemoState();
        }

        /// <summary>스캐폴딩 확인용 더미 상태 — 게임 로직 연결 시 제거</summary>
        void ApplyScaffoldDemoState()
        {
            TopHUD.SetWave(1, 20);
            TopHUD.SetCoreHp(80, 100);
            TopHUD.SetGold(150);
            TopHUD.SetGems(12);
            FieldOverlay.SetBadge(Direction.North, 24, ThreatLevel.Danger);
            FieldOverlay.SetBadge(Direction.East, 8, ThreatLevel.Normal);
            FieldOverlay.SetBadge(Direction.South, 0, ThreatLevel.None);
            FieldOverlay.SetBadge(Direction.West, 4, ThreatLevel.Normal);
            BottomPanel.SetRedDot("upgrade", true);
        }

        // 세이프 에어리어 패딩 — ui-guidelines §5 인라인 스타일 허용 예외 ②
        void ApplySafeArea(VisualElement root)
        {
            var safe = Screen.safeArea;
            float panelPerPixel = 1080f / Screen.width; // Match Width(0) 기준 환산
            root.style.paddingTop = (Screen.height - safe.yMax) * panelPerPixel;
            root.style.paddingBottom = safe.yMin * panelPerPixel;
        }
    }
}
