// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RecyclableScrollRect
{
    /// <summary>
    /// A paged scroll rect that keeps the content still and moves the items instead.
    /// The focused item is centered in the viewport, headers and footers are not supported.
    /// </summary>
    public class RSRCarousel : RSRPages
    {
        [SerializeField] private bool _loop = true;

        private ICarouselDataSource _carouselDataSource;
        private Vector2 _scrollPosition;
        private bool _isDraggingItems;
        private Vector2 _dragStartCursor;
        private Vector2 _dragStartScrollPosition;

        private bool _isLooping;
        private float _loopLength;
        private readonly HashSet<int> _loopVisibleIndices = new HashSet<int>();
        private readonly List<int> _itemsToHide = new List<int>();
        private readonly List<int> _siblingOrder = new List<int>();
        private Comparison<int> _compareSiblingOrder;

        /// <summary>
        /// Stores the scroll position instead of moving the content, and moves the visible items to match it
        /// </summary>
        protected override Vector2 ScrollPosition
        {
            get => _scrollPosition;

            set
            {
                _scrollPosition = value;
                if (_visibleItems == null)
                {
                    return;
                }

                if (_isLooping && IsInitialized)
                {
                    RefreshLoopVisibility();
                }
                LayoutVisibleItems();
            }
        }

        // the first and last items are inset by half the free viewport space so they can be centered,
        // which means the padding along the scrolling axis is not used. A loop has no first or last item so it needs no inset
        protected override float StartPadding => _isLooping ? 0 : CenteringInset(0);
        protected override float EndPadding => _isLooping ? 0 : CenteringInset(_itemsCount - 1);

        // a loop has no start or end, so the visible items are refreshed whenever the scroll position changes instead
        protected override bool HandlesScrollInLateUpdate => !_isLooping;

        protected override void Initialize()
        {
            _carouselDataSource = _dataSource as ICarouselDataSource;
            if (_sectionsSource != null && !(_sectionsSource is ItemsOnlySectionsSource))
            {
                if (HasHeadersOrFooters(_sectionsSource))
                {
                    Debug.LogWarning("RSRCarousel does not support headers and footers, they will be ignored");
                }
                _sectionsSource = new ItemsOnlySectionsSource(_sectionsSource);
            }

            base.Initialize();

            // the content never moves, make sure it starts at the origin so the items line up with the viewport
            var contentPosition = content.anchoredPosition;
            contentPosition[_axis] = 0;
            content.anchoredPosition = contentPosition;

            // a loop has no inset, so the current page has to be scrolled to the center
            if (_isLooping)
            {
                ContentPosition = GetScrollToItemTarget(_currentPage);
            }
        }

        protected override void BuildItemsMap()
        {
            base.BuildItemsMap();

            var wasLooping = _isLooping;
            UpdateLoopState();

            // the item positions depend on the padding, which changes when looping is turned on or off
            if (wasLooping != _isLooping && _itemPositions != null)
            {
                foreach (var itemPosition in _itemPositions)
                {
                    itemPosition.ResetAllFlags();
                }
            }
        }

        /// <summary>
        /// Looping needs every item size up front to know the loop length, and enough items that none has to be shown twice at once
        /// </summary>
        private void UpdateLoopState()
        {
            _isLooping = false;
            if (!_loop || _itemsCount == 0)
            {
                return;
            }

            if (!IsItemSizeKnown)
            {
                Debug.LogWarning("RSRCarousel can only loop when the item sizes are known, looping is disabled");
                return;
            }

            var rsrDataSource = (IRSRDataSource)_dataSource;
            var minItemStep = float.MaxValue;
            _loopLength = 0;
            for (var i = 0; i < _itemsCount; i++)
            {
                var itemStep = rsrDataSource.GetItemSize(_itemData[i]) + _spacing[_axis];
                _loopLength += itemStep;
                minItemStep = Mathf.Min(minItemStep, itemStep);
            }

            var maxVisibleItems = minItemStep > 0 ? Mathf.CeilToInt(viewport.rect.size[_axis] / minItemStep) + 1 + 2 * ExtraItemsVisible : int.MaxValue;
            if (_itemsCount < maxVisibleItems)
            {
                Debug.LogWarning($"RSRCarousel needs at least {maxVisibleItems} items to loop with the current viewport size and extra items visible, looping is disabled");
                return;
            }

            _isLooping = true;
        }

        protected override void InitializeItems(int startIndex = 0)
        {
            if (_isLooping)
            {
                RefreshLoopVisibility();
                return;
            }
            base.InitializeItems(startIndex);
        }

        protected override void RefreshAfterReload()
        {
            base.RefreshAfterReload();
            if (_isLooping)
            {
                RefreshLoopVisibility();
            }
            LayoutVisibleItems();
        }

        /// <summary>
        /// Shows the items that overlap the viewport, plus the extra items on both sides, and hides the rest.
        /// The viewport is wrapped into one loop length and the walk wraps from the last item back to the first
        /// </summary>
        private void RefreshLoopVisibility()
        {
            var viewportStart = Mathf.Repeat(_scrollPosition[_axis] * (vertical ? 1 : -1), _loopLength);
            var viewportEnd = viewportStart + viewport.rect.size[_axis];

            // shortcut: relies on every item position being set, an item size change from ReloadItem is only picked up by the loop after ReloadData
            var index = FindFirstItemEndingAfter(viewportStart);
            var wrapOffset = 0f;
            if (index == _itemsCount)
            {
                index = 0;
                wrapOffset = _loopLength;
            }
            var firstIndex = index;

            _loopVisibleIndices.Clear();
            while (_loopVisibleIndices.Count < _itemsCount && _itemPositions[index].absTopLeftPosition[_axis] + wrapOffset < viewportEnd)
            {
                _loopVisibleIndices.Add(index);
                index++;
                if (index == _itemsCount)
                {
                    index = 0;
                    wrapOffset += _loopLength;
                }
            }

            for (var i = 1; i <= ExtraItemsVisible; i++)
            {
                _loopVisibleIndices.Add(WrapIndex(firstIndex - i));
                _loopVisibleIndices.Add(WrapIndex(index + i - 1));
            }

            _itemsToHide.Clear();
            foreach (var visibleItem in _visibleItems)
            {
                if (!_loopVisibleIndices.Contains(visibleItem.Key))
                {
                    _itemsToHide.Add(visibleItem.Key);
                }
            }

            foreach (var itemIndex in _itemsToHide)
            {
                HideItemAtIndex(itemIndex);
            }

            foreach (var itemIndex in _loopVisibleIndices)
            {
                if (!_visibleItems.ContainsKey(itemIndex))
                {
                    ShowItemAtIndex(itemIndex);
                }
            }
        }

        /// <summary>
        /// Binary search for the first item whose end is after the position, returns the items count if there is none
        /// </summary>
        private int FindFirstItemEndingAfter(float position)
        {
            var low = 0;
            var high = _itemsCount;
            while (low < high)
            {
                var mid = (low + high) / 2;
                if (_itemPositions[mid].absBottomRightPosition[_axis] > position)
                {
                    high = mid;
                }
                else
                {
                    low = mid + 1;
                }
            }
            return low;
        }

        private int WrapIndex(int itemIndex)
        {
            return (itemIndex % _itemsCount + _itemsCount) % _itemsCount;
        }

        // when looping, the visible items are refreshed from the scroll position directly, so the linear show and hide steps are skipped
        protected override void HideItemsAtTopLeft()
        {
            if (!_isLooping)
            {
                base.HideItemsAtTopLeft();
            }
        }

        protected override void ShowItemsAtBottomRight()
        {
            if (!_isLooping)
            {
                base.ShowItemsAtBottomRight();
            }
        }

        protected override void HideItemsAtBottomRight()
        {
            if (!_isLooping)
            {
                base.HideItemsAtBottomRight();
            }
        }

        protected override void ShowItemsAtTopLeft()
        {
            if (!_isLooping)
            {
                base.ShowItemsAtTopLeft();
            }
        }

        protected override bool IsLastRowColumn(int itemIndex)
        {
            return !_isLooping && base.IsLastRowColumn(itemIndex);
        }

        protected override void ClampContentPosition()
        {
            if (!_isLooping)
            {
                base.ClampContentPosition();
            }
        }

        protected override float GetScrollToItemTarget(int itemIndex)
        {
            if (!_isLooping)
            {
                return base.GetScrollToItemTarget(itemIndex);
            }

            var sign = vertical ? 1 : -1;
            var target = _itemPositions[itemIndex].absTopLeftPosition[_axis] - GetScrollToItemOffset(itemIndex);

            // pick the copy of the target closest to the current position, so scrolling takes the short way around the loop
            var currentPosition = _scrollPosition[_axis] * sign;
            target += _loopLength * Mathf.Round((currentPosition - target) / _loopLength);
            return target * sign;
        }

        protected override void PerformPostScrollingActions(bool callEvent, AnimationState animationState, bool scrollingDown, int itemIndex = -1)
        {
            base.PerformPostScrollingActions(callEvent, animationState, scrollingDown, itemIndex);

            // keep the scroll position within one loop length so it doesn't lose float precision after many pages
            if (_isLooping && animationState == AnimationState.Finished)
            {
                var sign = vertical ? 1 : -1;
                var scrollPosition = _scrollPosition;
                scrollPosition[_axis] = Mathf.Repeat(scrollPosition[_axis] * sign, _loopLength) * sign;
                ScrollPosition = scrollPosition;
            }
        }

        protected override int CalculateNextPageAfterDrag()
        {
            if (!_isLooping)
            {
                return base.CalculateNextPageAfterDrag();
            }

            var delta = (_scrollPosition[_axis] - _dragStartScrollPosition[_axis]) * (vertical ? 1 : -1);
            if (Mathf.Abs(delta) <= _swipeThreshold)
            {
                return _currentPage;
            }
            return WrapIndex(_currentPage + (delta > 0 ? 1 : -1));
        }

        protected override bool IsNextPage(int targetPage)
        {
            if (!_isLooping)
            {
                return base.IsNextPage(targetPage);
            }

            // around a loop, the next page is the one reached by going forward the shorter way
            return WrapIndex(targetPage - _currentPage) <= _itemsCount / 2;
        }

        /// <summary>
        /// Places a visible item, by default in a row around the center of the viewport.
        /// Override it for other layouts such as a card stack
        /// </summary>
        /// <param name="itemIndex">index of the item to place</param>
        /// <param name="itemRect">RectTransform of the item</param>
        /// <param name="offsetFromFocus">how many items away from the center of the viewport the item is, 0 is centered, negative is before it, positive is after it</param>
        protected virtual void LayoutItem(int itemIndex, RectTransform itemRect, float offsetFromFocus)
        {
            // the content sits at the origin of the viewport, so the item position is measured from the viewport center
            var position = _itemPositions[itemIndex].topLeftPosition;
            var itemSize = GetItemAxisSize(itemIndex);
            var itemStart = viewport.rect.size[_axis] / 2f + offsetFromFocus * (itemSize + _spacing[_axis]) - itemSize / 2f;
            position[_axis] = itemStart * (vertical ? -1 : 1);
            itemRect.anchoredPosition = position;
        }

        protected override void SetItemPosition(int itemIndex, RectTransform rect = null)
        {
            base.SetItemPosition(itemIndex, rect);
            if (rect != null && _visibleItems.TryGetValue(itemIndex, out var visibleItem))
            {
                UpdateItemLayout(itemIndex, visibleItem);
            }
        }

        private void LayoutVisibleItems()
        {
            foreach (var visibleItem in _visibleItems)
            {
                UpdateItemLayout(visibleItem.Key, visibleItem.Value);
            }

            // layouts like a card stack change which item is on top while scrolling
            SetSiblingIndices();
        }

        private void UpdateItemLayout(int itemIndex, Item item)
        {
            var offsetFromFocus = GetOffsetFromFocus(itemIndex);
            LayoutItem(itemIndex, item.transform, offsetFromFocus);
            _carouselDataSource?.ItemOffsetChanged(item.item, _itemData[itemIndex], offsetFromFocus);
        }

        /// <summary>
        /// The distance between the item's center and the viewport's center, in items.
        /// When looping it's the shorter distance around the loop
        /// </summary>
        protected float GetOffsetFromFocus(int itemIndex)
        {
            var itemPosition = _itemPositions[itemIndex];
            var itemSize = itemPosition.itemSize[_axis];
            var itemCenter = itemPosition.absTopLeftPosition[_axis] + itemSize / 2f;
            var viewportCenter = _scrollPosition[_axis] * (vertical ? 1 : -1) + viewport.rect.size[_axis] / 2f;
            var distance = itemCenter - viewportCenter;
            if (_isLooping)
            {
                distance -= _loopLength * Mathf.Round(distance / _loopLength);
            }

            var itemStep = itemSize + _spacing[_axis];
            return itemStep > 0 ? distance / itemStep : 0;
        }

        /// <summary>
        /// Draws the items in order of their position rather than their index, so the order stays right across the loop
        /// </summary>
        protected override void SetSiblingIndices()
        {
            _siblingOrder.Clear();
            foreach (var visibleItem in _visibleItems)
            {
                _siblingOrder.Add(visibleItem.Key);
            }

            _compareSiblingOrder ??= (a, b) => GetSiblingOrder(a).CompareTo(GetSiblingOrder(b));
            _siblingOrder.Sort(_compareSiblingOrder);

            // this runs on every scroll, so only touch the hierarchy when the order actually changed
            var previousSiblingIndex = -1;
            var isInOrder = true;
            foreach (var itemIndex in _siblingOrder)
            {
                var siblingIndex = _visibleItems[itemIndex].transform.GetSiblingIndex();
                if (siblingIndex < previousSiblingIndex)
                {
                    isInOrder = false;
                    break;
                }
                previousSiblingIndex = siblingIndex;
            }
            if (isInOrder)
            {
                return;
            }

            foreach (var itemIndex in _siblingOrder)
            {
                _visibleItems[itemIndex].transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// Items with a higher order are drawn on top, by default the items further along are on top
        /// </summary>
        protected virtual float GetSiblingOrder(int itemIndex)
        {
            return GetOffsetFromFocus(itemIndex);
        }

        protected override float GetScrollToItemOffset(int itemIndex)
        {
            return (viewport.rect.size[_axis] - GetItemAxisSize(itemIndex)) / 2f;
        }

        private float CenteringInset(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= _itemPositions.Count)
            {
                return 0;
            }
            return Mathf.Max(0, (viewport.rect.size[_axis] - _itemPositions[itemIndex].itemSize[_axis]) / 2f);
        }

        protected override void SetItemSize(int itemIndex, RectTransform rect = null)
        {
            var oldSize = GetItemAxisSize(itemIndex);
            base.SetItemSize(itemIndex, rect);

            // with unknown item sizes, the insets of the first and last items are only right once those items are measured
            var isEdgeItem = itemIndex == 0 || itemIndex == _itemsCount - 1;
            if (isEdgeItem && !_isLooping && !Mathf.Approximately(oldSize, GetItemAxisSize(itemIndex)))
            {
                CalculateContentSize();
            }
        }

        protected override void RecalculateTrailingItems(int itemIndex)
        {
            base.RecalculateTrailingItems(itemIndex);

            // an item changed size, keep the focused page centered unless the user or an animation is moving the items
            if (!_isAnimating && !_isDraggingItems && _currentPage < _itemsCount && _itemPositions[_currentPage].positionSet)
            {
                ContentPosition = GetScrollToItemTarget(_currentPage);
            }
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            _isDraggingItems = !_isAnimating && eventData.button == PointerEventData.InputButton.Left && IsActive() &&
                               RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out _dragStartCursor);
            _dragStartScrollPosition = _scrollPosition;
        }

        /// <summary>
        /// Moves the items with the pointer instead of letting the ScrollRect move the content
        /// </summary>
        public override void OnDrag(PointerEventData eventData)
        {
            if (!_isDraggingItems || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out var cursor))
            {
                return;
            }

            var scrollPosition = _dragStartScrollPosition;
            scrollPosition[_axis] += (cursor - _dragStartCursor)[_axis];
            ScrollPosition = scrollPosition;
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            _isDraggingItems = false;
            base.OnEndDrag(eventData);
        }

        // shortcut: the mouse wheel does nothing since the ScrollRect would move the content directly, page with it if requested
        public override void OnScroll(PointerEventData data)
        {
        }

        private static bool HasHeadersOrFooters(ISectionsSource sectionsSource)
        {
            if (sectionsSource.ScrollRectHasHeader || sectionsSource.ScrollRectHasFooter)
            {
                return true;
            }

            for (var i = 0; i < sectionsSource.SectionsCount; i++)
            {
                if (sectionsSource.SectionHasHeader(i) || sectionsSource.SectionHasFooter(i))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Exposes the sections of a sections source without their headers and footers
        /// </summary>
        private sealed class ItemsOnlySectionsSource : ISectionsSource
        {
            private readonly ISectionsSource _source;

            public ItemsOnlySectionsSource(ISectionsSource source)
            {
                _source = source;
            }

            public int SectionsCount => _source.SectionsCount;
            public bool ScrollRectHasHeader => false;
            public bool ScrollRectHasFooter => false;
            public bool ScrollRectHeaderIsPinned => false;
            public bool SectionHasHeader(int sectionIndex) => false;
            public bool SectionHasFooter(int sectionIndex) => false;
            public bool HeaderIsPinned(int sectionIndex) => false;
        }
    }
}
