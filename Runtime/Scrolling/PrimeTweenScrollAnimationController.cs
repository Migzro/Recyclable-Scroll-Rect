// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
#if PRIMETWEEN
using PrimeTween;
#endif
using System;
using UnityEngine;

namespace RecyclableScrollRect
{
#if PRIMETWEEN
    public class PrimeTweenScrollAnimationController : BaseScrollAnimationController<Ease>
    {
        private Tween _currentTween;

        public override void ScrollToContentPosition(float targetContentPosition, float timeOrSpeed, bool isSpeed, Action<AnimationState> onFinished)
            => ScrollToContentPosition(targetContentPosition, timeOrSpeed, isSpeed, Ease.Linear, onFinished);

        public override void ScrollToContentPosition(float targetContentPosition, float timeOrSpeed, bool isSpeed, Ease ease, Action<AnimationState> onFinished)
        {
            base.ScrollToContentPosition(targetContentPosition, timeOrSpeed, isSpeed, onFinished);
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
                .OnComplete(FinishCurrentAnimation);
        }

        public override float GetAnimationRemainingTime()
        {
            if (_animationState == AnimationState.Animating && _currentTween.isAlive)
            {
                return _currentTween.durationTotal - _currentTween.elapsedTimeTotal;
            }
            return 0;
        }

        public override void CancelCurrentAnimation()
        {
            if (_currentTween.isAlive)
            {
                _currentTween.Stop();
            }
            base.CancelCurrentAnimation();
        }
    }
#endif
}