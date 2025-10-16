using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclableSR
{
    public class RSRGrid : RSRBase
    {
        [SerializeField] private Vector2 _gridItemSize;
        [SerializeField] private GridLayoutGroup.Axis _gridStartAxis;
        [SerializeField] private GridLayoutGroup.Constraint _gridConstraint;
        [SerializeField] private int _gridConstraintCount;

        private IGridSource _gridSource;
        private int _originalItemsCount;
        private int _extraRowsColumnsVisible;
        private int _maxGridItemsInAxis;
        private int _gridWidth;
        private int _gridHeight;
        private int[,] _gridIndices;
        private Vector2 _gridLayoutPadding;
        
        protected override bool ReachedMinItemInViewPort => _minVisibleItemInViewPort == 0;
        protected override bool ReachedMaxItemInViewPort => _maxVisibleItemInViewPort == (_maxGridItemsInAxis - 1) * _gridConstraintCount;

        protected override void Initialize()
        {
            _gridSource = (IGridSource)_dataSource;
            base.Initialize();
        }

        protected override void ResetVariables()
        {
            base.ResetVariables();

            _extraRowsColumnsVisible = _gridSource.ExtraRowsColumnsVisible;
            if (_gridConstraint == GridLayoutGroup.Constraint.Flexible)
            {
                // Calculate how many items can fit in the current scroll view opposite axis, this is our _gridConstraintCount
                var contentSizeWithoutPadding = viewport.rect.size;
                contentSizeWithoutPadding.x -= _padding.right + _padding.left;
                contentSizeWithoutPadding.y -= _padding.top + _padding.bottom;

                if (vertical)
                {
                    _gridConstraintCount = Mathf.FloorToInt(contentSizeWithoutPadding.x / (_gridItemSize.x + _spacing.x));
                }
                else
                {
                    _gridConstraintCount = Mathf.FloorToInt(contentSizeWithoutPadding.y / (_gridItemSize.y + _spacing.y));
                }
            }
            
            // calculate the grid width and height, _maxGridItemsInAxis is how many rows are needed in a vertical layout or columns in a horizontal one
            _maxGridItemsInAxis = Mathf.CeilToInt(_itemsCount / (float)_gridConstraintCount);
            _gridWidth = vertical ? _gridConstraintCount : _maxGridItemsInAxis;
            _gridHeight = vertical ? _maxGridItemsInAxis : _gridConstraintCount;
            
            // This is done since we consider a grid to have all the items in its width and height regardless of how many items are in the actual data source
            // this helps configure different configurations of the grid
            _originalItemsCount = _itemsCount;
            _itemsCount = _gridWidth * _gridHeight;
        }

        /// <summary>
        /// Calculate the content size in their respective direction based on the scrolling direction
        /// If the item size is known, we simply add all the item sizes, spacing and padding
        /// If not we set the item size as -1 as it will be calculated once the item comes into view
        /// </summary>
        protected override void CalculateContentSize()
        {
            base.CalculateContentSize();

            var contentSizeDelta = viewport.sizeDelta;
            contentSizeDelta[_axis] = (_maxGridItemsInAxis * _gridItemSize[_axis]) + (_spacing[_axis] * (_maxGridItemsInAxis - 1));

            for (var i = 0; i < _itemsCount; i++)
            {
                _itemPositions[i].SetSize(_gridItemSize);
            }
            
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
            BuildItems2DIndices();
        }
        
        /// <summary>
        /// Sets every item position in the grid based on the grid configuration
        /// </summary>
        private void BuildItems2DIndices()
        {
            _gridIndices = new int[_gridWidth, _gridHeight];
            
            for (var i = 0; i < _gridWidth; i++)
            {
                for (var j = 0; j < _gridHeight; j++)
                {
                    _gridIndices[i, j] = -1;
                }
            }
            
            for (var i = 0; i < _originalItemsCount; i++)
            {
                int xIndexInGrid;
                int yIndexInGrid;
                if (_gridStartAxis == GridLayoutGroup.Axis.Vertical)
                {
                    xIndexInGrid = Mathf.FloorToInt(i / (float)_gridHeight);
                    yIndexInGrid = i % _gridHeight;
                }
                else
                {
                    // TODO: Reversed Grid Code
                    xIndexInGrid = i % _gridWidth;
                    yIndexInGrid = Mathf.FloorToInt(i / (float)_gridWidth);
                }
                
                // if (_reverseDirection)
                // {
                //     // TODO: this should be horizontal
                //     if (_gridConstraint == GridLayoutGroup.Constraint.FixedRowCount && _gridStartAxis == GridLayoutGroup.Axis.Vertical)
                //     {
                //         newItemPosition.x = -_gridLayoutPadding.x - xIndexInGrid * _itemPositions[newIndex].itemSize[0] - _spacing[0] * xIndexInGrid;
                //         newItemPosition.y = -_gridLayoutPadding.y - yIndexInGrid * _itemPositions[newIndex].itemSize[1] - _spacing[1] * yIndexInGrid;
                //     }
                //     // TODO: this should be vertical
                //     else if (_gridConstraint == GridLayoutGroup.Constraint.FixedColumnCount && _gridStartAxis == GridLayoutGroup.Axis.Horizontal)
                //     {
                //         newItemPosition.x = _gridLayoutPadding.x + xIndexInGrid * _itemPositions[newIndex].itemSize[0] + _spacing[0] * xIndexInGrid;
                //         newItemPosition.y = _gridLayoutPadding.y + yIndexInGrid * _itemPositions[newIndex].itemSize[1] + _spacing[1] * yIndexInGrid;
                //     }
                // }
                // else
                // {
                //     newItemPosition.x = _gridLayoutPadding.x + xIndexInGrid * _itemPositions[newIndex].itemSize[0] + _spacing[0] * xIndexInGrid;
                //     newItemPosition.y = -_gridLayoutPadding.y - yIndexInGrid * _itemPositions[newIndex].itemSize[1] - _spacing[1] * yIndexInGrid;
                // }

                // Debug.LogError(xIndexInGrid + " " + yIndexInGrid + " " + i);
                _gridIndices[xIndexInGrid, yIndexInGrid] = i;
            }

            // Debugging code
            // for (var j = 0; j < _gridHeight; j++)
            // {
            //     var stringToShow = "";
            //     for (var i = 0; i < _gridWidth; i++)
            //     {
            //         stringToShow += _gridIndices[i, j] + " ";
            //     }
            //     Debug.LogError(stringToShow);
            // }
        }
        
        /// <summary>
        /// get the index of the item
        /// </summary>
        /// <returns></returns>
        protected override int GetActualItemIndex(int itemIndex)
        {
            var grid2dIndex = Get2dIndex(itemIndex);
            return _gridIndices[grid2dIndex.x, grid2dIndex.y];
        }
        
        /// <summary>
        /// Calculates the grid layout padding which offsets each element in the grid based on the padding and anchors set in GridLayout
        /// </summary>
        protected override void CalculatePadding()
        {
            base.CalculatePadding();
            
            _gridLayoutPadding = Vector2.zero;
            
            // get content size without padding
            var contentSize = content.rect.size;
            var contentSizeWithoutPadding = contentSize;
            contentSizeWithoutPadding.x -= _padding.right + _padding.left;
            contentSizeWithoutPadding.y -= _padding.top + _padding.bottom;
            
            if (vertical)
            {
                var rightPadding = _reverseDirection ? _padding.left : _padding.right;
                var leftPadding = _reverseDirection ? _padding.right : _padding.left;

                if (_childAlignment == TextAnchor.LowerCenter || _childAlignment == TextAnchor.MiddleCenter || _childAlignment == TextAnchor.UpperCenter)
                {
                    _gridLayoutPadding.x = leftPadding + (contentSize.x - (_gridItemSize.x * _gridConstraintCount) - (_spacing.x * (_gridConstraintCount - 1))) / 2 - rightPadding;
                }
                else if (_childAlignment == TextAnchor.LowerRight || _childAlignment == TextAnchor.MiddleRight || _childAlignment == TextAnchor.UpperRight)
                {
                    _gridLayoutPadding.x = contentSize.x - _gridItemSize.x - rightPadding;
                }
                else
                {
                    _gridLayoutPadding.x = leftPadding;
                }
                _gridLayoutPadding.y = _reverseDirection ? _padding.bottom : _padding.top;
            }
            else
            {
                var topPadding = _reverseDirection ? _padding.bottom : _padding.top;
                var bottomPadding = _reverseDirection ? _padding.top : _padding.bottom;
                
                if (_childAlignment == TextAnchor.MiddleLeft || _childAlignment == TextAnchor.MiddleCenter || _childAlignment == TextAnchor.MiddleRight)
                {
                    _gridLayoutPadding.y = topPadding + (contentSize.y - (_gridItemSize.y * _gridConstraintCount) - (_spacing.y * (_gridConstraintCount - 1))) / 2 - bottomPadding;
                }
                else if (_childAlignment == TextAnchor.LowerLeft || _childAlignment == TextAnchor.LowerCenter || _childAlignment == TextAnchor.LowerRight)
                {
                    _gridLayoutPadding.y = contentSize.y - _gridItemSize.y - bottomPadding;
                }
                else
                {
                    _gridLayoutPadding.y = topPadding;
                }
                _gridLayoutPadding.x = _reverseDirection ? _padding.right : _padding.left;
            }
        }
        
        /// <summary>
        /// This function sets the position of the item whether its new or retrieved from pool based on its index
        /// it just sets the position of the grid items one after the other regardless of the data in each item
        /// </summary>
        /// <param name="rect">rect of the item which position will be set</param>
        /// <param name="newIndex">index of the item that needs its position set</param>
        protected override void SetItemAxisPosition(RectTransform rect, int newIndex)
        {
            base.SetItemAxisPosition(rect, newIndex);
            
            var newItemPosition = rect.anchoredPosition;
            var gridIndex = Get2dIndex(newIndex);
            newItemPosition.x = _gridLayoutPadding.x + gridIndex.x * _itemPositions[newIndex].itemSize[0] + _spacing[0] * gridIndex.x;
            newItemPosition.y = -_gridLayoutPadding.y - gridIndex.y * _itemPositions[newIndex].itemSize[1] - _spacing[1] * gridIndex.y;
            rect.anchoredPosition = newItemPosition;
            _itemPositions[newIndex].SetPosition(newItemPosition);
        }
        
        /// <summary>
        /// This function just sets the rect.sizeDelta of the grid
        /// </summary>
        /// <param name="rect">rect of the item which the size will be calculated for</param>
        /// <param name="index">item index which the size will be calculated for</param>
        protected override void CalculateItemAxisSize(RectTransform rect, int index)
        {
            base.CalculateItemAxisSize(rect, index);
            rect.sizeDelta = _gridItemSize;
        }
        
        /// <summary>
        /// We consider each starting item in the row/column the only one that needs to be initialized.
        /// this is because we do not care about the following items, they are initialized by ShowHideItemsAtIndex, which just completes the row/column.
        /// this is to simplify the calculations required for the different configurations of the grid.
        /// The grid indices remain constant, what changes is the itemIndex that the gridIndex holds
        /// </summary>
        /// <param name="startIndex">the starting item index on which we want initialized</param>
        protected override void InitializeItems(int startIndex = 0)
        {
            base.InitializeItems(startIndex);

            // use the current starting row or column index since we base all our calculations on the top or left indices
            var current2DIndex = Get2dIndex(startIndex);
            var currentStartItemInRowColumn = current2DIndex[_axis] * _gridConstraintCount;
            
            GetContentBounds();
            var contentHasSpace = currentStartItemInRowColumn == 0 || _itemPositions[currentStartItemInRowColumn].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
            var extraRowsColumnsInitialized = contentHasSpace ? 0 : (_maxExtraVisibleItemInViewPort - _maxVisibleItemInViewPort) / _gridConstraintCount;

            while ((contentHasSpace || extraRowsColumnsInitialized < _extraRowsColumnsVisible) && currentStartItemInRowColumn < _itemsCount)
            {
                ShowHideRowsColumnsAtIndex(currentStartItemInRowColumn, true);
                
                if (!contentHasSpace)
                    extraRowsColumnsInitialized++;
                else
                    _maxVisibleItemInViewPort = currentStartItemInRowColumn;
            
                contentHasSpace = _itemPositions[currentStartItemInRowColumn].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
                _maxExtraVisibleItemInViewPort = currentStartItemInRowColumn;
                
                // get the first item in the next row or column that needs to be initialized
                current2DIndex[_axis]++;
                currentStartItemInRowColumn = current2DIndex[_axis] * _gridConstraintCount;
            }
        }
        
        public override void ReloadData(bool reloadAllItems = false)
        {
            base.ReloadData(reloadAllItems);
            RefreshAfterReload(reloadAllItems);
        }

        /// <summary>
        /// we need to check all the visible items, and hide the ones that currently have an actual index (not -1)
        /// we need to check all the items that might need showing that are currently not showing and show them
        /// </summary>
        /// <param name="reloadAllItems"></param>
        protected override void RefreshAfterReload(bool reloadAllItems)
        {
            base.RefreshAfterReload(reloadAllItems);

            if (!reloadAllItems)
            {
                return;
            }

            // we start from the _minExtraVisibleItemInViewPort row/column till the _maxExtraVisibleItemInViewPort
            var indicesToShow = new List<int>();
            var indicesToHide = new List<int>();
            var startingRowColumn = Get2dIndex(_minExtraVisibleItemInViewPort)[_axis];
            var endingRowColumn = Get2dIndex(_maxExtraVisibleItemInViewPort)[_axis];
            for (var i = startingRowColumn; i <= endingRowColumn; i++)
            {
                for (var j = 0; j < _gridConstraintCount; j++)
                {
                    var flatIndex = j + (i * _gridConstraintCount);
                    var indexValue = vertical ? _gridIndices[j, i] : _gridIndices[i, j];
                    var isVisible = _visibleItems.ContainsKey(flatIndex);
                    var shouldBeVisible = indexValue != -1;
                    if (isVisible && !shouldBeVisible)
                    {
                        indicesToHide.Add(flatIndex);
                    }
                    else if (!isVisible && shouldBeVisible)
                    {
                        indicesToShow.Add(flatIndex);
                    }
                }
            }

            foreach (var index in indicesToHide)
            {
                HideItemAtIndex(index);
            }

            foreach (var index in indicesToShow)
            {
                ShowItemAtIndex(index);
            }
        }

        /// <summary>
        /// Used to determine which items will be shown or hidden in case it's a grid layout since we need to show more than one item depending on the grid configuration
        /// </summary>
        /// <param name="newIndex">current index of item we need to show</param>
        /// <param name="show">show or hide current item</param>
        private void ShowHideRowsColumnsAtIndex(int newIndex, bool show)
        {
            var indices = new List<int>(_gridConstraintCount);
            var item2dIndex = Get2dIndex(newIndex);

            for (var i = 0; i < _gridConstraintCount; i++)
            {
                int indexValue;
                int flatIndex;

                if (vertical)
                {
                    flatIndex = item2dIndex.y * _gridConstraintCount + i;
                    indexValue = _gridIndices[i, item2dIndex.y];
                }
                else
                {
                    flatIndex = item2dIndex.x * _gridConstraintCount + i;
                    indexValue = _gridIndices[item2dIndex.x, i];
                }

                if (indexValue != -1)
                {
                    indices.Add(flatIndex);
                }
            }

            foreach (var index in indices)
            {
                var isVisible = _visibleItems.ContainsKey(index);

                if (show && !isVisible)
                {
                    ShowItemAtIndex(index);
                }
                else if (!show && isVisible)
                {
                    HideItemAtIndex(index);
                }
            }
        }
        
        protected override void HideItemsAtTopLeft()
        {
            base.HideItemsAtTopLeft();
            
            if (_minVisibleItemInViewPort < _itemsCount - _gridConstraintCount - 1 && _contentTopLeftCorner[_axis] >= _itemPositions[_minVisibleItemInViewPort].absBottomRightPosition[_axis])
            {
                var itemToHide = _minVisibleItemInViewPort - (_extraRowsColumnsVisible * _gridConstraintCount);
                _minVisibleItemInViewPort += _gridConstraintCount;
                if (itemToHide > -1)
                {
                    _minExtraVisibleItemInViewPort += _gridConstraintCount;
                    ShowHideRowsColumnsAtIndex(itemToHide, false);
                }
            }
        }
        
        protected override void ShowItemsAtBottomRight()
        {
            base.ShowItemsAtBottomRight();
            
            if (_maxVisibleItemInViewPort < _itemsCount - _gridConstraintCount - 1 && _contentBottomRightCorner[_axis] > _itemPositions[_maxVisibleItemInViewPort].absBottomRightPosition[_axis] + _spacing[_axis])
            {
                _maxVisibleItemInViewPort += _gridConstraintCount;
                var itemToShow = _maxVisibleItemInViewPort + (_extraRowsColumnsVisible * _gridConstraintCount);
                if (itemToShow < _itemsCount)
                {
                    _maxExtraVisibleItemInViewPort = itemToShow;
                    ShowHideRowsColumnsAtIndex(itemToShow, true);
                }
            }
        }

        protected override void HideItemsAtBottomRight()
        {
            base.HideItemsAtBottomRight();
            
            if (_maxVisibleItemInViewPort > 0 && _contentBottomRightCorner[_axis] <= _itemPositions[_maxVisibleItemInViewPort].absTopLeftPosition[_axis])
            {
                var itemToHide = _maxVisibleItemInViewPort + (_extraRowsColumnsVisible * _gridConstraintCount);
                _maxVisibleItemInViewPort -= _gridConstraintCount;
                if (itemToHide < _itemsCount)
                {
                    _maxExtraVisibleItemInViewPort -= _gridConstraintCount;
                    ShowHideRowsColumnsAtIndex(itemToHide, false);
                }
            }
        }
        
        protected override void ShowItemsAtTopLeft()
        {
            base.ShowItemsAtTopLeft();
            
            if (_minVisibleItemInViewPort > 0 && _contentTopLeftCorner[_axis] < _itemPositions[_minVisibleItemInViewPort].absTopLeftPosition[_axis] - _spacing[_axis])
            {
                _minVisibleItemInViewPort -= _gridConstraintCount;
                var itemToShow = _minVisibleItemInViewPort - (_extraRowsColumnsVisible * _gridConstraintCount);
                if (itemToShow > -1)
                {
                    _minExtraVisibleItemInViewPort = itemToShow;
                    ShowHideRowsColumnsAtIndex(itemToShow, true);
                }
            }
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
                    HideItemAtIndex(i);
                }
            }
            
            var lastItem2dIndex = Get2dIndex(_itemsCount - 1);
            var lastItemFlatIndex = lastItem2dIndex[_axis] * _gridConstraintCount;
            if (lastItemFlatIndex < _maxVisibleItemInViewPort)
            {
                _maxVisibleItemInViewPort = lastItemFlatIndex;
            }
            
            if (lastItemFlatIndex < _maxExtraVisibleItemInViewPort)
            {
                // get the amount of extra rows/columns possible based on the new item count
                var extraRowsColumnsPossible = Mathf.Min((lastItemFlatIndex - _maxVisibleItemInViewPort) / _gridConstraintCount, _extraRowsColumnsVisible);
                if (vertical)
                {
                    _maxExtraVisibleItemInViewPort = _maxVisibleItemInViewPort + (extraRowsColumnsPossible * _gridConstraintCount);
                }
                else
                {
                    _maxExtraVisibleItemInViewPort = _maxVisibleItemInViewPort + extraRowsColumnsPossible;
                }
            }
        }

        /// <summary>
        /// get the 2d index from a 1d index
        /// </summary>
        /// <param name="index">1d index</param>
        /// <returns></returns>
        private Vector2Int Get2dIndex(int index)
        {
            int startX;
            int startY;
            if (vertical)
            {
                startX = index % _gridWidth;
                startY = index / _gridWidth;
            }
            else
            {
                startX = index / _gridHeight;
                startY = index % _gridHeight;
            }

            return new Vector2Int(startX, startY);
        }
    }
}