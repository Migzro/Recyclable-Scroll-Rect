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
            GetContentBounds();

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
        protected override void RefreshAfterReload(bool reloadAllItems)
        {
            base.RefreshAfterReload(reloadAllItems);
            
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

            _minVisibleRowColumnInViewPort = newMinVisibleItemInViewPort;
            _minExtraVisibleRowColumnInViewPort = Mathf.Clamp(newMinVisibleItemInViewPort - _extraItemsVisible, 0, Mathf.Max(0, _itemsCount - 1));
            
            var newMaxExtraVisibleItemInViewPort = Mathf.Clamp(newMaxVisibleItemInViewPort + _extraItemsVisible, 0, Mathf.Max(0, _itemsCount - 1));
            if (_maxExtraVisibleRowColumnInViewPort > newMaxExtraVisibleItemInViewPort)
            {
                for (var i = _maxExtraVisibleRowColumnInViewPort + 1; i <= newMaxExtraVisibleItemInViewPort; i++)
                {
                    HideItemAtIndex(i);
                }
            }
            
            _maxVisibleRowColumnInViewPort = newMaxVisibleItemInViewPort;
            if (_maxExtraVisibleRowColumnInViewPort < newMaxExtraVisibleItemInViewPort || (_itemsCount > 0 && newMaxVisibleItemInViewPort == 0))
            {
                // here we initialize items instead of using ShowItemAtIndex because we don't know much viewport space is left, initialize items handles setting _maxExtraVisibleRowColumnInViewPort
                var startItemIndex = _maxExtraVisibleRowColumnInViewPort;
                if (_visibleItems.ContainsKey(startItemIndex))
                {
                    startItemIndex++;
                }
                InitializeItems(startItemIndex);
            }
        }

        protected override void ReloadItemInternal(int itemIndex, string reloadTag = "", bool reloadItemData = false, bool isReloadingAllData = false)
        {
            base.ReloadItemInternal(itemIndex, reloadTag, reloadItemData, isReloadingAllData);
            if (_visibleItems.TryGetValue(itemIndex, out var visibleItem))
            {
                SetItemSizeWithPositionAfterReload(visibleItem, itemIndex, isReloadingAllData);
            }
        }

        /// <summary>
        /// Sets the item new size and position after reloading, if item size changed, recalculate all the items that follow
        /// </summary>
        /// <param name="item"></param>
        /// <param name="itemIndex"></param>
        /// <param name="isReloadingAllData"></param>
        private void SetItemSizeWithPositionAfterReload(Item item, int itemIndex, bool isReloadingAllData)
        {
            var oldSize = _itemPositions[itemIndex].itemSize[_axis];
            SetItemSize(itemIndex, item.transform);
            SetItemPosition(itemIndex, item.transform);
            
            // no need to call this while reloading data, since ReloadData will call it after reloading items
            // calling it while reload data will add unneeded redundancy
            if (!isReloadingAllData)
            {
                // no need to call RefreshAfterReload if content moved since it will be handled in Update
                var contentMoved = RecalculateFollowingItems(itemIndex, oldSize);
                if (!contentMoved)
                {
                    RefreshAfterReload(false);
                }
            }
        }
        
        /// <summary>
        /// Sets the positions of all items of index + 1
        /// Persists content position to avoid sudden jumps if an item size changes
        /// </summary>
        /// <param name="itemIndex">index of item to start calculate following items from</param>
        /// <param name="oldSize">old item size used to offset content position with</param>
        /// <returns></returns>
        private bool RecalculateFollowingItems(int itemIndex, float oldSize)
        {
            // need to adjust all the items position after itemIndex 
            var startingItemToAdjustPosition = itemIndex + 1;
            for (var i = startingItemToAdjustPosition; i <= _maxExtraVisibleRowColumnInViewPort; i++)
            {
                _itemPositions[i].ResetPositionFlag();
                SetItemPosition(i, _visibleItems[i].transform);
            }

            if (_isAnimating)
            {
                return true;
            }

            var contentPosition = content.anchoredPosition;
            var contentMoved = false;
            var oldContentPosition = contentPosition[_axis];
            if (itemIndex < _minExtraVisibleRowColumnInViewPort)
            {
                // this is a very special case as items reloaded at the top or right will have a different bottomRight position
                // and since we are here at the item, if we don't manually set the position of the content, it will seem as the content suddenly shifted and disorient the user
                contentPosition[_axis] = _itemPositions[itemIndex].absBottomRightPosition[_axis];
            }
            else if (_minExtraVisibleRowColumnInViewPort <= itemIndex && _minVisibleRowColumnInViewPort > itemIndex)
            {
                contentPosition[_axis] -= (oldSize - _itemPositions[itemIndex].itemSize[_axis]);
            }
            
            var contentPositionDiff = Mathf.Abs(contentPosition[_axis] - oldContentPosition);
            if (contentPositionDiff > 0)
            {
                contentMoved = true;
            }

            if (contentMoved)
            {
                content.anchoredPosition = contentPosition;
                // this is important since the scroll rect will likely be dragging, and it will cause a jump
                // this only took me 6 hours to figure out :(
                m_ContentStartPosition = contentPosition;
            }

            return contentMoved;
        }

        protected override void HideItemsAtTopLeft()
        {
            if (_minVisibleRowColumnInViewPort < _itemsCount - 1 && _contentTopLeftCorner[_axis] >= _itemPositions[_minVisibleRowColumnInViewPort].absBottomRightPosition[_axis])
            {
                var itemToHide = _minVisibleRowColumnInViewPort - _extraItemsVisible;
                _minVisibleRowColumnInViewPort++;
                if (itemToHide > -1)
                {
                    _minExtraVisibleRowColumnInViewPort++;
                    HideItemAtIndex(itemToHide);
                }
            }
        }
        
        protected override void ShowItemsAtBottomRight()
        {
            if (_maxVisibleRowColumnInViewPort < _itemsCount - 1 && _contentBottomRightCorner[_axis] > _itemPositions[_maxVisibleRowColumnInViewPort].absBottomRightPosition[_axis] + _spacing[_axis])
            {
                _maxVisibleRowColumnInViewPort++;
                var itemToShow = _maxVisibleRowColumnInViewPort + _extraItemsVisible;
                if (itemToShow < _itemsCount)
                {
                    _maxExtraVisibleRowColumnInViewPort = itemToShow;
                    ShowItemAtIndex(itemToShow);
                }
            }
        }

        protected override void HideItemsAtBottomRight()
        {
            if (_maxVisibleRowColumnInViewPort > 0 && _contentBottomRightCorner[_axis] <= _itemPositions[_maxVisibleRowColumnInViewPort].absTopLeftPosition[_axis])
            {
                var itemToHide = _maxVisibleRowColumnInViewPort + _extraItemsVisible;
                _maxVisibleRowColumnInViewPort--;
                if (itemToHide < _itemsCount)
                {
                    _maxExtraVisibleRowColumnInViewPort--;
                    HideItemAtIndex(itemToHide);
                }
            }
        }
        
        protected override void ShowItemsAtTopLeft()
        {
            if (_minVisibleRowColumnInViewPort > 0 && _contentTopLeftCorner[_axis] < _itemPositions[_minVisibleRowColumnInViewPort].absTopLeftPosition[_axis] - _spacing[_axis])
            {
                _minVisibleRowColumnInViewPort--;
                var itemToShow = _minVisibleRowColumnInViewPort - _extraItemsVisible;
                if (itemToShow > -1)
                {
                    _minExtraVisibleRowColumnInViewPort = itemToShow;
                    ShowItemAtIndex(itemToShow);
                }
            }
        }
    }
}