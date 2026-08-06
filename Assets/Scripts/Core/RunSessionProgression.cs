using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Core
{
    [Serializable]
    public sealed class RunSessionSummonSaveData
    {
        public string unitId;
        public int rank;
        public bool isDeployed;
        public float hpRatio = 1f;
    }

    [Serializable]
    public sealed class RunSessionSaveData
    {
        public int version = 1;
        public int healthCheckpointVersion;
        public int runEventSeed;
        public string stageId;
        public int waveIndex;
        public int gold;
        public int summonContracts;
        public float coreHp;
        public float coreHpRatio = 1f;
        public List<string> runRelicIds = new();
        public RunTraitProgressionSaveData runTraits = new();
        public List<RunSessionSummonSaveData> summonedUnits = new();
    }

    /// <summary>
    /// 마지막으로 플레이한 스테이지를 기억하는 저장소다.
    /// 전투 중간 상태는 복구하지 않으며 재실행하면 저장된 DAY의 시작부터 진행한다.
    /// </summary>
    public sealed class RunSessionProgression
    {
        public const string DefaultPlayerPrefsKey = "CrossDefense.RunSession.v1";

        readonly Func<string> _loadJson;
        readonly Action<string> _saveJson;
        readonly Action _delete;
        readonly Action _flush;

        public RunSessionProgression(
            Func<string> loadJson = null,
            Action<string> saveJson = null,
            Action delete = null,
            Action flush = null)
        {
            _loadJson = loadJson;
            _saveJson = saveJson;
            _delete = delete;
            _flush = flush;
        }

        public static RunSessionProgression CreatePersistent(
            string playerPrefsKey = DefaultPlayerPrefsKey)
        {
            string key = string.IsNullOrWhiteSpace(playerPrefsKey)
                ? DefaultPlayerPrefsKey
                : playerPrefsKey;
            return new RunSessionProgression(
                () => PlayerPrefs.GetString(key, string.Empty),
                json => PlayerPrefs.SetString(key, json),
                () => PlayerPrefs.DeleteKey(key),
                PlayerPrefs.Save);
        }

        public bool TryLoad(string expectedStageId, out RunSessionSaveData data)
        {
            data = null;
            string json = _loadJson?.Invoke();
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                RunSessionSaveData loaded = JsonUtility.FromJson<RunSessionSaveData>(json);
                if (loaded == null || loaded.version != 1 ||
                    !string.Equals(
                        loaded.stageId ?? string.Empty,
                        expectedStageId ?? string.Empty,
                        StringComparison.Ordinal))
                    return false;

                loaded.waveIndex = Mathf.Max(0, loaded.waveIndex);
                loaded.gold = Mathf.Max(0, loaded.gold);
                loaded.summonContracts = Mathf.Max(0, loaded.summonContracts);
                loaded.coreHp = Mathf.Max(0f, loaded.coreHp);
                if (loaded.healthCheckpointVersion > 0)
                    loaded.coreHpRatio = Mathf.Clamp01(loaded.coreHpRatio);
                loaded.runRelicIds ??= new List<string>();
                loaded.runTraits ??= new RunTraitProgressionSaveData();
                loaded.summonedUnits ??= new List<RunSessionSummonSaveData>();
                if (loaded.healthCheckpointVersion > 0)
                {
                    foreach (RunSessionSummonSaveData summon in loaded.summonedUnits)
                    {
                        if (summon != null)
                            summon.hpRatio = Mathf.Clamp01(summon.hpRatio);
                    }
                }
                data = loaded;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[CrossDefense] Failed to load run session: {exception.Message}");
                return false;
            }
        }

        public void Save(RunSessionSaveData data, bool flush)
        {
            if (data == null)
                return;
            _saveJson?.Invoke(JsonUtility.ToJson(data));
            if (flush)
                _flush?.Invoke();
        }

        public void Clear(bool flush)
        {
            _delete?.Invoke();
            if (flush)
                _flush?.Invoke();
        }

        public void Flush() => _flush?.Invoke();
    }
}
