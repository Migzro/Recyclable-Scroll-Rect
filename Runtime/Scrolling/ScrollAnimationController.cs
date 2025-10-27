// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class ScrollAnimationController : BaseScrollAnimationController
    {
        private bool _animating;
        private float _start;
        private float _target;
        private float _duration;
        private float _elapsed;
        private Action _onFinished;

        public override void ScrollToContentPosition(float targetContentPosition, float timeOrSpeed, bool isSpeed, Action onFinished)
        {
            _onFinished = onFinished;
            _start = _scrollRect.ContentPosition;
            _target = targetContentPosition;

            var distance = Mathf.Abs(_target - _start);
            _duration = Mathf.Max(0.0001f, isSpeed ? distance / timeOrSpeed : timeOrSpeed);
            
            _elapsed = 0f;
            _animating = true;

            Debug.LogError($"[ScrollAnimationController] Animation started (target={_target}, duration={_duration:0.00}s)");
        }

        private void LateUpdate()
        {
            if (!_animating)
            {
                return;
            }

            _elapsed += Time.smoothDeltaTime;
            var t = Mathf.Clamp01(_elapsed / _duration);

            _scrollRect.ContentPosition = Mathf.Lerp(_start, _target, t);

            if (_elapsed >= _duration)
            {
                _scrollRect.ContentPosition = _target;
                StopCurrentAnimation();
            }
        }

        public override float StopCurrentAnimation()
        {
            if (!_animating)
            {
                return 0f;
            }

            Debug.LogError($"[ScrollAnimationController] Animation ended after {_elapsed:0.00}s");
            _animating = false;
            _onFinished?.Invoke();

            return _duration - _elapsed;
        }
    }
}
