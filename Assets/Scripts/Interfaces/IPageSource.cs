// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;

namespace RSR
{
    public interface IPageSource : IDataSource
    {
        void PageFocused(int itemIndex, bool isNextPage, IItem item);
        void PageUnFocused(int itemIndex, bool isNextPage, IItem item);
        void PageWillFocus(int itemIndex, bool isNextPage, IItem item, RectTransform rect, Vector2 originalPosition);
        void PageWillUnFocus(int itemIndex, bool isNextPage, IItem item, RectTransform rect);
    }
}