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
            return SpawnAtPosition(
                gameManager,
                target,
                data,
                GetSpawnPosition(zone, random),
                hpMultiplier,
                speedMultiplier,
                rewardMultiplier,
                sizeMultiplier,
                allowDefeatSplit: true,
                grantsDefeatRewards: true);
        }

        public MonsterController SpawnAtPosition(
            GameManager gameManager,
            Transform target,
            MonsterData data,
            Vector3 position,
            float hpMultiplier,
            float speedMultiplier,
            float rewardMultiplier,
            float sizeMultiplier,
            bool allowDefeatSplit = false,
            bool grantsDefeatRewards = false)
        {
            if (data == null) return null;
            var spawned = RuntimePoolService.Spawn(
                _monsterTemplate,
                position,
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
                sizeMultiplier,
                allowDefeatSplit,
                grantsDefeatRewards);
            return monster;
        }

        public MonsterController SpawnGoldenGoblin(
            GameManager gameManager,
            Transform target,
            GoldenGoblinSettings settings,
            SpawnZone zone,
            float waveHpMultiplier,
            float waveSpeedMultiplier,
            System.Random random)
        {
            if (settings?.Monster == null)
                return null;

            Vector3 spawnPosition = GetSpawnPosition(zone, random);
            var spawned = RuntimePoolService.Spawn(
                _monsterTemplate,
                spawnPosition,
                Quaternion.identity,
                _parent);
            if (spawned == null) return null;

            var monster = spawned.GetComponent<MonsterController>();
            monster.Initialize(
                gameManager,
                target,
                settings.Monster,
                Mathf.Max(0.01f, waveHpMultiplier * settings.HpMultiplier),
                Mathf.Max(0.01f, waveSpeedMultiplier * settings.SpeedMultiplier),
                1f,
                settings.SizeMultiplier);
            monster.ConfigureGoldenRunner(
                GetOppositeExitPosition(zone, spawnPosition),
                settings.EscapeDuration);
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

        Vector3 GetOppositeExitPosition(SpawnZone originZone, Vector3 origin)
        {
            GetFieldBounds(out float minX, out float maxX, out float minY, out float maxY);
            float depth = _maxOutsideDistance + 0.25f;
            return originZone switch
            {
                SpawnZone.Top => new Vector3(origin.x, minY - depth, 0f),
                SpawnZone.Right => new Vector3(minX - depth, origin.y, 0f),
                SpawnZone.Bottom => new Vector3(origin.x, maxY + depth, 0f),
                _ => new Vector3(maxX + depth, origin.y, 0f),
            };
        }

        void GetFieldBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            if (_gameplayBackground != null && _gameplayBackground.sprite != null)
            {
                Bounds bounds = _gameplayBackground.bounds;
                minX = bounds.min.x;
                maxX = bounds.max.x;
                minY = bounds.min.y;
                maxY = bounds.max.y;
                return;
            }

            float height = _camera != null ? _camera.orthographicSize : 5f;
            float width = height * (_camera != null ? _camera.aspect : 0.5625f);
            Vector3 center = _camera != null ? _camera.transform.position : Vector3.zero;
            minX = center.x - width;
            maxX = center.x + width;
            minY = center.y - height;
            maxY = center.y + height;
        }

    }
}
