using UnityEngine;

namespace RecyclableSR
{
    public interface IPageSource : IDataSource
    {
        void PageFocused(int itemIndex, bool isNextPage, IItem item);
        void PageUnFocused(int itemIndex, bool isNextPage, IItem item);
        void PageWillFocus(int itemIndex, bool isNextPage, IItem item, RectTransform rect, Vector2 originalPosition);
        void PageWillUnFocus(int itemIndex, bool isNextPage, IItem item, RectTransform rect);
    }
}