using System;
using System.Collections;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class ScrollAnimationController : BaseScrollAnimationController
    {
        private bool _stopAnimation;
        private float _timeLeft;
        
        public override void ScrollToNormalizedPosition(float targetNormalizedPosition, float time, bool isSpeed, bool instant, Action onFinished)
        {
            if (instant)
            {
                FinishNormalizedAnimation(targetNormalizedPosition, onFinished);
            }
            else
            {
                StartCoroutine(ScrollToNormalizedPositionInternal(targetNormalizedPosition, time, isSpeed, onFinished));
            }
        }

        public override void ScrollToContentPosition(float targetContentPosition, float time, bool isSpeed, bool instant, Action onFinished)
        {
            if (instant)
            {
                FinishContentAnimation(targetContentPosition, onFinished);
            }
            else
            {
                StartCoroutine(ScrollToContentPositionInternal(targetContentPosition, time, isSpeed, onFinished));
            }
        }

        private IEnumerator ScrollToNormalizedPositionInternal(float targetNormalizedPosition, float time, bool isSpeed, Action onFinished)
        {
            var current = _scrollRect.GetNormalizedPosition;
            var distance = Mathf.Abs(targetNormalizedPosition - current);
            var duration = isSpeed ? (distance / Mathf.Max(time, 0.0001f)) : time;
            var start = current;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                if (_stopAnimation)
                {
                    _stopAnimation = false;
                    yield break;
                }

                elapsed += Time.deltaTime;
                _timeLeft = duration - elapsed;
                var t = Mathf.Clamp01(elapsed / duration);
                var easedT = isSpeed ? t : Mathf.SmoothStep(0f, 1f, t);
                current = Mathf.Lerp(start, targetNormalizedPosition, easedT);
                _scrollRect.SetNormalizedPosition(current);
                yield return null;
            }

            FinishNormalizedAnimation(targetNormalizedPosition, onFinished);
        }
        
        private IEnumerator ScrollToContentPositionInternal(float targetContentPosition, float time, bool isSpeed, Action onFinished)
        {
            var current = _scrollRect.GetContentPosition;
            var distance = Mathf.Abs(targetContentPosition - current);
            var duration = isSpeed ? (distance / Mathf.Max(time, 0.0001f)) : time;
            var start = current;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                if (_stopAnimation)
                {
                    _stopAnimation = false;
                    yield break;
                }

                elapsed += Time.deltaTime;
                _timeLeft = duration - elapsed;
                var t = Mathf.Clamp01(elapsed / duration);
                var easedT = isSpeed ? t : Mathf.SmoothStep(0f, 1f, t);
                current = Mathf.Lerp(start, targetContentPosition, easedT);
                _scrollRect.SetContentPosition(current);
                yield return null;
            }

            FinishContentAnimation(targetContentPosition, onFinished);
        }
        
        public override float StopCurrentAnimation()
        {
            _stopAnimation = true;
            return _timeLeft;
        }
        
        private void FinishNormalizedAnimation(float targetNormalizedPosition, Action onFinished)
        {
            _scrollRect.SetNormalizedPosition(targetNormalizedPosition);
            onFinished?.Invoke();
        }
        
        private void FinishContentAnimation(float targetContentPosition, Action onFinished)
        {
            _scrollRect.SetContentPosition(targetContentPosition);
            onFinished?.Invoke();
        }
    }
}
