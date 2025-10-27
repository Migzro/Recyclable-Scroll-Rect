// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
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

        public override void ScrollToContentPosition(float targetContentPosition, float timeOrSpeed, bool isSpeed, Action onFinished)
            => ScrollToContentPosition(targetContentPosition, timeOrSpeed, isSpeed, Ease.Linear, onFinished);

        public override void ScrollToContentPosition(float targetContentPosition, float timeOrSpeed, bool isSpeed, Ease ease, Action onFinished)
        {
            if (isSpeed)
            {
                // calculate time from speed
                timeOrSpeed = Mathf.Abs(targetContentPosition - _scrollRect.ContentPosition) / timeOrSpeed;
            }
            _currentTween = Tween.Custom(
                    startValue: _scrollRect.ContentPosition,
                    endValue: targetContentPosition,
                    duration: timeOrSpeed,
                    ease: ease,
                    onValueChange: val => _scrollRect.ContentPosition = val)
                .OnComplete(() => onFinished?.Invoke());
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