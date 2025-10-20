// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;

namespace RecyclableScrollRect
{
    public interface IDataSource
    {
        int ItemsCount { get; }
        bool IsItemSizeKnown { get; }
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
        void LastItemIsVisible();
    }
}