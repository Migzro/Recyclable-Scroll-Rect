// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace RecyclableScrollRect
{
    public interface IRSRDataSource : IDataSource
    {
        bool IsItemSizeKnown { get; }
        float GetItemSize(int itemIndex);
    }
}