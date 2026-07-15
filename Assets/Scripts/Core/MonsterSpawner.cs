using CrossDefense.Data;
using CrossDefense.Units;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>카메라 밖 확장 영역에서 몬스터를 생성하고 간단한 풀링을 담당한다.</summary>
    public sealed class MonsterSpawner
    {
        readonly Transform _parent;
        readonly Camera _camera;
        readonly Transform _monsterTemplate;
        readonly float _spawnMargin;
        readonly float _spawnDepth;

        public MonsterSpawner(Transform parent, Camera camera, float spawnMargin = 1.5f, float spawnDepth = 2f)
        {
            _parent = parent;
            _camera = camera != null ? camera : Camera.main;
            _spawnMargin = Mathf.Max(0.1f, spawnMargin);
            _spawnDepth = Mathf.Max(0.1f, spawnDepth);
            _monsterTemplate = RuntimePoolService.GetOrCreateTemplate(
                "CrossDefenseMonster",
                gameObject =>
                {
                    var renderer = gameObject.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 2;
                    var collider = gameObject.AddComponent<CircleCollider2D>();
                    collider.radius = 0.35f;
                    gameObject.AddComponent<MonsterController>();
                },
                24,
                256);
        }

        public MonsterController Spawn(GameManager gameManager, Transform target, MonsterData data,
            SpawnZone zone, float hpMultiplier, float speedMultiplier, float rewardMultiplier, System.Random random)
        {
            var spawned = RuntimePoolService.Spawn(
                _monsterTemplate,
                GetSpawnPosition(zone, random),
                Quaternion.identity,
                _parent);
            if (spawned == null) return null;
            var monster = spawned.GetComponent<MonsterController>();
            monster.Initialize(gameManager, target, data, hpMultiplier, speedMultiplier, rewardMultiplier);
            return monster;
        }

        public void Release(MonsterController monster)
        {
            if (monster == null) return;
            monster.ResetForPool();
            RuntimePoolService.Despawn(monster.transform);
        }

        Vector3 GetSpawnPosition(SpawnZone zone, System.Random random)
        {
            if (_camera == null)
                return Vector3.right * 8f;

            float height = _camera.orthographicSize;
            float width = height * _camera.aspect;
            Vector3 center = _camera.transform.position;
            float minX = center.x - width;
            float maxX = center.x + width;
            float minY = center.y - height;
            float maxY = center.y + height;
            float along = (float)random.NextDouble();
            float depth = _spawnMargin + (float)random.NextDouble() * _spawnDepth;

            return zone switch
            {
                SpawnZone.Top => new Vector3(Mathf.Lerp(minX, maxX, along), maxY + depth, 0f),
                SpawnZone.Right => new Vector3(maxX + depth, Mathf.Lerp(minY, maxY, along), 0f),
                SpawnZone.Bottom => new Vector3(Mathf.Lerp(minX, maxX, along), minY - depth, 0f),
                _ => new Vector3(minX - depth, Mathf.Lerp(minY, maxY, along), 0f),
            };
        }

    }
}
