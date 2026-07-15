using System;
using System.Collections.Generic;
using DarkTonic.PoolBoss;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>PoolBoss 런타임 등록을 한 곳에서 관리한다. 전용 아트 프리팹이 생겨도 호출부는 유지된다.</summary>
    public static class RuntimePoolService
    {
        const string Category = "CrossDefense Runtime";

        static readonly Dictionary<string, Transform> Templates = new(StringComparer.Ordinal);
        static Transform _templateRoot;

        public static Transform GetOrCreateTemplate(string name, Action<GameObject> configure, int preload = 8, int hardLimit = 128)
        {
            if (Templates.TryGetValue(name, out var cached) && cached != null)
                return cached;

            EnsurePoolBoss();
            EnsureTemplateRoot();

            var templateObject = new GameObject(name);
            templateObject.transform.SetParent(_templateRoot, false);
            configure?.Invoke(templateObject);
            templateObject.SetActive(false);
            Templates[name] = templateObject.transform;

            if (!PoolBoss.PrefabIsInPool(templateObject.transform))
            {
                PoolBoss.CreateNewPoolItem(
                    templateObject.transform,
                    Mathf.Max(1, preload),
                    true,
                    Mathf.Max(preload, hardLimit),
                    false,
                    Category,
                    PoolBoss.PrefabSource.Prefab,
                    false);
            }

            return templateObject.transform;
        }

        public static Transform Spawn(Transform template, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (template == null) return null;
            EnsurePoolBoss();

            if (!PoolBoss.PrefabIsInPool(template))
            {
                PoolBoss.CreateNewPoolItem(
                    template,
                    1,
                    true,
                    128,
                    false,
                    Category,
                    PoolBoss.PrefabSource.Prefab,
                    false);
            }

            if (PoolBoss.IsReady)
                return PoolBoss.Spawn(template, position, rotation, parent);

            var fallback = UnityEngine.Object.Instantiate(template, position, rotation, parent);
            fallback.gameObject.SetActive(true);
            return fallback;
        }

        public static void Despawn(Transform instance)
        {
            if (instance == null) return;
            if (PoolBoss.IsReady && PoolBoss.Despawn(instance))
                return;
            instance.gameObject.SetActive(false);
        }

        static void EnsurePoolBoss()
        {
            if (PoolBoss.Instance != null) return;
            var poolObject = new GameObject("PoolBoss");
            poolObject.AddComponent<PoolBoss>();
        }

        static void EnsureTemplateRoot()
        {
            if (_templateRoot != null) return;
            var rootObject = new GameObject("CrossDefensePoolTemplates");
            _templateRoot = rootObject.transform;
        }
    }
}
