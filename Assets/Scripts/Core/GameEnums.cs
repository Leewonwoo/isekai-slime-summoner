namespace CrossDefense
{
    /// <summary>Legacy compass labels retained for data migration only.</summary>
    public enum Direction
    {
        North,
        East,
        South,
        West,
    }

    /// <summary>화면 외곽 스폰 구역. 레인이 아니라 스폰 위치 분산용이다.</summary>
    public enum SpawnZone
    {
        Top,
        Right,
        Bottom,
        Left,
    }

    public enum ThreatLevel
    {
        None,
        Normal,
        Danger,
    }

    public enum DamageTextKind
    {
        Dealt,
        Received,
        Healing,
    }

    public enum RunPhase
    {
        Prepare,
        InWave,
        Intermission,
        TraitChoice,
        Merchant,
        Victory,
        Defeat,
    }

    [System.Flags]
    public enum GameplayPauseReason
    {
        None = 0,
        TraitChoice = 1 << 0,
        SummonRoulette = 1 << 1,
        MonsterCodex = 1 << 2,
        Merchant = 1 << 3,
        SlimeCodex = 1 << 4,
        RunResult = 1 << 5,
        Settings = 1 << 6,
        Tutorial = 1 << 7,
    }
}
