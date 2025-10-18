using System.Collections.Generic;
using RecyclableSR;
using UnityEngine;

public class VerticalDynamicRSRDemo : MonoBehaviour, IRSRSource
{
    [SerializeField] private int _itemsCount;
    [SerializeField] private RSR _scrollRect;
    [SerializeField] private GameObject[] _prototypeItems;
        
    private List<string> _dataSource;
    
    public int ItemsCount => _itemsCount;
    public bool IsItemSizeKnown => false;
    public GameObject[] PrototypeItems => _prototypeItems;

    private void Start()
    {
        _dataSource = new List<string>();
        for (var i = 0; i < _itemsCount; i++)
            _dataSource.Add(i + " " + HelperFunctions.RandomString(Random.Range(100, 200)));
        _scrollRect.Initialize(this);
        Invoke(nameof(ReloadItem), 5);
    }

    private void ReloadItem()
    {
        _dataSource[5] = "5";
        _scrollRect.ReloadItem(5, "", true);
    }

    public float GetItemSize(int itemIndex)
    {
        return -1;
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

    public void LastItemInScrollIsVisible()
    {
    }
}