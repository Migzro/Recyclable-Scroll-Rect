using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclableSR
{
    public class RSRGrid : RSRBase
    {
        [SerializeField] private Vector2 _gridCellSize;
        [SerializeField] private GridLayoutGroup.Axis _gridStartAxis;
        [SerializeField] private GridLayoutGroup.Constraint _gridConstraint;
        [SerializeField] private int _gridConstraintCount;
        [SerializeField] private int _extraVisibleRowsColumns;
        
        private int _maxGridItemsInAxis;
        private int[,] _gridIndices;
        private Dictionary<int, Vector2Int> _gridIndicesLookup;
        private Vector2 _gridLayoutPadding;
        
        /// <summary>
        /// Calculate the content size in their respective direction based on the scrolling direction
        /// If the cell size is known, we simply add all the cell sizes, spacing and padding
        /// If not we set the cell size as -1 as it will be calculated once the cell comes into view
        /// </summary>
        protected override void CalculateContentSize()
        {
            base.CalculateContentSize();
            
            var contentSizeDelta = viewport.sizeDelta;
            _maxGridItemsInAxis = 0;
            contentSizeDelta[_axis] = 0;

            if (_gridConstraint == GridLayoutGroup.Constraint.Flexible)
            {
                // Calculate how many items can fit in the current scroll view opposite axis, this is our _gridConstraintCount
                var contentSizeWithoutPadding = viewport.rect.size;
                contentSizeWithoutPadding.x -= _padding.right + _padding.left;
                contentSizeWithoutPadding.y -= _padding.top + _padding.bottom;

                if (vertical)
                {
                    _gridConstraintCount = Mathf.FloorToInt(contentSizeWithoutPadding.x / (_gridCellSize.x + _spacing.x));
                }
                else
                {
                    _gridConstraintCount = Mathf.FloorToInt(contentSizeWithoutPadding.y / (_gridCellSize.y + _spacing.y));
                }
            }
            _maxGridItemsInAxis = Mathf.CeilToInt(_itemsCount / (float)_gridConstraintCount);
            contentSizeDelta[_axis] = _maxGridItemsInAxis * _gridCellSize[_axis];

            for (var i = 0; i < _itemsCount; i++)
            {
                _itemPositions[i].SetSize(_gridCellSize);
            }
            contentSizeDelta[_axis] += _spacing[_axis] * (_maxGridItemsInAxis - 1);
            
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
            var itemsCountInOppositeAxis = Mathf.CeilToInt(_itemsCount / (float)_gridConstraintCount);
            if (vertical)
            {
                _gridIndices = new int[_gridConstraintCount, itemsCountInOppositeAxis];
            }
            else
            {
                _gridIndices = new int[itemsCountInOppositeAxis, _gridConstraintCount];
            }
            
            for (var i = 0; i < _gridIndices.GetLength(0); i++)
            {
                for (var j = 0; j < _gridIndices.GetLength(1); j++)
                {
                    _gridIndices[i, j] = -1;
                }
            }
            _gridIndicesLookup = new Dictionary<int, Vector2Int>();

            for (var i = 0; i < _itemsCount; i++)
            {
                int xIndexInGrid;
                int yIndexInGrid;
                if (horizontal)
                {
                    if (_gridStartAxis == GridLayoutGroup.Axis.Vertical)
                    {
                        xIndexInGrid = Mathf.FloorToInt(i / (float)_gridConstraintCount);
                        yIndexInGrid = i % _gridConstraintCount;
                    }
                    else
                    {
                        // TODO: Reversed Grid Code
                        xIndexInGrid = i % _maxGridItemsInAxis;
                        yIndexInGrid = Mathf.FloorToInt(i / (float)_maxGridItemsInAxis);
                    }
                }
                else
                {
                    if (_gridStartAxis == GridLayoutGroup.Axis.Vertical)
                    {
                        // TODO: Reversed Grid Code
                        xIndexInGrid = Mathf.FloorToInt(i / (float)_maxGridItemsInAxis);
                        yIndexInGrid = i % _maxGridItemsInAxis;
                    }
                    else
                    {
                        xIndexInGrid = i % _gridConstraintCount;
                        yIndexInGrid = Mathf.FloorToInt(i / (float)_gridConstraintCount);
                    }
                }
                
                // if (_reverseDirection)
                // {
                //     // TODO: this should be horizontal
                //     if (_gridConstraint == GridLayoutGroup.Constraint.FixedRowCount && _gridStartAxis == GridLayoutGroup.Axis.Vertical)
                //     {
                //         newItemPosition.x = -_gridLayoutPadding.x - xIndexInGrid * _itemPositions[newIndex].cellSize[0] - _spacing[0] * xIndexInGrid;
                //         newItemPosition.y = -_gridLayoutPadding.y - yIndexInGrid * _itemPositions[newIndex].cellSize[1] - _spacing[1] * yIndexInGrid;
                //     }
                //     // TODO: this should be vertical
                //     else if (_gridConstraint == GridLayoutGroup.Constraint.FixedColumnCount && _gridStartAxis == GridLayoutGroup.Axis.Horizontal)
                //     {
                //         newItemPosition.x = _gridLayoutPadding.x + xIndexInGrid * _itemPositions[newIndex].cellSize[0] + _spacing[0] * xIndexInGrid;
                //         newItemPosition.y = _gridLayoutPadding.y + yIndexInGrid * _itemPositions[newIndex].cellSize[1] + _spacing[1] * yIndexInGrid;
                //     }
                // }
                // else
                // {
                //     newItemPosition.x = _gridLayoutPadding.x + xIndexInGrid * _itemPositions[newIndex].cellSize[0] + _spacing[0] * xIndexInGrid;
                //     newItemPosition.y = -_gridLayoutPadding.y - yIndexInGrid * _itemPositions[newIndex].cellSize[1] - _spacing[1] * yIndexInGrid;
                // }

                _gridIndicesLookup[i] = new Vector2Int(xIndexInGrid, yIndexInGrid);
                // Debug.LogError(xIndexInGrid + " " + yIndexInGrid + " " + i);
                _gridIndices[xIndexInGrid, yIndexInGrid] = i;
            }

            // Debugging code
            // for (var j = 0; j < _gridIndices.GetLength(1); j++)
            // {
            //     var stringToShow = "";
            //     for (var i = 0; i < _gridIndices.GetLength(0); i++)
            //     {
            //         stringToShow += _gridIndices[i, j] + " ";
            //     }
            //     Debug.LogError(stringToShow);
            // }
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
                    _gridLayoutPadding.x = leftPadding + (contentSize.x - (_gridCellSize.x * _gridConstraintCount) - (_spacing.x * (_gridConstraintCount - 1))) / 2 - rightPadding;
                }
                else if (_childAlignment == TextAnchor.LowerRight || _childAlignment == TextAnchor.MiddleRight || _childAlignment == TextAnchor.UpperRight)
                {
                    _gridLayoutPadding.x = contentSize.x - _gridCellSize.x - rightPadding;
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
                    _gridLayoutPadding.y = topPadding + (contentSize.y - (_gridCellSize.y * _gridConstraintCount) - (_spacing.y * (_gridConstraintCount - 1))) / 2 - bottomPadding;
                }
                else if (_childAlignment == TextAnchor.LowerLeft || _childAlignment == TextAnchor.LowerCenter || _childAlignment == TextAnchor.LowerRight)
                {
                    _gridLayoutPadding.y = contentSize.y - _gridCellSize.y - bottomPadding;
                }
                else
                {
                    _gridLayoutPadding.y = topPadding;
                }
                _gridLayoutPadding.x = _reverseDirection ? _padding.right : _padding.left;
            }
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
            var gridIndex = _gridIndicesLookup[newIndex];
            newItemPosition.x = _gridLayoutPadding.x + gridIndex.x * _itemPositions[newIndex].cellSize[0] + _spacing[0] * gridIndex.x;
            newItemPosition.y = -_gridLayoutPadding.y - gridIndex.y * _itemPositions[newIndex].cellSize[1] - _spacing[1] * gridIndex.y;
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
            rect.sizeDelta = _gridCellSize;
        }
        
        /// <summary>
        /// We consider each starting item in the row/column the only one that needs to be initialized.
        /// this is because we do not care about the following cells, they are initialized by ShowHideCellsAtIndex, which just completes the row/column.
        /// this is to simplify the calculations required for the different configurations of the grid.
        /// The grid indices remain constant, what changes is the cellIndex that the gridIndex holds
        /// </summary>
        /// <param name="startIndex">the starting cell index on which we want initialized</param>
        protected override void InitializeCells(int startIndex = 0)
        {
            base.InitializeCells(startIndex);

            // get the starting grid index of the startIndex in that specific row/column. It's always the opposite of the current axis
            var current2DIndex = _gridIndicesLookup[startIndex];
            current2DIndex[1 - _axis] = 0;
            
            // use the current starting row or column index since we base all our calculations on the top or left indices
            var currentStartItemInRowColumn = _gridIndices[current2DIndex.x, current2DIndex.y];
            
            GetContentBounds();
            var contentHasSpace = currentStartItemInRowColumn == 0 || _itemPositions[currentStartItemInRowColumn].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
            var extraRowsColumnsInitialized = contentHasSpace ? 0 : (_maxExtraVisibleItemInViewPort - _maxVisibleItemInViewPort) / _gridConstraintCount;

            while ((contentHasSpace || extraRowsColumnsInitialized < _extraVisibleRowsColumns))
            {
                ShowHideCellsAtIndex(currentStartItemInRowColumn, true, GridLayoutPage.After);
                
                if (!contentHasSpace)
                    extraRowsColumnsInitialized++;
                else
                    _maxVisibleItemInViewPort = currentStartItemInRowColumn;
            
                contentHasSpace = _itemPositions[currentStartItemInRowColumn].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
                _maxExtraVisibleItemInViewPort = currentStartItemInRowColumn;
                
                // get the first item in the next row or column that needs to be initialized
                if (vertical)
                    current2DIndex.y++;
                else
                    current2DIndex.x++;

                if (current2DIndex.x >= _gridIndices.GetLength(0) || current2DIndex.y >= _gridIndices.GetLength(1))
                    break;
                
                currentStartItemInRowColumn = _gridIndices[current2DIndex.x, current2DIndex.y];
            }
        }
        
        protected override void HideItemsAtTopLeft()
        {
            base.HideItemsAtTopLeft();
            
            if (_minVisibleItemInViewPort < _itemsCount - _gridConstraintCount - 1 && _contentTopLeftCorner[_axis] >= _itemPositions[_minVisibleItemInViewPort].absBottomRightPosition[_axis])
            {
                var itemToHide = _minVisibleItemInViewPort - (_extraVisibleRowsColumns * _gridConstraintCount);
                _minVisibleItemInViewPort += _gridConstraintCount;
                if (itemToHide > -1)
                {
                    _minExtraVisibleItemInViewPort += _gridConstraintCount;
                    ShowHideCellsAtIndex(itemToHide, false, GridLayoutPage.Before);
                }
            }
        }
        
        protected override void ShowItemsAtBottomRight()
        {
            base.ShowItemsAtBottomRight();
            
            if (_maxVisibleItemInViewPort < _itemsCount - _gridConstraintCount - 1 && _contentBottomRightCorner[_axis] > _itemPositions[_maxVisibleItemInViewPort].absBottomRightPosition[_axis] + _spacing[_axis])
            {
                _maxVisibleItemInViewPort += _gridConstraintCount;
                var itemToShow = _maxVisibleItemInViewPort + (_extraVisibleRowsColumns * _gridConstraintCount);
                if (itemToShow < _itemsCount)
                {
                    _maxExtraVisibleItemInViewPort = itemToShow;
                    ShowHideCellsAtIndex(itemToShow, true, GridLayoutPage.After);
                }
            }
        }

        protected override void HideItemsAtBottomRight()
        {
            base.HideItemsAtBottomRight();
            
            if (_maxVisibleItemInViewPort > 0 && _contentBottomRightCorner[_axis] <= _itemPositions[_maxVisibleItemInViewPort].absTopLeftPosition[_axis])
            {
                var itemToHide = _maxVisibleItemInViewPort + (_extraVisibleRowsColumns * _gridConstraintCount);
                _maxVisibleItemInViewPort -= _gridConstraintCount;
                if (itemToHide < _itemsCount)
                {
                    _maxExtraVisibleItemInViewPort -= _gridConstraintCount;
                    ShowHideCellsAtIndex(itemToHide, false, GridLayoutPage.After);
                }
            }
        }
        
        protected override void ShowItemsAtTopLeft()
        {
            base.ShowItemsAtTopLeft();
            
            if (_minVisibleItemInViewPort > 0 && _contentTopLeftCorner[_axis] < _itemPositions[_minVisibleItemInViewPort].absTopLeftPosition[_axis] - _spacing[_axis])
            {
                _minVisibleItemInViewPort -= _gridConstraintCount;
                var itemToShow = _minVisibleItemInViewPort - (_extraVisibleRowsColumns * _gridConstraintCount);
                if (itemToShow > -1)
                {
                    _minExtraVisibleItemInViewPort = itemToShow;
                    ShowHideCellsAtIndex(itemToShow, true, GridLayoutPage.Before);
                }
            }
        }
        
        /// <summary>
        /// Used to determine which cells will be shown or hidden in case it's a grid layout since we need to show more than one cell depending on the grid configuration
        /// </summary>
        /// <param name="newIndex">current index of item we need to show</param>
        /// <param name="show">show or hide current cell</param>
        /// <param name="gridLayoutPage">used to determine if we are showing/hiding a cell in after the most visible/hidden one or before the least visible/hidden one</param>
        internal override void ShowHideCellsAtIndex(int newIndex, bool show, GridLayoutPage gridLayoutPage)
        {
            base.ShowHideCellsAtIndex(newIndex, show, gridLayoutPage);
            
            var indices = new List<int>();
            // if (gridLayoutPage == GridLayoutPage.After)
            // {
            //     if (_gridStartAxis == GridLayoutGroup.Axis.Horizontal)
            //     {
            //         // equation to get the highest multiple of newIndex where _gridConstraintCount is the multiple
            //         var maxItemToShow = _gridConstraintCount * Mathf.FloorToInt((float)newIndex / _gridConstraintCount) + _gridConstraintCount;
            //         for (var i = newIndex; i < maxItemToShow; i++)
            //         {
            //             if (i < _itemsCount)
            //             {
            //                 indices.Add(i);
            //             }
            //         }
            //     }
            // }
            // else if (gridLayoutPage == GridLayoutPage.Before)
            // {
            //     if (_gridStartAxis == GridLayoutGroup.Axis.Horizontal)
            //     {
            //         // equation to get the lowest multiple of newIndex where _gridConstraintCount is the multiple
            //         var minItemToShow = _gridConstraintCount * Mathf.FloorToInt((float)newIndex / _gridConstraintCount);
            //         for (var i = newIndex; i >= minItemToShow; i--)
            //         {
            //             indices.Add(i);
            //         }
            //     }
            // }
            // else if (gridLayoutPage == GridLayoutPage.Single)
            // {
            //     indices.Add(newIndex);
            // }
            var itemIndex = _gridIndicesLookup[newIndex];
            if (vertical)
            {
                // show or hide all items in a single row
                for (var i = 0; i < _gridConstraintCount; i++)
                {
                    var indexValue = _gridIndices[i, itemIndex.y]; 
                    if (indexValue != -1)
                        indices.Add(indexValue);
                }
            }
            else
            {
                // show or hide all items in a single column
                for (var i = 0; i < _gridConstraintCount; i++)
                {
                    var indexValue = _gridIndices[itemIndex.x, i]; 
                    if (indexValue != -1)
                        indices.Add(indexValue);
                }
            }

            // if (_gridStartAxis == GridLayoutGroup.Axis.Horizontal)
            // {
            //     // equation to get the highest multiple of newIndex where _gridConstraintCount is the multiple
            //     var maxItemToShow = _gridConstraintCount * Mathf.FloorToInt((float)newIndex / _gridConstraintCount) + _gridConstraintCount;
            //     for (var i = newIndex; i < maxItemToShow; i++)
            //     {
            //         if (i < _itemsCount)
            //         {
            //             indices.Add(i);
            //         }
            //     }
            // }
            // else if (_gridStartAxis == GridLayoutGroup.Axis.Vertical)
            // {
            //     // get the x index of the item in the grid
            //     var xIndexInGrid = Mathf.FloorToInt(newIndex / (float) _maxGridItemsInAxis);
            //         
            //     // need to get multiples of _maxItemsInGrid for the items needed to be added
            //     // for example if we show item 0 and _maxItemsInGrid is 15, then we need to add 0, 15, 30, 45, and so on till we either reach _maxGridItemsInAxis or _gridConstraintCount
            //     for (var i = 0; i < _gridConstraintCount - xIndexInGrid; i++)
            //     {
            //         var indexToAdd = newIndex + (i * _maxGridItemsInAxis);
            //         if (indexToAdd < _itemsCount)
            //         {
            //             indices.Add(indexToAdd);
            //         }
            //     }
            // }

            for (var i = 0; i < indices.Count; i++)
            {
                if (show && !_visibleItems.ContainsKey(indices[i]))
                {
                    ShowCellAtIndex(indices[i]);
                }
                else if (!show && _visibleItems.ContainsKey(indices[i]))
                {
                    HideCellAtIndex(indices[i]);
                }
            }
        }
    }
}