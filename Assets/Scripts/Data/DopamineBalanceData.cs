using UnityEngine;

namespace CrossDefense.Data
{
    /// <summary>콤보와 오버드라이브의 충전·지속·메테오 수치를 조정하는 데이터다.</summary>
    [CreateAssetMenu(
        fileName = "DopamineBalance",
        menuName = "Isekai Slime Summoner/Data/Dopamine Balance",
        order = 32)]
    public sealed class DopamineBalanceData : ScriptableObject
    {
        [Header("Combo")]
        [Min(0.1f)] [SerializeField] float comboGraceSeconds = 2f;
        [Min(1)] [SerializeField] int firstComboThreshold = 10;
        [Min(2)] [SerializeField] int secondComboThreshold = 20;
        [Min(3)] [SerializeField] int thirdComboThreshold = 30;
        [Min(0)] [SerializeField] int baseGaugePerKill = 3;
        [Min(0)] [SerializeField] int firstTierGaugePerKill = 5;
        [Min(0)] [SerializeField] int secondTierGaugePerKill = 8;
        [Min(0)] [SerializeField] int thirdTierGaugePerKill = 12;

        [Header("Overdrive")]
        [Min(1)] [SerializeField] int maxGauge = 100;
        [Min(0.1f)] [SerializeField] float overdriveDuration = 6f;
        [Min(1f)] [SerializeField] float overdriveDamageMultiplier = 1.3f;
        [Min(1f)] [SerializeField] float overdriveAttackSpeedMultiplier = 1.5f;

        public float ComboGraceSeconds => Mathf.Max(0.1f, comboGraceSeconds);
        public int FirstComboThreshold => Mathf.Max(1, firstComboThreshold);
        public int SecondComboThreshold => Mathf.Max(FirstComboThreshold + 1, secondComboThreshold);
        public int ThirdComboThreshold => Mathf.Max(SecondComboThreshold + 1, thirdComboThreshold);
        public int MaxGauge => Mathf.Max(1, maxGauge);
        public float OverdriveDuration => Mathf.Max(0.1f, overdriveDuration);
        public float OverdriveDamageMultiplier => Mathf.Max(1f, overdriveDamageMultiplier);
        public float OverdriveAttackSpeedMultiplier =>
            Mathf.Max(1f, overdriveAttackSpeedMultiplier);

        public int GaugePerKill(int combo)
        {
            int safeCombo = Mathf.Max(1, combo);
            if (safeCombo >= ThirdComboThreshold)
                return Mathf.Max(0, thirdTierGaugePerKill);
            if (safeCombo >= SecondComboThreshold)
                return Mathf.Max(0, secondTierGaugePerKill);
            if (safeCombo >= FirstComboThreshold)
                return Mathf.Max(0, firstTierGaugePerKill);
            return Mathf.Max(0, baseGaugePerKill);
        }

        public bool IsComboMilestone(int combo) =>
            combo == FirstComboThreshold ||
            combo == SecondComboThreshold ||
            combo == ThirdComboThreshold;

        public static DopamineBalanceData CreateRuntimeDefault()
        {
            var data = CreateInstance<DopamineBalanceData>();
            data.hideFlags = HideFlags.HideAndDontSave;
            return data;
        }

        void OnValidate()
        {
            comboGraceSeconds = Mathf.Max(0.1f, comboGraceSeconds);
            firstComboThreshold = Mathf.Max(1, firstComboThreshold);
            secondComboThreshold = Mathf.Max(firstComboThreshold + 1, secondComboThreshold);
            thirdComboThreshold = Mathf.Max(secondComboThreshold + 1, thirdComboThreshold);
            maxGauge = Mathf.Max(1, maxGauge);
            overdriveDuration = Mathf.Max(0.1f, overdriveDuration);
            overdriveDamageMultiplier = Mathf.Max(1f, overdriveDamageMultiplier);
            overdriveAttackSpeedMultiplier = Mathf.Max(
                1f,
                overdriveAttackSpeedMultiplier);
        }
    }
}
