using System;
using System.Collections.Generic;
using System.Linq;
using RecyclableSR;
using UnityEngine;
using Random = UnityEngine.Random;

public class ScrollDemoController : MonoBehaviour, IDataSource
{
    [SerializeField] private RecyclableScrollRect _scrollRect;
    [SerializeField] private GameObject[] _prototypeCells;
    [SerializeField] private int _extraItemsVisible;
        
    private List<string> _dataSource;
    
    public int ItemsCount => _itemsCount;
    public int ExtraItemsVisible => _extraItemsVisible;
    public bool IsCellSizeKnown => true;
    public bool IsSetVisibleUsingCanvasGroupAlpha => false;
    public GameObject[] PrototypeCells => _prototypeCells;

    private int _itemsCount;

    private void Start()
    {
        _itemsCount = 30;
        _dataSource = new List<string>();
        for (var i = 0; i < _itemsCount; i++)
        {
            _dataSource.Add( i.ToString() );
        }
        _scrollRect.Initialize(this);
        // _scrollRect.ScrollToCell(_itemsCount - 1, true, true);
        // Invoke(nameof(test), 5f);
        // Invoke(nameof(Test), 5f);
    }

    private void test()
    {
        _scrollRect.ScrollToCell(50);
        Invoke(nameof(ScrollBack), 5f);
    }
    
    private void ScrollBack()
    {
        // _dataSource[5] = "5 " + RandomString(Random.Range(0, 200));
        // _scrollRect.ReloadCell(5, "Tag", true);
        _scrollRect.ScrollToCell(0);
        // Invoke(nameof(Test), 5f);
        // _scrollRect.ScrollToTopRight();
    }

    public float GetCellSize(int cellIndex)
    {
        return _scrollRect.vertical ? 40.22f : 60.28f; // if cell size is known
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
        Debug.Log($"Scrolled to cell {cellIndex}");
    }

    public bool IgnoreContentPadding(int cellIndex)
    {
        return false;
    }

    public void PullToRefresh()
    {
        Debug.Log( "Pull to refresh" );
    }

    public void ReachedScrollStart()
    {
    }

    public void ReachedScrollEnd()
    {
        Debug.Log( "End" );
        
        // var newData = new List<string>();
        // for (var i = _itemsCount; i < _itemsCount + 10; i++)
        // {
        //     newData.Add(i.ToString());
        // }
        // _dataSource.AddRange(newData);
        // _itemsCount += 10;
        // _scrollRect.ReloadData();
    }

    public void LastItemInScrollIsVisible()
    {
    }

    private static System.Random random = new System.Random();
    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}