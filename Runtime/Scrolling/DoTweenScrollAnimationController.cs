// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
#if DOTWEEN
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using DG.Tweening;
#endif
using System;

namespace RecyclableScrollRect
{
#if DOTWEEN
    public class DoTweenScrollAnimationController : BaseScrollAnimationController<Ease>
    {
        private TweenerCore<float, float, FloatOptions> _currentTween;

        public override void ScrollToContentPosition(float targetContentPosition, float timeOrSpeed, bool isSpeed, Action<AnimationState> onFinished)
            => ScrollToContentPosition(targetContentPosition, timeOrSpeed, isSpeed, Ease.Linear, onFinished);

        public override void ScrollToContentPosition(float targetContentPosition, float timeOrSpeed, bool isSpeed, Ease ease, Action<AnimationState> onFinished)
        {
            base.ScrollToContentPosition(targetContentPosition, timeOrSpeed, isSpeed, onFinished);
            _currentTween = DOTween.To(
                    () => _scrollRect.ContentPosition,
                    x => _scrollRect.ContentPosition = x,
                    targetContentPosition,
                    timeOrSpeed
                )
                .SetEase(ease).SetSpeedBased(isSpeed).OnComplete(FinishCurrentAnimation);
        }

        public override float GetAnimationRemainingTime()
        {
            if (_animationState == AnimationState.Animating && _currentTween != null && _currentTween.IsPlaying())
            {
                return _currentTween.Duration() - _currentTween.Elapsed();
            }
            return 0;
        }

        public override void CancelCurrentAnimation()
        {
            if (_currentTween != null && _currentTween.IsPlaying())
            {
                _currentTween.Kill();
            }
            base.CancelCurrentAnimation();
        }
    }
#endif
}