using System;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RecyclableScrollRect
{
    public class DoTweenScrollAnimationController : BaseScrollAnimationController
    {
#if DOTWEEN
        public Ease ease;
#endif
        
        public override void ScrollToNormalizedPosition(float targetNormalizedPos, float time, bool isSpeed, bool instant, Action onFinished)
        {
#if DOTWEEN
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
#else
            Debug.LogError("DoTween is not present in the project. Please install DoTween from the Asset Store to use DoTweenScroll.");
#endif
        }

        public override void ScrollToItem(int itemIndex, bool callEvent = true, bool instant = false, float maxSpeedMultiplier = 1, float offset = 0)
        {
#if DOTWEEN
#else
            Debug.LogError("DoTween is not present in the project. Please install DoTween from the Asset Store to use DoTweenScroll.");
#endif
        }
    }
}
