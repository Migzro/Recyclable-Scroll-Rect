using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RecyclableSR
{
    public class RSRPages : RSRBase
    {
        [SerializeField] private float _cardZMultiplier;
        [SerializeField] private bool _cardMode;

        private IPageSource _pageSource;
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
        protected override void RefreshAfterReload()
        {
            base.RefreshAfterReload();
            
            if (_currentPage >= _itemsCount)
            {
                // scroll cell will handle the focus
                ScrollToCell(Mathf.Max(0, _currentPage - 1), instant:true);
            }
            else  if (_itemsCount > 0 && _visibleItems.ContainsKey(_currentPage))
            {
                _pageSource?.PageWillFocus(_currentPage, true, _visibleItems[_currentPage].cell, _visibleItems[_currentPage].transform, _itemPositions[_currentPage].topLeftPosition);
                _pageSource?.PageFocused(_currentPage, true, _visibleItems[_currentPage].cell);
            }
            
            if (_cardMode)
                SetCardsZIndices();
        }

        protected override void SetIndices()
        {
            base.SetIndices();
            
            // TODO: replace SetCardsZIndices With this
        }

        /// <summary>
        /// Set cell z index on card when after it finishes scrolling
        /// also set cell canvas group intractability & order in canvas
        /// </summary>
        protected override void SetCardsZIndices(int pageToStaggerAnimationFor = -1)
        {
            base.SetCardsZIndices(pageToStaggerAnimationFor);
            
            var childrenSiblingOrder = new SortedDictionary<int, Transform>();
            foreach (var visibleItem in _visibleItems)
            {
                // set card z position
                var cellZIndex = Mathf.Abs(visibleItem.Key -_currentPage) * _cardZMultiplier;
                var cellPosition = visibleItem.Value.transform.anchoredPosition3D; 
                cellPosition.y = 0;
                cellPosition.z = cellZIndex;
                visibleItem.Value.transform.anchoredPosition3D = cellPosition;

                // set card as interactable if is current index
                visibleItem.Value.cell.CanvasGroup.interactable = (visibleItem.Key == _currentPage);
                visibleItem.Value.cell.CanvasGroup.blocksRaycasts = (visibleItem.Key == _currentPage);
                
                // sort items
                var siblingOrder = visibleItem.Key - _currentPage;
                if (siblingOrder > 0)
                    siblingOrder = siblingOrder * -1 - _currentPage;

                visibleItem.Value.cell.CanvasGroup.alpha = visibleItem.Key >= _currentPage && !_reverseDirection ? 1 : 0;
                childrenSiblingOrder.Add(siblingOrder, visibleItem.Value.transform);
            }

            foreach (var child in childrenSiblingOrder)  
            {
                child.Value.SetAsLastSibling();
            }

            if (pageToStaggerAnimationFor != -1)
            {
                _visibleItems[pageToStaggerAnimationFor].cell.CanvasGroup.alpha = 1;
                _visibleItems[pageToStaggerAnimationFor].transform.SetAsLastSibling();
            }
        }

        public override void ScrollToTopRight()
        {
            base.ScrollToTopRight();
            ScrollToCell(0, instant:true);
        }
        
        protected override void PreformPreScrollingActions(int cellIndex, int direction)
        {
            base.PreformPreScrollingActions(cellIndex, direction);
            
            // create a list that will stop ScrollTo method from calling SetCellData on items that will only be visible in the one frame while scrolling, this assumes
            // that the paging cell is taking up the entire width or height
            var endingIndex = cellIndex;
            _ignoreSetCellDataIndices.Clear();
            if (direction > 0)
            {
                endingIndex -= _extraItemsVisible;
                endingIndex = Mathf.Clamp(endingIndex, 0, _itemsCount - 1);
                for (var j = _currentPage; j < endingIndex; j++)
                {
                    if (!_visibleItems.ContainsKey(j) && _itemPositions[j].positionSet)
                    {
                        _ignoreSetCellDataIndices.Add(j);
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
                        _ignoreSetCellDataIndices.Add(j);
                    }
                }
            }
            
            _forceCallWillFocusAfterAnimation = !_visibleItems.ContainsKey(cellIndex);
            var isNextPage = cellIndex > _currentPage && !_reverseDirection;
            if (_visibleItems.ContainsKey(cellIndex))
            {
                _pageSource?.PageWillFocus(cellIndex, isNextPage, _visibleItems[cellIndex].cell, _visibleItems[cellIndex].transform, _itemPositions[cellIndex].topLeftPosition);
            }

            if (_visibleItems.ContainsKey(_currentPage))
            {
                _pageSource?.PageWillUnFocus(_currentPage, isNextPage, _visibleItems[_currentPage].cell, _visibleItems[_currentPage].transform);
            }
        }

        protected override void PreformPostScrollingActions(int cellIndex, bool instant)
        {
            base.PreformPostScrollingActions(cellIndex, instant);
            
            if (_currentPage != cellIndex)
            {
                var isNextPage = cellIndex > _currentPage && !_reverseDirection;

                var pageToStaggerAnimation = -1;
                if (!instant)
                {
                    if (_manuallyHandleCardAnimations && isNextPage)
                        pageToStaggerAnimation = _currentPage;
                }

                if (_visibleItems.TryGetValue(_currentPage, out var visibleItem))
                    _pageSource?.PageUnFocused(_currentPage, isNextPage, visibleItem.cell);

                _currentPage = cellIndex;
                if (_forceCallWillFocusAfterAnimation)
                    _pageSource?.PageWillFocus(_currentPage, isNextPage, _visibleItems[_currentPage].cell, _visibleItems[_currentPage].transform, _itemPositions[_currentPage].topLeftPosition);
                _pageSource?.PageFocused(_currentPage, isNextPage, _visibleItems[_currentPage].cell);
                
                SetCardsZIndices(pageToStaggerAnimation);
            }
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (!_cardMode)
                base.OnBeginDrag(eventData);

            _isDragging = true;
            _dragStartingPosition = content.anchoredPosition * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
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
            _dataSource.ScrolledToCell(_visibleItems[newPage].cell, newPage);
            ScrollToCell(newPage, false);
        }

        protected virtual int CalculateNextPageAfterDrag()
        {
            var currentPagePosition = _visibleItems[_currentPage].transform.anchoredPosition;
            var currentPageStartingPosition = _itemPositions[_currentPage].topLeftPosition;
            var distance = Vector2.Distance(currentPageStartingPosition, currentPagePosition);
            var isNextPage = (currentPagePosition[_axis] < currentPageStartingPosition[_axis]) && !_reverseDirection;
            var newPage = _currentPage;
            if (distance > _swipeThreshold)
            {
                if (isNextPage && _currentPage < _itemsCount - 1)
                    newPage++;
                else if (!isNextPage && _currentPage > 0)
                    newPage--;

                if (newPage != _currentPage)
                {
                    _pageSource?.PageWillFocus(newPage, isNextPage, _visibleItems[newPage].cell, _visibleItems[newPage].transform, _itemPositions[newPage].topLeftPosition);
                    _pageSource?.PageWillUnFocus(_currentPage, isNextPage, _visibleItems[_currentPage].cell, _visibleItems[_currentPage].transform);
                }
            }
            
            _visibleItems[_currentPage].transform.anchoredPosition = currentPageStartingPosition;
            return newPage;
        }
        
        
#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.UpArrow))
            {
                ScrollToCell(Mathf.Max(_currentPage - 1, 0), false);
            }
            else if (Input.GetKeyUp( KeyCode.DownArrow))
            {
                ScrollToCell(Mathf.Min(_currentPage + 1, _itemsCount-1), false);
            }
        }
#endif
    }
}