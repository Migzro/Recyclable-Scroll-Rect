using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RecyclableSR
{
    public class RecyclableScrollRect : ScrollRect
    {
        // TODO: different start axes for grid layout
        // TODO: FixedColumnCount with Vertical Grids & FixedRowCount with Horizontal Grids (remaining _maxExtraVisibleItemInViewPort needs to be / _maxGridsItemsInAxis
        [SerializeField] private float _swipeThreshold = 200;
        [SerializeField] private bool _paged;
        [SerializeField] private bool _reverseDirection;
        
        private VerticalLayoutGroup _verticalLayoutGroup;
        private HorizontalLayoutGroup _horizontalLayoutGroup;
        private GridLayoutGroup _gridLayout;
        private ContentSizeFitter _contentSizeFitter;
        private LayoutElement _layoutElement;

        private IDataSource _dataSource;

        private List<bool> _staticCells;
        private List<string> _prototypeNames;
        private List<ItemPosition> _itemPositions;
        
        private int _axis;
        private int _itemsCount;
        private int _extraItemsVisible;
        private int _minVisibleItemInViewPort;
        private int _maxVisibleItemInViewPort;
        private int _minExtraVisibleItemInViewPort;
        private int _maxExtraVisibleItemInViewPort;
        private int _gridConstraint;
        private int _maxGridItemsInAxis;
        private int _currentPage;
        private int _queuedScrollToCell;
        private bool _init;
        private bool _isAnimating;
        private bool _needsClearance;
        private bool _hasLayoutGroup;
        private bool _pullToRefresh;
        private bool _isGridLayout;
        private bool _isDragging;

        private HashSet<int> _itemsMarkedForReload;
        private HashSet<int> _ignoreSetCellDataIndices;
        private Dictionary<string, List<Item>> _pooledItems;
        private Dictionary<int, HashSet<string>> _reloadTags;
        private SortedDictionary<int, Item> _visibleItems;
        
        private Vector2 _viewPortSize;
        private Vector2 _lastContentPosition;
        private Vector2 _contentTopLeftCorner;
        private Vector2 _contentBottomRightCorner;
        private Vector2 _spacing;
        private Vector2 _dragStartingPosition;
        private RectOffset _padding;
        private TextAnchor _alignment;
        private MovementType _movementType;
        private MovementType _initialMovementType;
        private bool _isReloadingData;

        public bool IsInitialized => _init;

        /// <summary>
        /// Initialize the scroll rect with the data source that contains all the details required to build the RecyclableScrollRect
        /// </summary>
        /// <param name="dataSource">The data source which is usually the class that implements IDataSource</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Initialize(IDataSource dataSource)
        {
            _dataSource = dataSource;

            if (_dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource), "RSR, IDataSource is null");
            }

            if (_dataSource.PrototypeCells == null || _dataSource.PrototypeCells.Length <= 0)
            {
                throw new ArgumentNullException(nameof(_dataSource.PrototypeCells), "RSR, No prototype cell defined IDataSource");
            }

            // get the layouts and their settings if present
            _gridLayout = content.gameObject.GetComponent<GridLayoutGroup>();
            if (_gridLayout != null)
            {
                _hasLayoutGroup = true;
                _isGridLayout = true;
                _padding = _gridLayout.padding;
                _spacing = _gridLayout.spacing;
                _alignment = _gridLayout.childAlignment;

                if (_gridLayout.constraint == GridLayoutGroup.Constraint.Flexible)
                {
                    throw new Exception("RSR, Flexible Constraints not currently supported");
                    // if (vertical)
                    // {
                    //     Debug.LogWarning("RSR, Flexible constraints is currently not supported, defaulting to 1 Fixed Column Count");
                    //     _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    //     _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
                    //     _gridConstraint = 1;
                    //     
                    // }
                    // else
                    // {
                    //     Debug.LogWarning("RSR, Flexible constraints is currently not supported, defaulting to 1 Fixed Row Count");
                    //     _gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                    //     _gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
                    //     _gridConstraint = 1;
                    // }
                }
                else if (_gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount && _gridLayout.startAxis == GridLayoutGroup.Axis.Vertical)
                {
                    throw new Exception("RSR, FixedColumnCount Constraints with Vertical Axis not currently supported");
                    // Debug.LogWarning("RSR, FixedColumnCount constraints with Vertical start axis is currently not supported, defaulting to Horizontal start axis");
                    // _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
                }
                else if (_gridLayout.constraint == GridLayoutGroup.Constraint.FixedRowCount && _gridLayout.startAxis == GridLayoutGroup.Axis.Horizontal)
                {
                    throw new Exception("RSR, FixedRowCount Constraints with Horizontal Axis not currently supported");
                    // Debug.LogWarning("RSR, FixedRowCount constraints with Horizontal start axis is currently not supported, defaulting to Vertical start axis");
                    // _gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
                }
                else
                    _gridConstraint = _gridLayout.constraintCount;
                
                if (_gridLayout.startCorner != GridLayoutGroup.Corner.UpperLeft)
                {
                    Debug.LogWarning("RSR, Upper Left Corner is the only mode currently supported, defaulting to Upper Left");
                }
            }
            else if (vertical)
            {
                _verticalLayoutGroup = content.gameObject.GetComponent<VerticalLayoutGroup>();
                if (_verticalLayoutGroup != null)
                {
                    _hasLayoutGroup = true;
                    _padding = _verticalLayoutGroup.padding;
                    _spacing = new Vector2(0, _verticalLayoutGroup.spacing);
                    _alignment = _verticalLayoutGroup.childAlignment;
                }
            }
            else
            {
                _horizontalLayoutGroup = content.gameObject.GetComponent<HorizontalLayoutGroup>();
                if (_horizontalLayoutGroup != null)
                {
                    _hasLayoutGroup = true;
                    _padding = _horizontalLayoutGroup.padding;
                    _spacing = new Vector2(_horizontalLayoutGroup.spacing, 0);
                    _alignment = _horizontalLayoutGroup.childAlignment;
                }
            }

            // add a LayoutElement if not present to set the content size in case another element is controlling it 
            if (_hasLayoutGroup)
            {
                _contentSizeFitter = content.gameObject.GetComponent<ContentSizeFitter>();
                _layoutElement = content.gameObject.GetComponent<LayoutElement>();
                if (_layoutElement == null)
                    _layoutElement = content.gameObject.AddComponent<LayoutElement>();
            }

            _currentPage = 0;
            _viewPortSize = viewport.rect.size;
            _axis = vertical ? 1 : 0;
            _initialMovementType = movementType;
            
            DisableContentLayouts();
            ResetData();
        }
        
        /// <summary>
        /// Reload the data in case the content of the RecyclableScrollRect has changed
        /// </summary>
        public void ResetData()
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

            _minVisibleItemInViewPort = 0;
            _minExtraVisibleItemInViewPort = 0;
            _maxVisibleItemInViewPort = 0;
            _maxExtraVisibleItemInViewPort = 0;

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
            _movementType = _initialMovementType;

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
            InitializeCells();
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
        }

        /// <summary>
        /// Disable all layouts since everything is calculated manually
        /// </summary>
        private void DisableContentLayouts()
        {
            if (_hasLayoutGroup)
            {
                if (_gridLayout != null)
                    _gridLayout.enabled = false;
                
                if (_horizontalLayoutGroup != null)
                    _horizontalLayoutGroup.enabled = false;

                if (_verticalLayoutGroup != null)
                    _verticalLayoutGroup.enabled = false;

                _contentSizeFitter.enabled = false;
            }
        }

        /// <summary>
        /// Calculate the content size in their respective direction based on the scrolling direction
        /// If the cell size is know we simply add all the cell sizes, spacing and padding
        /// If not we set the cell size as -1 as it will be calculated once the cell comes into view
        /// </summary>
        private void CalculateContentSize()
        {
            var contentSizeDelta = viewport.sizeDelta;
            _maxGridItemsInAxis = 0;
            contentSizeDelta[_axis] = 0;

            if (_isGridLayout)
            {
                // we considers all cell sizes the same in grid
                _maxGridItemsInAxis = Mathf.CeilToInt(_itemsCount / (float)_gridConstraint);
                contentSizeDelta[_axis] = _maxGridItemsInAxis * _gridLayout.cellSize[_axis];

                for (var i = 0; i < _itemsCount; i++)
                    _itemPositions[i].SetSize(_gridLayout.cellSize);
            }
            else
            {
                for (var i = 0; i < _itemsCount; i++)
                {
                    if (!_dataSource.IsCellSizeKnown)
                        contentSizeDelta[_axis] += _itemPositions[i].cellSize[_axis];
                    else
                    {
                        var cellSize = _itemPositions[i].cellSize;
                        cellSize[_axis] = _dataSource.GetCellSize(i);
                        _itemPositions[i].SetSize(cellSize);
                        contentSizeDelta[_axis] += _dataSource.GetCellSize(i);
                    }
                }
            }

            if (_hasLayoutGroup)
            {
                if (_isGridLayout)
                    contentSizeDelta[_axis] += _spacing[_axis] * (_maxGridItemsInAxis - 1);
                else
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
            }

            content.sizeDelta = contentSizeDelta;
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
                    if (_visibleItems.ContainsKey(i))
                        cellRect = _visibleItems[i].transform;
                    else
                        cellRect = (RectTransform)_dataSource.GetPrototypeCell(i).transform;
                    SetVisibilityInHierarchy(cellRect, false);

                    if (_dataSource.IsSetVisibleUsingCanvasGroupAlpha)
                    {
                        var cell = cellRect.GetComponent<ICell>() ?? cellRect.gameObject.AddComponent<BaseCell>();
                        cell.CanvasGroup.alpha = 0;
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
            // set an array of prototype names to be used when getting the correct prefab for the cell index it exists in its respective pool
            for (var i = 0; i < _itemsCount; i++)
            {
                if (i < _prototypeNames.Count)
                {
                    var newPrototype = _dataSource.GetPrototypeCell(i).name;
                    var oldPrototype = _prototypeNames[i];
                    if (newPrototype != oldPrototype)
                        ChangeCellPrototype(i);
                }
                else
                {
                    _prototypeNames.Add(_dataSource.GetPrototypeCell(i).name);
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
        /// Initialize all cells needed until the view port is filled
        /// extra visible items is an additional amount of cells that can be shown to prevent showing an empty view port if the scrolling is too fast and the update function didnt show all the items
        /// that need to be shown
        /// </summary>
        /// <param name="startIndex">the starting cell index on which we want initialized</param>
        private void InitializeCells(int startIndex = 0)
        {
            GetContentBounds();
            var contentHasSpace = startIndex == 0 || _itemPositions[startIndex - 1].bottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
            var extraItemsInitialized = contentHasSpace ? 0 : _maxExtraVisibleItemInViewPort - _maxVisibleItemInViewPort;
            var i = startIndex;
            var gridHasSpace = _isGridLayout && startIndex % _gridConstraint != 0 && i < _itemsCount;
            while ((contentHasSpace || gridHasSpace || extraItemsInitialized < _extraItemsVisible) && i < _itemsCount)
            {
                ShowHideCellsAtIndex(i, true, GridLayoutPage.After);
                if (!contentHasSpace)
                    extraItemsInitialized++;
                else
                    _maxVisibleItemInViewPort = i;

                contentHasSpace = _itemPositions[i].bottomRightPosition[_axis] + _spacing[_axis] <= _contentBottomRightCorner[_axis];
                gridHasSpace = _isGridLayout && startIndex % _gridConstraint != 0 && i < _itemsCount;
                i++;
            }
            _maxExtraVisibleItemInViewPort = i - 1;
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
                cell.RecyclableScrollRect = this;
                cell.CellIndex = index;
                itemGo.name = itemPrototypeCell.name + " " + index;
            }
            else
            {
                itemGo = itemPrototypeCell;
                cell = itemGo.GetComponent<ICell>() ?? itemGo.AddComponent<BaseCell>();
                cell.RecyclableScrollRect = this;
                cell.CellIndex = index;
                
                SetVisibilityInHierarchy((RectTransform)itemGo.transform, true);
                itemGo.SetActive(true);
                if (_dataSource.IsSetVisibleUsingCanvasGroupAlpha)
                    cell.CanvasGroup.alpha = 1;
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
        /// Reloads cell size and adjusts all the visible cells that follow that cell
        /// Creates new cells if the cell size has shrunk and there is room in the view port
        /// it also hides items that left the viewport
        /// </summary>
        /// <param name="cellIndex">cell index to reload</param>
        /// <param name="reloadTag">used to reload cell based on a tag, so if the reload is called more than once with the same tag, we can ignore it</param>
        /// <param name="reloadData">when set true, it will fetch data from IDataSource</param>
        public void ReloadCell(int cellIndex, string reloadTag = "", bool reloadData = false)
        {
            // add the reloadTag if it doesn't exist for the cellIndex
            // if reloading data, clear all the reloadTags for the cellIndex
            if (reloadData && _reloadTags.ContainsKey(cellIndex))
                _reloadTags[cellIndex].Clear();

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
            ReloadCellInternal(cellIndex, reloadData);
        }

        /// <summary>
        /// Forces a layout to rebuild in case it was called with a tag that already exists,
        /// This means that the size is already known, we just need to make sure the cell looks right
        /// </summary>
        /// <param name="cellIndex"></param>
        /// <returns></returns>
        private void ForceLayoutRebuild(int cellIndex)
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

        private void ReloadCellInternal (int cellIndex, bool reloadData = false)
        {
            // No need to reload cell at index {cellIndex} as its currently not visible and everything will be automatically handled when it appears
            // its ok to return here after setting the tag, as if a cell gets marked for reload with multiple tags, it only needs to reload once its visible
            // reloading the cell multiple times with different tags is needed when multiple changes happen to a cell over the course of some frames when its visible
            if (!_visibleItems.ContainsKey(cellIndex))
            {
                _itemsMarkedForReload.Add(cellIndex);
                return;
            }
            
            var cell = _visibleItems[cellIndex];
            if (reloadData)
                _dataSource.SetCellData(cell.cell, cellIndex);

            // No need to calculate anything for grid since its cell size doesn't change
            if (_isGridLayout)
                return;

            var oldSize = _itemPositions[cellIndex].cellSize[_axis];
            CalculateCellAxisSize(cell.transform, cellIndex);

            // need to adjust all the cells position after cellIndex 
            var startingCellToAdjustPosition = cellIndex + 1;
            for (var i = startingCellToAdjustPosition; i <= _maxExtraVisibleItemInViewPort; i++)
                SetCellAxisPosition(_visibleItems[i].transform, i);

            if (_isAnimating)
                return;
            
            var contentPosition = content.anchoredPosition;
            var contentMoved = false;
            if (cellIndex < _minExtraVisibleItemInViewPort)
            {
                // this is a very special case as items reloaded at the top or right will have a different bottomRight position
                // and since we are here at the item, if we don't manually set the position of the content, it will seem as the content suddenly shifted and disorient the user
                contentPosition[_axis] = _itemPositions[cellIndex].bottomRightPosition[_axis];
                
                // set the normalized position as well, because why not
                // (viewMin - (itemPosition - contentSize)) / (contentSize - viewSize)
                // var viewportRect = viewport.rect;
                // var contentRect = content.rect;
                // var viewPortBounds = new Bounds(viewportRect.center, viewportRect.size);
                // var newNormalizedPosition = (viewPortBounds.min[_axis] - (_itemPositions[cellIndex].bottomRightPosition[_axis] - contentRect.size[_axis])) / (contentRect.size[_axis] - viewportRect.size[_axis]);
                // SetNormalizedPosition(newNormalizedPosition, _axis);
                
                contentMoved = true;
            }
            else if (_minExtraVisibleItemInViewPort <= cellIndex && _minVisibleItemInViewPort > cellIndex)
            {
                contentPosition[_axis] -= oldSize - _itemPositions[cellIndex].cellSize[_axis];
                contentMoved = true;
            }

            if (contentMoved)
            {
                content.anchoredPosition = contentPosition;
                // this is important since the scroll rect will likely be dragging and it will cause a jump
                // this only took me 6 hours to figure out :(
                m_ContentStartPosition = contentPosition;
                return;
            }
            
            // figure out the new _minVisibleItemInViewPort && _maxVisibleItemInViewPort
            GetContentBounds();
            var newMinVisibleItemInViewPortSet = false;
            var newMinVisibleItemInViewPort = 0;
            var newMaxVisibleItemInViewPort = 0;
            foreach (var item in _visibleItems)
            {
                var itemPosition = _itemPositions[item.Key];
                if (itemPosition.bottomRightPosition[_axis] >= _contentTopLeftCorner[_axis] && !newMinVisibleItemInViewPortSet)
                {
                    newMinVisibleItemInViewPort = item.Key;
                    newMinVisibleItemInViewPortSet = true; // this boolean is needed as all items in the view port will satisfy the above condition and we only need the first one
                }

                if (itemPosition.topLeftPosition[_axis] <= _contentBottomRightCorner[_axis])
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
                if (!_isReloadingData)
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
        /// This function call is only needed once when the cell is created as it only sets the vertical size of the cell in a horizontal layout
        /// or the horizontal size of a cell in a vertical layout based on the settings of said layout
        /// It also sets the vertical position in horizontal layout or the horizontal position in a vertical layout based on the padding of said layout since these wont usually
        /// change during the runtime
        /// not needed in grid as items will have different positions in non axis position and the non axis size is the same in all of them
        /// </summary>
        /// <param name="rect">The rect of the cell that its size will be adjusted</param>
        /// <param name="cellIndex">The index of the cell that its size will be adjusted</param>
        private void CalculateNonAxisSizePosition(RectTransform rect, int cellIndex)
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
                anchorVector = new Vector2(0, 1);
            rect.anchorMin = anchorVector;
            rect.anchorMax = anchorVector;
            rect.pivot = anchorVector;
            
            if (_isGridLayout)
                return;

            var forceSize = false;
            // set size
            if (_hasLayoutGroup)
            {
                // expand item width if its in a vertical layout group and the conditions are satisfied
                if (vertical && _verticalLayoutGroup.childControlWidth && _verticalLayoutGroup.childForceExpandWidth)
                {
                    var itemSize = rect.sizeDelta;
                    itemSize.x = content.rect.width;
                    if (!_dataSource.IgnoreContentPadding(cellIndex))
                        itemSize.x -= _padding.right + _padding.left;

                    rect.sizeDelta = itemSize;
                    forceSize = true;
                }

                // expand item height if its in a horizontal layout group and the conditions are satisfied
                else if (!vertical && _horizontalLayoutGroup.childControlHeight && _horizontalLayoutGroup.childControlHeight)
                {
                    var itemSize = rect.sizeDelta;
                    itemSize.y = content.rect.height;
                    if (!_dataSource.IgnoreContentPadding(cellIndex))
                        itemSize.y -= _padding.top + _padding.bottom;
                    rect.sizeDelta = itemSize;
                    forceSize = true;
                }
            }

            // get content size without padding
            var contentSize = content.rect.size;
            var contentSizeWithoutPadding = contentSize;
            contentSizeWithoutPadding.x -= _padding.right - _padding.left;
            contentSizeWithoutPadding.y -= _padding.top - _padding.bottom;

            // set position of cell based on layout alignment
            // we check for multiple conditions together since the content is made to fit the items, so they only move in one axis in each different scroll direction
            var rectSize = rect.rect.size;
            var itemSizeSmallerThanContent = rectSize[_axis] < contentSizeWithoutPadding[_axis];
            if (_hasLayoutGroup && (itemSizeSmallerThanContent || forceSize))
            {
                var itemPosition = rect.anchoredPosition;
                if (vertical)
                {
                    var rightPadding = _reverseDirection ? _padding.left : _padding.right;
                    var leftPadding = _reverseDirection ? _padding.right : _padding.left;
                    if (_dataSource.IgnoreContentPadding(cellIndex))
                    {
                        rightPadding = 0;
                        leftPadding = 0;
                    }

                    if (_alignment == TextAnchor.LowerCenter || _alignment == TextAnchor.MiddleCenter || _alignment == TextAnchor.UpperCenter)
                    {
                        itemPosition.x = (leftPadding + (contentSize.x - rectSize.x) - rightPadding) / 2f;
                    }
                    else if (_alignment == TextAnchor.LowerRight || _alignment == TextAnchor.MiddleRight || _alignment == TextAnchor.UpperRight)
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
                    var topPadding = _reverseDirection ? _padding.bottom : _padding.top;
                    var bottomPadding = _reverseDirection ? _padding.top : _padding.bottom;
                    if (_dataSource.IgnoreContentPadding(cellIndex))
                    {
                        topPadding = 0;
                        bottomPadding = 0;
                    }
                    
                    if (_alignment == TextAnchor.MiddleLeft || _alignment == TextAnchor.MiddleCenter || _alignment == TextAnchor.MiddleRight)
                    {
                        itemPosition.y = -(topPadding + (contentSize.y - rectSize.y) - bottomPadding) / 2f;
                    }
                    else if (_alignment == TextAnchor.LowerLeft || _alignment == TextAnchor.LowerCenter || _alignment == TextAnchor.LowerRight)
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
        
        /// <summary>
        /// Used to force set cell position, can be used if the cell position is manipulated externally and later would want to restore it
        /// no need to set the position of an invisible item as it will get set automatically when its in view 
        /// </summary>
        /// <param name="cellIndex">cell index to set position to</param>
        public void SetCellPosition(int cellIndex)
        {
            if (!_visibleItems.ContainsKey(cellIndex))
                return;
            
            var cell = _visibleItems[cellIndex];
            SetCellAxisPosition(cell.transform, cellIndex);
        }

        /// <summary>
        /// This function sets the position of the item whether its new or retrieved from pool based on its index and the previous item index
        /// The current index position is the previous item position + previous item height
        /// or the previous item position - current item height
        /// </summary>
        /// <param name="rect">rect of the item which position will be set</param>
        /// <param name="newIndex">index of the item that needs its position set</param>
        private void SetCellAxisPosition(RectTransform rect, int newIndex)
        {
            var newItemPosition = rect.anchoredPosition;
            if (_isGridLayout)
            {
                int xIndexInGrid;
                int yIndexInGrid;
                
                if (_gridLayout.constraint == GridLayoutGroup.Constraint.FixedRowCount)
                {
                    if (_gridLayout.startAxis == GridLayoutGroup.Axis.Vertical)
                    {
                        xIndexInGrid = Mathf.FloorToInt(newIndex / (float) _gridConstraint);
                        yIndexInGrid = newIndex % _gridConstraint;
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
                    if (_gridLayout.startAxis == GridLayoutGroup.Axis.Vertical)
                    {
                        // TODO: Reversed Grid Code
                        xIndexInGrid = Mathf.FloorToInt(newIndex / (float) _maxGridItemsInAxis);
                        yIndexInGrid = newIndex % _maxGridItemsInAxis;
                    }
                    else
                    {
                        xIndexInGrid = newIndex % _gridConstraint;
                        yIndexInGrid = Mathf.FloorToInt(newIndex / (float) _gridConstraint);
                    }
                }

                if (_reverseDirection)
                {
                    if (_gridLayout.constraint == GridLayoutGroup.Constraint.FixedRowCount && _gridLayout.startAxis == GridLayoutGroup.Axis.Vertical)
                    {
                        newItemPosition.x = -_padding.right - xIndexInGrid * _itemPositions[newIndex].cellSize[0] - _spacing[0] * xIndexInGrid;
                        newItemPosition.y = -_padding.bottom - yIndexInGrid * _itemPositions[newIndex].cellSize[1] - _spacing[1] * yIndexInGrid;
                    }
                    else if (_gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount && _gridLayout.startAxis == GridLayoutGroup.Axis.Horizontal)
                    {
                        newItemPosition.x = _padding.right + xIndexInGrid * _itemPositions[newIndex].cellSize[0] + _spacing[0] * xIndexInGrid;
                        newItemPosition.y = _padding.bottom + yIndexInGrid * _itemPositions[newIndex].cellSize[1] + _spacing[1] * yIndexInGrid;
                    }
                }
                else
                {
                    newItemPosition.x = _padding.left + xIndexInGrid * _itemPositions[newIndex].cellSize[0] + _spacing[0] * xIndexInGrid;
                    newItemPosition.y = -_padding.top - yIndexInGrid * _itemPositions[newIndex].cellSize[1] - _spacing[1] * yIndexInGrid;
                }
            }
            else
            {
                // figure out where the prev cell position was
                if (newIndex == 0)
                {
                    if (vertical)
                    {
                        if (_reverseDirection)
                            newItemPosition.y = _padding.bottom;
                        else
                            newItemPosition.y = -_padding.top;
                    }
                    else
                    {
                        if (_reverseDirection)
                            newItemPosition.x = -_padding.right;
                        else
                            newItemPosition.x = _padding.left;
                    }
                }
                else
                {
                    var verticalSign = (vertical ? -1 : 1) * (_reverseDirection ? -1 : 1);
                    newItemPosition[_axis] = verticalSign * _itemPositions[newIndex - 1].bottomRightPosition[_axis] + verticalSign * _spacing[_axis];
                }
            }

            rect.anchoredPosition = newItemPosition;
            _itemPositions[newIndex].SetPosition(newItemPosition);
        }

        /// <summary>
        /// This function calculates the cell size if its unknown by forcing a Layout rebuild
        /// if it is known we just get the cell size
        /// then calculating the new content size based on the old cell size if it was set previously
        /// not needed in Gridlayout as the cell size will never change
        /// </summary>
        /// <param name="rect">rect of the cell which the size will be calculated for</param>
        /// <param name="index">cell index which the size will be calculated for</param>
        private void CalculateCellAxisSize(RectTransform rect, int index)
        {
            if (_isGridLayout)
            {
                rect.sizeDelta = _gridLayout.cellSize;
                return;
            }

            var newCellSize = _itemPositions[index].cellSize;
            var oldCellSize = newCellSize[_axis];

            if (!_dataSource.IsCellSizeKnown)
            {
                ForceLayoutRebuild(index);
                newCellSize[_axis] = rect.rect.size[_axis];
            }
            else
            {
                newCellSize[_axis] = _dataSource.GetCellSize(index);
            }

            // get difference in cell size if its size has changed
            _itemPositions[index].SetSize(newCellSize);
            
            var contentSize = content.sizeDelta;
            contentSize[_axis] += newCellSize[_axis] - oldCellSize;
            if (_hasLayoutGroup)
            {
                if (vertical)
                    _layoutElement.preferredHeight = contentSize.y;
                else
                    _layoutElement.preferredWidth = contentSize.x;
            }
            content.sizeDelta = contentSize;
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
            
            if (!_pullToRefresh && Mathf.RoundToInt(_contentTopLeftCorner[_axis]) <= -150)
            {
                _pullToRefresh = true;
                _dataSource.PullToRefresh();
            }
            else if (_pullToRefresh && Mathf.RoundToInt(_contentTopLeftCorner[_axis]) >= -150)
            {
                _pullToRefresh = false;
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
                
                if (atEnd)
                    _dataSource.ReachedScrollEnd();
            }
            else
                movementType = MovementType.Unrestricted;

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
            
            if (_itemPositions[_minVisibleItemInViewPort].topLeftPosition[_axis] - _contentTopLeftCorner[_axis] > topLeftMinClearance && _minVisibleItemInViewPort != 0)
            {
                showBottomRight = false;
                _needsClearance = true;
            }
            else if (_itemPositions[_maxVisibleItemInViewPort].bottomRightPosition[_axis] - _contentBottomRightCorner[_axis] < -bottomRightMinClearance && _maxVisibleItemInViewPort != _itemsCount - 1)
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
                if (_minVisibleItemInViewPort < _itemsCount - 1 && _contentTopLeftCorner[_axis] >= _itemPositions[_minVisibleItemInViewPort].bottomRightPosition[_axis])
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
                if (_maxVisibleItemInViewPort < _itemsCount - 1 && _contentBottomRightCorner[_axis] > _itemPositions[_maxVisibleItemInViewPort].bottomRightPosition[_axis] + _spacing[_axis])
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
                if (_maxVisibleItemInViewPort > 0 && _contentBottomRightCorner[_axis] <= _itemPositions[_maxVisibleItemInViewPort].topLeftPosition[_axis])
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
                if (_minVisibleItemInViewPort > 0 && _contentTopLeftCorner[_axis] < _itemPositions[_minVisibleItemInViewPort].topLeftPosition[_axis] - _spacing[_axis])
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
        /// if its not a grid layout, just call the Show, Hide functions
        /// </summary>
        /// <param name="newIndex">current index of item we need to show</param>
        /// <param name="show">show or hide current cell</param>
        /// <param name="gridLayoutPage">used to determine if we are showing/hiding a cell in after the most visible/hidden one or before the least visible/hidden one</param>
        private void ShowHideCellsAtIndex(int newIndex, bool show, GridLayoutPage gridLayoutPage)
        {
            if (!_isGridLayout)
            {
                if (show)
                    ShowCellAtIndex(newIndex);
                else
                    HideCellAtIndex(newIndex);
            }
            else
            {
                var indices = new List<int>();
                if (_gridLayout.constraint == GridLayoutGroup.Constraint.FixedRowCount && _gridLayout.startAxis == GridLayoutGroup.Axis.Vertical
                    || _gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount && _gridLayout.startAxis == GridLayoutGroup.Axis.Horizontal)
                {
                    if (gridLayoutPage == GridLayoutPage.After)
                    {
                        // equation to get the highest multiple of newIndex where _gridConstraint is the multiple
                        var maxItemToShow = _gridConstraint * Mathf.FloorToInt((float) newIndex / _gridConstraint) + _gridConstraint;
                        for (var i = newIndex; i < maxItemToShow; i++)
                        {
                            if (i < _itemsCount)
                                indices.Add(i);
                        }
                    }
                    else if (gridLayoutPage == GridLayoutPage.Before)
                    {
                        // equation to get the lowest multiple of newIndex where _gridConstraint is the multiple
                        var minItemToShow = _gridConstraint * Mathf.FloorToInt((float) newIndex / _gridConstraint);  
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
                        for (var i = 0; i < _gridConstraint; i++)
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

        /// <summary>
        /// User has scrolled and we need to show an item
        /// If there is a pooled item available, we get it and set its position, sibling index, and remove it from the pool
        /// If there is no pooled item available, we create a new one
        /// </summary>
        /// <param name="newIndex">current index of item we need to show</param>
        private void ShowCellAtIndex(int newIndex)
        {
            // Get empty cell and adjust its position and size, else just create a new a cell
            var cellPrototypeName = _prototypeNames[newIndex];
            if (_pooledItems[cellPrototypeName].Count > 0)
            {
                var item = _pooledItems[cellPrototypeName][0];
                _pooledItems[cellPrototypeName].RemoveAt(0);

                if (_dataSource.IsSetVisibleUsingCanvasGroupAlpha)
                    item.cell.CanvasGroup.alpha = 1;
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

            SetChildrenIndices();
        }

        /// <summary>
        /// Sets the indices of the items inside the content of the ScrollRect
        /// </summary>
        private void SetChildrenIndices()
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
        private void HideCellAtIndex(int cellIndex)
        {
            if (_dataSource.IsSetVisibleUsingCanvasGroupAlpha)
                _visibleItems[cellIndex].cell.CanvasGroup.alpha = 0;
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
            _isReloadingData = true;
            var oldItemsCount = _itemsCount;
            _itemsCount = _dataSource.ItemsCount;
            
            // removes extra items
            if (oldItemsCount > _itemsCount && _visibleItems.Count > _itemsCount)
            {
                var itemDiff = oldItemsCount - _itemsCount;
                for (var i = _itemsCount; i < _itemsCount + itemDiff; i++)
                {
                    if (_visibleItems.ContainsKey(i))
                        ShowHideCellsAtIndex(i, false, GridLayoutPage.After);
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
            InitializeCells(_maxExtraVisibleItemInViewPort + 1);

            if (reloadAllItems)
            {
                var visibleItemKeys = _visibleItems.Keys.ToList();
                foreach (var index in visibleItemKeys)
                    ReloadCell(index, "", true);
            }
            _isReloadingData = false;
        }

        /// <summary>
        /// A cell needs its game object type changed
        /// Useful for when adding items and there are static cells that need to be replaced
        /// </summary>
        /// <param name="cellIndex">cell index in which we need to change prototype for</param>
        private void ChangeCellPrototype(int cellIndex)
        {
            if (_visibleItems.ContainsKey(cellIndex))
            {
                ShowHideCellsAtIndex(cellIndex, false, GridLayoutPage.Single);
                _prototypeNames[cellIndex] = _dataSource.GetPrototypeCell(cellIndex).name;
                _staticCells[cellIndex] = _dataSource.IsCellStatic(cellIndex);
                ShowHideCellsAtIndex(cellIndex, true, GridLayoutPage.Single);
            }
            else
            {
                _prototypeNames[cellIndex] = _dataSource.GetPrototypeCell(cellIndex).name;
                _staticCells[cellIndex] = _dataSource.IsCellStatic(cellIndex);
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
        public void ScrollToTopRight()
        {
            // either or, both methods work fine
            StopMovement();
            StartCoroutine(ScrollToTargetNormalisedPosition((vertical ? 1 : 0) * (_reverseDirection ? 0 : 1)));
            // ScrollToCell(0);
        }

        /// <summary>
        /// A loop for animating to a desired NormalisedPosition
        /// </summary>
        /// <param name="targetNormalisedPos">required normalisedPosition</param>
        /// <returns></returns>
        private IEnumerator ScrollToTargetNormalisedPosition(float targetNormalisedPos)
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
            var itemVisiblePositionKnown = _itemPositions[cellIndex].positionSet && _visibleItems.ContainsKey(cellIndex);
            if (itemVisiblePositionKnown && instant)
            {
                var currentContentPosition = content.anchoredPosition;
                currentContentPosition[_axis] = _itemPositions[cellIndex].topLeftPosition[_axis] * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1)) + offset;
                content.anchoredPosition = currentContentPosition;
                m_ContentStartPosition = currentContentPosition;
                if (callEvent)
                    _dataSource.ScrolledToCell(_visibleItems[cellIndex].cell, cellIndex);
                _currentPage = cellIndex;
                _isAnimating = false;
            }
            else
            {
                var direction = cellIndex > _minVisibleItemInViewPort ? 1 : -1;

                float speedToUse;
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
                
                // create a list that will stop ScrollTo method from calling SetCellData on items that will only be visible in the one frame while scrolling, this assumes
                // that the paging cell is taking up the entire width or height
                if (_paged)
                {
                    var endingIndex = cellIndex;
                    _ignoreSetCellDataIndices.Clear();
                    if (direction > 0)
                    {
                        endingIndex -= _extraItemsVisible;
                        endingIndex = Mathf.Clamp(endingIndex, 0, _itemsCount - 1);
                        for (var j = _currentPage; j < endingIndex; j++)
                        {
                            if (!_visibleItems.ContainsKey(j) && _itemPositions[j].positionSet)
                            {
                                _ignoreSetCellDataIndices.Add(j);
                            }
                        }
                    }
                    else
                    {
                        endingIndex += _extraItemsVisible;
                        endingIndex = Mathf.Clamp(endingIndex, 0, _itemsCount - 1);
                        for (var j = _currentPage - 1; j > endingIndex; j--)
                        {
                            if (!_visibleItems.ContainsKey(j) && _itemPositions[j].positionSet)
                            {
                                _ignoreSetCellDataIndices.Add(j);
                            }
                        }
                    }
                }

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
            var contentBottomRightCorner = contentTopLeftCorner + _viewPortSize * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));

            if (_itemPositions[cellIndex].positionSet)
            {
                var itemTopLeftCorner = _itemPositions[cellIndex].topLeftPosition[_axis] + offset * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
                if (direction == 1) 
                {
                    if (itemTopLeftCorner <= Mathf.Abs(contentTopLeftCorner[_axis]))
                    {
                        contentTopLeftCorner[_axis] = itemTopLeftCorner * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
                        reachedCell = true;
                    }

                    // reached bottom or right
                    if (_maxExtraVisibleItemInViewPort == _itemsCount - 1 && Mathf.Abs(contentBottomRightCorner[_axis]) >= content.sizeDelta[_axis])
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
                    if (!_reverseDirection && (vertical && contentTopLeftCorner[_axis] <= 0 || !vertical && contentTopLeftCorner[_axis] >= 0)
                        || _reverseDirection && (vertical && contentTopLeftCorner[_axis] >= 0 || !vertical && contentTopLeftCorner[_axis] <= 0))
                    {
                        contentTopLeftCorner[_axis] = 0;
                        reachedCell = true;
                    }
                }
            }
            
            content.anchoredPosition = contentTopLeftCorner;
            m_ContentStartPosition = contentTopLeftCorner;

            yield return new WaitForEndOfFrame();
            if (!reachedCell)
                StartCoroutine(StartScrolling(increment, direction, cellIndex, callEvent, offset));
            else
            {
                if (callEvent)
                {
                    if (_visibleItems.ContainsKey(cellIndex))
                        _dataSource.ScrolledToCell(_visibleItems[cellIndex].cell, cellIndex);
                    else
                        _queuedScrollToCell = cellIndex;
                } 
                _currentPage = cellIndex;
                _isAnimating = false;
                _ignoreSetCellDataIndices.Clear();
            }
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
        
        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);

            if (!_paged)
                return;
            
            _isDragging = true;
            _dragStartingPosition = content.anchoredPosition * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            if (!_isDragging || !_paged)
                return;
            
            _isDragging = false;
            var currentContentPosition = content.anchoredPosition * ((vertical ? 1 : -1) * (_reverseDirection ? -1 : 1));
            var distance = Vector3.Distance(_dragStartingPosition, currentContentPosition);
            var isNextPage = currentContentPosition[_axis] > _dragStartingPosition[_axis];
            var newPage = _currentPage;
            if (distance > _swipeThreshold)
            {
                if (isNextPage && _currentPage < _itemsCount - 1)
                    newPage++;
                else if (!isNextPage && _currentPage > 0)
                    newPage--;
            }
            
            _dataSource.ScrolledToCell(_visibleItems[newPage].cell, newPage);
            ScrollToCell(newPage, false);
        }

        public Item? GetCellAtIndex(int cellIndex)
        {
            if (_visibleItems == null || _visibleItems.Count <= 0)
                return null;
            if (_visibleItems.ContainsKey(cellIndex))
                return _visibleItems[ cellIndex ];
            return null;
        }
    }
}