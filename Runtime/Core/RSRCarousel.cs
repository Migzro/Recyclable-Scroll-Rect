// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
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
        private ICarouselDataSource _carouselDataSource;
        private Vector2 _scrollPosition;
        private bool _isDraggingItems;
        private Vector2 _dragStartCursor;
        private Vector2 _dragStartScrollPosition;

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

                foreach (var visibleItem in _visibleItems)
                {
                    UpdateItemLayout(visibleItem.Key, visibleItem.Value);
                }
            }
        }

        // the first and last items are inset by half the free viewport space so they can be centered,
        // which means the padding along the scrolling axis is not used
        protected override float StartPadding => CenteringInset(0);
        protected override float EndPadding => CenteringInset(_itemsCount - 1);

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
        }

        /// <summary>
        /// Places a visible item, by default in a row relative to the current scroll position as if the content had moved.
        /// Override it for other layouts such as a card stack
        /// </summary>
        /// <param name="itemIndex">index of the item to place</param>
        /// <param name="itemRect">RectTransform of the item</param>
        /// <param name="offsetFromFocus">how many items away from the center of the viewport the item is, 0 is centered, negative is before it, positive is after it</param>
        protected virtual void LayoutItem(int itemIndex, RectTransform itemRect, float offsetFromFocus)
        {
            itemRect.anchoredPosition = _itemPositions[itemIndex].topLeftPosition + _scrollPosition;
        }

        protected override void SetItemPosition(int itemIndex, RectTransform rect = null)
        {
            base.SetItemPosition(itemIndex, rect);
            if (rect != null && _visibleItems.TryGetValue(itemIndex, out var visibleItem))
            {
                UpdateItemLayout(itemIndex, visibleItem);
            }
        }

        private void UpdateItemLayout(int itemIndex, Item item)
        {
            var offsetFromFocus = GetOffsetFromFocus(itemIndex);
            LayoutItem(itemIndex, item.transform, offsetFromFocus);
            _carouselDataSource?.ItemOffsetChanged(item.item, _itemData[itemIndex], offsetFromFocus);
        }

        /// <summary>
        /// The distance between the item's center and the viewport's center, in items
        /// </summary>
        protected float GetOffsetFromFocus(int itemIndex)
        {
            var itemPosition = _itemPositions[itemIndex];
            var itemSize = itemPosition.itemSize[_axis];
            var itemCenter = itemPosition.absTopLeftPosition[_axis] + itemSize / 2f;
            var viewportCenter = _scrollPosition[_axis] * (vertical ? 1 : -1) + viewport.rect.size[_axis] / 2f;
            var itemStep = itemSize + _spacing[_axis];
            return itemStep > 0 ? (itemCenter - viewportCenter) / itemStep : 0;
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

            // shortcut: uses the size known so far, with unknown item sizes the last item may not center exactly until it's measured, revisit when adding dynamic sizes support
            return Mathf.Max(0, (viewport.rect.size[_axis] - _itemPositions[itemIndex].itemSize[_axis]) / 2f);
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
