using UnityEngine;
using UnityEngine.UI;

public class RecyclableScrollRect : ScrollRect
{
    [SerializeField] private IDataSourceContainer _dataSourceContainer;
    [SerializeField] private GameObject _prototypeCell;
    [SerializeField] private bool _initOnStart;
    
    private IDataSource _dataSource;
    private VerticalLayoutGroup _verticalLayoutGroup;
    private HorizontalLayoutGroup _horizontalLayoutGroup;
    private ContentSizeFitter _contentSizeFitter;
    private LayoutElement _layoutElement;
    private bool _hasLayoutGroup;
    private float _contentSize;

    protected override void Start()
    {
        base.Start();
        if (_initOnStart)
            Initialize();
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
        CalculateLayoutSize();
        CalculateMinimumItemsInViewPort();
        InitializeCells();
        Debug.Log($"Content Size: {_contentSize}");
    }

    private void CalculateMinimumItemsInViewPort()
    {
        
    }

    private void CalculateLayoutSize()
    {
        for (var i = 0; i < _dataSource.GetItemCount(); i++)
        {
            _contentSize += _dataSource.GetCellSize(i);
        }
    }

    private void InitializeCells()
    {
        for (var i = 0; i < _dataSource.GetItemCount(); i++)
        {
            var item = Instantiate(_prototypeCell, content, false);
            _dataSource.SetCellData(item.GetComponent<ICell>(), i);
        }
    }
}