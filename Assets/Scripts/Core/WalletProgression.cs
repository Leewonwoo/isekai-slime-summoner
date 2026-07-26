using System;
using UnityEngine;

namespace CrossDefense.Core
{
    [Serializable]
    public sealed class WalletSaveData
    {
        public int version = 1;
        public int gold;
    }

    /// <summary>런과 무관하게 유지되는 플레이어 보유 금화를 저장한다.</summary>
    public sealed class WalletProgression
    {
        public const string DefaultPlayerPrefsKey = "CrossDefense.Wallet.v1";

        readonly Action<string> _saveJson;
        readonly Action _flush;

        public int Gold { get; private set; }

        public WalletProgression(
            int initialGold,
            Func<string> loadJson = null,
            Action<string> saveJson = null,
            Action flush = null)
        {
            _saveJson = saveJson;
            _flush = flush;
            Gold = Mathf.Max(0, initialGold);
            Restore(loadJson?.Invoke());
        }

        public static WalletProgression CreatePersistent(
            int initialGold,
            string playerPrefsKey = DefaultPlayerPrefsKey)
        {
            string key = string.IsNullOrWhiteSpace(playerPrefsKey)
                ? DefaultPlayerPrefsKey
                : playerPrefsKey;
            var wallet = new WalletProgression(
                initialGold,
                () => PlayerPrefs.GetString(key, string.Empty),
                json => PlayerPrefs.SetString(key, json),
                PlayerPrefs.Save);
            wallet.Flush();
            return wallet;
        }

        public void SetGold(int amount)
        {
            Gold = Mathf.Max(0, amount);
            Persist();
        }

        public void Flush()
        {
            Persist();
            _flush?.Invoke();
        }

        public string ToJson() => JsonUtility.ToJson(new WalletSaveData
        {
            gold = Gold,
        });

        void Persist() => _saveJson?.Invoke(ToJson());

        void Restore(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;
            try
            {
                WalletSaveData data = JsonUtility.FromJson<WalletSaveData>(json);
                if (data != null && data.version == 1)
                    Gold = Mathf.Max(0, data.gold);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[CrossDefense] Failed to load wallet: {exception.Message}");
            }
        }
    }
}
