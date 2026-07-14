namespace CrossDefense.UI
{
    /// <summary>UI 수치 표기 헬퍼 — 표기 포맷은 전부 여기로 (ui-guidelines §7-4)</summary>
    public static class UIFormat
    {
        public static string Gold(int value) => $"{value:N0}";

        public static string Wave(int current) => $"WAVE {current}";

        public static string RemainingMonsters(int value) => $"잔여 몬스터 {value:N0}";

        public static string Gems(int value) => $"{value:N0}";

        public static string SummonContracts(int value) => $"{value:N0}장";

        public static string Capacity(int current, int total) => $"{current:N0} / {total:N0}";

        /// <summary>"현재 → 다음" 변화량 표기 (SPEC §4.5)</summary>
        public static string Delta(float current, float next) => $"{Num(current)} → {Num(next)}";

        static string Num(float v) => v % 1f == 0f ? ((int)v).ToString("N0") : v.ToString("0.##");
    }
}
