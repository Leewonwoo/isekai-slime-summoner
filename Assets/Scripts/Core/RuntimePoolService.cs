using System;
using System.Collections.Generic;
using DarkTonic.PoolBoss;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>PoolBoss 런타임 등록과 초기화 중 임시 풀을 한 곳에서 관리한다.</summary>
    public static class RuntimePoolService
    {
        const string Category = "CrossDefense Runtime";

        static readonly Dictionary<string, Transform> Templates = new(StringComparer.Ordinal);
        static readonly Dictionary<string, PoolRegistration> Registrations = new(StringComparer.Ordinal);
        static readonly Dictionary<string, Stack<Transform>> FallbackPools = new(StringComparer.Ordinal);
        static readonly Dictionary<Transform, FallbackState> FallbackInstances = new();

        static Transform _templateRoot;
        static Transform _fallbackRoot;

        public static Transform GetOrCreateTemplate(string name, Action<GameObject> configure, int preload = 8, int hardLimit = 128)
        {
            if (Templates.TryGetValue(name, out var cached) && cached != null)
            {
                if (Registrations.TryGetValue(name, out var cachedRegistration))
                    PreparePoolBoss(cachedRegistration);

                return cached;
            }

            EnsureTemplateRoot();

            var templateObject = new GameObject(name);
            templateObject.transform.SetParent(_templateRoot, false);
            configure?.Invoke(templateObject);
            templateObject.SetActive(false);

            var registration = new PoolRegistration(
                templateObject.transform,
                Mathf.Max(1, preload),
                Mathf.Max(preload, hardLimit));

            Templates[name] = templateObject.transform;
            Registrations[name] = registration;
            PreparePoolBoss(registration);
            return templateObject.transform;
        }

        public static Transform Spawn(Transform template, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (template == null) return null;

            var registration = GetOrCreateRegistration(template);
            PreparePoolBoss(registration);

            if (EnsureRegistered(registration))
                return PoolBoss.Spawn(template, position, rotation, parent);

            // A scene-authored PoolBoss may still be spreading initialization over several frames.
            // Keep first-frame summons/projectiles alive without calling PoolBoss before IsReady.
            return SpawnFallback(template, position, rotation, parent);
        }

        public static void Despawn(Transform instance)
        {
            if (instance == null) return;

            if (FallbackInstances.TryGetValue(instance, out var fallbackState))
            {
                StoreFallback(instance, fallbackState);
                return;
            }

            if (PoolBoss.IsReady && PoolBoss.Despawn(instance))
                return;

            instance.gameObject.SetActive(false);
        }

        static PoolRegistration GetOrCreateRegistration(Transform template)
        {
            if (Registrations.TryGetValue(template.name, out var registration) && registration.Template == template)
                return registration;

            registration = new PoolRegistration(template, 1, 128);
            Registrations[template.name] = registration;
            return registration;
        }

        static void PreparePoolBoss(PoolRegistration registration)
        {
            var boss = PoolBoss.Instance;
            if (boss == null)
            {
                CreatePoolBossWithSeed(registration);
                return;
            }

            if (PoolBoss.IsReady)
            {
                EnsureRegistered(registration);
                return;
            }

            StageForInitialization(boss, registration);
        }

        static void CreatePoolBossWithSeed(PoolRegistration registration)
        {
            // PoolBoss with zero startup items never reaches IsReady in the installed version.
            // Configure the first item while inactive so Awake initializes a valid pool immediately.
            var poolObject = new GameObject("PoolBoss");
            poolObject.SetActive(false);

            var boss = poolObject.AddComponent<PoolBoss>();
            boss.poolItems.Add(CreatePoolItem(registration));
            registration.StagedForInitialization = true;

            poolObject.SetActive(true);
            registration.Registered = PoolBoss.IsReady &&
                                      PoolBoss.PoolItemInfoByName(registration.Template.name) != null;
        }

        static void StageForInitialization(PoolBoss boss, PoolRegistration registration)
        {
            if (registration.StagedForInitialization || ContainsPoolItem(boss, registration.Template.name))
            {
                registration.StagedForInitialization = true;
                return;
            }

            // ContinueInit reads poolItems every frame, so pending runtime templates can join startup safely.
            // For an originally empty PoolBoss, force the single-frame branch to avoid a zero init ratio.
            if (boss.poolItems.Count == 0)
                boss.framesForInit = 1;

            boss.poolItems.Add(CreatePoolItem(registration));
            registration.StagedForInitialization = true;
        }

        static bool EnsureRegistered(PoolRegistration registration)
        {
            if (!PoolBoss.IsReady)
                return false;

            if (PoolBoss.PoolItemInfoByName(registration.Template.name) == null)
            {
                PoolBoss.CreateNewPoolItem(
                    registration.Template,
                    registration.Preload,
                    true,
                    registration.HardLimit,
                    false,
                    Category,
                    PoolBoss.PrefabSource.Prefab,
                    false);
            }

            registration.Registered = PoolBoss.PoolItemInfoByName(registration.Template.name) != null;
            return registration.Registered;
        }

        static PoolBossItem CreatePoolItem(PoolRegistration registration)
        {
            return new PoolBossItem
            {
                prefabSource = PoolBoss.PrefabSource.Prefab,
                prefabTransform = registration.Template,
                gameObject = registration.Template.gameObject,
                instancesToPreload = registration.Preload,
                allowInstantiateMore = true,
                itemHardLimit = registration.HardLimit,
                logMessages = false,
                allowRecycle = false,
                categoryName = Category,
                isExpanded = true
            };
        }

        static bool ContainsPoolItem(PoolBoss boss, string templateName)
        {
            for (var i = 0; i < boss.poolItems.Count; i++)
            {
                var item = boss.poolItems[i];
                if (item?.prefabTransform != null && item.prefabTransform.name == templateName)
                    return true;
            }

            return false;
        }

        static Transform SpawnFallback(Transform template, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (!FallbackPools.TryGetValue(template.name, out var available))
            {
                available = new Stack<Transform>();
                FallbackPools[template.name] = available;
            }

            while (available.Count > 0)
            {
                var reused = available.Pop();
                if (reused == null)
                    continue;

                var state = FallbackInstances[reused];
                state.IsStored = false;
                reused.SetParent(parent, true);
                reused.SetPositionAndRotation(position, rotation);
                reused.gameObject.SetActive(true);
                return reused;
            }

            var instance = UnityEngine.Object.Instantiate(template, position, rotation, parent);
            instance.name = template.name + " (Fallback)";
            instance.gameObject.SetActive(true);
            FallbackInstances[instance] = new FallbackState(template.name);
            return instance;
        }

        static void StoreFallback(Transform instance, FallbackState state)
        {
            if (state.IsStored)
                return;

            state.IsStored = true;
            instance.gameObject.SetActive(false);
            EnsureFallbackRoot();
            instance.SetParent(_fallbackRoot, false);

            if (!FallbackPools.TryGetValue(state.PoolKey, out var available))
            {
                available = new Stack<Transform>();
                FallbackPools[state.PoolKey] = available;
            }

            available.Push(instance);
        }

        static void EnsureTemplateRoot()
        {
            if (_templateRoot != null) return;
            var rootObject = new GameObject("CrossDefensePoolTemplates");
            _templateRoot = rootObject.transform;
        }

        static void EnsureFallbackRoot()
        {
            if (_fallbackRoot != null) return;
            var rootObject = new GameObject("CrossDefenseFallbackPool");
            _fallbackRoot = rootObject.transform;
        }

        sealed class PoolRegistration
        {
            public PoolRegistration(Transform template, int preload, int hardLimit)
            {
                Template = template;
                Preload = preload;
                HardLimit = hardLimit;
            }

            public Transform Template { get; }
            public int Preload { get; }
            public int HardLimit { get; }
            public bool StagedForInitialization { get; set; }
            public bool Registered { get; set; }
        }

        sealed class FallbackState
        {
            public FallbackState(string poolKey)
            {
                PoolKey = poolKey;
            }

            public string PoolKey { get; }
            public bool IsStored { get; set; }
        }
    }
}
