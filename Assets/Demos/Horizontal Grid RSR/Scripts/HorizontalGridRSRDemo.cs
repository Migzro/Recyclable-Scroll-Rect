using System.Collections.Generic;
using RecyclableSR;
using UnityEngine;

public class HorizontalGridRSRDemo : MonoBehaviour, IGridSource
{
    [SerializeField] private int _itemsCount;
    [SerializeField] private RSRGrid _scrollRect;
    [SerializeField] private GameObject[] _prototypeCells;
    [SerializeField] private int _extraRowsColumnsVisible;
        
    private List<string> _dataSource;
    private int _itemCount;
    
    public int ItemsCount => _itemsCount;
    public int ExtraRowsColumnsVisible => _extraRowsColumnsVisible;
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
    
    [ContextMenu(nameof(ReloadData))]
    public void ReloadData()
    {
        var newItemsCount = 15;
        _dataSource.RemoveRange(newItemsCount, _itemsCount - newItemsCount);
        _itemsCount = newItemsCount;
        _scrollRect.ReloadData(true);
    }

    public float GetCellSize(int cellIndex)
    {
        return 500f;
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
}