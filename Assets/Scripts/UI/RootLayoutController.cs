using UnityEngine;
using UnityEngine.UIElements;
using CrossDefense.Core;
using System.Collections;

namespace CrossDefense.UI
{
    /// <summary>UI 진입점 — 유일한 UI MonoBehaviour (ui-guidelines §7-1).
    /// UIDocument 하나에 RootLayout을 태우고 서브 컨트롤러를 생성한다.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class RootLayoutController : MonoBehaviour
    {
        [SerializeField] VisualTreeAsset upgradeRowTemplate;
        GameManager _gameManager;

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
            _gameManager = FindFirstObjectByType<GameManager>();
            if (_gameManager == null)
                ApplyScaffoldDemoState();
            else
                StartCoroutine(BindGameManagerNextFrame());
        }

        IEnumerator BindGameManagerNextFrame()
        {
            yield return null;
            if (isActiveAndEnabled && _gameManager != null)
                BindGameManager();
        }

        void OnDisable()
        {
            if (_gameManager == null) return;
            _gameManager.WaveChanged -= OnWaveChanged;
            _gameManager.GoldChanged -= OnGoldChanged;
            _gameManager.SummonContractsChanged -= OnSummonContractsChanged;
            _gameManager.LivingMonsterCountChanged -= OnLivingMonsterCountChanged;
        }

        void BindGameManager()
        {
            _gameManager.WaveChanged += OnWaveChanged;
            _gameManager.GoldChanged += OnGoldChanged;
            _gameManager.SummonContractsChanged += OnSummonContractsChanged;
            _gameManager.LivingMonsterCountChanged += OnLivingMonsterCountChanged;
            if (_gameManager.StageTimeline != null)
                TopHUD.SetStageName(_gameManager.StageTimeline.DisplayName);
            OnWaveChanged(_gameManager.CurrentWave, _gameManager.TotalWaves);
            OnLivingMonsterCountChanged(_gameManager.LivingMonsterCount);
            OnGoldChanged(_gameManager.Gold);
            OnSummonContractsChanged(_gameManager.SummonContracts);
        }

        void OnWaveChanged(int current, int total) => FieldOverlay.SetWave(current, _gameManager.LivingMonsterCount);
        void OnLivingMonsterCountChanged(int count) => FieldOverlay.SetWave(_gameManager.CurrentWave, count);
        void OnGoldChanged(int amount) => TopHUD.SetGold(amount);
        void OnSummonContractsChanged(int amount) => BottomPanel.SetSummonContracts(amount);

        /// <summary>스캐폴딩 확인용 더미 상태 — 게임 로직 연결 시 제거</summary>
        void ApplyScaffoldDemoState()
        {
            TopHUD.SetSummonerProfile("위대한 소환사", 12);
            TopHUD.SetStageName("고블린 숲");
            FieldOverlay.SetWave(1, 24);
            TopHUD.SetGold(150);
            TopHUD.SetGems(12);
            BottomPanel.SetSummonContracts(10);
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
