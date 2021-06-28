
using UnityEngine;

namespace RecyclableSR
{
    public interface IDataSource
    {
        int ItemsCount { get; }
        int ExtraItemsVisible { get; }
        bool IsCellSizeKnown { get; }
        GameObject[] PrototypeCells { get; }
        float GetCellSize(int cellIndex);
        void SetCellData(ICell cell, int cellIndex);
        GameObject GetPrototypeCell(int cellIndex);
        bool IsCellStatic(int cellIndex);
    }
}