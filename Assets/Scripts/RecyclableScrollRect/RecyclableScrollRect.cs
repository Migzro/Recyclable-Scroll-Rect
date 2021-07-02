using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclableSR
{
    public class RecyclableScrollRect : ScrollRect
    {
        // todo: test horizontal for the seventeenth time
        // todo: that function that can be only one call
        // todo: convert cell size to vector2
        // todo: static cell
        // todo: check reload data and add maybe a new method that just adds items
        // todo: ExecuteInEditMode?
        // todo: add a scrollTo method?
        // todo: Simulate content size (O.o)^(o.O)

        private VerticalLayoutGroup _verticalLayoutGroup;
        private HorizontalLayoutGroup _horizontalLayoutGroup;
        private ContentSizeFitter _contentSizeFitter;
        private LayoutElement _layoutElement;

        private IDataSource _dataSource;
        private Vector2 _viewPortSize;
        private List<float> _cellSizes;
        private List<string> _prototypeNames;
        private int _axis;
        private int _itemsCount;
        private int _minVisibleItemInViewPort;
        private int _maxVisibleItemInViewPort;
        private int _minExtraVisibleItemInViewPort;
        private int _maxExtraVisibleItemInViewPort;
        private int _extraItemsVisible;
        private bool _hasLayoutGroup;
        private bool _init;

        private List<ItemPosition> _itemPositions;
        private HashSet<int> _itemsMarkedForReload;
        private Dictionary<string, List<Item>> _pooledItems;
        private SortedDictionary<int, Item> _visibleItems;
        private Vector2 _lastContentPosition;
        private Vector2 _contentTopLeftCorner;
        private Vector2 _contentBottomRightCorner;
        private RectOffset _padding;
        private TextAnchor _alignment;
        private float _spacing;
        private bool _needsClearance;

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
                throw new ArgumentNullException(nameof(dataSource), "IDataSource is null");
            }

            if (_dataSource.PrototypeCells == null || _dataSource.PrototypeCells.Length <= 0)
            {
                throw new ArgumentNullException(nameof(_dataSource.PrototypeCells), "No prototype cell defined IDataSource");
            }

            // get the layouts and their settings if present
            if (vertical)
            {
                _verticalLayoutGroup = content.gameObject.GetComponent<VerticalLayoutGroup>();
                if (_verticalLayoutGroup != null)
                {
                    _hasLayoutGroup = true;
                    _padding = _verticalLayoutGroup.padding;
                    _spacing = _verticalLayoutGroup.spacing;
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
                    _spacing = _horizontalLayoutGroup.spacing;
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

            _itemsMarkedForReload = new HashSet<int>();
            _visibleItems = new SortedDictionary<int, Item>();
            _pooledItems = new Dictionary<string, List<Item>>();
            _viewPortSize = viewport.rect.size;
            _axis = vertical ? 1 : 0;
            _init = true;
            
            // create a new list for each prototype cell to hold the pooled cells
            var prototypeCells = _dataSource.PrototypeCells;
            for (var i = 0; i < prototypeCells.Length; i++)
            {
                _pooledItems.Add(prototypeCells[i].name, new List<Item>());
            }
            ReloadData();
        }
        
        /// <summary>
        /// Reload the data in case the content of the RecyclableScrollRect has changed
        /// </summary>
        public void ReloadData()
        {
            GetContentBounds();

            _itemsCount = _dataSource.ItemsCount;
            _itemPositions = new List<ItemPosition>();
            _extraItemsVisible = _dataSource.ExtraItemsVisible;
            _lastContentPosition = _contentTopLeftCorner;

            DisableContentLayouts();
            CalculateContentSize();
            SetPrototypeNames();
            InitializeItemPositions();
            InitializeCells();
        }

        /// <summary>
        /// Disable all layouts since everything is calculated manually
        /// </summary>
        private void DisableContentLayouts()
        {
            if (_hasLayoutGroup)
            {
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
            contentSizeDelta[_axis] = 0;

            _cellSizes = new List<float>();
            if (_dataSource.IsCellSizeKnown)
            {
                for (var i = 0; i < _itemsCount; i++)
                {
                    var cellSize = _dataSource.GetCellSize(i);
                    _cellSizes.Add(cellSize);
                    contentSizeDelta[_axis] += cellSize;
                }
            }
            else
            {
                for (var i = 0; i < _itemsCount; i++)
                {
                    _cellSizes.Add(-1);
                }
            }

            if (_hasLayoutGroup)
            {
                if (vertical && _verticalLayoutGroup != null)
                {
                    contentSizeDelta.y += _spacing * (_itemsCount - 1);
                    contentSizeDelta.y += _padding.top + _padding.bottom;
                }
                else if (!vertical && _horizontalLayoutGroup != null)
                {
                    contentSizeDelta.x += _spacing * (_itemsCount - 1);
                    contentSizeDelta.x += _padding.right + _padding.left;
                }

                if (vertical)
                    _layoutElement.preferredHeight = contentSizeDelta.y;
                else
                    _layoutElement.preferredWidth = contentSizeDelta.x;
            }

            content.sizeDelta = contentSizeDelta;
        }

        /// <summary>
        /// Set prefab names for when needed to retrieve from pool
        /// </summary>
        private void SetPrototypeNames()
        {
            // set an array of prototype names to be used when getting the correct prefab for the cell index it exists in its respective pool
            _prototypeNames = new List<string>();
            for (var i = 0; i < _itemsCount; i++)
                _prototypeNames.Add(_dataSource.GetPrototypeCell(i).name);
        }

        /// <summary>
        /// Initialize item positions as zero to avoid using .Contains when initializing the positions
        /// </summary>
        private void InitializeItemPositions()
        {
            for (var i = 0; i < _itemsCount; i++)
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
            var contentHasSpace = startIndex == 0 || _itemPositions[startIndex - 1].bottomRightPosition[_axis] + _spacing <= _contentBottomRightCorner[_axis];
            var extraItemsInitialized = contentHasSpace ? 0 : _maxExtraVisibleItemInViewPort - _maxVisibleItemInViewPort;
            var i = startIndex;
            while (contentHasSpace || extraItemsInitialized < _extraItemsVisible)
            {
                ShowCellAtIndex(i, i - 1);
                if (!contentHasSpace)
                    extraItemsInitialized++;
                else
                    _maxVisibleItemInViewPort = i;

                contentHasSpace = _itemPositions[i].bottomRightPosition[_axis] + _spacing <= _contentBottomRightCorner[_axis];
                i++;
            }
            _maxExtraVisibleItemInViewPort = i - 1;
        }

        /// <summary>
        /// Initialize the cells
        /// Its only called when there are no pooled items available and the RecyclableScrollRect needs to show a cell
        /// </summary>
        /// <param name="index"></param>
        /// <returns>cell index that needs to be initialized</returns>
        private void InitializeCell(int index)
        {
            var itemPrototypeCell = _dataSource.GetPrototypeCell(index);
            var itemGo = Instantiate(itemPrototypeCell, content, false);
            itemGo.name = index.ToString();

            var cell = itemGo.GetComponent<ICell>();
            var rect = itemGo.GetComponent<RectTransform>();
            
            var item = new Item(cell, rect);
            _visibleItems.Add(index, item);
            _dataSource.SetCellData(cell, index);

            SetCellSize(rect);
            SetCellPosition(rect, index);
        }
        
        /// <summary>
        /// Reloads cell size and adjusts all the visible cells that follow that cell
        /// Creates new cells if the cell size has shrunk and there is room in the view port
        /// it also hides items that left the viewport
        /// </summary>
        /// <param name="cellIndex">cell index to reload</param>
        /// <param name="reloadData">when set true, it will fetch data from IDataSource</param>
        public void ReloadCell(int cellIndex, bool reloadData = false)
        {
            // No need to reload cell at index {cellIndex} as its currently not visible and everything will be automatically handled when it appears
            if (!_visibleItems.ContainsKey(cellIndex))
            {
                _itemsMarkedForReload.Add(cellIndex);
                return;
            }
            
            var cell = _visibleItems[cellIndex];
            if (reloadData)
                _dataSource.SetCellData(cell.cell, cellIndex);

            var oldSize = _cellSizes[cellIndex];
            CalculateCellSize(cell.transform, cellIndex);
            _itemPositions[cellIndex].SetSize(cell.transform.rect.size);
            
            // need to adjust all the cells position after cellIndex 
            var startingCellToAdjustPosition = cellIndex + 1;
            for (var i = startingCellToAdjustPosition; i <= _maxExtraVisibleItemInViewPort; i++)
                SetCellPosition(_visibleItems[i].transform, i);

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
                contentPosition[_axis] -= oldSize - _cellSizes[cellIndex];
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
                    HideCellAtIndex(i);
                
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
                    HideCellAtIndex(i);
            }
            else
            {
                for (var i = _minExtraVisibleItemInViewPort - 1; i >= newMinExtraVisibleItemInViewPort; i--)
                    ShowCellAtIndex(i, i + 1);
            }

            _minVisibleItemInViewPort = newMinVisibleItemInViewPort;
            _minExtraVisibleItemInViewPort = newMinExtraVisibleItemInViewPort;
        }

        /// <summary>
        /// This function call is only needed once when the cell is created as it only sets the vertical size of the cell in a horizontal layout
        /// or the horizontal size of a cell in a vertical layout based on the settings of said layout
        /// It also sets the vertical position in horizontal layout or the horizontal position in a vertical layout based on the padding of said layout since these wont usually
        /// change during the runtime unless
        /// </summary>
        /// <param name="rect">The rect of the cell that its size will be adjusted</param>
        private void SetCellSize(RectTransform rect)
        {
            // all items are pivoted & anchored to top left
            var anchorVector = new Vector2(0, 1);
            rect.anchorMin = anchorVector;
            rect.anchorMax = anchorVector;
            rect.pivot = anchorVector;

            var forceSize = false;
            // set size
            if (_hasLayoutGroup)
            {
                // expand item width if its in a vertical layout group and the conditions are satisfied
                if (vertical && _verticalLayoutGroup.childControlWidth && _verticalLayoutGroup.childForceExpandWidth)
                {
                    var itemSize = rect.sizeDelta;
                    itemSize.x = content.rect.width - _verticalLayoutGroup.padding.right - _verticalLayoutGroup.padding.left;
                    rect.sizeDelta = itemSize;
                    forceSize = true;
                }

                // expand item height if its in a horizontal layout group and the conditions are satisfied
                else if (!vertical && _horizontalLayoutGroup.childControlHeight && _horizontalLayoutGroup.childControlHeight)
                {
                    var itemSize = rect.sizeDelta;
                    itemSize.y = content.rect.height - _horizontalLayoutGroup.padding.top - _horizontalLayoutGroup.padding.bottom;
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
                    if (_alignment == TextAnchor.LowerCenter || _alignment == TextAnchor.MiddleCenter || _alignment == TextAnchor.UpperCenter)
                    {
                        itemPosition.x = (_padding.left + (contentSize.x - rectSize.x) - _padding.right) / 2f;
                    }
                    else if (_alignment == TextAnchor.LowerRight || _alignment == TextAnchor.MiddleRight || _alignment == TextAnchor.UpperRight)
                    {
                        itemPosition.x = contentSize.x - rectSize.x - _padding.right;
                    }
                    else
                    {
                        itemPosition.x = _padding.left;
                    }
                }
                else
                {
                    if (_alignment == TextAnchor.MiddleLeft || _alignment == TextAnchor.MiddleCenter || _alignment == TextAnchor.MiddleRight)
                    {
                        itemPosition.y = -(_padding.top + (contentSize.y - rectSize.y) - _padding.bottom) / 2f;
                    }
                    else if (_alignment == TextAnchor.LowerLeft || _alignment == TextAnchor.LowerCenter || _alignment == TextAnchor.LowerRight)
                    {
                        itemPosition.y = -(contentSize.y - rectSize.y - _padding.bottom);
                    }
                    else
                    {
                        itemPosition.y = -_padding.top;
                    }
                }
                rect.anchoredPosition = itemPosition;
            }
        }

        /// <summary>
        /// This function sets the position of the item whether its new or retrieved from pool based on its index and the previous item index
        /// The current index position is the previous item position + previous item height
        /// or the previous item position - current item height
        /// </summary>
        /// <param name="rect">rect of the item which position will be set</param>
        /// <param name="newIndex">index of the item that needs its position set</param>
        private void SetCellPosition(RectTransform rect, int newIndex)
        {
            if ((int) _cellSizes[newIndex] == -1)
                CalculateCellSize(rect, newIndex);

            // figure out where the prev cell position was
            var newItemPosition = rect.anchoredPosition;
            if (newIndex == 0)
            {
                if (vertical)
                    newItemPosition.y = _padding.top;
                else
                    newItemPosition.x = _padding.left;
            }
            else
            {
                var verticalSign = vertical ? -1 : 1;
                newItemPosition[_axis] = verticalSign * _itemPositions[newIndex - 1].bottomRightPosition[_axis] + verticalSign * _spacing;
            }

            rect.anchoredPosition = newItemPosition;
            var cellSize = rect.rect.size;
            cellSize[_axis] = _cellSizes[newIndex];
            _itemPositions[newIndex].SetPositionAndSize(newItemPosition, cellSize);
        }

        /// <summary>
        /// This function calculates the cell size if its unknown by forcing a Layout rebuild
        /// if it is known we just get the cell size
        /// then calculating the new content size based on the old cell size if it was set previously
        /// </summary>
        /// <param name="rect">rect of the cell which the size will be calculated for</param>
        /// <param name="index">cell index which the size will be calculated for</param>
        private void CalculateCellSize(RectTransform rect, int index)
        {
            float newCellSize;
            if (!_dataSource.IsCellSizeKnown)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                newCellSize = rect.rect.size[_axis];
            }
            else
            {
                newCellSize = _dataSource.GetCellSize(index);
            }

            // get difference in cell size if its size has changed
            var oldCellSize = 0f;
            if ((int)_cellSizes[index] != -1)
                oldCellSize = _cellSizes[index];
            _cellSizes[index] = newCellSize;
            
            var contentSize = content.sizeDelta;
            contentSize[_axis] += newCellSize - oldCellSize;
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
            if (Mathf.Approximately(content.anchoredPosition[_axis], _lastContentPosition[_axis]) && !_needsClearance)
                return;
            _lastContentPosition = content.anchoredPosition;

            // figure out which items that need to be rendered, bottom right or top left
            // generally if the content position is smaller than the position of _minVisibleItemInViewPort, this means we need to show items in tops left
            // if content position is bigger than the the position of _maxVisibleItemInViewPort, this means we need to show items in bottom right
            // _needsClearance is needed because sometimes the scrolling happens so fast that the items are not showing and normalizedPosition & _lastScrollPosition would be the same stopping the update loop
            GetContentBounds();
            var showBottomRight = _contentTopLeftCorner[_axis] > _lastContentPosition[_axis];
            _needsClearance = false;
            if (_itemPositions[_minVisibleItemInViewPort].topLeftPosition[_axis] - _contentTopLeftCorner[_axis] > 0.1f)
            {
                showBottomRight = false;
                _needsClearance = true;
            }
            else if (_itemPositions[_maxVisibleItemInViewPort].bottomRightPosition[_axis] - _contentBottomRightCorner[_axis] < -0.1f)
            {
                showBottomRight = true;
                _needsClearance = true;
            }
            
            if (showBottomRight)
            {
                // item at top or left is not in viewport
                if (_minVisibleItemInViewPort < _itemsCount - 1 && _contentTopLeftCorner[_axis] > _itemPositions[_minVisibleItemInViewPort].bottomRightPosition[_axis])
                {
                    var itemToHide = _minVisibleItemInViewPort - _extraItemsVisible;
                    if (itemToHide > -1)
                    {
                        _minExtraVisibleItemInViewPort++;
                        HideCellAtIndex(itemToHide);
                    }

                    _minVisibleItemInViewPort++;
                }

                // item at bottom or right needs to appear
                if (_maxVisibleItemInViewPort < _itemsCount - 1 && _contentBottomRightCorner[_axis] > _itemPositions[_maxVisibleItemInViewPort].bottomRightPosition[_axis])
                {
                    var newMaxItemToCheck = _maxVisibleItemInViewPort + 1;
                    var itemToShow = newMaxItemToCheck + _extraItemsVisible;
                    if (itemToShow < _itemsCount)
                    {
                        _maxExtraVisibleItemInViewPort = itemToShow;
                        ShowCellAtIndex(itemToShow, itemToShow - 1);
                    }

                    _maxVisibleItemInViewPort = newMaxItemToCheck;
                }
            }
            else
            {
                // item at bottom or right not in viewport
                if (_maxVisibleItemInViewPort > 0 && _contentBottomRightCorner[_axis] < _itemPositions[_maxVisibleItemInViewPort].topLeftPosition[_axis])
                {
                    var itemToHide = _maxVisibleItemInViewPort + _extraItemsVisible;
                    if (itemToHide < _itemsCount)
                    {
                        _maxExtraVisibleItemInViewPort--;
                        HideCellAtIndex(itemToHide);
                    }

                    _maxVisibleItemInViewPort--;
                }

                // item at top or left needs to appear
                if (_minVisibleItemInViewPort > 0 && _contentTopLeftCorner[_axis] < _itemPositions[_minVisibleItemInViewPort].topLeftPosition[_axis])
                {
                    var newMinItemToCheck = _minVisibleItemInViewPort - 1;
                    var itemToShow = newMinItemToCheck - _extraItemsVisible;
                    if (itemToShow > -1)
                    {
                        _minExtraVisibleItemInViewPort = itemToShow;
                        ShowCellAtIndex(itemToShow, itemToShow + 1);
                    }

                    _minVisibleItemInViewPort = newMinItemToCheck;
                }
            }
        }

        /// <summary>
        /// User has scrolled and we need to show an item
        /// If there is a pooled item available, we get it and set its position, sibling index, and remove it from the pool
        /// If there is no pooled item available, we create a new one
        /// </summary>
        /// <param name="newIndex">current index of item we need to show</param>
        /// <param name="prevIndex">index of item before the one we need to show</param>
        private void ShowCellAtIndex(int newIndex, int prevIndex)
        {
            // Get empty cell and adjust its position and size, else just create a new a cell
            var cellPrototypeName = _prototypeNames[newIndex];
            if (_pooledItems[cellPrototypeName].Count > 0)
            {
                var item = _pooledItems[cellPrototypeName][0];
                _pooledItems[cellPrototypeName].RemoveAt(0);

                if (newIndex > prevIndex)
                    item.transform.SetAsLastSibling();
                else
                    item.transform.SetAsFirstSibling();
                
                item.transform.gameObject.SetActive(true);
                SetVisibilityInHierarchy(item.transform, newIndex, true);

                _visibleItems.Add(newIndex, item);
                _dataSource.SetCellData(item.cell, newIndex);

                SetCellPosition(item.transform, newIndex);
                
                if (_itemsMarkedForReload.Contains(newIndex))
                {
                    // item needs to be reloaded
                    ReloadCell(newIndex);
                    _itemsMarkedForReload.Remove(newIndex);
                }
            }
            else
            {
                InitializeCell(newIndex);
            }
        }

        /// <summary>
        /// Hide cell at cellIndex and add it to the pool of items that can be used based on its prefab type
        /// </summary>
        /// <param name="cellIndex">cellIndex which will be hidden</param>
        private void HideCellAtIndex(int cellIndex)
        {
            _visibleItems[cellIndex].transform.gameObject.SetActive(false);
            SetVisibilityInHierarchy(_visibleItems[cellIndex].transform, cellIndex, false);
            _pooledItems[_prototypeNames[cellIndex]].Add(_visibleItems[cellIndex]);
            _visibleItems.Remove(cellIndex);
        }

        /// <summary>
        /// Updates content bounds for different uses
        /// </summary>
        private void GetContentBounds()
        {
            _contentTopLeftCorner = content.anchoredPosition * (vertical ? 1f : -1f);
            _contentBottomRightCorner = new Vector2(_contentTopLeftCorner.x + _viewPortSize.x, _contentTopLeftCorner.y + _viewPortSize.y);
        }

        /// <summary>
        /// Organize the items in the hierarchy based on its visibility
        /// Its only used for organization
        /// </summary>
        /// <param name="item">item which will have its hierarchy properties changed</param>
        /// <param name="cellIndex">cell index</param>
        /// <param name="visible">visibility of cell index</param>
        private void SetVisibilityInHierarchy(RectTransform item, int cellIndex, bool visible)
        {
#if UNITY_EDITOR
            var itemTransform = item.transform;
            itemTransform.name = cellIndex.ToString();
            itemTransform.hideFlags = visible ? HideFlags.None : HideFlags.HideInHierarchy;
#endif
        }
    }
}