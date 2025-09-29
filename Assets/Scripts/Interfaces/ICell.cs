using UnityEngine;

namespace RecyclableSR
{
    public interface ICell
    { 
        int CellIndex { set; }
        RSRBase RSRBase { set; }
        RectTransform[] CellsNeededForVisualUpdate { get; }
        CanvasGroup CanvasGroup { get; }
    }
}