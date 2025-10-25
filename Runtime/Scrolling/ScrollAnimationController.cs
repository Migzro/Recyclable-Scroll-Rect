using System;
using System.Collections;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class ScrollAnimationController : BaseScrollAnimationController
    {
        public override void ScrollToNormalizedPosition(float targetNormalizedPos, float time, bool isSpeed, bool instant, Action onFinished)
        {
            if (instant)
            {
                FinishAnimation(targetNormalizedPos, onFinished);
            }
            else
            {
                StartCoroutine(ScrollToNormalizedPositionInternal(targetNormalizedPos, time, isSpeed, onFinished));
            }
        }
        
        private IEnumerator ScrollToNormalizedPositionInternal(float targetNormalizedPos, float time, bool isSpeed, Action onFinished)
        {
            var normalizedPosition = _scrollRect.normalizedPosition;
            var current = Mathf.Clamp01(normalizedPosition[_scrollRect.Axis]);
            targetNormalizedPos = Mathf.Clamp01(targetNormalizedPos);

            if (Mathf.Abs(targetNormalizedPos - current) <= 0.001f)
            {
                FinishAnimation(targetNormalizedPos, onFinished);
                yield break;
            }

            if (isSpeed)
            {
                while (Mathf.Abs(targetNormalizedPos - current) > 0.001f)
                {
                    current = Mathf.MoveTowards(current, targetNormalizedPos, time * Time.deltaTime);
                    normalizedPosition[_scrollRect.Axis] = current;
                    _scrollRect.normalizedPosition = normalizedPosition;
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
                    normalizedPosition[_scrollRect.Axis] = current;
                    _scrollRect.normalizedPosition = normalizedPosition;
                    yield return null;
                }
            }

            FinishAnimation(targetNormalizedPos, onFinished);
        }
        
        private void FinishAnimation(float targetNormalizedPos, Action onFinished)
        {
            var normalizedPosition = _scrollRect.normalizedPosition;
            normalizedPosition[_scrollRect.Axis] = Mathf.Clamp01(targetNormalizedPos);
            _scrollRect.normalizedPosition = normalizedPosition;
            onFinished?.Invoke();
        }

        public override void ScrollToItem(int itemIndex, bool callEvent = true, bool instant = false, float maxSpeedMultiplier = 1, float offset = 0)
        {
        }
    }
}
