using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Item
{
    public ICell cell { get; private set; }
    public RectTransform transform { get; private set; }
        
    public Item(ICell cell, RectTransform transform)
    {
        this.cell = cell;
        this.transform = transform;
    }
}

public class RecyclableScrollRect : ScrollRect
{
    // todo: initialize items at positions manually instead of relying on layouts with padding, spacing, expanding
    // todo: remove initialize cell maybe?
    // todo: Remove any corner visible
    // todo: test horizontal
    // todo: test with dynamic item sizes
    // todo: add leniency in what items are shown before and after
    // todo: profile and check which calls are taking the longest time
    
    [SerializeField] private IDataSourceContainer _dataSourceContainer;
    [SerializeField] private GameObject _prototypeCell;
    [SerializeField] private bool _initOnStart;
    
    private VerticalLayoutGroup _verticalLayoutGroup;
    private HorizontalLayoutGroup _horizontalLayoutGroup;
    private ContentSizeFitter _contentSizeFitter;
    private LayoutElement _layoutElement;

    private IDataSource _dataSource;
    private Vector2 _viewPortSize;
    private Vector2 _contentSize;
    private float[] _cellSizes;
    private int _minimumItemsInViewPort;
    private int _itemsCount;
    private int _minVisibleItem;
    private int _maxVisibleItem;
    private bool _hasLayoutGroup;
    private bool _init;

    private Dictionary<int, Item> _visibleItems;
    private List<Item> _invisibleItems;
    private Vector2 _lastScrollPosition;
    private RectOffset _padding;
    private TextAnchor _alignment;
    private float _spacing;

    protected override void Start()
    {
        base.Start();
        if (_initOnStart)
            Initialize();
        _lastScrollPosition = normalizedPosition;
    }

    public void Initialize()
    {
        _dataSource = _dataSourceContainer.DataSource;

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

        if (_hasLayoutGroup)
        {
            _contentSizeFitter = content.gameObject.GetComponent<ContentSizeFitter>();
            _layoutElement = content.gameObject.GetComponent<LayoutElement>();
            if (_layoutElement == null)
                _layoutElement = content.gameObject.AddComponent<LayoutElement>();
        }

        _visibleItems = new Dictionary<int, Item>();
        _invisibleItems = new List<Item>();
        _viewPortSize = viewport.rect.size;
        _init = true;
        ReloadData();
    }

    public void ReloadData()
    {
        _itemsCount = _dataSource.GetItemCount();
        DisableContentLayouts();
        CalculateLayoutSize();
        CalculateMinimumItemsInViewPort();
        InitializeCells();
    }
    
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
    
    private void CalculateLayoutSize()
    {
        var contentSizeDelta = viewport.sizeDelta;
        if (vertical)
            contentSizeDelta.y = 0;
        else
            contentSizeDelta.x = 0;
        
        _cellSizes = new float[_itemsCount];
        for (var i = 0; i < _itemsCount; i++)
        {
            var cellSize = _dataSource.GetCellSize(i);
            _cellSizes[i] = cellSize;
            
            if (vertical)
                contentSizeDelta.y += cellSize;
            else
                contentSizeDelta.x += cellSize;
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
                contentSizeDelta.y += _padding.right + _padding.left;
            }
            
            if (vertical)
                _layoutElement.preferredHeight = contentSizeDelta.y;
            else
                _layoutElement.preferredWidth = contentSizeDelta.x;
        }
        
        content.sizeDelta = contentSizeDelta;
        _contentSize = content.rect.size;
    }

    private void CalculateMinimumItemsInViewPort()
    {
        _minimumItemsInViewPort = 0;
        var rect = viewport.rect;
        var remainingViewPortSize = vertical ? rect.height : rect.width;

        for (var i = 0; i < _itemsCount; i++)
        {
            remainingViewPortSize -= _cellSizes[i];
            _minimumItemsInViewPort++;

            if (remainingViewPortSize <= 0)
                break;
        }
        
        _minVisibleItem = 0;
        _maxVisibleItem = _minimumItemsInViewPort - 1;
    }

    private void InitializeCells()
    {
        for (var i = 0; i < _minimumItemsInViewPort; i++)
        {
            InitializeCell(i);
        }
    }

    private Item InitializeCell(int index)
    {
        var itemGo = Instantiate(_prototypeCell, content, false);
        itemGo.name = index.ToString();
        
        var cell = itemGo.GetComponent<ICell>();
        var rect = itemGo.GetComponent<RectTransform>();
        
        var item = new Item(cell, rect);
        _visibleItems.Add(index, item);
        _dataSource.SetCellData(cell, index);

        SetCellSizePosition(rect, index, index - 1);
        return item;
    }

    private void SetCellSizePosition(RectTransform rect, int newIndex, int prevIndex)
    {
        // set position
        // all items are anchored to top left
        var anchorVector = new Vector2(0, 1);
        rect.anchorMin = anchorVector;
        rect.anchorMax = anchorVector;
        
        // set size
        if (_hasLayoutGroup)
        {
            // expand item width if its in a vertical layout group and the conditions are satisfied
            if (vertical && _verticalLayoutGroup.childControlWidth && _verticalLayoutGroup.childForceExpandWidth)
            {
                var itemSize = rect.sizeDelta;
                itemSize.x = content.rect.width - _verticalLayoutGroup.padding.right - _verticalLayoutGroup.padding.left;
                rect.sizeDelta = itemSize;
            }
            
            // expand item height if its in a horizontal layout group and the conditions are satisfied
            else if (!vertical && _horizontalLayoutGroup.childControlHeight && _horizontalLayoutGroup.childControlHeight)
            {
                var itemSize = rect.sizeDelta;
                itemSize.y = content.rect.height - _horizontalLayoutGroup.padding.top - _horizontalLayoutGroup.padding.bottom;
                rect.sizeDelta = itemSize;
            }
        }

        // get content size without padding
        var contentSizeWithPadding = _contentSize;
        contentSizeWithPadding.x -= _padding.right - _padding.left;
        contentSizeWithPadding.y -= _padding.top - _padding.bottom;

        // figure out where the prev cell position was
        var prevItemPosition = Vector2.zero;
        if (newIndex == 0)
        {
            prevItemPosition.y -= _padding.top;
            prevItemPosition.x += _padding.left;
        }
        if (prevIndex > -1 && prevIndex < _itemsCount - 1)
        {
            var isAfter = newIndex > prevIndex;
            prevItemPosition = _visibleItems[prevIndex].transform.anchoredPosition;
            var sign = isAfter ? -1 : 1;
            if (vertical)
                prevItemPosition.y += (_visibleItems[prevIndex].transform.rect.height * sign) + (_spacing * sign);
            else
                prevItemPosition.x += (_visibleItems[prevIndex].transform.rect.width * sign) + (_spacing * sign);
        }
                
        // set position of cell based on layout alignment
        var rectSize = rect.rect.size;
        var itemSizeSmallerThanContent = vertical && rectSize.x < contentSizeWithPadding.x || !vertical && rectSize.y < contentSizeWithPadding.y;
        if (_hasLayoutGroup && itemSizeSmallerThanContent)
        {
            if (vertical)
            {
                if (_alignment == TextAnchor.LowerCenter || _alignment == TextAnchor.MiddleCenter || _alignment == TextAnchor.UpperCenter)
                {
                    prevItemPosition.x = (_padding.left + (_contentSize.x - rectSize.x) - _padding.right) / 2f;
                }
                else if (_alignment == TextAnchor.LowerRight || _alignment == TextAnchor.MiddleRight || _alignment == TextAnchor.UpperRight)
                {
                    prevItemPosition.x = _contentSize.x - rectSize.x - _padding.right;
                }
                else
                {
                    prevItemPosition.x = _padding.left;
                }
            }
            else
            {
                // todo : this for horizontal
                rect.anchoredPosition = new Vector2(0, (_padding.top + (_contentSize.y - rectSize.y) - _padding.bottom) / 2f);
            }
        }
        rect.anchoredPosition = prevItemPosition;
    }
    
    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (!_init)
            return;
        if (normalizedPosition == _lastScrollPosition)
            return;
        if (_visibleItems.Count <= 0)
            return;

        // check the direction of the scrolling
        var isDownOrRight = true;
        if (vertical && _lastScrollPosition.y < normalizedPosition.y)
            isDownOrRight = false;
        else if (!vertical && _lastScrollPosition.x < normalizedPosition.x)
            isDownOrRight = false;
        _lastScrollPosition = normalizedPosition;
        
        var contentTopLeftCorner = content.localPosition;
        var contentBottomRightCorner = new Vector2 (contentTopLeftCorner.x + _viewPortSize.x, contentTopLeftCorner.y + _viewPortSize.y);
        if (isDownOrRight && _maxVisibleItem < _itemsCount - 1)
        {
            // item at top or left can hide
            if (_visibleItems[_minVisibleItem].transform.gameObject.activeSelf && !viewport.AnyCornerVisible(_visibleItems[_minVisibleItem].transform))
            {
                // set _minVisibleItem as not active
                _visibleItems[_minVisibleItem].transform.gameObject.SetActive(false);
                
                // remove _minVisibleItem from _visibleItems and add it in _invisibleItems
                _invisibleItems.Add(_visibleItems[_minVisibleItem]);
                _visibleItems.Remove(_minVisibleItem);
                
                // increment the _minVisibleItem by 1
                _minVisibleItem++;
            }
            
            // item at bottom or right needs to appear
            var maxItemBottomRightCorner = _visibleItems[_maxVisibleItem].transform.anchoredPosition.Abs() + _visibleItems[_maxVisibleItem].transform.rect.size;
            if (vertical && contentBottomRightCorner.y > maxItemBottomRightCorner.y || !vertical && contentBottomRightCorner.x > maxItemBottomRightCorner.x)
            {
                // Increment _maxVisibleItem by 1
                var newMaxItemToCheck = _maxVisibleItem + 1;
                
                // Show the new newMaxItemToCheck
                ShowCellAtIndex(newMaxItemToCheck, _maxVisibleItem);
                _maxVisibleItem = newMaxItemToCheck;
            }
        }
        else if (!isDownOrRight && _minVisibleItem > 0)
        {
            // item at bottom or right can hide
            if (_visibleItems[_maxVisibleItem].transform.gameObject.activeSelf && !viewport.AnyCornerVisible(_visibleItems[_maxVisibleItem].transform))
            {
                // set _maxVisibleItem as not active
                _visibleItems[_maxVisibleItem].transform.gameObject.SetActive(false);
                
                // remove _maxVisibleItem from _visibleItems and add it in _invisibleItems
                _invisibleItems.Add(_visibleItems[_maxVisibleItem]);
                _visibleItems.Remove(_maxVisibleItem);
                
                // Decrement the _maxVisibleItem by 1
                _maxVisibleItem--;
            }
            
            // item at top or left needs to appear
            var minItemBottomRightCorner = _visibleItems[_minVisibleItem].transform.anchoredPosition.Abs();
            if (vertical && contentTopLeftCorner.y < minItemBottomRightCorner.y || !vertical && contentTopLeftCorner.x < minItemBottomRightCorner.x)
            {
                // Decrement _minVisibleItem by 1
                var newMinItemToCheck = _minVisibleItem - 1;
                
                // Show the new newMinItemToCheck
                ShowCellAtIndex(newMinItemToCheck, _minVisibleItem);
                _minVisibleItem = newMinItemToCheck;
            }
        }
    }

    private void ShowCellAtIndex (int newIndex, int prevIndex)
    {
        // Get empty cell
        var isAfter = newIndex > prevIndex;
        if (_invisibleItems.Count > 0)
        {
            var item = _invisibleItems[0];
            _invisibleItems.RemoveAt(0);
            
            if (isAfter)
                item.transform.SetAsLastSibling();
            else
                item.transform.SetAsFirstSibling();
            
            item.transform.name = newIndex.ToString();
            item.transform.gameObject.SetActive(true);
            
            _dataSource.SetCellData(item.cell, newIndex);
            _visibleItems.Add(newIndex, item);
            
            SetCellSizePosition(item.transform, newIndex, prevIndex);
        }
        else
        {
            InitializeCell(newIndex);
        }
    }
}

public static class Helpers
{
    public static Vector2 Abs(this Vector2 vec2)
    {
        vec2.x = Mathf.Abs(vec2.x);
        vec2.y = Mathf.Abs(vec2.y);
        return vec2;
    }
}