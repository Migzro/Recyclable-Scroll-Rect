
using UnityEngine;

namespace RecyclableSR
{
    public interface IDataSource
    {
        int GetItemCount();
        float GetCellSize(int cellIndex);
        void SetCellData(ICell cell, int cellIndex);
        GameObject GetCellPrototypeCell(int cellIndex);
        bool IsCellStatic(int cellIndex);
    }
}