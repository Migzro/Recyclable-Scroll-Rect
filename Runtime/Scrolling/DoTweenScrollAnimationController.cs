// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
#if DOTWEEN
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using DG.Tweening;
#endif

namespace RecyclableScrollRect
{
#if DOTWEEN
    public class DoTweenScrollAnimationController : BaseScrollAnimationController<Ease>
    {
        private TweenerCore<float, float, FloatOptions> _currentTween;

        public override void ScrollToContentPosition(float targetContentPosition, float timeOrSpeed, bool isSpeed, Action onFinished)
            => ScrollToContentPosition(targetContentPosition, timeOrSpeed, isSpeed, Ease.Linear, onFinished);

        public override void ScrollToContentPosition(float targetContentPosition, float timeOrSpeed, bool isSpeed, Ease ease, Action onFinished)
        {
            _currentTween = DOTween.To(
                    () => _scrollRect.ContentPosition,
                    x => _scrollRect.ContentPosition = x,
                    targetContentPosition,
                    timeOrSpeed
                )
                .SetEase(ease).SetSpeedBased(isSpeed).OnComplete(() => { onFinished?.Invoke(); });
        }
        
        public override float StopCurrentAnimation()
        {
            if (_currentTween.IsPlaying())
            {
                _currentTween.Kill();
                return _currentTween.Duration() - _currentTween.Elapsed();
            }
            return 0;
        }
    }
#endif
}