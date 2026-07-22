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

        [Header("Defeat Restart")]
        [SerializeField] bool restartStageOnDefeat = true;
        [Min(0f)] [SerializeField] float defeatRestartDelay = 1.25f;

        [Header("Growth")]
        [SerializeField] GrowthBalanceData growthBalance;

        [Header("Persistent Collections")]
        [SerializeField] MonsterCatalog monsterCatalog;
        [SerializeField] EquipmentCatalog equipmentCatalog;
        [SerializeField] MerchantCatalog merchantCatalog;

        [Header("Dopamine")]
        [SerializeField] DopamineBalanceData dopamineBalance;

        [Header("Summon Roulette")]
        [SerializeField] List<SummonUnitData> summonPool = new();
        [SerializeField] Sprite runtimePrototypeSummonSprite;
        [SerializeField] Sprite runtimePrototypePunchSprite;
        [SerializeField] Sprite runtimePrototypeWatergunSprite;
        [SerializeField] Sprite runtimePrototypeFlameSprite;
        [SerializeField] Sprite runtimePrototypeIceSprite;
        [SerializeField] Sprite runtimePrototypeGreenSprite;
        [SerializeField] Sprite runtimePrototypeBuffSprite;
        [SerializeField] Sprite runtimePrototypeExplosionSprite;
        [SerializeField] Sprite runtimePrototypeFreezeSprite;
        [Header("Summon Rank Sprites")]
        [SerializeField] Sprite runtimePrototypePunchStar2Sprite;
        [SerializeField] Sprite runtimePrototypePunchStar3Sprite;
        [SerializeField] Sprite runtimePrototypeWatergunStar2Sprite;
        [SerializeField] Sprite runtimePrototypeWatergunStar3Sprite;
        [SerializeField] Sprite runtimePrototypeFlameStar2Sprite;
        [SerializeField] Sprite runtimePrototypeFlameStar3Sprite;
        [SerializeField] Sprite runtimePrototypeIceStar2Sprite;
        [SerializeField] Sprite runtimePrototypeIceStar3Sprite;
        [SerializeField] Sprite runtimePrototypeGreenStar2Sprite;
        [SerializeField] Sprite runtimePrototypeGreenStar3Sprite;
        [SerializeField] Sprite runtimePrototypeBuffStar2Sprite;
        [SerializeField] Sprite runtimePrototypeBuffStar3Sprite;
        [SerializeField] Sprite runtimePrototypeExplosionStar2Sprite;
        [SerializeField] Sprite runtimePrototypeExplosionStar3Sprite;
        [SerializeField] Sprite runtimePrototypeFreezeStar2Sprite;
        [SerializeField] Sprite runtimePrototypeFreezeStar3Sprite;
        [SerializeField] Sprite[] runtimePrototypePunchMoveFrames;
        [Min(1f)] [SerializeField] float runtimePrototypePunchMoveFps = 9f;
        [SerializeField] Sprite runtimePrototypeNeutralProjectileSprite;
        [SerializeField] Sprite runtimePrototypeFireProjectileSprite;
        [SerializeField] Sprite runtimePrototypeIceProjectileSprite;
        [Header("Rank Projectile Visuals")]
        [SerializeField] Sprite[] runtimePrototypeWatergunProjectiles;
        [SerializeField] Sprite[] runtimePrototypeFlameProjectiles;
        [SerializeField] Sprite[] runtimePrototypeIceProjectiles;
        [SerializeField] Sprite[] runtimePrototypeGreenProjectiles;
        [SerializeField] Sprite[] runtimePrototypeExplosionProjectiles;
        [SerializeField] Sprite[] runtimePrototypeFreezeProjectiles;
        [Header("Star 3 Skill Effects")]
        [SerializeField] Sprite runtimeStar3NeutralEffectSprite;
        [SerializeField] Sprite runtimeStar3FireEffectSprite;
        [SerializeField] Sprite runtimeStar3IceEffectSprite;
        [SerializeField] Sprite runtimeStar3NatureEffectSprite;
        [SerializeField] Sprite[] runtimeExplosionStar3EffectFrames;
        [Header("Summoner Active Skill Effects")]
        [SerializeField] Sprite runtimeMeteorProjectileSprite;
        [SerializeField] Sprite[] runtimeMeteorEffectFrames;
        [SerializeField] Sprite[] runtimeIceWallEffectFrames;
        [SerializeField] Sprite runtimeAegisEffectSprite;
        [Header("Monster Death Effect")]
        [SerializeField] Sprite[] runtimeGoblinDeathEffectFrames;
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
        MerchantCatalog _runtimeMerchantCatalog;
        WorldHealthBar _summonerHealthBar;
        SpriteRenderer _summonerRenderer;
        DamageFloatingTextService _damageFloatingText;
        CombatEffectService _monsterDeathEffects;
        SummonerProgression _summonerProgression;
        PermanentTraitProgression _permanentTraits;
        RunTraitProgression _runTraits;
        GrowthManager _growthManager;
        SummonerSkillLoadout _summonerSkillLoadout;
        SummonerSkillController _summonerSkillController;
        DopamineController _dopamineController;
        MonsterCodexProgression _monsterCodex;
        EquipmentProgression _equipment;
        RunRelicInventory _runRelics;
        MerchantManager _merchant;
        Coroutine _defeatRestartRoutine;
        float _coreHp;
        float _effectiveMaxCoreHp;
        float _coreShieldHp;
        float _coreShieldUntil;
        int _gold;
        int _summonContracts;
        RunPhase _phase;
        StageWave _currentWaveData;
        readonly HashSet<int> _grantedRushGoldWaves = new();
        GameplayPauseReason _gameplayPauseReasons;
        float _timeScaleBeforeGameplayPause = 1f;

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
        public GrowthManager Growth => _growthManager;
        public SummonerSkillLoadout SummonerSkillLoadout => _summonerSkillLoadout;
        public SummonerSkillController SummonerSkills => _summonerSkillController;
        public DopamineController Dopamine => _dopamineController;
        public MonsterCatalog MonsterCatalog => monsterCatalog;
        public MonsterCodexProgression MonsterCodex => _monsterCodex;
        public EquipmentProgression Equipment => _equipment;
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
        public GameplayPauseReason GameplayPauseReasons => _gameplayPauseReasons;
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
            (_runRelics?.AttackSpeedMultiplier ?? 1f);
        public float SlimeAttackSpeedMultiplier =>
            RunAttackSpeedMultiplier *
            (_permanentTraits?.Snapshot.SlimeAttackSpeedMultiplier ?? 1f) *
            (_runRelics?.AttackSpeedMultiplier ?? 1f);
        public bool IsRunTraitChoicePending => _runTraits?.IsChoicePending ?? false;
        public bool IsMerchantOpen => _merchant?.IsOpen ?? false;
        public CombatProjectileService Projectiles => _summonedUnitManager?.Projectiles;
        public bool IsRunOver => _phase == RunPhase.Victory || _phase == RunPhase.Defeat;
        public int CurrentWave => _waveManager == null ? 0 : _waveManager.CurrentWaveIndex + 1;
        public int TotalWaves => _waveManager == null ? 0 : _waveManager.TotalWaves;
        public int LivingMonsterCount => _waveManager == null ? 0 : _waveManager.LivingMonsterCount;
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

        void OnValidate()
        {
            if (gameplayBackground == null)
                gameplayBackground = transform.Find("Background")?.GetComponent<SpriteRenderer>();
            monsterSpawnMinOutsideDistance = Mathf.Max(0.1f, monsterSpawnMinOutsideDistance);
            monsterSpawnMaxOutsideDistance = Mathf.Max(
                monsterSpawnMinOutsideDistance,
                monsterSpawnMaxOutsideDistance);
            summonedUnitTargetSearchRange = Mathf.Max(0.1f, summonedUnitTargetSearchRange);
            runtimePrototypePunchMoveFps = Mathf.Max(1f, runtimePrototypePunchMoveFps);
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
            _permanentTraits = PermanentTraitProgression.CreatePersistent(
                growthBalance,
                () => _summonerProgression.Snapshot.Level);
            _permanentTraits.Changed += OnPermanentTraitsChanged;
            _summonerSkillLoadout = SummonerSkillLoadout.CreatePersistent(
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
            _runRelics = new RunRelicInventory();
            _runRelics.Changed += OnRunRelicsChanged;
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

            maxCoreHp = Mathf.Max(1f, maxCoreHp);
            _effectiveMaxCoreHp = maxCoreHp *
                                  _summonerProgression.Snapshot.MaxHpMultiplier *
                                  _permanentTraits.Snapshot.CoreMaxHpMultiplier;
            _coreHp = _effectiveMaxCoreHp;
            _gold = Mathf.Max(0, startingGold);
            _summonContracts = Mathf.Max(0, startingSummonContracts);
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
            _growthManager = new GrowthManager(this, _summonManager, growthBalance);
            _merchant = new MerchantManager(this, merchantCatalog, _equipment, _runRelics);

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

            _summonerSkillController = GetComponent<SummonerSkillController>();
            if (_summonerSkillController == null)
                _summonerSkillController = gameObject.AddComponent<SummonerSkillController>();
            _summonerSkillController.Initialize(
                this,
                summoner != null ? summoner.GetComponent<SummonerAttackController>() : null,
                _summonerSkillLoadout,
                runtimeMeteorProjectileSprite,
                runtimeMeteorEffectFrames,
                runtimeIceWallEffectFrames,
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
                stageTimeline?.RandomSeed ?? 0);

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
        }

        IEnumerable<SummonUnitData> BuildSummonPool()
        {
            bool hasConfiguredUnit = false;
            foreach (var unit in summonPool)
            {
                if (unit == null) continue;
                hasConfiguredUnit = true;
                yield return unit;
            }

            if (hasConfiguredUnit) yield break;

            var punch = SummonUnitData.CreatePrototype(
                "punch-slime", "주먹 슬라임", SummonUnitRarity.Common, 100,
                ResolvePrototypeSprite(runtimePrototypePunchSprite), MonsterAttribute.None, SummonAttackStyle.Melee,
                10f, 1.15f, 0.85f, 2.8f,
                ResolvePrototypeTint(runtimePrototypePunchSprite, new Color(0.82f, 0.9f, 1f)));
            punch.ConfigurePrototypeRankSprites(
                runtimePrototypePunchStar2Sprite,
                runtimePrototypePunchStar3Sprite);
            punch.ConfigurePrototypeAnimation(runtimePrototypePunchMoveFrames, runtimePrototypePunchMoveFps);
            punch.ConfigurePrototypeStar3Skill(
                "대지 강타", Star3SkillMode.SelfArea, 9f, 2.2f, 1.35f,
                0f, 1f, 1, runtimeStar3NeutralEffectSprite, 1.25f);
            punch.ConfigurePrototypeUnlockLevel(1);
            yield return punch;

            var watergun = SummonUnitData.CreatePrototype(
                "watergun-slime", "물총 슬라임", SummonUnitRarity.Common, 90,
                ResolvePrototypeSprite(runtimePrototypeWatergunSprite), MonsterAttribute.None, SummonAttackStyle.Projectile,
                7f, 1.35f, 4.5f, 2.35f,
                ResolvePrototypeTint(runtimePrototypeWatergunSprite, new Color(0.45f, 0.8f, 1f)));
            watergun.ConfigurePrototypeRankSprites(
                runtimePrototypeWatergunStar2Sprite,
                runtimePrototypeWatergunStar3Sprite);
            watergun.ConfigurePrototypeEffects(runtimePrototypeNeutralProjectileSprite, 0f, 0f, 0f, 0f, 0f, 1);
            watergun.ConfigurePrototypeRankProjectiles(runtimePrototypeWatergunProjectiles);
            watergun.ConfigurePrototypeStar3Skill(
                "초압축 수포", Star3SkillMode.PiercingProjectile, 8f, 2.3f, 0f,
                0f, 1f, 3, runtimeStar3NeutralEffectSprite, 1.4f);
            watergun.ConfigurePrototypeUnlockLevel(2);
            yield return watergun;

            var flame = SummonUnitData.CreatePrototype(
                "flame-slime", "불꽃 슬라임", SummonUnitRarity.Common, 72,
                ResolvePrototypeSprite(runtimePrototypeFlameSprite), MonsterAttribute.Fire, SummonAttackStyle.Area,
                8f, 0.85f, 2.1f, 2.2f,
                ResolvePrototypeTint(runtimePrototypeFlameSprite, new Color(1f, 0.42f, 0.22f)));
            flame.ConfigurePrototypeRankSprites(
                runtimePrototypeFlameStar2Sprite,
                runtimePrototypeFlameStar3Sprite);
            flame.ConfigurePrototypeEffects(runtimePrototypeFireProjectileSprite, 1.15f, 0f, 0f, 0f, 0f, 1);
            flame.ConfigurePrototypeRankProjectiles(runtimePrototypeFlameProjectiles);
            flame.ConfigurePrototypeStar3Skill(
                "작열핵", Star3SkillMode.TargetArea, 10f, 2f, 1.6f,
                0f, 1f, 1, runtimeStar3FireEffectSprite, 1.45f,
                skillDotMultiplier: 0.22f, skillDotDuration: 4f);
            flame.ConfigurePrototypeUnlockLevel(6);
            yield return flame;

            var ice = SummonUnitData.CreatePrototype(
                "ice-slime", "얼음 슬라임", SummonUnitRarity.Rare, 52,
                ResolvePrototypeSprite(runtimePrototypeIceSprite), MonsterAttribute.Ice, SummonAttackStyle.Projectile,
                5.5f, 1f, 4.2f, 2.2f,
                ResolvePrototypeTint(runtimePrototypeIceSprite, new Color(0.55f, 0.9f, 1f)));
            ice.ConfigurePrototypeRankSprites(
                runtimePrototypeIceStar2Sprite,
                runtimePrototypeIceStar3Sprite);
            ice.ConfigurePrototypeEffects(runtimePrototypeIceProjectileSprite, 0f, 0.32f, 2f, 0f, 0f, 1);
            ice.ConfigurePrototypeRankProjectiles(runtimePrototypeIceProjectiles);
            ice.ConfigurePrototypeStar3Skill(
                "빙하 파동", Star3SkillMode.TargetArea, 11f, 1.6f, 1.45f,
                0f, 1f, 1, runtimeStar3IceEffectSprite, 1.35f,
                skillSlowPercent: 0.55f, skillSlowDuration: 3f);
            ice.ConfigurePrototypeUnlockLevel(8);
            yield return ice;

            var green = SummonUnitData.CreatePrototype(
                "green-slime", "초록 슬라임", SummonUnitRarity.Rare, 45,
                ResolvePrototypeSprite(runtimePrototypeGreenSprite), MonsterAttribute.Nature, SummonAttackStyle.Projectile,
                4.5f, 1f, 4f, 2.2f,
                ResolvePrototypeTint(runtimePrototypeGreenSprite, new Color(0.5f, 1f, 0.48f)));
            green.ConfigurePrototypeRankSprites(
                runtimePrototypeGreenStar2Sprite,
                runtimePrototypeGreenStar3Sprite);
            green.ConfigurePrototypeEffects(runtimePrototypeNeutralProjectileSprite, 0f, 0f, 0f, 3f, 2.5f, 1);
            green.ConfigurePrototypeRankProjectiles(runtimePrototypeGreenProjectiles);
            green.ConfigurePrototypeStar3Skill(
                "맹독 개화", Star3SkillMode.TargetArea, 10f, 1.2f, 1.5f,
                0f, 1f, 1, runtimeStar3NatureEffectSprite, 1.4f,
                skillDotMultiplier: 0.35f, skillDotDuration: 5f);
            green.ConfigurePrototypeUnlockLevel(12);
            yield return green;

            var buff = SummonUnitData.CreatePrototype(
                "buff-slime", "버프 슬라임", SummonUnitRarity.Rare, 38,
                ResolvePrototypeSprite(runtimePrototypeBuffSprite), MonsterAttribute.None, SummonAttackStyle.Support,
                0.1f, 1f, 0.8f, 2f,
                ResolvePrototypeTint(runtimePrototypeBuffSprite, new Color(0.78f, 0.55f, 1f)));
            buff.ConfigurePrototypeRankSprites(
                runtimePrototypeBuffStar2Sprite,
                runtimePrototypeBuffStar3Sprite);
            buff.ConfigurePrototypeEffects(
                null, 0f, 0f, 0f, 0f, 0f, 1, 0.16f, 2.8f, 2f,
                new[] { 0.03f, 0.04f, 0.05f });
            buff.ConfigurePrototypeStar3Skill(
                "과충전 공명", Star3SkillMode.AuraOverdrive, 12f, 0f, 0f,
                5f, 2f, 1, runtimeStar3NatureEffectSprite, 1.35f);
            buff.ConfigurePrototypeUnlockLevel(16);
            yield return buff;

            var explosion = SummonUnitData.CreatePrototype(
                "explosion-slime", "폭발 슬라임", SummonUnitRarity.Rare, 32,
                ResolvePrototypeSprite(runtimePrototypeExplosionSprite), MonsterAttribute.Fire, SummonAttackStyle.Area,
                15f, 0.55f, 1.1f, 2.7f,
                ResolvePrototypeTint(runtimePrototypeExplosionSprite, new Color(1f, 0.62f, 0.3f)));
            explosion.ConfigurePrototypeRankSprites(
                runtimePrototypeExplosionStar2Sprite,
                runtimePrototypeExplosionStar3Sprite);
            explosion.ConfigurePrototypeEffects(runtimePrototypeFireProjectileSprite, 1.45f, 0f, 0f, 0f, 0f, 1);
            explosion.ConfigurePrototypeRankProjectiles(runtimePrototypeExplosionProjectiles);
            explosion.ConfigurePrototypeStar3Skill(
                "대폭발", Star3SkillMode.SelfArea, 13f, 3.2f, 1.9f,
                0f, 1f, 1, runtimeStar3FireEffectSprite, 1.75f);
            explosion.ConfigurePrototypeStar3SkillFrames(runtimeExplosionStar3EffectFrames);
            explosion.ConfigurePrototypeUnlockLevel(20);
            yield return explosion;

            var freeze = SummonUnitData.CreatePrototype(
                "freeze-slime", "빙결 슬라임", SummonUnitRarity.Legendary, 18,
                ResolvePrototypeSprite(runtimePrototypeFreezeSprite), MonsterAttribute.Ice, SummonAttackStyle.Piercing,
                13f, 0.65f, 6f, 1.9f,
                ResolvePrototypeTint(runtimePrototypeFreezeSprite, new Color(0.65f, 0.72f, 1f)));
            freeze.ConfigurePrototypeRankSprites(
                runtimePrototypeFreezeStar2Sprite,
                runtimePrototypeFreezeStar3Sprite);
            freeze.ConfigurePrototypeEffects(runtimePrototypeIceProjectileSprite, 0f, 0.15f, 1f, 0f, 0f, 3);
            freeze.ConfigurePrototypeRankProjectiles(runtimePrototypeFreezeProjectiles);
            freeze.ConfigurePrototypeStar3Skill(
                "절대관통창", Star3SkillMode.PiercingProjectile, 14f, 2.8f, 0f,
                0f, 1f, 6, runtimeStar3IceEffectSprite, 1.55f,
                skillSlowPercent: 0.3f, skillSlowDuration: 2f);
            freeze.ConfigurePrototypeUnlockLevel(24);
            yield return freeze;
        }

        Sprite ResolvePrototypeSprite(Sprite configuredSprite) =>
            configuredSprite != null ? configuredSprite : runtimePrototypeSummonSprite;

        static Color ResolvePrototypeTint(Sprite configuredSprite, Color fallbackTint) =>
            configuredSprite != null ? Color.white : fallbackTint;

        void Start()
        {
            if (autoStart)
                StartRun();
        }

        public void StartRun()
        {
            if (_waveManager == null) return;
            _waveManager.RunFrom(this);
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
                if (restartStageOnDefeat && _defeatRestartRoutine == null)
                    _defeatRestartRoutine = StartCoroutine(RestartStageAfterDefeat());
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
            _summonerProgression?.Flush();
            _permanentTraits?.Flush();
            _summonerSkillLoadout?.Flush();
            _monsterCodex?.Flush();
            _equipment?.Flush();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
                SceneManager.LoadScene(scene.buildIndex);
            else if (!string.IsNullOrWhiteSpace(scene.name))
                SceneManager.LoadScene(scene.name);
        }

        public void AddGold(int amount)
        {
            _gold = Mathf.Max(0, _gold + amount);
            GoldChanged?.Invoke(_gold);
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0 || _gold < amount) return false;
            _gold -= amount;
            GoldChanged?.Invoke(_gold);
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
                                    (_runRelics?.DamageMultiplier ?? 1f);
            return _growthManager?.ModifyPlayerDamage(permanentDamage) ?? permanentDamage;
        }

        public float ModifySummonerDamage(float baseDamage)
        {
            float permanentDamage = Mathf.Max(0f, baseDamage) *
                                    (_summonerProgression?.Snapshot.DamageMultiplier ?? 1f) *
                                    (_permanentTraits?.Snapshot.SummonerDamageMultiplier ?? 1f) *
                                    (_equipment?.DamageMultiplier ?? 1f) *
                                    (_runRelics?.DamageMultiplier ?? 1f);
            return _growthManager?.ModifyPlayerDamage(
                permanentDamage,
                _equipment?.CriticalChanceBonus ?? 0f) ?? permanentDamage;
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
        }

        public bool TrySpendSummonContract()
        {
            if (_summonContracts <= 0) return false;
            _summonContracts--;
            SummonContractsChanged?.Invoke(_summonContracts);
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
            if (phase == RunPhase.Victory || phase == RunPhase.Defeat)
            {
                _merchant?.Close();
                _runRelics?.Clear();
            }
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
            WaveChanged?.Invoke(current, total);
            Debug.Log($"[CrossDefense] Wave {current}/{total}: {wave.Label} prep={wave.PreparationTime:0.##} monsters={wave.TotalMonsterCount}", this);
        }

        public void NotifyMonsterSpawned(MonsterController monster, StageWave wave, int livingCount)
        {
            _monsterCodex?.RecordEncounter(monster?.Data);
            MonsterSpawned?.Invoke(monster, wave, livingCount);
            LivingMonsterCountChanged?.Invoke(livingCount);
        }

        public void NotifyMonsterDefeated(MonsterController monster, int rewardGold)
        {
            if (_waveManager == null) return;
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
            _summonerProgression?.AddExperience(growthBalance.SummonerExperienceReward(rewardGold));
            _monsterCodex?.RecordKill(monster?.Data);
            _waveManager.NotifyMonsterDefeated(monster);
            _dopamineController?.NotifyMonsterDefeated();
            _monsterSpawner.Release(monster);
            MonsterResolved?.Invoke(monster);
            int goldAward = Mathf.Max(0, Mathf.RoundToInt(rewardGold * (_runRelics?.GoldMultiplier ?? 1f)));
            if (_goldRewardFlow != null)
                _goldRewardFlow.Present(rewardOrigin, goldAward);
            else
                AddGold(goldAward);
            LivingMonsterCountChanged?.Invoke(_waveManager.LivingMonsterCount);
        }

        void OnSummonerProgressionChanged(SummonerProgressionSnapshot snapshot)
        {
            RecalculateEffectiveCoreHp(true);
        }

        void OnPermanentTraitsChanged(PermanentTraitSnapshot snapshot)
        {
            RecalculateEffectiveCoreHp(true);
        }

        void OnEquipmentChanged() => RecalculateEffectiveCoreHp(true);
        void OnRunRelicsChanged() => RecalculateEffectiveCoreHp(true);

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

        void OnDestroy()
        {
            RestoreGameplayTimeScale();
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
            _monsterCodex?.Flush();
            _equipment?.Flush();
            if (_equipment != null) _equipment.Changed -= OnEquipmentChanged;
            if (_runRelics != null) _runRelics.Changed -= OnRunRelicsChanged;
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
            if (_runtimeMerchantCatalog != null)
                Destroy(_runtimeMerchantCatalog);
        }

        IEnumerator RestartStageAfterDefeat()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, defeatRestartDelay));
            RestartCurrentStage();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _summonerProgression?.Flush();
                _permanentTraits?.Flush();
                _summonerSkillLoadout?.Flush();
                _monsterCodex?.Flush();
                _equipment?.Flush();
            }
        }

        void OnApplicationQuit()
        {
            _summonerProgression?.Flush();
            _permanentTraits?.Flush();
            _summonerSkillLoadout?.Flush();
            _monsterCodex?.Flush();
            _equipment?.Flush();
        }

    }
}
