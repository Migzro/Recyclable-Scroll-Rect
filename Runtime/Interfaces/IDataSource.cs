// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;

namespace RecyclableScrollRect
{
    public interface IDataSource
    {
        GameObject[] PrototypeItems { get; }
        int GetItemsCount(int sectionIndex);
        GameObject GetItemPrototype(ItemData itemData);
        void SetItemData(IItem item, ItemData itemData);
        void ItemCreated(IItem item, GameObject itemGo, ItemData itemData);
        void ItemHidden(IItem item, ItemData itemData);
        void ScrolledToItem(IItem item, ItemData itemData);
        bool IgnoreContentPadding(ItemData itemData);
        void PullToRefresh();
        void PushToClose();
        void ReachedScrollStart();
        void ReachedScrollEnd();
        void LastItemIsVisible();
    }
}