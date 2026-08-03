// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RecyclableScrollRect
{
    public class RSRPages : RSR
    {
        [SerializeField] private float _scrollingDuration = 0.15f;
        [SerializeField] protected float _swipeThreshold = 200;

        private IPageDataSource _pageDataSource;
        private List<int> _pageMap;
        private int _currentPage;
        private bool _isDragging;
        private int _pendingPageAfterScroll = -1;
        private bool _focusPageAfterReload;

        private int TotalPages => _pageMap?.Count ?? 0;
        private int CurrentPageDataIndex => _pageMap[_currentPage];
        private int ClampPage(int page) => Mathf.Clamp(page, 0, TotalPages - 1);

        protected override void BuildItemsMap()
        {
            base.BuildItemsMap();
            BuildPageMap();
        }
        
        private void BuildPageMap()
        {
            _pageMap ??= new List<int>();
            _pageMap.Clear();
            for (var i = 0; i < _itemData.Count; i++)
            {
                if (_itemData[i].itemType == ItemType.Item)
                {
                    _pageMap.Add(i);
                }
            }
        }

        protected override void Initialize()
        {
            _pageDataSource = (IPageDataSource)_dataSource;
            _currentPage = 0;
            base.Initialize();
            
            if (TotalPages > 0 && _visibleItems.TryGetValue(CurrentPageDataIndex, out var visibleItem))
            {
                _pageDataSource?.PageWillFocus(visibleItem.item, _itemData[CurrentPageDataIndex], true);
            }
        }

        /// <summary>
        /// Called after ReloadData — clamps the current page to the new page count
        /// and re-fires the focus callback for whichever page we land on.
        /// </summary>
        protected override void RefreshAfterReload()
        {
            base.RefreshAfterReload();

            _pendingPageAfterScroll = -1;
            _focusPageAfterReload = false;

            if (TotalPages == 0)
            {
                _currentPage = 0;
                return;
            }

            _currentPage = ClampPage(_currentPage);
            var dataIndex = CurrentPageDataIndex;
            if (_visibleItems.TryGetValue(dataIndex, out var visibleItem))
            {
                _pageDataSource?.PageWillFocus(visibleItem.item, _itemData[dataIndex], true);
            }
            else
            {
                _focusPageAfterReload = true;
                ScrollToItemIndex(_currentPage, instant: true);
            }
        }
                
        public override void ScrollToItemIndex(int pageNumber, float timeOrSpeed = -1, bool isSpeed = false, bool instant = false, bool callEvent = false, object ease = null)
        {
            if (_pageMap == null || _pageMap.Count == 0)
                return;

            var clampedPage = ClampPage(pageNumber);
            var dataIndex = _pageMap[clampedPage];

            // walk back to include any headers/footers sitting above this page
            var scrollTarget = dataIndex;
            for (var i = dataIndex - 1; i >= 0; i--)
            {
                var itemType = _itemData[i].itemType;
                var isHeader = itemType is ItemType.Header or ItemType.RSRHeader;

                if (!isHeader)
                    break;

                scrollTarget = i;
            }

            _pendingPageAfterScroll = clampedPage;
            base.ScrollToItemIndex(scrollTarget, timeOrSpeed, isSpeed, instant, callEvent, ease);
        }

        protected override float GetScrollToItemOffset(int itemIndex)
        {
            if (_sectionsSource == null)
                return 0f;

            var offset = 0f;
            var itemData = _itemData[itemIndex];

            if (_sectionsSource.ScrollRectHasHeader &&
                _sectionsSource.ScrollRectHeaderIsPinned &&
                itemData.itemType != ItemType.RSRHeader)
            {
                offset += GetItemAxisSize(0) + _spacing[_axis];
            }

            if (itemData.itemType == ItemType.Item &&
                _sectionsSource.SectionHasHeader(itemData.sectionIndex) &&
                _sectionsSource.HeaderIsPinned(itemData.sectionIndex))
            {
                var headerIndex = GetHeaderIndexFromSection(itemData.sectionIndex);
                if (headerIndex >= 0)
                    offset += GetItemAxisSize(headerIndex) + _spacing[_axis];
            }

            return offset;
        }
        
        protected override void PerformPreScrollingActions(int itemIndex)
        {
            var targetPage = _pendingPageAfterScroll != -1 ? _pendingPageAfterScroll : _pageMap.IndexOf(itemIndex);
            base.PerformPreScrollingActions(itemIndex);
            
            if (targetPage == -1 || targetPage == _currentPage)
                return;

            var isNextPage = targetPage > _currentPage;
            if (_visibleItems.TryGetValue(CurrentPageDataIndex, out var visibleItem))
                _pageDataSource?.PageWillUnFocus(visibleItem.item, _itemData[CurrentPageDataIndex], isNextPage);
        }

        protected override void PerformPostScrollingActions(bool callEvent, AnimationState animationState, bool scrollingDown, int itemIndex = -1)
        {
            base.PerformPostScrollingActions(callEvent, animationState, scrollingDown, itemIndex);
            
            if (animationState != AnimationState.Finished || _pendingPageAfterScroll == -1)
                return;

            var newPageIndex = _pendingPageAfterScroll;
            _pendingPageAfterScroll = -1;

            var pageChanged = newPageIndex != _currentPage;
            var shouldFocus = pageChanged || _focusPageAfterReload;
            var isNextPage = newPageIndex >= _currentPage;
            _currentPage = newPageIndex;
            _focusPageAfterReload = false;

            if (shouldFocus && _visibleItems.TryGetValue(CurrentPageDataIndex, out var visibleItem))
                _pageDataSource?.PageWillFocus(visibleItem.item, _itemData[CurrentPageDataIndex], isNextPage);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (_isAnimating)
                return;

            base.OnBeginDrag(eventData);
            _isDragging = true;
            _dragStartingPosition = content.anchoredPosition * (vertical ? 1 : -1);
        }
        
        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            if (!_isDragging)
                return;

            _isDragging = false;
            ScrollToItemIndex(CalculateNextPageAfterDrag(), _scrollingDuration);
        }

        protected virtual int CalculateNextPageAfterDrag()
        {
            if (TotalPages == 0)
                return _currentPage;

            var currentContentPosition = content.anchoredPosition * (vertical ? 1 : -1);
            var delta = currentContentPosition[_axis] - _dragStartingPosition[_axis];
            var distance = Mathf.Abs(delta);

            if (distance <= _swipeThreshold)
                return _currentPage;

            var isNextPage = delta > 0;
            return ClampPage(isNextPage ? _currentPage + 1 : _currentPage - 1);
        }
        
#if UNITY_EDITOR
        private void Update()
        {
            if (TotalPages == 0)
                return;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.wasReleasedThisFrame)
                    ScrollToItemIndex(ClampPage(_currentPage - 1), _scrollingDuration);
                else if (Keyboard.current.downArrowKey.wasReleasedThisFrame)
                    ScrollToItemIndex(ClampPage(_currentPage + 1), _scrollingDuration);
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyUp(KeyCode.UpArrow))
                ScrollToItemIndex(ClampPage(_currentPage - 1), _scrollingDuration);
            else if (Input.GetKeyUp(KeyCode.DownArrow))
                ScrollToItemIndex(ClampPage(_currentPage + 1), _scrollingDuration);
#endif
        }
#endif
    }
}
