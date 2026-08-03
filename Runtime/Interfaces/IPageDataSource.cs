// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace RecyclableScrollRect
{
    public interface IPageDataSource : IRSRDataSource
    {
        void PageWillFocus(IItem item, ItemData itemData, bool isNextPage);
        void PageWillUnFocus(IItem item, ItemData itemData, bool isNextPage);
    }
}