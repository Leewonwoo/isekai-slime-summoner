using System;
using System.Collections.Generic;
using CrossDefense.Data;

namespace CrossDefense.Core
{
    public sealed class RunRelicInventory
    {
        readonly Dictionary<string, RunRelicDefinition> _owned = new();
        public IReadOnlyCollection<RunRelicDefinition> Owned => _owned.Values;
        public event Action Changed;

        public bool Contains(string id) => !string.IsNullOrWhiteSpace(id) && _owned.ContainsKey(id);

        public List<string> CaptureOwnedIds() => new(_owned.Keys);

        public bool TryAdd(RunRelicDefinition relic)
        {
            if (relic == null || string.IsNullOrWhiteSpace(relic.Id) || _owned.ContainsKey(relic.Id)) return false;
            _owned.Add(relic.Id, relic);
            Changed?.Invoke();
            return true;
        }

        public void Clear()
        {
            if (_owned.Count == 0) return;
            _owned.Clear();
            Changed?.Invoke();
        }

        public void Restore(
            IEnumerable<string> relicIds,
            Func<string, RunRelicDefinition> resolver)
        {
            _owned.Clear();
            if (relicIds != null && resolver != null)
            {
                foreach (string id in relicIds)
                {
                    if (string.IsNullOrWhiteSpace(id) || _owned.ContainsKey(id))
                        continue;
                    RunRelicDefinition relic = resolver(id);
                    if (relic != null)
                        _owned.Add(id, relic);
                }
            }
            Changed?.Invoke();
        }

        public float Value(RunRelicEffect effect)
        {
            float total = 0f;
            foreach (RunRelicDefinition relic in _owned.Values)
                if (relic.Effect == effect) total += relic.Value;
            return total;
        }

        public float DamageMultiplier => 1f + Value(RunRelicEffect.AllDamage);
        public float AttackSpeedMultiplier => 1f + Value(RunRelicEffect.AllAttackSpeed);
        public float GoldMultiplier => 1f + Value(RunRelicEffect.GoldReward);
        public float MaxHpMultiplier => 1f + Value(RunRelicEffect.SummonerMaxHp);
        public int WaveContractBonus => UnityEngine.Mathf.RoundToInt(Value(RunRelicEffect.WaveContract));
        public float JackpotChanceBonus => Value(RunRelicEffect.JackpotChance);
    }
}
