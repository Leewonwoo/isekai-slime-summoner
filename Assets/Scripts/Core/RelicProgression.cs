using System;
using System.Collections.Generic;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    [Serializable]
    public sealed class RelicOwnedSaveData
    {
        public int family;
        public int rank;
    }

    [Serializable]
    public sealed class RelicProgressionSaveData
    {
        public int version = 1;
        public int equippedFamily;
        public List<RelicOwnedSaveData> owned = new();
    }

    public sealed class RelicProgression
    {
        public const string DefaultPlayerPrefsKey = "CrossDefense.Relics.v1";

        readonly RelicCatalog _catalog;
        readonly Dictionary<RelicFamily, int> _ranks = new();
        readonly Action<string> _saveJson;
        readonly Action _flush;

        public RelicCatalog Catalog => _catalog;
        public RelicFamily EquippedFamily { get; private set; } = RelicFamily.None;
        public RelicDefinition EquippedDefinition => _catalog.Find(EquippedFamily);
        public int EquippedRank => Rank(EquippedFamily);
        public event Action Changed;

        public RelicProgression(
            RelicCatalog catalog,
            Func<string> loadJson = null,
            Action<string> saveJson = null,
            Action flush = null)
        {
            _catalog = catalog;
            _saveJson = saveJson;
            _flush = flush;
            Load(loadJson?.Invoke());
            EnsureValidState();
        }

        public static RelicProgression CreatePersistent(
            RelicCatalog catalog,
            string playerPrefsKey = DefaultPlayerPrefsKey)
        {
            string key = string.IsNullOrWhiteSpace(playerPrefsKey)
                ? DefaultPlayerPrefsKey
                : playerPrefsKey;
            return new RelicProgression(
                catalog,
                () => PlayerPrefs.GetString(key, string.Empty),
                json => PlayerPrefs.SetString(key, json),
                PlayerPrefs.Save);
        }

        public int Rank(RelicFamily family) =>
            _ranks.TryGetValue(family, out int rank) ? rank : 0;

        public bool IsOwned(RelicFamily family) => Rank(family) > 0;

        public bool CanAcquire(RelicFamily family)
        {
            RelicDefinition definition = _catalog?.Find(family);
            return definition != null && definition.MerchantAvailable &&
                   Rank(family) < definition.MaxRank;
        }

        public bool TryAcquire(RelicFamily family)
        {
            if (!CanAcquire(family))
                return false;
            _ranks[family] = Rank(family) + 1;
            Persist(true);
            Changed?.Invoke();
            return true;
        }

        public bool TryEquip(RelicFamily family)
        {
            if (!IsOwned(family) || _catalog?.Find(family) == null)
                return false;
            if (EquippedFamily == family)
                return true;
            EquippedFamily = family;
            Persist(true);
            Changed?.Invoke();
            return true;
        }

        public void Flush() => Persist(true);

        public string ToJson()
        {
            var data = new RelicProgressionSaveData
            {
                equippedFamily = (int)EquippedFamily,
            };
            foreach (var pair in _ranks)
                if (pair.Value > 0)
                    data.owned.Add(new RelicOwnedSaveData
                    {
                        family = (int)pair.Key,
                        rank = pair.Value,
                    });
            return JsonUtility.ToJson(data);
        }

        void Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;
            try
            {
                RelicProgressionSaveData data =
                    JsonUtility.FromJson<RelicProgressionSaveData>(json);
                if (data == null || data.version != 1)
                    return;
                if (data.owned != null)
                    foreach (RelicOwnedSaveData item in data.owned)
                    {
                        if (item == null || !Enum.IsDefined(typeof(RelicFamily), item.family))
                            continue;
                        RelicFamily family = (RelicFamily)item.family;
                        RelicDefinition definition = _catalog?.Find(family);
                        if (definition != null)
                            _ranks[family] = Mathf.Clamp(item.rank, 0, definition.MaxRank);
                    }
                if (Enum.IsDefined(typeof(RelicFamily), data.equippedFamily))
                    EquippedFamily = (RelicFamily)data.equippedFamily;
            }
            catch (ArgumentException)
            {
                _ranks.Clear();
            }
        }

        void EnsureValidState()
        {
            if (EquippedFamily != RelicFamily.None &&
                (!IsOwned(EquippedFamily) || _catalog?.Find(EquippedFamily) == null))
                EquippedFamily = RelicFamily.None;
        }

        void Persist(bool flush)
        {
            _saveJson?.Invoke(ToJson());
            if (flush)
                _flush?.Invoke();
        }
    }
}
