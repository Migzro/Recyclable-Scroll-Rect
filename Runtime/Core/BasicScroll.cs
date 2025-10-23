using System;
using System.Collections;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class BasicScroll : IScroll
    {
        public void ScrollToNormalizedPosition(RSRBase scrollRect, float targetNormalizedPos, float speed, bool isTime, bool instant, Action onFinished)
        {
            if (instant)
            {
                FinishAnimation(scrollRect, targetNormalizedPos, onFinished);
            }
            else
            {
                scrollRect.StartCoroutine(ScrollToNormalizedPositionInternal(scrollRect, targetNormalizedPos, speed, isTime, onFinished));
            }
        }
        
        private IEnumerator ScrollToNormalizedPositionInternal(RSRBase scrollRect, float targetNormalizedPos, float speed, bool isTime, Action onFinished)
        {
            var normalizedPosition = scrollRect.normalizedPosition;
            var current = Mathf.Clamp01(normalizedPosition[scrollRect.Axis]);
            targetNormalizedPos = Mathf.Clamp01(targetNormalizedPos);

            if (Mathf.Abs(targetNormalizedPos - current) <= 0.001f)
            {
                FinishAnimation(scrollRect, targetNormalizedPos, onFinished);
                yield break;
            }

            if (isTime)
            {
                var start = current;
                var elapsed = 0f;
                while (elapsed < speed)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / speed);
                    var easedT = Mathf.SmoothStep(0f, 1f, t);
                    current = Mathf.Lerp(start, targetNormalizedPos, easedT);
                    normalizedPosition[scrollRect.Axis] = current;
                    scrollRect.normalizedPosition = normalizedPosition;
                    yield return null;
                }
            }
            else
            {
                while (Mathf.Abs(targetNormalizedPos - current) > 0.001f)
                {
                    current = Mathf.MoveTowards(current, targetNormalizedPos, speed * Time.deltaTime);
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
