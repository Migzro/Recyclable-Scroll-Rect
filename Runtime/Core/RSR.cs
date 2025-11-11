// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;

namespace RecyclableScrollRect
{
    public class RSR : RSRBase
    {
        [SerializeField] private bool _childForceExpand;
        [SerializeField] private bool _reverseArrangement;
        [SerializeField] protected int _extraItemsVisible;
        
        private IRSRDataSource _rsrDataSource;

        protected override bool IsItemSizeKnown => _rsrDataSource.IsItemSizeKnown;
        protected override bool ReachedMinRowColumnInViewPort => _minVisibleRowColumnInViewPort == 0;
        protected override bool ReachedMaxRowColumnInViewPort => _maxVisibleRowColumnInViewPort == _itemsCount - 1;
        
        protected override void Initialize()
        {
            _rsrDataSource = (IRSRDataSource)_dataSource;
            base.Initialize();
        }
        
        /// <summary>
        /// get the index of the item
        /// </summary>
        /// <returns></returns>
        protected override int GetActualItemIndex(int itemIndex)
        {
            if (_reverseArrangement)
            {
                return _itemsCount - 1 - itemIndex;
            }
            return itemIndex;
        }

        protected override bool IsLastRowColumn(int itemIndex)
        {
            return itemIndex == _itemsCount - 1;
        }

        /// <summary>
        /// Initialize all items needed until the view port is filled
        /// extra visible items is an additional amount of items that can be shown to prevent showing an empty view port if the scrolling is too fast and the update function didn't show all the items
        /// that need to be shown
        /// </summary>
        /// <param name="startIndex">the starting item index on which we want initialized</param>
        protected override void InitializeItems(int startIndex = 0)
        {
            SetContentBounds();

            var contentHasSpace = startIndex == 0 || _itemPositions[startIndex - 1].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
            var extraItemsInitialized = contentHasSpace ? 0 : _maxExtraVisibleRowColumnInViewPort - _maxVisibleRowColumnInViewPort;
            for (var i = startIndex; (contentHasSpace || extraItemsInitialized < _extraItemsVisible) && i < _itemsCount; i++)
            {
                ShowItemAtIndex(i);
                if (!contentHasSpace)
                    extraItemsInitialized++;
                else
                    _maxVisibleRowColumnInViewPort = i;

                contentHasSpace = _itemPositions[i].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
                _maxExtraVisibleRowColumnInViewPort = i;
            }
        }
        
        /// <summary>
        /// Calculate the content size in their respective direction based on the scrolling direction
        /// If the item size is know we simply add all the item sizes, spacing and padding
        /// If not we set the item size as -1 as it will be calculated once the item comes into view
        /// </summary>
        protected override void CalculateContentSize()
        {
            var contentSizeDelta = viewport.sizeDelta;
            contentSizeDelta[_axis] = 0;

            for (var i = 0; i < _itemsCount; i++)
            {
                contentSizeDelta[_axis] += _itemPositions[i].itemSize[_axis];
            }
            contentSizeDelta[_axis] += _spacing[_axis] * (Mathf.Max(0, _itemsCount - 1));

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

        protected override void SetNonAxisSize(int itemIndex, RectTransform rect = null)
        {
            var itemPosition = _itemPositions[itemIndex];
            var newItemSize = itemPosition.itemSize;

            if (!itemPosition.nonAxisSizeSet)
            {
                if (_childForceExpand)
                {
                    if (vertical)
                    {
                        // expand item width if it's in a vertical scrollRect and the conditions are satisfied
                        newItemSize.x = content.rect.width;
                        if (!_dataSource.IgnoreContentPadding(itemIndex))
                        {
                            newItemSize.x -= _padding.right + _padding.left;
                        }
                    }
                    else if (!vertical)
                    {
                        // expand item height if it's in a horizontal scrollRect and the conditions are satisfied
                        newItemSize.y = content.rect.height;
                        if (!_dataSource.IgnoreContentPadding(itemIndex))
                        {
                            newItemSize.y -= _padding.top + _padding.bottom;
                        }
                    }
                }
                else
                {
                    newItemSize[1 - _axis] = _dataSource.GetItemPrototype(itemIndex).GetComponent<RectTransform>().sizeDelta[1 - _axis];
                }

                itemPosition.SetNonAxisSize(newItemSize);
            }

            if (rect != null)
            {
                rect.sizeDelta = newItemSize;
            }
        }

        /// <summary>
        /// This function sets the position of the item whether its new or retrieved from pool based on its index and the previous item index
        /// The current index position is the previous item position + previous item height
        /// or the previous item position - current item height
        /// </summary>
        /// <param name="itemIndex">index of the item that needs its position set</param>
        /// <param name="rect">RectTransform to set position for</param>
        protected override void SetItemPosition(int itemIndex, RectTransform rect = null)
        {
            var itemPosition = _itemPositions[itemIndex];
            if (!itemPosition.positionSet)
            {
                var newItemPosition = itemPosition.topLeftPosition;
                // figure out where the prev item position was
                if (itemIndex == 0)
                {
                    if (vertical)
                    {
                        newItemPosition.y = -_padding.top;
                    }
                    else
                    {
                        newItemPosition.x = _padding.left;
                    }
                }
                else
                {
                    var verticalSign = vertical ? -1 : 1;
                    newItemPosition[_axis] = verticalSign * _itemPositions[itemIndex - 1].absBottomRightPosition[_axis] + verticalSign * _spacing[_axis];
                }

                // Sets the vertical position in horizontal layout or the horizontal position in a vertical layout based on the padding of said layout
                var itemSize = itemPosition.itemSize;
                var contentSize = content.rect.size;
                var itemSizeSmallerThanContent = itemSize[1 - _axis] < contentSize[1 - _axis];
                if (itemSizeSmallerThanContent)
                {
                    if (vertical)
                    {
                        var rightPadding = _padding.right;
                        var leftPadding = _padding.left;
                        if (_dataSource.IgnoreContentPadding(itemIndex))
                        {
                            rightPadding = 0;
                            leftPadding = 0;
                        }

                        if (_itemsAlignment == ItemsAlignment.Center)
                        {
                            newItemPosition.x = (leftPadding + (contentSize.x - itemSize.x) - rightPadding) / 2f;
                        }
                        else if (_itemsAlignment == ItemsAlignment.RightOrDown)
                        {
                            newItemPosition.x = contentSize.x - itemSize.x - rightPadding;
                        }
                        else
                        {
                            newItemPosition.x = leftPadding;
                        }
                    }
                    else
                    {
                        var topPadding = _padding.top;
                        var bottomPadding = _padding.bottom;
                        if (_dataSource.IgnoreContentPadding(itemIndex))
                        {
                            topPadding = 0;
                            bottomPadding = 0;
                        }

                        if (_itemsAlignment == ItemsAlignment.Center)
                        {
                            newItemPosition.y = -(topPadding + (contentSize.y - itemSize.y) - bottomPadding) / 2f;
                        }
                        else if (_itemsAlignment == ItemsAlignment.RightOrDown)
                        {
                            newItemPosition.y = -(contentSize.y - itemSize.y - bottomPadding);
                        }
                        else
                        {
                            newItemPosition.y = -topPadding;
                        }
                    }
                }

                itemPosition.SetPosition(newItemPosition);
            }
            
            if (rect != null)
            {
                rect.anchoredPosition = itemPosition.topLeftPosition;
            }
        }
        
        /// <summary>
        /// This function calculates the item size if its unknown by forcing a Layout rebuild
        /// if it is known we just get the item size
        /// </summary>
        /// <param name="itemIndex">item index which the size will be calculated for</param>
        /// <param name="rect">RectTransform to set size for</param>
        protected override void SetItemSize(int itemIndex, RectTransform rect = null)
        {
            var itemPosition = _itemPositions[itemIndex];

            if (!itemPosition.sizeSet)
            {
                var newItemSize = itemPosition.itemSize;
                var oldItemSize = itemPosition.itemSize[_axis];

                if (!_rsrDataSource.IsItemSizeKnown)
                {
                    ForceLayoutRebuild(itemIndex);
                    newItemSize[_axis] = _visibleItems[itemIndex].transform.rect.size[_axis];

                    // set the content size since items size was not known at the time of the initialization
                    var contentSize = content.sizeDelta;
                    contentSize[_axis] += newItemSize[_axis] - oldItemSize;

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
                else
                {
                    newItemSize[_axis] = _rsrDataSource.GetItemSize(itemIndex);
                }

                itemPosition.SetSize(newItemSize);
            }
            
            if (rect != null)
            {
                rect.sizeDelta = itemPosition.itemSize;
            }
        }

        /// <summary>
        /// this removes all items that are not needed after item reload if _itemsCount has been reduced
        /// </summary>
        /// <param name="itemDiff">the amount of items that have been deleted</param>
        protected override void RemoveExtraItems(int itemDiff)
        {
            base.RemoveExtraItems(itemDiff);
            
            if (_itemsCount - 1 < _maxVisibleRowColumnInViewPort)
            {
                _maxVisibleRowColumnInViewPort = Mathf.Max(0, _itemsCount - 1);
            }
            if (_itemsCount - 1 < _maxExtraVisibleRowColumnInViewPort)
            {
                _maxExtraVisibleRowColumnInViewPort = Mathf.Min(Mathf.Max(0, _itemsCount - 1), _maxVisibleRowColumnInViewPort + _extraItemsVisible);
            }
        }
        
        /// <summary>
        /// Checks if items need to be hidden, shown, instantiated after an item is reloaded and its size changes
        /// </summary>
        protected override void RefreshAfterReload()
        {
            base.RefreshAfterReload();
            
            var newMinVisibleItemInViewPortSet = false;
            var newMinVisibleItemInViewPort = -1;
            var newMaxVisibleItemInViewPort = -1;
            var newMinExtraVisibleItemInViewPort = -1;
            var newMaxExtraVisibleItemInViewPort = -1;
            for (var i = 0; i < _itemPositions.Count; i++)
            {
                if (!_itemPositions[i].positionSet)
                {
                    continue;
                }

                var itemPosition = _itemPositions[i];
                if (!newMinVisibleItemInViewPortSet && itemPosition.absBottomRightPosition[_axis] >= _contentTopLeftCorner[_axis])
                {
                    newMinVisibleItemInViewPort = i;
                    newMinVisibleItemInViewPortSet = true;
                }

                if (itemPosition.absTopLeftPosition[_axis] <= _contentBottomRightCorner[_axis])
                {
                    newMaxVisibleItemInViewPort = i;
                }
            }

            if (newMinVisibleItemInViewPort >= 0)
            {
                newMinExtraVisibleItemInViewPort = Mathf.Clamp(newMinVisibleItemInViewPort - _extraItemsVisible, 0, Mathf.Max(0, _itemsCount - 1));
            }
            if (newMaxVisibleItemInViewPort >= 0)
            {
                newMaxExtraVisibleItemInViewPort = Mathf.Clamp(newMaxVisibleItemInViewPort + _extraItemsVisible, 0, Mathf.Max(0, _itemsCount - 1));
            }
            
            // // hide all items that are at the top, from _minExtraVisibleRowColumnInViewPort to newMinExtraVisibleItemInViewPort
            // for (var i = _minExtraVisibleRowColumnInViewPort; i < newMinExtraVisibleItemInViewPort; i++)
            // {
            //     if (_visibleItems.ContainsKey(i))
            //     {
            //         HideItemAtIndex(i);
            //     }
            // }
            
            // hide all the items that are the bottom, from newMaxExtraVisibleItemInViewPort to _maxExtraVisibleRowColumnInViewPort
            for (var i = newMaxExtraVisibleItemInViewPort + 1; i <= _maxExtraVisibleRowColumnInViewPort; i++)
            {
                if (_visibleItems.ContainsKey(i))
                {
                    HideItemAtIndex(i);
                }
            }
            
            // make sure all items from newMinExtraVisibleItemInViewPort till newMaxExtraVisibleItemInViewPort are visible
            for (var i = newMinExtraVisibleItemInViewPort; i <= newMaxExtraVisibleItemInViewPort; i++)
            {
                if (i >= 0 && i < _itemsCount && !_visibleItems.ContainsKey(i))
                {
                    ShowItemAtIndex(i);
                }
            }
            
            _minVisibleRowColumnInViewPort = Mathf.Clamp(newMinVisibleItemInViewPort, 0, Mathf.Max(0, _itemsCount - 1));
            _minExtraVisibleRowColumnInViewPort = Mathf.Clamp(newMinExtraVisibleItemInViewPort, 0, Mathf.Max(0, _itemsCount - 1));
            _maxVisibleRowColumnInViewPort = Mathf.Clamp(newMaxVisibleItemInViewPort, 0, Mathf.Max(0, _itemsCount - 1));
            _maxExtraVisibleRowColumnInViewPort = Mathf.Clamp(newMaxExtraVisibleItemInViewPort, 0, Mathf.Max(0, _itemsCount - 1));
            
            // fill the view port if possible
            InitializeItems(newMaxExtraVisibleItemInViewPort + 1);
        }
        
        /// <summary>
        /// Sets the positions of all items of index + 1
        /// </summary>
        /// <returns></returns>
        protected override void RecalculateTrailingItems(int itemIndex)
        {
            var startingItemToAdjustPosition = itemIndex + 1;
            var anyItemPositionChanged = false;
            for (var i = startingItemToAdjustPosition; i < _itemsCount; i++)
            {
                _itemPositions[i].ResetPositionFlag();
                if (_visibleItems.ContainsKey(i))
                {
                    SetItemPosition(i, _visibleItems[i].transform);
                    anyItemPositionChanged = true;
                }
            }

            if (anyItemPositionChanged)
            {
                RefreshAfterReload();
            }
        }

        protected override void HideItemsAtTopLeft()
        {
            while (_minVisibleRowColumnInViewPort < _itemsCount - 1 && _contentTopLeftCorner[_axis] >= _itemPositions[_minVisibleRowColumnInViewPort].absBottomRightPosition[_axis])
            {
                var itemToHide = _minVisibleRowColumnInViewPort - _extraItemsVisible;
                _minVisibleRowColumnInViewPort++;
                if (itemToHide >= 0 && _visibleItems.ContainsKey(itemToHide))
                {
                    _minExtraVisibleRowColumnInViewPort++;
                    HideItemAtIndex(itemToHide);
                }
            }
        }

        protected override void ShowItemsAtBottomRight()
        {
            while (_maxVisibleRowColumnInViewPort < _itemsCount - 1 && _contentBottomRightCorner[_axis] > _itemPositions[_maxVisibleRowColumnInViewPort].absBottomRightPosition[_axis] + _spacing[_axis])
            {
                _maxVisibleRowColumnInViewPort++;
                var itemToShow = _maxVisibleRowColumnInViewPort + _extraItemsVisible;
                if (itemToShow < _itemsCount && !_visibleItems.ContainsKey(itemToShow))
                {
                    _maxExtraVisibleRowColumnInViewPort = itemToShow;
                    ShowItemAtIndex(itemToShow);
                }
            }
        }

        protected override void HideItemsAtBottomRight()
        {
            while (_maxVisibleRowColumnInViewPort > 0 && _contentBottomRightCorner[_axis] <= _itemPositions[_maxVisibleRowColumnInViewPort].absTopLeftPosition[_axis])
            {
                var itemToHide = _maxVisibleRowColumnInViewPort + _extraItemsVisible;
                _maxVisibleRowColumnInViewPort--;
                if (itemToHide < _itemsCount && _visibleItems.ContainsKey(itemToHide))
                {
                    _maxExtraVisibleRowColumnInViewPort--;
                    HideItemAtIndex(itemToHide);
                }
            }
        }

        protected override void ShowItemsAtTopLeft()
        {
            while (_minVisibleRowColumnInViewPort > 0 && _contentTopLeftCorner[_axis] < _itemPositions[_minVisibleRowColumnInViewPort].absTopLeftPosition[_axis] - _spacing[_axis])
            {
                _minVisibleRowColumnInViewPort--;
                var itemToShow = _minVisibleRowColumnInViewPort - _extraItemsVisible;
                if (itemToShow >= 0 && !_visibleItems.ContainsKey(itemToShow))
                {
                    _minExtraVisibleRowColumnInViewPort = itemToShow;
                    ShowItemAtIndex(itemToShow);
                }
            }
        }
    }
}