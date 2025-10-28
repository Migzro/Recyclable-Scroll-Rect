// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class ScrollAnimationController : BaseScrollAnimationController
    {
        private float _start;
        private float _target;
        private float _duration;
        private float _elapsed;

        public override void ScrollToContentPosition(float targetContentPosition, float timeOrSpeed, bool isSpeed, Action<AnimationState> onFinished)
        {
            base.ScrollToContentPosition(targetContentPosition, timeOrSpeed, isSpeed, onFinished);
            _start = _scrollRect.ContentPosition;
            _target = targetContentPosition;

            var distance = Mathf.Abs(_target - _start);
            _duration = Mathf.Max(0.0001f, isSpeed ? distance / timeOrSpeed : timeOrSpeed);
            _elapsed = 0f;
        }

        private void Update()
        {
            if (_animationState != AnimationState.Animating)
            {
                return;
            }

            _elapsed += Time.smoothDeltaTime;
            var t = Mathf.Clamp01(_elapsed / _duration);

            _scrollRect.ContentPosition = Mathf.Lerp(_start, _target, t);

            if (_elapsed >= _duration)
            {
                FinishCurrentAnimation();
            }
        }

        public override float GetAnimationRemainingTime()
        {
            if (_animationState == AnimationState.Animating)
            {
                return Mathf.Max(0, _duration - _elapsed);
            }
            return 0;
        }
    }
}
