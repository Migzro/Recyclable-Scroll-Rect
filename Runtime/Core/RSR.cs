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
        
        protected override bool ReachedMinRowColumnInViewPort => _minVisibleRowColumnInViewPort == 0;
        protected override bool ReachedMaxRowColumnInViewPort => _maxVisibleRowColumnInViewPort == _itemsCount - 1;
        
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
            var i = startIndex;
            while ((contentHasSpace || extraItemsInitialized < _extraItemsVisible) && i < _itemsCount)
            {
                ShowItemAtIndex(i);
                if (!contentHasSpace)
                    extraItemsInitialized++;
                else
                    _maxVisibleRowColumnInViewPort = i;

                contentHasSpace = _itemPositions[i].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
                i++;
            }
            _maxExtraVisibleRowColumnInViewPort = i - 1;
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
                if (!_dataSource.IsItemSizeKnown)
                {
                    contentSizeDelta[_axis] += _itemPositions[i].itemSize[_axis];
                }
                else
                {
                    var itemSize = _itemPositions[i].itemSize;
                    itemSize[_axis] = _dataSource.GetItemSize(i);
                    _itemPositions[i].SetSize(itemSize);
                    contentSizeDelta[_axis] += itemSize[_axis];
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
        /// <param name="itemIndex">index of the item that needs its position set</param>
        protected override void SetItemAxisPosition(RectTransform rect, int itemIndex)
        {
            var newItemPosition = rect.anchoredPosition;
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

            rect.anchoredPosition = newItemPosition;
            _itemPositions[itemIndex].SetPosition(newItemPosition);
        }
        
        /// <summary>
        /// This function calculates the item size if its unknown by forcing a Layout rebuild
        /// if it is known we just get the item size
        /// </summary>
        /// <param name="rect">rect of the item which the size will be calculated for</param>
        /// <param name="itemIndex">item index which the size will be calculated for</param>
        protected override void CalculateItemAxisSize(RectTransform rect, int itemIndex)
        {
            var newItemSize = _itemPositions[itemIndex].itemSize;
            var oldItemSize = newItemSize[_axis];

            if (!_dataSource.IsItemSizeKnown)
            {
                ForceLayoutRebuild(itemIndex);
                newItemSize[_axis] = rect.rect.size[_axis];
                
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
                newItemSize[_axis] = _dataSource.GetItemSize(itemIndex);
            }

            _itemPositions[itemIndex].SetSize(newItemSize);
        }

        protected override void CalculateNonAxisSizePosition(RectTransform rect, int itemIndex)
        {
            base.CalculateNonAxisSizePosition(rect, itemIndex);
            
            var forceSize = false;
            // expand item width if it's in a vertical scrollRect and the conditions are satisfied
            if (vertical && _childForceExpand)
            {
                var itemSize = rect.sizeDelta;
                itemSize.x = content.rect.width;
                if (!_dataSource.IgnoreContentPadding(itemIndex))
                {
                    itemSize.x -= _padding.right + _padding.left;
                }

                rect.sizeDelta = itemSize;
                _itemPositions[itemIndex].SetSize(itemSize);
                forceSize = true;
            }

            // expand item height if it's in a horizontal scrollRect and the conditions are satisfied
            else if (!vertical && _childForceExpand)
            {
                var itemSize = rect.sizeDelta;
                itemSize.y = content.rect.height;
                if (!_dataSource.IgnoreContentPadding(itemIndex))
                {
                    itemSize.y -= _padding.top + _padding.bottom;
                }

                rect.sizeDelta = itemSize;
                _itemPositions[itemIndex].SetSize(itemSize);
                forceSize = true;
            }

            // get content size without padding
            var contentSize = content.rect.size;
            var contentSizeWithoutPadding = contentSize;
            contentSizeWithoutPadding.x -= _padding.right + _padding.left;
            contentSizeWithoutPadding.y -= _padding.top + _padding.bottom;

            // set position of item based on layout alignment
            // we check for multiple conditions together since the content is made to fit the items, so they only move in one axis in each different scroll direction
            var rectSize = rect.rect.size;
            var itemSizeSmallerThanContent = rectSize[_axis] < contentSizeWithoutPadding[_axis];
            if (itemSizeSmallerThanContent || forceSize)
            {
                var itemPosition = rect.anchoredPosition;
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
                        itemPosition.x = (leftPadding + (contentSize.x - rectSize.x) - rightPadding) / 2f;
                    }
                    else if (_itemsAlignment == ItemsAlignment.RightOrDown)
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
                    var topPadding = _padding.top;
                    var bottomPadding = _padding.bottom;
                    if (_dataSource.IgnoreContentPadding(itemIndex))
                    {
                        topPadding = 0;
                        bottomPadding = 0;
                    }
                    
                    if (_itemsAlignment == ItemsAlignment.Center)
                    {
                        itemPosition.y = -(topPadding + (contentSize.y - rectSize.y) - bottomPadding) / 2f;
                    }
                    else if (_itemsAlignment == ItemsAlignment.RightOrDown)
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
            CalculateNewMinMaxItemsAfterReloadItem();
            RefreshAfterReload(reloadAllItems);
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
                _maxVisibleRowColumnInViewPort = _itemsCount - 1;
            }
            if (_itemsCount - 1 < _maxExtraVisibleRowColumnInViewPort)
            {
                _maxExtraVisibleRowColumnInViewPort = Mathf.Min(_itemsCount - 1, _maxVisibleRowColumnInViewPort + _extraItemsVisible);
            }
        }
        
        /// <summary>
        /// Checks if items need to be hidden, shown, instantiated after an item is reloaded and its size changes
        /// </summary>
        private void CalculateNewMinMaxItemsAfterReloadItem()
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
            var newMaxExtraVisibleItemInViewPort = Mathf.Min (_itemsCount > 0 ? _itemsCount - 1 : 0, newMaxVisibleItemInViewPort + _extraItemsVisible);
            if (newMaxExtraVisibleItemInViewPort < _maxExtraVisibleRowColumnInViewPort)
            {
                for (var i = newMaxExtraVisibleItemInViewPort + 1; i <= _maxExtraVisibleRowColumnInViewPort; i++)
                {
                    HideItemAtIndex(i);
                }

                _maxVisibleRowColumnInViewPort = newMaxVisibleItemInViewPort;
                _maxExtraVisibleRowColumnInViewPort = newMaxExtraVisibleItemInViewPort;
            }
            else
            {
                // here we initialize items instead of using ShowItemAtIndex because we don't know much viewport space is left
                InitializeItems(_maxExtraVisibleRowColumnInViewPort + 1);
            }
            
            if (newMinExtraVisibleItemInViewPort > _minExtraVisibleRowColumnInViewPort)
            {
                for (var i = _minExtraVisibleRowColumnInViewPort; i < newMinExtraVisibleItemInViewPort; i++)
                {
                    HideItemAtIndex(i);
                }
            }
            else
            {
                for (var i = _minExtraVisibleRowColumnInViewPort - 1; i >= newMinExtraVisibleItemInViewPort; i--)
                {
                    ShowItemAtIndex(i);
                }
            }

            _minVisibleRowColumnInViewPort = newMinVisibleItemInViewPort;
            _minExtraVisibleRowColumnInViewPort = newMinExtraVisibleItemInViewPort;
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
            CalculateNonAxisSizePosition(item.transform, itemIndex);
            SetItemAxisPosition(item.transform, itemIndex);
            CalculateItemAxisSize(item.transform, itemIndex);
            
            // no need to call this while reloading data, since ReloadData will call it after reloading items
            // calling it while reload data will add unneeded redundancy
            if (!isReloadingAllData)
            {
                // no need to call CalculateNewMinMaxItemsAfterReloadItem if content moved since it will be handled in Update
                var contentMoved = RecalculateFollowingItems(itemIndex, oldSize);
                if (!contentMoved)
                    CalculateNewMinMaxItemsAfterReloadItem();
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
                SetItemAxisPosition(_visibleItems[i].transform, i);

            if (_isAnimating)
                return true;
            
            var contentPosition = content.anchoredPosition;
            var contentMoved = false;
            var oldContentPosition = contentPosition[_axis];
            if (itemIndex < _minExtraVisibleRowColumnInViewPort)
            {
                // this is a very special case as items reloaded at the top or right will have a different bottomRight position
                // and since we are here at the item, if we don't manually set the position of the content, it will seem as the content suddenly shifted and disorient the user
                contentPosition[_axis] = _itemPositions[itemIndex].absBottomRightPosition[_axis];
                
                // set the normalized position as well, because why not
                // (viewMin - (itemPosition - contentSize)) / (contentSize - viewSize)
                // var viewportRect = viewport.rect;
                // var contentRect = content.rect;
                // var viewPortBounds = new Bounds(viewportRect.center, viewportRect.size);
                // var newNormalizedPosition = (viewPortBounds.min[_axis] - (_itemPositions[itemIndex].bottomRightPosition[_axis] - contentRect.size[_axis])) / (contentRect.size[_axis] - viewportRect.size[_axis]);
                // SetNormalizedPosition(newNormalizedPosition, _axis);
            }
            else if (_minExtraVisibleRowColumnInViewPort <= itemIndex && _minVisibleRowColumnInViewPort > itemIndex)
            {
                contentPosition[_axis] -= (oldSize - _itemPositions[itemIndex].itemSize[_axis]);
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

        public override void ScrollToTopRight()
        {
            base.ScrollToTopRight();
            StartCoroutine(ScrollToTargetNormalisedPosition(vertical ? 1 : 0));
        }
    }
}