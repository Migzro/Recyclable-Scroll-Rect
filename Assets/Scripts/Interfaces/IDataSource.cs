
using UnityEngine;

namespace RecyclableSR
{
    public interface IDataSource
    {
        int ItemsCount { get; }
        int ExtraItemsVisible { get; }
        bool IsCellSizeKnown { get; }
        bool IsSetVisibleUsingCanvasGroupAlpha { get; }
        GameObject[] PrototypeCells { get; }
        float GetCellSize(int cellIndex);
        void SetCellData(ICell cell, int cellIndex);
        void CellHidden(ICell cell, int cellIndex);
        GameObject GetPrototypeCell(int cellIndex);
        void CellCreated(int cellIndex, ICell cell, GameObject cellGo);
        bool IsCellStatic(int cellIndex);
        void ScrolledToCell(ICell cell, int cellIndex);
        bool IgnoreContentPadding(int cellIndex);
        void PullToRefresh();
        void ReachedScrollStart();
        void ReachedScrollEnd();
    }
}