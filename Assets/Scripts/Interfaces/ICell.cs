using UnityEngine;

namespace RecyclableSR
{
    public interface ICell
    { 
        int CellIndex { set; }
        RecyclableScrollRect RecyclableScrollRect { set; }
        RectTransform[] CellsNeededForVisualUpdate { get; }
        CanvasGroup CanvasGroup { get; }
    }
}