using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclableSR
{
    public class RSRBase : ScrollRect
    {
        // TODO: reverse direction for cards
        // TODO: Remove SetCardsZIndices from RSRPages and put it in RSRCards and change it to SetIndices
        // TODO: Rework cards behaviours
        // TODO: remove _manuallyHandleCardAnimations
        
        // TODO: encapsulate grid data and functions
        // TODO: maybe when removing extra items, dont remove extraRowsColumnsItems in both grid and rsr
        
        // TODO: different start axes for grid layout
        // TODO: FixedColumnCount with Vertical Grids & FixedRowCount with Horizontal Grids (remaining _maxExtraVisibleItemInViewPort needs to be / _maxGridsItemsInAxis
        // TODO: Fix all behaviours for gridLayout and make sure _reverseDirection is working properly
        // TODO: Add support for start corners
        // TODO: make this class abstract
        
        // TODO: Separate Scrolling animation
        // TODO: Redo Scrolling animation
        // TODO: remove _maxExtraVisibleItemInViewPort from here and move them to RSR, rename ones in RSRGrid to maxRowColumn
        
        // TODO: Maybe remove ScrolledToItem event call in pages?
        // TODO: check todos in RSRPages
        // TODO: check todos in this class
        // TODO: Add headers, footers, sections
        // TODO: Check TODOs in RSRBaseEditor
        // TODO: i don't like static items?
        
        [SerializeField] protected bool _reverseDirection;
        [SerializeField] protected bool _childForceExpand;
        [SerializeField] private float _pullToRefreshThreshold = 150;
        [SerializeField] private float _pushToCloseThreshold = 150;
        [SerializeField] private bool _useConstantScrollingSpeed;
        [SerializeField] private float _constantScrollingSpeed;
        
        [SerializeField] protected Vector2 _spacing;
        [SerializeField] protected RectOffset _padding;
        [SerializeField] protected TextAnchor _childAlignment;
        
        protected IDataSource _dataSource;

        protected LayoutElement _layoutElement;
        private ScreenResolutionDetector _screenResolutionDetector;

        protected List<ItemPosition> _itemPositions;
        private List<bool> _staticItems;
        private List<string> _prototypeNames;
        
        protected int _axis;
        protected int _itemsCount;
        protected int _currentPage;
        protected int _minVisibleItemInViewPort;
        protected int _maxVisibleItemInViewPort;
        protected int _minExtraVisibleItemInViewPort;
        protected int _maxExtraVisibleItemInViewPort;
        private int _queuedScrollToItem;
        protected bool _isAnimating;
        private bool _init;
        private bool _needsClearance;
        private bool _pullToRefresh;
        private bool _pushToClose;
        private bool _canCallReachedScrollEnd;
        private bool _canCallReachedScrollStart;
        private bool _isApplicationQuitting;

        protected SortedDictionary<int, Item> _visibleItems;
        protected HashSet<int> _ignoreSetItemDataIndices;
        private HashSet<int> _itemsMarkedForReload;
        private Dictionary<string, List<Item>> _pooledItems;
        private Dictionary<int, HashSet<string>> _reloadTags;
        
        protected Vector2 _dragStartingPosition;
        protected Vector2 _contentTopLeftCorner;
        protected Vector2 _contentBottomRightCorner;
        private Vector2 _viewPortSize;
        private Vector2 _lastContentPosition;
        private MovementType _movementType;
        private MovementType _initialMovementType;

        public bool IsInitialized => _init;
        protected virtual bool ReachedMinItemInViewPort => false;
        protected virtual bool ReachedMaxItemInViewPort => false;

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
                _layoutElement = content.gameObject.AddComponent<LayoutElement>();

            _axis = vertical ? 1 : 0;
            _initialMovementType = movementType;
            
            InitializeData();
        }
        
        /// <summary>
        /// Reload the data in case the content of the RecyclableScrollRect has changed
        /// </summary>
        private void InitializeData()
        {
            _init = false;
            
            if (_visibleItems != null)
            {
                foreach (var visibleItem in _visibleItems)
                {
                    if (!_staticItems[visibleItem.Key])
                        Destroy(visibleItem.Value.transform.gameObject);
                }
                _visibleItems.Clear();
            }

            StopMovement();
            
            _minVisibleItemInViewPort = 0;
            _minExtraVisibleItemInViewPort = 0;
            _maxVisibleItemInViewPort = 0;
            _maxExtraVisibleItemInViewPort = 0;
            
            _currentPage = 0;

            var zeroContentPosition = content.anchoredPosition;
            zeroContentPosition[_axis] = 0;
            m_ContentStartPosition = zeroContentPosition;
            content.anchoredPosition = zeroContentPosition;
            GetContentBounds();

            _itemsCount = _dataSource.ItemsCount;
            _staticItems = new List<bool>();
            _prototypeNames = new List<string>();
            _itemPositions = new List<ItemPosition>();
            _reloadTags = new Dictionary<int, HashSet<string>>();
            _itemsMarkedForReload = new HashSet<int>();
            _ignoreSetItemDataIndices = new HashSet<int>();
            _lastContentPosition = _contentTopLeftCorner;
            SetMovementType(_initialMovementType);

            _visibleItems = new SortedDictionary<int, Item>();
            
            if (_pooledItems == null)
                _pooledItems = new Dictionary<string, List<Item>>();
            
            // create a new list for each prototype items to hold the pooled items
            var prototypeItems = _dataSource.PrototypeItems;
            for (var i = 0; i < prototypeItems.Length; i++)
            {
                if (!_pooledItems.ContainsKey(prototypeItems[i].name))
                    _pooledItems.Add(prototypeItems[i].name, new List<Item>());
            }

            ResetVariables();
            SetContentAnchorsPivot();
            InitializeItemPositions();
            CalculateContentSize();
            CalculatePadding();
            SetStaticItems();
            HideStaticItems();
            SetPrototypeNames();
            InitializeItems();
            RefreshAfterReload(false);

            _init = true;
        }

        /// <summary>
        /// Sets the content anchors and pivot based on the direction of the scroll and if it's reversed or not
        /// </summary>
        private void SetContentAnchorsPivot()
        {
            if (_reverseDirection)
            {
                if (vertical)
                {
                    content.anchorMin = Vector2.zero;
                    content.anchorMax = new Vector2(1, 0);
                    content.pivot = Vector2.zero;
                }
                else
                {
                    content.anchorMin = new Vector2(1, 0);
                    content.anchorMax = new Vector2(1, 1);
                    content.pivot = new Vector2(1, 1);
                }
            }
            else
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
        }

        /// <summary>
        /// A common function to reset variables when calling ResetData or ReloadData
        /// </summary>
        protected virtual void ResetVariables()
        {
            _isAnimating = false;
            _queuedScrollToItem = -1;
            _canCallReachedScrollStart = true;
            _canCallReachedScrollEnd = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Application.isPlaying && !_isApplicationQuitting && ScreenResolutionDetector.Instance != null)
                ScreenResolutionDetector.Instance.OnResolutionChanged -= UpdateContentLayouts;
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
        /// get the index of the item
        /// </summary>
        /// <returns></returns>
        protected virtual int GetActualItemIndex(int itemIndex)
        {
            return itemIndex;
        }

        /// <summary>
        /// Calculate the content size in their respective direction based on the scrolling direction
        /// If the item size is know we simply add all the item sizes, spacing and padding
        /// If not we set the item size as -1 as it will be calculated once the item comes into view
        /// </summary>
        protected virtual void CalculateContentSize()
        {
        }

        /// <summary>
        /// used to calculate the padding in grid layout, it's a separate function because it needs to be called in a certain order when initializing everything
        /// </summary>
        protected virtual void CalculatePadding()
        {
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

                    if (_dataSource.IsSetVisibleUsingCanvasGroupAlpha)
                    {
                        var item = itemRect.GetComponent<IItem>() ?? itemRect.gameObject.AddComponent<BaseItem>();
                        item.CanvasGroup.alpha = 0;
                        item.CanvasGroup.interactable = false;
                        item.CanvasGroup.blocksRaycasts = false;
                    }
                    else
                        itemRect.gameObject.SetActive(false);
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
                        var isVisible = _visibleItems.ContainsKey(i);
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
                _itemPositions.Add(new ItemPosition());
        }

        /// <summary>
        /// Initialize all items needed until the view port is filled
        /// extra visible items is an additional amount of items that can be shown to prevent showing an empty view port if the scrolling is too fast and the update function didn't show all the items
        /// that need to be shown
        /// </summary>
        /// <param name="startIndex">the starting item index on which we want initialized</param>
        protected virtual void InitializeItems(int startIndex = 0)
        {
        }

        /// <summary>
        /// Initialize the items
        /// Its only called when there are no pooled items available and the RecyclableScrollRect needs to show an item
        /// </summary>
        /// <param name="index"></param>
        private void InitializeItem(int index)
        {
            var actualItemIndex = GetActualItemIndex(index);
            var itemPrototypeItem = _dataSource.GetItemPrototype(actualItemIndex);

            GameObject itemGo;
            IItem itemImpl;
            if (!_staticItems[index])
            {
                itemGo = Instantiate(itemPrototypeItem, content, false);
                itemImpl = itemGo.GetComponent<IItem>();
                itemImpl.RSRBase = this;
                itemImpl.ItemIndex = index;
                itemGo.name = itemPrototypeItem.name + " " + index;
            }
            else
            {
                itemGo = itemPrototypeItem;
                itemImpl = itemGo.GetComponent<IItem>() ?? itemGo.AddComponent<BaseItem>();
                itemImpl.RSRBase = this;
                itemImpl.ItemIndex = index;
                
                SetVisibilityInHierarchy((RectTransform)itemGo.transform, true);
                itemGo.SetActive(true);
                if (_dataSource.IsSetVisibleUsingCanvasGroupAlpha)
                {
                    itemImpl.CanvasGroup.alpha = 1;
                    itemImpl.CanvasGroup.interactable = true;
                    itemImpl.CanvasGroup.blocksRaycasts = true;
                }
            }

            var rect = itemGo.transform as RectTransform;
            var item = new Item(itemImpl, rect);
            _visibleItems.Add(index, item);
            _dataSource.ItemCreated(actualItemIndex, itemImpl, itemGo);
            
            CalculateNonAxisSizePosition(rect, index);
            SetItemAxisPosition(rect, index);
            _dataSource.SetItemData(itemImpl, actualItemIndex);
            CalculateItemAxisSize(rect, index);
        }
        
        /// <summary>
        /// Hides private ReloadItem implementation to avoid calling it with unneeded variables (isReloadingAllData)
        /// </summary>
        /// <param name="itemIndex">item index to reload</param>
        /// <param name="reloadTag">used to reload item based on a tag, so if the reload is called more than once with the same tag, we can ignore it</param>
        /// <param name="reloadItemData">when set true, it will fetch item data from IDataSource</param>
        public void ReloadItem(int itemIndex, string reloadTag = "", bool reloadItemData = false)
        {
            ReloadItemInternal(itemIndex, reloadTag, reloadItemData);
        }

        /// <summary>
        /// Creates and checks tags for reloading items, this avoids calling calculating the item size if it's called with same tag more than once and only calls ForceLayoutRebuild
        /// </summary>
        /// <param name="itemIndex">item index to reload</param>
        /// <param name="reloadTag">used to reload item based on a tag, so if the reload is called more than once with the same tag, we can ignore it</param>
        /// <param name="reloadItemData">when set true, it will fetch item data from IDataSource</param>
        /// <param name="isReloadingAllData">Used to prevent calling CalculateNewMinMaxItemsAfterReloadItem in ReloadItemInternal each time when this function is called from ReloadData since we call
        /// CalculateNewMinMaxItemsAfterReloadItem at the end of ReloadData</param> 
        protected virtual void ReloadItemInternal(int itemIndex, string reloadTag = "", bool reloadItemData = false, bool isReloadingAllData = false)
        {
            // add the reloadTag if it doesn't exist for the itemIndex
            // if reloading data, clear all the reloadTags for the itemIndex
            if (reloadItemData && _reloadTags.TryGetValue( itemIndex, out var itemTags ))
                itemTags.Clear();

            if (!string.IsNullOrEmpty(reloadTag))
            {
                if (_reloadTags.ContainsKey(itemIndex))
                {
                    if (_reloadTags[itemIndex].Contains(reloadTag))
                    {
                        ForceLayoutRebuild(itemIndex);
                        return;
                    }
                    else
                        _reloadTags[itemIndex].Add(reloadTag);
                }
                else
                {
                    _reloadTags.Add(itemIndex, new HashSet<string>{reloadTag});
                }
            }
            
            // No need to reload item at index {itemIndex} as its currently not visible and everything will be automatically handled when it appears
            // it's ok to return here after setting the tag, as if an item gets marked for reload with multiple tags, it only needs to reload once its visible
            // reloading the item multiple times with different tags is needed when multiple changes happen to an item over the course of some frames when its visible
            if (!_visibleItems.ContainsKey(itemIndex))
            {
                _itemsMarkedForReload.Add(itemIndex);
                return;
            }

            // item has been deleted, no need to reload
            if (itemIndex >= _itemsCount)
                return;
            
            var visibleItem = _visibleItems[itemIndex];
            if (reloadItemData)
            {
                var actualItemIndex = GetActualItemIndex(itemIndex);
                if (actualItemIndex != -1)
                    _dataSource.SetItemData(visibleItem.item, actualItemIndex);
            }
        }

        /// <summary>
        /// Forces a layout to rebuild in case it was called with a tag that already exists,
        /// This means that the size is already known, we just need to make sure the item looks right
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <returns></returns>
        protected void ForceLayoutRebuild(int itemIndex)
        {
            if (_visibleItems.ContainsKey(itemIndex))
            {
                RectTransform[] rects = null;
                if (!_staticItems[itemIndex])
                    rects = _visibleItems[itemIndex].item.ItemsNeededForVisualUpdate;

                if (rects != null)
                {
                    foreach (var rect in rects)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(_visibleItems[itemIndex].transform);
            }
        }

        /// <summary>
        /// This function call is only needed when the item is created, or when the resolution changes
        /// it sets the vertical size of the item in a horizontal layout
        /// or the horizontal size of an item in a vertical layout based on the settings of said layout
        /// It also sets the vertical position in horizontal layout or the horizontal position in a vertical layout based on the padding of said layout
        /// Is not needed in grid as items will have different positions in non axis position and the non axis size is the same in all of them
        /// </summary>
        /// <param name="rect">The rect of the item that its size will be adjusted</param>
        /// <param name="itemIndex">The index of the item that its size will be adjusted</param>
        protected virtual void CalculateNonAxisSizePosition(RectTransform rect, int itemIndex)
        {
            Vector2 anchorVector;
            if (_reverseDirection)
            {
                if (vertical)
                    anchorVector = new Vector2(0, 0);
                else
                    anchorVector = new Vector2(1, 1);
            }
            else
            {
                anchorVector = new Vector2(0, 1);
            }

            rect.anchorMin = anchorVector;
            rect.anchorMax = anchorVector;
            rect.pivot = anchorVector;
        }
        
        /// <summary>
        /// Used to force set item position, can be used if the item position is manipulated externally and later would want to restore it.
        /// It doesn't need to set the position of an invisible item as it will get set automatically when its in view 
        /// </summary>
        /// <param name="itemIndex">item index to set position to</param>
        public void SetItemPosition(int itemIndex)
        {
            if (!_visibleItems.TryGetValue(itemIndex, out var visibleItem))
                return;

            SetItemAxisPosition(visibleItem.transform, itemIndex);
        }

        /// <summary>
        /// This function sets the position of the item whether its new or retrieved from pool based on its index and the previous item index
        /// The current index position is the previous item position + previous item height
        /// or the previous item position - current item height
        /// </summary>
        /// <param name="rect">rect of the item which position will be set</param>
        /// <param name="newIndex">index of the item that needs its position set</param>
        protected virtual void SetItemAxisPosition(RectTransform rect, int newIndex)
        {
        }

        /// <summary>
        /// This function calculates the item size if its unknown by forcing a Layout rebuild
        /// if it is known we just get the item size
        /// </summary>
        /// <param name="rect">rect of the item which the size will be calculated for</param>
        /// <param name="index">item index which the size will be calculated for</param>
        protected virtual void CalculateItemAxisSize(RectTransform rect, int index)
        {
        }

        /// <summary>
        /// The function in which we calculate which items need to be shown and which items need to hide
        /// </summary>
        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (!_init)
                return;
            if (_visibleItems.Count <= 0)
                return;
            var currentContentAnchoredPosition = content.anchoredPosition * ((vertical ? 1f : -1f) * (_reverseDirection ? -1f : 1f));
            if (Mathf.Approximately(currentContentAnchoredPosition[_axis], _lastContentPosition[_axis]) && !_needsClearance)
                return;
            
            GetContentBounds();
            
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
            var atEnd = _contentBottomRightCorner[_axis] >= content.rect.size[_axis] && ReachedMaxItemInViewPort;
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
            
            int topLeftPadding;
            int bottomLeftPadding;
            if (_reverseDirection)
            {
                topLeftPadding = vertical ? _padding.bottom : _padding.right;
                bottomLeftPadding = vertical ? _padding.top : _padding.left;
            }
            else
            {
                topLeftPadding = vertical ? _padding.top : _padding.left;
                bottomLeftPadding = vertical ? _padding.bottom : _padding.right;
            }
            
            var topLeftMinClearance = 0.1f + topLeftPadding * (ReachedMinItemInViewPort ? 1 : 0) + _spacing[_axis] * (ReachedMinItemInViewPort ? 0 : 1);
            var bottomRightMinClearance = 0.1f + bottomLeftPadding * (ReachedMaxItemInViewPort ? 1 : 0) + _spacing[_axis] * (ReachedMaxItemInViewPort ? 0 : 1);
            
            if (_itemPositions[_minVisibleItemInViewPort].absTopLeftPosition[_axis] - _contentTopLeftCorner[_axis] > topLeftMinClearance && !ReachedMinItemInViewPort)
            {
                showBottomRight = false;
                _needsClearance = true;
            }
            else if (_itemPositions[_maxVisibleItemInViewPort].absBottomRightPosition[_axis] - _contentBottomRightCorner[_axis] < -bottomRightMinClearance && !ReachedMaxItemInViewPort)
            {
                showBottomRight = true;
                _needsClearance = true;
            }
            _lastContentPosition = currentContentAnchoredPosition;

            if (reachedLimits && !_needsClearance)
                return;
            
            if (showBottomRight)
            {
                HideItemsAtTopLeft();
                ShowItemsAtBottomRight();
            }
            else
            {
                HideItemsAtBottomRight();
                ShowItemsAtTopLeft();
            }
        }
        
        /// <summary>
        /// hide items at top left 
        /// </summary>
        protected virtual void HideItemsAtTopLeft()
        {
        }
        
        /// <summary>
        ///  show items at bottom right
        /// </summary>
        protected virtual void ShowItemsAtBottomRight()
        {
        }

        /// <summary>
        /// hide items at bottom right
        /// </summary>
        protected virtual void HideItemsAtBottomRight()
        {
        }
        
        /// <summary>
        /// show items at top left
        /// </summary>
        protected virtual void ShowItemsAtTopLeft()
        {
        }

        /// <summary>
        /// User has scrolled, and we need to show an item
        /// If there is a pooled item available, we get it and set its position, sibling index, and remove it from the pool
        /// If there is no pooled item available, we create a new one
        /// </summary>
        /// <param name="newIndex">current index of item we need to show</param>
        protected void ShowItemAtIndex(int newIndex)
        {
            // Get empty item and adjust its position and size, else just create a new an item
            var itemPrototypeName = _prototypeNames[newIndex];
            if (_pooledItems[itemPrototypeName].Count > 0)
            {
                var item = _pooledItems[itemPrototypeName][0];
                _pooledItems[itemPrototypeName].RemoveAt(0);

                if (_dataSource.IsSetVisibleUsingCanvasGroupAlpha)
                {
                    item.item.CanvasGroup.alpha = 1;
                    item.item.CanvasGroup.interactable = true;
                    item.item.CanvasGroup.blocksRaycasts = true;
                }
                else
                {
                    item.transform.gameObject.SetActive(true);
                }

                SetVisibilityInHierarchy(item.transform, true);

                _visibleItems.Add(newIndex, item);
                item.item.ItemIndex = newIndex;
                
                SetItemAxisPosition(item.transform, newIndex);
                if (_ignoreSetItemDataIndices.Count <= 0 || _ignoreSetItemDataIndices.Count > 0 && !_ignoreSetItemDataIndices.Contains(newIndex))
                {
                    var actualItemIndex = GetActualItemIndex(newIndex);
                    _dataSource.SetItemData(item.item, actualItemIndex);
                }

                CalculateItemAxisSize(item.transform, newIndex);
                
                if (_itemsMarkedForReload.Contains(newIndex))
                {
                    // item needs to be reloaded
                    ReloadItem(newIndex);
                    _itemsMarkedForReload.Remove(newIndex);
                }
                
                if (!_staticItems[newIndex])
                    item.transform.name = itemPrototypeName + " " + newIndex;
            }
            else
            {
                InitializeItem(newIndex);
            }

            if (_queuedScrollToItem != -1 && _queuedScrollToItem == newIndex)
            {
                var actualItemIndex = GetActualItemIndex(newIndex);
                _dataSource.ScrolledToItem(_visibleItems[newIndex].item, actualItemIndex);
                _queuedScrollToItem = -1;
            }

            SetSiblingIndices();
            
            if (newIndex == _itemsCount - 1)
                _dataSource.LastItemInScrollIsVisible();
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
            if (_dataSource.IsSetVisibleUsingCanvasGroupAlpha)
            {
                _visibleItems[itemIndex].item.CanvasGroup.alpha = 0;
                _visibleItems[itemIndex].item.CanvasGroup.interactable = false;
                _visibleItems[itemIndex].item.CanvasGroup.blocksRaycasts = false;
            }
            else
                _visibleItems[itemIndex].transform.gameObject.SetActive(false);
            
            var actualItemIndex = GetActualItemIndex(itemIndex);
            SetVisibilityInHierarchy(_visibleItems[itemIndex].transform, false);
            _dataSource.ItemHidden(_visibleItems[itemIndex].item, actualItemIndex);
            _pooledItems[_prototypeNames[itemIndex]].Add(_visibleItems[itemIndex]);
            _visibleItems.Remove(itemIndex);
        }

        /// <summary>
        /// Updates content bounds for different uses
        /// </summary>
        protected void GetContentBounds()
        {
            _viewPortSize = viewport.rect.size;
            _contentTopLeftCorner = content.anchoredPosition * ((vertical ? 1f : -1f) * (_reverseDirection ? -1f : 1f));
            _contentBottomRightCorner[1 - _axis] = _contentTopLeftCorner[1 - _axis];
            _contentBottomRightCorner[_axis] = _contentTopLeftCorner[_axis] + _viewPortSize[_axis];
        }

        /// <summary>
        /// Reload data in scroll view
        /// </summary>
        /// <param name="reloadAllItems">should be only used when adding items to the top of the current visible items</param>
        public virtual void ReloadData(bool reloadAllItems = false)
        {
            var oldItemsCount = _itemsCount;
            _itemsCount = _dataSource.ItemsCount;
            
            // removes extra items
            if (oldItemsCount > _itemsCount)
            {
                var itemDiff = oldItemsCount - _itemsCount;
                if (_visibleItems.Count > _itemsCount)
                {
                    RemoveExtraItems(itemDiff);
                }

                _itemPositions.RemoveRange(_itemsCount, itemDiff);
                _prototypeNames.RemoveRange(_itemsCount, itemDiff);
                _staticItems.RemoveRange(_itemsCount, itemDiff);
            }

            ResetVariables();
            SetContentAnchorsPivot();
            InitializeItemPositions();
            CalculateContentSize();
            CalculatePadding();
            SetStaticItems();
            SetPrototypeNames();

            if (reloadAllItems)
            {
                foreach (var item in _visibleItems)
                    ReloadItemInternal(item.Key, "", true, true);
            }
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

        protected virtual void RefreshAfterReload(bool reloadAllItems)
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
        /// Scrolls to top of scrollRect
        /// </summary>
        public virtual void ScrollToTopRight()
        {
            StopMovement();
        }

        /// <summary>
        /// A loop for animating to a desired NormalisedPosition
        /// </summary>
        /// <param name="targetNormalisedPos">required normalisedPosition</param>
        /// <returns></returns>
        protected IEnumerator ScrollToTargetNormalisedPosition(float targetNormalisedPos)
        {
            _isAnimating = true;
            var currentNormalisedPosition = Mathf.Clamp01(normalizedPosition[_axis]);
            var increment = 1f / _itemsCount;
            if (Mathf.Abs(currentNormalisedPosition - targetNormalisedPos) <= increment || currentNormalisedPosition > 1 || currentNormalisedPosition < 0)
            {
                SetNormalizedPosition(targetNormalisedPos, _axis);
                _isAnimating = false;
            }
            else
            {
                var sign = currentNormalisedPosition < targetNormalisedPos ? 1 : -1;
                var newNormalisedPosition = currentNormalisedPosition + increment * sign;
                SetNormalizedPosition(newNormalisedPosition, _axis);
                yield return new WaitForEndOfFrame();
                StartCoroutine(ScrollToTargetNormalisedPosition(targetNormalisedPos));
            }
        }

        /// <summary>
        /// Scroll to a certain item index
        /// if the item position is known, go to it,
        /// if not keep scrolling until its known
        /// </summary>
        /// <param name="itemIndex">item index needed to scroll to</param>
        /// <param name="callEvent">call scroll to item event, usually not needed when calling ScrollToItem from OnEndDrag if RSR is paged</param>
        /// <param name="instant">scroll instantly</param>
        /// <param name="maxSpeedMultiplier">a multiplier that is used at max speed for scrolling</param>
        /// <param name="offset">value to offset target scroll position with</param>
        public void ScrollToItem(int itemIndex, bool callEvent = true, bool instant = false, float maxSpeedMultiplier = 1, float offset = 0)
        {
            StopMovement();
            var direction = itemIndex > _currentPage ? 1 : -1;
            PreformPreScrollingActions(itemIndex, direction);
            
            var itemVisiblePositionKnown = _itemPositions[itemIndex].positionSet && _visibleItems.ContainsKey(itemIndex);
            if (itemVisiblePositionKnown && instant)
            {
                var currentContentPosition = content.anchoredPosition;
                currentContentPosition[_axis] = _itemPositions[itemIndex].absTopLeftPosition[_axis] * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1)) + offset;
                content.anchoredPosition = currentContentPosition;
                m_ContentStartPosition = currentContentPosition;
                if (callEvent)
                {
                    var actualItemIndex = GetActualItemIndex(itemIndex);
                    _dataSource.ScrolledToItem(_visibleItems[itemIndex].item, actualItemIndex);
                }

                PreformPostScrollingActions(itemIndex, true);
                _isAnimating = false;
            }
            else
            {
                float speedToUse;
                if (_useConstantScrollingSpeed && !instant)
                {
                    speedToUse = _constantScrollingSpeed;
                }
                else
                {
                    var itemSizeAverage = 0f;
                    int i;
                    for (i = 0; i < _itemsCount; i++)
                    {
                        if (!_dataSource.IsItemSizeKnown && _itemPositions[i].sizeSet)
                        {
                            itemSizeAverage += _itemPositions[i].itemSize[_axis];
                        }
                        else if (_dataSource.IsItemSizeKnown)
                        {
                            var actualItemIndex = GetActualItemIndex(i);
                            itemSizeAverage += _dataSource.GetItemSize(actualItemIndex);
                        }
                        else
                        {
                            break;
                        }
                    }

                    itemSizeAverage /= i;
                    if (instant)
                    {
                        speedToUse = itemSizeAverage;
                    }
                    else
                    {
                        var scrollingDistance = Mathf.Max(1, Mathf.Abs(_minVisibleItemInViewPort - itemIndex));
                        var scrollingDistancePercentage = Mathf.Clamp01((float)scrollingDistance / Mathf.Min(10, _itemsCount));
                        var exponentialSpeed = (Mathf.Exp(scrollingDistancePercentage) - 1) * itemSizeAverage;
                        speedToUse = Mathf.Min(itemSizeAverage, exponentialSpeed);
                    }

                    if (speedToUse >= itemSizeAverage)
                        speedToUse *= maxSpeedMultiplier;
                }

                _isAnimating = true;
                var increment = speedToUse * direction * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
                StartCoroutine(StartScrolling(increment, direction, itemIndex, callEvent, offset));
            }
        }

        /// <summary>
        /// Coroutine that keeps scrolling until we find the designated scrollToTargetIndex which is set ShowItemAtIndex
        /// </summary>
        /// <param name="increment">increment in which we scroll by, based on direction, vertical or horizontal, instant scrolling or not</param>
        /// <param name="direction">direction of scroll, 1 for down or right, -1 for up or left</param>
        /// <param name="itemIndex">item index which we want to scroll to</param>
        /// <param name="callEvent">call scroll to item event, usually not needed when calling ScrollToItem from OnEndDrag if RSR is paged</param>
        /// <param name="offset">value to offset target scroll position with</param>
        private IEnumerator StartScrolling(float increment, int direction, int itemIndex, bool callEvent, float offset)
        {
            var reachedItem = false;        
            
            var contentTopLeftCorner = content.anchoredPosition;
            contentTopLeftCorner[_axis] += increment;

            if ( contentTopLeftCorner[ _axis ] >= content.sizeDelta[ _axis ] )
                contentTopLeftCorner[ _axis ] = content.sizeDelta[ _axis ] - _viewPortSize[_axis];
            
            var contentBottomRightCorner = contentTopLeftCorner;
            contentBottomRightCorner[_axis] += _viewPortSize[_axis] * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
            
            if (_itemPositions[itemIndex].positionSet)
            {
                var itemTopLeftCorner = _itemPositions[itemIndex].absTopLeftPosition[_axis] + offset * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
                if (direction == 1) 
                {
                    if (itemTopLeftCorner <= Mathf.Abs(contentTopLeftCorner[_axis]))
                    {
                        contentTopLeftCorner[_axis] = itemTopLeftCorner * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
                        reachedItem = true;
                    }

                    // reached bottom or right
                    else if (_maxExtraVisibleItemInViewPort == _itemsCount - 1 && Mathf.Abs(contentBottomRightCorner[_axis]) >= content.sizeDelta[_axis])
                    {
                        contentTopLeftCorner[_axis] = (content.rect.size[_axis] - _viewPortSize[_axis]) * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
                        reachedItem = true;
                    }
                }
                else 
                {
                    if (itemTopLeftCorner >= Mathf.Abs(contentTopLeftCorner[_axis]))
                    {
                        contentTopLeftCorner[_axis] = itemTopLeftCorner * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
                        reachedItem = true;
                    }

                    // reached top or left
                    else if (!_reverseDirection && (vertical && contentTopLeftCorner[_axis] <= 0 || !vertical && contentTopLeftCorner[_axis] >= 0)
                        || _reverseDirection && (vertical && contentTopLeftCorner[_axis] >= 0 || !vertical && contentTopLeftCorner[_axis] <= 0))
                    {
                        contentTopLeftCorner[_axis] = 0;
                        reachedItem = true;
                    }
                }
            }
            
            content.anchoredPosition = contentTopLeftCorner;
            m_ContentStartPosition = contentTopLeftCorner;

            if (reachedItem)
                StopMovement();

            yield return new WaitForEndOfFrame();
            if (!reachedItem)
            {
                StartCoroutine(StartScrolling(increment, direction, itemIndex, callEvent, offset));
            }
            else
            {
                if (callEvent)
                {
                    if (_visibleItems.TryGetValue(itemIndex, out var item))
                    {
                        var actualItemIndex = GetActualItemIndex(itemIndex);
                        _dataSource.ScrolledToItem(item.item, actualItemIndex);
                    }
                    else
                    {
                        _queuedScrollToItem = itemIndex;
                    }
                }

                PreformPostScrollingActions(itemIndex, false);
                _isAnimating = false;
                _ignoreSetItemDataIndices.Clear();
            }
        }

        protected virtual void PreformPreScrollingActions(int itemIndex, int direction)
        {
        }
        

        protected virtual void PreformPostScrollingActions(int itemIndex, bool instant)
        {
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
    }
}