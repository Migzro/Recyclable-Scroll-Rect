using UnityEngine;

namespace RecyclableSR
{
    public interface ICell
    { 
        int cellIndex { set; }
        RecyclableScrollRect recyclableScrollRect { set; }
        RectTransform[] CellsNeededForVisualUpdate { get; }
    }
}