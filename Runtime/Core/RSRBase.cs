// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecyclableScrollRect
{
    public abstract class RSRBase : ScrollRect
    {
        [SerializeField] private bool _showUsingCanvasGroupAlpha;
        [SerializeField] private float _pullToRefreshThreshold = 150;
        [SerializeField] private float _pushToCloseThreshold = 150;
        
        [SerializeField] protected Vector2 _spacing;
        [SerializeField] protected RectOffset _padding;
        [SerializeField] protected ItemsAlignment _itemsAlignment;
        [SerializeField] private BaseScrollAnimationController _scrollAnimationController;
        
        protected IDataSource _dataSource;

        protected LayoutElement _layoutElement;
        private ScreenResolutionDetector _screenResolutionDetector;

        protected List<ItemPosition> _itemPositions;
        private List<bool> _staticItems;
        private List<string> _prototypeNames;
        
        protected int _axis;
        protected int _itemsCount;
        protected int _minVisibleRowColumnInViewPort;
        protected int _maxVisibleRowColumnInViewPort;
        protected int _minExtraVisibleRowColumnInViewPort;
        protected int _maxExtraVisibleRowColumnInViewPort;
        protected bool _isAnimating;
        private bool _needsClearance;
        private bool _pullToRefresh;
        private bool _pushToClose;
        private bool _canCallReachedScrollEnd;
        private bool _canCallReachedScrollStart;
        private bool _isApplicationQuitting;

        protected SortedDictionary<int, Item> _visibleItems;
        private Dictionary<string, List<Item>> _pooledItems;
        
        protected Vector2 _dragStartingPosition;
        protected Vector2 _contentTopLeftCorner;
        protected Vector2 _contentBottomRightCorner;
        private Vector2 _viewPortSize;
        private Vector2 _lastContentPosition;
        private MovementType _movementType;
        private MovementType _initialMovementType;

        public bool IsInitialized { get; private set; }
        
        internal float ContentPosition
        {
            get => content.anchoredPosition[_axis];

            set
            {
                var currentAnchoredPosition = content.anchoredPosition;
                currentAnchoredPosition[_axis] = value;
                content.anchoredPosition = currentAnchoredPosition;
                m_ContentStartPosition = currentAnchoredPosition;
            }
        }
        
        private float AverageItemSize
        {
            get {
                var averageItemSize = 0f;
                var totalItems = 0;
                for (var i = 0; i < _itemsCount; i++)
                {
                    if (_itemPositions[i].sizeSet)
                    {
                        averageItemSize += _itemPositions[i].itemSize[_axis];
                        totalItems++;
                    }
                }

                averageItemSize = averageItemSize / totalItems;
                return averageItemSize;
            }
        }

        private bool AllItemsPositionsSet
        {
            get
            {
                for (var i = 0; i < _itemsCount; i++)
                {
                    if (!_itemPositions[i].positionSet)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        protected abstract bool IsItemSizeKnown { get; }
        protected abstract bool ReachedMinRowColumnInViewPort { get; }
        protected abstract bool ReachedMaxRowColumnInViewPort { get; }
        protected abstract bool IsLastRowColumn(int itemIndex);
        protected abstract void InitializeItems(int startIndex = 0);
        protected abstract void SetItemPosition(int itemIndex, RectTransform rect = null);
        protected abstract void SetItemSize(int itemIndex, RectTransform rect = null);
        protected abstract int GetActualItemIndex(int itemIndex);
        protected abstract void CalculateContentSize();
        protected abstract void HideItemsAtTopLeft();
        protected abstract void ShowItemsAtBottomRight();
        protected abstract void HideItemsAtBottomRight();
        protected abstract void ShowItemsAtTopLeft();

        public void Initialize(IDataSource dataSource)
        {
            _dataSource = dataSource;
            
            if (_dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource), "RSR, IDataSource is null");
            }
            Initialize();
        }
        
        /// <summary>
        /// Initialize the scroll rect with the data source that contains all the details required to build the RecyclableScrollRect
        /// </summary>
        protected virtual void Initialize()
        {
            if (_dataSource.PrototypeItems == null || _dataSource.PrototypeItems.Length <= 0)
            {
                throw new ArgumentNullException(nameof(_dataSource.PrototypeItems), "RSR, No prototype item defined IDataSource");
            }
            
            // Register event delegate for resolution change
            ScreenResolutionDetector.Instance.OnResolutionChanged += UpdateContentLayouts;

            // add a LayoutElement if not present to set the content size in case another element is controlling it 
            _layoutElement = content.gameObject.GetComponent<LayoutElement>();
            if (_layoutElement == null)
            {
                _layoutElement = content.gameObject.AddComponent<LayoutElement>();
            }

            _axis = vertical ? 1 : 0;
            _initialMovementType = movementType;
            
            InitializeData();
            _scrollAnimationController ??= gameObject.GetComponent<BaseScrollAnimationController>();
            _scrollAnimationController ??= gameObject.AddComponent<ScrollAnimationController>();
        }

        /// <summary>
        /// Reload the data in case the content of the RecyclableScrollRect has changed
        /// </summary>
        private void InitializeData()
        {
            IsInitialized = false;
            
            if (_visibleItems != null)
            {
                foreach (var visibleItem in _visibleItems)
                {
                    if (!_staticItems[visibleItem.Key])
                    {
                        Destroy(visibleItem.Value.transform.gameObject);
                    }
                }
                _visibleItems.Clear();
            }

            StopMovement();
            
            _minVisibleRowColumnInViewPort = 0;
            _minExtraVisibleRowColumnInViewPort = 0;
            _maxVisibleRowColumnInViewPort = 0;
            _maxExtraVisibleRowColumnInViewPort = 0;
            
            ContentPosition = 0;
            SetContentBounds();

            _itemsCount = _dataSource.ItemsCount;
            _staticItems = new List<bool>();
            _prototypeNames = new List<string>();
            _itemPositions = new List<ItemPosition>();
            _lastContentPosition = _contentTopLeftCorner;
            SetMovementType(_initialMovementType);

            _visibleItems = new SortedDictionary<int, Item>();
            
            if (_pooledItems == null)
            {
                _pooledItems = new Dictionary<string, List<Item>>();
            }
            
            // create a new list for each prototype items to hold the pooled items
            var prototypeItems = _dataSource.PrototypeItems;
            for (var i = 0; i < prototypeItems.Length; i++)
            {
                if (!_pooledItems.ContainsKey(prototypeItems[i].name))
                {
                    _pooledItems.Add(prototypeItems[i].name, new List<Item>());
                }
            }

            ResetVariables();
            SetContentAnchorsPivot();
            InitializeItemPositions();
            CalculateContentSize();
            SetStaticItems();
            HideStaticItems();
            SetPrototypeNames();
            InitializeItems();

            IsInitialized = true;
        }

        /// <summary>
        /// Sets the content anchors and pivot based on the direction of the scroll and if it's reversed or not
        /// </summary>
        private void SetContentAnchorsPivot()
        {
            if (vertical)
            {
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = new Vector2(1, 1);
                content.pivot = new Vector2(0, 1);
            }
            else
            {
                content.anchorMin = Vector2.zero;
                content.anchorMax = new Vector2(0, 1);
                content.pivot = new Vector2(0, 1);
            }
        }

        /// <summary>
        /// A common function to reset variables when calling ResetData or ReloadData
        /// </summary>
        protected virtual void ResetVariables()
        {
            _isAnimating = false;
            _canCallReachedScrollStart = true;
            _canCallReachedScrollEnd = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Application.isPlaying && !_isApplicationQuitting && ScreenResolutionDetector.Instance != null)
            {
                ScreenResolutionDetector.Instance.OnResolutionChanged -= UpdateContentLayouts;
            }
        }

        private void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }

        private void UpdateContentLayouts()
        {
            ReloadData(true);
        }
        
        /// <summary>
        /// Cache the static items
        /// </summary>
        private void SetStaticItems()
        {
            for (var i = 0; i < _itemsCount; i++)
            {
                var actualItemIndex = GetActualItemIndex(i);
                var isCellStatic = actualItemIndex != -1 && _dataSource.IsItemStatic(actualItemIndex); 
                if (i < _staticItems.Count)
                {
                    _staticItems[i] = isCellStatic;
                }
                else
                {
                    _staticItems.Add(isCellStatic);
                }
            }
        }
        
        /// <summary>
        /// Hide the static items at the start
        /// Their visibility will depend on whether they are in viewport or not
        /// </summary>
        private void HideStaticItems()
        {
            for (var i = 0; i < _itemsCount; i++)
            {
                // no need to check if an item with actual index of -1 is going to be hidden or not, since it's set in SetStaticItems
                if (_staticItems[i])
                {
                    RectTransform itemRect;
                    if (_visibleItems.TryGetValue(i, out var visibleItem))
                    {
                        itemRect = visibleItem.transform;
                    }
                    else
                    {
                        var actualItemIndex = GetActualItemIndex(i);
                        itemRect = (RectTransform)_dataSource.GetItemPrototype(actualItemIndex).transform;
                    }
                    SetVisibilityInHierarchy(itemRect, false);

                    if (_showUsingCanvasGroupAlpha)
                    {
                        var item = itemRect.GetComponent<IItem>() ?? itemRect.gameObject.AddComponent<BaseItem>();
                        var canvasGroup = item.CanvasGroup;
                        canvasGroup.alpha = 0;
                        canvasGroup.interactable = false;
                        canvasGroup.blocksRaycasts = false;
                    }
                    else
                    {
                        itemRect.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        /// <summary>
        /// Set prefab names for when needed to retrieve from pool
        /// if item already has a prototype name and isVisible, change the prototype gameObject
        /// </summary>
        private void SetPrototypeNames()
        {
            // set an array of prototype names to be used when getting the correct prefab for the item index if it exists
            for (var i = 0; i < _itemsCount; i++)
            {
                var actualItemIndex = GetActualItemIndex(i);
                var newPrototypeName = actualItemIndex == -1 ? string.Empty : _dataSource.GetItemPrototype(actualItemIndex).name;
                if (i < _prototypeNames.Count)
                {
                    if (newPrototypeName != _prototypeNames[i] && actualItemIndex != -1)
                    {
                        // hide the item if its visible, remove the old prototype name from the pool, show the cell again with the new prototype name
                        var isVisible = _visibleItems.TryGetValue(i, out _);
                        if (isVisible)
                        {
                            HideItemAtIndex(i);
                        }
                        _prototypeNames[i] = newPrototypeName;
                        if (isVisible)
                        {
                            ShowItemAtIndex(i);
                        }
                    }
                }
                else
                {
                    _prototypeNames.Add(newPrototypeName);
                }
            }
        }

        /// <summary>
        /// Initialize item positions as zero to avoid using .Contains when initializing the positions
        /// </summary>
        private void InitializeItemPositions()
        {
            for (var i = _itemPositions.Count; i < _itemsCount; i++)
            {
                _itemPositions.Add(new ItemPosition());
            }

            if (IsItemSizeKnown)
            {
                for (var i = 0; i < _itemsCount; i++)
                {
                    SetNonAxisSize(i);
                    SetItemSize(i);
                    SetItemPosition(i);
                }
            }
        }
        
        /// <summary>
        /// Initialize the items
        /// Its only called when there are no pooled items available and the RecyclableScrollRect needs to show an item
        /// </summary>
        /// <param name="itemIndex"></param>
        private void InitializeItem(int itemIndex)
        {
            var actualItemIndex = GetActualItemIndex(itemIndex);
            var itemPrototypeItem = _dataSource.GetItemPrototype(actualItemIndex);

            GameObject itemGo;
            IItem itemImpl;
            if (!_staticItems[itemIndex])
            {
                itemGo = Instantiate(itemPrototypeItem, content, false);
                itemImpl = itemGo.GetComponent<IItem>();
                itemImpl.RSRBase = this;
                itemImpl.ItemIndex = itemIndex;
                itemGo.name = $"{itemPrototypeItem.name} {itemIndex}";
            }
            else
            {
                itemGo = itemPrototypeItem;
                itemImpl = itemGo.GetComponent<IItem>() ?? itemGo.AddComponent<BaseItem>();
                itemImpl.RSRBase = this;
                itemImpl.ItemIndex = itemIndex;
                
                SetVisibilityInHierarchy((RectTransform)itemGo.transform, true);
                itemGo.SetActive(true);
                if (_showUsingCanvasGroupAlpha)
                {
                    var canvasGroup = itemImpl.CanvasGroup; 
                    canvasGroup.alpha = 1;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            }

            var rect = (RectTransform)itemGo.transform;
            var item = new Item(itemImpl, rect);
            _visibleItems.Add(itemIndex, item);
            _dataSource.ItemCreated(actualItemIndex, itemImpl, itemGo);

            // anchors and pivot will always be 0,1 no matter the settings of RSR
            var anchorVector = new Vector2(0, 1);
            rect.anchorMin = anchorVector;
            rect.anchorMax = anchorVector;
            rect.pivot = anchorVector;
            
            SetItemRectAndData(itemIndex, item);
        }
        
        /// <summary>
        /// Hides private ReloadItem implementation to avoid calling it with unneeded variables (isReloadingAllData)
        /// </summary>
        /// <param name="itemIndex">item index to reload</param>
        public void ReloadItem(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= _itemsCount)
            {
                return;
            }
            
            _itemPositions[itemIndex].ResetAllFlags();
            // No need to reload item at index {itemIndex} as its currently not visible and everything will be automatically handled when it appears
            if (!_visibleItems.TryGetValue(itemIndex, out var visibleItem))
            {
                return;
            }
            
            var oldSize = _itemPositions[itemIndex].itemSize[_axis];
            SetItemRectAndData(itemIndex, visibleItem);
            if (oldSize != 0 && Mathf.Abs(oldSize - _itemPositions[itemIndex].itemSize[_axis]) > 0.01f)
            {
                RecalculateTrailingItems(itemIndex);
            }
        }
        
        protected virtual void RecalculateTrailingItems(int itemIndex)
        {
        }

        /// <summary>
        /// Forces a layout to rebuild in case it was called with a tag that already exists,
        /// This means that the size is already known, we just need to make sure the item looks right
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <returns></returns>
        protected void ForceLayoutRebuild(int itemIndex)
        {
            if (_visibleItems.TryGetValue(itemIndex, out var visibleItem))
            {
                RectTransform[] rects = null;
                if (!_staticItems[itemIndex])
                {
                    rects = visibleItem.item.ItemsNeededForVisualUpdate;
                }

                if (rects != null)
                {
                    foreach (var rect in rects)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    }
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(visibleItem.transform);
            }
        }

        /// <summary>
        /// it sets the vertical size of the item in a horizontal layout
        /// or the horizontal size of an item in a vertical layout based on the settings of said layout
        /// Is not needed in grid as items will have different positions in non axis position and the non axis size is the same in all of them
        /// </summary>
        /// <param name="itemIndex">The index of the item that its size will be adjusted</param>
        /// <param name="rect">rect transform that needs setting</param>
        protected virtual void SetNonAxisSize(int itemIndex, RectTransform rect = null)
        {
        }
        
        /// <summary>
        /// Used to force set item position, can be used if the item position is manipulated externally and later would want to restore it.
        /// It doesn't need to set the position of an invisible item as it will get set automatically when its in view 
        /// </summary>
        /// <param name="itemIndex">item index to set position to</param>
        public void RestoreItemPosition(int itemIndex)
        {
            if (!_visibleItems.TryGetValue(itemIndex, out var visibleItem))
                return;

            SetItemPosition(itemIndex, visibleItem.transform);
        }

        /// <summary>
        /// The function in which we calculate which items need to be shown and which items need to hide
        /// </summary>
        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (!IsInitialized)
                return;
            if (_visibleItems.Count <= 0)
                return;
            var currentContentAnchoredPosition = content.anchoredPosition * (vertical ? 1f : -1f);
            if (Mathf.Approximately(currentContentAnchoredPosition[_axis], _lastContentPosition[_axis]) && !_needsClearance)
                return;
            
            SetContentBounds();
            
            if (!_pullToRefresh && Mathf.RoundToInt(_contentTopLeftCorner[_axis]) <= -_pullToRefreshThreshold)
            {
                _pullToRefresh = true;
                _dataSource.PullToRefresh();
            }
            else if (_pullToRefresh && Mathf.RoundToInt(_contentTopLeftCorner[_axis]) >= -_pullToRefreshThreshold)
            {
                _pullToRefresh = false;
            }
            
            if (!_pushToClose && Mathf.RoundToInt(_contentBottomRightCorner[_axis]) >= content.rect.size[_axis] + _pushToCloseThreshold) 
                              // && (!_paged || (_paged && _currentPage < _itemsCount))) TODO: why is this needed?
            {
                _pushToClose = true;
                _dataSource.PushToClose();
            }
            else if (_pushToClose && Mathf.RoundToInt(_contentBottomRightCorner[_axis]) <= content.rect.size[_axis])
            {
                _pushToClose = false;
            }
            
            // figure out which items that need to be rendered, bottom right or top left
            // generally if the content position is smaller than the position of _minVisibleItemInViewPort, this means we need to show items in tops left
            // if content position is bigger than the position of _maxVisibleItemInViewPort, this means we need to show items in bottom right
            var reachedLimits = false;
            var atStart = _contentTopLeftCorner[_axis] <= 0;
            var atEnd = _contentBottomRightCorner[_axis] >= content.rect.size[_axis] && ReachedMaxRowColumnInViewPort;
            if (atStart || atEnd)
            {
                movementType = _movementType;
                reachedLimits = true;
                
                if ( atStart && _canCallReachedScrollStart )
                {
                    _dataSource.ReachedScrollStart();
                    _canCallReachedScrollStart = false;
                }
                
                if ( atEnd && _canCallReachedScrollEnd )
                {
                    _dataSource.ReachedScrollEnd();
                    _canCallReachedScrollEnd = false;
                }
            }
            else
            {
                _canCallReachedScrollStart = true;
                _canCallReachedScrollEnd = true;
                movementType = MovementType.Unrestricted;
            }

            var showBottomRight = _contentTopLeftCorner[_axis] > _lastContentPosition[_axis];
            _needsClearance = false;

            var topLeftPadding = vertical ? _padding.top : _padding.left;
            var bottomLeftPadding = vertical ? _padding.bottom : _padding.right;
            var topLeftMinClearance = 0.1f + topLeftPadding * (ReachedMinRowColumnInViewPort ? 1 : 0) + _spacing[_axis] * (ReachedMinRowColumnInViewPort ? 0 : 1);
            var bottomRightMinClearance = 0.1f + bottomLeftPadding * (ReachedMaxRowColumnInViewPort ? 1 : 0) + _spacing[_axis] * (ReachedMaxRowColumnInViewPort ? 0 : 1);
            
            if (_itemPositions[_minVisibleRowColumnInViewPort].absTopLeftPosition[_axis] - _contentTopLeftCorner[_axis] > topLeftMinClearance && !ReachedMinRowColumnInViewPort)
            {
                showBottomRight = false;
                _needsClearance = true;
            }
            else if (_itemPositions[_maxVisibleRowColumnInViewPort].absBottomRightPosition[_axis] - _contentBottomRightCorner[_axis] < -bottomRightMinClearance && !ReachedMaxRowColumnInViewPort)
            {
                showBottomRight = true;
                _needsClearance = true;
            }
            _lastContentPosition = currentContentAnchoredPosition;

            if (reachedLimits && !_needsClearance)
                return;
            
            ShowHideItems(showBottomRight);
        }

        private void ShowHideItems(bool showBottomRight)
        {
            if (showBottomRight)
            {
                ShowItemsAtBottomRight();
                HideItemsAtTopLeft();
            }
            else
            {
                ShowItemsAtTopLeft();
                HideItemsAtBottomRight();
            }
        }
        
        /// <summary>
        /// User has scrolled, and we need to show an item
        /// If there is a pooled item available, we get it and set its position, sibling index, and remove it from the pool
        /// If there is no pooled item available, we create a new one
        /// </summary>
        /// <param name="itemIndex">current index of item we need to show</param>
        protected void ShowItemAtIndex(int itemIndex)
        {
            // Get empty item and adjust its position and size, else just create a new an item
            var itemPrototypeName = _prototypeNames[itemIndex];

            if (!_pooledItems.TryGetValue(itemPrototypeName, out var pool) || pool.Count == 0)
            {
                InitializeItem(itemIndex);
            }
            else
            {
                var item = pool[0];
                pool.RemoveAt(0);

                if (_showUsingCanvasGroupAlpha)
                {
                    var canvasGroup = item.item.CanvasGroup; 
                    canvasGroup.alpha = 1;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
                else
                {
                    item.transform.gameObject.SetActive(true);
                }

                SetVisibilityInHierarchy(item.transform, true);

                _visibleItems.Add(itemIndex, item);
                item.item.ItemIndex = itemIndex;
                SetItemRectAndData(itemIndex, item);
                if (!_staticItems[itemIndex])
                {
                    item.transform.name = $"{itemPrototypeName} {itemIndex}";
                }
            }

            SetSiblingIndices();
            
            if (IsLastRowColumn(itemIndex))
                _dataSource.LastItemIsVisible();
        }

        private void SetItemRectAndData(int itemIndex, Item item)
        {
            SetNonAxisSize(itemIndex, item.transform);
            var actualItemIndex = GetActualItemIndex(itemIndex);
            if (actualItemIndex != -1)
            {
                _dataSource.SetItemData(item.item, actualItemIndex);
            }
            SetItemSize(itemIndex, item.transform);
            SetItemPosition(itemIndex, item.transform);
        }

        /// <summary>
        /// Sets the indices of the items inside the content of the ScrollRect
        /// </summary>
        protected virtual void SetSiblingIndices()
        {
            foreach (var visibleItem in _visibleItems)
            {
                visibleItem.Value.transform.SetSiblingIndex(visibleItem.Key);
            }
        }
        
        /// <summary>
        /// Hide item at itemIndex and add it to the pool of items that can be used based on its prefab type
        /// </summary>
        /// <param name="itemIndex">itemIndex which will be hidden</param>
        protected void HideItemAtIndex(int itemIndex)
        {
            _visibleItems.TryGetValue(itemIndex, out var visibleItem);
            if (_showUsingCanvasGroupAlpha)
            {
                var canvasGroup = visibleItem.item.CanvasGroup;
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                visibleItem.transform.gameObject.SetActive(false);
            }

            var actualItemIndex = GetActualItemIndex(itemIndex);
            SetVisibilityInHierarchy(visibleItem.transform, false);
            _dataSource.ItemHidden(visibleItem.item, actualItemIndex);

            var protoName = _prototypeNames[itemIndex];
            if (!_pooledItems.TryGetValue(protoName, out var pool))
            {
                pool = new List<Item>();
                _pooledItems[protoName] = pool;
            }
            pool.Add(visibleItem);
            _visibleItems.Remove(itemIndex);
        }

        /// <summary>
        /// Updates content bounds for different uses
        /// </summary>
        protected void SetContentBounds()
        {
            _viewPortSize = viewport.rect.size;
            _contentTopLeftCorner = content.anchoredPosition * (vertical ? 1f : -1f);
            _contentBottomRightCorner[1 - _axis] = _contentTopLeftCorner[1 - _axis];
            _contentBottomRightCorner[_axis] = _contentTopLeftCorner[_axis] + _viewPortSize[_axis];
        }

        /// <summary>
        /// Reload data in scroll view
        /// </summary>
        /// <param name="reloadAllItems">should be only used when adding items to the top of the current visible items</param>
        public void ReloadData(bool reloadAllItems = false)
        {
            var oldItemsCount = _itemsCount;
            _itemsCount = _dataSource.ItemsCount;
            
            // removes extra items
            if (oldItemsCount > _itemsCount)
            {
                var itemDiff = oldItemsCount - _itemsCount;
                RemoveExtraItems(itemDiff);
                _itemPositions.RemoveRange(_itemsCount, itemDiff);
                _prototypeNames.RemoveRange(_itemsCount, itemDiff);
                _staticItems.RemoveRange(_itemsCount, itemDiff);
            }

            if (reloadAllItems)
            {
                foreach (var itemPosition in _itemPositions)
                {
                    itemPosition.ResetAllFlags();
                }

                if (!IsItemSizeKnown)
                {
                    ContentPosition = 0;
                    SetContentBounds();
                }
            }
            
            ResetVariables();
            SetContentAnchorsPivot();
            InitializeItemPositions();
            CalculateContentSize();
            ClampContentPosition();
            SetStaticItems();
            SetPrototypeNames();
            RefreshAfterReload();
        }

        /// <summary>
        /// this removes all items that are not needed after item reload if _itemsCount has been reduced
        /// </summary>
        /// <param name="itemDiff">the amount of items that have been deleted</param>
        protected virtual void RemoveExtraItems(int itemDiff)
        {
            for (var i = _itemsCount; i < _itemsCount + itemDiff; i++)
            {
                if (_visibleItems.ContainsKey(i))
                {
                    HideItemAtIndex(i);
                }
            }
        }

        private void ClampContentPosition()
        {
            SetContentBounds();
            if (ContentPosition < 0)
            {
                ContentPosition = 0;
            }
            else if (AllItemsPositionsSet && _contentBottomRightCorner[_axis] >= content.sizeDelta[_axis])
            {
                ContentPosition = Mathf.Max(0, content.sizeDelta[_axis] - _viewPortSize[_axis]);
            }
            SetContentBounds();
        }

        protected virtual void RefreshAfterReload()
        {
        }

        /// <summary>
        /// Change the movement type of the scroll view, needed to keep track of internal _movementType
        /// </summary>
        /// <param name="type"></param>
        public void SetMovementType(MovementType type)
        {
            _movementType = type;
            movementType = type;
        }
        
        /// <summary>
        /// Scroll to top right
        /// </summary>
        /// <param name="timeOrSpeed">speed/time to scroll with</param>
        /// <param name="isSpeed">scrolls to top right using time value as speed</param>
        /// <param name="instant">instant scroll</param>
        /// <param name="ease">Tweening ease if DoTween or PrimeTween are being used</param>
        public void ScrollToTopRight(float timeOrSpeed = -1, bool isSpeed = false, bool instant = false, object ease = null)
        {
            ScrollToContentPosition(0, 0, timeOrSpeed, isSpeed, instant, ease, (state, scrollingDown) => PerformPostScrollingActions(false, state, scrollingDown));
        }
        
        public void ScrollToNormalisedPosition(float targetNormalizedPosition, float timeOrSpeed = -1, bool isSpeed = false, bool instant = false, object ease = null)
        {
            targetNormalizedPosition = Mathf.Clamp01(targetNormalizedPosition);
            var targetContentPosition = (content.rect.size[_axis] - _viewPortSize[_axis]) * targetNormalizedPosition * (vertical ? 1 : -1);
            ScrollToContentPosition(0, targetContentPosition, timeOrSpeed, isSpeed, instant, ease, (state, scrollingDown) => PerformPostScrollingActions(false, state, scrollingDown));
        }
        
        public void ScrollToItemIndex(int itemIndex, float timeOrSpeed = -1, bool isSpeed = false, bool instant = false, bool callEvent = false, object ease = null)
        {
            if (itemIndex < 0 || itemIndex >= _itemsCount)
            {
                Debug.LogError($"ScrollToItemIndex: itemIndex {itemIndex} is out of range (0 to {_itemsCount - 1})");
                return;
            }
            
            StopCoroutine(nameof(ScrollToItemIndexRoutine));
            StartCoroutine(ScrollToItemIndexRoutine(itemIndex, timeOrSpeed, isSpeed, instant, callEvent, ease));
        }
        
        private IEnumerator ScrollToItemIndexRoutine(int itemIndex, float timeOrSpeed, bool isSpeed, bool instant, bool callEvent, object ease)
        {
            var itemPosition = _itemPositions[itemIndex];
            var animationTimeLeft = timeOrSpeed;
            AnimationState animationState;
            
            while (!itemPosition.positionSet)
            {
                animationState = AnimationState.Animating; 
                var estimatedItemTop = itemIndex * (AverageItemSize + _spacing[_axis]) * (vertical ? 1 : -1);
                ScrollToContentPosition(itemIndex, estimatedItemTop, animationTimeLeft, isSpeed, instant, ease, (state, scrollingDown) =>
                {
                    animationState = state;
                    // The reason this is here and not after the yield return is that I want to call PerformPostScrollingActions immediately and not wait until the next frame
                    PerformPostScrollingActions(false, state, scrollingDown, itemIndex);
                });
                yield return new WaitUntil(() => animationState == AnimationState.Finished || animationState == AnimationState.Canceled || itemPosition.positionSet);
                if (animationState == AnimationState.Canceled)
                {
                    yield break;
                }
                
                if (itemPosition.positionSet)
                {
                    animationTimeLeft = _scrollAnimationController.GetAnimationRemainingTime(); 
                }
                else
                {
                    animationTimeLeft = 0;
                }
            }
            
            // if all item positions are known, we can clamp the content position to avoid over shooting
            var itemPositionFinal = itemPosition.absTopLeftPosition[_axis] * (vertical ? 1 : -1);
            if (AllItemsPositionsSet)
            {
                var minContentPosition = vertical ? 0 : (content.rect.size[_axis] - _viewPortSize[_axis]) * (vertical ? 1 : -1);
                var maxContentPosition = vertical ? content.rect.size[_axis] - _viewPortSize[_axis] : 0;
                itemPositionFinal = Mathf.Clamp(itemPositionFinal, minContentPosition, maxContentPosition);
            }
            ScrollToContentPosition(itemIndex, itemPositionFinal, animationTimeLeft, isSpeed, instant, ease, (state, scrollingDown) => PerformPostScrollingActions(callEvent, state, scrollingDown, itemIndex));
        }
        
        private void ScrollToContentPosition(int itemIndex, float targetContentPosition, float timeOrSpeed, bool isSpeed, bool instant, object ease = null, Action<AnimationState, bool> animationFinished = null)
        {
            PerformPreScrollingActions(itemIndex);
            var scrollingDown = Mathf.Abs(targetContentPosition) >= Mathf.Abs(ContentPosition);
            if (instant || timeOrSpeed <= 0)
            {
                ContentPosition = targetContentPosition;
                animationFinished?.Invoke(AnimationState.Finished, scrollingDown);
                return;
            }
            
            switch (_scrollAnimationController)
            {
#if DOTWEEN
                case DoTweenScrollAnimationController doTween:
                    var doEase = ease is DG.Tweening.Ease de ? de : DG.Tweening.Ease.Linear;
                    doTween.ScrollToContentPosition(targetContentPosition, timeOrSpeed, isSpeed, doEase, state => animationFinished?.Invoke(state, scrollingDown)); 
                    break;
#endif

#if PRIMETWEEN
                case PrimeTweenScrollAnimationController primeTween:
                    var primeEase = ease is PrimeTween.Ease pe ? pe : PrimeTween.Ease.Linear;
                    primeTween.ScrollToContentPosition(targetContentPosition, timeOrSpeed, isSpeed, primeEase, state => animationFinished?.Invoke(state, scrollingDown));
                    break;
#endif

                default:
                    _scrollAnimationController.ScrollToContentPosition(targetContentPosition, timeOrSpeed, isSpeed, state => animationFinished?.Invoke(state, scrollingDown));
                    break;
            }
        }

        protected virtual void PerformPreScrollingActions(int itemIndex)
        {
            _scrollAnimationController.CancelCurrentAnimation();
            // late update handles setting the movementType back to _movementType
            movementType = MovementType.Unrestricted;
            StopMovement();
            _isAnimating = true;
        }

        protected virtual void PerformPostScrollingActions(bool callEvent, AnimationState animationState, bool scrollingDown, int itemIndex = -1)
        {
            _isAnimating = false;
            StopMovement();
            SetContentBounds();
            ShowHideItems(scrollingDown);
            
            if (animationState == AnimationState.Finished && callEvent)
            {
                var actualItemIndex = GetActualItemIndex(itemIndex);
                _dataSource.ScrolledToItem(_visibleItems[itemIndex].item, actualItemIndex);
            }
        }
        
        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (_isAnimating)
            {
                return;
            }
            base.OnBeginDrag(eventData);
        }

        /// <summary>
        /// Organize the items in the hierarchy based on its visibility
        /// Its only used for organization
        /// </summary>
        /// <param name="item">item which will have its hierarchy properties changed</param>
        /// <param name="visible">visibility of item index</param>
        private void SetVisibilityInHierarchy(RectTransform item, bool visible)
        {
#if UNITY_EDITOR
            var itemTransform = item.transform;
            itemTransform.hideFlags = visible ? HideFlags.None : HideFlags.HideInHierarchy;
#endif
        }
        
        public Item? GetItemAtIndex(int itemIndex)
        {
            if (_visibleItems == null || _visibleItems.Count <= 0)
                return null;
            if (_visibleItems.TryGetValue( itemIndex, out var item ))
                return item;
            return null;
        }

        public bool IsItemPartiallyVisible(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= _itemPositions.Count)
            {
                return false;
            }

            if (_visibleItems.TryGetValue(itemIndex, out _))
            {
                var itemPosition = _itemPositions[itemIndex];
                if (itemPosition.positionSet)
                {
                    if (itemPosition.absTopLeftPosition[_axis] <= _contentBottomRightCorner[_axis] && itemPosition.absBottomRightPosition[_axis] >= _contentTopLeftCorner[_axis])
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        public bool IsItemFullyVisible(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= _itemPositions.Count)
            {
                return false;
            }
            
            if (_visibleItems.TryGetValue(itemIndex, out _))
            {
                var itemPosition = _itemPositions[itemIndex];
                if (itemPosition.positionSet)
                {
                    if (itemPosition.absTopLeftPosition[_axis] >= _contentTopLeftCorner[_axis] && itemPosition.absBottomRightPosition[_axis] <= _contentBottomRightCorner[_axis])
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}