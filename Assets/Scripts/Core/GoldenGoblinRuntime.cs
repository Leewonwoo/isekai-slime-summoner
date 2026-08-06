using UnityEngine;

namespace CrossDefense.Core
{
    public enum GoldenGoblinState
    {
        Hidden,
        Warning,
        Active,
        Defeated,
        Escaped,
    }

    public readonly struct GoldenGoblinSnapshot
    {
        public GoldenGoblinState State { get; }
        public float RemainingTime { get; }
        public float Duration { get; }
        public int GoldReward { get; }

        public GoldenGoblinSnapshot(
            GoldenGoblinState state,
            float remainingTime,
            float duration,
            int goldReward)
        {
            State = state;
            RemainingTime = Mathf.Max(0f, remainingTime);
            Duration = Mathf.Max(0f, duration);
            GoldReward = Mathf.Max(0, goldReward);
        }
    }
}
