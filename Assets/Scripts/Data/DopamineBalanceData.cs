using UnityEngine;

namespace CrossDefense.Data
{
    /// <summary>콤보와 오버드라이브의 충전·지속·메테오 수치를 조정하는 데이터다.</summary>
    [CreateAssetMenu(
        fileName = "DopamineBalance",
        menuName = "Cross Defense/Data/Dopamine Balance",
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
        [Min(0.05f)] [SerializeField] float meteorInterval = 0.3f;
        [Min(1)] [SerializeField] int meteorCount = 20;
        [Min(0f)] [SerializeField] float meteorDamageMultiplier = 0.55f;
        [Min(0.1f)] [SerializeField] float meteorRadius = 1.35f;
        [Min(0f)] [SerializeField] float meteorTargetJitter = 0.45f;

        public float ComboGraceSeconds => Mathf.Max(0.1f, comboGraceSeconds);
        public int FirstComboThreshold => Mathf.Max(1, firstComboThreshold);
        public int SecondComboThreshold => Mathf.Max(FirstComboThreshold + 1, secondComboThreshold);
        public int ThirdComboThreshold => Mathf.Max(SecondComboThreshold + 1, thirdComboThreshold);
        public int MaxGauge => Mathf.Max(1, maxGauge);
        public float OverdriveDuration => Mathf.Max(0.1f, overdriveDuration);
        public float MeteorInterval => Mathf.Max(0.05f, meteorInterval);
        public int MeteorCount => Mathf.Max(1, meteorCount);
        public float MeteorDamageMultiplier => Mathf.Max(0f, meteorDamageMultiplier);
        public float MeteorRadius => Mathf.Max(0.1f, meteorRadius);
        public float MeteorTargetJitter => Mathf.Max(0f, meteorTargetJitter);

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
            meteorInterval = Mathf.Max(0.05f, meteorInterval);
            meteorCount = Mathf.Max(1, meteorCount);
            meteorDamageMultiplier = Mathf.Max(0f, meteorDamageMultiplier);
            meteorRadius = Mathf.Max(0.1f, meteorRadius);
            meteorTargetJitter = Mathf.Max(0f, meteorTargetJitter);
        }
    }
}
