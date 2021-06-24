using System.Collections.Generic;
using UnityEngine;

public class MainController : MonoBehaviour, IDataSource
{
    [SerializeField] private int _itemsCount;
    [SerializeField] private RecyclableScrollRect _scrollRect;
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
        return 100;
    }

    public void SetCellData(ICell cell, int cellIndex)
    {
        (cell as DemoCellPrototype)?.Initialize(_dataSource[cellIndex], cellIndex);
    }

    public bool IsCellStatic(int cellIndex)
    {
        return false;
    }
}