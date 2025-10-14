using UnityEngine;

namespace RecyclableSR
{
    public class RSR : RSRBase
    {
        /// <summary>
        /// Initialize all cells needed until the view port is filled
        /// extra visible items is an additional amount of cells that can be shown to prevent showing an empty view port if the scrolling is too fast and the update function didn't show all the items
        /// that need to be shown
        /// </summary>
        /// <param name="startIndex">the starting cell index on which we want initialized</param>
        protected override void InitializeCells(int startIndex = 0)
        {
            base.InitializeCells(startIndex);
            
            GetContentBounds();
            var contentHasSpace = startIndex == 0 || _itemPositions[startIndex - 1].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
            var extraItemsInitialized = contentHasSpace ? 0 : _maxExtraVisibleItemInViewPort - _maxVisibleItemInViewPort;
            var i = startIndex;
            while ((contentHasSpace || extraItemsInitialized < _extraItemsVisible) && i < _itemsCount)
            {
                ShowHideCellsAtIndex(i, true);
                if (!contentHasSpace)
                    extraItemsInitialized++;
                else
                    _maxVisibleItemInViewPort = i;

                contentHasSpace = _itemPositions[i].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
                i++;
            }
            _maxExtraVisibleItemInViewPort = i - 1;
        }
        
        /// <summary>
        /// Calculate the content size in their respective direction based on the scrolling direction
        /// If the cell size is know we simply add all the cell sizes, spacing and padding
        /// If not we set the cell size as -1 as it will be calculated once the cell comes into view
        /// </summary>
        protected override void CalculateContentSize()
        {
            base.CalculateContentSize();
            
            var contentSizeDelta = viewport.sizeDelta;
            contentSizeDelta[_axis] = 0;

            for (var i = 0; i < _itemsCount; i++)
            {
                if (!_dataSource.IsCellSizeKnown)
                {
                    contentSizeDelta[_axis] += _itemPositions[i].cellSize[_axis];
                }
                else
                {
                    var cellSize = _itemPositions[i].cellSize;
                    cellSize[_axis] = _dataSource.GetCellSize(i);
                    _itemPositions[i].SetSize(cellSize);
                    contentSizeDelta[_axis] += cellSize[_axis];
                }
            }

            contentSizeDelta[_axis] += _spacing[_axis] * (_itemsCount - 1);

            if (vertical)
            {
                contentSizeDelta.y += _padding.top + _padding.bottom;
                _layoutElement.preferredHeight = contentSizeDelta.y;
            }
            else
            {
                contentSizeDelta.x += _padding.right + _padding.left;
                _layoutElement.preferredWidth = contentSizeDelta.x;
            }

            content.sizeDelta = contentSizeDelta;
        }
        
        /// <summary>
        /// This function sets the position of the item whether its new or retrieved from pool based on its index and the previous item index
        /// The current index position is the previous item position + previous item height
        /// or the previous item position - current item height
        /// </summary>
        /// <param name="rect">rect of the item which position will be set</param>
        /// <param name="newIndex">index of the item that needs its position set</param>
        protected override void SetCellAxisPosition(RectTransform rect, int newIndex)
        {
            base.SetCellAxisPosition(rect, newIndex);
            
            var newItemPosition = rect.anchoredPosition;
            // figure out where the prev cell position was
            if (newIndex == 0)
            {
                if (vertical)
                {
                    if (_reverseDirection)
                    {
                        newItemPosition.y = _padding.bottom;
                    }
                    else
                    {
                        newItemPosition.y = -_padding.top;
                    }
                }
                else
                {
                    if (_reverseDirection)
                    {
                        newItemPosition.x = -_padding.right;
                    }
                    else
                    {
                        newItemPosition.x = _padding.left;
                    }
                }
            }
            else
            {
                var verticalSign = (vertical ? -1 : 1) * (_reverseDirection ? -1 : 1);
                newItemPosition[_axis] = verticalSign * _itemPositions[newIndex - 1].absBottomRightPosition[_axis] + verticalSign * _spacing[_axis];
            }

            rect.anchoredPosition = newItemPosition;
            _itemPositions[newIndex].SetPosition(newItemPosition);
        }
        
        /// <summary>
        /// This function calculates the cell size if its unknown by forcing a Layout rebuild
        /// if it is known we just get the cell size
        /// then calculating the new content size based on the old cell size if it was set previously
        /// </summary>
        /// <param name="rect">rect of the cell which the size will be calculated for</param>
        /// <param name="index">cell index which the size will be calculated for</param>
        protected override void CalculateCellAxisSize(RectTransform rect, int index)
        {
            base.CalculateCellAxisSize(rect, index);
            
            var newCellSize = _itemPositions[index].cellSize;
            var oldCellSize = newCellSize[_axis];

            if (!_dataSource.IsCellSizeKnown)
            {
                ForceLayoutRebuild(index);
                newCellSize[_axis] = rect.rect.size[_axis];
            }
            else
            {
                newCellSize[_axis] = _dataSource.GetCellSize(index);
            }

            // get difference in cell size if its size has changed
            _itemPositions[index].SetSize(newCellSize);
            
            var contentSize = content.sizeDelta;
            contentSize[_axis] += newCellSize[_axis] - oldCellSize;

            if (vertical)
            {
                _layoutElement.preferredHeight = contentSize.y;
            }
            else
            {
                _layoutElement.preferredWidth = contentSize.x;
            }

            content.sizeDelta = contentSize;
        }

        protected override void CalculateNonAxisSizePosition(RectTransform rect, int cellIndex)
        {
            base.CalculateNonAxisSizePosition(rect, cellIndex);
            
            var forceSize = false;
            // expand item width if it's in a vertical layout group and the conditions are satisfied
            if (vertical && _childForceExpand)
            {
                var itemSize = rect.sizeDelta;
                itemSize.x = content.rect.width;
                if (!_dataSource.IgnoreContentPadding(cellIndex))
                {
                    itemSize.x -= _padding.right + _padding.left;
                }

                rect.sizeDelta = itemSize;
                forceSize = true;
            }

            // expand item height if it's in a horizontal layout group and the conditions are satisfied
            else if (!vertical && _childForceExpand)
            {
                var itemSize = rect.sizeDelta;
                itemSize.y = content.rect.height;
                if (!_dataSource.IgnoreContentPadding(cellIndex))
                {
                    itemSize.y -= _padding.top + _padding.bottom;
                }

                rect.sizeDelta = itemSize;
                forceSize = true;
            }

            // get content size without padding
            var contentSize = content.rect.size;
            var contentSizeWithoutPadding = contentSize;
            contentSizeWithoutPadding.x -= _padding.right + _padding.left;
            contentSizeWithoutPadding.y -= _padding.top + _padding.bottom;

            // set position of cell based on layout alignment
            // we check for multiple conditions together since the content is made to fit the items, so they only move in one axis in each different scroll direction
            var rectSize = rect.rect.size;
            var itemSizeSmallerThanContent = rectSize[_axis] < contentSizeWithoutPadding[_axis];
            if (itemSizeSmallerThanContent || forceSize)
            {
                var itemPosition = rect.anchoredPosition;
                if (vertical)
                {
                    var rightPadding = _reverseDirection ? _padding.left : _padding.right;
                    var leftPadding = _reverseDirection ? _padding.right : _padding.left;
                    if (_dataSource.IgnoreContentPadding(cellIndex))
                    {
                        rightPadding = 0;
                        leftPadding = 0;
                    }

                    if (_childAlignment == TextAnchor.LowerCenter || _childAlignment == TextAnchor.MiddleCenter || _childAlignment == TextAnchor.UpperCenter)
                    {
                        itemPosition.x = (leftPadding + (contentSize.x - rectSize.x) - rightPadding) / 2f;
                    }
                    else if (_childAlignment == TextAnchor.LowerRight || _childAlignment == TextAnchor.MiddleRight || _childAlignment == TextAnchor.UpperRight)
                    {
                        itemPosition.x = contentSize.x - rectSize.x - rightPadding;
                    }
                    else
                    {
                        itemPosition.x = leftPadding;
                    }
                }
                else
                {
                    var topPadding = _reverseDirection ? _padding.bottom : _padding.top;
                    var bottomPadding = _reverseDirection ? _padding.top : _padding.bottom;
                    if (_dataSource.IgnoreContentPadding(cellIndex))
                    {
                        topPadding = 0;
                        bottomPadding = 0;
                    }
                    
                    if (_childAlignment == TextAnchor.MiddleLeft || _childAlignment == TextAnchor.MiddleCenter || _childAlignment == TextAnchor.MiddleRight)
                    {
                        itemPosition.y = -(topPadding + (contentSize.y - rectSize.y) - bottomPadding) / 2f;
                    }
                    else if (_childAlignment == TextAnchor.LowerLeft || _childAlignment == TextAnchor.LowerCenter || _childAlignment == TextAnchor.LowerRight)
                    {
                        itemPosition.y = -(contentSize.y - rectSize.y - bottomPadding);
                    }
                    else
                    {
                        itemPosition.y = -topPadding;
                    }
                }
                rect.anchoredPosition = itemPosition;
            }
        }
        
        public override void ReloadData(bool reloadAllItems = false)
        {
            base.ReloadData(reloadAllItems);
            CalculateNewMinMaxItemsAfterReloadCell();
            RefreshAfterReload(reloadAllItems);
        }

        /// <summary>
        /// this removes all items that are not needed after item reload if _itemsCount has been reduced
        /// </summary>
        /// <param name="itemDiff">the amount of items that have been deleted</param>
        protected override void RemoveExtraItems(int itemDiff)
        {
            base.RemoveExtraItems(itemDiff);
            
            for (var i = _itemsCount; i < _itemsCount + itemDiff; i++)
            {
                if (_visibleItems.ContainsKey(i))
                {
                    HideCellAtIndex(i);
                }
            }
            _maxVisibleItemInViewPort = Mathf.Max(0, _maxVisibleItemInViewPort - itemDiff);
            _maxExtraVisibleItemInViewPort = Mathf.Min(_itemsCount - 1, _maxVisibleItemInViewPort + _extraItemsVisible);
        }
        
        /// <summary>
        /// Checks if cells need to be hidden, shown, instantiated after a cell is reloaded and its size changes
        /// </summary>
        private void CalculateNewMinMaxItemsAfterReloadCell()
        {
            // figure out the new _minVisibleItemInViewPort && _maxVisibleItemInViewPort
            GetContentBounds();
            var newMinVisibleItemInViewPortSet = false;
            var newMinVisibleItemInViewPort = 0;
            var newMaxVisibleItemInViewPort = 0;
            foreach (var item in _visibleItems)
            {
                var itemPosition = _itemPositions[item.Key];
                if (itemPosition.absBottomRightPosition[_axis] >= _contentTopLeftCorner[_axis] && !newMinVisibleItemInViewPortSet)
                {
                    newMinVisibleItemInViewPort = item.Key;
                    newMinVisibleItemInViewPortSet = true; // this boolean is needed as all items in the view port will satisfy the above condition, and we only need the first one
                }

                if (itemPosition.absTopLeftPosition[_axis] <= _contentBottomRightCorner[_axis])
                {
                    newMaxVisibleItemInViewPort = item.Key;
                }
            }

            var newMinExtraVisibleItemInViewPort = Mathf.Max (0, newMinVisibleItemInViewPort - _extraItemsVisible);
            var newMaxExtraVisibleItemInViewPort = Mathf.Min (_itemsCount - 1, newMaxVisibleItemInViewPort + _extraItemsVisible);
            if (newMaxExtraVisibleItemInViewPort < _maxExtraVisibleItemInViewPort)
            {
                for (var i = newMaxExtraVisibleItemInViewPort + 1; i <= _maxExtraVisibleItemInViewPort; i++)
                {
                    ShowHideCellsAtIndex(i, false);
                }

                _maxVisibleItemInViewPort = newMaxVisibleItemInViewPort;
                _maxExtraVisibleItemInViewPort = newMaxExtraVisibleItemInViewPort;
            }
            else
            {
                // here we initialize cells instead of using ShowCellAtIndex because we don't know much viewport space is left
                InitializeCells(_maxExtraVisibleItemInViewPort + 1);
            }
            
            if (newMinExtraVisibleItemInViewPort > _minExtraVisibleItemInViewPort)
            {
                for (var i = _minExtraVisibleItemInViewPort; i < newMinExtraVisibleItemInViewPort; i++)
                {
                    ShowHideCellsAtIndex(i, false);
                }
            }
            else
            {
                for (var i = _minExtraVisibleItemInViewPort - 1; i >= newMinExtraVisibleItemInViewPort; i--)
                {
                    ShowHideCellsAtIndex(i, true);
                }
            }

            _minVisibleItemInViewPort = newMinVisibleItemInViewPort;
            _minExtraVisibleItemInViewPort = newMinExtraVisibleItemInViewPort;
        }

        protected override void ReloadCellInternal(int cellIndex, string reloadTag = "", bool reloadCellData = false, bool isReloadingAllData = false)
        {
            base.ReloadCellInternal(cellIndex, reloadTag, reloadCellData, isReloadingAllData);
            SetCellSizeWithPositionAfterReload(_visibleItems[cellIndex], cellIndex, isReloadingAllData);
        }

        /// <summary>
        /// Sets the cell new size and position after reloading, if cell size changed, recalculate all the cells that follow
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="cellIndex"></param>
        /// <param name="isReloadingAllData"></param>
        private void SetCellSizeWithPositionAfterReload(Item cell, int cellIndex, bool isReloadingAllData)
        {
            var oldSize = _itemPositions[cellIndex].cellSize[_axis];
            CalculateNonAxisSizePosition(cell.transform, cellIndex);
            CalculateCellAxisSize(cell.transform, cellIndex);
            SetCellAxisPosition(cell.transform, cellIndex);
            
            // no need to call this while reloading data, since ReloadData will call it after reloading cells
            // calling it while reload data will add unneeded redundancy
            if (!isReloadingAllData)
            {
                // no need to call CalculateNewMinMaxItemsAfterReloadCell if content moved since it will be handled in Update
                var contentMoved = RecalculateFollowingCells(cellIndex, oldSize);
                if (!contentMoved)
                    CalculateNewMinMaxItemsAfterReloadCell();
            }
        }
        
        /// <summary>
        /// Sets the positions of all cells of index + 1
        /// Persists content position to avoid sudden jumps if a cell size changes
        /// </summary>
        /// <param name="cellIndex">index of cell to start calculate following cells from</param>
        /// <param name="oldSize">old cell size used to offset content position with</param>
        /// <returns></returns>
        private bool RecalculateFollowingCells(int cellIndex, float oldSize)
        {
            // need to adjust all the cells position after cellIndex 
            var startingCellToAdjustPosition = cellIndex + 1;
            for (var i = startingCellToAdjustPosition; i <= _maxExtraVisibleItemInViewPort; i++)
                SetCellAxisPosition(_visibleItems[i].transform, i);

            if (_isAnimating)
                return true;
            
            var contentPosition = content.anchoredPosition;
            var contentMoved = false;
            var oldContentPosition = contentPosition[_axis];
            if (cellIndex < _minExtraVisibleItemInViewPort)
            {
                // this is a very special case as items reloaded at the top or right will have a different bottomRight position
                // and since we are here at the item, if we don't manually set the position of the content, it will seem as the content suddenly shifted and disorient the user
                contentPosition[_axis] = _itemPositions[cellIndex].absBottomRightPosition[_axis];
                
                // set the normalized position as well, because why not
                // (viewMin - (itemPosition - contentSize)) / (contentSize - viewSize)
                // var viewportRect = viewport.rect;
                // var contentRect = content.rect;
                // var viewPortBounds = new Bounds(viewportRect.center, viewportRect.size);
                // var newNormalizedPosition = (viewPortBounds.min[_axis] - (_itemPositions[cellIndex].bottomRightPosition[_axis] - contentRect.size[_axis])) / (contentRect.size[_axis] - viewportRect.size[_axis]);
                // SetNormalizedPosition(newNormalizedPosition, _axis);
            }
            else if (_minExtraVisibleItemInViewPort <= cellIndex && _minVisibleItemInViewPort > cellIndex)
            {
                contentPosition[_axis] -= (oldSize - _itemPositions[cellIndex].cellSize[_axis]) * (_reverseDirection ? -1 : 1);
            }
            
            var contentPositionDiff = Mathf.Abs(contentPosition[_axis] - oldContentPosition);
            if (contentPositionDiff > 0)
                contentMoved = true;

            if (contentMoved)
            {
                content.anchoredPosition = contentPosition;
                // this is important since the scroll rect will likely be dragging, and it will cause a jump
                // this only took me 6 hours to figure out :(
                m_ContentStartPosition = contentPosition;
            }

            return contentMoved;
        }

        /// <summary>
        /// Call the Show, Hide functions
        /// </summary>
        /// <param name="newIndex">current index of item we need to show</param>
        /// <param name="show">show or hide current cell</param>
        internal override void ShowHideCellsAtIndex(int newIndex, bool show)
        {
            base.ShowHideCellsAtIndex(newIndex, show);

            if (show)
            {
                ShowCellAtIndex(newIndex);
            }
            else
            {
                HideCellAtIndex(newIndex);
            }
        }
        
        protected override void HideItemsAtTopLeft()
        {
            base.HideItemsAtTopLeft();
            
            if (_minVisibleItemInViewPort < _itemsCount - 1 && _contentTopLeftCorner[_axis] >= _itemPositions[_minVisibleItemInViewPort].absBottomRightPosition[_axis])
            {
                var itemToHide = _minVisibleItemInViewPort - _extraItemsVisible;
                _minVisibleItemInViewPort++;
                if (itemToHide > -1)
                {
                    _minExtraVisibleItemInViewPort++;
                    ShowHideCellsAtIndex(itemToHide, false);
                }
            }
        }
        
        protected override void ShowItemsAtBottomRight()
        {
            base.ShowItemsAtBottomRight();
            
            if (_maxVisibleItemInViewPort < _itemsCount - 1 && _contentBottomRightCorner[_axis] > _itemPositions[_maxVisibleItemInViewPort].absBottomRightPosition[_axis] + _spacing[_axis])
            {
                _maxVisibleItemInViewPort++;
                var itemToShow = _maxVisibleItemInViewPort + _extraItemsVisible;
                if (itemToShow < _itemsCount)
                {
                    _maxExtraVisibleItemInViewPort = itemToShow;
                    ShowHideCellsAtIndex(itemToShow, true);
                }
            }
        }

        protected override void HideItemsAtBottomRight()
        {
            base.HideItemsAtBottomRight();
            
            if (_maxVisibleItemInViewPort > 0 && _contentBottomRightCorner[_axis] <= _itemPositions[_maxVisibleItemInViewPort].absTopLeftPosition[_axis])
            {
                var itemToHide = _maxVisibleItemInViewPort + _extraItemsVisible;
                _maxVisibleItemInViewPort--;
                if (itemToHide < _itemsCount)
                {
                    _maxExtraVisibleItemInViewPort--;
                    ShowHideCellsAtIndex(itemToHide, false);
                }
            }
        }
        
        protected override void ShowItemsAtTopLeft()
        {
            base.ShowItemsAtTopLeft();
            
            if (_minVisibleItemInViewPort > 0 && _contentTopLeftCorner[_axis] < _itemPositions[_minVisibleItemInViewPort].absTopLeftPosition[_axis] - _spacing[_axis])
            {
                _minVisibleItemInViewPort--;
                var itemToShow = _minVisibleItemInViewPort - _extraItemsVisible;
                if (itemToShow > -1)
                {
                    _minExtraVisibleItemInViewPort = itemToShow;
                    ShowHideCellsAtIndex(itemToShow, true);
                }
            }
        }

        public override void ScrollToTopRight()
        {
            base.ScrollToTopRight();
            StartCoroutine(ScrollToTargetNormalisedPosition((vertical ? 1 : 0) * (_reverseDirection ? 0 : 1)));
        }
    }
}