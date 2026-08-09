using System;
using System.Collections;
using System.Collections.Generic;
using CrossDefense.Data;
using CrossDefense.UI;
using CrossDefense.Units;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrossDefense.Core
{
    /// <summary>런 상태, 소환사 HP·골드·용병 계약서, 웨이브 실행기의 진입점.</summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        const string GameplaySpeedPrefsKey = "CrossDefense.GameplaySpeed.v1";
        const float NormalGameplaySpeed = 1f;
        const float FastGameplaySpeed = 1.5f;

        [Header("Stage")]
        [SerializeField] StageTimeline stageTimeline;
        [SerializeField] bool autoStart = true;
        [SerializeField] bool useRuntimePrototypeWhenTimelineMissing = true;
        [SerializeField] Sprite runtimePrototypeMonsterSprite;
        [SerializeField] Sprite[] runtimePrototypeMonsterRunFrames;

        [Header("Encounter Bounds")]
        [SerializeField] SpriteRenderer gameplayBackground;
        [Min(0.1f)] [SerializeField] float monsterSpawnMinOutsideDistance = 0.4f;
        [Min(0.1f)] [SerializeField] float monsterSpawnMaxOutsideDistance = 1.1f;
        [Min(0.1f)] [SerializeField] float summonedUnitTargetSearchRange = 4.5f;

        [Header("Combat Motion")]
        [SerializeField] CombatPbdSettings combatPbd = new();
        [SerializeField] SummonFormationSettings summonFormation = new();

        [Header("Summoner")]
        [SerializeField] Transform summoner;
        [Min(1f)] [SerializeField] float maxCoreHp = 100f;
        [Min(0)] [SerializeField] int startingGold = 0;
        [Min(0)] [SerializeField] int startingSummonContracts = 4;

        [Header("Growth")]
        [SerializeField] GrowthBalanceData growthBalance;

        [Header("Persistent Collections")]
        [SerializeField] MonsterCatalog monsterCatalog;
        [SerializeField] EquipmentCatalog equipmentCatalog;
        [SerializeField] RelicCatalog relicCatalog;
        [SerializeField] MerchantCatalog merchantCatalog;
        [SerializeField] SummonUnitCatalog summonUnitCatalog;
        [SerializeField] SkillCatalog skillCatalog;

        [Header("Dopamine")]
        [SerializeField] DopamineBalanceData dopamineBalance;

        [Header("Summon Roulette")]
        [SerializeField] List<SummonUnitData> summonPool = new();
        [Header("Summoner Active Skill Effects")]
        [SerializeField] Sprite runtimeStar3NeutralEffectSprite;
        [SerializeField] Sprite runtimeMeteorProjectileSprite;
        [SerializeField] Sprite[] runtimeMeteorEffectFrames;
        [SerializeField] Sprite[] runtimeIceWallEffectFrames;
        [SerializeField] Sprite runtimeAegisEffectSprite;
        [Header("Monster Death Effect")]
        [SerializeField] Sprite[] runtimeGoblinDeathEffectFrames;
        [Header("Audio")]
        [SerializeField] AudioClip waveStartSfx;
        [Range(0f, 1f)] [SerializeField] float waveStartSfxVolume = 0.8f;
        [SerializeField] AudioClip goblinDeathSfx;
        [SerializeField] AudioClip goblinDeathKiekSfx;
        [Range(0f, 1f)] [SerializeField] float goblinDeathSfxVolume = 0.72f;
        [SerializeField] bool autoDeploySummonedUnits = true;
        [Range(0f, 1f)] [SerializeField] float currencyResultChance = 0.18f;
        [Range(0f, 1f)] [SerializeField] float directRankOneChance = 0.05f;
        [Min(0)] [SerializeField] int currencyResultGold = 10;
        [Min(1)] [SerializeField] int summonBenchCapacity = 12;

        WaveManager _waveManager;
        MonsterSpawner _monsterSpawner;
        SummonManager _summonManager;
        SummonedUnitManager _summonedUnitManager;
        GoldRewardFlow _goldRewardFlow;
        Func<Vector2> _goldScreenPositionProvider;
        StageTimeline _runtimeTimeline;
        GrowthBalanceData _runtimeGrowthBalance;
        DopamineBalanceData _runtimeDopamineBalance;
        RunRewardCatalog _runtimeRunRewardCatalog;
        MonsterCatalog _runtimeMonsterCatalog;
        EquipmentCatalog _runtimeEquipmentCatalog;
        RelicCatalog _runtimeRelicCatalog;
        MerchantCatalog _runtimeMerchantCatalog;
        WorldHealthBar _summonerHealthBar;
        SpriteRenderer _summonerRenderer;
        DamageFloatingTextService _damageFloatingText;
        CombatEffectService _monsterDeathEffects;
        AudioSource _sfxSource;
        MonsterProjectileService _monsterProjectiles;
        SummonerProgression _summonerProgression;
        PermanentTraitProgression _permanentTraits;
        RunTraitProgression _runTraits;
        SummonerCombatBuildProgression _combatBuild;
        GrowthManager _growthManager;
        SummonerSkillLoadout _summonerSkillLoadout;
        SummonerSkillController _summonerSkillController;
        SummonerBuffLoadout _summonerBuffLoadout;
        SummonerBuffController _summonerBuffController;
        DopamineController _dopamineController;
        MonsterCodexProgression _monsterCodex;
        EquipmentProgression _equipment;
        RelicProgression _relics;
        RunRelicInventory _runRelics;
        MerchantManager _merchant;
        RunSessionProgression _runSession;
        WalletProgression _wallet;
        float _coreHp;
        float _effectiveMaxCoreHp;
        float _coreShieldHp;
        float _coreShieldUntil;
        int _gold;
        int _summonContracts;
        int _resumeWaveIndex;
        int _runEventSeed;
        bool _runSessionActive = true;
        bool _suppressRunSessionSave;
        RunPhase _phase;
        StageWave _currentWaveData;
        readonly HashSet<int> _grantedRushGoldWaves = new();
        GameplayPauseReason _gameplayPauseReasons;
        bool _tutorialWaveHold;
        float _timeScaleBeforeGameplayPause = 1f;
        float _gameplaySpeed = NormalGameplaySpeed;
        float _nextRunHealthCheckpointTime;
        float _nextGoldenGoblinUiTime;
        MonsterController _activeGoldenGoblin;

        public StageTimeline StageTimeline => stageTimeline;
        public Transform Summoner => summoner;
        public RunPhase Phase => _phase;
        public float CoreHp => _coreHp;
        public float MaxCoreHp => _effectiveMaxCoreHp;
        public int Gold => _gold;
        public int SummonContracts => _summonContracts;
        public SummonManager SummonManager => _summonManager;
        public SummonedUnitManager SummonedUnitManager => _summonedUnitManager;
        public SummonerProgression SummonerProgression => _summonerProgression;
        public PermanentTraitProgression PermanentTraits => _permanentTraits;
        public RunTraitProgression RunTraits => _runTraits;
        public SummonerCombatBuildProgression CombatBuild => _combatBuild;
        public GrowthManager Growth => _growthManager;
        public SummonerSkillLoadout SummonerSkillLoadout => _summonerSkillLoadout;
        public SummonerSkillController SummonerSkills => _summonerSkillController;
        public SummonerBuffLoadout SummonerBuffLoadout => _summonerBuffLoadout;
        public SummonerBuffController SummonerBuffs => _summonerBuffController;
        public DopamineController Dopamine => _dopamineController;
        public MonsterCatalog MonsterCatalog => monsterCatalog;
        public MonsterCodexProgression MonsterCodex => _monsterCodex;
        public EquipmentProgression Equipment => _equipment;
        public RelicProgression Relics => _relics;
        public RunRelicInventory RunRelics => _runRelics;
        public MerchantManager Merchant => _merchant;
        public StageWave CurrentWaveData => _currentWaveData;
        public float CoreShieldHp =>
            Time.time < _coreShieldUntil ? Mathf.Max(0f, _coreShieldHp) : 0f;
        public GrowthBalanceData GrowthBalance => growthBalance;
        public int MaxSummonSlotCapacity => Mathf.Max(
            1,
            Mathf.Min(summonBenchCapacity, growthBalance?.MaxSummonCapacity ?? summonBenchCapacity));
        public int SummonSlotCapacity => Mathf.Clamp(
            (growthBalance?.BaseSummonCapacity ?? summonBenchCapacity) +
            (_growthManager?.SummonCapacityBonus ?? 0) +
            (_permanentTraits?.Snapshot.SummonCapacityBonus ?? 0),
            1,
            MaxSummonSlotCapacity);
        public DamageFloatingTextService DamageFloatingText => _damageFloatingText;
        public bool IsGameplayPaused => _gameplayPauseReasons != GameplayPauseReason.None;
        public bool IsWaveProgressionHeld => _tutorialWaveHold;
        public GameplayPauseReason GameplayPauseReasons => _gameplayPauseReasons;
        public float GameplaySpeed => _gameplaySpeed;
        public float DirectRankOneChance => Mathf.Min(0.25f,
            directRankOneChance +
            (_summonerProgression?.Snapshot.JackpotChanceBonus ?? 0f) +
            (_permanentTraits?.Snapshot.JackpotChanceBonus ?? 0f) +
            (_equipment?.JackpotChanceBonus ?? 0f) +
            (_runRelics?.JackpotChanceBonus ?? 0f));
        public float RunAttackSpeedMultiplier => _growthManager?.RunAttackSpeedMultiplier ?? 1f;
        public float SummonerAttackSpeedMultiplier =>
            RunAttackSpeedMultiplier *
            (_permanentTraits?.Snapshot.SummonerAttackSpeedMultiplier ?? 1f) *
            (_equipment?.AttackSpeedMultiplier ?? 1f) *
            (_runRelics?.AttackSpeedMultiplier ?? 1f) *
            (_combatBuild?.BuildProfile().AttackSpeedMultiplier ?? 1f) *
            (_dopamineController?.SummonerAttackSpeedMultiplier ?? 1f);
        public float SlimeAttackSpeedMultiplier =>
            RunAttackSpeedMultiplier *
            (_permanentTraits?.Snapshot.SlimeAttackSpeedMultiplier ?? 1f) *
            (_runRelics?.AttackSpeedMultiplier ?? 1f) *
            (_summonerBuffController?.SlimeAttackSpeedMultiplier ?? 1f);
        public bool IsRunTraitChoicePending => _runTraits?.IsChoicePending ?? false;
        public bool IsCombatBuildChoicePending => _combatBuild?.IsChoicePending ?? false;
        public bool IsMerchantOpen => _merchant?.IsOpen ?? false;
        public CombatProjectileService Projectiles => _summonedUnitManager?.Projectiles;
        public MonsterProjectileService MonsterProjectiles => _monsterProjectiles;
        public bool IsRunOver => _phase == RunPhase.Victory || _phase == RunPhase.Defeat;
        public bool HasNextStage
        {
            get
            {
                string nextScene = stageTimeline?.NextSceneName;
                if (!string.IsNullOrWhiteSpace(nextScene))
                    return Application.CanStreamedLevelBeLoaded(nextScene);
                Scene scene = SceneManager.GetActiveScene();
                return scene.buildIndex >= 0 &&
                       scene.buildIndex + 1 < SceneManager.sceneCountInBuildSettings;
            }
        }
        public int CurrentWave => _waveManager == null ? 0 : _waveManager.CurrentWaveIndex + 1;
        public int TotalWaves => _waveManager == null ? 0 : _waveManager.TotalWaves;
        public int LivingMonsterCount => _waveManager == null ? 0 : _waveManager.LivingMonsterCount;
        public int RunEventSeed => _runEventSeed;
        public SpriteRenderer GameplayBackground => gameplayBackground;
        public float MonsterSpawnMinOutsideDistance => monsterSpawnMinOutsideDistance;
        public float MonsterSpawnMaxOutsideDistance => monsterSpawnMaxOutsideDistance;
        public float SummonedUnitTargetSearchRange => summonedUnitTargetSearchRange;

        public event Action<RunPhase> PhaseChanged;
        public event Action<int, int> WaveChanged;
        public event Action<float, float> CoreHpChanged;
        public event Action<int> GoldChanged;
        public event Action<int> SummonContractsChanged;
        public event Action<int> LivingMonsterCountChanged;
        public event Action<MonsterController, StageWave, int> MonsterSpawned;
        public event Action<MonsterController> MonsterResolved;
        public event Action<bool> GameplayPauseChanged;
        public event Action<float> GameplaySpeedChanged;
        public event Action<GoldenGoblinSnapshot> GoldenGoblinStateChanged;

        void OnValidate()
        {
            if (gameplayBackground == null)
                gameplayBackground = transform.Find("Background")?.GetComponent<SpriteRenderer>();
            monsterSpawnMinOutsideDistance = Mathf.Max(0.1f, monsterSpawnMinOutsideDistance);
            monsterSpawnMaxOutsideDistance = Mathf.Max(
                monsterSpawnMinOutsideDistance,
                monsterSpawnMaxOutsideDistance);
            summonedUnitTargetSearchRange = Mathf.Max(0.1f, summonedUnitTargetSearchRange);
        }

        void OnDrawGizmosSelected()
        {
            Vector3 center;
            float width;
            float height;
            if (gameplayBackground != null && gameplayBackground.sprite != null)
            {
                Bounds bounds = gameplayBackground.bounds;
                center = bounds.center;
                width = bounds.extents.x;
                height = bounds.extents.y;
            }
            else
            {
                var worldCamera = Camera.main;
                if (worldCamera == null || !worldCamera.orthographic) return;
                center = worldCamera.transform.position;
                center.z = 0f;
                height = worldCamera.orthographicSize;
                width = height * worldCamera.aspect;
            }

            Gizmos.color = new Color(0.2f, 1f, 0.55f, 0.75f);
            DrawSpawnRect(center, width + monsterSpawnMinOutsideDistance,
                height + monsterSpawnMinOutsideDistance);
            Gizmos.color = new Color(0.15f, 0.65f, 1f, 0.75f);
            DrawSpawnRect(center, width + monsterSpawnMaxOutsideDistance,
                height + monsterSpawnMaxOutsideDistance);
        }

        static void DrawSpawnRect(Vector3 center, float halfWidth, float halfHeight)
        {
            var topLeft = center + new Vector3(-halfWidth, halfHeight);
            var topRight = center + new Vector3(halfWidth, halfHeight);
            var bottomRight = center + new Vector3(halfWidth, -halfHeight);
            var bottomLeft = center + new Vector3(-halfWidth, -halfHeight);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }

        void Awake()
        {
            _gameplaySpeed = PlayerPrefs.GetInt(GameplaySpeedPrefsKey, 0) == 1
                ? FastGameplaySpeed
                : NormalGameplaySpeed;
            _timeScaleBeforeGameplayPause = _gameplaySpeed;
            Time.timeScale = _gameplaySpeed;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;
            _sfxSource.volume = GameAudioSettings.EffectsVolume;
            GameAudioSettings.ApplyStoredSettings();

            if (PersistentProgressReset.ConsumePendingReset())
                Debug.Log("[CrossDefense] 예약된 영구 진행 초기화를 적용했습니다. Lv.1부터 시작합니다.", this);

            if (gameplayBackground == null)
                gameplayBackground = transform.Find("Background")?.GetComponent<SpriteRenderer>();
            if (summoner == null)
                summoner = transform.Find("Summoner");
            if (summoner == null)
                summoner = GameObject.Find("Summoner")?.transform;

            if (stageTimeline == null && useRuntimePrototypeWhenTimelineMissing)
            {
                _runtimeTimeline = StageTimeline.CreatePrototype(
                    defaultMonsterSprite: runtimePrototypeMonsterSprite,
                    defaultMonsterMoveFrames: runtimePrototypeMonsterRunFrames);
                stageTimeline = _runtimeTimeline;
                Debug.Log("[CrossDefense] StageTimeline이 비어 있어 런타임 프로토타입 타임라인을 사용합니다.", this);
            }

            if (growthBalance == null)
            {
                _runtimeGrowthBalance = GrowthBalanceData.CreateRuntimeDefault();
                growthBalance = _runtimeGrowthBalance;
            }
            if (dopamineBalance == null)
            {
                _runtimeDopamineBalance = DopamineBalanceData.CreateRuntimeDefault();
                dopamineBalance = _runtimeDopamineBalance;
            }
            _summonerProgression = SummonerProgression.CreatePersistent(growthBalance);
            _summonerProgression.Changed += OnSummonerProgressionChanged;
            if (skillCatalog == null)
                Debug.LogError("[CrossDefense] SkillCatalog is not assigned.", this);
            else if (!skillCatalog.Validate(out string skillCatalogError))
                Debug.LogError($"[CrossDefense] Invalid SkillCatalog: {skillCatalogError}", this);
            _summonerSkillLoadout = SummonerSkillLoadout.CreatePersistent(
                () => _summonerProgression.Snapshot.Level,
                skillCatalog);
            _summonerBuffLoadout = SummonerBuffLoadout.CreatePersistent(
                () => _summonerProgression.Snapshot.Level);
            if (monsterCatalog == null)
            {
                _runtimeMonsterCatalog = MonsterCatalog.CreateRuntime(stageTimeline?.EnumerateMonsters());
                monsterCatalog = _runtimeMonsterCatalog;
            }
            _monsterCodex = MonsterCodexProgression.CreatePersistent(monsterCatalog);
            if (equipmentCatalog == null)
            {
                _runtimeEquipmentCatalog = EquipmentCatalog.CreateRuntimeDefault();
                equipmentCatalog = _runtimeEquipmentCatalog;
            }
            _equipment = EquipmentProgression.CreatePersistent(equipmentCatalog);
            _equipment.Changed += OnEquipmentChanged;
            if (relicCatalog == null)
            {
                _runtimeRelicCatalog = RelicCatalog.CreateRuntimeDefault();
                relicCatalog = _runtimeRelicCatalog;
            }
            _relics = RelicProgression.CreatePersistent(relicCatalog);
            _permanentTraits = PermanentTraitProgression.CreatePersistent(
                growthBalance,
                () => _summonerProgression.Snapshot.Level,
                availabilityProvider: IsPermanentLevelRewardAvailable);
            _permanentTraits.Changed += OnPermanentTraitsChanged;
            _runRelics = new RunRelicInventory();
            if (merchantCatalog == null)
            {
                _runtimeMerchantCatalog = MerchantCatalog.CreateRuntimeDefault(equipmentCatalog);
                merchantCatalog = _runtimeMerchantCatalog;
            }
            RunRewardCatalog runRewardCatalog = stageTimeline?.RunRewardCatalog;
            if (runRewardCatalog == null)
            {
                _runtimeRunRewardCatalog = RunRewardCatalog.CreateRuntimeDefault();
                runRewardCatalog = _runtimeRunRewardCatalog;
                Debug.LogWarning(
                    "[CrossDefense] StageTimeline에 RunRewardCatalog가 없어 런타임 기본 보상 카탈로그를 사용합니다.",
                    this);
            }
            _runTraits = new RunTraitProgression(runRewardCatalog, stageTimeline?.RandomSeed ?? 0);
            _combatBuild = new SummonerCombatBuildProgression(
                runRewardCatalog,
                unchecked((stageTimeline?.RandomSeed ?? 0) ^ 0x45D9F3B),
                _summonerProgression.Snapshot.Level);

            _runSession = RunSessionProgression.CreatePersistent();
            _resumeWaveIndex = 0;
            if (_runSession.TryLoad(stageTimeline?.StageId, out RunSessionSaveData checkpoint))
            {
                _resumeWaveIndex = Mathf.Max(0, checkpoint.waveIndex);
                Debug.Log(
                    $"[CrossDefense] DAY checkpoint restored: DAY {_resumeWaveIndex + 1}.",
                    this);
            }
            bool hasHealthCheckpoint = checkpoint?.healthCheckpointVersion > 0;
            bool checkpointWasDefeated = hasHealthCheckpoint && checkpoint.coreHpRatio <= 0f;
            if (checkpointWasDefeated)
                _resumeWaveIndex = 0;
            _runEventSeed = checkpoint?.runEventSeed ?? 0;
            if (_runEventSeed == 0 || checkpointWasDefeated)
                _runEventSeed = CreateRunEventSeed();
            _runTraits.Restore(checkpoint?.runTraits);
            _runRelics.Restore(checkpoint?.runRelicIds, merchantCatalog.FindRelic);
            _runRelics.Changed += OnRunRelicsChanged;
            _runTraits.Changed += OnRunTraitsChanged;

            maxCoreHp = Mathf.Max(1f, maxCoreHp);
            _effectiveMaxCoreHp = maxCoreHp *
                                  _summonerProgression.Snapshot.MaxHpMultiplier *
                                  _permanentTraits.Snapshot.CoreMaxHpMultiplier *
                                  (_equipment?.MaxHpMultiplier ?? 1f) *
                                  (_runRelics?.MaxHpMultiplier ?? 1f);
            _coreHp = hasHealthCheckpoint && !checkpointWasDefeated
                ? _effectiveMaxCoreHp * Mathf.Clamp01(checkpoint.coreHpRatio)
                : _effectiveMaxCoreHp;
            _wallet = WalletProgression.CreatePersistent(Mathf.Max(0, startingGold));
            _gold = _wallet.Gold;
            _summonContracts = ResolveStartingSummonContracts(
                checkpoint,
                startingSummonContracts);
            _phase = RunPhase.Prepare;

            if (summoner != null && summoner.TryGetComponent(out _summonerRenderer))
            {
                _summonerHealthBar = WorldHealthBar.GetOrAdd(summoner.gameObject);
                _summonerHealthBar.Configure(_summonerRenderer, WorldHealthBarProfile.Summoner);
                _summonerHealthBar.SetHealth(_coreHp, _effectiveMaxCoreHp);
            }

            _damageFloatingText = DamageFloatingTextService.GetOrAdd(gameObject);
            _damageFloatingText?.Initialize(Camera.main);
            _monsterDeathEffects = new CombatEffectService(
                transform,
                rootName: "MonsterDeathEffects");
            _monsterProjectiles = new MonsterProjectileService(this, transform);

            _summonManager = new SummonManager(
                this,
                BuildSummonPool(),
                currencyResultChance,
                directRankOneChance,
                currencyResultGold,
                summonBenchCapacity,
                directRankOneChanceProvider: () => DirectRankOneChance,
                capacityProvider: () => SummonSlotCapacity,
                summonerLevelProvider: () => _summonerProgression?.Snapshot.Level ?? 1);
            _growthManager = GrowthManager.CreatePersistent(this, _summonManager, growthBalance);
            _merchant = new MerchantManager(
                this, merchantCatalog, _equipment, _relics, _runRelics);

            _summonedUnitManager = GetComponent<SummonedUnitManager>();
            if (_summonedUnitManager == null)
                _summonedUnitManager = gameObject.AddComponent<SummonedUnitManager>();
            _summonedUnitManager.Initialize(
                this,
                _summonManager,
                summoner,
                Camera.main,
                autoDeploySummonedUnits,
                summonedUnitTargetSearchRange,
                combatPbd,
                summonFormation);
            _summonManager.BenchChanged += OnBenchRosterChanged;
            _summonedUnitManager.UnitsChanged += OnFieldRosterChanged;
            RestoreSummonedUnits(
                checkpointWasDefeated ? null : checkpoint?.summonedUnits,
                hasHealthCheckpoint);

            SummonerAttackController summonerAttack =
                summoner != null ? summoner.GetComponent<SummonerAttackController>() : null;
            summonerAttack?.ConfigureSkillCatalog(skillCatalog);
            _summonerSkillController = GetComponent<SummonerSkillController>();
            if (_summonerSkillController == null)
                _summonerSkillController = gameObject.AddComponent<SummonerSkillController>();
            _summonerSkillController.Initialize(
                this,
                summonerAttack,
                _relics,
                skillCatalog,
                runtimeMeteorProjectileSprite,
                runtimeMeteorEffectFrames,
                runtimeIceWallEffectFrames,
                runtimeAegisEffectSprite != null
                    ? runtimeAegisEffectSprite
                    : runtimeStar3NeutralEffectSprite);

            _summonerBuffController = GetComponent<SummonerBuffController>();
            if (_summonerBuffController == null)
                _summonerBuffController = gameObject.AddComponent<SummonerBuffController>();
            _summonerBuffController.Initialize(
                this,
                _summonerBuffLoadout,
                runtimeAegisEffectSprite != null
                    ? runtimeAegisEffectSprite
                    : runtimeStar3NeutralEffectSprite);

            _dopamineController = GetComponent<DopamineController>();
            if (_dopamineController == null)
                _dopamineController = gameObject.AddComponent<DopamineController>();
            _dopamineController.Initialize(
                this,
                _summonerSkillController,
                dopamineBalance,
                stageTimeline?.RandomSeed ?? 0,
                stageTimeline?.StartingOverdriveGauge ?? 0);

            var input = GetComponent<CombatInputController>();
            if (input == null)
                input = gameObject.AddComponent<CombatInputController>();
            input.Initialize(
                this,
                _summonedUnitManager,
                summoner != null ? summoner.GetComponent<SummonerAttackController>() : null,
                _summonerSkillController);

            _monsterSpawner = new MonsterSpawner(
                transform,
                Camera.main,
                gameplayBackground,
                monsterSpawnMinOutsideDistance,
                monsterSpawnMaxOutsideDistance);
            _goldRewardFlow = new GoldRewardFlow(
                Camera.main,
                () => _goldScreenPositionProvider?.Invoke() ??
                    new Vector2(Screen.width * 0.78f, Screen.height * 0.95f),
                AddGold);
            _waveManager = new WaveManager();
            _waveManager.Initialize(this, stageTimeline, _monsterSpawner, summoner);
            CoreHpChanged?.Invoke(_coreHp, _effectiveMaxCoreHp);
            GoldChanged?.Invoke(_gold);
            SummonContractsChanged?.Invoke(_summonContracts);
            SaveRunSession(true);
        }

        IEnumerable<SummonUnitData> BuildSummonPool()
        {
            if (summonUnitCatalog != null && summonUnitCatalog.Units.Count > 0)
            {
                foreach (SummonUnitData unit in summonUnitCatalog.Units)
                    if (unit != null) yield return unit;
                yield break;
            }

            bool hasConfiguredUnit = false;
            foreach (var unit in summonPool)
            {
                if (unit == null) continue;
                hasConfiguredUnit = true;
                yield return unit;
            }

            if (!hasConfiguredUnit)
                Debug.LogError("SummonUnitCatalog과 임시 summonPool이 모두 비어 있습니다.", this);
        }

        void Start()
        {
            if (autoStart)
                StartRun();
        }

        void Update()
        {
            if (_activeGoldenGoblin != null &&
                !_activeGoldenGoblin.IsResolved &&
                Time.unscaledTime >= _nextGoldenGoblinUiTime)
            {
                _nextGoldenGoblinUiTime = Time.unscaledTime + 0.1f;
                GoldenGoblinStateChanged?.Invoke(new GoldenGoblinSnapshot(
                    GoldenGoblinState.Active,
                    _activeGoldenGoblin.GoldenEscapeTimeRemaining,
                    _activeGoldenGoblin.GoldenEscapeDuration,
                    stageTimeline?.GoldenGoblin?.TotalGoldReward ?? 0));
            }

            if (Time.unscaledTime >= _nextRunHealthCheckpointTime)
            {
                _nextRunHealthCheckpointTime = Time.unscaledTime + 1f;
                SaveRunSession(true);
            }
        }

        public void StartRun()
        {
            if (_waveManager == null) return;
            _waveManager.RunFrom(this, _resumeWaveIndex);
        }

        public void ApplyCoreDamage(int amount)
        {
            if (IsRunOver || amount <= 0) return;
            float remainingDamage = amount;
            if (Time.time < _coreShieldUntil && _coreShieldHp > 0f)
            {
                float absorbed = Mathf.Min(_coreShieldHp, remainingDamage);
                _coreShieldHp -= absorbed;
                remainingDamage -= absorbed;
            }
            float previousHp = _coreHp;
            _coreHp = Mathf.Max(0f, _coreHp - remainingDamage);
            _summonerHealthBar?.SetHealth(_coreHp, _effectiveMaxCoreHp);
            float hpDamage = previousHp - _coreHp;
            if (hpDamage > 0f)
                PresentDamageNumber(
                    GetSummonerDamageAnchor(),
                    hpDamage,
                    DamageTextKind.Received);
            CoreHpChanged?.Invoke(_coreHp, _effectiveMaxCoreHp);
            if (_coreHp <= 0f)
            {
                SetPhase(RunPhase.Defeat);
                Debug.Log("[CrossDefense] 소환사 HP가 0이 되어 런이 종료되었습니다.", this);
            }
        }

        public void GrantCoreShield(float amount, float duration)
        {
            if (amount <= 0f || duration <= 0f || IsRunOver)
                return;
            _coreShieldHp = Mathf.Max(_coreShieldHp, amount);
            _coreShieldUntil = Mathf.Max(_coreShieldUntil, Time.time + duration);
        }

        public void RestartCurrentStage()
        {
            _wallet?.Flush();
            SaveSameStageRestartSession();
            _summonerProgression?.Flush();
            _permanentTraits?.Flush();
            _summonerSkillLoadout?.Flush();
            _summonerBuffLoadout?.Flush();
            _growthManager?.Flush();
            _monsterCodex?.Flush();
            _equipment?.Flush();
            _relics?.Flush();
            RestoreGameplayTimeScale();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
                SceneManager.LoadScene(scene.buildIndex);
            else if (!string.IsNullOrWhiteSpace(scene.name))
                SceneManager.LoadScene(scene.name);
        }

        public void ReplayCurrentStage()
        {
            Scene scene = SceneManager.GetActiveScene();
            PrepareFreshStageTransition();
            if (scene.buildIndex >= 0)
                SceneManager.LoadScene(scene.buildIndex);
            else if (!string.IsNullOrWhiteSpace(scene.name))
                SceneManager.LoadScene(scene.name);
        }

        public bool ContinueToNextStage()
        {
            string nextScene = stageTimeline?.NextSceneName;
            if (!string.IsNullOrWhiteSpace(nextScene) &&
                Application.CanStreamedLevelBeLoaded(nextScene))
            {
                PrepareFreshStageTransition();
                SceneManager.LoadScene(nextScene);
                return true;
            }

            Scene scene = SceneManager.GetActiveScene();
            int nextBuildIndex = scene.buildIndex + 1;
            if (scene.buildIndex < 0 ||
                nextBuildIndex >= SceneManager.sceneCountInBuildSettings)
                return false;

            PrepareFreshStageTransition();
            SceneManager.LoadScene(nextBuildIndex);
            return true;
        }

        void PrepareFreshStageTransition()
        {
            _runSessionActive = false;
            _suppressRunSessionSave = true;
            _runRelics?.Clear();
            _runSession?.Clear(true);
            _wallet?.Flush();
            _summonerProgression?.Flush();
            _permanentTraits?.Flush();
            _summonerSkillLoadout?.Flush();
            _summonerBuffLoadout?.Flush();
            _growthManager?.Flush();
            _monsterCodex?.Flush();
            _equipment?.Flush();
            _relics?.Flush();
            RestoreGameplayTimeScale();
        }

        public void ResetAllProgressAndRestart()
        {
            PersistentProgressReset.RequestOnNextSceneLoad();
            RestoreGameplayTimeScale();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
                SceneManager.LoadScene(scene.buildIndex);
            else if (!string.IsNullOrWhiteSpace(scene.name))
                SceneManager.LoadScene(scene.name);
        }

        public void AddGold(int amount)
        {
            _gold = Mathf.Max(0, _gold + amount);
            _wallet?.SetGold(_gold);
            GoldChanged?.Invoke(_gold);
            SaveRunSession(false);
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0 || _gold < amount) return false;
            _gold -= amount;
            _wallet?.SetGold(_gold);
            GoldChanged?.Invoke(_gold);
            SaveRunSession(false);
            return true;
        }

        public void HealCore(float amount)
        {
            if (amount <= 0f || _coreHp <= 0f || _coreHp >= _effectiveMaxCoreHp) return;
            _coreHp = Mathf.Min(_effectiveMaxCoreHp, _coreHp + amount);
            _summonerHealthBar?.SetHealth(_coreHp, _effectiveMaxCoreHp);
            CoreHpChanged?.Invoke(_coreHp, _effectiveMaxCoreHp);
        }

        public void PresentDamageNumber(
            Vector3 worldPosition,
            float amount,
            DamageTextKind kind)
        {
            if (amount <= 0f)
                return;
            if (_damageFloatingText == null)
            {
                _damageFloatingText = DamageFloatingTextService.GetOrAdd(gameObject);
                _damageFloatingText?.Initialize(Camera.main);
            }
            _damageFloatingText?.Show(worldPosition, amount, kind);
        }

        Vector3 GetSummonerDamageAnchor()
        {
            if (_summonerRenderer != null && _summonerRenderer.sprite != null)
            {
                Bounds bounds = _summonerRenderer.bounds;
                return new Vector3(
                    bounds.center.x,
                    Mathf.Lerp(bounds.center.y, bounds.max.y, 0.55f),
                    transform.position.z);
            }
            return summoner != null ? summoner.position + Vector3.up * 0.35f : Vector3.zero;
        }

        public float ModifySlimeDamage(float baseDamage)
        {
            float permanentDamage = Mathf.Max(0f, baseDamage) *
                                    (_permanentTraits?.Snapshot.SlimeDamageMultiplier ?? 1f) *
                                    (_runRelics?.DamageMultiplier ?? 1f) *
                                    (_summonerBuffController?.SlimeDamageMultiplier ?? 1f);
            return _growthManager?.ModifyPlayerDamage(permanentDamage) ?? permanentDamage;
        }

        public float ModifySummonerDamage(float baseDamage)
        {
            return ModifySummonerDamage(baseDamage, out _);
        }

        public float ModifySummonerDamage(float baseDamage, out bool critical)
        {
            float permanentDamage = Mathf.Max(0f, baseDamage) *
                                    (_summonerProgression?.Snapshot.DamageMultiplier ?? 1f) *
                                    (_permanentTraits?.Snapshot.SummonerDamageMultiplier ?? 1f) *
                                    (_equipment?.DamageMultiplier ?? 1f) *
                                    (_runRelics?.DamageMultiplier ?? 1f) *
                                    (_dopamineController?.SummonerDamageMultiplier ?? 1f);
            if (_growthManager == null)
            {
                critical = false;
                return permanentDamage;
            }
            return _growthManager.ModifyPlayerDamage(
                permanentDamage,
                _equipment?.CriticalChanceBonus ?? 0f,
                out critical);
        }

        public void RegisterGoldScreenPositionProvider(Func<Vector2> provider)
        {
            _goldScreenPositionProvider = provider;
        }

        public void AddSummonContracts(int amount)
        {
            if (amount <= 0) return;
            _summonContracts += amount;
            SummonContractsChanged?.Invoke(_summonContracts);
            SaveRunSession(false);
        }

        public bool TrySpendSummonContract()
        {
            if (_summonContracts <= 0) return false;
            _summonContracts--;
            SummonContractsChanged?.Invoke(_summonContracts);
            SaveRunSession(false);
            return true;
        }

        public void GrantWaveClearReward(StageWave wave)
        {
            if (wave == null) return;
            int reward = Mathf.Max(0, wave.SummonContractReward) + (_runRelics?.WaveContractBonus ?? 0);
            if (reward <= 0) return;
            AddSummonContracts(reward);
            Debug.Log($"[CrossDefense] Wave clear reward: summon contracts +{reward}", this);
        }

        public void GrantWaveClearGoldBonus(int waveNumber, int amount)
        {
            if (waveNumber <= 0 || amount <= 0 || !_grantedRushGoldWaves.Add(waveNumber)) return;
            AddGold(amount);
            Debug.Log($"[CrossDefense] Rush clear reward: gold +{amount}", this);
        }

        public bool BeginMerchant(int clearedWave)
        {
            if (_merchant == null || _merchant.IsOpen) return false;
            _merchant.Open(clearedWave, stageTimeline?.RandomSeed ?? 0);
            SetPhase(RunPhase.Merchant);
            SetGameplayPause(GameplayPauseReason.Merchant, true);
            return true;
        }

        public void CloseMerchant()
        {
            _merchant?.Close();
            SetGameplayPause(GameplayPauseReason.Merchant, false);
        }

        public bool TryChoosePermanentTrait(PermanentTraitType type)
        {
            if (_permanentTraits == null)
                return false;

            EquipmentData equipmentReward = type == PermanentTraitType.EquipmentSupply
                ? PickPermanentEquipmentReward()
                : null;
            RelicDefinition relicReward = type == PermanentTraitType.RelicDiscovery
                ? PickPermanentRelicReward()
                : null;
            if (type == PermanentTraitType.EquipmentSupply && equipmentReward == null)
                return false;
            if (type == PermanentTraitType.RelicDiscovery && relicReward == null)
                return false;
            if (!_permanentTraits.TryChoose(type))
                return false;

            if (equipmentReward != null)
            {
                bool acquired = _equipment.Acquire(equipmentReward);
                if (acquired)
                    Debug.Log($"[CrossDefense] 레벨업 장비 획득: {equipmentReward.DisplayName}", this);
                return acquired;
            }
            if (relicReward != null)
            {
                bool acquired = _relics.TryAcquire(relicReward.Family);
                if (acquired)
                {
                    int rank = _relics.Rank(relicReward.Family);
                    Debug.Log(
                        $"[CrossDefense] 레벨업 신물 획득: {relicReward.Rank(rank)?.DisplayName} ★{rank}",
                        this);
                }
                return acquired;
            }
            return true;
        }

        bool IsPermanentLevelRewardAvailable(PermanentTraitType type) =>
            type switch
            {
                PermanentTraitType.EquipmentSupply => HasPermanentEquipmentReward(),
                PermanentTraitType.RelicDiscovery => HasPermanentRelicReward(),
                _ => true,
            };

        bool HasPermanentEquipmentReward()
        {
            if (_equipment?.Catalog?.Equipment == null)
                return false;
            foreach (EquipmentData item in _equipment.Catalog.Equipment)
                if (item != null && !_equipment.IsOwned(item.EquipmentId))
                    return true;
            return false;
        }

        bool HasPermanentRelicReward()
        {
            if (_relics?.Catalog?.Relics == null)
                return false;
            foreach (RelicDefinition relic in _relics.Catalog.Relics)
                if (relic != null && _relics.CanAcquire(relic.Family))
                    return true;
            return false;
        }

        EquipmentData PickPermanentEquipmentReward()
        {
            var candidates = new List<EquipmentData>();
            if (_equipment?.Catalog?.Equipment != null)
                foreach (EquipmentData item in _equipment.Catalog.Equipment)
                    if (item != null && !_equipment.IsOwned(item.EquipmentId))
                        candidates.Add(item);
            return PickPermanentReward(candidates);
        }

        RelicDefinition PickPermanentRelicReward()
        {
            var candidates = new List<RelicDefinition>();
            if (_relics?.Catalog?.Relics != null)
                foreach (RelicDefinition relic in _relics.Catalog.Relics)
                    if (relic != null && _relics.CanAcquire(relic.Family))
                        candidates.Add(relic);
            return PickPermanentReward(candidates);
        }

        T PickPermanentReward<T>(List<T> candidates) where T : class
        {
            if (candidates == null || candidates.Count == 0)
                return null;
            int seed = unchecked(
                (stageTimeline?.RandomSeed ?? 0) * 397 ^
                (_summonerProgression?.Snapshot.Level ?? 1) * 7919 ^
                (_permanentTraits?.TotalChoiceCount ?? 0) * 104729);
            return candidates[new System.Random(seed).Next(candidates.Count)];
        }

        public bool BeginRunTraitChoice(int clearedWave)
        {
            if (_runTraits == null || !_runTraits.BeginChoice(clearedWave))
                return false;
            SetPhase(RunPhase.TraitChoice);
            return true;
        }

        public bool TryChooseRunReward(
            string rewardId,
            out IReadOnlyList<SummonResult> summonResults)
        {
            var results = new List<SummonResult>(3);
            summonResults = results;
            if (_runTraits == null ||
                !_runTraits.TryChoose(rewardId, out RunRewardDefinition selected))
                return false;
            if (selected == null || !selected.IsImmediate || _summonManager == null)
                return true;

            switch (selected.Effect)
            {
                case RunRewardEffect.TripleSummon:
                    GrantRandomRunRewardUnits(Mathf.Max(1, selected.Count), 0, results);
                    break;
                case RunRewardEffect.MergeSupport:
                    int requested = Mathf.Max(1, selected.Count);
                    int granted = _summonManager.GrantMergeSupport(requested, results);
                    if (granted < requested)
                        GrantRandomRunRewardUnits(requested - granted, 0, results);
                    break;
                case RunRewardEffect.JackpotEgg:
                    if (_summonManager.TryGrantJackpotEgg(
                            selected.PrimaryValue,
                            selected.SecondaryValue,
                            out SummonResult jackpotResult))
                        results.Add(jackpotResult);
                    break;
            }
            return true;
        }

        public bool TryChooseCombatBuild(string rewardId) =>
            _combatBuild?.TryChoose(rewardId) ?? false;

        void GrantRandomRunRewardUnits(int amount, int rank, List<SummonResult> results)
        {
            for (int i = 0; i < Mathf.Max(0, amount); i++)
            {
                if (!_summonManager.TryGrantRandomReward(rank, out SummonResult result))
                    break;
                results.Add(result);
            }
        }

        public void SetPhase(RunPhase phase)
        {
            if (_phase == phase) return;
            _phase = phase;
            if (phase == RunPhase.Defeat)
            {
                _resumeWaveIndex = 0;
                _wallet?.Flush();
                _merchant?.Close();
                SaveSameStageRestartSession();
            }
            else if (phase == RunPhase.Victory)
            {
                _wallet?.Flush();
                _merchant?.Close();
                SaveRunSession(true);
            }
            else
                SaveRunSession(false);
            if (phase == RunPhase.InWave && waveStartSfx != null && _sfxSource != null)
                _sfxSource.PlayOneShot(waveStartSfx, waveStartSfxVolume);
            PhaseChanged?.Invoke(phase);
            Debug.Log($"[CrossDefense] Phase: {phase}", this);
        }

        public void SetGameplayPause(GameplayPauseReason reason, bool paused)
        {
            if (reason == GameplayPauseReason.None)
                return;

            GameplayPauseReason previous = _gameplayPauseReasons;
            _gameplayPauseReasons = paused
                ? _gameplayPauseReasons | reason
                : _gameplayPauseReasons & ~reason;
            if (previous == _gameplayPauseReasons)
                return;

            if (previous == GameplayPauseReason.None)
            {
                _timeScaleBeforeGameplayPause = Time.timeScale;
                Time.timeScale = 0f;
                GameplayPauseChanged?.Invoke(true);
                return;
            }

            if (_gameplayPauseReasons != GameplayPauseReason.None)
                return;

            Time.timeScale = _timeScaleBeforeGameplayPause;
            GameplayPauseChanged?.Invoke(false);
        }

        public void SetTutorialWaveHold(bool held) => _tutorialWaveHold = held;

        public void SetEffectsVolume(float volume)
        {
            GameAudioSettings.SetEffectsVolume(volume);
            if (_sfxSource != null)
                _sfxSource.volume = GameAudioSettings.EffectsVolume;
        }

        public void ToggleGameplaySpeed()
        {
            SetGameplaySpeed(
                Mathf.Approximately(_gameplaySpeed, FastGameplaySpeed)
                    ? NormalGameplaySpeed
                    : FastGameplaySpeed);
        }

        public void SetGameplaySpeed(float speed)
        {
            float next = speed >= 1.25f ? FastGameplaySpeed : NormalGameplaySpeed;
            if (Mathf.Approximately(_gameplaySpeed, next))
                return;

            _gameplaySpeed = next;
            _timeScaleBeforeGameplayPause = next;
            if (!IsGameplayPaused)
                Time.timeScale = next;
            PlayerPrefs.SetInt(
                GameplaySpeedPrefsKey,
                Mathf.Approximately(next, FastGameplaySpeed) ? 1 : 0);
            PlayerPrefs.Save();
            GameplaySpeedChanged?.Invoke(next);
        }

        void RestoreGameplayTimeScale()
        {
            if (_gameplayPauseReasons == GameplayPauseReason.None)
                return;

            _gameplayPauseReasons = GameplayPauseReason.None;
            Time.timeScale = _timeScaleBeforeGameplayPause;
            GameplayPauseChanged?.Invoke(false);
        }

        public void SetWave(int current, int total, StageWave wave)
        {
            _currentWaveData = wave;
            _resumeWaveIndex = Mathf.Max(0, current - 1);
            WaveChanged?.Invoke(current, total);
            SaveRunSession(true);
            Debug.Log($"[CrossDefense] Wave {current}/{total}: {wave.Label} prep={wave.PreparationTime:0.##} monsters={wave.TotalMonsterCount}", this);
        }

        public void NotifyMonsterSpawned(MonsterController monster, StageWave wave, int livingCount)
        {
            _monsterCodex?.RecordEncounter(monster?.Data);
            MonsterSpawned?.Invoke(monster, wave, livingCount);
            LivingMonsterCountChanged?.Invoke(livingCount);
        }

        public void NotifyGoldenGoblinWarning(float warningLeadTime)
        {
            GoldenGoblinStateChanged?.Invoke(new GoldenGoblinSnapshot(
                GoldenGoblinState.Warning,
                warningLeadTime,
                warningLeadTime,
                stageTimeline?.GoldenGoblin?.TotalGoldReward ?? 0));
        }

        public void NotifyGoldenGoblinSpawned(MonsterController monster)
        {
            _activeGoldenGoblin = monster;
            _nextGoldenGoblinUiTime = 0f;
            GoldenGoblinStateChanged?.Invoke(new GoldenGoblinSnapshot(
                GoldenGoblinState.Active,
                monster?.GoldenEscapeTimeRemaining ?? 0f,
                monster?.GoldenEscapeDuration ?? 0f,
                stageTimeline?.GoldenGoblin?.TotalGoldReward ?? 0));
        }

        public void NotifyGoldenGoblinSpawnFailed()
        {
            _activeGoldenGoblin = null;
            GoldenGoblinStateChanged?.Invoke(new GoldenGoblinSnapshot(
                GoldenGoblinState.Hidden,
                0f,
                0f,
                0));
        }

        public void NotifyGoldenGoblinEscaped(MonsterController monster)
        {
            if (monster == null || _waveManager == null)
                return;
            _waveManager.NotifyMonsterResolved(monster);
            _monsterSpawner.Release(monster);
            MonsterResolved?.Invoke(monster);
            if (_activeGoldenGoblin == monster)
                _activeGoldenGoblin = null;
            GoldenGoblinStateChanged?.Invoke(new GoldenGoblinSnapshot(
                GoldenGoblinState.Escaped,
                0f,
                0f,
                0));
            LivingMonsterCountChanged?.Invoke(_waveManager.LivingMonsterCount);
        }

        public void NotifyMonsterDefeated(MonsterController monster, int rewardGold)
        {
            if (_waveManager == null) return;
            bool goldenGoblin = monster != null && monster.IsGoldenRunner;
            bool grantsRewards = monster == null || monster.GrantsDefeatRewards;
            AudioClip deathSfx = goblinDeathSfx;
            if (goblinDeathKiekSfx != null &&
                (deathSfx == null || UnityEngine.Random.value < 0.5f))
                deathSfx = goblinDeathKiekSfx;
            if (deathSfx != null && _sfxSource != null)
                _sfxSource.PlayOneShot(deathSfx, goblinDeathSfxVolume);
            Vector3 rewardOrigin = monster != null ? monster.transform.position : Vector3.zero;
            float deathEffectScale = monster != null
                ? Mathf.Clamp(monster.CombatRadius * 2.4f, 0.65f, 2.4f)
                : 1f;
            _monsterDeathEffects?.PlayFrames(
                rewardOrigin,
                runtimeGoblinDeathEffectFrames,
                Color.white,
                deathEffectScale,
                18f);
            int previousSummonerLevel = _summonerProgression?.Snapshot.Level ?? 1;
            if (grantsRewards)
            {
                int experienceReward = growthBalance.SummonerExperienceReward(rewardGold);
                _summonerProgression?.AddExperience(experienceReward);
                _monsterCodex?.RecordKill(monster?.Data);
                _dopamineController?.NotifyMonsterDefeated();
            }
            int currentSummonerLevel = _summonerProgression?.Snapshot.Level ?? previousSummonerLevel;
            _combatBuild?.NotifySummonerLevelChanged(
                previousSummonerLevel,
                currentSummonerLevel);
            _waveManager.NotifyMonsterDefeated(monster);
            _monsterSpawner.Release(monster);
            MonsterResolved?.Invoke(monster);
            int baseGold = grantsRewards
                ? goldenGoblin
                    ? stageTimeline?.GoldenGoblin?.TotalGoldReward ?? rewardGold
                    : rewardGold
                : 0;
            int goldAward = Mathf.Max(
                0,
                Mathf.RoundToInt(baseGold * (_runRelics?.GoldMultiplier ?? 1f)));
            if (goldAward > 0)
            {
                if (_goldRewardFlow != null)
                    _goldRewardFlow.Present(rewardOrigin, goldAward);
                else
                    AddGold(goldAward);
            }
            if (goldenGoblin)
            {
                if (_activeGoldenGoblin == monster)
                    _activeGoldenGoblin = null;
                GoldenGoblinStateChanged?.Invoke(new GoldenGoblinSnapshot(
                    GoldenGoblinState.Defeated,
                    0f,
                    0f,
                    goldAward));
            }
            LivingMonsterCountChanged?.Invoke(_waveManager.LivingMonsterCount);
        }

        void OnSummonerProgressionChanged(SummonerProgressionSnapshot snapshot)
        {
            _summonerBuffLoadout?.EnsureUnlockedSlotsFilled();
            RecalculateEffectiveCoreHp(true);
        }

        void OnPermanentTraitsChanged(PermanentTraitSnapshot snapshot)
        {
            RecalculateEffectiveCoreHp(true);
        }

        void OnEquipmentChanged() => RecalculateEffectiveCoreHp(true);

        void OnRunRelicsChanged()
        {
            RecalculateEffectiveCoreHp(true);
            SaveRunSession(true);
        }

        void OnRunTraitsChanged(RunTraitSnapshot snapshot) => SaveRunSession(true);

        void OnBenchRosterChanged(IReadOnlyList<SummonUnitInstance> _) =>
            SaveRunSession(true);

        void OnFieldRosterChanged(IReadOnlyList<SummonedUnitController> _) =>
            SaveRunSession(true);

        void SaveRunSession(bool flush)
        {
            if (!_runSessionActive || _suppressRunSessionSave || _runSession == null)
                return;

            _runSession.Save(new RunSessionSaveData
            {
                healthCheckpointVersion = 1,
                runEventSeed = _runEventSeed,
                stageId = stageTimeline?.StageId ?? string.Empty,
                waveIndex = Mathf.Max(0, _resumeWaveIndex),
                gold = Mathf.Max(0, _gold),
                summonContracts = Mathf.Max(0, _summonContracts),
                coreHp = Mathf.Max(0f, _coreHp),
                coreHpRatio = _effectiveMaxCoreHp > 0f
                    ? Mathf.Clamp01(_coreHp / _effectiveMaxCoreHp)
                    : 0f,
                runRelicIds = _runRelics?.CaptureOwnedIds() ?? new List<string>(),
                runTraits = _runTraits?.CaptureSaveData() ?? new RunTraitProgressionSaveData(),
                summonedUnits = CaptureSummonedUnits(),
            }, flush);
        }

        void SaveSameStageRestartSession()
        {
            if (_runSession == null || _suppressRunSessionSave)
                return;

            _resumeWaveIndex = 0;
            _runEventSeed = CreateRunEventSeed();
            _runSession.Save(new RunSessionSaveData
            {
                healthCheckpointVersion = 1,
                runEventSeed = _runEventSeed,
                stageId = stageTimeline?.StageId ?? string.Empty,
                waveIndex = 0,
                gold = Mathf.Max(0, _gold),
                summonContracts = Mathf.Max(0, _summonContracts),
                coreHp = Mathf.Max(0f, _effectiveMaxCoreHp),
                coreHpRatio = 1f,
                runRelicIds = _runRelics?.CaptureOwnedIds() ?? new List<string>(),
                runTraits = _runTraits?.CaptureSaveData() ?? new RunTraitProgressionSaveData(),
                summonedUnits = new List<RunSessionSummonSaveData>(),
            }, true);

            // Scene destruction must not overwrite the death checkpoint with the defeated roster.
            _suppressRunSessionSave = true;
        }

        internal static int ResolveStartingSummonContracts(
            RunSessionSaveData checkpoint,
            int startingAmount) =>
            Mathf.Max(
                0,
                checkpoint?.healthCheckpointVersion > 0
                    ? checkpoint.summonContracts
                    : startingAmount);

        void ClearRunSession()
        {
            _runSessionActive = false;
            _suppressRunSessionSave = true;
            _runRelics?.Clear();
            _suppressRunSessionSave = false;
            _runSession?.Save(new RunSessionSaveData
            {
                stageId = stageTimeline?.StageId ?? string.Empty,
                waveIndex = Mathf.Max(0, _resumeWaveIndex),
            }, true);
        }

        List<RunSessionSummonSaveData> CaptureSummonedUnits()
        {
            var saved = new List<RunSessionSummonSaveData>();
            var seen = new HashSet<int>();

            void Add(SummonUnitInstance instance, bool isDeployed, float hpRatio)
            {
                if (instance?.Unit == null || !seen.Add(instance.InstanceId))
                    return;
                saved.Add(new RunSessionSummonSaveData
                {
                    unitId = instance.Unit.UnitId,
                    rank = SummonRank.Clamp(instance.Rank),
                    isDeployed = isDeployed,
                    hpRatio = Mathf.Clamp01(hpRatio),
                });
            }

            if (_summonedUnitManager?.Units != null)
                foreach (SummonedUnitController unit in _summonedUnitManager.Units)
                {
                    if (unit != null && !unit.IsDefeated && unit.MaxHp > 0f)
                        Add(unit.Instance, true, unit.CurrentHp / unit.MaxHp);
                }
            if (_summonManager?.Bench != null)
                foreach (SummonUnitInstance instance in _summonManager.Bench)
                    Add(instance, false, 1f);
            return saved;
        }

        void RestoreSummonedUnits(
            IReadOnlyList<RunSessionSummonSaveData> saved,
            bool restoreHealth)
        {
            if (saved == null || saved.Count == 0 || _summonManager?.Pool == null)
                return;

            _suppressRunSessionSave = true;
            int restored = 0;
            for (int i = 0; i < saved.Count; i++)
            {
                RunSessionSummonSaveData entry = saved[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.unitId) ||
                    restoreHealth && entry.hpRatio <= 0f)
                    continue;
                SummonUnitData data = null;
                foreach (SummonUnitData candidate in _summonManager.Pool)
                {
                    if (candidate != null && candidate.UnitId == entry.unitId)
                    {
                        data = candidate;
                        break;
                    }
                }
                if (data != null &&
                    _summonManager.TryGrantRewardUnit(
                        data,
                        SummonRank.Clamp(entry.rank),
                        out _,
                        out SummonUnitInstance instance))
                {
                    restored++;
                    if (restoreHealth && entry.isDeployed)
                    {
                        if (!_summonedUnitManager.TryRestoreUnitHealth(
                                instance.InstanceId,
                                entry.hpRatio))
                        {
                            _summonedUnitManager.TryAutoDeploy(instance.InstanceId);
                            _summonedUnitManager.TryRestoreUnitHealth(
                                instance.InstanceId,
                                entry.hpRatio);
                        }
                    }
                }
            }
            _suppressRunSessionSave = false;
            if (restored > 0)
                Debug.Log($"[CrossDefense] 슬라임 체크포인트 복구: {restored}마리", this);
        }

        void RecalculateEffectiveCoreHp(bool healAddedMaximum)
        {
            float previousMax = Mathf.Max(1f, _effectiveMaxCoreHp);
            float nextMax = Mathf.Max(
                1f,
                maxCoreHp *
                (_summonerProgression?.Snapshot.MaxHpMultiplier ?? 1f) *
                (_permanentTraits?.Snapshot.CoreMaxHpMultiplier ?? 1f) *
                (_equipment?.MaxHpMultiplier ?? 1f) *
                (_runRelics?.MaxHpMultiplier ?? 1f));
            _effectiveMaxCoreHp = nextMax;
            if (healAddedMaximum && nextMax > previousMax)
                _coreHp = Mathf.Min(nextMax, _coreHp + nextMax - previousMax);
            else
                _coreHp = Mathf.Min(_coreHp, nextMax);
            _summonerHealthBar?.SetHealth(_coreHp, nextMax);
            CoreHpChanged?.Invoke(_coreHp, nextMax);
        }

        static int CreateRunEventSeed()
        {
            int seed = Guid.NewGuid().GetHashCode() & int.MaxValue;
            return seed == 0 ? 1 : seed;
        }

        void OnDestroy()
        {
            RestoreGameplayTimeScale();
            Time.timeScale = NormalGameplaySpeed;
            SaveRunSession(true);
            _wallet?.Flush();
            if (_summonerProgression != null)
            {
                _summonerProgression.Flush();
                _summonerProgression.Changed -= OnSummonerProgressionChanged;
            }
            if (_permanentTraits != null)
            {
                _permanentTraits.Flush();
                _permanentTraits.Changed -= OnPermanentTraitsChanged;
            }
            _summonerSkillLoadout?.Flush();
            _summonerBuffLoadout?.Flush();
            _growthManager?.Flush();
            _monsterCodex?.Flush();
            _equipment?.Flush();
            _relics?.Flush();
            if (_equipment != null) _equipment.Changed -= OnEquipmentChanged;
            if (_summonManager != null)
                _summonManager.BenchChanged -= OnBenchRosterChanged;
            if (_summonedUnitManager != null)
                _summonedUnitManager.UnitsChanged -= OnFieldRosterChanged;
            if (_runRelics != null) _runRelics.Changed -= OnRunRelicsChanged;
            if (_runTraits != null) _runTraits.Changed -= OnRunTraitsChanged;
            if (_runtimeGrowthBalance != null)
                Destroy(_runtimeGrowthBalance);
            if (_runtimeDopamineBalance != null)
                Destroy(_runtimeDopamineBalance);
            if (_runtimeRunRewardCatalog != null)
                Destroy(_runtimeRunRewardCatalog);
            if (_runtimeMonsterCatalog != null)
                Destroy(_runtimeMonsterCatalog);
            if (_runtimeEquipmentCatalog != null)
                Destroy(_runtimeEquipmentCatalog);
            if (_runtimeRelicCatalog != null)
                Destroy(_runtimeRelicCatalog);
            if (_runtimeMerchantCatalog != null)
                Destroy(_runtimeMerchantCatalog);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveRunSession(true);
                _wallet?.Flush();
                _summonerProgression?.Flush();
                _permanentTraits?.Flush();
                _summonerSkillLoadout?.Flush();
                _summonerBuffLoadout?.Flush();
                _growthManager?.Flush();
                _monsterCodex?.Flush();
                _equipment?.Flush();
                _relics?.Flush();
            }
        }

        void OnApplicationQuit()
        {
            SaveRunSession(true);
            _wallet?.Flush();
            _summonerProgression?.Flush();
            _permanentTraits?.Flush();
            _summonerSkillLoadout?.Flush();
            _summonerBuffLoadout?.Flush();
            _growthManager?.Flush();
            _monsterCodex?.Flush();
            _equipment?.Flush();
            _relics?.Flush();
        }

    }
}
