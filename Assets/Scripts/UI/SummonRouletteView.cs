using System;
using CrossDefense.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>소환 결과를 슬롯 릴처럼 보여주는 UI 연출. 결과 판정이나 보상 지급은 담당하지 않는다.</summary>
    public sealed class SummonRouletteView
    {
        const float CardWidth = 180f;
        const float CardGap = 16f;
        const int FinalCardIndex = 10;

        readonly VisualElement _viewport;
        readonly VisualElement _strip;
        readonly VisualElement _rouletteRoot;
        readonly Label _resultLabel;
        readonly Button _skipButton;
        readonly string[] _decoyNames =
        {
            "고블린 정찰병", "고블린 궁수", "재화 보상", "고블린 방패병",
            "고블린 화염술사", "재화 보상", "고블린 궁수", "고블린 정찰병",
            "희귀 유닛", "재화 보상"
        };

        Tween _moveTween;
        Tween _finishTween;
        Action _onComplete;
        SummonResult _result;
        VisualElement _finalCard;
        bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        public SummonRouletteView(VisualElement root)
        {
            _rouletteRoot = root.Q<VisualElement>("summon-modal-overlay");
            _viewport = root.Q<VisualElement>("summon-modal-reel-viewport");
            _strip = root.Q<VisualElement>("summon-modal-reel-strip");
            _resultLabel = root.Q<Label>("summon-modal-result-label");
            _skipButton = root.Q<Button>("summon-modal-skip-button");
            if (_skipButton != null)
            {
                _skipButton.clicked += Skip;
                _skipButton.SetEnabled(false);
            }
        }

        public void Play(SummonResult result, Action onComplete)
        {
            if (_isPlaying || _viewport == null || _strip == null)
            {
                onComplete?.Invoke();
                return;
            }

            _isPlaying = true;
            _rouletteRoot?.RemoveFromClassList("hidden");
            _result = result;
            _onComplete = onComplete;
            _moveTween?.Kill();
            _finishTween?.Kill();
            _strip.Clear();
            _strip.style.translate = new Translate(0f, 0f, 0f);
            if (_resultLabel != null)
                _resultLabel.text = "릴을 돌리는 중...";
            if (_skipButton != null)
                _skipButton.SetEnabled(true);

            for (int i = 0; i <= FinalCardIndex; i++)
            {
                bool isFinal = i == FinalCardIndex;
                string label = isFinal ? GetResultName(result) : _decoyNames[i % _decoyNames.Length];
                string subLabel = isFinal ? GetResultSubLabel(result) : "결과 확인 중";
                var card = CreateCard(label, subLabel, isFinal);
                _strip.Add(card);
                if (isFinal)
                    _finalCard = card;
            }

            _viewport.schedule.Execute(StartMove).StartingIn(1);
        }

        void StartMove()
        {
            if (!_isPlaying) return;

            float targetX = CalculateTargetX();

            _moveTween = DOTween.To(
                    () => _strip.resolvedStyle.translate.x,
                    value => _strip.style.translate = new Translate(value, 0f, 0f),
                    targetX,
                    1.35f)
                .SetEase(Ease.OutCubic)
                .OnComplete(FinishRoll);
        }

        public void Skip()
        {
            if (!_isPlaying) return;
            _moveTween?.Kill(false);

            float targetX = CalculateTargetX();
            _strip.style.translate = new Translate(targetX, 0f, 0f);
            FinishRoll(true);
        }

        float CalculateTargetX()
        {
            float viewportWidth = _viewport.resolvedStyle.width;
            if (float.IsNaN(viewportWidth) || viewportWidth <= 0f)
                viewportWidth = CardWidth * 3f + CardGap * 2f;

            float contentWidth = viewportWidth
                - _viewport.resolvedStyle.paddingLeft
                - _viewport.resolvedStyle.paddingRight;
            if (float.IsNaN(contentWidth) || contentWidth <= 0f)
                contentWidth = viewportWidth;

            return -(FinalCardIndex * (CardWidth + CardGap)
                - (contentWidth - CardWidth) * 0.5f);
        }

        void FinishRoll()
        {
            FinishRoll(false);
        }

        void FinishRoll(bool immediate)
        {
            if (!_isPlaying) return;
            if (_resultLabel != null)
                _resultLabel.text = GetResultSummary(_result);
            if (_skipButton != null)
                _skipButton.SetEnabled(false);

            if (immediate)
            {
                Complete();
                return;
            }

            _finishTween = _finalCard == null
                ? DOVirtual.DelayedCall(0.05f, Complete)
                : DOTween.To(
                    () => (Vector2)_finalCard.resolvedStyle.scale.value,
                    value => _finalCard.style.scale = new Scale(value),
                    new Vector2(1.08f, 1.08f),
                    0.12f)
                    .SetEase(Ease.OutBack)
                    .SetLoops(2, LoopType.Yoyo)
                    .OnComplete(Complete);
        }

        void Complete()
        {
            if (!_isPlaying) return;
            _isPlaying = false;
            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
            DOVirtual.DelayedCall(0.35f, () => _rouletteRoot?.AddToClassList("hidden"));
        }

        static VisualElement CreateCard(string title, string subtitle, bool final)
        {
            var card = new VisualElement();
            card.AddToClassList("summon-reel-card");
            if (final)
                card.AddToClassList("summon-reel-card--final");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("summon-reel-card__title");
            titleLabel.pickingMode = PickingMode.Ignore;
            card.Add(titleLabel);

            var subtitleLabel = new Label(subtitle);
            subtitleLabel.AddToClassList("summon-reel-card__subtitle");
            subtitleLabel.pickingMode = PickingMode.Ignore;
            card.Add(subtitleLabel);
            return card;
        }

        static string GetResultName(SummonResult result) =>
            result.Kind == SummonResultKind.Currency ? "재화 보상" : result.Unit?.DisplayName ?? "알 수 없는 유닛";

        static string GetResultSubLabel(SummonResult result)
        {
            if (result.Kind == SummonResultKind.Currency)
                return $"+{result.CurrencyAmount} G";
            return result.IsJackpot ? "★1 직행 잭팟" : "기본 유닛";
        }

        static string GetResultSummary(SummonResult result) =>
            result.Kind == SummonResultKind.Currency
                ? $"재화 보상 +{result.CurrencyAmount} G"
                : result.IsJackpot
                    ? $"잭팟! {result.Unit.DisplayName} ★1"
                    : $"{result.Unit.DisplayName} 기본 유닛 획득";
    }
}
