using UnityEngine;
using UnityEngine.UIElements;
using CrossDefense.Core;
using CrossDefense.Units;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CrossDefense.Data;

namespace CrossDefense.UI
{
    /// <summary>UI 진입점 — 유일한 UI MonoBehaviour (ui-guidelines §7-1).
    /// UIDocument 하나에 RootLayout을 태우고 서브 컨트롤러를 생성한다.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class RootLayoutController : MonoBehaviour
    {
        [SerializeField] VisualTreeAsset upgradeRowTemplate;
        [SerializeField] TraitChoicePopupController traitChoicePopupPrefab;
        GameManager _gameManager;
        TraitChoicePopupController _traitChoicePopup;
        bool _traitChoicePopupOpen;
        VisualElement _root;
        readonly List<SummonUnitInstance> _deployedUnitBuffer = new();
        readonly List<GrowthRowModel> _growthRowBuffer = new();
        readonly List<GrowthRowModel> _summonerRowBuffer = new();

        public TopHUDController TopHUD { get; private set; }
        public FieldOverlayController FieldOverlay { get; private set; }
        public BottomPanelController BottomPanel { get; private set; }
        public SummonRouletteView SummonRoulette { get; private set; }
        public SummonUnitDetailView SummonUnitDetail { get; private set; }

        void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            ApplySafeArea(_root);
            TopHUD = new TopHUDController(_root);
            FieldOverlay = new FieldOverlayController(_root);
            BottomPanel = new BottomPanelController(_root, upgradeRowTemplate);
            SummonRoulette = new SummonRouletteView(_root);
            SummonUnitDetail = new SummonUnitDetailView(_root);
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
                _gameManager.CoreHpChanged -= OnCoreHpChanged;
                _gameManager.LivingMonsterCountChanged -= OnLivingMonsterCountChanged;
                if (_gameManager.SummonManager != null)
                {
                    _gameManager.SummonManager.BenchChanged -= OnBenchChanged;
                    _gameManager.SummonManager.UnitUpgradeChanged -= OnUnitUpgradeChanged;
                }
                if (_gameManager.SummonedUnitManager != null)
                    _gameManager.SummonedUnitManager.UnitsChanged -= OnUnitsChanged;
                if (_gameManager.Growth != null)
                    _gameManager.Growth.Changed -= OnGrowthChanged;
                if (_gameManager.SummonerProgression != null)
                    _gameManager.SummonerProgression.Changed -= OnSummonerProgressionChanged;
                if (_gameManager.PermanentTraits != null)
                    _gameManager.PermanentTraits.Changed -= OnPermanentTraitsChanged;
                if (_gameManager.RunTraits != null)
                    _gameManager.RunTraits.Changed -= OnRunTraitsChanged;
                _gameManager.RegisterGoldScreenPositionProvider(null);
            }
            if (BottomPanel != null)
            {
                BottomPanel.SummonRequested -= OnSummonRequested;
                BottomPanel.BenchSlotSelected -= OnBenchSlotSelected;
                BottomPanel.BenchDragStarted -= OnBenchDragStarted;
                BottomPanel.BenchDragMoved -= OnBenchDragMoved;
                BottomPanel.BenchDragEnded -= OnBenchDragEnded;
                BottomPanel.RunUpgradeRequested -= OnRunUpgradeRequested;
                BottomPanel.SlimeLevelUpRequested -= OnSlimeLevelUpRequested;
                BottomPanel.SummonerLevelUpRequested -= OnSummonerLevelUpRequested;
                BottomPanel.Dispose();
            }
            _traitChoicePopupOpen = false;
            _traitChoicePopup?.Hide();
            SummonUnitDetail?.Dispose();
            TopHUD?.Dispose();
        }

        void BindGameManager()
        {
            _gameManager.WaveChanged += OnWaveChanged;
            _gameManager.GoldChanged += OnGoldChanged;
            _gameManager.SummonContractsChanged += OnSummonContractsChanged;
            _gameManager.CoreHpChanged += OnCoreHpChanged;
            _gameManager.LivingMonsterCountChanged += OnLivingMonsterCountChanged;
            if (_gameManager.SummonManager != null)
            {
                _gameManager.SummonManager.BenchChanged += OnBenchChanged;
                _gameManager.SummonManager.UnitUpgradeChanged += OnUnitUpgradeChanged;
            }
            if (_gameManager.SummonedUnitManager != null)
                _gameManager.SummonedUnitManager.UnitsChanged += OnUnitsChanged;
            if (_gameManager.Growth != null)
                _gameManager.Growth.Changed += OnGrowthChanged;
            if (_gameManager.SummonerProgression != null)
                _gameManager.SummonerProgression.Changed += OnSummonerProgressionChanged;
            if (_gameManager.PermanentTraits != null)
                _gameManager.PermanentTraits.Changed += OnPermanentTraitsChanged;
            if (_gameManager.RunTraits != null)
                _gameManager.RunTraits.Changed += OnRunTraitsChanged;
            BottomPanel.SummonRequested += OnSummonRequested;
            BottomPanel.BenchSlotSelected += OnBenchSlotSelected;
            BottomPanel.BenchDragStarted += OnBenchDragStarted;
            BottomPanel.BenchDragMoved += OnBenchDragMoved;
            BottomPanel.BenchDragEnded += OnBenchDragEnded;
            BottomPanel.RunUpgradeRequested += OnRunUpgradeRequested;
            BottomPanel.SlimeLevelUpRequested += OnSlimeLevelUpRequested;
            BottomPanel.SummonerLevelUpRequested += OnSummonerLevelUpRequested;
            _gameManager.RegisterGoldScreenPositionProvider(TopHUD.GetGoldScreenPosition);
            if (_gameManager.StageTimeline != null)
                TopHUD.SetStageName(_gameManager.StageTimeline.DisplayName);
            OnWaveChanged(_gameManager.CurrentWave, _gameManager.TotalWaves);
            OnLivingMonsterCountChanged(_gameManager.LivingMonsterCount);
            OnGoldChanged(_gameManager.Gold);
            OnSummonContractsChanged(_gameManager.SummonContracts);
            RefreshOwnedUnits();
            OnSummonerProgressionChanged(_gameManager.SummonerProgression.Snapshot);
            RefreshGrowthUI();
            TryShowTraitChoice();
        }

        void OnWaveChanged(int current, int total) => FieldOverlay.SetWave(current, _gameManager.LivingMonsterCount);
        void OnLivingMonsterCountChanged(int count) => FieldOverlay.SetWave(_gameManager.CurrentWave, count);
        void OnGoldChanged(int amount)
        {
            TopHUD.SetGold(amount);
            RefreshGrowthUI();
        }
        void OnSummonContractsChanged(int amount) => BottomPanel.SetSummonContracts(amount);
        void OnCoreHpChanged(float _, float __) => RefreshGrowthUI();
        void OnBenchChanged(IReadOnlyList<SummonUnitInstance> _)
        {
            RefreshOwnedUnits();
            RefreshGrowthUI();
        }
        void OnUnitsChanged(IReadOnlyList<SummonedUnitController> _)
        {
            RefreshOwnedUnits();
            RefreshGrowthUI();
        }
        void OnUnitUpgradeChanged(SummonUnitUpgradeState _) => RefreshGrowthUI();
        void OnGrowthChanged() => RefreshGrowthUI();

        void OnSummonerProgressionChanged(SummonerProgressionSnapshot snapshot)
        {
            TopHUD.SetSummonerProfile("위대한 소환사", snapshot.Level);
            BottomPanel.SetDirectRankOneChance(_gameManager.DirectRankOneChance);
            RefreshSummonerUI(snapshot);
            TryShowTraitChoice();
        }

        void OnPermanentTraitsChanged(PermanentTraitSnapshot snapshot)
        {
            BottomPanel.SetDirectRankOneChance(_gameManager.DirectRankOneChance);
            RefreshSummonerUI(_gameManager.SummonerProgression.Snapshot);
        }

        void OnRunTraitsChanged(RunTraitSnapshot snapshot)
        {
            RefreshSummonerUI(_gameManager.SummonerProgression.Snapshot);
            if (snapshot.IsChoicePending)
                TryShowTraitChoice();
        }

        void RefreshOwnedUnits()
        {
            _deployedUnitBuffer.Clear();
            var controllers = _gameManager?.SummonedUnitManager?.Units;
            if (controllers != null)
            {
                for (int i = 0; i < controllers.Count; i++)
                {
                    var instance = controllers[i]?.Instance;
                    if (instance != null)
                        _deployedUnitBuffer.Add(instance);
                }
            }
            BottomPanel?.SetOwnedUnits(_gameManager?.SummonManager?.Bench, _deployedUnitBuffer);
        }

        void RefreshGrowthUI()
        {
            var growth = _gameManager?.Growth;
            var summonManager = _gameManager?.SummonManager;
            if (growth == null || summonManager == null || BottomPanel == null) return;

            _growthRowBuffer.Clear();
            AddRunUpgradeRow(RunUpgradeType.AttackPower, "전체 공격력", "row__icon--atk");
            AddRunUpgradeRow(RunUpgradeType.AttackSpeed, "전체 공격 속도", "row__icon--aspd");
            AddRunUpgradeRow(RunUpgradeType.CoreRecovery, "소환사 HP 회복", "row__icon--hp");
            AddRunUpgradeRow(RunUpgradeType.CriticalChance, "치명타 확률", "row__icon--crit");

            var ownedById = new Dictionary<string, SummonUnitInstance>();
            AddOwnedUnitTypes(summonManager.Bench, ownedById);
            AddOwnedUnitTypes(_deployedUnitBuffer, ownedById);
            foreach (var pair in ownedById.OrderBy(pair => pair.Value.Unit.DisplayName))
            {
                var instance = pair.Value;
                var state = summonManager.GetUnitUpgradeState(pair.Key);
                bool maxed = state.Level >= growth.Balance.SlimeMaxLevel;
                int nextLevel = maxed ? state.Level : state.Level + 1;
                int cost = growth.GetSlimeLevelUpCost(pair.Key);
                string values =
                    $"공격 {UIFormat.PercentDelta(state.DamageMultiplier, growth.Balance.SlimeDamageMultiplier(nextLevel))}" +
                    $" · 공속 {UIFormat.PercentDelta(state.AttackSpeedMultiplier, growth.Balance.SlimeAttackSpeedMultiplier(nextLevel))}";
                _growthRowBuffer.Add(new GrowthRowModel(
                    $"slime:{pair.Key}",
                    GrowthRowKind.SlimeLevel,
                    $"{instance.Unit.DisplayName} Lv.{state.Level:N0}",
                    values,
                    maxed ? "MAX" : $"{cost:N0} G",
                    "row__icon--atk",
                    !maxed && growth.CanLevelUpSlime(pair.Key),
                    unitId: pair.Key));
            }

            BottomPanel.SetGrowthRows(_growthRowBuffer);
            BottomPanel.SetRedDot("upgrade", _growthRowBuffer.Any(row => row.CanPurchase));
        }

        void AddRunUpgradeRow(RunUpgradeType type, string name, string iconClass)
        {
            GrowthManager growth = _gameManager.Growth;
            int level = growth.GetRunUpgradeLevel(type);
            bool maxed = level >= growth.Balance.RunUpgradeMaxLevel;
            int nextLevel = maxed ? level : level + 1;
            string values = type switch
            {
                RunUpgradeType.AttackPower => UIFormat.PercentDelta(
                    growth.Balance.RunAttackPowerMultiplier(level),
                    growth.Balance.RunAttackPowerMultiplier(nextLevel)),
                RunUpgradeType.AttackSpeed => UIFormat.PercentDelta(
                    growth.Balance.RunAttackSpeedMultiplier(level),
                    growth.Balance.RunAttackSpeedMultiplier(nextLevel)),
                RunUpgradeType.CoreRecovery => UIFormat.HpDelta(
                    growth.Balance.CoreRecoveryAmount(maxed ? level : level + 1),
                    growth.Balance.CoreRecoveryAmount(maxed ? level : level + 2)),
                RunUpgradeType.CriticalChance => UIFormat.ChanceDelta(
                    growth.Balance.RunCriticalChance(level),
                    growth.Balance.RunCriticalChance(nextLevel)),
                _ => string.Empty,
            };
            int cost = growth.GetRunUpgradeCost(type);
            _growthRowBuffer.Add(new GrowthRowModel(
                $"run:{type}",
                GrowthRowKind.RunUpgrade,
                $"{name} Lv.{level:N0}",
                values,
                maxed ? "MAX" : $"{cost:N0} G",
                iconClass,
                !maxed && growth.CanPurchaseRunUpgrade(type),
                type));
        }

        void RefreshSummonerUI(SummonerProgressionSnapshot snapshot)
        {
            if (_gameManager == null || BottomPanel == null) return;
            float currentJackpot = _gameManager.DirectRankOneChance;
            float baseJackpot = currentJackpot - snapshot.JackpotChanceBonus;
            float nextJackpot = Mathf.Clamp01(baseJackpot + snapshot.NextJackpotChanceBonus);
            _summonerRowBuffer.Clear();
            _summonerRowBuffer.Add(ReadOnlySummonerRow(
                "summoner:damage",
                "영구 공격력",
                UIFormat.PercentDelta(snapshot.DamageMultiplier, snapshot.NextDamageMultiplier),
                "row__icon--atk"));
            _summonerRowBuffer.Add(ReadOnlySummonerRow(
                "summoner:hp",
                "영구 최대 HP",
                UIFormat.PercentDelta(snapshot.MaxHpMultiplier, snapshot.NextMaxHpMultiplier),
                "row__icon--hp"));
            _summonerRowBuffer.Add(ReadOnlySummonerRow(
                "summoner:jackpot",
                "★1 직행 확률",
                UIFormat.ChanceDelta(currentJackpot, nextJackpot),
                "row__icon--crit"));

            PermanentTraitProgression traits = _gameManager.PermanentTraits;
            if (traits != null)
            {
                foreach (PermanentTraitType type in System.Enum.GetValues(typeof(PermanentTraitType)))
                {
                    int level = traits.GetLevel(type);
                    if (level <= 0) continue;
                    _summonerRowBuffer.Add(ReadOnlySummonerRow(
                        $"trait:{type}",
                        $"{traits.GetDisplayName(type)} Lv.{level:N0}",
                        traits.GetCurrentEffect(type),
                        TraitIconClass(type)));
                }
            }

            RunTraitProgression runTraits = _gameManager.RunTraits;
            if (runTraits != null)
            {
                foreach (RunTraitType type in System.Enum.GetValues(typeof(RunTraitType)))
                {
                    int level = runTraits.GetLevel(type);
                    if (level <= 0) continue;
                    _summonerRowBuffer.Add(ReadOnlySummonerRow(
                        $"run-trait:{type}",
                        $"{runTraits.GetDisplayName(type)} Lv.{level:N0}",
                        runTraits.GetCurrentEffect(type),
                        RunTraitIconClass(type),
                        "이번 런"));
                }
            }
            BottomPanel.SetSummonerProgression(snapshot, _summonerRowBuffer);
            BottomPanel.SetRedDot(
                "summoner",
                snapshot.CanLevelUp || (_gameManager.PermanentTraits?.PendingChoiceCount ?? 0) > 0);
        }

        static string TraitIconClass(PermanentTraitType type) =>
            type switch
            {
                PermanentTraitType.CoreVitality => "row__icon--hp",
                PermanentTraitType.LuckySummon => "row__icon--crit",
                PermanentTraitType.SummonerHaste or PermanentTraitType.SlimeHaste => "row__icon--aspd",
                _ => "row__icon--atk",
            };

        static string RunTraitIconClass(RunTraitType type) =>
            type switch
            {
                RunTraitType.CoreVitality => "row__icon--hp",
                RunTraitType.CriticalFocus => "row__icon--crit",
                RunTraitType.AllAttackSpeed => "row__icon--aspd",
                _ => "row__icon--atk",
            };

        static GrowthRowModel ReadOnlySummonerRow(
            string key,
            string name,
            string values,
            string iconClass,
            string actionText = "영구") =>
            new(key, GrowthRowKind.ReadOnly, name, values, actionText, iconClass, false);

        static void AddOwnedUnitTypes(
            IReadOnlyList<SummonUnitInstance> units,
            Dictionary<string, SummonUnitInstance> output)
        {
            if (units == null) return;
            for (int i = 0; i < units.Count; i++)
            {
                SummonUnitInstance instance = units[i];
                if (instance?.Unit == null || string.IsNullOrWhiteSpace(instance.Unit.UnitId) ||
                    output.ContainsKey(instance.Unit.UnitId))
                    continue;
                output.Add(instance.Unit.UnitId, instance);
            }
        }

        void OnRunUpgradeRequested(RunUpgradeType type) =>
            _gameManager?.Growth?.TryPurchaseRunUpgrade(type);

        void OnSlimeLevelUpRequested(string unitId) =>
            _gameManager?.Growth?.TryLevelUpSlime(unitId);

        void OnSummonerLevelUpRequested() =>
            _gameManager?.SummonerProgression?.TryLevelUp();

        void TryShowTraitChoice()
        {
            if (_traitChoicePopupOpen || _gameManager == null)
                return;
            if (traitChoicePopupPrefab == null)
            {
                Debug.LogWarning("[CrossDefense] TraitChoicePopup prefab reference is missing.", this);
                return;
            }

            if (_traitChoicePopup == null)
                _traitChoicePopup = Instantiate(traitChoicePopupPrefab, transform);

            RunTraitProgression runTraits = _gameManager.RunTraits;
            if (runTraits != null && runTraits.IsChoicePending)
            {
                IReadOnlyList<RunTraitChoice> runChoices = runTraits.GetCurrentChoices();
                if (runChoices.Count != 3)
                    return;
                _traitChoicePopupOpen = true;
                _traitChoicePopup.Show(
                    runChoices,
                    runTraits.ClearedWave,
                    OnRunTraitChoiceConfirmed);
                return;
            }

            PermanentTraitProgression traits = _gameManager.PermanentTraits;
            if (traits == null || traits.PendingChoiceCount <= 0)
                return;
            IReadOnlyList<PermanentTraitChoice> choices = traits.GetCurrentChoices();
            if (choices.Count != 3)
                return;
            _traitChoicePopupOpen = true;
            _traitChoicePopup.Show(choices, traits.PendingChoiceCount, OnTraitChoiceConfirmed);
        }

        void OnTraitChoiceConfirmed(PermanentTraitType type)
        {
            _traitChoicePopupOpen = false;
            if (_gameManager?.PermanentTraits?.TryChoose(type) != true)
                return;
            StartCoroutine(ShowNextTraitChoiceNextFrame());
        }

        void OnRunTraitChoiceConfirmed(RunTraitType type)
        {
            _traitChoicePopupOpen = false;
            if (_gameManager?.RunTraits?.TryChoose(type) != true)
                return;
            StartCoroutine(ShowNextTraitChoiceNextFrame());
        }

        IEnumerator ShowNextTraitChoiceNextFrame()
        {
            yield return null;
            if (isActiveAndEnabled)
                TryShowTraitChoice();
        }

        void OnBenchSlotSelected(SummonUnitInstance instance, int quantity) =>
            SummonUnitDetail?.Show(instance, quantity);

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
