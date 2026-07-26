using System;
using System.Collections.Generic;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    [Serializable]
    public sealed class EquipmentProgressionSaveData
    {
        public List<string> ownedIds = new();
        public string weaponId;
        public string armorId;
        public string accessoryId;
    }

    public sealed class EquipmentProgression
    {
        public const string DefaultPlayerPrefsKey = "CrossDefense.Equipment.v1";
        readonly EquipmentCatalog _catalog;
        readonly HashSet<string> _owned = new();
        readonly Dictionary<EquipmentSlot, string> _equipped = new();
        readonly Action<string> _save;
        readonly Action _flush;
        public EquipmentCatalog Catalog => _catalog;
        public IReadOnlyCollection<string> OwnedIds => _owned;
        public event Action Changed;

        public EquipmentProgression(EquipmentCatalog catalog, Func<string> load = null,
            Action<string> save = null, Action flush = null)
        {
            _catalog = catalog;
            _save = save;
            _flush = flush;
            Restore(load?.Invoke());
        }

        public static EquipmentProgression CreatePersistent(EquipmentCatalog catalog, string key = DefaultPlayerPrefsKey)
        {
            string safeKey = string.IsNullOrWhiteSpace(key) ? DefaultPlayerPrefsKey : key;
            var progression = new EquipmentProgression(catalog,
                () => PlayerPrefs.GetString(safeKey, string.Empty),
                json => PlayerPrefs.SetString(safeKey, json), PlayerPrefs.Save);
            progression.Persist(true);
            return progression;
        }

        public bool IsOwned(string id) => !string.IsNullOrWhiteSpace(id) && _owned.Contains(id);
        public EquipmentData Equipped(EquipmentSlot slot) =>
            _equipped.TryGetValue(slot, out string id) ? _catalog?.Find(id) : null;

        public bool Acquire(EquipmentData equipment)
        {
            if (equipment == null || _catalog?.Find(equipment.EquipmentId) == null || !_owned.Add(equipment.EquipmentId))
                return false;
            if (Equipped(equipment.Slot) == null) _equipped[equipment.Slot] = equipment.EquipmentId;
            Persist(true);
            Changed?.Invoke();
            return true;
        }

        public bool TryEquip(string id)
        {
            EquipmentData equipment = _catalog?.Find(id);
            if (equipment == null || !IsOwned(id)) return false;
            _equipped[equipment.Slot] = id;
            Persist(true);
            Changed?.Invoke();
            return true;
        }

        public float DamageMultiplier => 1f + EffectValue(EquipmentEffect.SummonerDamage);
        public float MaxHpMultiplier => 1f + EffectValue(EquipmentEffect.SummonerMaxHp);
        public float AttackSpeedMultiplier => 1f + EffectValue(EquipmentEffect.SummonerAttackSpeed);
        public float CriticalChanceBonus => EffectValue(EquipmentEffect.CriticalChance);
        public float JackpotChanceBonus => EffectValue(EquipmentEffect.JackpotChance);
        public void Flush() => _flush?.Invoke();

        public string ToJson() => JsonUtility.ToJson(new EquipmentProgressionSaveData
        {
            ownedIds = new List<string>(_owned),
            weaponId = Equipped(EquipmentSlot.Weapon)?.EquipmentId,
            armorId = Equipped(EquipmentSlot.Armor)?.EquipmentId,
            accessoryId = Equipped(EquipmentSlot.Accessory)?.EquipmentId,
        });

        void Restore(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                var data = JsonUtility.FromJson<EquipmentProgressionSaveData>(json);
                if (data == null) return;
                if (data.ownedIds != null)
                    foreach (string id in data.ownedIds)
                        if (_catalog?.Find(id) != null) _owned.Add(id);
                RestoreSlot(EquipmentSlot.Weapon, data.weaponId);
                RestoreSlot(EquipmentSlot.Armor, data.armorId);
                RestoreSlot(EquipmentSlot.Accessory, data.accessoryId);
            }
            catch (Exception)
            {
                _owned.Clear();
                _equipped.Clear();
            }
        }

        void RestoreSlot(EquipmentSlot slot, string id)
        {
            EquipmentData item = _catalog?.Find(id);
            if (item != null && item.Slot == slot && _owned.Contains(id)) _equipped[slot] = id;
        }

        float EffectValue(EquipmentEffect effect)
        {
            float total = 0f;
            foreach (var pair in _equipped)
            {
                EquipmentData item = _catalog?.Find(pair.Value);
                if (item != null && item.Effect == effect) total += item.Value;
            }
            return total;
        }

        void Persist(bool flush)
        {
            _save?.Invoke(ToJson());
            if (flush) _flush?.Invoke();
        }
    }
}
