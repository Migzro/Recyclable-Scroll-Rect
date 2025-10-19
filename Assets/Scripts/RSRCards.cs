using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RecyclableSR
{
    public class RSRCards : RSRPages
    {
        [SerializeField] private float _cardZMultiplier;
        [SerializeField] private bool _manuallyHandleCardAnimations;

        private bool _isDragging;

        protected override void RefreshAfterReload(bool reloadAllItems)
        {
            base.RefreshAfterReload(reloadAllItems);
            SetSiblingIndices();
        }

        protected override void SetSiblingIndices()
        {
            SetCardsZIndices();
        }
        
        /// <summary>
        /// Set item z index on card when after it finishes scrolling
        /// also set item canvas group intractability & order in canvas
        /// </summary>
        private void SetCardsZIndices(int pageToStaggerAnimationFor = -1)
        {
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
        
        public override void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _dragStartingPosition = content.anchoredPosition * (vertical ? 1 : -1);
        }
        
        /// <summary>
        /// only used in cards mode, this overrides the dragging behavior of scroll view and moves the cards by themselves
        /// </summary>
        /// <param name="eventData"></param>
        public override void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;

            var deltaMovement = eventData.delta;
            deltaMovement[1 - _axis] = 0;
            _visibleItems[_currentPage].transform.anchoredPosition += deltaMovement;
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;
            
            _isDragging = false;
            var newPage = CalculateNextPageAfterDrag();
            ScrollToItem(newPage);
        }
        
        protected override int CalculateNextPageAfterDrag()
        {
            var currentPagePosition = _visibleItems[_currentPage].transform.anchoredPosition;
            var currentPageStartingPosition = _itemPositions[_currentPage].topLeftPosition;
            var distance = Vector2.Distance(currentPageStartingPosition, currentPagePosition);
            var isNextPage = (currentPagePosition[_axis] < currentPageStartingPosition[_axis]);
            var newPage = _currentPage;
            if (distance > _swipeThreshold)
            {
                if (isNextPage && _currentPage < _itemsCount - 1)
                    newPage++;
                else if (!isNextPage && _currentPage > 0)
                    newPage--;

                if (newPage != _currentPage)
                {
                    _pageSource?.PageWillFocus(newPage, isNextPage, _visibleItems[newPage].item, _visibleItems[newPage].transform, _itemPositions[newPage].topLeftPosition);
                    _pageSource?.PageWillUnFocus(_currentPage, isNextPage, _visibleItems[_currentPage].item, _visibleItems[_currentPage].transform);
                }
            }
            
            _visibleItems[_currentPage].transform.anchoredPosition = currentPageStartingPosition;
            return newPage;
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
                SetCardsZIndices(pageToStaggerAnimation);
            }
        }
    }
}