using System.Collections;
using System.Collections.Generic;
using CrossDefense.Data;
using CrossDefense.Units;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>StageTimeline을 읽어 준비·스폰·정리·다음 웨이브 전환을 수행한다.</summary>
    public sealed class WaveManager
    {
        readonly HashSet<MonsterController> _livingMonsters = new();
        GameManager _gameManager;
        StageTimeline _timeline;
        MonsterSpawner _spawner;
        Transform _summoner;
        System.Random _random;
        Coroutine _routine;
        int _waveIndex;

        public int CurrentWaveIndex => _waveIndex;
        public int TotalWaves => _timeline == null ? 0 : _timeline.WaveCount;
        public int LivingMonsterCount => _livingMonsters.Count;

        public void Initialize(GameManager gameManager, StageTimeline timeline, MonsterSpawner spawner, Transform summoner)
        {
            _gameManager = gameManager;
            _timeline = timeline;
            _spawner = spawner;
            _summoner = summoner;
            _random = new System.Random(timeline.RandomSeed);
        }

        public void RunFrom(MonoBehaviour coroutineHost)
        {
            if (_routine != null) coroutineHost.StopCoroutine(_routine);
            _routine = coroutineHost.StartCoroutine(Run(coroutineHost));
        }

        public void Stop(MonoBehaviour coroutineHost)
        {
            if (_routine == null) return;
            coroutineHost.StopCoroutine(_routine);
            _routine = null;
        }

        public void NotifyMonsterDefeated(MonsterController monster)
        {
            _livingMonsters.Remove(monster);
        }

        public void NotifyMonsterReachedCore(MonsterController monster)
        {
            _livingMonsters.Remove(monster);
        }

        IEnumerator Run(MonoBehaviour coroutineHost)
        {
            if (_timeline == null || _timeline.WaveCount == 0)
            {
                _gameManager.SetPhase(RunPhase.Defeat);
                yield break;
            }

            for (_waveIndex = 0; _waveIndex < _timeline.WaveCount; _waveIndex++)
            {
                if (_gameManager.IsRunOver) yield break;
                if (!_timeline.TryGetWave(_waveIndex, out var wave)) continue;

                _gameManager.SetWave(_waveIndex + 1, _timeline.WaveCount, wave);
                _gameManager.SetPhase(RunPhase.Prepare);
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, wave.PreparationTime));
                if (_gameManager.IsRunOver) yield break;

                _gameManager.SetPhase(RunPhase.InWave);
                Debug.Log($"[CrossDefense] Spawn start: {wave.TotalMonsterCount} monsters", _gameManager);
                yield return SpawnWave(coroutineHost, wave);

                while (_livingMonsters.Count > 0 && !_gameManager.IsRunOver)
                    yield return null;

                if (_gameManager.IsRunOver) yield break;

                bool hasNextWave = _waveIndex + 1 < _timeline.WaveCount;
                if (hasNextWave)
                    _gameManager.GrantWaveClearReward(wave);

                _gameManager.SetPhase(RunPhase.Intermission);
                yield return new WaitForSecondsRealtime(0.5f);
            }

            if (!_gameManager.IsRunOver)
                _gameManager.SetPhase(RunPhase.Victory);
        }

        IEnumerator SpawnWave(MonoBehaviour coroutineHost, StageWave wave)
        {
            foreach (var entry in wave.MonsterSpawns)
            {
                if (entry == null || entry.Monster == null) continue;
                for (int i = 0; i < entry.Count; i++)
                {
                    var zone = _timeline.ChooseSpawnZone(wave, _random);
                    var monster = _spawner.Spawn(
                        _gameManager,
                        _summoner,
                        entry.Monster,
                        zone,
                        _timeline.GetMonsterHpMultiplier(wave, entry),
                        _timeline.GetMonsterSpeedMultiplier(wave, entry),
                        entry.RewardMultiplier,
                        _random);
                    _livingMonsters.Add(monster);
                    _gameManager.NotifyMonsterSpawned(monster, wave, _livingMonsters.Count);
                    yield return new WaitForSecondsRealtime(_timeline.GetSpawnInterval(wave, entry));
                    if (_gameManager.IsRunOver) yield break;
                }
            }
        }
    }
}
