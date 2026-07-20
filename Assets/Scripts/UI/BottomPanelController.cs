using System;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    public readonly struct GrowthRowModel
    {
        public string Key { get; }
        public RunUpgradeType RunUpgradeType { get; }
        public string Name { get; }
        public string Values { get; }
        public string ActionText { get; }
        public string IconClass { get; }
        public bool CanPurchase { get; }

        public GrowthRowModel(
            string key,
            string name,
            string values,
            string actionText,
            string iconClass,
            bool canPurchase,
            RunUpgradeType runUpgradeType = RunUpgradeType.AttackPower)
        {
            Key = key;
            RunUpgradeType = runUpgradeType;
            Name = name;
            Values = values;
            ActionText = actionText;
            IconClass = iconClass;
            CanPurchase = canPurchase;
        }
    }

    public readonly struct SummonerInfoModel
    {
        public string Name { get; }
        public string Level { get; }
        public string Experience { get; }
        public float ExperienceProgress { get; }
        public string Damage { get; }
        public string AttackSpeed { get; }
        public string MaxHp { get; }
        public string CriticalChance { get; }
        public string JackpotChance { get; }
        public string PermanentTraits { get; }
        public string RunTraits { get; }

        public SummonerInfoModel(
            string name,
            string level,
            string experience,
            float experienceProgress,
            string damage,
            string attackSpeed,
            string maxHp,
            string criticalChance,
            string jackpotChance,
            string permanentTraits,
            string runTraits)
        {
            Name = name;
            Level = level;
            Experience = experience;
            ExperienceProgress = Mathf.Clamp01(experienceProgress);
            Damage = damage;
            AttackSpeed = attackSpeed;
            MaxHp = maxHp;
            CriticalChance = criticalChance;
            JackpotChance = jackpotChance;
            PermanentTraits = permanentTraits;
            RunTraits = runTraits;
        }
    }

    /// <summary>하단 패널 — 탭 5개 전환, 벤치/리스트 콘텐츠 구성</summary>
    public class BottomPanelController
    {
        const int BenchSlotCount = 12;
        const int GearSlotCount = 6;
        const float BenchDragThreshold = 22f;

        static readonly string[] TabKeys = { "summon", "upgrade", "skill", "gear", "summoner" };

        readonly Dictionary<string, Button> _tabButtons = new();
        readonly Dictionary<string, VisualElement> _tabPages = new();
        readonly VisualElement _root;
        readonly VisualElement _bench;
        readonly List<VisualElement> _benchSlots = new();
        readonly VisualTreeAsset _upgradeRowTemplate;
        readonly ScrollView _upgradeList;
        readonly Dictionary<string, GrowthRowBinding> _upgradeRows = new();
        readonly Label _benchCount;
        readonly Label _summonContractCount;
        readonly Label _summonProbability;
        readonly Button _summonButton;
        readonly Label _summonerTabName;
        readonly Label _summonerTabLevel;
        readonly Label _summonerExpText;
        readonly VisualElement _summonerExpFill;
        readonly Label _summonerDamage;
        readonly Label _summonerAttackSpeed;
        readonly Label _summonerMaxHp;
        readonly Label _summonerCriticalChance;
        readonly Label _summonerJackpotChance;
        readonly Label _summonerPermanentTraits;
        readonly Label _summonerRunTraits;
        int _summonContracts;
        int _summonCapacity = BenchSlotCount;
        int _ownedUnitCount;
        bool _isSummonAnimating;

        public event Action SummonRequested;
        public event Action<SummonUnitInstance, int> BenchSlotSelected;
        public event Action<SummonUnitInstance, Vector2> BenchDragStarted;
        public event Action<SummonUnitInstance, Vector2> BenchDragMoved;
        public event Action<SummonUnitInstance, Vector2> BenchDragEnded;
        public event Action<RunUpgradeType> RunUpgradeRequested;
        public bool IsSummonAnimating => _isSummonAnimating;

        public BottomPanelController(VisualElement root, VisualTreeAsset upgradeRowTemplate)
        {
            _root = root;
            _upgradeRowTemplate = upgradeRowTemplate;
            foreach (var key in TabKeys)
            {
                var captured = key;
                var button = root.Q<Button>($"tab-{key}");
                _tabButtons[key] = button;
                _tabPages[key] = root.Q<VisualElement>($"page-{key}");
                button.clicked += () => SelectTab(captured);
            }

            _bench = root.Q<VisualElement>("bench");
            BuildBenchSlots();
            BuildSlots(root.Q<VisualElement>("gear-slots"), GearSlotCount);
            _benchCount = root.Q<Label>("bench-count");
            _summonContractCount = root.Q<Label>("summon-contract-count");
            _summonProbability = root.Q<Label>("summon-prob");
            _summonButton = root.Q<Button>("summon-button");
            _summonButton.clicked += () => SummonRequested?.Invoke();
            _upgradeList = root.Q<ScrollView>("upgrade-list");
            _summonerTabName = root.Q<Label>("summoner-tab-name");
            _summonerTabLevel = root.Q<Label>("summoner-tab-level");
            _summonerExpText = root.Q<Label>("summoner-exp-text");
            _summonerExpFill = root.Q<VisualElement>("summoner-exp-fill");
            _summonerDamage = root.Q<Label>("summoner-stat-damage");
            _summonerAttackSpeed = root.Q<Label>("summoner-stat-attack-speed");
            _summonerMaxHp = root.Q<Label>("summoner-stat-max-hp");
            _summonerCriticalChance = root.Q<Label>("summoner-stat-critical");
            _summonerJackpotChance = root.Q<Label>("summoner-stat-jackpot");
            _summonerPermanentTraits = root.Q<Label>("summoner-permanent-traits");
            _summonerRunTraits = root.Q<Label>("summoner-run-traits");
            SetBenchUsage(0, 0, BenchSlotCount);
            SetSummonContracts(0);

            BuildDummyRows(root.Q<ScrollView>("skill-list"), upgradeRowTemplate,
                new[] { "메테오", "급속 소환", "코어 실드" }, "개방");
        }

        public void Dispose()
        {
            DisposeRows(_upgradeRows);
        }

        public void SelectTab(string key)
        {
            foreach (var tab in TabKeys)
            {
                bool active = tab == key;
                _tabButtons[tab].EnableInClassList("tab-bar__button--active", active);
                _tabPages[tab].EnableInClassList("hidden", !active);
            }
        }

        public void SetRedDot(string tabKey, bool on) =>
            _root.Q<VisualElement>($"reddot-{tabKey}")?.EnableInClassList("hidden", !on);

        public void SetBenchUsage(int usedStacks, int totalUnits, int capacity)
        {
            _summonCapacity = Mathf.Clamp(capacity, 1, BenchSlotCount);
            _ownedUnitCount = Mathf.Max(0, totalUnits);
            _benchCount.text = UIFormat.StackedCapacity(usedStacks, _summonCapacity, _ownedUnitCount);
            UpdateSummonButtonState();
        }

        public void SetSummonContracts(int amount)
        {
            _summonContracts = amount;
            _summonContractCount.text = UIFormat.SummonContracts(amount);
            UpdateSummonButtonState();
        }

        public void SetDirectRankOneChance(float chance) =>
            _summonProbability.text = $"★2 직행 잭팟 {Mathf.Clamp01(chance) * 100f:0.0}%";

        public void SetSummonAnimationState(bool isAnimating)
        {
            _isSummonAnimating = isAnimating;
            UpdateSummonButtonState();
        }

        public void SetGrowthRows(IReadOnlyList<GrowthRowModel> rows) =>
            SyncGrowthRows(_upgradeList, _upgradeRows, rows);

        public void SetSummonerInfo(SummonerInfoModel model)
        {
            _summonerTabName.text = model.Name;
            _summonerTabLevel.text = model.Level;
            _summonerExpText.text = model.Experience;
            _summonerExpFill.style.width = Length.Percent(model.ExperienceProgress * 100f);
            _summonerDamage.text = model.Damage;
            _summonerAttackSpeed.text = model.AttackSpeed;
            _summonerMaxHp.text = model.MaxHp;
            _summonerCriticalChance.text = model.CriticalChance;
            _summonerJackpotChance.text = model.JackpotChance;
            _summonerPermanentTraits.text = model.PermanentTraits;
            _summonerRunTraits.text = model.RunTraits;
        }

        public void SetOwnedUnits(
            IReadOnlyList<SummonUnitInstance> benchUnits,
            IReadOnlyList<SummonUnitInstance> deployedUnits,
            int capacity)
        {
            var stacks = BuildOwnedStacks(benchUnits, deployedUnits);
            for (int i = 0; i < _benchSlots.Count; i++)
            {
                var slot = _benchSlots[i];
                slot.EnableInClassList("hidden", i >= capacity);
                slot.Clear();
                SetSlotState(slot, null, false);
                if (i >= stacks.Count)
                    continue;

                var stack = stacks[i];
                var instance = stack.Representative;
                bool mergeable =
                    instance.Rank < SummonRank.MaxInternalRank &&
                    stack.Quantity >= SummonRank.MergeMaterialCount;
                SetSlotState(slot, instance, mergeable);
                var card = new VisualElement();
                card.AddToClassList("bench-card");
                card.pickingMode = PickingMode.Position;

                var icon = new Image
                {
                    sprite = instance.Unit?.WorldSpriteAtRank(instance.Rank),
                    scaleMode = ScaleMode.ScaleToFit,
                    pickingMode = PickingMode.Ignore,
                };
                icon.AddToClassList("bench-card__icon");
                var name = new Label(instance.Unit == null ? "알 수 없는 유닛" : instance.Unit.DisplayName);
                name.AddToClassList("bench-card__name");
                name.pickingMode = PickingMode.Ignore;
                var rank = new Label($"Lv.{instance.Level} · {FormatRank(instance.Rank)}");
                rank.AddToClassList("bench-card__rank");
                rank.pickingMode = PickingMode.Ignore;
                var quantity = new Label($"x{stack.Quantity:N0}");
                quantity.AddToClassList("bench-card__quantity");
                quantity.pickingMode = PickingMode.Ignore;
                card.Add(icon);
                card.Add(rank);
                card.Add(name);
                card.Add(quantity);
                if (mergeable)
                {
                    var mergeBadge = new Label("합성");
                    mergeBadge.AddToClassList("bench-card__merge-badge");
                    mergeBadge.pickingMode = PickingMode.Ignore;
                    card.Add(mergeBadge);
                }
                RegisterOwnedCardInteraction(
                    card,
                    instance,
                    stack.BenchRepresentative,
                    stack.Quantity);
                slot.Add(card);
            }

            int totalUnits = 0;
            for (int i = 0; i < stacks.Count; i++)
                totalUnits += stacks[i].Quantity;
            SetBenchUsage(stacks.Count, totalUnits, capacity);
        }

        void RegisterOwnedCardInteraction(
            VisualElement card,
            SummonUnitInstance detailInstance,
            SummonUnitInstance benchInstance,
            int quantity)
        {
            Vector2 pressPosition = default;
            bool dragging = false;
            card.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                card.CapturePointer(evt.pointerId);
                pressPosition = evt.position;
                dragging = false;
                evt.StopPropagation();
            });
            card.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!card.HasPointerCapture(evt.pointerId)) return;
                if (!dragging && benchInstance != null &&
                    Vector2.Distance(pressPosition, evt.position) >= BenchDragThreshold)
                {
                    dragging = true;
                    BenchDragStarted?.Invoke(benchInstance, evt.position);
                }
                if (!dragging) return;
                BenchDragMoved?.Invoke(benchInstance, evt.position);
                evt.StopPropagation();
            });
            card.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!card.HasPointerCapture(evt.pointerId)) return;
                card.ReleasePointer(evt.pointerId);
                if (dragging)
                    BenchDragEnded?.Invoke(benchInstance, evt.position);
                else
                    BenchSlotSelected?.Invoke(detailInstance, quantity);
                dragging = false;
                evt.StopPropagation();
            });
        }

        static List<BenchStack> BuildOwnedStacks(
            IReadOnlyList<SummonUnitInstance> benchUnits,
            IReadOnlyList<SummonUnitInstance> deployedUnits)
        {
            var stacks = new List<BenchStack>();
            var stackIndices = new Dictionary<string, int>();
            AddToOwnedStacks(benchUnits, true, stacks, stackIndices);
            AddToOwnedStacks(deployedUnits, false, stacks, stackIndices);
            return stacks;
        }

        static void AddToOwnedStacks(
            IReadOnlyList<SummonUnitInstance> units,
            bool isBenchUnit,
            List<BenchStack> stacks,
            Dictionary<string, int> stackIndices)
        {
            if (units == null) return;
            foreach (var instance in units)
            {
                if (instance?.Unit == null) continue;
                string key = StackKey(instance);
                if (stackIndices.TryGetValue(key, out int index))
                {
                    stacks[index].Add(instance, isBenchUnit);
                    continue;
                }

                stackIndices.Add(key, stacks.Count);
                stacks.Add(new BenchStack(instance, isBenchUnit));
            }
        }

        static string StackKey(SummonUnitInstance instance) => $"{instance.Unit.UnitId}:{instance.Rank}";

        static string FormatRank(int rank) => SummonRank.FormatStars(rank);

        static void SetSlotState(VisualElement slot, SummonUnitInstance instance, bool mergeable)
        {
            slot.EnableInClassList("bench-slot--common", instance?.Unit?.Rarity == SummonUnitRarity.Common);
            slot.EnableInClassList("bench-slot--rare", instance?.Unit?.Rarity == SummonUnitRarity.Rare);
            slot.EnableInClassList("bench-slot--legendary", instance?.Unit?.Rarity == SummonUnitRarity.Legendary);
            slot.EnableInClassList("bench-slot--mergeable", mergeable);
        }

        sealed class BenchStack
        {
            public SummonUnitInstance Representative { get; }
            public SummonUnitInstance BenchRepresentative { get; private set; }
            public int Quantity { get; private set; }

            public BenchStack(SummonUnitInstance representative, bool isBenchUnit)
            {
                Representative = representative;
                Add(representative, isBenchUnit);
            }

            public void Add(SummonUnitInstance instance, bool isBenchUnit)
            {
                Quantity++;
                if (isBenchUnit && BenchRepresentative == null)
                    BenchRepresentative = instance;
            }
        }

        sealed class GrowthRowBinding
        {
            public VisualElement Root { get; }
            public UpgradeRowView View { get; }
            public LongPressRepeater Repeater { get; set; }
            public GrowthRowModel Model { get; set; }

            public GrowthRowBinding(VisualElement root)
            {
                Root = root;
                View = new UpgradeRowView(root);
            }
        }

        void UpdateSummonButtonState()
        {
            bool canSummon =
                _summonContracts > 0 &&
                !_isSummonAnimating &&
                _ownedUnitCount < _summonCapacity;
            _summonButton.SetEnabled(canSummon);
            _summonButton.EnableInClassList("btn--disabled", !canSummon);
        }

        void BuildBenchSlots()
        {
            if (_bench == null) return;
            for (int i = 0; i < BenchSlotCount; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("bench-slot");
                _bench.Add(slot);
                _benchSlots.Add(slot);
            }
        }

        static void BuildSlots(VisualElement container, int count)
        {
            if (container == null) return;
            for (int i = 0; i < count; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("bench-slot");
                container.Add(slot);
            }
        }

        void SyncGrowthRows(
            ScrollView list,
            Dictionary<string, GrowthRowBinding> bindings,
            IReadOnlyList<GrowthRowModel> models)
        {
            if (list == null || _upgradeRowTemplate == null) return;
            var activeKeys = new HashSet<string>();
            int count = models?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                GrowthRowModel model = models[i];
                if (string.IsNullOrWhiteSpace(model.Key)) continue;
                activeKeys.Add(model.Key);
                if (!bindings.TryGetValue(model.Key, out var binding))
                {
                    binding = new GrowthRowBinding(_upgradeRowTemplate.Instantiate());
                    bindings.Add(model.Key, binding);
                    list.Add(binding.Root);
                    binding.Repeater = new LongPressRepeater(
                        binding.View.ActionButton,
                        () => InvokeGrowthAction(binding));
                }

                binding.Model = model;
                binding.View.Bind(model.Name, model.Values, model.ActionText, model.IconClass);
                binding.View.SetAffordable(model.CanPurchase);
            }

            var staleKeys = new List<string>();
            foreach (var pair in bindings)
            {
                if (!activeKeys.Contains(pair.Key))
                    staleKeys.Add(pair.Key);
            }
            foreach (string key in staleKeys)
            {
                GrowthRowBinding binding = bindings[key];
                binding.Repeater?.Dispose();
                binding.Root.RemoveFromHierarchy();
                bindings.Remove(key);
            }
        }

        void InvokeGrowthAction(GrowthRowBinding binding)
        {
            GrowthRowModel model = binding.Model;
            if (!model.CanPurchase) return;
            RunUpgradeRequested?.Invoke(model.RunUpgradeType);
        }

        static void DisposeRows(Dictionary<string, GrowthRowBinding> rows)
        {
            foreach (var binding in rows.Values)
                binding.Repeater?.Dispose();
            rows.Clear();
        }

        static void BuildDummyRows(
            ScrollView list,
            VisualTreeAsset template,
            string[] names,
            string actionText,
            string[] iconClasses = null)
        {
            if (list == null || template == null) return;
            for (int i = 0; i < names.Length; i++)
            {
                var row = template.Instantiate();
                string iconClass = iconClasses != null && i < iconClasses.Length ? iconClasses[i] : null;
                new UpgradeRowView(row).Bind($"{names[i]} Lv.1", UIFormat.Delta(10, 12), actionText, iconClass);
                list.Add(row);
            }
        }
    }
}
