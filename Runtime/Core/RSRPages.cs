// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;
using UnityEngine.EventSystems;

namespace RecyclableScrollRect
{
    public class RSRPages : RSR
    {
        [SerializeField] private float _scrollingDuration = 0.15f;
        [SerializeField] protected float _swipeThreshold = 200;

        private IPageDataSource _pageDataSource;
        private int _currentPage;
        private bool _isDragging;

        protected override void Initialize()
        {
            _pageDataSource = (IPageDataSource)_dataSource;
            _currentPage = 0;
            base.Initialize();
        }

        /// <summary>
        /// Used to refresh needed data if is in paged mode
        /// Focuses first item if a new item was added
        /// Scrolls to new page if currentPage page was deleted
        /// </summary>
        /// <param name="reloadAllItems"></param>
        protected override void RefreshAfterReload(bool reloadAllItems)
        {
            base.RefreshAfterReload(reloadAllItems);
            
            if (_currentPage >= _itemsCount)
            {
                // scroll item will handle the focus
                ScrollToItemIndex(Mathf.Max(0, _currentPage - 1), instant:true);
            }
            else  if (_itemsCount > 0 && _visibleItems.TryGetValue(_currentPage, out var visibleItem))
            {
                _pageDataSource?.PageWillFocus(_currentPage, true, visibleItem.item);
            }
        }
        
        protected override void PerformPreScrollingActions(int itemIndex)
        {
            base.PerformPreScrollingActions(itemIndex);
            
            var isNextPage = itemIndex > _currentPage;
            if (_visibleItems.TryGetValue(_currentPage, out var visibleItem))
            {
                _pageDataSource?.PageWillUnFocus(_currentPage, isNextPage, visibleItem.item);
            }
        }

        protected override void PerformPostScrollingActions(bool callEvent, AnimationState animationState, int itemIndex = -1)
        {
            base.PerformPostScrollingActions(callEvent, animationState, itemIndex);
            
            if (animationState == AnimationState.Finished && _currentPage != itemIndex)
            {
                var isNextPage = itemIndex > _currentPage;
                _currentPage = itemIndex;
                if (_visibleItems.TryGetValue(_currentPage, out var visibleItem))
                {
                    _pageDataSource?.PageWillFocus(_currentPage, isNextPage, visibleItem.item);
                }
            }
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (_isAnimating)
            {
                return;
            }
            base.OnBeginDrag(eventData);
            _isDragging = true;
            _dragStartingPosition = content.anchoredPosition * (vertical ? 1 : -1);
        }
        
        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            if (!_isDragging)
            {
                return;
            }
            _isDragging = false;
            var newPageIndex = CalculateNextPageAfterDrag();
            ScrollToItemIndex(newPageIndex, _scrollingDuration);
        }

        protected virtual int CalculateNextPageAfterDrag()
        {
            var currentContentPosition = content.anchoredPosition * (vertical ? 1 : -1);
            var distance = Vector3.Distance(_dragStartingPosition, currentContentPosition);
            var isNextPage = currentContentPosition[_axis] > _dragStartingPosition[_axis];
            var newPage = _currentPage;
            if (distance > _swipeThreshold)
            {
                if (isNextPage && _currentPage < _itemsCount - 1)
                    newPage++;
                else if (!isNextPage && _currentPage > 0)
                    newPage--;
            }
            
            return newPage;
        }
        
#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.UpArrow))
            {
                ScrollToItemIndex(Mathf.Max(_currentPage - 1, 0), _scrollingDuration);
            }
            else if (Input.GetKeyUp( KeyCode.DownArrow))
            {
                ScrollToItemIndex(Mathf.Min(_currentPage + 1, _itemsCount - 1), _scrollingDuration);
            }
        }
#endif
    }
}