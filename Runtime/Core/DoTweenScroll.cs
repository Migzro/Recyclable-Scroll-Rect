using System;
using DG.Tweening;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class DoTweenScroll : MonoBehaviour, IScroll
    {
#if DOTWEEN
        public Ease ease;
#endif
        
        public void ScrollToNormalizedPosition(RSRBase scrollRect, float targetNormalizedPos, float speed, bool isTime, bool instant, Action onFinished)
        {
#if DOTWEEN
            var _axis = scrollRect.vertical ? 1 : 0;
            targetNormalizedPos = Mathf.Clamp01(targetNormalizedPos);
            if (instant)
            {
                var normalizedPosition = scrollRect.normalizedPosition;
                normalizedPosition[_axis] = Mathf.Clamp01(targetNormalizedPos);
                scrollRect.normalizedPosition = normalizedPosition;
                onFinished?.Invoke();
            }
            else
            {
                DOTween.To(
                        () => scrollRect.normalizedPosition[_axis],
                        x =>
                        {
                            var normalizedPosition = scrollRect.normalizedPosition;
                            normalizedPosition[_axis] = x;
                            scrollRect.normalizedPosition = normalizedPosition;
                        },
                        targetNormalizedPos,
                        speed
                    )
                    .SetEase(ease).SetSpeedBased(!isTime).OnComplete(() => { onFinished?.Invoke(); });
            }
#else
            Debug.LogError("DoTween is not present in the project. Please install DoTween from the Asset Store to use DoTweenScroll.");
#endif
        }

        public void ScrollToItem(int itemIndex, bool callEvent = true, bool instant = false, float maxSpeedMultiplier = 1, float offset = 0)
        {
#if DOTWEEN
#else
            Debug.LogError("DoTween is not present in the project. Please install DoTween from the Asset Store to use DoTweenScroll.");
#endif
        }
    }
}
