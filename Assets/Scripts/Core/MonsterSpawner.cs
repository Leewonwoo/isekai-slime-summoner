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
        readonly SpriteRenderer _gameplayBackground;
        readonly Transform _monsterTemplate;
        readonly float _minOutsideDistance;
        readonly float _maxOutsideDistance;

        public MonsterSpawner(
            Transform parent,
            Camera camera,
            SpriteRenderer gameplayBackground,
            float minOutsideDistance = 0.4f,
            float maxOutsideDistance = 1.1f)
        {
            _parent = parent;
            _camera = camera != null ? camera : Camera.main;
            _gameplayBackground = gameplayBackground;
            _minOutsideDistance = Mathf.Max(0.1f, minOutsideDistance);
            _maxOutsideDistance = Mathf.Max(_minOutsideDistance, maxOutsideDistance);
            _monsterTemplate = RuntimePoolService.GetOrCreateTemplate(
                "CrossDefenseMonster",
                gameObject =>
                {
                    var renderer = gameObject.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 2;
                    var collider = gameObject.AddComponent<CircleCollider2D>();
                    collider.radius = 0.35f;
                    gameObject.AddComponent<WorldHealthBar>();
                    gameObject.AddComponent<MonsterController>();
                },
                24,
                256);
        }

        public MonsterController Spawn(GameManager gameManager, Transform target, MonsterData data,
            SpawnZone zone, float hpMultiplier, float speedMultiplier, float rewardMultiplier,
            float sizeMultiplier, System.Random random)
        {
            var spawned = RuntimePoolService.Spawn(
                _monsterTemplate,
                GetSpawnPosition(zone, random),
                Quaternion.identity,
                _parent);
            if (spawned == null) return null;
            var monster = spawned.GetComponent<MonsterController>();
            monster.Initialize(
                gameManager,
                target,
                data,
                hpMultiplier,
                speedMultiplier,
                rewardMultiplier,
                sizeMultiplier);
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
            if (_gameplayBackground == null && _camera == null)
                return Vector3.right * 8f;

            float minX;
            float maxX;
            float minY;
            float maxY;
            if (_gameplayBackground != null && _gameplayBackground.sprite != null)
            {
                Bounds bounds = _gameplayBackground.bounds;
                minX = bounds.min.x;
                maxX = bounds.max.x;
                minY = bounds.min.y;
                maxY = bounds.max.y;
            }
            else
            {
                float height = _camera.orthographicSize;
                float width = height * _camera.aspect;
                Vector3 center = _camera.transform.position;
                minX = center.x - width;
                maxX = center.x + width;
                minY = center.y - height;
                maxY = center.y + height;
            }
            float along = (float)random.NextDouble();
            float depth = Mathf.Lerp(
                _minOutsideDistance,
                _maxOutsideDistance,
                (float)random.NextDouble());

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
