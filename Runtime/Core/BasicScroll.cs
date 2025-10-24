using System;
using System.Collections;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class BasicScroll : IScroll
    {
        public void ScrollToNormalizedPosition(RSRBase scrollRect, float targetNormalizedPos, float time, bool isSpeed, bool instant, Action onFinished)
        {
            if (instant)
            {
                FinishAnimation(scrollRect, targetNormalizedPos, onFinished);
            }
            else
            {
                scrollRect.StartCoroutine(ScrollToNormalizedPositionInternal(scrollRect, targetNormalizedPos, time, isSpeed, onFinished));
            }
        }
        
        private IEnumerator ScrollToNormalizedPositionInternal(RSRBase scrollRect, float targetNormalizedPos, float time, bool isSpeed, Action onFinished)
        {
            var normalizedPosition = scrollRect.normalizedPosition;
            var current = Mathf.Clamp01(normalizedPosition[scrollRect.Axis]);
            targetNormalizedPos = Mathf.Clamp01(targetNormalizedPos);

            if (Mathf.Abs(targetNormalizedPos - current) <= 0.001f)
            {
                FinishAnimation(scrollRect, targetNormalizedPos, onFinished);
                yield break;
            }

            if (isSpeed)
            {
                while (Mathf.Abs(targetNormalizedPos - current) > 0.001f)
                {
                    current = Mathf.MoveTowards(current, targetNormalizedPos, time * Time.deltaTime);
                    normalizedPosition[scrollRect.Axis] = current;
                    scrollRect.normalizedPosition = normalizedPosition;
                    yield return null;
                }
            }
            else
            {
                var start = current;
                var elapsed = 0f;
                while (elapsed < time)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / time);
                    var easedT = Mathf.SmoothStep(0f, 1f, t);
                    current = Mathf.Lerp(start, targetNormalizedPos, easedT);
                    normalizedPosition[scrollRect.Axis] = current;
                    scrollRect.normalizedPosition = normalizedPosition;
                    yield return null;
                }
            }

            FinishAnimation(scrollRect, targetNormalizedPos, onFinished);
        }
        
        private void FinishAnimation(RSRBase scrollRect, float targetNormalizedPos, Action onFinished)
        {
            var normalizedPosition = scrollRect.normalizedPosition;
            normalizedPosition[scrollRect.Axis] = Mathf.Clamp01(targetNormalizedPos);
            scrollRect.normalizedPosition = normalizedPosition;
            onFinished?.Invoke();
        }

        public void ScrollToItem(int itemIndex, bool callEvent = true, bool instant = false, float maxSpeedMultiplier = 1, float offset = 0)
        {
        }
    }
}
