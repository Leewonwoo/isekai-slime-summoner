using System;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    [Serializable]
    public sealed class SummonerSkillLoadoutSaveData
    {
        public int version = 1;
        public int equippedSkill;
    }

    public sealed class SummonerSkillLoadout
    {
        public const string DefaultPlayerPrefsKey = "CrossDefense.SummonerSkillLoadout.v1";

        readonly Func<int> _levelProvider;
        readonly Action<string> _saveJson;
        readonly Action _flush;

        public SummonerSkillId EquippedSkill { get; private set; } = SummonerSkillId.Meteor;
        public event Action<SummonerSkillId> Changed;

        public SummonerSkillLoadout(
            Func<int> levelProvider,
            Func<string> loadJson = null,
            Action<string> saveJson = null,
            Action flush = null)
        {
            _levelProvider = levelProvider;
            _saveJson = saveJson;
            _flush = flush;
            Load(loadJson?.Invoke());
        }

        public static SummonerSkillLoadout CreatePersistent(
            Func<int> levelProvider,
            string playerPrefsKey = DefaultPlayerPrefsKey)
        {
            string key = string.IsNullOrWhiteSpace(playerPrefsKey)
                ? DefaultPlayerPrefsKey
                : playerPrefsKey;
            return new SummonerSkillLoadout(
                levelProvider,
                () => PlayerPrefs.GetString(key, string.Empty),
                json => PlayerPrefs.SetString(key, json),
                PlayerPrefs.Save);
        }

        public bool TryEquip(SummonerSkillId id)
        {
            if (!Enum.IsDefined(typeof(SummonerSkillId), id) ||
                !SummonerSkillCatalog.IsRelicSkill(id) ||
                !SummonerSkillCatalog.IsUnlocked(id, _levelProvider?.Invoke() ?? 1))
                return false;
            if (EquippedSkill == id)
                return true;
            EquippedSkill = id;
            Persist(true);
            Changed?.Invoke(id);
            return true;
        }

        public void Flush() => Persist(true);

        public string ToJson() => JsonUtility.ToJson(new SummonerSkillLoadoutSaveData
        {
            equippedSkill = (int)EquippedSkill,
        });

        void Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;
            try
            {
                SummonerSkillLoadoutSaveData data =
                    JsonUtility.FromJson<SummonerSkillLoadoutSaveData>(json);
                if (data == null || data.version != 1 ||
                    !Enum.IsDefined(typeof(SummonerSkillId), data.equippedSkill))
                    return;
                SummonerSkillId loaded = (SummonerSkillId)data.equippedSkill;
                if (SummonerSkillCatalog.IsRelicSkill(loaded) &&
                    SummonerSkillCatalog.IsUnlocked(loaded, _levelProvider?.Invoke() ?? 1))
                    EquippedSkill = loaded;
            }
            catch (ArgumentException)
            {
                EquippedSkill = SummonerSkillId.Meteor;
            }
        }

        void Persist(bool flush)
        {
            _saveJson?.Invoke(ToJson());
            if (flush)
                _flush?.Invoke();
        }
    }
}
