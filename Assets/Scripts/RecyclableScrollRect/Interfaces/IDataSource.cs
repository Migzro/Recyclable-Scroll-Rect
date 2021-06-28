
using UnityEngine;

namespace RecyclableSR
{
    public interface IDataSource
    {
        int GetItemCount();
        int GetExtraItemsVisible();
        bool IsCellSizeKnown();
        float GetCellSize(int cellIndex);
        void SetCellData(ICell cell, int cellIndex);
        GameObject GetCellPrototypeCell(int cellIndex);
        GameObject[] GetCellPrototypeCells();
        bool IsCellStatic(int cellIndex);
    }
}