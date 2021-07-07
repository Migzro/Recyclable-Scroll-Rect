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
    public bool IsCellSizeKnown => false;
    public GameObject[] PrototypeCells => _prototypeCells;

    private void Start()
    {
        _dataSource = new List<string>();
        for (var i = 0; i < _itemsCount; i++)
        {
            _dataSource.Add(i.ToString());
        }
        _scrollRect.Initialize(this);
        Invoke(nameof(ChangeCellData), 5);
    }

    private void ChangeCellData()
    {
        _dataSource[5] = "5 " + RandomString(Random.Range(0, 200));
        _scrollRect.ReloadCell(5, "Tag", true);
    }

    public float GetCellSize(int cellIndex)
    {
        // return _scrollRect.vertical ? 40.22f : 60.28f; // if cell size is known
        return -1;
    }

    public void SetCellData(ICell cell, int cellIndex)
    {
        (cell as DemoCellPrototype)?.Initialize(_dataSource[cellIndex]);
    }

    public GameObject GetPrototypeCell(int cellIndex)
    {
        if (cellIndex % 2 == 0)
            return _prototypeCells[0]; 
        
        return _prototypeCells[1];
    }

    public void CellCreated(ICell cell, GameObject cellGo)
    {
        
    }

    public bool IsCellStatic(int cellIndex)
    {
        return false;
    }
    
    private static System.Random random = new System.Random();
    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}