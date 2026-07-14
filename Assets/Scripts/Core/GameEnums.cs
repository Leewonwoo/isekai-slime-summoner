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
}
