// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;

namespace RecyclableScrollRect
{
    /// <summary>
    /// A carousel layout where the cards overlap around the focused card, which is centered and drawn on top
    /// </summary>
    public class RSRCardStack : RSRCarousel
    {
        public enum CardStackMode
        {
            // the next cards wait in a stack behind the focused card, the previous cards slide off the screen
            SlideAway,
            // like SlideAway, but the previous cards slide out and tuck back under the stack
            TuckUnder,
            // the cards spread out on both sides of the focused card, each one peeking out from under its neighbour
            Fan
        }

        [SerializeField, Tooltip("How the cards around the focused card are laid out")] private CardStackMode _mode;
        [SerializeField, Tooltip("How much smaller each card gets per step away from the focused card")] private float _depthScaleStep = 0.05f;
        [SerializeField, Tooltip("Cards deeper than this in the stack stop getting smaller, fanned cards keep getting smaller")] private float _maxDepth = 3f;
        [SerializeField, Tooltip("How far each card in the stack peeks out from under the card in front of it, negative values peek out the other side")] private float _depthPeek = 24f;
        [SerializeField, Tooltip("How far a card slides out before tucking back under the stack, in item sizes")] private float _tuckSlideDistance = 0.5f;
        [SerializeField, Tooltip("How far apart neighbouring cards sit along the scrolling axis")] private float _fanSpacing = 80f;
        [SerializeField, Tooltip("Keep as many cards alive as fit across the viewport at the fan spacing, instead of only the extra items visible")] private bool _fanFillViewport;
        [SerializeField, Tooltip("Cards that would pass the edge of the viewport stop there and pile up, so they stay in view")] private bool _fanPileAtEdges;

        public CardStackMode Mode => _mode;

        protected override int ExtraItemsVisible
        {
            get
            {
                if (_mode != CardStackMode.Fan || !_fanFillViewport || _fanSpacing <= 0)
                {
                    return base.ExtraItemsVisible;
                }
                return Mathf.Max(base.ExtraItemsVisible, Mathf.CeilToInt(viewport.rect.size[_axis] / 2f / _fanSpacing));
            }
        }

        protected override void LayoutItem(int itemIndex, RectTransform itemRect, float offsetFromFocus)
        {
            base.LayoutItem(itemIndex, itemRect, offsetFromFocus);

            // how deep in the stack the card is, how far it sits from the center along the scrolling axis,
            // and how far it peeks out along the cross axis
            var itemSize = GetItemAxisSize(itemIndex);
            var itemStep = itemSize + _spacing[_axis];
            float depth;
            float axisOffset;
            float crossPeek;
            if (_mode == CardStackMode.Fan)
            {
                depth = Mathf.Abs(offsetFromFocus);
                axisOffset = offsetFromFocus * _fanSpacing;
                crossPeek = 0;

                if (_fanPileAtEdges)
                {
                    // keep the scaled card inside the viewport, cards past the edge pile up on top of each other there
                    var edgeOffset = Mathf.Max(0, (viewport.rect.size[_axis] - itemSize * ScaleAtDepth(depth)) / 2f);
                    axisOffset = Mathf.Clamp(axisOffset, -edgeOffset, edgeOffset);
                }
            }
            else if (offsetFromFocus > 0)
            {
                depth = Mathf.Min(offsetFromFocus, _maxDepth);
                axisOffset = 0;
                crossPeek = depth * _depthPeek;
            }
            else if (_mode == CardStackMode.TuckUnder)
            {
                // follow the drag out for the first half, then swing back to the center while sinking to the back of the stack
                var progress = -offsetFromFocus;
                depth = Mathf.Min(progress, 1f) * _maxDepth;
                axisOffset = -2f * _tuckSlideDistance * Mathf.Min(progress, Mathf.Max(0, 1f - progress)) * itemStep;
                crossPeek = depth * _depthPeek;
            }
            else
            {
                // the focused item and the ones before it keep the row layout, so they slide away with the drag
                itemRect.localScale = Vector3.one;
                return;
            }

            // move the card from its row position, which is offsetFromFocus item steps from the center, to its axis offset
            var position = itemRect.anchoredPosition;
            position[_axis] += (axisOffset - offsetFromFocus * itemStep) * (vertical ? -1 : 1);

            // scaling happens around the top left pivot, shift by half the lost size to keep the item centered,
            // then push it along the cross axis so its edge peeks out from under the item in front of it
            var scale = ScaleAtDepth(depth);
            var size = itemRect.rect.size;
            position += new Vector2(size.x, -size.y) * ((1f - scale) / 2f);
            position[1 - _axis] += crossPeek * (vertical ? 1 : -1);

            itemRect.localScale = new Vector3(scale, scale, 1f);
            itemRect.anchoredPosition = position;
        }

        // never below zero, since fanned cards keep getting smaller the further they are
        private float ScaleAtDepth(float depth)
        {
            return Mathf.Max(0, 1f - depth * _depthScaleStep);
        }

        /// <summary>
        /// The focused card is drawn on top, the cards further from it go underneath.
        /// When sliding away, earlier cards are drawn on top so they cover the stack as they leave,
        /// and a tucked card stays on top while it slides out, then goes under the whole stack
        /// </summary>
        protected override float GetSiblingOrder(int itemIndex)
        {
            var offsetFromFocus = GetOffsetFromFocus(itemIndex);
            if (_mode == CardStackMode.Fan)
            {
                return -Mathf.Abs(offsetFromFocus);
            }

            if (offsetFromFocus >= 0 || _mode == CardStackMode.SlideAway)
            {
                return -offsetFromFocus;
            }

            var progress = -offsetFromFocus;
            return progress < 0.5f ? progress : -(_maxDepth + progress);
        }
    }
}
