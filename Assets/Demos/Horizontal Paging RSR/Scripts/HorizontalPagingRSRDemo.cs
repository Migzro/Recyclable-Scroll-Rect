using System.Collections.Generic;
using RecyclableSR;
using UnityEngine;

public class HorizontalPagingRSRDemo : MonoBehaviour, IPageSource
{
    [SerializeField] private int _itemsCount;
    [SerializeField] private RSRPages _scrollRect;
    [SerializeField] private GameObject[] _prototypeItems;
        
    private List<string> _dataSource;
    private int _itemCount;
    
    public int ItemsCount => _itemsCount;
    public bool IsItemSizeKnown => true;
    public bool IsSetVisibleUsingCanvasGroupAlpha => false;
    public GameObject[] PrototypeItems => _prototypeItems;

    private void Start()
    {
        _dataSource = new List<string>();
        for (var i = 0; i < _itemsCount; i++)
            _dataSource.Add( i.ToString() );
        _scrollRect.Initialize(this);
    }

    public float GetItemSize(int itemIndex)
    {
        return 1334f;
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

    public void PageFocused(int itemIndex, bool isNextPage, IItem item)
    {
    }

    public void PageUnFocused(int itemIndex, bool isNextPage, IItem item)
    {
    }

    public void PageWillFocus(int itemIndex, bool isNextPage, IItem item, RectTransform rect, Vector2 originalPosition)
    {
    }

    public void PageWillUnFocus(int itemIndex, bool isNextPage, IItem item, RectTransform rect)
    {
    }
}