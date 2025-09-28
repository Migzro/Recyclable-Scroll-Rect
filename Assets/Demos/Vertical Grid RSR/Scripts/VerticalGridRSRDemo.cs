using System.Collections.Generic;
using RecyclableSR;
using UnityEngine;

public class VerticalGridRSRDemo : MonoBehaviour, IDataSource
{
    [SerializeField] private int _itemsCount;
    [SerializeField] private RecyclableScrollRect _scrollRect;
    [SerializeField] private GameObject[] _prototypeCells;
    [SerializeField] private int _extraItemsVisible;
        
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
        return _prototypeCells[0];
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