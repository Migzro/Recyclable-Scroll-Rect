using System;
using UnityEngine;
using UnityEngine.UI;

public class RecyclableScrollRect : ScrollRect
{
    [SerializeField] private IDataSourceContainer _dataSourceContainer;
    [SerializeField] private GameObject _prototypeCell;
    [SerializeField] private bool _initOnStart;
    
    private VerticalLayoutGroup _verticalLayoutGroup;
    private HorizontalLayoutGroup _horizontalLayoutGroup;
    private ContentSizeFitter _contentSizeFitter;
    private LayoutElement _layoutElement;

    private IDataSource _dataSource;
    private float[] _cellSizes;
    private float _contentSize;
    private int _minimumItemsInViewPort;
    private int _itemsCount;
    private int _minVisibleItem;
    private int _maxVisibleItem;
    private bool _hasLayoutGroup;

    private RectTransform[] _createdItems;
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
        
        ReloadData();
    }

    public void ReloadData()
    {
        _contentSize = 0;
        _itemsCount = _dataSource.GetItemCount();
        CalculateLayoutSize();
        CalculateMinimumItemsInViewPort();
        InitializeCells();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        DisableContentLayouts();
        HideMinimumItemsInViewPort();
    }

    private void CalculateMinimumItemsInViewPort()
    {
        _minimumItemsInViewPort = 0;
        var rect = viewport.rect;
        var viewPortSize = vertical ? rect.height : rect.width;

        for (var i = 0; i < _itemsCount; i++)
        {
            viewPortSize -= _cellSizes[i];
            _minimumItemsInViewPort++;

            if (viewPortSize <= 0 || i + 1 < _itemsCount && viewPortSize <= _cellSizes[i+1])
                break;
        }
    }

    private void CalculateLayoutSize()
    {
        _cellSizes = new float[_itemsCount];
        for (var i = 0; i < _itemsCount; i++)
        {
            var cellSize = _dataSource.GetCellSize(i);
            _cellSizes[i] = cellSize;
            _contentSize += cellSize;
        }
    }

    private void InitializeCells()
    {
        _createdItems = new RectTransform[_itemsCount];
        for (var i = 0; i < _itemsCount; i++)
        {
            var item = Instantiate(_prototypeCell, content, false);
            _createdItems[i] = item.GetComponent<RectTransform>();
            _dataSource.SetCellData(item.GetComponent<ICell>(), i);
        }
    }

    private void HideMinimumItemsInViewPort()
    {
        for (var i = 0; i < _itemsCount; i++)
        {
            if (i > _minimumItemsInViewPort)
                _createdItems[i].gameObject.SetActive(false);
        }

        _minVisibleItem = 0;
        _maxVisibleItem = _minimumItemsInViewPort;
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
            _layoutElement.preferredWidth = _contentSize;
        }
        else
        {
            var sizeDelta = content.sizeDelta;
            if (vertical)
                sizeDelta.y = _contentSize;
            else
                sizeDelta.x = _contentSize;

            content.sizeDelta = sizeDelta;
        }
    }
    
    protected override void LateUpdate()
    {
        base.LateUpdate();
        
        if (Mathf.Abs(normalizedPosition.magnitude - lastScrollPosition.magnitude) <= Double.Epsilon)
            return;
        if (_createdItems.Length <= 0)
            return;

        var isDownOrRight = true;
        if (vertical && lastScrollPosition.y < normalizedPosition.y)
            isDownOrRight = false;
        else if (!vertical && lastScrollPosition.x < normalizedPosition.x)
            isDownOrRight = false;
        lastScrollPosition = normalizedPosition;

        if (isDownOrRight)
        {
            var newMaxItemToCheck = Mathf.Min(_itemsCount - 1, _maxVisibleItem + 1);
            if (_createdItems[_minVisibleItem].gameObject.activeSelf && !viewport.AnyCornerVisible(_createdItems[_minVisibleItem]))
            {
                _createdItems[_minVisibleItem].gameObject.SetActive(false);
                _minVisibleItem = Mathf.Min(_itemsCount - 1, _minVisibleItem + 1);
            }
            
            if (!_createdItems[newMaxItemToCheck].gameObject.activeSelf && viewport.AnyCornerVisible(_createdItems[newMaxItemToCheck]))
            {
                _createdItems[newMaxItemToCheck].gameObject.SetActive(true);
                _maxVisibleItem = newMaxItemToCheck;
            }
        }
        else
        {
            var newMinItemToCheck = Mathf.Max(0, _minVisibleItem - 1);
            if (!_createdItems[newMinItemToCheck].gameObject.activeSelf && viewport.AnyCornerVisible(_createdItems[newMinItemToCheck]))
            {
                _createdItems[newMinItemToCheck].gameObject.SetActive(true);
                _minVisibleItem = newMinItemToCheck;
            }
            
            if (_createdItems[_maxVisibleItem].gameObject.activeSelf && !viewport.AnyCornerVisible(_createdItems[_maxVisibleItem]))
            {
                _createdItems[_maxVisibleItem].gameObject.SetActive(false);
                _maxVisibleItem = Mathf.Max(0, _maxVisibleItem - 1);
            }
        }
    }
}