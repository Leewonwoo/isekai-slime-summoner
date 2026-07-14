using System.Collections.Generic;
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
        readonly Stack<MonsterController> _pool = new();
        readonly float _spawnMargin;
        readonly float _spawnDepth;

        public MonsterSpawner(Transform parent, Camera camera, float spawnMargin = 1.5f, float spawnDepth = 2f)
        {
            _parent = parent;
            _camera = camera != null ? camera : Camera.main;
            _spawnMargin = Mathf.Max(0.1f, spawnMargin);
            _spawnDepth = Mathf.Max(0.1f, spawnDepth);
        }

        public MonsterController Spawn(GameManager gameManager, Transform target, MonsterData data,
            SpawnZone zone, float hpMultiplier, float speedMultiplier, float rewardMultiplier, System.Random random)
        {
            var monster = _pool.Count > 0 ? _pool.Pop() : CreateMonsterObject();
            monster.transform.SetParent(_parent, true);
            monster.transform.position = GetSpawnPosition(zone, random);
            monster.gameObject.SetActive(true);
            monster.Initialize(gameManager, target, data, hpMultiplier, speedMultiplier, rewardMultiplier);
            return monster;
        }

        public void Release(MonsterController monster)
        {
            if (monster == null) return;
            monster.ResetForPool();
            _pool.Push(monster);
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

        static MonsterController CreateMonsterObject()
        {
            var gameObject = new GameObject("Monster");
            gameObject.AddComponent<SpriteRenderer>();
            return gameObject.AddComponent<MonsterController>();
        }
    }
}
