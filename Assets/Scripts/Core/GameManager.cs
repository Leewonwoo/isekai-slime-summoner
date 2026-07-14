using System;
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

        [Header("Summoner")]
        [SerializeField] Transform summoner;
        [Min(1f)] [SerializeField] float maxCoreHp = 100f;
        [Min(0)] [SerializeField] int startingGold = 0;
        [Min(0)] [SerializeField] int startingSummonContracts = 10;

        WaveManager _waveManager;
        MonsterSpawner _monsterSpawner;
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
                _runtimeTimeline = StageTimeline.CreatePrototype();
                stageTimeline = _runtimeTimeline;
                Debug.Log("[CrossDefense] StageTimeline이 비어 있어 런타임 프로토타입 타임라인을 사용합니다.", this);
            }

            maxCoreHp = Mathf.Max(1f, maxCoreHp);
            _coreHp = maxCoreHp;
            _gold = Mathf.Max(0, startingGold);
            _summonContracts = Mathf.Max(0, startingSummonContracts);
            _phase = RunPhase.Prepare;

            _monsterSpawner = new MonsterSpawner(transform, Camera.main);
            _waveManager = new WaveManager();
            _waveManager.Initialize(this, stageTimeline, _monsterSpawner, summoner);
            CoreHpChanged?.Invoke(_coreHp, maxCoreHp);
            GoldChanged?.Invoke(_gold);
            SummonContractsChanged?.Invoke(_summonContracts);
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
            _waveManager.NotifyMonsterDefeated(monster);
            _monsterSpawner.Release(monster);
            AddGold(rewardGold);
            MonsterResolved?.Invoke(monster);
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
