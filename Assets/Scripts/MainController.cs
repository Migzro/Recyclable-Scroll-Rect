using System.Collections.Generic;
using System.Linq;
using RecyclableSR;
using UnityEngine;

public class MainController : MonoBehaviour, IDataSource
{
    [SerializeField] private bool _isVertical;
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
            // _dataSource.Add(i.ToString());
            if (i == 5)
                _dataSource.Add(i + " " + RandomString(Random.Range(1, 1)));
            else
                _dataSource.Add(i + " " + RandomString(Random.Range(100, 200)));
            // _dataSource.Add(i.ToString());
        }
        _scrollRect.Initialize(this);
        
        Invoke(nameof(ChangeCell), 1);
    }

    private void ChangeCell()
    {
        Debug.LogWarning("Lets Go");
        _dataSource[5] = "5 " + RandomString(Random.Range(220, 300));
        _scrollRect.ReloadCell(5, true);
    }

    public float GetCellSize(int cellIndex)
    {
        // var verticalCellSize = cellIndex % 2 == 0 ? 100 : 200;
        // return _isVertical ? 40.22f : 60.28f;
        return -1;
    }

    public void SetCellData(ICell cell, int cellIndex)
    {
        (cell as DemoCellPrototype)?.Initialize(_dataSource[cellIndex], cellIndex);
    }

    public GameObject GetPrototypeCell(int cellIndex)
    {
        if (cellIndex % 2 == 0)
            return _prototypeCells[0]; 
        
        return _prototypeCells[1];
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