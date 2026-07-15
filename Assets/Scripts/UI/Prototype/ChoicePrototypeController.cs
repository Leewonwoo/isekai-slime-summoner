using UnityEngine;
using UnityEngine.UI;

namespace CrossDefense.UI.Prototype
{
    /// <summary>
    /// uGUI 3택 화면의 선택 감각만 검증하는 임시 프로토타입 컨트롤러.
    /// 실제 보상 지급이나 게임 상태 변경은 담당하지 않는다.
    /// </summary>
    public sealed class ChoicePrototypeController : MonoBehaviour
    {
        readonly Button[] _cards = new Button[3];
        readonly Image[] _fills = new Image[3];
        readonly Outline[] _outlines = new Outline[3];

        RectTransform _selectedLabel;
        Color _selectedFill;
        Color _normalFill;
        Color _selectedOutline;
        Color _normalOutline;
        int _selectedIndex;

        void Awake()
        {
            var modal = transform.Find("ChoiceModal");
            if (modal == null)
                return;

            for (var i = 0; i < _cards.Length; i++)
            {
                var card = modal.Find($"ChoiceCard{i + 1}");
                if (card == null)
                    return;

                _cards[i] = card.GetComponent<Button>();
                _fills[i] = card.GetComponent<Image>();
                _outlines[i] = card.GetComponent<Outline>();

                var index = i;
                _cards[i].onClick.AddListener(() => Select(index));
            }

            _selectedLabel = _cards[0].transform.Find("SelectedLabel") as RectTransform;
            _selectedFill = _fills[0].color;
            _normalFill = _fills[1].color;
            _selectedOutline = _outlines[0].effectColor;
            _normalOutline = _outlines[1].effectColor;

            var confirm = modal.Find("ConfirmButton")?.GetComponent<Button>();
            if (confirm != null)
                confirm.onClick.AddListener(ConfirmSelection);

            Select(0);
        }

        void Select(int index)
        {
            _selectedIndex = index;

            for (var i = 0; i < _cards.Length; i++)
            {
                var selected = i == index;
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
            Debug.Log($"[ChoicePrototype] Selected choice {_selectedIndex + 1}");
        }
    }
}
