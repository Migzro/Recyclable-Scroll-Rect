using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclableSR
{
    public class RecyclableScrollRect : ScrollRect
    {
        // todo: convert cell size to vector2
        // todo: static cell
        // todo: check reload data and add maybe a new method that just adds items
        // todo: ExecuteInEditMode?
        // todo: add a scrollto method?
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
        private int _minimumItemsInViewPort;
        private int _itemsCount;
        private int _minVisibleItemInViewPort;
        private int _maxVisibleItemInViewPort;
        private int _extraItemsVisible;
        private bool _hasLayoutGroup;
        private bool _init;

        private List<ItemPosition> _itemPositions;
        private Dictionary<int, Item> _visibleItems;
        private Dictionary<string, List<Item>> _pooledItems;
        private Vector2 _lastScrollPosition;
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

            _visibleItems = new Dictionary<int, Item>();
            _pooledItems = new Dictionary<string, List<Item>>();
            _viewPortSize = viewport.rect.size;
            _init = true;
            _lastScrollPosition = normalizedPosition;
            _axis = vertical ? 1 : 0;
            
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
            _itemsCount = _dataSource.ItemsCount;
            _itemPositions = new List<ItemPosition>();
            _extraItemsVisible = _dataSource.ExtraItemsVisible;

            DisableContentLayouts();
            CalculateContentSize();
            CalculateMinimumItemsInViewPort();
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
        /// If the cell size is know we calculate the amount of items needed to fill the view port
        /// </summary>
        private void CalculateMinimumItemsInViewPort()
        {
            if (!_dataSource.IsCellSizeKnown)
                return;
            
            _minimumItemsInViewPort = 0;
            var rect = viewport.rect;
            var remainingViewPortSize = rect.size[_axis];

            for (var i = 0; i < _itemsCount; i++)
            {
                var spacing = _spacing;
                if (i == _itemsCount - 1)
                    spacing = 0;

                remainingViewPortSize -= _cellSizes[i] + spacing;
                _minimumItemsInViewPort++;

                if (remainingViewPortSize <= 0)
                    break;
            }

            _minVisibleItemInViewPort = 0;
            _maxVisibleItemInViewPort = _minimumItemsInViewPort - 1;
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
        /// Initialize all cells needed to fill the view port
        /// If cell size is know we initiate the amount of cells calculated CalculateMinimumItemsInViewPort
        /// if not we keep initializing cells until the view port is filled
        /// extra visible items is an additional amount of cells that can be shown to prevent showing an empty view port if the scrolling is too fast and the update function didnt show all the items
        /// that need to be shown
        /// </summary>
        /// <param name="startingIndex">used to initialize cells in case a cell size has changed and the viewport is empty</param>
        private void InitializeCells(int startingIndex = 0)
        {
            if (_dataSource.IsCellSizeKnown)
            {
                var itemsToShow = Mathf.Min(_itemsCount, _minimumItemsInViewPort + _extraItemsVisible);
                for (var i = startingIndex; i < itemsToShow; i++)
                {
                    InitializeCell(i);
                }
            }
            else
            {
                var contentHasSpace = true;
                var extraItemsInitialized = 0;
                var i = startingIndex;
                // _maxVisibleItemInViewPort is 0 if starting index is 0
                // or startingIndex - 1 which will be the current _maxVisibleItemInViewPort as its called from ReloadCell
                _maxVisibleItemInViewPort = startingIndex - 1;
                while (contentHasSpace || extraItemsInitialized < _extraItemsVisible)
                {
                    InitializeCell(i);
                    if (!contentHasSpace)
                        extraItemsInitialized++;
                    else
                        _maxVisibleItemInViewPort++;

                    contentHasSpace = _itemPositions[i].topLeftPosition[_axis] + _cellSizes[i] + _spacing <= viewport.rect.size[_axis];
                    i++;
                }
            }
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
            SetCellPosition(rect, index, index - 1);
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
        /// <param name="prevIndex">index of the item that new position will be based upon</param>
        private void SetCellPosition(RectTransform rect, int newIndex, int prevIndex)
        {
            if ((int)_cellSizes[newIndex] == -1)
                CalculateUnknownCellSize(rect, newIndex);
            
            // figure out where the prev cell position was
            var newItemPosition = rect.anchoredPosition;
            if (newIndex == 0)
            {
                if (vertical)
                    newItemPosition.y = _padding.top;
                else
                    newItemPosition.x = _padding.left;
            }

            if (prevIndex > -1 && prevIndex < _itemsCount - 1)
            {
                var isAfter = newIndex > prevIndex;
                
                newItemPosition = _visibleItems[prevIndex].transform.anchoredPosition;

                var sign = isAfter ? -1 : 1;
                var cellSizeToUse = isAfter ? _cellSizes[prevIndex] : _cellSizes[newIndex];
                if (vertical)
                    newItemPosition[_axis] += (cellSizeToUse * sign) + (_spacing * sign);
                else
                    newItemPosition[_axis] -= (cellSizeToUse * sign) + (_spacing * sign);
            }

            rect.anchoredPosition = newItemPosition;
            _itemPositions[newIndex].SetPositionAndSize(newItemPosition.Abs(), rect.rect.size);
        }

        /// <summary>
        /// Reloads cell size and adjusts all the visible cells that follow that cell
        /// Creates new cells if the cell size has shrunk and there is room in the view port
        /// it also hides items that left the viewport
        /// </summary>
        /// <param name="cellIndex">cell index to reload</param>
        /// <param name="reloadData">when set true, it will fetch data from IDataSource</param>
        public void ReloadCell (int cellIndex, bool reloadData = false)
        {
            // No need to reload cell at index {cellIndex} as its currently not visible and everything will be automatically handled when it appears
            if (!_visibleItems.ContainsKey(cellIndex))
                return;
            
            var cell = _visibleItems[cellIndex];
            if (reloadData)
                _dataSource.SetCellData(cell.cell, cellIndex);

            var oldSize = _cellSizes[cellIndex];
            if (_dataSource.IsCellSizeKnown)
                _cellSizes[cellIndex] = _dataSource.GetCellSize(cellIndex);
            else
                CalculateUnknownCellSize(cell.transform, cellIndex);
            _itemPositions[cellIndex].SetSize(cell.transform.rect.size);
            var shrank = _cellSizes[cellIndex] < oldSize;

            // need to adjust all the following cells position
            var startingCellToAdjustPosition = cellIndex + 1;
            var lastCellToAdjustPosition = _maxVisibleItemInViewPort + _extraItemsVisible;
            
            var currentMaxVisibleItemInViewPort = cellIndex;
            var contentTopLeftCorner = content.anchoredPosition * (vertical ? 1f : -1f);
            var contentBottomRightCorner = new Vector2(contentTopLeftCorner.x + _viewPortSize.x, contentTopLeftCorner.y + _viewPortSize.y);
            for (var i = startingCellToAdjustPosition; i <= lastCellToAdjustPosition; i++)
            {
                SetCellPosition(_visibleItems[i].transform, i, i - 1);

                // need to check if more item have appeared in Viewport which will require recalculation of _maxVisibleItemInViewPort
                if (_itemPositions[i].topLeftPosition[_axis] < contentBottomRightCorner[_axis])
                    currentMaxVisibleItemInViewPort = i;
            }
            
            if (shrank)
            {
                // we initialize cells in case the new newMaxVisibleItemInViewPort is bigger than the _maxVisibleItemInViewPort which means there are new items that need to initialized
                // we dont set _maxVisibleItemInViewPort here as its already handled in the following function calls
                CalculateMinimumItemsInViewPort();
                InitializeCells(currentMaxVisibleItemInViewPort + 1);
            }
            else
            {
                // hide the items that left the viewport if there any
                // while generally leaving items that are out of viewport won't hurt, we do it for performance
                for (var i = currentMaxVisibleItemInViewPort + 1; i <= _maxVisibleItemInViewPort + _extraItemsVisible; i++)
                    HideCellAtIndex(i);
                
                _maxVisibleItemInViewPort = currentMaxVisibleItemInViewPort;
            }
        }

        /// <summary>
        /// This function calculates the cell size if its unknown by forcing a Layout rebuild
        /// then calculating the new content size based on the old cell size if it was set previously
        /// </summary>
        /// <param name="rect">rect of the cell which the size will be calculated for</param>
        /// <param name="index">cell index which the size will be calculated for</param>
        private void CalculateUnknownCellSize(RectTransform rect, int index)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            var contentSize = content.sizeDelta;

            // get difference in cell size if its size has changed
            var newCellSize = rect.rect.size[_axis];
            var oldCellSize = 0f;
            if ((int)_cellSizes[index] != -1)
                oldCellSize = _cellSizes[index];
            
            _cellSizes[index] = newCellSize;
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
            if (normalizedPosition == _lastScrollPosition && !_needsClearance)
                return;
            
            // get the top corner and bottom corner positions of the scroll content
            var contentTopLeftCorner = content.anchoredPosition * (vertical ? 1f : -1f);
            var contentBottomRightCorner = new Vector2(contentTopLeftCorner.x + _viewPortSize.x, contentTopLeftCorner.y + _viewPortSize.y);

            // figure out which items that need to be rendered, bottom right or top left
            // generally if the content position is smaller than the position of _minVisibleItemInViewPort, this means we need to show items in tops left
            // if content position is bigger than the the position of _maxVisibleItemInViewPort, this means we need to show items in bottom right
            // _needsClearance is needed because sometimes the scrolling happens so fast that the items are not showing and normalizedPosition & _lastScrollPosition would be the same stopping the update loop
            _needsClearance = false;
            var showBottomRight = false;
            if (_itemPositions[_minVisibleItemInViewPort].topLeftPosition[_axis] - contentTopLeftCorner[_axis] > 0.1f)
            {
                _needsClearance = true;
            }
            else if (_itemPositions[_maxVisibleItemInViewPort].bottomRightPosition[_axis] - contentBottomRightCorner[_axis] < -0.1f)
            {
                showBottomRight = true;
                _needsClearance = true;
            }
            _lastScrollPosition = normalizedPosition;

            if (showBottomRight && _maxVisibleItemInViewPort < _itemsCount - 1)
            {
                // item at top or left is not in viewport
                if (contentTopLeftCorner[_axis] > _itemPositions[_minVisibleItemInViewPort].bottomRightPosition[_axis])
                {
                    var itemToHide = _minVisibleItemInViewPort - _extraItemsVisible;
                    if (itemToHide > -1)
                        HideCellAtIndex(itemToHide);
                    _minVisibleItemInViewPort++;
                }

                // item at bottom or right needs to appear
                if (contentBottomRightCorner[_axis] > _itemPositions[_maxVisibleItemInViewPort].bottomRightPosition[_axis])
                {
                    var newMaxItemToCheck = _maxVisibleItemInViewPort + 1;
                    var itemToShow = newMaxItemToCheck + _extraItemsVisible;
                    if (itemToShow < _itemsCount)
                        ShowCellAtIndex(itemToShow, itemToShow - 1);
                    _maxVisibleItemInViewPort = newMaxItemToCheck;
                }
            }
            else if (!showBottomRight && _minVisibleItemInViewPort > 0)
            {
                // item at bottom or right not in viewport
                if (contentBottomRightCorner[_axis] < _itemPositions[_maxVisibleItemInViewPort].topLeftPosition[_axis])
                {
                    var itemToHide = _maxVisibleItemInViewPort + _extraItemsVisible;
                    if (itemToHide < _itemsCount)
                        HideCellAtIndex(itemToHide);
                    _maxVisibleItemInViewPort--;
                }

                // item at top or left needs to appear
                if (contentTopLeftCorner[_axis] < _itemPositions[_minVisibleItemInViewPort].topLeftPosition[_axis])
                {
                    var newMinItemToCheck = _minVisibleItemInViewPort - 1;
                    var itemToShow = newMinItemToCheck - _extraItemsVisible;
                    if (itemToShow > -1)
                        ShowCellAtIndex(itemToShow, itemToShow + 1);
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

                SetCellPosition(item.transform, newIndex, prevIndex);
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