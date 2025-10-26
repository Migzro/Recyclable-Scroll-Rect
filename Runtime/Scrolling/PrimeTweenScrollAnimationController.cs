using System;
using UnityEngine;
#if PRIMETWEEN
using PrimeTween;
#endif

namespace RecyclableScrollRect
{
#if PRIMETWEEN
    public class PrimeTweenScrollAnimationController : BaseScrollAnimationController<Ease>
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
                if (isSpeed)
                {
                    // calculate time from speed
                    time = Mathf.Abs(targetNormalizedPos - _scrollRect.normalizedPosition[_scrollRect.Axis]) / time;
                }
                Tween.Custom(
                        startValue: _scrollRect.normalizedPosition[_scrollRect.Axis],
                        endValue: targetNormalizedPos,
                        duration: time,
                        ease: ease,
                        onValueChange: val =>
                        {
                            var normalizedPosition = _scrollRect.normalizedPosition;
                            normalizedPosition[_scrollRect.Axis] = val;
                            _scrollRect.normalizedPosition = normalizedPosition;
                        })
                    .OnComplete(() => onFinished?.Invoke());
            }
        }

        public override void ScrollToItem(int itemIndex, bool callEvent = true, bool instant = false, float maxSpeedMultiplier = 1, float offset = 0)
        {
        }
    }
#endif
}