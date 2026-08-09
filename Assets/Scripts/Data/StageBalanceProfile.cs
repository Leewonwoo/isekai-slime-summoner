using UnityEngine;

namespace CrossDefense.Data
{
    /// <summary>여러 스테이지가 공유할 수 있는 전역 난이도 배율.</summary>
    [CreateAssetMenu(
        fileName = "StageBalanceProfile",
        menuName = "Isekai Slime Summoner/Data/Stage Balance Profile",
        order = 11)]
    public sealed class StageBalanceProfile : ScriptableObject
    {
        [Min(0.01f)] [SerializeField] float hpMultiplier = 1f;
        [Min(0.01f)] [SerializeField] float speedMultiplier = 1f;
        [Min(0.01f)] [SerializeField] float rewardMultiplier = 1f;
        [Min(0.01f)] [SerializeField] float spawnIntervalMultiplier = 1f;

        public float HpMultiplier => Mathf.Max(0.01f, hpMultiplier);
        public float SpeedMultiplier => Mathf.Max(0.01f, speedMultiplier);
        public float RewardMultiplier => Mathf.Max(0.01f, rewardMultiplier);
        public float SpawnIntervalMultiplier => Mathf.Max(0.01f, spawnIntervalMultiplier);
    }
}
