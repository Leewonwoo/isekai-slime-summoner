using System;
using System.Collections.Generic;
using CrossDefense.Data;
using CrossDefense.Units;
using UnityEngine;

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

        [Header("Summoner")]
        [SerializeField] Transform summoner;
        [Min(1f)] [SerializeField] float maxCoreHp = 100f;
        [Min(0)] [SerializeField] int startingGold = 0;
        [Min(0)] [SerializeField] int startingSummonContracts = 10;

        [Header("Summon Roulette")]
        [SerializeField] List<SummonUnitData> summonPool = new();
        [SerializeField] Sprite runtimePrototypeSummonSprite;
        [SerializeField] Sprite runtimePrototypeNeutralProjectileSprite;
        [SerializeField] Sprite runtimePrototypeFireProjectileSprite;
        [SerializeField] Sprite runtimePrototypeIceProjectileSprite;
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
        float _coreHp;
        int _gold;
        int _summonContracts;
        RunPhase _phase;

        public StageTimeline StageTimeline => stageTimeline;
        public Transform Summoner => summoner;
        public RunPhase Phase => _phase;
        public float CoreHp => _coreHp;
        public float MaxCoreHp => maxCoreHp;
        public int Gold => _gold;
        public int SummonContracts => _summonContracts;
        public SummonManager SummonManager => _summonManager;
        public SummonedUnitManager SummonedUnitManager => _summonedUnitManager;
        public CombatProjectileService Projectiles => _summonedUnitManager?.Projectiles;
        public bool IsRunOver => _phase == RunPhase.Victory || _phase == RunPhase.Defeat;
        public int CurrentWave => _waveManager == null ? 0 : _waveManager.CurrentWaveIndex + 1;
        public int TotalWaves => _waveManager == null ? 0 : _waveManager.TotalWaves;
        public int LivingMonsterCount => _waveManager == null ? 0 : _waveManager.LivingMonsterCount;

        public event Action<RunPhase> PhaseChanged;
        public event Action<int, int> WaveChanged;
        public event Action<float, float> CoreHpChanged;
        public event Action<int> GoldChanged;
        public event Action<int> SummonContractsChanged;
        public event Action<int> LivingMonsterCountChanged;
        public event Action<MonsterController, StageWave, int> MonsterSpawned;
        public event Action<MonsterController> MonsterResolved;

        void Awake()
        {
            if (summoner == null)
                summoner = transform.Find("Summoner");
            if (summoner == null)
                summoner = GameObject.Find("Summoner")?.transform;

            if (stageTimeline == null && useRuntimePrototypeWhenTimelineMissing)
            {
                _runtimeTimeline = StageTimeline.CreatePrototype(defaultMonsterSprite: runtimePrototypeMonsterSprite);
                stageTimeline = _runtimeTimeline;
                Debug.Log("[CrossDefense] StageTimeline이 비어 있어 런타임 프로토타입 타임라인을 사용합니다.", this);
            }

            maxCoreHp = Mathf.Max(1f, maxCoreHp);
            _coreHp = maxCoreHp;
            _gold = Mathf.Max(0, startingGold);
            _summonContracts = Mathf.Max(0, startingSummonContracts);
            _phase = RunPhase.Prepare;

            _summonManager = new SummonManager(
                this,
                BuildSummonPool(),
                currencyResultChance,
                directRankOneChance,
                currencyResultGold,
                summonBenchCapacity);

            _summonedUnitManager = GetComponent<SummonedUnitManager>();
            if (_summonedUnitManager == null)
                _summonedUnitManager = gameObject.AddComponent<SummonedUnitManager>();
            _summonedUnitManager.Initialize(
                this,
                _summonManager,
                summoner,
                Camera.main,
                autoDeploySummonedUnits);

            var input = GetComponent<CombatInputController>();
            if (input == null)
                input = gameObject.AddComponent<CombatInputController>();
            input.Initialize(this, _summonedUnitManager, summoner != null
                ? summoner.GetComponent<SummonerAttackController>()
                : null);

            _monsterSpawner = new MonsterSpawner(transform, Camera.main);
            _goldRewardFlow = new GoldRewardFlow(
                Camera.main,
                () => _goldScreenPositionProvider?.Invoke() ??
                    new Vector2(Screen.width * 0.78f, Screen.height * 0.95f),
                AddGold);
            _waveManager = new WaveManager();
            _waveManager.Initialize(this, stageTimeline, _monsterSpawner, summoner);
            CoreHpChanged?.Invoke(_coreHp, maxCoreHp);
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
                runtimePrototypeSummonSprite, MonsterAttribute.None, SummonAttackStyle.Melee,
                10f, 1.15f, 0.85f, 2.8f, new Color(0.82f, 0.9f, 1f));
            yield return punch;

            var watergun = SummonUnitData.CreatePrototype(
                "watergun-slime", "물총 슬라임", SummonUnitRarity.Common, 90,
                runtimePrototypeSummonSprite, MonsterAttribute.None, SummonAttackStyle.Projectile,
                7f, 1.35f, 4.5f, 2.35f, new Color(0.45f, 0.8f, 1f));
            watergun.ConfigurePrototypeEffects(runtimePrototypeNeutralProjectileSprite, 0f, 0f, 0f, 0f, 0f, 1);
            yield return watergun;

            var ember = SummonUnitData.CreatePrototype(
                "ember-slime", "불씨 슬라임", SummonUnitRarity.Common, 72,
                runtimePrototypeSummonSprite, MonsterAttribute.Fire, SummonAttackStyle.Area,
                8f, 0.85f, 2.1f, 2.2f, new Color(1f, 0.42f, 0.22f));
            ember.ConfigurePrototypeEffects(runtimePrototypeFireProjectileSprite, 1.15f, 0f, 0f, 0f, 0f, 1);
            yield return ember;

            var frost = SummonUnitData.CreatePrototype(
                "frost-slime", "서리 슬라임", SummonUnitRarity.Rare, 52,
                runtimePrototypeSummonSprite, MonsterAttribute.Ice, SummonAttackStyle.Projectile,
                5.5f, 1f, 4.2f, 2.2f, new Color(0.55f, 0.9f, 1f));
            frost.ConfigurePrototypeEffects(runtimePrototypeIceProjectileSprite, 0f, 0.32f, 2f, 0f, 0f, 1);
            yield return frost;

            var sprout = SummonUnitData.CreatePrototype(
                "sprout-slime", "새싹 슬라임", SummonUnitRarity.Rare, 45,
                runtimePrototypeSummonSprite, MonsterAttribute.Nature, SummonAttackStyle.Projectile,
                4.5f, 1f, 4f, 2.2f, new Color(0.5f, 1f, 0.48f));
            sprout.ConfigurePrototypeEffects(runtimePrototypeNeutralProjectileSprite, 0f, 0f, 0f, 3f, 2.5f, 1);
            yield return sprout;

            var resonance = SummonUnitData.CreatePrototype(
                "resonance-slime", "공명 슬라임", SummonUnitRarity.Rare, 38,
                runtimePrototypeSummonSprite, MonsterAttribute.None, SummonAttackStyle.Support,
                0.1f, 1f, 0.8f, 2f, new Color(0.78f, 0.55f, 1f));
            resonance.ConfigurePrototypeEffects(null, 0f, 0f, 0f, 0f, 0f, 1, 0.16f, 2.8f);
            yield return resonance;

            var burst = SummonUnitData.CreatePrototype(
                "burst-slime", "팽창 슬라임", SummonUnitRarity.Rare, 32,
                runtimePrototypeSummonSprite, MonsterAttribute.Fire, SummonAttackStyle.Area,
                15f, 0.55f, 1.1f, 2.7f, new Color(1f, 0.62f, 0.3f));
            burst.ConfigurePrototypeEffects(runtimePrototypeFireProjectileSprite, 1.45f, 0f, 0f, 0f, 0f, 1);
            yield return burst;

            var crystal = SummonUnitData.CreatePrototype(
                "crystal-slime", "빙정 슬라임", SummonUnitRarity.Legendary, 18,
                runtimePrototypeSummonSprite, MonsterAttribute.Ice, SummonAttackStyle.Piercing,
                13f, 0.65f, 6f, 1.9f, new Color(0.65f, 0.72f, 1f));
            crystal.ConfigurePrototypeEffects(runtimePrototypeIceProjectileSprite, 0f, 0.15f, 1f, 0f, 0f, 3);
            yield return crystal;
        }

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
            _coreHp = Mathf.Max(0f, _coreHp - amount);
            CoreHpChanged?.Invoke(_coreHp, maxCoreHp);
            if (_coreHp <= 0f)
            {
                SetPhase(RunPhase.Defeat);
                Debug.Log("[CrossDefense] 소환사 HP가 0이 되어 런이 종료되었습니다.", this);
            }
        }

        public void AddGold(int amount)
        {
            _gold = Mathf.Max(0, _gold + amount);
            GoldChanged?.Invoke(_gold);
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
            if (wave == null || wave.SummonContractReward <= 0) return;
            AddSummonContracts(wave.SummonContractReward);
            Debug.Log($"[CrossDefense] Wave clear reward: summon contracts +{wave.SummonContractReward}", this);
        }

        public void SetPhase(RunPhase phase)
        {
            if (_phase == phase) return;
            _phase = phase;
            PhaseChanged?.Invoke(phase);
            Debug.Log($"[CrossDefense] Phase: {phase}", this);
        }

        public void SetWave(int current, int total, StageWave wave)
        {
            WaveChanged?.Invoke(current, total);
            Debug.Log($"[CrossDefense] Wave {current}/{total}: {wave.Label} prep={wave.PreparationTime:0.##} monsters={wave.TotalMonsterCount}", this);
        }

        public void NotifyMonsterSpawned(MonsterController monster, StageWave wave, int livingCount)
        {
            MonsterSpawned?.Invoke(monster, wave, livingCount);
            LivingMonsterCountChanged?.Invoke(livingCount);
        }

        public void NotifyMonsterDefeated(MonsterController monster, int rewardGold)
        {
            if (_waveManager == null) return;
            Vector3 rewardOrigin = monster != null ? monster.transform.position : Vector3.zero;
            _waveManager.NotifyMonsterDefeated(monster);
            _monsterSpawner.Release(monster);
            MonsterResolved?.Invoke(monster);
            if (_goldRewardFlow != null)
                _goldRewardFlow.Present(rewardOrigin, rewardGold);
            else
                AddGold(rewardGold);
            LivingMonsterCountChanged?.Invoke(_waveManager.LivingMonsterCount);
        }

        public void NotifyMonsterReachedCore(MonsterController monster, int contactDamage)
        {
            if (_waveManager == null) return;
            _waveManager.NotifyMonsterReachedCore(monster);
            _monsterSpawner.Release(monster);
            ApplyCoreDamage(contactDamage);
            MonsterResolved?.Invoke(monster);
            LivingMonsterCountChanged?.Invoke(_waveManager.LivingMonsterCount);
        }
    }
}
