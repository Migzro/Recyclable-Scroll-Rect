using UnityEngine;

namespace RecyclableSR
{
    public interface IDataSource
    {
        int ItemsCount { get; }
        bool IsItemSizeKnown { get; }
        bool IsSetVisibleUsingCanvasGroupAlpha { get; }
        GameObject[] PrototypeItems { get; }
        float GetItemSize(int itemIndex);
        GameObject GetItemPrototype(int itemIndex);
        bool IsItemStatic(int itemIndex);
        void SetItemData(IItem item, int itemIndex);
        void ItemCreated(int itemIndex, IItem item, GameObject itemGo);
        void ItemHidden(IItem item, int itemIndex);
        void ScrolledToItem(IItem item, int itemIndex);
        bool IgnoreContentPadding(int itemIndex);
        void PullToRefresh();
        void PushToClose();
        void ReachedScrollStart();
        void ReachedScrollEnd();
        void LastItemInScrollIsVisible();
    }
}