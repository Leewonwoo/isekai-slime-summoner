using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Data
{
    [CreateAssetMenu(fileName = "MonsterCatalog", menuName = "Cross Defense/Data/Monster Catalog", order = 42)]
    public sealed class MonsterCatalog : ScriptableObject
    {
        [SerializeField] List<MonsterData> monsters = new();
        public IReadOnlyList<MonsterData> Monsters => monsters;

        public MonsterData Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < monsters.Count; i++)
                if (monsters[i] != null && monsters[i].MonsterId == id) return monsters[i];
            return null;
        }

        public static MonsterCatalog CreateRuntime(IEnumerable<MonsterData> source)
        {
            var catalog = CreateInstance<MonsterCatalog>();
            catalog.hideFlags = HideFlags.HideAndDontSave;
            var seen = new HashSet<string>();
            if (source != null)
                foreach (MonsterData monster in source)
                    if (monster != null && seen.Add(monster.MonsterId)) catalog.monsters.Add(monster);
            return catalog;
        }
    }
}
