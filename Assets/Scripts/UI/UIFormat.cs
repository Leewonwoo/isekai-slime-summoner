namespace CrossDefense.UI
{
    /// <summary>UI 수치 표기 헬퍼 — 표기 포맷은 전부 여기로 (ui-guidelines §7-4)</summary>
    public static class UIFormat
    {
        public static string Gold(int value) => $"{value:N0}";

        public static string Wave(int current, int total) => $"Wave {current}/{total}";

        /// <summary>"현재 → 다음" 변화량 표기 (SPEC §4.5)</summary>
        public static string Delta(float current, float next) => $"{Num(current)} → {Num(next)}";

        public static string Badge(Direction dir, int count) => $"{DirLetter(dir)} ×{count}";

        public static string DirLetter(Direction dir) => dir switch
        {
            Direction.North => "N",
            Direction.East => "E",
            Direction.South => "S",
            _ => "W",
        };

        static string Num(float v) => v % 1f == 0f ? ((int)v).ToString("N0") : v.ToString("0.##");
    }
}
