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
        // TODO: different start axes for grid layout
        // TODO: FixedColumnCount with Vertical Grids & FixedRowCount with Horizontal Grids (remaining _maxExtraVisibleItemInViewPort needs to be / _maxGridsItemsInAxis
        // TODO: Separate Scrolling animation
        // TODO: Redo Scrolling animation
        // TODO: Remove SetCardsZIndices from RSRPages and put it in RSRCards and change it to SetIndices
        // TODO: Maybe remove ScrolledToCell event call in pages?
        // TODO: check todos in RSRPages
        // TODO: check todos in this class
        // TODO: Fix all behaviours for gridLayout and make sure _reverseDirection is working properly
        // TODO: Rework cards behaviours
        // TODO: Add headers, footers, sections
        // TODO: remove _manuallyHandleCardAnimations
        // TODO: Check TODOs in RSRBaseEditor
        // TODO: i don't like static cells?
        
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
        private List<bool> _staticCells;
        private List<string> _prototypeNames;
        
        protected int _axis;
        protected int _itemsCount;
        protected int _currentPage;
        protected int _extraItemsVisible;
        private int _minVisibleItemInViewPort;
        private int _maxVisibleItemInViewPort;
        private int _minExtraVisibleItemInViewPort;
        private int _maxExtraVisibleItemInViewPort;
        private int _queuedScrollToCell;
        private bool _init;
        private bool _isAnimating;
        private bool _needsClearance;
        private bool _pullToRefresh;
        private bool _pushToClose;
        private bool _canCallReachedScrollEnd;
        private bool _canCallReachedScrollStart;
        private bool _isApplicationQuitting;

        protected SortedDictionary<int, Item> _visibleItems;
        protected HashSet<int> _ignoreSetCellDataIndices;
        private HashSet<int> _itemsMarkedForReload;
        private Dictionary<string, List<Item>> _pooledItems;
        private Dictionary<int, HashSet<string>> _reloadTags;
        
        protected Vector2 _dragStartingPosition;
        private Vector2 _viewPortSize;
        private Vector2 _lastContentPosition;
        private Vector2 _contentTopLeftCorner;
        private Vector2 _contentBottomRightCorner;
        private MovementType _movementType;
        private MovementType _initialMovementType;

        public bool IsInitialized => _init;

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
            if (_dataSource.PrototypeCells == null || _dataSource.PrototypeCells.Length <= 0)
            {
                throw new ArgumentNullException(nameof(_dataSource.PrototypeCells), "RSR, No prototype cell defined IDataSource");
            }
            
            // Register event delegate for resolution change
            ScreenResolutionDetector.Instance.OnResolutionChanged += UpdateContentLayouts;

            // add a LayoutElement if not present to set the content size in case another element is controlling it 
            _layoutElement = content.gameObject.GetComponent<LayoutElement>();
            if (_layoutElement == null)
                _layoutElement = content.gameObject.AddComponent<LayoutElement>();

            _axis = vertical ? 1 : 0;
            _initialMovementType = movementType;
            
            ResetData();
        }
        
        /// <summary>
        /// Reload the data in case the content of the RecyclableScrollRect has changed
        /// </summary>
        public virtual void ResetData()
        {
            _init = false;
            
            if (_visibleItems != null)
            {
                foreach (var visibleItem in _visibleItems)
                {
                    if (!_staticCells[visibleItem.Key])
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
            _staticCells = new List<bool>();
            _prototypeNames = new List<string>();
            _itemPositions = new List<ItemPosition>();
            _reloadTags = new Dictionary<int, HashSet<string>>();
            _itemsMarkedForReload = new HashSet<int>();
            _ignoreSetCellDataIndices = new HashSet<int>();
            _extraItemsVisible = _dataSource.ExtraItemsVisible;
            _lastContentPosition = _contentTopLeftCorner;
            SetMovementType( _initialMovementType );

            _visibleItems = new SortedDictionary<int, Item>();
            
            if (_pooledItems == null)
                _pooledItems = new Dictionary<string, List<Item>>();
            
            // create a new list for each prototype cell to hold the pooled cells
            var prototypeCells = _dataSource.PrototypeCells;
            for (var i = 0; i < prototypeCells.Length; i++)
            {
                if (!_pooledItems.ContainsKey(prototypeCells[i].name))
                    _pooledItems.Add(prototypeCells[i].name, new List<Item>());
            }

            ResetVariables();
            SetContentAnchorsPivot();
            SetStaticCells();
            HideStaticCells();
            SetPrototypeNames();
            InitializeItemPositions();
            CalculateContentSize();
            CalculatePadding();
            InitializeCells();
            RefreshAfterReload();

            _init = true;
        }

        /// <summary>
        /// Sets the content anchors and pivot based on the direction of the scroll and if its reversed or not
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
        private void ResetVariables()
        {
            _isAnimating = false;
            _queuedScrollToCell = -1;
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
        /// Calculate the content size in their respective direction based on the scrolling direction
        /// If the cell size is know we simply add all the cell sizes, spacing and padding
        /// If not we set the cell size as -1 as it will be calculated once the cell comes into view
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
        /// Cache the static cells
        /// </summary>
        private void SetStaticCells()
        {
            for (var i = 0; i < _itemsCount; i++)
            {
                if (i < _staticCells.Count)
                    _staticCells[i] = _dataSource.IsCellStatic(i);
                else
                    _staticCells.Add(_dataSource.IsCellStatic(i));
            }
        }
        
        /// <summary>
        /// Hide the static cells at the start
        /// Their visibility will depend on whether they are in viewport or not
        /// </summary>
        private void HideStaticCells()
        {
            for (var i = 0; i < _itemsCount; i++)
            {
                if (_staticCells[i])
                {
                    RectTransform cellRect;
                    if (_visibleItems.TryGetValue( i, out var item ))
                        cellRect = item.transform;
                    else
                        cellRect = (RectTransform)_dataSource.GetPrototypeCell(i).transform;
                    SetVisibilityInHierarchy(cellRect, false);

                    if (_dataSource.IsSetVisibleUsingCanvasGroupAlpha)
                    {
                        var cell = cellRect.GetComponent<ICell>() ?? cellRect.gameObject.AddComponent<BaseCell>();
                        cell.CanvasGroup.alpha = 0;
                        cell.CanvasGroup.interactable = false;
                        cell.CanvasGroup.blocksRaycasts = false;
                    }
                    else
                        cellRect.gameObject.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// Set prefab names for when needed to retrieve from pool
        /// </summary>
        private void SetPrototypeNames()
        {
            // create an array of cells that need their prototype changed
            var changeCellPrototypeCellList = new List<int>();

            // set an array of prototype names to be used when getting the correct prefab for the cell index it exists in its respective pool
            for (var i = 0; i < _itemsCount; i++)
            {
                if (i < _prototypeNames.Count)
                {
                    var newPrototype = _dataSource.GetPrototypeCell(i).name;
                    var oldPrototype = _prototypeNames[i];
                    if (newPrototype != oldPrototype)
                        changeCellPrototypeCellList.Add(i);
                }
                else
                {
                    _prototypeNames.Add(_dataSource.GetPrototypeCell(i).name);
                }
            }
            
            for (var i = 0; i < changeCellPrototypeCellList.Count; i++)
                ChangeCellPrototype(changeCellPrototypeCellList);
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
        /// Initialize all cells needed until the view port is filled
        /// extra visible items is an additional amount of cells that can be shown to prevent showing an empty view port if the scrolling is too fast and the update function didnt show all the items
        /// that need to be shown
        /// </summary>
        /// <param name="startIndex">the starting cell index on which we want initialized</param>
        private void InitializeCells(int startIndex = 0)
        {
            GetContentBounds();
            var contentHasSpace = startIndex == 0 || _itemPositions[startIndex - 1].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
            var extraItemsInitialized = contentHasSpace ? 0 : _maxExtraVisibleItemInViewPort - _maxVisibleItemInViewPort;
            var i = startIndex;
            var gridHasSpace = CheckInitializeCellsExtraConditions(startIndex);
            while ((contentHasSpace || extraItemsInitialized < _extraItemsVisible) && gridHasSpace && i < _itemsCount)
            {
                ShowHideCellsAtIndex(i, true, GridLayoutPage.After);
                if (!contentHasSpace)
                    extraItemsInitialized++;
                else
                    _maxVisibleItemInViewPort = i;

                contentHasSpace = _itemPositions[i].absBottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
                gridHasSpace = CheckInitializeCellsExtraConditions(i);
                i++;
            }
            _maxExtraVisibleItemInViewPort = i - 1;
        }

        /// <summary>
        /// Check for extra conditions if needed in child classes when initializing cells
        /// </summary>
        /// <param name="cellIndex">cell index</param>
        /// <returns></returns>
        protected virtual bool CheckInitializeCellsExtraConditions(int cellIndex)
        {
            return true;
        }
        
        /// <summary>
        /// Initialize the cells
        /// Its only called when there are no pooled items available and the RecyclableScrollRect needs to show a cell
        /// </summary>
        /// <param name="index"></param>
        private void InitializeCell(int index)
        {
            var itemPrototypeCell = _dataSource.GetPrototypeCell(index);

            GameObject itemGo;
            ICell cell;
            if (!_staticCells[index])
            {
                itemGo = Instantiate(itemPrototypeCell, content, false);
                cell = itemGo.GetComponent<ICell>();
                cell.RSRBase = this;
                cell.CellIndex = index;
                itemGo.name = itemPrototypeCell.name + " " + index;
            }
            else
            {
                itemGo = itemPrototypeCell;
                cell = itemGo.GetComponent<ICell>() ?? itemGo.AddComponent<BaseCell>();
                cell.RSRBase = this;
                cell.CellIndex = index;
                
                SetVisibilityInHierarchy((RectTransform)itemGo.transform, true);
                itemGo.SetActive(true);
                if (_dataSource.IsSetVisibleUsingCanvasGroupAlpha)
                {
                    cell.CanvasGroup.alpha = 1;
                    cell.CanvasGroup.interactable = true;
                    cell.CanvasGroup.blocksRaycasts = true;
                }
            }

            var rect = itemGo.transform as RectTransform;
            var item = new Item(cell, rect);
            _visibleItems.Add(index, item);
            _dataSource.CellCreated(index, cell, itemGo);
            
            CalculateNonAxisSizePosition(rect, index);
            SetCellAxisPosition(rect, index);
            _dataSource.SetCellData(cell, index);
            CalculateCellAxisSize(rect, index);
        }
        
        /// <summary>
        /// Hides private ReloadCell implementation to avoid calling it with unneeded variables (isReloadingAllData)
        /// </summary>
        /// <param name="cellIndex">cell index to reload</param>
        /// <param name="reloadTag">used to reload cell based on a tag, so if the reload is called more than once with the same tag, we can ignore it</param>
        /// <param name="reloadCellData">when set true, it will fetch cell data from IDataSource</param>
        public void ReloadCell(int cellIndex, string reloadTag = "", bool reloadCellData = false)
        {
            PreReloadCell(cellIndex, reloadTag, reloadCellData);
        }

        /// <summary>
        /// Creates and checks tags for reloading cells, this avoids calling calculating the cell size if it's called with same tag more than once and only calls ForceLayoutRebuild
        /// </summary>
        /// <param name="cellIndex">cell index to reload</param>
        /// <param name="reloadTag">used to reload cell based on a tag, so if the reload is called more than once with the same tag, we can ignore it</param>
        /// <param name="reloadCellData">when set true, it will fetch cell data from IDataSource</param>
        /// <param name="isReloadingAllData">Used to prevent calling CalculateNewMinMaxItemsAfterReloadCell in ReloadCellInternal each time when this function is called from ReloadData since we call
        /// CalculateNewMinMaxItemsAfterReloadCell at the end of ReloadData</param> 
        private void PreReloadCell(int cellIndex, string reloadTag = "", bool reloadCellData = false, bool isReloadingAllData = false)
        {
            // add the reloadTag if it doesn't exist for the cellIndex
            // if reloading data, clear all the reloadTags for the cellIndex
            if (reloadCellData && _reloadTags.TryGetValue( cellIndex, out var cellTags ))
                cellTags.Clear();

            if (!string.IsNullOrEmpty(reloadTag))
            {
                if (_reloadTags.ContainsKey(cellIndex))
                {
                    if (_reloadTags[cellIndex].Contains(reloadTag))
                    {
                        ForceLayoutRebuild(cellIndex);
                        return;
                    }
                    else
                        _reloadTags[cellIndex].Add(reloadTag);
                }
                else
                {
                    _reloadTags.Add(cellIndex, new HashSet<string>{reloadTag});
                }
            }
            ReloadCellInternal(cellIndex, reloadCellData, isReloadingAllData);
        }

        /// <summary>
        /// Forces a layout to rebuild in case it was called with a tag that already exists,
        /// This means that the size is already known, we just need to make sure the cell looks right
        /// </summary>
        /// <param name="cellIndex"></param>
        /// <returns></returns>
        protected void ForceLayoutRebuild(int cellIndex)
        {
            if (_visibleItems.ContainsKey(cellIndex))
            {
                RectTransform[] rects = null;
                if (!_staticCells[cellIndex])
                    rects = _visibleItems[cellIndex].cell.CellsNeededForVisualUpdate;

                if (rects != null)
                {
                    foreach (var rect in rects)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(_visibleItems[cellIndex].transform);
            }
        }

        /// <summary>
        /// Reloads cell size and data
        /// </summary>
        /// <param name="cellIndex">cell index to reload</param>
        /// <param name="reloadCellData">when set true, it will fetch cell data from IDataSource</param>
        /// <param name="isReloadingAllData">Used to prevent calling CalculateNewMinMaxItemsAfterReloadCell in ReloadCellInternal each time when this function is called from ReloadData since we call
        /// CalculateNewMinMaxItemsAfterReloadCell at the end of ReloadData</param> 
        private void ReloadCellInternal (int cellIndex, bool reloadCellData = false, bool isReloadingAllData = false)
        {
            // No need to reload cell at index {cellIndex} as its currently not visible and everything will be automatically handled when it appears
            // it's ok to return here after setting the tag, as if a cell gets marked for reload with multiple tags, it only needs to reload once its visible
            // reloading the cell multiple times with different tags is needed when multiple changes happen to a cell over the course of some frames when its visible
            if (!_visibleItems.ContainsKey(cellIndex))
            {
                _itemsMarkedForReload.Add(cellIndex);
                return;
            }

            // item has been deleted, no need to reload
            if (cellIndex >= _itemsCount)
                return;
            
            var cell = _visibleItems[cellIndex];
            if (reloadCellData)
                _dataSource.SetCellData(cell.cell, cellIndex);

            var oldSize = _itemPositions[cellIndex].cellSize[_axis];
            CalculateNonAxisSizePosition(cell.transform, cellIndex);
            CalculateCellAxisSize(cell.transform, cellIndex);
            SetCellAxisPosition(cell.transform, cellIndex);
            
            // no need to call this while reloading data, since ReloadData will call it after reloading call cells
            // calling it while reload data will add unneeded redundancy
            if (!isReloadingAllData)
            {
                // no need to call CalculateNewMinMaxItemsAfterReloadCell if content moved since it will be handled in Update
                var contentMoved = RecalculateFollowingCells(cellIndex, oldSize);
                if (!contentMoved)
                    CalculateNewMinMaxItemsAfterReloadCell();
            }
        }

        /// <summary>
        /// Sets the positions of all cells of index + 1
        /// Persists content position to avoid sudden jumps if a cell size changes
        /// </summary>
        /// <param name="cellIndex">index of cell to start calculate following cells from</param>
        /// <param name="oldSize">old cell size used to offset content position with</param>
        /// <returns></returns>
        private bool RecalculateFollowingCells(int cellIndex, float oldSize)
        {
            // need to adjust all the cells position after cellIndex 
            var startingCellToAdjustPosition = cellIndex + 1;
            for (var i = startingCellToAdjustPosition; i <= _maxExtraVisibleItemInViewPort; i++)
                SetCellAxisPosition(_visibleItems[i].transform, i);

            if (_isAnimating)
                return true;
            
            var contentPosition = content.anchoredPosition;
            var contentMoved = false;
            var oldContentPosition = contentPosition[_axis];
            if (cellIndex < _minExtraVisibleItemInViewPort)
            {
                // this is a very special case as items reloaded at the top or right will have a different bottomRight position
                // and since we are here at the item, if we don't manually set the position of the content, it will seem as the content suddenly shifted and disorient the user
                contentPosition[_axis] = _itemPositions[cellIndex].absBottomRightPosition[_axis];
                
                // set the normalized position as well, because why not
                // (viewMin - (itemPosition - contentSize)) / (contentSize - viewSize)
                // var viewportRect = viewport.rect;
                // var contentRect = content.rect;
                // var viewPortBounds = new Bounds(viewportRect.center, viewportRect.size);
                // var newNormalizedPosition = (viewPortBounds.min[_axis] - (_itemPositions[cellIndex].bottomRightPosition[_axis] - contentRect.size[_axis])) / (contentRect.size[_axis] - viewportRect.size[_axis]);
                // SetNormalizedPosition(newNormalizedPosition, _axis);
            }
            else if (_minExtraVisibleItemInViewPort <= cellIndex && _minVisibleItemInViewPort > cellIndex)
            {
                contentPosition[_axis] -= (oldSize - _itemPositions[cellIndex].cellSize[_axis]) * (_reverseDirection ? -1 : 1);
            }
            
            var contentPositionDiff = Mathf.Abs(contentPosition[_axis] - oldContentPosition);
            if (contentPositionDiff > 0)
                contentMoved = true;

            if (contentMoved)
            {
                content.anchoredPosition = contentPosition;
                // this is important since the scroll rect will likely be dragging and it will cause a jump
                // this only took me 6 hours to figure out :(
                m_ContentStartPosition = contentPosition;
            }

            return contentMoved;
        }

        /// <summary>
        /// Checks if cells need to be hidden, shown, instantiated after a cell is reloaded and its size changes
        /// </summary>
        private void CalculateNewMinMaxItemsAfterReloadCell()
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
                    newMinVisibleItemInViewPortSet = true; // this boolean is needed as all items in the view port will satisfy the above condition and we only need the first one
                }

                if (itemPosition.absTopLeftPosition[_axis] <= _contentBottomRightCorner[_axis])
                {
                    newMaxVisibleItemInViewPort = item.Key;
                }
            }

            var newMinExtraVisibleItemInViewPort = Mathf.Max (0, newMinVisibleItemInViewPort - _extraItemsVisible);
            var newMaxExtraVisibleItemInViewPort = Mathf.Min (_itemsCount - 1, newMaxVisibleItemInViewPort + _extraItemsVisible);
            if (newMaxExtraVisibleItemInViewPort < _maxExtraVisibleItemInViewPort)
            {
                for (var i = newMaxExtraVisibleItemInViewPort + 1; i <= _maxExtraVisibleItemInViewPort; i++)
                    ShowHideCellsAtIndex(i, false, GridLayoutPage.After);
                
                _maxVisibleItemInViewPort = newMaxVisibleItemInViewPort;
                _maxExtraVisibleItemInViewPort = newMaxExtraVisibleItemInViewPort;
            }
            else
            {
                // here we initialize cells instead of using ShowCellAtIndex because we don't know much viewport space is left
                InitializeCells(_maxExtraVisibleItemInViewPort + 1);
            }
            
            if (newMinExtraVisibleItemInViewPort > _minExtraVisibleItemInViewPort)
            {
                for (var i = _minExtraVisibleItemInViewPort; i < newMinExtraVisibleItemInViewPort; i++)
                    ShowHideCellsAtIndex(i, false, GridLayoutPage.Before);
            }
            else
            {
                for (var i = _minExtraVisibleItemInViewPort - 1; i >= newMinExtraVisibleItemInViewPort; i--)
                    ShowHideCellsAtIndex(i, true, GridLayoutPage.Before);
            }

            _minVisibleItemInViewPort = newMinVisibleItemInViewPort;
            _minExtraVisibleItemInViewPort = newMinExtraVisibleItemInViewPort;
        }

        /// <summary>
        /// This function call is only needed when the cell is created, or when the resolution changes
        /// it sets the vertical size of the cell in a horizontal layout
        /// or the horizontal size of a cell in a vertical layout based on the settings of said layout
        /// It also sets the vertical position in horizontal layout or the horizontal position in a vertical layout based on the padding of said layout
        /// not needed in grid as items will have different positions in non axis position and the non axis size is the same in all of them
        /// </summary>
        /// <param name="rect">The rect of the cell that its size will be adjusted</param>
        /// <param name="cellIndex">The index of the cell that its size will be adjusted</param>
        protected virtual void CalculateNonAxisSizePosition(RectTransform rect, int cellIndex)
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
        /// Used to force set cell position, can be used if the cell position is manipulated externally and later would want to restore it.
        /// It doesn't need to set the position of an invisible item as it will get set automatically when its in view 
        /// </summary>
        /// <param name="cellIndex">cell index to set position to</param>
        public void SetCellPosition(int cellIndex)
        {
            if (!_visibleItems.TryGetValue(cellIndex, out var visibleItem))
                return;

            SetCellAxisPosition(visibleItem.transform, cellIndex);
        }

        /// <summary>
        /// This function sets the position of the item whether its new or retrieved from pool based on its index and the previous item index
        /// The current index position is the previous item position + previous item height
        /// or the previous item position - current item height
        /// </summary>
        /// <param name="rect">rect of the item which position will be set</param>
        /// <param name="newIndex">index of the item that needs its position set</param>
        protected virtual void SetCellAxisPosition(RectTransform rect, int newIndex)
        {
        }

        /// <summary>
        /// This function calculates the cell size if its unknown by forcing a Layout rebuild
        /// if it is known we just get the cell size
        /// then calculating the new content size based on the old cell size if it was set previously
        /// not needed in Gridlayout as the cell size will never change
        /// </summary>
        /// <param name="rect">rect of the cell which the size will be calculated for</param>
        /// <param name="index">cell index which the size will be calculated for</param>
        protected virtual void CalculateCellAxisSize(RectTransform rect, int index)
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
            // if content position is bigger than the the position of _maxVisibleItemInViewPort, this means we need to show items in bottom right
            // TODO: if writing code for reversed grids _maxExtraVisibleItemInViewPort needs to be _maxGridItemsInAxis - 1
            var reachedLimits = false;
            var atStart = _contentTopLeftCorner[_axis] <= 0;
            var atEnd = _contentBottomRightCorner[_axis] >= content.rect.size[_axis] && _maxExtraVisibleItemInViewPort == _itemsCount - 1;
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
            
            var topLeftMinClearance = 0.1f + topLeftPadding * (_minVisibleItemInViewPort == 0 ? 1 : 0) + _spacing[_axis] * (_minVisibleItemInViewPort == 0 ? 0 : 1);
            var bottomRightMinClearance = 0.1f + bottomLeftPadding * (_maxVisibleItemInViewPort == _itemsCount - 1 ? 1 : 0) + _spacing[_axis] * (_maxVisibleItemInViewPort == _itemsCount - 1 ? 0 : 1);
            
            if (_itemPositions[_minVisibleItemInViewPort].absTopLeftPosition[_axis] - _contentTopLeftCorner[_axis] > topLeftMinClearance && _minVisibleItemInViewPort != 0)
            {
                showBottomRight = false;
                _needsClearance = true;
            }
            else if (_itemPositions[_maxVisibleItemInViewPort].absBottomRightPosition[_axis] - _contentBottomRightCorner[_axis] < -bottomRightMinClearance && _maxVisibleItemInViewPort != _itemsCount - 1)
            {
                showBottomRight = true;
                _needsClearance = true;
            }
            _lastContentPosition = currentContentAnchoredPosition;

            if (reachedLimits && !_needsClearance)
                return;
            
            if (showBottomRight)
            {
                // item at top or left is not in viewport
                if (_minVisibleItemInViewPort < _itemsCount - 1 && _contentTopLeftCorner[_axis] >= _itemPositions[_minVisibleItemInViewPort].absBottomRightPosition[_axis])
                {
                    var itemToHide = _minVisibleItemInViewPort - _extraItemsVisible;
                    _minVisibleItemInViewPort++;
                    if (itemToHide > -1)
                    {
                        _minExtraVisibleItemInViewPort++;
                        ShowHideCellsAtIndex(itemToHide, false, GridLayoutPage.Before);
                    }
                }

                // item at bottom or right needs to appear
                if (_maxVisibleItemInViewPort < _itemsCount - 1 && _contentBottomRightCorner[_axis] > _itemPositions[_maxVisibleItemInViewPort].absBottomRightPosition[_axis] + _spacing[_axis])
                {
                    var newMaxItemToCheck = _maxVisibleItemInViewPort + 1;
                    var itemToShow = newMaxItemToCheck + _extraItemsVisible;
                    _maxVisibleItemInViewPort = newMaxItemToCheck;
                    if (itemToShow < _itemsCount)
                    {
                        _maxExtraVisibleItemInViewPort = itemToShow;
                        ShowHideCellsAtIndex(itemToShow, true, GridLayoutPage.After);
                    }
                }
            }
            else
            {
                // item at bottom or right not in viewport
                if (_maxVisibleItemInViewPort > 0 && _contentBottomRightCorner[_axis] <= _itemPositions[_maxVisibleItemInViewPort].absTopLeftPosition[_axis])
                {
                    var itemToHide = _maxVisibleItemInViewPort + _extraItemsVisible;
                    _maxVisibleItemInViewPort--;
                    if (itemToHide < _itemsCount)
                    {
                        _maxExtraVisibleItemInViewPort--;
                        ShowHideCellsAtIndex(itemToHide, false, GridLayoutPage.After);
                    }
                }

                // item at top or left needs to appear
                if (_minVisibleItemInViewPort > 0 && _contentTopLeftCorner[_axis] < _itemPositions[_minVisibleItemInViewPort].absTopLeftPosition[_axis] - _spacing[_axis])
                {
                    var newMinItemToCheck = _minVisibleItemInViewPort - 1;
                    var itemToShow = newMinItemToCheck - _extraItemsVisible;
                    _minVisibleItemInViewPort = newMinItemToCheck;
                    if (itemToShow > -1)
                    {
                        _minExtraVisibleItemInViewPort = itemToShow;
                        ShowHideCellsAtIndex(itemToShow, true, GridLayoutPage.Before);
                    }
                }
            }
        }
        
        /// <summary>
        /// Used to determine which cells will be shown or hidden in case its a grid layout since we need to show more than one cell depending on the grid configuration
        /// if it's not a grid layout, just call the Show, Hide functions
        /// </summary>
        /// <param name="newIndex">current index of item we need to show</param>
        /// <param name="show">show or hide current cell</param>
        /// <param name="gridLayoutPage">used to determine if we are showing/hiding a cell in after the most visible/hidden one or before the least visible/hidden one</param>
        internal virtual void ShowHideCellsAtIndex(int newIndex, bool show, GridLayoutPage gridLayoutPage)
        {
        }

        /// <summary>
        /// User has scrolled, and we need to show an item
        /// If there is a pooled item available, we get it and set its position, sibling index, and remove it from the pool
        /// If there is no pooled item available, we create a new one
        /// </summary>
        /// <param name="newIndex">current index of item we need to show</param>
        protected void ShowCellAtIndex(int newIndex)
        {
            // Get empty cell and adjust its position and size, else just create a new a cell
            var cellPrototypeName = _prototypeNames[newIndex];
            if (_pooledItems[cellPrototypeName].Count > 0)
            {
                var item = _pooledItems[cellPrototypeName][0];
                _pooledItems[cellPrototypeName].RemoveAt(0);

                if (_dataSource.IsSetVisibleUsingCanvasGroupAlpha)
                {
                    item.cell.CanvasGroup.alpha = 1;
                    item.cell.CanvasGroup.interactable = true;
                    item.cell.CanvasGroup.blocksRaycasts = true;
                }
                else
                    item.transform.gameObject.SetActive(true);
                SetVisibilityInHierarchy(item.transform, true);

                _visibleItems.Add(newIndex, item);
                item.cell.CellIndex = newIndex;
                
                SetCellAxisPosition(item.transform, newIndex);
                if (_ignoreSetCellDataIndices.Count <= 0 || _ignoreSetCellDataIndices.Count > 0 && !_ignoreSetCellDataIndices.Contains(newIndex))
                    _dataSource.SetCellData(item.cell, newIndex);
                CalculateCellAxisSize(item.transform, newIndex);
                
                if (_itemsMarkedForReload.Contains(newIndex))
                {
                    // item needs to be reloaded
                    ReloadCell(newIndex);
                    _itemsMarkedForReload.Remove(newIndex);
                }
                
                if (!_staticCells[newIndex])
                    item.transform.name = cellPrototypeName + " " + newIndex;
            }
            else
            {
                InitializeCell(newIndex);
            }

            if (_queuedScrollToCell != -1 && _queuedScrollToCell == newIndex)
            {
                _dataSource.ScrolledToCell(_visibleItems[newIndex].cell, newIndex);
                _queuedScrollToCell = -1;
            }

            SetIndices();
            
            if (newIndex == _itemsCount - 1)
                _dataSource.LastItemInScrollIsVisible();
        }

        /// <summary>
        /// Sets the indices of the items inside the content of the ScrollRect
        /// </summary>
        protected virtual void SetIndices()
        {
            foreach (var visibleItem in _visibleItems)
            {
                visibleItem.Value.transform.SetSiblingIndex(visibleItem.Key);
            }
        }
        
        /// <summary>
        /// Hide cell at cellIndex and add it to the pool of items that can be used based on its prefab type
        /// </summary>
        /// <param name="cellIndex">cellIndex which will be hidden</param>
        protected void HideCellAtIndex(int cellIndex)
        {
            if (_dataSource.IsSetVisibleUsingCanvasGroupAlpha)
            {
                _visibleItems[cellIndex].cell.CanvasGroup.alpha = 0;
                _visibleItems[cellIndex].cell.CanvasGroup.interactable = false;
                _visibleItems[cellIndex].cell.CanvasGroup.blocksRaycasts = false;
            }
            else
                _visibleItems[cellIndex].transform.gameObject.SetActive(false);
            
            SetVisibilityInHierarchy(_visibleItems[cellIndex].transform, false);
            _dataSource.CellHidden(_visibleItems[cellIndex].cell, cellIndex);
            _pooledItems[_prototypeNames[cellIndex]].Add(_visibleItems[cellIndex]);
            _visibleItems.Remove(cellIndex);
        }

        /// <summary>
        /// Updates content bounds for different uses
        /// </summary>
        private void GetContentBounds()
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
        public void ReloadData(bool reloadAllItems = false)
        {
            var oldItemsCount = _itemsCount;
            _itemsCount = _dataSource.ItemsCount;
            
            // removes extra items
            if (oldItemsCount > _itemsCount && _visibleItems.Count > _itemsCount)
            {
                var itemDiff = oldItemsCount - _itemsCount;
                for (var i = _itemsCount; i < _itemsCount + itemDiff; i++)
                {
                    if (_visibleItems.ContainsKey(i))
                        ShowHideCellsAtIndex(i, false, GridLayoutPage.Single);
                }

                _itemPositions.RemoveRange(_itemsCount, itemDiff);
                _prototypeNames.RemoveRange(_itemsCount, itemDiff);
                _staticCells.RemoveRange(_itemsCount, itemDiff);
                
                _maxVisibleItemInViewPort = Math.Max(0, _maxVisibleItemInViewPort - itemDiff);
                _maxExtraVisibleItemInViewPort = Mathf.Min(_itemsCount - 1, _maxVisibleItemInViewPort + _extraItemsVisible);
            }
            
            ResetVariables();
            SetContentAnchorsPivot();
            SetPrototypeNames();
            SetStaticCells();
            InitializeItemPositions();
            CalculateContentSize();
            CalculatePadding();

            if (reloadAllItems)
            {
                foreach (var item in _visibleItems)
                    PreReloadCell(item.Key, "", true, true);
            }
            CalculateNewMinMaxItemsAfterReloadCell();
            RefreshAfterReload();
        }

        protected virtual void RefreshAfterReload()
        {
        }

        /// <summary>
        /// A cell needs its game object type changed
        /// Useful for when adding items and there are static cells that need to be replaced
        /// </summary>
        /// <param name="cellIndices">cell index in which we need to change prototype for</param>
        private void ChangeCellPrototype(List<int> cellIndices)
        {
            var wasVisible = new List<int>();
            
            for (var i = 0; i < cellIndices.Count; i++)
            {
                if (_visibleItems.ContainsKey(cellIndices[i]))
                {
                    wasVisible.Add(cellIndices[i]);
                    ShowHideCellsAtIndex(cellIndices[i], false, GridLayoutPage.Single);
                }
            }
            
            for (var i = 0; i < cellIndices.Count; i++)
            {
                _prototypeNames[cellIndices[i]] = _dataSource.GetPrototypeCell(cellIndices[i]).name;
                _staticCells[cellIndices[i]] = _dataSource.IsCellStatic(cellIndices[i]);
                
                if (wasVisible.Contains(cellIndices[i]))
                    ShowHideCellsAtIndex(cellIndices[i], true, GridLayoutPage.Single);
            }
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
        /// Scroll to a certain cell index
        /// if the cell position is known, go to it,
        /// if not keep scrolling until its known
        /// </summary>
        /// <param name="cellIndex">cell index needed to scroll to</param>
        /// <param name="callEvent">call scroll to cell event, usually not needed when calling ScrollToCell from OnEndDrag if RSR is paged</param>
        /// <param name="instant">scroll instantly</param>
        /// <param name="maxSpeedMultiplier">a multiplier that is used at max speed for scrolling</param>
        /// <param name="offset">value to offset target scroll position with</param>
        public void ScrollToCell(int cellIndex, bool callEvent = true, bool instant = false, float maxSpeedMultiplier = 1, float offset = 0)
        {
            StopMovement();
            var direction = cellIndex > _currentPage ? 1 : -1;
            PreformPreScrollingActions(cellIndex, direction);
            
            var itemVisiblePositionKnown = _itemPositions[cellIndex].positionSet && _visibleItems.ContainsKey(cellIndex);
            if (itemVisiblePositionKnown && instant)
            {
                var currentContentPosition = content.anchoredPosition;
                currentContentPosition[_axis] = _itemPositions[cellIndex].absTopLeftPosition[_axis] * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1)) + offset;
                content.anchoredPosition = currentContentPosition;
                m_ContentStartPosition = currentContentPosition;
                if (callEvent)
                    _dataSource.ScrolledToCell(_visibleItems[cellIndex].cell, cellIndex);
                
                PreformPostScrollingActions(cellIndex, true);
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
                    var cellSizeAverage = 0f;
                    int i;
                    for (i = 0; i < _itemsCount; i++)
                    {
                        if (!_dataSource.IsCellSizeKnown && _itemPositions[i].sizeSet)
                            cellSizeAverage += _itemPositions[i].cellSize[_axis];
                        else if (_dataSource.IsCellSizeKnown)
                            cellSizeAverage += _dataSource.GetCellSize(i);
                        else
                            break;
                    }

                    cellSizeAverage /= i;
                    if (instant)
                    {
                        speedToUse = cellSizeAverage;
                    }
                    else
                    {
                        var scrollingDistance = Mathf.Max(1, Mathf.Abs(_minVisibleItemInViewPort - cellIndex));
                        var scrollingDistancePercentage = Mathf.Clamp01((float)scrollingDistance / Mathf.Min(10, _itemsCount));
                        var exponentialSpeed = (Mathf.Exp(scrollingDistancePercentage) - 1) * cellSizeAverage;
                        speedToUse = Mathf.Min(cellSizeAverage, exponentialSpeed);
                    }

                    if (speedToUse >= cellSizeAverage)
                        speedToUse *= maxSpeedMultiplier;
                }

                _isAnimating = true;
                var increment = speedToUse * direction * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
                StartCoroutine(StartScrolling(increment, direction, cellIndex, callEvent, offset));
            }
        }

        /// <summary>
        /// Coroutine that keeps scrolling until we find the designated scrollToTargetIndex which is set ShowCellAtIndex
        /// </summary>
        /// <param name="increment">increment in which we scroll by, based on direction, vertical or horizontal, instant scrolling or not</param>
        /// <param name="direction">direction of scroll, 1 for down or right, -1 for up or left</param>
        /// <param name="cellIndex">cell index which we want to scroll to</param>
        /// <param name="callEvent">call scroll to cell event, usually not needed when calling ScrollToCell from OnEndDrag if RSR is paged</param>
        /// <param name="offset">value to offset target scroll position with</param>
        private IEnumerator StartScrolling(float increment, int direction, int cellIndex, bool callEvent, float offset)
        {
            var reachedCell = false;        
            
            var contentTopLeftCorner = content.anchoredPosition;
            contentTopLeftCorner[_axis] += increment;

            if ( contentTopLeftCorner[ _axis ] >= content.sizeDelta[ _axis ] )
                contentTopLeftCorner[ _axis ] = content.sizeDelta[ _axis ] - _viewPortSize[_axis];
            
            var contentBottomRightCorner = contentTopLeftCorner;
            contentBottomRightCorner[_axis] += _viewPortSize[_axis] * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
            
            if (_itemPositions[cellIndex].positionSet)
            {
                var itemTopLeftCorner = _itemPositions[cellIndex].absTopLeftPosition[_axis] + offset * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
                if (direction == 1) 
                {
                    if (itemTopLeftCorner <= Mathf.Abs(contentTopLeftCorner[_axis]))
                    {
                        contentTopLeftCorner[_axis] = itemTopLeftCorner * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
                        reachedCell = true;
                    }

                    // reached bottom or right
                    else if (_maxExtraVisibleItemInViewPort == _itemsCount - 1 && Mathf.Abs(contentBottomRightCorner[_axis]) >= content.sizeDelta[_axis])
                    {
                        contentTopLeftCorner[_axis] = (content.rect.size[_axis] - _viewPortSize[_axis]) * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
                        reachedCell = true;
                    }
                }
                else 
                {
                    if (itemTopLeftCorner >= Mathf.Abs(contentTopLeftCorner[_axis]))
                    {
                        contentTopLeftCorner[_axis] = itemTopLeftCorner * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
                        reachedCell = true;
                    }

                    // reached top or left
                    else if (!_reverseDirection && (vertical && contentTopLeftCorner[_axis] <= 0 || !vertical && contentTopLeftCorner[_axis] >= 0)
                        || _reverseDirection && (vertical && contentTopLeftCorner[_axis] >= 0 || !vertical && contentTopLeftCorner[_axis] <= 0))
                    {
                        contentTopLeftCorner[_axis] = 0;
                        reachedCell = true;
                    }
                }
            }
            
            content.anchoredPosition = contentTopLeftCorner;
            m_ContentStartPosition = contentTopLeftCorner;

            if (reachedCell)
                StopMovement();

            yield return new WaitForEndOfFrame();
            if (!reachedCell)
                StartCoroutine(StartScrolling(increment, direction, cellIndex, callEvent, offset));
            else
            {
                if (callEvent)
                {
                    if (_visibleItems.TryGetValue( cellIndex, out var item ))
                        _dataSource.ScrolledToCell(item.cell, cellIndex);
                    else
                        _queuedScrollToCell = cellIndex;
                }

                PreformPostScrollingActions(cellIndex, false);
                _isAnimating = false;
                _ignoreSetCellDataIndices.Clear();
            }
        }

        protected virtual void PreformPreScrollingActions(int cellIndex, int direction)
        {
        }
        

        protected virtual void PreformPostScrollingActions(int cellIndex, bool instant)
        {
        }

        /// <summary>
        /// Organize the items in the hierarchy based on its visibility
        /// Its only used for organization
        /// </summary>
        /// <param name="item">item which will have its hierarchy properties changed</param>
        /// <param name="visible">visibility of cell index</param>
        private void SetVisibilityInHierarchy(RectTransform item, bool visible)
        {
#if UNITY_EDITOR
            var itemTransform = item.transform;
            itemTransform.hideFlags = visible ? HideFlags.None : HideFlags.HideInHierarchy;
#endif
        }
        
        public Item? GetCellAtIndex(int cellIndex)
        {
            if (_visibleItems == null || _visibleItems.Count <= 0)
                return null;
            if (_visibleItems.TryGetValue( cellIndex, out var item ))
                return item;
            return null;
        }
    }
}