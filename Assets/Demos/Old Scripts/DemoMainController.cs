using System.Collections.Generic;
using System.Linq;
using RecyclableSR;
using UnityEngine;

public class DemoMainController : MonoBehaviour, IDataSource
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
    public bool IsSetVisibleUsingCanvasGroupAlpha { get; }
    public GameObject[] PrototypeCells => _prototypeCells;

    private void Start()
    {
        _dataSource = new List<string>();
        for (var i = 0; i < _itemsCount; i++)
        {
            // _dataSource.Add(i + " " + RandomString(Random.Range(0, 200)));
            _dataSource.Add( i.ToString() );
        }
        _scrollRect.Initialize(this);
        // Invoke(nameof(ChangeCellData), 2.5f);
    }

    private void ChangeCellData()
    {
        // _dataSource[5] = "5 " + RandomString(Random.Range(0, 200));
        // _scrollRect.ReloadCell(5, "Tag", true);
        _scrollRect.ScrollToCell(10);
        Invoke(nameof(ScrollBack), 7f);
        // Invoke(nameof(Test), 5f);
    }
    
    private void ScrollBack()
    {
        // _dataSource[5] = "5 " + RandomString(Random.Range(0, 200));
        // _scrollRect.ReloadCell(5, "Tag", true);
        _scrollRect.ScrollToCell(0);
        // Invoke(nameof(Test), 5f);
    }
    
    private void Test()
    {
        _dataSource[ 5 ] = "5";
        _scrollRect.ReloadCell(5, "Tag", true);
        // _dataSource[5] = "5 " + RandomString(Random.Range(0, 200));
        // _scrollRect.ReloadCell(5, "Tag", true);
        // _scrollRect.ScrollToCell(0);
    }

    public float GetCellSize(int cellIndex)
    {
        // return _scrollRect.vertical ? 40.22f : 60.28f; // if cell size is known
        return 1334;
        // return 500;
    }

    public void SetCellData(ICell cell, int cellIndex)
    {
        Debug.LogWarning($"Setting cell data {cellIndex}");
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

    public void PushToClose()
    {
    }

    public void ReachedScrollStart()
    {
    }

    public void ReachedScrollEnd()
    {
        Debug.Log( "End" );
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