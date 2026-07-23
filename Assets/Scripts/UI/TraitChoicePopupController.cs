using System;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CrossDefense.UI
{
    /// <summary>소환사 영구 특성과 5웨이브 런 보상 3택을 표시하는 유일한 uGUI 팝업.</summary>
    [DisallowMultipleComponent]
    public sealed class TraitChoicePopupController : MonoBehaviour
    {
        readonly Button[] _cards = new Button[3];
        readonly Image[] _fills = new Image[3];
        readonly Outline[] _outlines = new Outline[3];
        readonly Text[] _titles = new Text[3];
        readonly Text[] _descriptions = new Text[3];

        RectTransform _selectedLabel;
        Button _confirmButton;
        Text _title;
        Text _subtitle;
        Color _selectedFill;
        Color _normalFill;
        Color _selectedOutline;
        Color _normalOutline;
        Action<int> _onConfirmed;
        int _selectedIndex;
        bool _initialized;

        public bool IsShowing => gameObject.activeSelf;

        void Awake()
        {
            Initialize();
            gameObject.SetActive(false);
        }

        public void Show(
            IReadOnlyList<PermanentTraitChoice> choices,
            int pendingChoiceCount,
            Action<PermanentTraitType> onConfirmed)
        {
            if (choices == null || choices.Count != _cards.Length)
            {
                Debug.LogError("[CrossDefense] 영구 특성 팝업에는 정확히 3개의 선택지가 필요합니다.", this);
                return;
            }

            var types = new PermanentTraitType[3];
            var names = new string[3];
            var descriptions = new string[3];
            for (int i = 0; i < choices.Count; i++)
            {
                types[i] = choices[i].Type;
                names[i] = choices[i].DisplayName;
                descriptions[i] = choices[i].Description;
            }
            ShowCards(
                "영구 특성 선택",
                $"레벨업 보상 · 남은 선택 {Mathf.Max(1, pendingChoiceCount):N0}",
                names,
                descriptions,
                index => onConfirmed?.Invoke(types[index]));
        }

        public void Show(
            IReadOnlyList<RunTraitChoice> choices,
            int clearedWave,
            Action<string> onConfirmed)
        {
            if (choices == null || choices.Count != _cards.Length)
            {
                Debug.LogError("[CrossDefense] 런 특성 팝업에는 정확히 3개의 선택지가 필요합니다.", this);
                return;
            }

            var rewardIds = new string[3];
            var names = new string[3];
            var descriptions = new string[3];
            bool awakening = true;
            for (int i = 0; i < choices.Count; i++)
            {
                rewardIds[i] = choices[i].RewardId;
                names[i] = choices[i].DisplayName;
                descriptions[i] =
                    $"{CategoryLabel(choices[i].Category)} · {choices[i].StatusLabel}\n{choices[i].Description}";
                awakening &= choices[i].Category == RunRewardCategory.Awakening;
            }
            ShowCards(
                awakening ? "소환사 속성 각성" : "웨이브 보상 선택",
                awakening
                    ? $"WAVE {Mathf.Max(1, clearedWave):N0} 클리어 · 이번 런의 주 공격 선택"
                    : $"WAVE {Mathf.Max(1, clearedWave):N0} 클리어 · 사망 시 소멸",
                names,
                descriptions,
                index => onConfirmed?.Invoke(rewardIds[index]));
        }

        static string CategoryLabel(RunRewardCategory category)
        {
            return category switch
            {
                RunRewardCategory.Awakening => "소환사 각성",
                RunRewardCategory.SummonerEvolution => "공격 진화",
                RunRewardCategory.SlimeArmy => "슬라임 군단",
                RunRewardCategory.Summon => "운명의 소환",
                _ => "런 보상",
            };
        }

        void ShowCards(
            string title,
            string subtitle,
            IReadOnlyList<string> names,
            IReadOnlyList<string> descriptions,
            Action<int> onConfirmed)
        {
            Initialize();
            if (!_initialized)
                return;

            _onConfirmed = onConfirmed;
            gameObject.SetActive(true);
            if (_title != null)
                _title.text = title;
            if (_subtitle != null)
                _subtitle.text = subtitle;
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_titles[i] != null)
                    _titles[i].text = names[i];
                if (_descriptions[i] != null)
                    _descriptions[i].text = descriptions[i];
            }
            Select(0);
        }

        public void Hide()
        {
            _onConfirmed = null;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        void Initialize()
        {
            if (_initialized) return;
            var modal = transform.Find("ChoiceModal");
            if (modal == null)
                return;

            for (int i = 0; i < _cards.Length; i++)
            {
                Transform card = modal.Find($"ChoiceCard{i + 1}");
                if (card == null)
                    return;

                _cards[i] = card.GetComponent<Button>();
                _fills[i] = card.GetComponent<Image>();
                _outlines[i] = card.GetComponent<Outline>();
                _titles[i] = card.Find("CardTitle")?.GetComponent<Text>();
                _descriptions[i] = card.Find("CardDescription")?.GetComponent<Text>();
                if (_cards[i] == null || _fills[i] == null || _outlines[i] == null)
                    return;

                int index = i;
                _cards[i].onClick.AddListener(() => Select(index));
            }

            _selectedLabel = _cards[0].transform.Find("SelectedLabel") as RectTransform;
            _selectedFill = _fills[0].color;
            _normalFill = _fills[1].color;
            _selectedOutline = _outlines[0].effectColor;
            _normalOutline = _outlines[1].effectColor;
            _confirmButton = modal.Find("ConfirmButton")?.GetComponent<Button>();
            _title = modal.Find("Title")?.GetComponent<Text>();
            _subtitle = modal.Find("Subtitle")?.GetComponent<Text>();
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(ConfirmSelection);
            _initialized = true;
        }

        void Select(int index)
        {
            if (!_initialized || index < 0 || index >= _cards.Length)
                return;
            _selectedIndex = index;

            for (int i = 0; i < _cards.Length; i++)
            {
                bool selected = i == index;
                _fills[i].color = selected ? _selectedFill : _normalFill;
                _outlines[i].effectColor = selected ? _selectedOutline : _normalOutline;
            }

            if (_selectedLabel == null)
                return;
            _selectedLabel.SetParent(_cards[index].transform, false);
            _selectedLabel.anchoredPosition = new Vector2(300f, 75f);
        }

        void ConfirmSelection()
        {
            if (!_initialized || _selectedIndex < 0 || _selectedIndex >= _cards.Length)
                return;

            int selectedIndex = _selectedIndex;
            Action<int> callback = _onConfirmed;
            Hide();
            callback?.Invoke(selectedIndex);
        }
    }
}
