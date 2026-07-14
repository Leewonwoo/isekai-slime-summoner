using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Units
{
    /// <summary>외곽에서 소환사를 향해 이동하는 몬스터의 최소 생명주기.</summary>
    public sealed class MonsterController : MonoBehaviour
    {
        MonsterData _data;
        GameManager _gameManager;
        Transform _target;
        float _hp;
        float _speed;
        int _contactDamage;
        int _rewardGold;
        bool _resolved;

        public MonsterData Data => _data;
        public float CurrentHp => _hp;
        public float MaxHp { get; private set; }

        public void Initialize(GameManager gameManager, Transform target, MonsterData data,
            float hpMultiplier, float speedMultiplier, float rewardMultiplier)
        {
            _gameManager = gameManager;
            _target = target;
            _data = data;
            MaxHp = Mathf.Max(1f, data.BaseHp * hpMultiplier);
            _hp = MaxHp;
            _speed = Mathf.Max(0.01f, data.MoveSpeed * speedMultiplier);
            _contactDamage = Mathf.Max(0, data.ContactDamage);
            _rewardGold = Mathf.Max(0, Mathf.RoundToInt(data.RewardGold * rewardMultiplier));
            _resolved = false;
            ApplyVisual(data);
        }

        void Update()
        {
            if (_resolved || _target == null) return;

            Vector3 targetPosition = _target.position;
            Vector3 offset = targetPosition - transform.position;
            float arrivalDistance = 0.45f;
            if (offset.sqrMagnitude <= arrivalDistance * arrivalDistance)
            {
                ResolveReachedCore();
                return;
            }

            transform.position += offset.normalized * (_speed * Time.unscaledDeltaTime);
        }

        public void TakeDamage(float amount)
        {
            if (_resolved || amount <= 0f) return;

            _hp = Mathf.Max(0f, _hp - amount);
            if (_hp <= 0f)
                ResolveDefeated();
        }

        public void ResetForPool()
        {
            _data = null;
            _gameManager = null;
            _target = null;
            _hp = 0f;
            MaxHp = 0f;
            _resolved = false;
            gameObject.SetActive(false);
        }

        void ResolveDefeated()
        {
            if (_resolved) return;
            _resolved = true;
            _gameManager.NotifyMonsterDefeated(this, _rewardGold);
        }

        void ResolveReachedCore()
        {
            if (_resolved) return;
            _resolved = true;
            _gameManager.NotifyMonsterReachedCore(this, _contactDamage);
        }

        void ApplyVisual(MonsterData data)
        {
            if (!TryGetComponent<SpriteRenderer>(out var renderer))
                renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSprite.Shared;
            renderer.color = AttributeColor(data.Attribute);
            float size = Mathf.Clamp(data.SizeMultiplier, 0.5f, 2f);
            transform.localScale = Vector3.one * size;
            renderer.sortingOrder = 2;
            gameObject.name = data.DisplayName;
        }

        static Color AttributeColor(MonsterAttribute attribute)
        {
            return attribute switch
            {
                MonsterAttribute.Fire => new Color(1f, 0.32f, 0.2f),
                MonsterAttribute.Ice => new Color(0.35f, 0.75f, 1f),
                MonsterAttribute.Nature => new Color(0.4f, 0.9f, 0.45f),
                _ => new Color(0.72f, 0.72f, 0.72f),
            };
        }

        static class RuntimeSprite
        {
            static Sprite _shared;
            public static Sprite Shared => _shared ??= Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }
    }
}
