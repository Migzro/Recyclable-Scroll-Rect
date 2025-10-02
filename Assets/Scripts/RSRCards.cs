using UnityEngine;

namespace RecyclableSR
{
    public class RSRCards : RSRPages
    {
        protected override int CalculateNextPageAfterDrag()
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
    }
}