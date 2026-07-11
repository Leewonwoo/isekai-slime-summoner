namespace CrossDefense
{
    /// <summary>레인 방향 (십자 맵 4방향)</summary>
    public enum Direction
    {
        North,
        East,
        South,
        West,
    }

    /// <summary>방향 예고 배지 위협도 (SPEC §4.2)</summary>
    public enum ThreatLevel
    {
        None,    // 회색 — 없음
        Normal,  // 노랑 — 보통
        Danger,  // 빨강 — 위험
    }
}
