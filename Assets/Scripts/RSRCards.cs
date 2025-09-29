using UnityEngine;

namespace RecyclableSR
{
    public class RSRCards : RSRPages
    {
        protected override int CalculateNextPageAfterDrag()
        {
            var currentContentPosition = content.anchoredPosition * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
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
    }
}