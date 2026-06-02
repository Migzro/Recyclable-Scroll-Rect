// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;

namespace RecyclableScrollRect
{
    public interface IDataSource
    {
        int SectionsCount { get; }
        GameObject[] PrototypeItems { get; }
        int GetItemsCountInSection(int sectionIndex);
        GameObject GetItemPrototype(int sectionIndex, int itemIndex, ItemType itemType);
        bool IsItemStatic(int sectionIndex, int itemIndex);
        void SetItemData(IItem item, int sectionIndex, int itemIndex);
        void ItemCreated(int sectionIndex, int itemIndex, IItem item, GameObject itemGo);
        void ItemHidden(IItem item, int sectionIndex, int itemIndex);
        void ScrolledToItem(IItem item, int sectionIndex, int itemIndex);
        bool IgnoreContentPadding(int sectionIndex, int itemIndex);
        void PullToRefresh();
        void PushToClose();
        void ReachedScrollStart();
        void ReachedScrollEnd();
        void LastItemIsVisible();
        bool SectionHasHeader(int sectionIndex);
        bool SectionHasFooter(int sectionIndex);
        bool HeaderIsPinned (int sectionIndex);
    }
}