// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;

namespace RecyclableScrollRect
{
    public class DemoScrollingHelper : MonoBehaviour
    {
        [SerializeField] private RSR _scrollRect;
        [SerializeField] private int _itemToScrollTo;
        [SerializeField] private float _timeToScroll;
        [SerializeField] private float _targetNormalizedPosition;
        [SerializeField] private bool _isSpeed;
        [SerializeField] private bool _isInstant;
        [SerializeField] private bool _callEvent;

        private void Start()
        {
            Invoke(nameof(ScrollToItem), 2);
        }

        [ContextMenu(nameof(ScrollToNormalizedPosition))]
        public void ScrollToNormalizedPosition()
        {
            _scrollRect.ScrollToNormalisedPosition(_targetNormalizedPosition, _timeToScroll, _isSpeed, _isInstant);
        }
        
        [ContextMenu(nameof(ScrollToTopRight))]
        public void ScrollToTopRight()
        {
            _scrollRect.ScrollToTopRight(_timeToScroll, _isSpeed, _isInstant);
        }
        
        [ContextMenu(nameof(ScrollToItem))]
        public void ScrollToItem()
        {
            _scrollRect.ScrollToItemIndex(_itemToScrollTo, _timeToScroll, _isSpeed, _isInstant, _callEvent);
        }
    }
}