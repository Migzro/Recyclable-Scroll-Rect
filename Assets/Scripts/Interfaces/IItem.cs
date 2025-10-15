using UnityEngine;

namespace RecyclableSR
{
    public interface IItem
    { 
        int ItemIndex { set; }
        RSRBase RSRBase { set; }
        RectTransform[] ItemsNeededForVisualUpdate { get; }
        CanvasGroup CanvasGroup { get; }
    }
}