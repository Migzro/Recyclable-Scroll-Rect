using System;
using System.Collections;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class BasicScroll : IScroll
    {
        private readonly int _axis;

        public BasicScroll(int axis)
        {
            _axis = axis;
        }
        
        public void ScrollToNormalizedPosition(RSRBase scrollRect, float targetNormalizedPos, float speed, bool isTime, bool instant, Action onFinished)
        {
            if (instant)
            {
                var normalizedPosition = scrollRect.normalizedPosition;
                normalizedPosition[_axis] = Mathf.Clamp01(targetNormalizedPos);
                scrollRect.normalizedPosition = normalizedPosition;
                onFinished?.Invoke();
            }
            else
            {
                scrollRect.StartCoroutine(ScrollToNormalizedPositionInternal(scrollRect, targetNormalizedPos, speed, isTime, onFinished));
            }
        }
        
        private IEnumerator ScrollToNormalizedPositionInternal(RSRBase scrollRect, float targetNormalizedPos, float speed, bool isTime, Action onFinished)
        {
            var normalizedPosition = scrollRect.normalizedPosition;
            var current = Mathf.Clamp01(normalizedPosition[_axis]);
            targetNormalizedPos = Mathf.Clamp01(targetNormalizedPos);

            if (Mathf.Abs(targetNormalizedPos - current) <= 0.001f)
            {
                normalizedPosition[_axis] = targetNormalizedPos;
                scrollRect.normalizedPosition = normalizedPosition;
                onFinished?.Invoke();
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
                    normalizedPosition[_axis] = current;
                    scrollRect.normalizedPosition = normalizedPosition;
                    yield return null;
                }
            }
            else
            {
                while (Mathf.Abs(targetNormalizedPos - current) > 0.001f)
                {
                    current = Mathf.MoveTowards(current, targetNormalizedPos, speed * Time.deltaTime);
                    normalizedPosition[_axis] = current;
                    scrollRect.normalizedPosition = normalizedPosition;
                    yield return null;
                }
            }

            normalizedPosition[_axis] = targetNormalizedPos;
            scrollRect.normalizedPosition = normalizedPosition;
            onFinished?.Invoke();
        }

        public void ScrollToItem(int itemIndex, bool callEvent = true, bool instant = false, float maxSpeedMultiplier = 1, float offset = 0)
        {
        }
    }
}
