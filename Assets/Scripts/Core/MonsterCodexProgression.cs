using System;
using System.Collections.Generic;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    [Serializable]
    public sealed class MonsterCodexEntrySaveData
    {
        public string monsterId;
        public bool encountered;
        public int kills;
    }

    [Serializable]
    public sealed class MonsterCodexSaveData
    {
        public List<MonsterCodexEntrySaveData> entries = new();
    }

    public readonly struct MonsterCodexEntry
    {
        public string MonsterId { get; }
        public bool Encountered { get; }
        public int Kills { get; }
        public MonsterCodexEntry(string id, bool encountered, int kills)
        {
            MonsterId = id;
            Encountered = encountered;
            Kills = Mathf.Max(0, kills);
        }
    }

    public sealed class MonsterCodexProgression
    {
        public const string DefaultPlayerPrefsKey = "CrossDefense.MonsterCodex.v1";
        readonly MonsterCatalog _catalog;
        readonly Dictionary<string, MonsterCodexEntrySaveData> _entries = new();
        readonly Action<string> _save;
        readonly Action _flush;
        public event Action<string> Changed;

        public MonsterCodexProgression(MonsterCatalog catalog, Func<string> load = null,
            Action<string> save = null, Action flush = null)
        {
            _catalog = catalog;
            _save = save;
            _flush = flush;
            Restore(load?.Invoke());
        }

        public static MonsterCodexProgression CreatePersistent(MonsterCatalog catalog, string key = DefaultPlayerPrefsKey)
        {
            string safeKey = string.IsNullOrWhiteSpace(key) ? DefaultPlayerPrefsKey : key;
            return new MonsterCodexProgression(catalog,
                () => PlayerPrefs.GetString(safeKey, string.Empty),
                json => PlayerPrefs.SetString(safeKey, json), PlayerPrefs.Save);
        }

        public MonsterCodexEntry Get(string monsterId) =>
            _entries.TryGetValue(monsterId ?? string.Empty, out var entry)
                ? new MonsterCodexEntry(entry.monsterId, entry.encountered, entry.kills)
                : new MonsterCodexEntry(monsterId, false, 0);

        public bool RecordEncounter(MonsterData monster)
        {
            if (!IsKnown(monster)) return false;
            var entry = GetMutable(monster.MonsterId);
            if (entry.encountered) return false;
            entry.encountered = true;
            Persist();
            Changed?.Invoke(monster.MonsterId);
            return true;
        }

        public bool RecordKill(MonsterData monster)
        {
            if (!IsKnown(monster)) return false;
            var entry = GetMutable(monster.MonsterId);
            entry.encountered = true;
            entry.kills = Mathf.Max(0, entry.kills) + 1;
            Persist();
            Changed?.Invoke(monster.MonsterId);
            return true;
        }

        public string ToJson()
        {
            var data = new MonsterCodexSaveData();
            foreach (var pair in _entries) data.entries.Add(pair.Value);
            return JsonUtility.ToJson(data);
        }

        public void Flush() => _flush?.Invoke();

        void Restore(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                var data = JsonUtility.FromJson<MonsterCodexSaveData>(json);
                if (data?.entries == null) return;
                foreach (var entry in data.entries)
                {
                    if (entry == null || _catalog?.Find(entry.monsterId) == null) continue;
                    entry.kills = Mathf.Max(0, entry.kills);
                    _entries[entry.monsterId] = entry;
                }
            }
            catch (Exception) { _entries.Clear(); }
        }

        bool IsKnown(MonsterData monster) => monster != null && _catalog?.Find(monster.MonsterId) != null;

        MonsterCodexEntrySaveData GetMutable(string id)
        {
            if (_entries.TryGetValue(id, out var entry)) return entry;
            entry = new MonsterCodexEntrySaveData { monsterId = id };
            _entries[id] = entry;
            return entry;
        }

        void Persist() => _save?.Invoke(ToJson());
    }
}
