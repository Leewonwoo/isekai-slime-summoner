using System;
using System.Collections.Generic;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    [Serializable]
    public sealed class SummonerBuffLoadoutSaveData
    {
        public int version = 1;
        public int[] equippedBuffs = Array.Empty<int>();
    }

    public sealed class SummonerBuffLoadout
    {
        public const string DefaultPlayerPrefsKey = "CrossDefense.SummonerBuffLoadout.v1";

        readonly Func<int> _levelProvider;
        readonly Action<string> _saveJson;
        readonly Action _flush;
        readonly List<SummonerBuffId> _equipped = new(SummonerBuffCatalog.MaxEquipped);

        public IReadOnlyList<SummonerBuffId> Equipped => _equipped;
        public event Action Changed;

        public SummonerBuffLoadout(
            Func<int> levelProvider,
            Func<string> loadJson = null,
            Action<string> saveJson = null,
            Action flush = null)
        {
            _levelProvider = levelProvider;
            _saveJson = saveJson;
            _flush = flush;
            Load(loadJson?.Invoke());
            if (_equipped.Count == 0)
                FillUnlockedSlots();
        }

        public static SummonerBuffLoadout CreatePersistent(
            Func<int> levelProvider,
            string playerPrefsKey = DefaultPlayerPrefsKey)
        {
            string key = string.IsNullOrWhiteSpace(playerPrefsKey)
                ? DefaultPlayerPrefsKey
                : playerPrefsKey;
            return new SummonerBuffLoadout(
                levelProvider,
                () => PlayerPrefs.GetString(key, string.Empty),
                json => PlayerPrefs.SetString(key, json),
                PlayerPrefs.Save);
        }

        public bool IsEquipped(SummonerBuffId id) => _equipped.Contains(id);

        public bool TryToggle(SummonerBuffId id)
        {
            if (!Enum.IsDefined(typeof(SummonerBuffId), id) ||
                !SummonerBuffCatalog.IsUnlocked(id, CurrentLevel))
                return false;

            int index = _equipped.IndexOf(id);
            if (index >= 0)
                _equipped.RemoveAt(index);
            else
            {
                if (_equipped.Count >= SummonerBuffCatalog.MaxEquipped)
                    return false;
                _equipped.Add(id);
            }

            Persist(true);
            Changed?.Invoke();
            return true;
        }

        public bool EnsureUnlockedSlotsFilled()
        {
            if (!FillUnlockedSlots())
                return false;
            Persist(true);
            Changed?.Invoke();
            return true;
        }

        public void Flush() => Persist(true);

        public string ToJson()
        {
            var values = new int[_equipped.Count];
            for (int i = 0; i < _equipped.Count; i++)
                values[i] = (int)_equipped[i];
            return JsonUtility.ToJson(new SummonerBuffLoadoutSaveData
            {
                equippedBuffs = values,
            });
        }

        int CurrentLevel => Mathf.Max(1, _levelProvider?.Invoke() ?? 1);

        void Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;
            try
            {
                SummonerBuffLoadoutSaveData data =
                    JsonUtility.FromJson<SummonerBuffLoadoutSaveData>(json);
                if (data == null || data.version != 1 || data.equippedBuffs == null)
                    return;
                for (int i = 0;
                     i < data.equippedBuffs.Length && _equipped.Count < SummonerBuffCatalog.MaxEquipped;
                     i++)
                {
                    int raw = data.equippedBuffs[i];
                    if (!Enum.IsDefined(typeof(SummonerBuffId), raw))
                        continue;
                    SummonerBuffId id = (SummonerBuffId)raw;
                    if (SummonerBuffCatalog.IsUnlocked(id, CurrentLevel) && !_equipped.Contains(id))
                        _equipped.Add(id);
                }
            }
            catch (ArgumentException)
            {
                _equipped.Clear();
            }
        }

        bool FillUnlockedSlots()
        {
            bool changed = false;
            foreach (SummonerBuffDefinition definition in SummonerBuffCatalog.All)
            {
                if (_equipped.Count >= SummonerBuffCatalog.MaxEquipped)
                    break;
                if (!SummonerBuffCatalog.IsUnlocked(definition.Id, CurrentLevel))
                    continue;
                if (_equipped.Contains(definition.Id))
                    continue;
                _equipped.Add(definition.Id);
                changed = true;
            }
            return changed;
        }

        void Persist(bool flush)
        {
            _saveJson?.Invoke(ToJson());
            if (flush)
                _flush?.Invoke();
        }
    }
}
