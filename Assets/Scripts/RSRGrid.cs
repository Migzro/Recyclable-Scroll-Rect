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
        
        private int _maxGridItemsInAxis;
        private Vector2 _gridLayoutPadding;
        
        public override void ResetData()
        {
            _gridLayoutPadding = Vector2.zero;
            base.ResetData();
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
            _maxGridItemsInAxis = 0;
            contentSizeDelta[_axis] = 0;
            
            // we consider all cell sizes the same in grid
            _maxGridItemsInAxis = Mathf.CeilToInt(_itemsCount / (float)_gridConstraintCount);
            contentSizeDelta[_axis] = _maxGridItemsInAxis * _gridCellSize[_axis];

            for (var i = 0; i < _itemsCount; i++)
                _itemPositions[i].SetSize(_gridCellSize);
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
        }
        
        /// <summary>
        /// Calculates the grid layout padding which offsets each element in the grid based on the padding and anchors set in GridLayout
        /// </summary>
        protected override void CalculatePadding()
        {
            base.CalculatePadding();
            
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
            int xIndexInGrid;
            int yIndexInGrid;
            
            if (_gridConstraint == GridLayoutGroup.Constraint.FixedRowCount)
            {
                if (_gridStartAxis == GridLayoutGroup.Axis.Vertical)
                {
                    xIndexInGrid = Mathf.FloorToInt(newIndex / (float) _gridConstraintCount);
                    yIndexInGrid = newIndex % _gridConstraintCount;
                }
                else
                {
                    // TODO: Reversed Grid Code
                    xIndexInGrid = newIndex % _maxGridItemsInAxis;
                    yIndexInGrid = Mathf.FloorToInt(newIndex / (float) _maxGridItemsInAxis);
                }
            }
            else
            {
                if (_gridStartAxis == GridLayoutGroup.Axis.Vertical)
                {
                    // TODO: Reversed Grid Code
                    xIndexInGrid = Mathf.FloorToInt(newIndex / (float) _maxGridItemsInAxis);
                    yIndexInGrid = newIndex % _maxGridItemsInAxis;
                }
                else
                {
                    xIndexInGrid = newIndex % _gridConstraintCount;
                    yIndexInGrid = Mathf.FloorToInt(newIndex / (float) _gridConstraintCount);
                }
            }

            if (_reverseDirection)
            {
                if (_gridConstraint == GridLayoutGroup.Constraint.FixedRowCount && _gridStartAxis == GridLayoutGroup.Axis.Vertical)
                {
                    newItemPosition.x = -_gridLayoutPadding.x - xIndexInGrid * _itemPositions[newIndex].cellSize[0] - _spacing[0] * xIndexInGrid;
                    newItemPosition.y = -_gridLayoutPadding.y - yIndexInGrid * _itemPositions[newIndex].cellSize[1] - _spacing[1] * yIndexInGrid;
                }
                else if (_gridConstraint == GridLayoutGroup.Constraint.FixedColumnCount && _gridStartAxis == GridLayoutGroup.Axis.Horizontal)
                {
                    newItemPosition.x = _gridLayoutPadding.x + xIndexInGrid * _itemPositions[newIndex].cellSize[0] + _spacing[0] * xIndexInGrid;
                    newItemPosition.y = _gridLayoutPadding.y + yIndexInGrid * _itemPositions[newIndex].cellSize[1] + _spacing[1] * yIndexInGrid;
                }
            }
            else
            {
                newItemPosition.x = _gridLayoutPadding.x + xIndexInGrid * _itemPositions[newIndex].cellSize[0] + _spacing[0] * xIndexInGrid;
                newItemPosition.y = -_gridLayoutPadding.y - yIndexInGrid * _itemPositions[newIndex].cellSize[1] - _spacing[1] * yIndexInGrid;
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
            rect.sizeDelta = _gridCellSize;
        }
        
        /// <summary>
        /// check if grid has enough space to initialize items in
        /// </summary>
        /// <param name="cellIndex">cell index</param>
        /// <returns></returns>
        protected override bool CheckInitializeCellsExtraConditions (int cellIndex)
        {
            base.CheckInitializeCellsExtraConditions(cellIndex);
            
            if (cellIndex == 0 || cellIndex < _itemsCount)
                return true;

            if ((cellIndex + 1) % _gridConstraintCount != 0)
                return true;

            return false;
        }
        
        /// <summary>
        /// Used to determine which cells will be shown or hidden in case its a grid layout since we need to show more than one cell depending on the grid configuration
        /// if it's not a grid layout, just call the Show, Hide functions
        /// </summary>
        /// <param name="newIndex">current index of item we need to show</param>
        /// <param name="show">show or hide current cell</param>
        /// <param name="gridLayoutPage">used to determine if we are showing/hiding a cell in after the most visible/hidden one or before the least visible/hidden one</param>
        internal override void ShowHideCellsAtIndex(int newIndex, bool show, GridLayoutPage gridLayoutPage)
        {
            base.ShowHideCellsAtIndex(newIndex, show, gridLayoutPage);
            
            var indices = new List<int>();
            if (_gridConstraint == GridLayoutGroup.Constraint.FixedRowCount && _gridStartAxis == GridLayoutGroup.Axis.Vertical
                || _gridConstraint == GridLayoutGroup.Constraint.FixedColumnCount && _gridStartAxis == GridLayoutGroup.Axis.Horizontal)
            {
                if (gridLayoutPage == GridLayoutPage.After)
                {
                    // equation to get the highest multiple of newIndex where _gridConstraintCount is the multiple
                    var maxItemToShow = _gridConstraintCount * Mathf.FloorToInt((float) newIndex / _gridConstraintCount) + _gridConstraintCount;
                    for (var i = newIndex; i < maxItemToShow; i++)
                    {
                        if (i < _itemsCount)
                            indices.Add(i);
                    }
                }
                else if (gridLayoutPage == GridLayoutPage.Before)
                {
                    // equation to get the lowest multiple of newIndex where _gridConstraintCount is the multiple
                    var minItemToShow = _gridConstraintCount * Mathf.FloorToInt((float) newIndex / _gridConstraintCount);  
                    for (var i = newIndex; i >= minItemToShow; i--)
                        indices.Add(i);
                }
                else if (gridLayoutPage == GridLayoutPage.Single)
                    indices.Add(newIndex);
            }
            else
            {
                // TODO: Reversed Grid Code
                if (gridLayoutPage == GridLayoutPage.Single)
                    indices.Add(newIndex);
                else if (newIndex < _maxGridItemsInAxis)
                {
                    for (var i = 0; i < _gridConstraintCount; i++)
                    {
                        var cellIndex = newIndex + i * _maxGridItemsInAxis;
                        if (cellIndex < _itemsCount)
                            indices.Add(cellIndex);
                    }
                }
            }

            for (var i = 0; i < indices.Count; i++)
            {
                if (show && !_visibleItems.ContainsKey(indices[i]))
                    ShowCellAtIndex(indices[i]);
                else if (!show && _visibleItems.ContainsKey(indices[i]))
                    HideCellAtIndex(indices[i]);
            }
        }
    }
}