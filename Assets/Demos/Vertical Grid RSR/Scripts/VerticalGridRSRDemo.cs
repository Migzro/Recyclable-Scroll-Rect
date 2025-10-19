using System.Collections.Generic;
using RecyclableSR;
using UnityEngine;

public class VerticalGridRSRDemo : MonoBehaviour, IGridSource
{
    [SerializeField] private int _itemsCount;
    [SerializeField] private RSRGrid _scrollRect;
    [SerializeField] private GameObject[] _prototypeItems;
    [SerializeField] private int _itemsToReloadTo;
        
    private List<string> _dataSource;
    private int _itemCount;
    
    public int ItemsCount => _dataSource.Count;
    public bool IsItemSizeKnown => true;
    public GameObject[] PrototypeItems => _prototypeItems;

    private void Start()
    {
        _dataSource = new List<string>();
        for (var i = 0; i < _itemsCount; i++)
            _dataSource.Add( i.ToString() );
        _scrollRect.Initialize(this);
    }

    [ContextMenu(nameof(ReloadData))]
    public void ReloadData()
    {
        _dataSource.RemoveRange(_itemsToReloadTo, _itemsCount - _itemsToReloadTo);
        _itemsCount = _itemsToReloadTo;
        _scrollRect.ReloadData(true);
    }

    public float GetItemSize(int itemIndex)
    {
        return 500f;
    }

    public void SetItemData(IItem item, int itemIndex)
    {
        (item as DemoItemPrototype)?.Initialize(_dataSource[itemIndex]);
    }

    public void ItemHidden(IItem item, int itemIndex)
    {
    }

    public GameObject GetItemPrototype(int itemIndex)
    {
        if (itemIndex % 2 == 0)
            return _prototypeItems[0];
        return _prototypeItems[1];
    }

    public void ItemCreated(int itemIndex, IItem item, GameObject itemGo)
    {
        
    }

    public bool IsItemStatic(int itemIndex)
    {
        return false;
    }

    public void ScrolledToItem(IItem item, int itemIndex)
    {
    }

    public bool IgnoreContentPadding(int itemIndex)
    {
        return false;
    }

    public void PullToRefresh()
    {
    }

    public void PushToClose()
    {
    }

    public void ReachedScrollStart()
    {
    }

    public void ReachedScrollEnd()
    {
    }

    public void LastItemIsVisible()
    {
    }
}