using UnityEngine;

namespace RecyclableSR
{
    public interface IPageSource : IDataSource
    {
        void PageFocused(int cellIndex, bool isNextPage, ICell cell);
        void PageUnFocused(int cellIndex, bool isNextPage, ICell cell);
        void PageWillFocus(int cellIndex, bool isNextPage, ICell cell, RectTransform rect, Vector2 originalPosition);
        void PageWillUnFocus(int cellIndex, bool isNextPage, ICell cell, RectTransform rect);
    }
}