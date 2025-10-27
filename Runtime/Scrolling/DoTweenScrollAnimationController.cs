using System;
using UnityEngine;
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
                _currentTween = DOTween.To(
                        () => _scrollRect.GetNormalizedPosition,
                        x => _scrollRect.SetNormalizedPosition(x),
                        targetNormalizedPosition,
                        time
                    )
                    .SetEase(ease).SetSpeedBased(isSpeed).OnComplete(() => { onFinished?.Invoke(); });
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
                _currentTween = DOTween.To(
                        () => _scrollRect.GetContentPosition,
                        x => _scrollRect.SetContentPosition(x),
                        targetContentPosition,
                        time
                    )
                    .SetEase(ease).SetSpeedBased(isSpeed).OnComplete(() => { onFinished?.Invoke(); });
            }
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
