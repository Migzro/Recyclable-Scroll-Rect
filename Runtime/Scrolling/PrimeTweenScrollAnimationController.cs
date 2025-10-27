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
        private Tween _currentTween;

        public override void ScrollToNormalizedPosition(float targetNormalizedPosition, float time, bool isSpeed, bool instant, Action onFinished)
            => ScrollToNormalizedPosition(targetNormalizedPosition, time, isSpeed, instant, Ease.Linear, onFinished);

        public override void ScrollToContentPosition(float targetContentPosition, float time, bool isSpeed, bool instant, Action onFinished)
            => ScrollToContentPosition(targetContentPosition, time, isSpeed, instant, Ease.Linear, onFinished);

        public override void ScrollToNormalizedPosition(float targetNormalizedPosition, float time, bool isSpeed, bool instant, Ease ease, Action onFinished)
        {
            targetNormalizedPosition = Mathf.Clamp01(targetNormalizedPosition);
            if (instant)
            {
                _scrollRect.SetNormalizedPosition(targetNormalizedPosition);
                onFinished?.Invoke();
            }
            else
            {
                if (isSpeed)
                {
                    // calculate time from speed
                    time = Mathf.Abs(targetNormalizedPosition - _scrollRect.GetNormalizedPosition) / time;
                }

                _currentTween = Tween.Custom(
                        startValue: _scrollRect.GetNormalizedPosition,
                        endValue: targetNormalizedPosition,
                        duration: time,
                        ease: ease,
                        onValueChange: val => _scrollRect.SetNormalizedPosition(val))
                    .OnComplete(() => onFinished?.Invoke());
            }
        }

        public override void ScrollToContentPosition(float targetContentPosition, float time, bool isSpeed, bool instant, Ease ease, Action onFinished)
        {
            if (instant)
            {
                _scrollRect.SetContentPosition(targetContentPosition);
                onFinished?.Invoke();
            }
            else
            {
                if (isSpeed)
                {
                    // calculate time from speed
                    time = Mathf.Abs(targetContentPosition - _scrollRect.GetContentPosition) / time;
                }
                _currentTween = Tween.Custom(
                        startValue: _scrollRect.GetNormalizedPosition,
                        endValue: targetContentPosition,
                        duration: time,
                        ease: ease,
                        onValueChange: val => _scrollRect.SetContentPosition(val))
                    .OnComplete(() => onFinished?.Invoke());
            }
        }

        public override float StopCurrentAnimation()
        {
            if (_currentTween.isAlive)
            {
                _currentTween.Stop();
                return _currentTween.durationTotal - _currentTween.elapsedTimeTotal;
            }
            return 0;
        }
    }
#endif
}