// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace RecyclableScrollRect
{
    public interface ICarouselDataSource : IPageDataSource
    {
        /// <summary>
        /// Called whenever a visible item moves, use it to drive visuals such as scale, alpha or rotation
        /// </summary>
        /// <param name="item">the item that moved</param>
        /// <param name="itemData">the data of the item</param>
        /// <param name="offsetFromFocus">how many items away from the center of the viewport the item is, 0 is centered, negative is before it, positive is after it</param>
        void ItemOffsetChanged(IItem item, ItemData itemData, float offsetFromFocus);
    }
}
