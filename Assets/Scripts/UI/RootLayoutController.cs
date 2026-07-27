using UnityEngine;
using UnityEngine.InputSystem;
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
        IReadOnlyList<SummonResult> _runRewardSummonResults;
        int _runRewardSummonResultIndex;
        int _lastUnlockLevel;

        public TopHUDController TopHUD { get; private set; }
        public FieldOverlayController FieldOverlay { get; private set; }
        public BottomPanelController BottomPanel { get; private set; }
        public SummonRouletteView SummonRoulette { get; private set; }
        public SummonUnitDetailView SummonUnitDetail { get; private set; }
        public SlimeCodexModalController SlimeCodexModal { get; private set; }
        public MonsterCodexModalController MonsterCodexModal { get; private set; }
        public MerchantModalController MerchantModal { get; private set; }

        void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            ApplySafeArea(_root);
            TopHUD = new TopHUDController(_root);
            FieldOverlay = new FieldOverlayController(_root);
            BottomPanel = new BottomPanelController(_root, upgradeRowTemplate);
            SummonRoulette = new SummonRouletteView(_root);
            SummonUnitDetail = new SummonUnitDetailView(_root);
            SlimeCodexModal = new SlimeCodexModalController(_root);
            MonsterCodexModal = new MonsterCodexModalController(_root);
            MerchantModal = new MerchantModalController(_root);
            SummonUnitDetail.LevelUpRequested += OnSlimeLevelUpRequested;
            SlimeCodexModal.CloseRequested += OnSlimeCodexCloseRequested;
            MonsterCodexModal.CloseRequested += OnCodexCloseRequested;
            MerchantModal.CloseRequested += OnMerchantCloseRequested;
            MerchantModal.PurchaseRequested += OnMerchantPurchaseRequested;
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
            _runRewardSummonResults = null;
            _runRewardSummonResultIndex = 0;
            SummonRoulette?.Dispose();
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
                if (_gameManager.CombatBuild != null)
                    _gameManager.CombatBuild.Changed -= OnCombatBuildChanged;
                if (_gameManager.SummonerSkills != null)
                    _gameManager.SummonerSkills.StateChanged -= RefreshSkillUI;
                if (_gameManager.SummonerBuffs != null)
                    _gameManager.SummonerBuffs.StateChanged -= RefreshSkillUI;
                if (_gameManager.Dopamine != null)
                {
                    _gameManager.Dopamine.StateChanged -= OnDopamineStateChanged;
                    _gameManager.Dopamine.ComboCashedOut -= OnComboCashedOut;
                }
                if (_gameManager.Equipment != null)
                    _gameManager.Equipment.Changed -= OnEquipmentChanged;
                if (_gameManager.RunRelics != null)
                    _gameManager.RunRelics.Changed -= OnRunRelicsChanged;
                if (_gameManager.Relics != null)
                    _gameManager.Relics.Changed -= RefreshSkillUI;
                _gameManager.GameplaySpeedChanged -= OnGameplaySpeedChanged;
                _gameManager.RegisterGoldScreenPositionProvider(null);
                _gameManager.SetGameplayPause(GameplayPauseReason.TraitChoice, false);
                _gameManager.SetGameplayPause(GameplayPauseReason.SummonRoulette, false);
                _gameManager.SetGameplayPause(GameplayPauseReason.SlimeCodex, false);
                _gameManager.SetGameplayPause(GameplayPauseReason.MonsterCodex, false);
                _gameManager.SetGameplayPause(GameplayPauseReason.Merchant, false);
            }
            if (BottomPanel != null)
            {
                BottomPanel.SummonRequested -= OnSummonRequested;
                BottomPanel.BenchSlotSelected -= OnBenchSlotSelected;
                BottomPanel.BenchDragStarted -= OnBenchDragStarted;
                BottomPanel.BenchDragMoved -= OnBenchDragMoved;
                BottomPanel.BenchDragEnded -= OnBenchDragEnded;
                BottomPanel.RunUpgradeRequested -= OnRunUpgradeRequested;
                BottomPanel.SkillEquipRequested -= OnSkillEquipRequested;
                BottomPanel.RelicSkillEquipRequested -= OnRelicSkillEquipRequested;
                BottomPanel.EquipmentEquipRequested -= OnEquipmentEquipRequested;
                BottomPanel.Dispose();
            }
            if (FieldOverlay != null)
            {
                FieldOverlay.SkillRequested -= OnSkillRequested;
                FieldOverlay.BuffSkillRequested -= OnBuffSkillRequested;
                FieldOverlay.SpeedToggleRequested -= OnSpeedToggleRequested;
                FieldOverlay.SlimeCodexRequested -= OnSlimeCodexRequested;
                FieldOverlay.MonsterCodexRequested -= OnCodexRequested;
            }
            _traitChoicePopupOpen = false;
            _traitChoicePopup?.Hide();
            if (SummonUnitDetail != null)
            {
                SummonUnitDetail.LevelUpRequested -= OnSlimeLevelUpRequested;
                SummonUnitDetail.Dispose();
            }
            TopHUD?.Dispose();
            FieldOverlay?.Dispose();
            if (SlimeCodexModal != null)
            {
                SlimeCodexModal.CloseRequested -= OnSlimeCodexCloseRequested;
                SlimeCodexModal.Dispose();
            }
            if (MonsterCodexModal != null)
            {
                MonsterCodexModal.CloseRequested -= OnCodexCloseRequested;
                MonsterCodexModal.Dispose();
            }
            if (MerchantModal != null)
            {
                MerchantModal.CloseRequested -= OnMerchantCloseRequested;
                MerchantModal.PurchaseRequested -= OnMerchantPurchaseRequested;
                MerchantModal.Dispose();
            }
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
            if (_gameManager.CombatBuild != null)
                _gameManager.CombatBuild.Changed += OnCombatBuildChanged;
            if (_gameManager.SummonerSkills != null)
                _gameManager.SummonerSkills.StateChanged += RefreshSkillUI;
            if (_gameManager.SummonerBuffs != null)
                _gameManager.SummonerBuffs.StateChanged += RefreshSkillUI;
            if (_gameManager.Dopamine != null)
            {
                _gameManager.Dopamine.StateChanged += OnDopamineStateChanged;
                _gameManager.Dopamine.ComboCashedOut += OnComboCashedOut;
            }
            if (_gameManager.Equipment != null)
                _gameManager.Equipment.Changed += OnEquipmentChanged;
            if (_gameManager.RunRelics != null)
                _gameManager.RunRelics.Changed += OnRunRelicsChanged;
            if (_gameManager.Relics != null)
                _gameManager.Relics.Changed += RefreshSkillUI;
            _gameManager.GameplaySpeedChanged += OnGameplaySpeedChanged;
            FieldOverlay.SkillRequested += OnSkillRequested;
            FieldOverlay.BuffSkillRequested += OnBuffSkillRequested;
            FieldOverlay.SpeedToggleRequested += OnSpeedToggleRequested;
            FieldOverlay.SlimeCodexRequested += OnSlimeCodexRequested;
            FieldOverlay.MonsterCodexRequested += OnCodexRequested;
            BottomPanel.SummonRequested += OnSummonRequested;
            BottomPanel.BenchSlotSelected += OnBenchSlotSelected;
            BottomPanel.BenchDragStarted += OnBenchDragStarted;
            BottomPanel.BenchDragMoved += OnBenchDragMoved;
            BottomPanel.BenchDragEnded += OnBenchDragEnded;
            BottomPanel.RunUpgradeRequested += OnRunUpgradeRequested;
            BottomPanel.SkillEquipRequested += OnSkillEquipRequested;
            BottomPanel.RelicSkillEquipRequested += OnRelicSkillEquipRequested;
            BottomPanel.EquipmentEquipRequested += OnEquipmentEquipRequested;
            SlimeCodexModal.Bind(_gameManager.SummonManager.Pool, _gameManager.SummonerProgression);
            MonsterCodexModal.Bind(_gameManager.MonsterCatalog, _gameManager.MonsterCodex);
            MerchantModal.Bind(_gameManager.Merchant);
            _gameManager.RegisterGoldScreenPositionProvider(TopHUD.GetGoldScreenPosition);
            if (_gameManager.StageTimeline != null)
                TopHUD.SetStageName(_gameManager.StageTimeline.DisplayName);
            OnWaveChanged(_gameManager.CurrentWave, _gameManager.TotalWaves);
            OnLivingMonsterCountChanged(_gameManager.LivingMonsterCount);
            OnGoldChanged(_gameManager.Gold);
            OnSummonContractsChanged(_gameManager.SummonContracts);
            RefreshOwnedUnits();
            _lastUnlockLevel = _gameManager.SummonerProgression.Snapshot.Level;
            OnSummonerProgressionChanged(_gameManager.SummonerProgression.Snapshot);
            RefreshEquipmentUI();
            RefreshGrowthUI();
            RefreshSkillUI();
            RefreshDopamineUI();
            FieldOverlay.SetGameplaySpeed(_gameManager.GameplaySpeed);
            if (_gameManager.CombatBuild != null)
                OnCombatBuildChanged(_gameManager.CombatBuild.Snapshot);
            TryShowTraitChoice();
        }

        void OnWaveChanged(int current, int total)
        {
            FieldOverlay.SetWaveKind(_gameManager.CurrentWaveData?.Kind ?? StageWaveKind.Normal);
            FieldOverlay.SetWave(current, _gameManager.LivingMonsterCount);
        }
        void OnLivingMonsterCountChanged(int count) => FieldOverlay.SetWave(_gameManager.CurrentWave, count);
        void OnSpeedToggleRequested() => _gameManager?.ToggleGameplaySpeed();
        void OnGameplaySpeedChanged(float speed) => FieldOverlay?.SetGameplaySpeed(speed);
        void OnComboCashedOut(int combo, float damage, int gold) =>
            FieldOverlay?.ShowUnlockToast(
                $"{combo:N0} COMBO 정산!\n전체 피해 {damage:N0} · 골드 +{gold:N0}");
        void OnGoldChanged(int amount)
        {
            TopHUD.SetGold(amount);
            RefreshGrowthUI();
            RefreshOpenUnitDetail();
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
        void OnUnitUpgradeChanged(SummonUnitUpgradeState _)
        {
            RefreshGrowthUI();
            RefreshOpenUnitDetail();
        }
        void OnGrowthChanged()
        {
            RefreshOwnedUnits();
            RefreshGrowthUI();
            if (_gameManager?.SummonerProgression != null)
                RefreshSummonerUI(_gameManager.SummonerProgression.Snapshot);
        }

        void OnSummonerProgressionChanged(SummonerProgressionSnapshot snapshot)
        {
            if (_lastUnlockLevel > 0 && snapshot.Level > _lastUnlockLevel && _gameManager?.SummonManager?.Pool != null)
            {
                var unlocked = new List<string>();
                foreach (SummonUnitData unit in _gameManager.SummonManager.Pool)
                    if (unit != null && unit.UnlockLevel > _lastUnlockLevel && unit.UnlockLevel <= snapshot.Level)
                        unlocked.Add(unit.DisplayName);
                if (unlocked.Count > 0)
                    FieldOverlay.ShowUnlockToast($"새 슬라임 해금!\n{string.Join(" · ", unlocked)}");
            }
            _lastUnlockLevel = snapshot.Level;
            BottomPanel.SetDirectRankOneChance(_gameManager.DirectRankOneChance);
            RefreshSummonerUI(snapshot);
            RefreshSkillUI();
            TryShowTraitChoice();
        }

        void OnSkillRequested() => _gameManager?.SummonerSkills?.PressSkillButton();
        void OnBuffSkillRequested(int slotIndex) =>
            _gameManager?.SummonerBuffs?.PressSkillButton(slotIndex);

        void OnSlimeCodexRequested()
        {
            SlimeCodexModal?.Show();
            _gameManager?.SetGameplayPause(GameplayPauseReason.SlimeCodex, true);
        }

        void OnSlimeCodexCloseRequested()
        {
            SlimeCodexModal?.Hide();
            _gameManager?.SetGameplayPause(GameplayPauseReason.SlimeCodex, false);
        }

        void OnCodexRequested()
        {
            MonsterCodexModal?.Show();
            _gameManager?.SetGameplayPause(GameplayPauseReason.MonsterCodex, true);
        }

        void OnCodexCloseRequested()
        {
            MonsterCodexModal?.Hide();
            _gameManager?.SetGameplayPause(GameplayPauseReason.MonsterCodex, false);
        }

        void OnMerchantCloseRequested() => _gameManager?.CloseMerchant();

        void OnMerchantPurchaseRequested(int index)
        {
            _gameManager?.Merchant?.TryPurchase(index);
            MerchantModal?.Refresh();
            RefreshEquipmentUI();
        }

        void OnEquipmentEquipRequested(string id)
        {
            if (_gameManager?.Equipment?.TryEquip(id) == true)
                RefreshEquipmentUI();
        }

        void OnEquipmentChanged()
        {
            RefreshEquipmentUI();
            RefreshSummonerUI(_gameManager.SummonerProgression.Snapshot);
        }

        void OnRunRelicsChanged()
        {
            RefreshEquipmentUI();
            RefreshSummonerUI(_gameManager.SummonerProgression.Snapshot);
        }

        void RefreshEquipmentUI() =>
            BottomPanel?.SetEquipmentData(
                _gameManager?.Equipment,
                _gameManager?.RunRelics,
                BuildRunTraitText(_gameManager?.RunTraits, _gameManager?.CombatBuild));

        void OnSkillEquipRequested(SummonerBuffId id)
        {
            if (_gameManager?.SummonerBuffLoadout?.TryToggle(id) == true)
                RefreshSkillUI();
        }

        void OnRelicSkillEquipRequested(RelicFamily family)
        {
            if (_gameManager?.Relics?.TryEquip(family) == true)
                RefreshSkillUI();
        }

        void RefreshSkillUI()
        {
            if (_gameManager?.SummonerSkills == null ||
                _gameManager.SummonerBuffs == null ||
                _gameManager.SummonerProgression == null)
                return;
            SummonerSkillController skills = _gameManager.SummonerSkills;
            FieldOverlay.SetSkillState(
                skills.EquippedDefinition,
                skills.RemainingCooldown,
                skills.IsTargeting);
            BottomPanel.SetSkillRows(
                _gameManager.SummonerProgression.Snapshot.Level,
                _gameManager.SummonerBuffLoadout?.Equipped);
            BottomPanel.SetRelicData(_gameManager.Relics);
            for (int i = 0; i < SummonerBuffCatalog.MaxEquipped; i++)
            {
                SummonerBuffId? id = _gameManager.SummonerBuffs.EquippedAt(i);
                FieldOverlay.SetBuffSkillState(
                    i,
                    id.HasValue ? SummonerBuffCatalog.Get(id.Value) : null,
                    id.HasValue
                        ? _gameManager.SummonerBuffs.RemainingCooldown(id.Value)
                        : 0f,
                    id.HasValue && _gameManager.SummonerBuffs.IsActive(id.Value));
            }
        }

        void OnDopamineStateChanged(DopamineSnapshot _) => RefreshDopamineUI();

        void RefreshDopamineUI()
        {
            if (_gameManager?.Dopamine == null || FieldOverlay == null)
                return;
            FieldOverlay.SetDopamineState(
                _gameManager.Dopamine.Snapshot,
                _gameManager.Dopamine.Balance);
        }

        void OnPermanentTraitsChanged(PermanentTraitSnapshot snapshot)
        {
            BottomPanel.SetDirectRankOneChance(_gameManager.DirectRankOneChance);
            RefreshOwnedUnits();
            RefreshSummonerUI(_gameManager.SummonerProgression.Snapshot);
        }

        void OnRunTraitsChanged(RunTraitSnapshot snapshot)
        {
            RefreshSummonerUI(_gameManager.SummonerProgression.Snapshot);
            RefreshEquipmentUI();
            if (snapshot.IsChoicePending)
                TryShowTraitChoice();
        }

        void OnCombatBuildChanged(SummonerCombatBuildSnapshot snapshot)
        {
            RefreshSummonerUI(_gameManager.SummonerProgression.Snapshot);
            RefreshEquipmentUI();
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
            BottomPanel?.SetOwnedUnits(
                _gameManager?.SummonManager?.Bench,
                _deployedUnitBuffer,
                _gameManager?.SummonSlotCapacity ?? 12);
        }

        void RefreshGrowthUI()
        {
            var growth = _gameManager?.Growth;
            if (growth == null || BottomPanel == null) return;

            _growthRowBuffer.Clear();
            AddRunUpgradeRow(RunUpgradeType.AttackPower, "전체 공격력", "row__icon--atk");
            AddRunUpgradeRow(RunUpgradeType.AttackSpeed, "전체 공격 속도", "row__icon--aspd");
            AddRunUpgradeRow(RunUpgradeType.CoreRecovery, "소환사 HP 회복", "row__icon--hp");
            AddRunUpgradeRow(RunUpgradeType.CriticalChance, "치명타 확률", "row__icon--crit");
            AddRunUpgradeRow(RunUpgradeType.SummonCapacity, "슬라임 슬롯", "row__icon--hp");

            BottomPanel.SetGrowthRows(_growthRowBuffer);
            BottomPanel.SetRedDot("upgrade", _growthRowBuffer.Any(row => row.CanPurchase));
        }

        void AddRunUpgradeRow(RunUpgradeType type, string name, string iconClass)
        {
            GrowthManager growth = _gameManager.Growth;
            int level = growth.GetRunUpgradeLevel(type);
            bool maxed = growth.IsRunUpgradeMaxed(type);
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
                RunUpgradeType.SummonCapacity =>
                    $"{_gameManager.SummonSlotCapacity:N0}칸 → " +
                    $"{Mathf.Min(_gameManager.MaxSummonSlotCapacity, _gameManager.SummonSlotCapacity + 1):N0}칸",
                _ => string.Empty,
            };
            int cost = growth.GetRunUpgradeCost(type);
            _growthRowBuffer.Add(new GrowthRowModel(
                $"run:{type}",
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
            PermanentTraitProgression traits = _gameManager.PermanentTraits;
            RunTraitProgression runTraits = _gameManager.RunTraits;
            float damageMultiplier =
                snapshot.DamageMultiplier *
                (traits?.Snapshot.SummonerDamageMultiplier ?? 1f) *
                (_gameManager.Growth?.RunDamageMultiplier ?? 1f) *
                (_gameManager.Equipment?.DamageMultiplier ?? 1f) *
                (_gameManager.RunRelics?.DamageMultiplier ?? 1f);
            float criticalChance = Mathf.Clamp01(
                (_gameManager.Growth?.CriticalChance ?? 0f) +
                (_gameManager.Equipment?.CriticalChanceBonus ?? 0f));
            string experience = snapshot.IsMaxLevel
                ? "EXP MAX"
                : $"EXP {snapshot.Experience:N0} / {snapshot.ExperienceToNext:N0}";
            BottomPanel.SetSummonerInfo(new SummonerInfoModel(
                "위대한 소환사",
                $"Lv.{snapshot.Level:N0}",
                experience,
                snapshot.ExperienceProgress,
                $"x{damageMultiplier:0.##}",
                $"x{_gameManager.SummonerAttackSpeedMultiplier:0.##}",
                $"{_gameManager.MaxCoreHp:0.#}",
                $"{criticalChance * 100f:0.#}%",
                $"{_gameManager.DirectRankOneChance * 100f:0.#}%",
                BuildPermanentTraitText(traits),
                BuildRunTraitText(runTraits, _gameManager.CombatBuild)));
            BottomPanel.SetRedDot(
                "summoner",
                (_gameManager.PermanentTraits?.PendingChoiceCount ?? 0) > 0);
        }

        static string BuildPermanentTraitText(PermanentTraitProgression traits)
        {
            if (traits == null)
                return "획득한 영구 특성이 없습니다.";
            var lines = new List<string>();
            foreach (PermanentTraitType type in System.Enum.GetValues(typeof(PermanentTraitType)))
            {
                int level = traits.GetLevel(type);
                if (level > 0)
                    lines.Add($"• {traits.GetDisplayName(type)} Lv.{level:N0} — {traits.GetCurrentEffect(type)}");
            }
            return lines.Count == 0
                ? "획득한 영구 특성이 없습니다."
                : string.Join("\n", lines);
        }

        static string BuildRunTraitText(
            RunTraitProgression traits,
            SummonerCombatBuildProgression combatBuild)
        {
            var lines = new List<string>();
            if (combatBuild != null)
            {
                IReadOnlyList<RunRewardDefinition> combatRewards = combatBuild.GetAcquiredRewards();
                for (int i = 0; i < combatRewards.Count; i++)
                {
                    RunRewardDefinition reward = combatRewards[i];
                    int level = combatBuild.GetLevel(reward.RewardId);
                    if (level > 0)
                        lines.Add(
                            $"• [빌드] {reward.DisplayName} Lv.{level:N0} — " +
                            combatBuild.GetCurrentEffect(reward));
                }
            }
            if (traits != null)
            {
                IReadOnlyList<RunRewardDefinition> rewards = traits.GetAcquiredRewards();
                for (int i = 0; i < rewards.Count; i++)
                {
                    RunRewardDefinition reward = rewards[i];
                    int level = traits.GetLevel(reward.RewardId);
                    if (level > 0)
                        lines.Add(
                            $"• [진화] {reward.DisplayName} Lv.{level:N0} — " +
                            traits.GetCurrentEffect(reward));
                }
            }
            return lines.Count == 0
                ? "획득한 도전 빌드가 없습니다."
                : string.Join("\n", lines);
        }

        void OnRunUpgradeRequested(RunUpgradeType type)
        {
            if (_gameManager?.Growth == null) return;
            _gameManager.Growth.TryPurchaseRunUpgrade(type);
            RefreshGrowthUI();
        }

        void OnSlimeLevelUpRequested(string unitId)
        {
            if (_gameManager?.Growth == null) return;
            _gameManager.Growth.TryLevelUpSlime(unitId);
            RefreshGrowthUI();
            RefreshOpenUnitDetail();
        }

        bool TryShowTraitChoice()
        {
            if (_traitChoicePopupOpen || _gameManager == null)
                return false;
            if (traitChoicePopupPrefab == null)
            {
                Debug.LogWarning("[CrossDefense] TraitChoicePopup prefab reference is missing.", this);
                return false;
            }

            if (_traitChoicePopup == null)
                _traitChoicePopup = Instantiate(traitChoicePopupPrefab, transform);

            SummonerCombatBuildProgression combatBuild = _gameManager.CombatBuild;
            if (combatBuild != null && combatBuild.IsChoicePending)
            {
                IReadOnlyList<RunTraitChoice> combatChoices = combatBuild.GetCurrentChoices();
                if (combatChoices.Count != 3)
                    return false;
                _traitChoicePopupOpen = true;
                _gameManager.SetGameplayPause(GameplayPauseReason.TraitChoice, true);
                _traitChoicePopup.ShowSummonerLevelBuild(
                    combatChoices,
                    combatBuild.SummonerLevel,
                    combatBuild.PendingChoiceCount,
                    OnCombatBuildChoiceConfirmed);
                return true;
            }

            RunTraitProgression runTraits = _gameManager.RunTraits;
            if (runTraits != null && runTraits.IsChoicePending)
            {
                IReadOnlyList<RunTraitChoice> runChoices = runTraits.GetCurrentChoices();
                if (runChoices.Count != 3)
                    return false;
                _traitChoicePopupOpen = true;
                _gameManager.SetGameplayPause(GameplayPauseReason.TraitChoice, true);
                _traitChoicePopup.Show(
                    runChoices,
                    runTraits.ClearedWave,
                    OnRunTraitChoiceConfirmed);
                return true;
            }

            PermanentTraitProgression traits = _gameManager.PermanentTraits;
            if (traits == null || traits.PendingChoiceCount <= 0)
                return false;
            IReadOnlyList<PermanentTraitChoice> choices = traits.GetCurrentChoices();
            if (choices.Count != 3)
                return false;
            _traitChoicePopupOpen = true;
            _gameManager.SetGameplayPause(GameplayPauseReason.TraitChoice, true);
            _traitChoicePopup.Show(choices, traits.PendingChoiceCount, OnTraitChoiceConfirmed);
            return true;
        }

        void OnTraitChoiceConfirmed(PermanentTraitType type)
        {
            _traitChoicePopupOpen = false;
            if (_gameManager?.TryChoosePermanentTrait(type) != true)
            {
                _gameManager?.SetGameplayPause(GameplayPauseReason.TraitChoice, false);
                return;
            }
            StartCoroutine(ShowNextTraitChoiceNextFrame());
        }

        void OnRunTraitChoiceConfirmed(string rewardId)
        {
            _traitChoicePopupOpen = false;
            if (_gameManager?.TryChooseRunReward(
                    rewardId,
                    out IReadOnlyList<SummonResult> summonResults) != true)
            {
                _gameManager?.SetGameplayPause(GameplayPauseReason.TraitChoice, false);
                return;
            }

            if (summonResults != null && summonResults.Count > 0)
            {
                _runRewardSummonResults = summonResults;
                _runRewardSummonResultIndex = 0;
                BottomPanel?.SetSummonAnimationState(true);
                _gameManager?.SetGameplayPause(GameplayPauseReason.SummonRoulette, true);
                PlayNextRunRewardSummon();
                return;
            }
            StartCoroutine(ShowNextTraitChoiceNextFrame());
        }

        void OnCombatBuildChoiceConfirmed(string rewardId)
        {
            _traitChoicePopupOpen = false;
            if (_gameManager?.TryChooseCombatBuild(rewardId) != true)
            {
                _gameManager?.SetGameplayPause(GameplayPauseReason.TraitChoice, false);
                return;
            }
            StartCoroutine(ShowNextTraitChoiceNextFrame());
        }

        void PlayNextRunRewardSummon()
        {
            if (_runRewardSummonResults == null ||
                _runRewardSummonResultIndex >= _runRewardSummonResults.Count ||
                SummonRoulette == null)
            {
                FinishRunRewardSummonSequence();
                return;
            }

            SummonResult result = _runRewardSummonResults[_runRewardSummonResultIndex++];
            SummonRoulette.Play(result, PlayNextRunRewardSummon);
        }

        void FinishRunRewardSummonSequence()
        {
            _runRewardSummonResults = null;
            _runRewardSummonResultIndex = 0;
            BottomPanel?.SetSummonAnimationState(false);
            _gameManager?.SetGameplayPause(GameplayPauseReason.SummonRoulette, false);
            if (isActiveAndEnabled)
                StartCoroutine(ShowNextTraitChoiceNextFrame());
        }

        IEnumerator ShowNextTraitChoiceNextFrame()
        {
            yield return null;
            if (!isActiveAndEnabled)
                yield break;
            if (!TryShowTraitChoice())
                _gameManager?.SetGameplayPause(GameplayPauseReason.TraitChoice, false);
        }

        void OnBenchSlotSelected(SummonUnitInstance instance, int quantity)
        {
            if (SummonUnitDetail == null || instance?.Unit == null)
                return;
            SummonUnitDetail.Show(instance, quantity, CreateSlimeLevelUpModel(instance));
        }

        void RefreshOpenUnitDetail()
        {
            if (SummonUnitDetail?.IsOpen != true || SummonUnitDetail.CurrentInstance?.Unit == null)
                return;
            SummonUnitInstance instance = SummonUnitDetail.CurrentInstance;
            SummonUnitDetail.Show(
                instance,
                SummonUnitDetail.CurrentQuantity,
                CreateSlimeLevelUpModel(instance));
        }

        SlimeLevelUpViewModel CreateSlimeLevelUpModel(SummonUnitInstance instance)
        {
            GrowthManager growth = _gameManager?.Growth;
            SummonManager summonManager = _gameManager?.SummonManager;
            string unitId = instance?.Unit?.UnitId;
            if (growth == null || summonManager == null || string.IsNullOrWhiteSpace(unitId))
                return default;

            SummonUnitUpgradeState state = summonManager.GetUnitUpgradeState(unitId);
            bool maxed = state.Level >= growth.Balance.SlimeMaxLevel;
            int nextLevel = maxed ? state.Level : state.Level + 1;
            return new SlimeLevelUpViewModel(
                state.Level,
                growth.Balance.SlimeMaxLevel,
                growth.GetSlimeLevelUpCost(unitId),
                state.DamageMultiplier,
                maxed ? state.DamageMultiplier : growth.Balance.SlimeDamageMultiplier(nextLevel),
                state.AttackSpeedMultiplier,
                maxed ? state.AttackSpeedMultiplier : growth.Balance.SlimeAttackSpeedMultiplier(nextLevel),
                !maxed && growth.CanLevelUpSlime(unitId));
        }

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
            _gameManager.SetGameplayPause(GameplayPauseReason.SummonRoulette, true);
            SummonRoulette.Play(
                result,
                () =>
                {
                    try
                    {
                        _gameManager?.SummonManager?.CommitPending(result);
                    }
                    finally
                    {
                        BottomPanel?.SetSummonAnimationState(false);
                        _gameManager?.SetGameplayPause(GameplayPauseReason.SummonRoulette, false);
                    }
                });
        }

        /// <summary>스캐폴딩 확인용 더미 상태 — 게임 로직 연결 시 제거</summary>
        void ApplyScaffoldDemoState()
        {
            TopHUD.SetStageName("고블린 숲");
            FieldOverlay.SetWave(1, 24);
            TopHUD.SetGold(150);
            BottomPanel.SetSummonContracts(10);
            BottomPanel.SetRedDot("upgrade", true);
        }

        // 세이프 에어리어 패딩 — ui-guidelines §5 인라인 스타일 허용 예외 ②
        void ApplySafeArea(VisualElement root)
        {
            var safe = Screen.safeArea;
            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);
            float safeYMin = Mathf.Clamp(safe.yMin, 0f, screenHeight);
            float safeYMax = Mathf.Clamp(safe.yMax, safeYMin, screenHeight);
            float panelPerPixel = 1080f / screenWidth; // Match Width(0) 기준 환산
            float topInset = Mathf.Max(0f, screenHeight - safeYMax) * panelPerPixel;
            float bottomInset = safeYMin * panelPerPixel;
            root.style.paddingTop = topInset;
            root.style.paddingBottom = bottomInset;

            // Keep modal panels inside the safe area while their dimmed backdrops
            // extend through the unsafe top and bottom screen regions.
            ExtendModalBackdrop(root.Q<VisualElement>("summon-modal-backdrop"), topInset, bottomInset);
            ExtendModalBackdrop(root.Q<VisualElement>("merchant-modal-backdrop"), topInset, bottomInset);
        }

        static void ExtendModalBackdrop(VisualElement backdrop, float topInset, float bottomInset)
        {
            if (backdrop == null) return;
            backdrop.style.top = -topInset;
            backdrop.style.bottom = -bottomInset;
        }

        void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
            if (SlimeCodexModal?.IsVisible == true)
                OnSlimeCodexCloseRequested();
            else if (MonsterCodexModal?.IsVisible == true)
                OnCodexCloseRequested();
            else if (MerchantModal?.IsVisible == true)
                OnMerchantCloseRequested();
        }
    }
}
