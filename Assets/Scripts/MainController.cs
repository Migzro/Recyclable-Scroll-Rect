using System.Collections.Generic;
using RecyclableSR;
using UnityEngine;

public class MainController : MonoBehaviour, IDataSource
{
    [SerializeField] private bool _isVertical;
    [SerializeField] private int _itemsCount;
    [SerializeField] private RecyclableScrollRect _scrollRect;
    [SerializeField] private GameObject[] _prototypeCells;
        
    private List<string> _dataSource;

    private void Start()
    {
        _dataSource = new List<string>();
        for (var i = 0; i < _itemsCount; i++)
        {
            _dataSource.Add(i.ToString());
        }
        _scrollRect.Initialize();
    }

    public int GetItemCount()
    {
        return _itemsCount;
    }

    public float GetCellSize(int cellIndex)
    {
        var verticalCellSize = cellIndex % 2 == 0 ? 100 : 200;
        return _isVertical ? verticalCellSize : 300;
    }

    public void SetCellData(ICell cell, int cellIndex)
    {
        (cell as DemoCellPrototype)?.Initialize(_dataSource[cellIndex], cellIndex);
    }

    public GameObject GetCellPrototypeCell(int cellIndex)
    {
        if (cellIndex % 2 == 0)
            return _prototypeCells[0]; 
        
        return _prototypeCells[1];
    }

    public bool IsCellStatic(int cellIndex)
    {
        return false;
    }
}