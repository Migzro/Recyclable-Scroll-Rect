using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RecyclableSR
{
    public class RSRPages : RSR
    {
        [SerializeField] protected float _swipeThreshold = 200;
        [SerializeField] private float _cardZMultiplier;
        [SerializeField] private bool _cardMode;
        [SerializeField] private bool _manuallyHandleCardAnimations;

        protected IPageSource _pageSource;
        private bool _isDragging;
        private bool _forceCallWillFocusAfterAnimation;

        protected override void Initialize()
        {
            _pageSource = (IPageSource)_dataSource;
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
                ScrollToItem(Mathf.Max(0, _currentPage - 1), instant:true);
            }
            else  if (_itemsCount > 0 && _visibleItems.ContainsKey(_currentPage))
            {
                _pageSource?.PageWillFocus(_currentPage, true, _visibleItems[_currentPage].item, _visibleItems[_currentPage].transform, _itemPositions[_currentPage].topLeftPosition);
                _pageSource?.PageFocused(_currentPage, true, _visibleItems[_currentPage].item);
            }
            
            SetCardsZIndices();
        }

        /// <summary>
        /// Set item z index on card when after it finishes scrolling
        /// also set item canvas group intractability & order in canvas
        /// </summary>
        private void SetCardsZIndices(int pageToStaggerAnimationFor = -1)
        {
            if (!_cardMode)
                return;
            
            var childrenSiblingOrder = new SortedDictionary<int, Transform>();
            foreach (var visibleItem in _visibleItems)
            {
                // set card z position
                var itemZIndex = Mathf.Abs(visibleItem.Key -_currentPage) * _cardZMultiplier;
                var itemPosition = visibleItem.Value.transform.anchoredPosition3D; 
                itemPosition.y = 0;
                itemPosition.z = itemZIndex;
                visibleItem.Value.transform.anchoredPosition3D = itemPosition;

                // set card as interactable if is current index
                visibleItem.Value.item.CanvasGroup.interactable = (visibleItem.Key == _currentPage);
                visibleItem.Value.item.CanvasGroup.blocksRaycasts = (visibleItem.Key == _currentPage);
                
                // sort items
                var siblingOrder = visibleItem.Key - _currentPage;
                if (siblingOrder > 0)
                    siblingOrder = siblingOrder * -1 - _currentPage;

                visibleItem.Value.item.CanvasGroup.alpha = visibleItem.Key >= _currentPage ? 1 : 0;
                childrenSiblingOrder.Add(siblingOrder, visibleItem.Value.transform);
            }

            foreach (var child in childrenSiblingOrder)  
            {
                child.Value.SetAsLastSibling();
            }

            if (pageToStaggerAnimationFor != -1)
            {
                _visibleItems[pageToStaggerAnimationFor].item.CanvasGroup.alpha = 1;
                _visibleItems[pageToStaggerAnimationFor].transform.SetAsLastSibling();
            }
        }

        public override void ScrollToTopRight()
        {
            base.ScrollToTopRight();
            ScrollToItem(0, instant:true);
        }
        
        protected override void PreformPreScrollingActions(int itemIndex, int direction)
        {
            base.PreformPreScrollingActions(itemIndex, direction);
            
            // create a list that will stop ScrollTo method from calling SetItemData on items that will only be visible in the one frame while scrolling, this assumes
            // that the paging item is taking up the entire width or height
            var endingIndex = itemIndex;
            _ignoreSetItemDataIndices.Clear();
            if (direction > 0)
            {
                endingIndex -= _extraItemsVisible;
                endingIndex = Mathf.Clamp(endingIndex, 0, _itemsCount - 1);
                for (var j = _currentPage; j < endingIndex; j++)
                {
                    if (!_visibleItems.ContainsKey(j) && _itemPositions[j].positionSet)
                    {
                        _ignoreSetItemDataIndices.Add(j);
                    }
                }
            }
            else
            {
                endingIndex += _extraItemsVisible;
                endingIndex = Mathf.Clamp(endingIndex, 0, _itemsCount - 1);
                for (var j = _currentPage - 1; j > endingIndex; j--)
                {
                    if (!_visibleItems.ContainsKey(j) && _itemPositions[j].positionSet)
                    {
                        _ignoreSetItemDataIndices.Add(j);
                    }
                }
            }
            
            _forceCallWillFocusAfterAnimation = !_visibleItems.ContainsKey(itemIndex);
            var isNextPage = itemIndex > _currentPage;
            if (_visibleItems.ContainsKey(itemIndex))
            {
                _pageSource?.PageWillFocus(itemIndex, isNextPage, _visibleItems[itemIndex].item, _visibleItems[itemIndex].transform, _itemPositions[itemIndex].topLeftPosition);
            }

            if (_visibleItems.ContainsKey(_currentPage))
            {
                _pageSource?.PageWillUnFocus(_currentPage, isNextPage, _visibleItems[_currentPage].item, _visibleItems[_currentPage].transform);
            }
        }

        protected override void PreformPostScrollingActions(int itemIndex, bool instant)
        {
            base.PreformPostScrollingActions(itemIndex, instant);
            
            if (_currentPage != itemIndex)
            {
                var isNextPage = itemIndex > _currentPage;
                var pageToStaggerAnimation = -1;
                if (!instant)
                {
                    if (_manuallyHandleCardAnimations && isNextPage)
                        pageToStaggerAnimation = _currentPage;
                }

                if (_visibleItems.TryGetValue(_currentPage, out var visibleItem))
                    _pageSource?.PageUnFocused(_currentPage, isNextPage, visibleItem.item);

                _currentPage = itemIndex;
                if (_forceCallWillFocusAfterAnimation)
                    _pageSource?.PageWillFocus(_currentPage, isNextPage, _visibleItems[_currentPage].item, _visibleItems[_currentPage].transform, _itemPositions[_currentPage].topLeftPosition);
                _pageSource?.PageFocused(_currentPage, isNextPage, _visibleItems[_currentPage].item);
                
                SetCardsZIndices(pageToStaggerAnimation);
            }
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (!_cardMode)
                base.OnBeginDrag(eventData);

            _isDragging = true;
            _dragStartingPosition = content.anchoredPosition * (vertical ? 1 : -1);
        }
        
        /// <summary>
        /// only used in cards mode, this overrides the dragging behavior of scroll view and moves the cards by themselves
        /// </summary>
        /// <param name="eventData"></param>
        public override void OnDrag(PointerEventData eventData)
        {
            if (!_cardMode)
                base.OnDrag(eventData);

            if (!_isDragging || !_cardMode)
                return;

            var deltaMovement = eventData.delta;
            deltaMovement[1 - _axis] = 0;
            _visibleItems[_currentPage].transform.anchoredPosition += deltaMovement;
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (!_cardMode)
                base.OnEndDrag(eventData);
            
            if (!_isDragging)
                return;
            
            _isDragging = false;
            var newPage = CalculateNextPageAfterDrag();
            // TODO: why was this here?
            // _dataSource.ScrolledToItem(_visibleItems[newPage].item, newPage);
            ScrollToItem(newPage);
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
                ScrollToItem(Mathf.Max(_currentPage - 1, 0), false);
            }
            else if (Input.GetKeyUp( KeyCode.DownArrow))
            {
                ScrollToItem(Mathf.Min(_currentPage + 1, _itemsCount-1), false);
            }
        }
#endif
    }
}