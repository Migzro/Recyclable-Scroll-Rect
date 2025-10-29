// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace RecyclableScrollRect
{
    public interface IPageDataSource : IRSRDataSource
    {
        void PageWillFocus(int itemIndex, bool isNextPage, IItem item);
        void PageWillUnFocus(int itemIndex, bool isNextPage, IItem item);
    }
}