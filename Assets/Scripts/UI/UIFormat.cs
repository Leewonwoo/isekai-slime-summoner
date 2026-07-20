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

        public static string StackedCapacity(int stacks, int capacity, int totalUnits) =>
            $"보유 {totalUnits:N0}/{capacity:N0} · 슬롯 {stacks:N0}개";

        /// <summary>"현재 → 다음" 변화량 표기 (SPEC §4.5)</summary>
        public static string Delta(float current, float next) => $"{Num(current)} → {Num(next)}";

        public static string Percent(float multiplier) => $"{multiplier * 100f:0.#}%";

        public static string PercentDelta(float current, float next) =>
            $"{Percent(current)} → {Percent(next)}";

        public static string ChanceDelta(float current, float next) =>
            $"{current * 100f:0.#}% → {next * 100f:0.#}%";

        public static string HpDelta(float current, float next) =>
            $"{Num(current)} HP → {Num(next)} HP";

        static string Num(float v) => v % 1f == 0f ? ((int)v).ToString("N0") : v.ToString("0.##");
    }
}
