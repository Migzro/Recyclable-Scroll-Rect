// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;

namespace RecyclableScrollRect
{
    /// <summary>
    /// A carousel layout where the items after the focused one wait in a stack behind it,
    /// and the focused item slides away when swiping to the next one
    /// </summary>
    public class RSRCardStack : RSRCarousel
    {
        // how much smaller each card in the stack gets, and how far it peeks out from under the card in front of it
        protected virtual float DepthScaleStep => 0.05f;
        protected virtual float DepthPeek => 24f;
        protected virtual float MaxDepth => 3f;

        protected override void LayoutItem(int itemIndex, RectTransform itemRect, float offsetFromFocus)
        {
            base.LayoutItem(itemIndex, itemRect, offsetFromFocus);

            // the focused item and the ones before it keep the row layout, so they slide away with the drag
            if (offsetFromFocus <= 0)
            {
                itemRect.localScale = Vector3.one;
                return;
            }

            // pull the waiting items back to the center so they sit under the focused item
            var position = itemRect.anchoredPosition;
            var itemStep = GetItemAxisSize(itemIndex) + _spacing[_axis];
            position[_axis] -= offsetFromFocus * itemStep * (vertical ? -1 : 1);

            // scaling happens around the top left pivot, shift by half the lost size to keep the item centered,
            // then push it along the cross axis so its edge peeks out from under the item in front of it
            var depth = Mathf.Min(offsetFromFocus, MaxDepth);
            var scale = 1f - depth * DepthScaleStep;
            var size = itemRect.rect.size;
            position += new Vector2(size.x, -size.y) * ((1f - scale) / 2f);
            position[1 - _axis] += depth * DepthPeek * (vertical ? 1 : -1);

            itemRect.localScale = new Vector3(scale, scale, 1f);
            itemRect.anchoredPosition = position;
        }

        /// <summary>
        /// Earlier items are drawn on top, so the focused item covers the ones waiting behind it
        /// </summary>
        protected override void SetSiblingIndices()
        {
            foreach (var visibleItem in _visibleItems)
            {
                visibleItem.Value.transform.SetAsFirstSibling();
            }
        }
    }
}
