using UnityEngine;
using UnityEngine.UIElements;
using CrossDefense.Core;
using System.Collections;
using System.Collections.Generic;

namespace CrossDefense.UI
{
    /// <summary>UI 진입점 — 유일한 UI MonoBehaviour (ui-guidelines §7-1).
    /// UIDocument 하나에 RootLayout을 태우고 서브 컨트롤러를 생성한다.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class RootLayoutController : MonoBehaviour
    {
        [SerializeField] VisualTreeAsset upgradeRowTemplate;
        GameManager _gameManager;
        VisualElement _root;

        public TopHUDController TopHUD { get; private set; }
        public FieldOverlayController FieldOverlay { get; private set; }
        public BottomPanelController BottomPanel { get; private set; }
        public SummonRouletteView SummonRoulette { get; private set; }

        void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            ApplySafeArea(_root);
            TopHUD = new TopHUDController(_root);
            FieldOverlay = new FieldOverlayController(_root);
            BottomPanel = new BottomPanelController(_root, upgradeRowTemplate);
            SummonRoulette = new SummonRouletteView(_root);
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
            if (_gameManager != null)
            {
                _gameManager.WaveChanged -= OnWaveChanged;
                _gameManager.GoldChanged -= OnGoldChanged;
                _gameManager.SummonContractsChanged -= OnSummonContractsChanged;
                _gameManager.LivingMonsterCountChanged -= OnLivingMonsterCountChanged;
                if (_gameManager.SummonManager != null)
                    _gameManager.SummonManager.BenchChanged -= OnBenchChanged;
                _gameManager.RegisterGoldScreenPositionProvider(null);
            }
            if (BottomPanel != null)
            {
                BottomPanel.SummonRequested -= OnSummonRequested;
                BottomPanel.BenchDragStarted -= OnBenchDragStarted;
                BottomPanel.BenchDragMoved -= OnBenchDragMoved;
                BottomPanel.BenchDragEnded -= OnBenchDragEnded;
            }
            TopHUD?.Dispose();
        }

        void BindGameManager()
        {
            _gameManager.WaveChanged += OnWaveChanged;
            _gameManager.GoldChanged += OnGoldChanged;
            _gameManager.SummonContractsChanged += OnSummonContractsChanged;
            _gameManager.LivingMonsterCountChanged += OnLivingMonsterCountChanged;
            if (_gameManager.SummonManager != null)
                _gameManager.SummonManager.BenchChanged += OnBenchChanged;
            BottomPanel.SummonRequested += OnSummonRequested;
            BottomPanel.BenchDragStarted += OnBenchDragStarted;
            BottomPanel.BenchDragMoved += OnBenchDragMoved;
            BottomPanel.BenchDragEnded += OnBenchDragEnded;
            _gameManager.RegisterGoldScreenPositionProvider(TopHUD.GetGoldScreenPosition);
            if (_gameManager.StageTimeline != null)
                TopHUD.SetStageName(_gameManager.StageTimeline.DisplayName);
            OnWaveChanged(_gameManager.CurrentWave, _gameManager.TotalWaves);
            OnLivingMonsterCountChanged(_gameManager.LivingMonsterCount);
            OnGoldChanged(_gameManager.Gold);
            OnSummonContractsChanged(_gameManager.SummonContracts);
            OnBenchChanged(_gameManager.SummonManager?.Bench);
        }

        void OnWaveChanged(int current, int total) => FieldOverlay.SetWave(current, _gameManager.LivingMonsterCount);
        void OnLivingMonsterCountChanged(int count) => FieldOverlay.SetWave(_gameManager.CurrentWave, count);
        void OnGoldChanged(int amount) => TopHUD.SetGold(amount);
        void OnSummonContractsChanged(int amount) => BottomPanel.SetSummonContracts(amount);
        void OnBenchChanged(IReadOnlyList<SummonUnitInstance> units) => BottomPanel.SetBench(units);

        void OnBenchDragStarted(SummonUnitInstance instance, Vector2 panelPosition)
        {
            _gameManager?.SummonedUnitManager?.BeginBenchDrag(instance, PanelToScreen(panelPosition));
        }

        void OnBenchDragMoved(SummonUnitInstance _, Vector2 panelPosition)
        {
            _gameManager?.SummonedUnitManager?.UpdateBenchDrag(PanelToScreen(panelPosition));
        }

        void OnBenchDragEnded(SummonUnitInstance _, Vector2 panelPosition)
        {
            _gameManager?.SummonedUnitManager?.EndBenchDrag(PanelToScreen(panelPosition));
        }

        Vector2 PanelToScreen(Vector2 panelPosition)
        {
            float panelWidth = _root?.resolvedStyle.width ?? Screen.width;
            float pixelsPerPoint = panelWidth > 0f && !float.IsNaN(panelWidth)
                ? Screen.width / panelWidth
                : 1f;
            return new Vector2(
                panelPosition.x * pixelsPerPoint,
                Screen.height - panelPosition.y * pixelsPerPoint);
        }

        void OnSummonRequested()
        {
            if (_gameManager == null || _gameManager.SummonManager == null ||
                BottomPanel.IsSummonAnimating || SummonRoulette == null || SummonRoulette.IsPlaying)
                return;
            if (!_gameManager.SummonManager.TryBeginSummon(out var result))
                return;

            BottomPanel.SetSummonAnimationState(true);
            SummonRoulette.Play(
                result,
                () =>
                {
                    _gameManager.SummonManager.CommitPending(result);
                    BottomPanel.SetSummonAnimationState(false);
                });
        }

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
