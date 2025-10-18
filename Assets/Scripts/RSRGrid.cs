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
        [SerializeField] private GridLayoutGroup.Corner _gridStartCorner;
        [SerializeField] private int _extraRowsColumnsVisible;

        private IGridSource _gridSource;
        private Grid _grid;
        private Vector2 _gridLayoutPadding;
        private int _originalItemsCount;

        protected override bool ReachedMinItemInViewPort => _minVisibleRowColumnInViewPort == 0;
        protected override bool ReachedMaxItemInViewPort => _maxVisibleRowColumnInViewPort == (_grid.maxGridItemsInAxis - 1) * _gridConstraintCount;

        protected override void Initialize()
        {
            _gridSource = (IGridSource)_dataSource;
            base.Initialize();
        }

        protected override void ResetVariables()
        {
            base.ResetVariables();

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
            
            _grid = new Grid(_itemsCount, _gridConstraintCount, vertical, _gridStartAxis, _gridStartCorner);
            // set the items count to the fill the entire grid with items
            _itemsCount = _grid.width * _grid.height;
            CalculateGridPadding();
        }

        /// <summary>
        /// Calculate the content size in their respective direction based on the scrolling direction
        /// If the item size is known, we simply add all the item sizes, spacing and padding
        /// If not we set the item size as -1 as it will be calculated once the item comes into view
        /// </summary>
        protected override void CalculateContentSize()
        {
            var contentSizeDelta = viewport.sizeDelta;
            contentSizeDelta[_axis] = (_grid.maxGridItemsInAxis * _gridItemSize[_axis]) + (_spacing[_axis] * (_grid.maxGridItemsInAxis - 1));

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
        }
        
        /// <summary>
        /// get the index of the item
        /// </summary>
        /// <returns></returns>
        protected override int GetActualItemIndex(int itemIndex)
        {
            return _grid.GetActualItemIndex(itemIndex);
        }
        
        /// <summary>
        /// Calculates the grid layout padding which offsets each element in the grid based on the padding and anchors set in GridLayout
        /// </summary>
        private void CalculateGridPadding()
        {
            _gridLayoutPadding = Vector2.zero;
            
            // get content size without padding
            var contentSize = content.rect.size;
            var contentSizeWithoutPadding = contentSize;
            contentSizeWithoutPadding.x -= _padding.right + _padding.left;
            contentSizeWithoutPadding.y -= _padding.top + _padding.bottom;
            
            if (vertical)
            {
                if (_childAlignment == TextAnchor.LowerCenter || _childAlignment == TextAnchor.MiddleCenter || _childAlignment == TextAnchor.UpperCenter)
                {
                    _gridLayoutPadding.x = _padding.left + (contentSize.x - (_gridItemSize.x * _gridConstraintCount) - (_spacing.x * (_gridConstraintCount - 1))) / 2 - _padding.right;
                }
                else if (_childAlignment == TextAnchor.LowerRight || _childAlignment == TextAnchor.MiddleRight || _childAlignment == TextAnchor.UpperRight)
                {
                    _gridLayoutPadding.x = contentSize.x - _gridItemSize.x - _padding.right;
                }
                else
                {
                    _gridLayoutPadding.x = _padding.left;
                }
                _gridLayoutPadding.y = _padding.top;
            }
            else
            {
                if (_childAlignment == TextAnchor.MiddleLeft || _childAlignment == TextAnchor.MiddleCenter || _childAlignment == TextAnchor.MiddleRight)
                {
                    _gridLayoutPadding.y = _padding.top + (contentSize.y - (_gridItemSize.y * _gridConstraintCount) - (_spacing.y * (_gridConstraintCount - 1))) / 2 - _padding.bottom;
                }
                else if (_childAlignment == TextAnchor.LowerLeft || _childAlignment == TextAnchor.LowerCenter || _childAlignment == TextAnchor.LowerRight)
                {
                    _gridLayoutPadding.y = contentSize.y - _gridItemSize.y - _padding.bottom;
                }
                else
                {
                    _gridLayoutPadding.y = _padding.top;
                }
                _gridLayoutPadding.x = _padding.left;
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
            var newItemPosition = rect.anchoredPosition;
            var gridIndex = _grid.To2dIndex(newIndex);
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
            // use the current starting row or column index since we base all our calculations on the top or left indices
            var current2DIndex = _grid.To2dIndex(startIndex);
            var currentStartItemInRowColumn = current2DIndex[_axis] * _gridConstraintCount;
            
            GetContentBounds();
            var contentHasSpace = currentStartItemInRowColumn == 0 || _itemPositions[currentStartItemInRowColumn].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
            var extraRowsColumnsInitialized = contentHasSpace ? 0 : (_maxExtraVisibleRowColumnInViewPort - _maxVisibleRowColumnInViewPort) / _gridConstraintCount;

            while ((contentHasSpace || extraRowsColumnsInitialized < _extraRowsColumnsVisible) && currentStartItemInRowColumn < _itemsCount)
            {
                ShowHideRowsColumnsAtIndex(currentStartItemInRowColumn, true);
                
                if (!contentHasSpace)
                    extraRowsColumnsInitialized++;
                else
                    _maxVisibleRowColumnInViewPort = currentStartItemInRowColumn;
            
                contentHasSpace = _itemPositions[currentStartItemInRowColumn].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
                _maxExtraVisibleRowColumnInViewPort = currentStartItemInRowColumn;
                
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
        /// this removes all items that are not needed after item reload if _itemsCount has been reduced
        /// </summary>
        /// <param name="itemDiff">the amount of items that have been deleted</param>
        protected override void RemoveExtraItems(int itemDiff)
        {
            base.RemoveExtraItems(itemDiff);

            var lastItem2dIndex = _grid.To2dIndex(_itemsCount - 1);
            var lastItemFlatIndex = lastItem2dIndex[_axis] * _gridConstraintCount;
            if (lastItemFlatIndex < _maxVisibleRowColumnInViewPort)
            {
                _maxVisibleRowColumnInViewPort = lastItemFlatIndex;
            }
            
            if (lastItemFlatIndex < _maxExtraVisibleRowColumnInViewPort)
            {
                // get the amount of extra rows/columns possible based on the new item count
                var extraRowsColumnsPossible = Mathf.Min((lastItemFlatIndex - _maxVisibleRowColumnInViewPort) / _gridConstraintCount, _extraRowsColumnsVisible);
                if (vertical)
                {
                    _maxExtraVisibleRowColumnInViewPort = _maxVisibleRowColumnInViewPort + (extraRowsColumnsPossible * _gridConstraintCount);
                }
                else
                {
                    _maxExtraVisibleRowColumnInViewPort = _maxVisibleRowColumnInViewPort + extraRowsColumnsPossible;
                }
            }
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
            var startingRowColumn = _grid.To2dIndex(_minExtraVisibleRowColumnInViewPort)[_axis];
            var endingRowColumn = _grid.To2dIndex(_maxExtraVisibleRowColumnInViewPort)[_axis];
            for (var i = startingRowColumn; i <= endingRowColumn; i++)
            {
                for (var j = 0; j < _gridConstraintCount; j++)
                {
                    var flatIndex = j + (i * _gridConstraintCount);
                    var indexActualValue = vertical ? _grid.GetActualItemIndex(j, i) : _grid.GetActualItemIndex(i, j);
                    var isVisible = _visibleItems.ContainsKey(flatIndex);
                    var shouldBeVisible = indexActualValue != -1;
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
            for (var i = 0; i < _gridConstraintCount; i++)
            {
                var currentFlatIndex = newIndex + i;
                var indexValue = _grid.GetActualItemIndex(currentFlatIndex);
                if (indexValue != -1)
                {
                    indices.Add(currentFlatIndex);
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
            if (_minVisibleRowColumnInViewPort < _itemsCount - _gridConstraintCount - 1 && _contentTopLeftCorner[_axis] >= _itemPositions[_minVisibleRowColumnInViewPort].absBottomRightPosition[_axis])
            {
                var itemToHide = _minVisibleRowColumnInViewPort - (_extraRowsColumnsVisible * _gridConstraintCount);
                _minVisibleRowColumnInViewPort += _gridConstraintCount;
                if (itemToHide > -1)
                {
                    _minExtraVisibleRowColumnInViewPort += _gridConstraintCount;
                    ShowHideRowsColumnsAtIndex(itemToHide, false);
                }
            }
        }
        
        protected override void ShowItemsAtBottomRight()
        {
            if (_maxVisibleRowColumnInViewPort < _itemsCount - _gridConstraintCount - 1 && _contentBottomRightCorner[_axis] > _itemPositions[_maxVisibleRowColumnInViewPort].absBottomRightPosition[_axis] + _spacing[_axis])
            {
                _maxVisibleRowColumnInViewPort += _gridConstraintCount;
                var itemToShow = _maxVisibleRowColumnInViewPort + (_extraRowsColumnsVisible * _gridConstraintCount);
                if (itemToShow < _itemsCount)
                {
                    _maxExtraVisibleRowColumnInViewPort = itemToShow;
                    ShowHideRowsColumnsAtIndex(itemToShow, true);
                }
            }
        }

        protected override void HideItemsAtBottomRight()
        {
            if (_maxVisibleRowColumnInViewPort > 0 && _contentBottomRightCorner[_axis] <= _itemPositions[_maxVisibleRowColumnInViewPort].absTopLeftPosition[_axis])
            {
                var itemToHide = _maxVisibleRowColumnInViewPort + (_extraRowsColumnsVisible * _gridConstraintCount);
                _maxVisibleRowColumnInViewPort -= _gridConstraintCount;
                if (itemToHide < _itemsCount)
                {
                    _maxExtraVisibleRowColumnInViewPort -= _gridConstraintCount;
                    ShowHideRowsColumnsAtIndex(itemToHide, false);
                }
            }
        }
        
        protected override void ShowItemsAtTopLeft()
        {
            if (_minVisibleRowColumnInViewPort > 0 && _contentTopLeftCorner[_axis] < _itemPositions[_minVisibleRowColumnInViewPort].absTopLeftPosition[_axis] - _spacing[_axis])
            {
                _minVisibleRowColumnInViewPort -= _gridConstraintCount;
                var itemToShow = _minVisibleRowColumnInViewPort - (_extraRowsColumnsVisible * _gridConstraintCount);
                if (itemToShow > -1)
                {
                    _minExtraVisibleRowColumnInViewPort = itemToShow;
                    ShowHideRowsColumnsAtIndex(itemToShow, true);
                }
            }
        }
    }
}