using System;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RecyclableScrollRect
{
#if DOTWEEN
    public class DoTweenScrollAnimationController : BaseScrollAnimationController<Ease>
    {
        public override void ScrollToNormalizedPosition(float targetNormalizedPos, float time, bool isSpeed, bool instant, Action onFinished)
            => ScrollToNormalizedPosition(targetNormalizedPos, time, isSpeed, instant, Ease.Linear, onFinished);
        
        public override void ScrollToNormalizedPosition(float targetNormalizedPos, float time, bool isSpeed, bool instant, Ease ease, Action onFinished)
        {
            targetNormalizedPos = Mathf.Clamp01(targetNormalizedPos);
            if (instant)
            {
                var normalizedPosition = _scrollRect.normalizedPosition;
                normalizedPosition[_scrollRect.Axis] = Mathf.Clamp01(targetNormalizedPos);
                _scrollRect.normalizedPosition = normalizedPosition;
                onFinished?.Invoke();
            }
            else
            {
                DOTween.To(
                        () => _scrollRect.normalizedPosition[_scrollRect.Axis],
                        x =>
                        {
                            var normalizedPosition = _scrollRect.normalizedPosition;
                            normalizedPosition[_scrollRect.Axis] = x;
                            _scrollRect.normalizedPosition = normalizedPosition;
                        },
                        targetNormalizedPos,
                        time
                    )
                    .SetEase(ease).SetSpeedBased(isSpeed).OnComplete(() => { onFinished?.Invoke(); });
            }
        }

        public override void ScrollToItem(int itemIndex, bool callEvent = true, bool instant = false, float maxSpeedMultiplier = 1, float offset = 0)
        {
        }
    }
#endif
}
