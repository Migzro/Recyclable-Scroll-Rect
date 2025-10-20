// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RSR
{
    public enum ItemsAlignment
    {
        LeftOrUp = 0,
        Center,
        RightOrDown
    }
    
    public readonly struct Item
    {
        public IItem item { get; }
        public RectTransform transform { get; }

        public Item(IItem item, RectTransform transform)
        {
            this.item = item;
            this.transform = transform;
        }
    }

    public class ItemPosition
    {
        public Vector2 topLeftPosition{ get; private set; }
        public Vector2 absTopLeftPosition{ get; private set; }
        public Vector2 absBottomRightPosition{ get; private set; }
        public Vector2 itemSize{ get; private set; }
        public bool positionSet { get; private set; }
        public bool sizeSet { get; private set; }

        public ItemPosition()
        {
            absTopLeftPosition = Vector2.zero;
            absBottomRightPosition = Vector2.zero;
            itemSize = Vector2.zero;
            positionSet = false;
            sizeSet = false;
        }

        public void SetPosition(Vector2 position)
        {
            topLeftPosition = position;
            absTopLeftPosition = position.Abs();
            positionSet = true;

            if (sizeSet)
                absBottomRightPosition = absTopLeftPosition + itemSize;
        }
        
        public void SetPositionAndSize (Vector2 position, Vector2 size)
        {
            itemSize = size;
            absTopLeftPosition = position.Abs();
            absBottomRightPosition = absTopLeftPosition + itemSize;
            positionSet = true;
            sizeSet = true;
        }

        public void SetSize(Vector2 size)
        {
            itemSize = size;
            absBottomRightPosition = absTopLeftPosition + itemSize;
            sizeSet = true;
        }

        public override string ToString()
        {
            return $"Top Left Position {absTopLeftPosition}, Bottom Right Position {absBottomRightPosition}, Size {itemSize}";
        }
    }

    public class Grid
    {
        private int[,] _gridActualIndices;
        private Dictionary<int, Vector2Int> _grid2dIndices;
        private readonly GridLayoutGroup.Axis _gridStartAxis;
        private readonly int _realItemsCount;
        private readonly int _gridConstraintCount;
        private readonly bool _vertical;
        private readonly GridLayoutGroup.Corner _gridStartCorner;

        public int width { get; private set; }
        public int height { get; private set; }
        public int maxGridItemsInAxis { get; private set; }

        public Grid(int itemsCount, int gridConstraintCount, bool vertical, GridLayoutGroup.Axis gridStartAxis, GridLayoutGroup.Corner gridStartCorner)
        {
            _realItemsCount = itemsCount;
            _gridConstraintCount = gridConstraintCount;
            _vertical = vertical;
            _gridStartAxis = gridStartAxis;
            _gridStartCorner = gridStartCorner;

            CalculateWidthWithHeight();
            BuildIndices();
        }

        private void CalculateWidthWithHeight()
        {
            // calculate the grid width and height, _maxGridItemsInAxis is how many rows are needed in a vertical layout or columns in a horizontal one
            maxGridItemsInAxis = Mathf.CeilToInt(_realItemsCount / (float)_gridConstraintCount);
            width = _vertical ? _gridConstraintCount : maxGridItemsInAxis;
            height = _vertical ? maxGridItemsInAxis : _gridConstraintCount;
        }

        /// <summary>
        /// we consider the grid size as width*height as opposed to _realItemsCount
        /// we set all the extra items actual indices as -1
        /// this helps when placing the items based on the grid configuration
        /// </summary>
        private void BuildIndices()
        {
            _gridActualIndices = new int[width, height];
            _grid2dIndices = new Dictionary<int, Vector2Int>();

            var allItemsInGridCount = width * height;
            for (var i = 0; i < allItemsInGridCount; i++)
            {
                Set2dIndex(i);
                
                int xIndexInGrid;
                int yIndexInGrid;
                if (_gridStartAxis == GridLayoutGroup.Axis.Vertical)
                {
                    if (_gridStartCorner == GridLayoutGroup.Corner.LowerRight)
                    {
                        xIndexInGrid = (width - 1) - (i / height);
                        yIndexInGrid = (height - 1) - (i % height);
                    }
                    else if (_gridStartCorner == GridLayoutGroup.Corner.LowerLeft)
                    {
                        xIndexInGrid = i / height;
                        yIndexInGrid = (height - 1) - (i % height);
                    }
                    else if (_gridStartCorner == GridLayoutGroup.Corner.UpperRight)
                    {
                        xIndexInGrid = (width - 1) - (i / height);
                        yIndexInGrid = i % height;
                    }
                    else // UpperLeft
                    {
                        xIndexInGrid = i / height;
                        yIndexInGrid = i % height;
                    }
                }
                else
                {
                    if (_gridStartCorner == GridLayoutGroup.Corner.LowerRight)
                    {
                        xIndexInGrid = (width - 1) - (i % width);
                        yIndexInGrid = (height - 1) - (i / width);
                    }
                    else if (_gridStartCorner == GridLayoutGroup.Corner.LowerLeft)
                    {
                        xIndexInGrid = i % width;
                        yIndexInGrid = (height - 1) - (i / width);
                    }
                    else if (_gridStartCorner == GridLayoutGroup.Corner.UpperRight)
                    {
                        xIndexInGrid = (width - 1) - (i % width);
                        yIndexInGrid = i / width;
                    }
                    else // UpperLeft
                    {
                        xIndexInGrid = i % width;
                        yIndexInGrid = i / width;
                    }
                }

                if (i < _realItemsCount)
                {
                    _gridActualIndices[xIndexInGrid, yIndexInGrid] = i;
                }
                else
                {
                    _gridActualIndices[xIndexInGrid, yIndexInGrid] = -1;   
                }
            }
        }
        
        private void Set2dIndex(int index)
        {
            int x;
            int y;
            if (_vertical)
            {
                x = index % width;
                y = index / width;
            }
            else
            {
                x = index / height;
                y = index % height;
            }

            _grid2dIndices[index] = new Vector2Int(x, y);
        }

        public int GetActualItemIndex(int flatItemIndex)
        {
            var grid2dIndex = To2dIndex(flatItemIndex);
            return _gridActualIndices[grid2dIndex.x, grid2dIndex.y];
        }
        
        public int GetActualItemIndex(int x, int y)
        {
            return _gridActualIndices[x, y];
        }

        public Vector2Int To2dIndex(int index)
        {
            return _grid2dIndices[index];
        }
    }
}