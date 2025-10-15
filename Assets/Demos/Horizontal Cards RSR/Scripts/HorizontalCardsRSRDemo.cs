using System.Collections.Generic;
using DG.Tweening;
using RecyclableSR;
using UnityEngine;

public class HorizontalCardsRSRDemo : MonoBehaviour, IPageSource
{
    [SerializeField] private int _itemsCount;
    [SerializeField] private RSRCards _scrollRect;
    [SerializeField] private GameObject[] _prototypeCells;
    [SerializeField] private int _extraItemsVisible;
    [SerializeField] private float _animationTime;
    [SerializeField] private Ease _animationEase;
        
    private List<string> _dataSource;
    private int _itemCount;
    
    public int ItemsCount => _itemsCount;
    public int ExtraItemsVisible => _extraItemsVisible;
    public bool IsCellSizeKnown => true;
    public bool IsSetVisibleUsingCanvasGroupAlpha => false;
    public GameObject[] PrototypeCells => _prototypeCells;

    private void Start()
    {
        _dataSource = new List<string>();
        for (var i = 0; i < _itemsCount; i++)
            _dataSource.Add( i.ToString() );
        _scrollRect.Initialize(this);
    }

    public float GetCellSize(int cellIndex)
    {
        return 1000f;
    }

    public void SetCellData(ICell cell, int cellIndex)
    {
        (cell as DemoCellPrototype)?.Initialize(_dataSource[cellIndex]);
    }

    public void CellHidden(ICell cell, int cellIndex)
    {
    }

    public GameObject GetPrototypeCell(int cellIndex)
    {
        if (cellIndex % 2 == 0)
            return _prototypeCells[0]; 
        return _prototypeCells[1];
    }

    public void CellCreated(int cellIndex, ICell cell, GameObject cellGo)
    {
        
    }

    public bool IsCellStatic(int cellIndex)
    {
        return false;
    }

    public void ScrolledToCell(ICell cell, int cellIndex)
    {
    }

    public bool IgnoreContentPadding(int cellIndex)
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

    public void PageFocused(int cellIndex, bool isNextPage, ICell cell)
    {
        if (!isNextPage)
            cell.CanvasGroup.DOFade(1, _animationTime).SetEase(_animationEase);
    }

    public void PageUnFocused(int cellIndex, bool isNextPage, ICell cell)
    {
        if (isNextPage)
            cell.CanvasGroup.DOFade(0, _animationTime).SetEase(_animationEase);
    }

    public void PageWillFocus(int cellIndex, bool isNextPage, ICell cell, RectTransform rect, Vector2 originalPosition)
    {
        if (!isNextPage)
        {
            var tempPosition = originalPosition;
            tempPosition.x -= 1000;
            rect.localPosition = tempPosition;
            rect.DOAnchorPosX(originalPosition.x, _animationTime).SetEase(_animationEase);
        }
    }

    public void PageWillUnFocus(int cellIndex, bool isNextPage, ICell cell, RectTransform rect)
    {
        if (isNextPage)
        {
            rect.DOAnchorPosX(rect.anchoredPosition.x - 1000, _animationTime).SetEase(_animationEase);
        }
    }
}