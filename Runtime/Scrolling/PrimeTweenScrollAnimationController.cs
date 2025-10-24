using System;
using UnityEngine;
#if PRIMETWEEN
using PrimeTween;
#endif

namespace RecyclableScrollRect
{
    public class PrimeTweenScrollAnimationController : MonoBehaviour, IScrollAnimationController
    {
#if PRIMETWEEN
        public Ease ease;
#endif

        public void ScrollToNormalizedPosition(RSRBase scrollRect, float targetNormalizedPos, float time, bool isSpeed, bool instant, Action onFinished)
        {
#if PRIMETWEEN
            targetNormalizedPos = Mathf.Clamp01(targetNormalizedPos);
            if (instant)
            {
                var normalizedPosition = scrollRect.normalizedPosition;
                normalizedPosition[scrollRect.Axis] = Mathf.Clamp01(targetNormalizedPos);
                scrollRect.normalizedPosition = normalizedPosition;
                onFinished?.Invoke();
            }
            else
            {
                if (isSpeed)
                {
                    // calculate time from speed
                    time = Mathf.Abs(targetNormalizedPos - scrollRect.normalizedPosition[scrollRect.Axis]) / time;
                }
                Tween.Custom(
                        startValue: scrollRect.normalizedPosition[scrollRect.Axis],
                        endValue: targetNormalizedPos,
                        duration: time,
                        ease: ease,
                        onValueChange: val =>
                        {
                            var normalizedPosition = scrollRect.normalizedPosition;
                            normalizedPosition[scrollRect.Axis] = val;
                            scrollRect.normalizedPosition = normalizedPosition;
                        })
                    .OnComplete(() => onFinished?.Invoke());
            }
#else
            Debug.LogError("Prime Tween is not present in the project. Please install Prime Tween to use PrimeTweenScroll.");
#endif
        }

        public void ScrollToItem(int itemIndex, bool callEvent = true, bool instant = false, float maxSpeedMultiplier = 1, float offset = 0)
        {
#if PRIMETWEEN
#else
            Debug.LogError("Prime Tween is not present in the project. Please install Prime Tween to use PrimeTweenScroll.");
#endif
        }
    }
}