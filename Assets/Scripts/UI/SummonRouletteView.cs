using System;
using CrossDefense.Core;
using CrossDefense.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>소환 결과를 슬롯 릴처럼 보여주는 UI 연출. 결과 판정이나 보상 지급은 담당하지 않는다.</summary>
    public sealed class SummonRouletteView
    {
        const float CardWidth = 180f;
        const float CardGap = 0f;
        const int ResultLeadCardCount = 14;
        const int ResultIndexVariationCount = 4;
        const int TrailingCardCount = 6;

        readonly VisualElement _viewport;
        readonly VisualElement _strip;
        readonly VisualElement _rouletteRoot;
        readonly Label _resultLabel;
        readonly Button _skipButton;
        readonly string[] _decoyNames =
        {
            "주먹 슬라임", "물총 슬라임", "재화 보상", "불꽃 슬라임",
            "얼음 슬라임", "재화 보상", "초록 슬라임", "버프 슬라임",
            "폭발 슬라임", "빙결 슬라임"
        };

        Tween _moveTween;
        Tween _finishTween;
        Tween _hideTween;
        Action _onComplete;
        SummonResult _result;
        VisualElement _finalCard;
        int _resultCardIndex;
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
            _hideTween?.Kill();
            _strip.Clear();
            _strip.style.translate = new Translate(0f, 0f, 0f);
            _finalCard = null;
            _resultCardIndex = ResultLeadCardCount +
                Mathf.Abs(result.Id % ResultIndexVariationCount);
            if (_resultLabel != null)
                _resultLabel.text = "릴을 돌리는 중...";
            if (_skipButton != null)
                _skipButton.SetEnabled(true);

            int cardCount = _resultCardIndex + TrailingCardCount + 1;
            int decoyOffset = Mathf.Abs(result.Id * 3 % _decoyNames.Length);
            for (int i = 0; i < cardCount; i++)
            {
                bool isFinal = i == _resultCardIndex;
                string label = isFinal
                    ? GetResultName(result)
                    : _decoyNames[(decoyOffset + i) % _decoyNames.Length];
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
                .SetUpdate(true)
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

            return -(_resultCardIndex * (CardWidth + CardGap)
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
                ? DOVirtual.DelayedCall(0.05f, Complete).SetUpdate(true)
                : DOTween.To(
                    () => (Vector2)_finalCard.resolvedStyle.scale.value,
                    value => _finalCard.style.scale = new Scale(value),
                    new Vector2(1.08f, 1.08f),
                    0.12f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
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
            if (_isPlaying)
                return;
            _hideTween?.Kill();
            _hideTween = DOVirtual.DelayedCall(
                    0.35f,
                    () => _rouletteRoot?.AddToClassList("hidden"))
                .SetUpdate(true);
        }

        public void Dispose()
        {
            _moveTween?.Kill();
            _finishTween?.Kill();
            _hideTween?.Kill();
            _moveTween = null;
            _finishTween = null;
            _hideTween = null;

            bool wasPlaying = _isPlaying;
            _isPlaying = false;
            var callback = _onComplete;
            _onComplete = null;
            if (wasPlaying)
                callback?.Invoke();

            _rouletteRoot?.AddToClassList("hidden");
            if (_skipButton != null)
                _skipButton.clicked -= Skip;
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
            return result.Rank > SummonRank.MinInternalRank
                ? $"{SummonRank.FormatStars(result.Rank)} 직행 보상"
                : "★1 기본 소환";
        }

        static string GetResultSummary(SummonResult result) =>
            result.Kind == SummonResultKind.Currency
                ? $"재화 보상 +{result.CurrencyAmount} G"
                : result.Rank > SummonRank.MinInternalRank
                    ? $"대박! {result.Unit.DisplayName} {SummonRank.FormatStars(result.Rank)}"
                    : $"{result.Unit.DisplayName} ★1 획득";
    }
}
