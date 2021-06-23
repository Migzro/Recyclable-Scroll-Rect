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
    // todo: Remove any corner visible
    // todo: test horizontal
    // todo: test with items less that content
    // todo: test with dynamic item sizes
    
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
    private Vector2 lastScrollPosition;

    protected override void Start()
    {
        base.Start();
        if (_initOnStart)
            Initialize();
        lastScrollPosition = normalizedPosition;
    }

    public void Initialize()
    {
        _dataSource = _dataSourceContainer.DataSource;
        
        if (vertical)
            _verticalLayoutGroup = content.gameObject.GetComponent<VerticalLayoutGroup>();
        else
            _horizontalLayoutGroup = content.gameObject.GetComponent<HorizontalLayoutGroup>();
        
        if (_verticalLayoutGroup != null || _horizontalLayoutGroup != null)
            _hasLayoutGroup = true;

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
        _contentSize = viewport.sizeDelta;
        if (vertical)
            _contentSize.y = 0;
        else
            _contentSize.x = 0;
        
        _itemsCount = _dataSource.GetItemCount();
        CalculateLayoutSize();
        CalculateMinimumItemsInViewPort();
        InitializeCells();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        DisableContentLayouts();
    }
    
    private void CalculateLayoutSize()
    {
        _cellSizes = new float[_itemsCount];
        for (var i = 0; i < _itemsCount; i++)
        {
            var cellSize = _dataSource.GetCellSize(i);
            _cellSizes[i] = cellSize;
            
            if (vertical)
                _contentSize.y += cellSize;
            else
                _contentSize.x += cellSize;
        }
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

        var itemSize = rect.sizeDelta;
        if (_hasLayoutGroup)
        {
            if (vertical && _verticalLayoutGroup.childControlWidth && _verticalLayoutGroup.childForceExpandWidth)
            {
                itemSize.x = _contentSize.x;
            }
            
            if (!vertical && _verticalLayoutGroup.childControlHeight && _verticalLayoutGroup.childControlHeight)
            {
                itemSize.y = _contentSize.y;
            }
        }

        rect.sizeDelta = itemSize;
        return item;
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

            if (vertical)
                _layoutElement.preferredHeight = _contentSize.y;
            else
                _layoutElement.preferredWidth = _contentSize.x;
        }

        content.sizeDelta = _contentSize;
    }
    
    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (!_init)
            return;
        if (normalizedPosition == lastScrollPosition)
            return;
        if (_visibleItems.Count <= 0)
            return;

        var isDownOrRight = true;
        if (vertical && lastScrollPosition.y < normalizedPosition.y)
            isDownOrRight = false;
        else if (!vertical && lastScrollPosition.x < normalizedPosition.x)
            isDownOrRight = false;
        lastScrollPosition = normalizedPosition;
        
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
                _minVisibleItem = Mathf.Min(_itemsCount - 1, _minVisibleItem + 1);
            }
            
            // item at bottom or right needs to appear
            var maxItemBottomRightCorner = _visibleItems[_maxVisibleItem].transform.anchoredPosition.Abs() + _visibleItems[_maxVisibleItem].transform.rect.size;
            if (vertical && contentBottomRightCorner.y > maxItemBottomRightCorner.y || !vertical && contentBottomRightCorner.x > maxItemBottomRightCorner.x)
            {
                // Increment _maxVisibleItem by 1
                var newMaxItemToCheck = Mathf.Min(_itemsCount - 1, _maxVisibleItem + 1);
                
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
                _maxVisibleItem = Mathf.Max(0, _maxVisibleItem - 1);
            }
            
            // item at top or left needs to appear
            var minItemBottomRightCorner = _visibleItems[_minVisibleItem].transform.anchoredPosition.Abs();
            if (vertical && contentTopLeftCorner.y < minItemBottomRightCorner.y || !vertical && contentTopLeftCorner.x < minItemBottomRightCorner.x)
            {
                // Decrement _minVisibleItem by 1
                var newMinItemToCheck = Mathf.Max(0, _minVisibleItem - 1);
                
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
        Item item;
        if (_invisibleItems.Count > 0)
        {
            item = _invisibleItems[0];
            _invisibleItems.RemoveAt(0);
            
            if (isAfter)
                item.transform.SetAsLastSibling();
            else
                item.transform.SetAsFirstSibling();
            
            item.transform.name = newIndex.ToString();
            item.transform.gameObject.SetActive(true);
            
            _dataSource.SetCellData(item.cell, newIndex);
            _visibleItems.Add(newIndex, item);
        }
        else
        {
            item = InitializeCell(newIndex);
        }
        
        // Set position, size of cell
        var prevIndexPosition = _visibleItems[prevIndex].transform.anchoredPosition;
        var sign = isAfter ? 1 : -1;
        if (vertical)
            prevIndexPosition.y += Mathf.Sign(prevIndexPosition.y) * _visibleItems[prevIndex].transform.rect.height * sign;
        else
            prevIndexPosition.x += Mathf.Sign(prevIndexPosition.x) * _visibleItems[prevIndex].transform.rect.width * sign;
        
        item.transform.anchoredPosition = prevIndexPosition;
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