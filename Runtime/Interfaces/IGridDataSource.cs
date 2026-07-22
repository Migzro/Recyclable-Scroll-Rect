// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace RecyclableScrollRect
{
    public interface IGridDataSource : IDataSource
    {
        float GetHeaderFooterSize(ItemData itemData);
    }
}