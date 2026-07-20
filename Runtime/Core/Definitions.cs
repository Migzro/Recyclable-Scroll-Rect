// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclableScrollRect
{
    public enum ItemsAlignment
    {
        LeftOrUp = 0,
        Center,
        RightOrDown
    }

    public enum ItemType
    {
        RSRHeader = 0,
        RSRFooter,
        Header,
        Item,
        Footer
    }

    public class ItemData
    {
        public ItemType itemType;
        public int sectionIndex;
        public int itemIndex;
        internal int actualItemIndex;

        public override string ToString()
        {
            return $"ItemData: Type {itemType}, Section {sectionIndex}, Item {itemIndex}, Actual Item Index {actualItemIndex}";
        }
    }
    
    public readonly struct Item
    {
        public IItem item { get; }
        public RectTransform transform { get; }

        public Item(IItem item, RectTransform transform)
        {
            this.item = item;
            this.transform = transform;
        }
    }

    public class ItemPosition
    {
        public Vector2 topLeftPosition{ get; private set; }
        public Vector2 absTopLeftPosition{ get; private set; }
        public Vector2 absBottomRightPosition{ get; private set; }
        public Vector2 itemSize{ get; private set; }
        public bool positionSet { get; private set; }
        public bool sizeSet { get; private set; }
        public bool nonAxisSizeSet { get; private set; }

        public ItemPosition()
        {
            topLeftPosition = Vector2.zero;
            absTopLeftPosition = Vector2.zero;
            absBottomRightPosition = Vector2.zero;
            itemSize = Vector2.zero;
            ResetAllFlags();
        }

        public void SetPosition(Vector2 position)
        {
            topLeftPosition = position;
            absTopLeftPosition = position.Abs();
            positionSet = true;

            if (sizeSet)
            {
                absBottomRightPosition = absTopLeftPosition + itemSize;
            }
        }
        
        public void SetNonAxisSize(Vector2 size)
        {
            itemSize = size;
            nonAxisSizeSet = true;
        }

        public void SetSize(Vector2 size)
        {
            itemSize = size;
            sizeSet = true;
        }

        public void ResetAllFlags()
        {
            positionSet = false;
            sizeSet = false;
            nonAxisSizeSet = false;
        }

        public void ResetPositionFlag()
        {
            positionSet = false;
        }

        public override string ToString()
        {
            return $"Top Left Position {absTopLeftPosition}, Bottom Right Position {absBottomRightPosition}, Size {itemSize}";
        }
    }

    public sealed class Grid
    {
        public readonly struct Section
        {
            public readonly int sectionIndex;
            public readonly int itemCount;
            public readonly bool hasHeader;
            public readonly bool hasFooter;

            public Section(int sectionIndex, int itemCount, bool hasHeader, bool hasFooter)
            {
                this.sectionIndex = sectionIndex;
                this.itemCount = Mathf.Max(0, itemCount);
                this.hasHeader = hasHeader;
                this.hasFooter = hasFooter;
            }
        }

        public readonly struct Slot
        {
            public readonly ItemType itemType;
            public readonly int sectionIndex;
            public readonly int itemIndex;
            public readonly int crossAxisSpan;
            public readonly bool isEmpty;

            public Slot(ItemType itemType, int sectionIndex, int itemIndex, int crossAxisSpan, bool isEmpty)
            {
                this.itemType = itemType;
                this.sectionIndex = sectionIndex;
                this.itemIndex = itemIndex;
                this.crossAxisSpan = crossAxisSpan;
                this.isEmpty = isEmpty;
            }

            public static Slot Empty(int sectionIndex)
            {
                return new Slot(ItemType.Item, sectionIndex, -1, 1, true);
            }
        }

        private readonly Slot[] _slots;
        private readonly Vector2Int[] _positions;
        private readonly int _constraintCount;
        private readonly bool _vertical;
        private readonly GridLayoutGroup.Axis _startAxis;
        private readonly GridLayoutGroup.Corner _startCorner;

        public int width { get; }
        public int height { get; }
        public int maxGridItemsInAxis { get; }
        public int SlotsCount => _slots.Length;
        public int LastVisibleFlatIndex { get; private set; } = -1;

        public Grid(IReadOnlyList<Section> sections, int gridConstraintCount, bool vertical, GridLayoutGroup.Axis gridStartAxis, GridLayoutGroup.Corner gridStartCorner, bool hasScrollRectHeader, bool hasScrollRectFooter)
        {
            if (sections == null)
            {
                throw new System.ArgumentNullException(nameof(sections));
            }
            if (gridConstraintCount <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(gridConstraintCount));
            }

            _constraintCount = gridConstraintCount;
            _vertical = vertical;
            _startAxis = gridStartAxis;
            _startCorner = gridStartCorner;
            maxGridItemsInAxis = CalculateLineCount(sections, hasScrollRectHeader, hasScrollRectFooter);
            width = vertical ? gridConstraintCount : maxGridItemsInAxis;
            height = vertical ? maxGridItemsInAxis : gridConstraintCount;
            _slots = new Slot[maxGridItemsInAxis * gridConstraintCount];
            _positions = new Vector2Int[_slots.Length];

            BuildSlots(sections, hasScrollRectHeader, hasScrollRectFooter);
        }

        public Slot GetSlot(int flatIndex)
        {
            return flatIndex < 0 || flatIndex >= _slots.Length ? Slot.Empty(-1) : _slots[flatIndex];
        }

        public Vector2Int To2dIndex(int flatIndex)
        {
            return flatIndex < 0 || flatIndex >= _positions.Length ? Vector2Int.zero : _positions[flatIndex];
        }

        private int CalculateLineCount(IReadOnlyList<Section> sections, bool hasScrollRectHeader, bool hasScrollRectFooter)
        {
            var lines = hasScrollRectHeader ? 1 : 0;
            for (var i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                lines += section.hasHeader ? 1 : 0;
                lines += Mathf.CeilToInt(section.itemCount / (float)_constraintCount);
                lines += section.hasFooter ? 1 : 0;
            }
            return lines + (hasScrollRectFooter ? 1 : 0);
        }

        private void BuildSlots(IReadOnlyList<Section> sections, bool hasScrollRectHeader, bool hasScrollRectFooter)
        {
            var line = 0;
            if (hasScrollRectHeader)
                AddFullLine(line++, ItemType.RSRHeader, 0);

            for (var i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                if (section.hasHeader)
                    AddFullLine(line++, ItemType.Header, section.sectionIndex);

                line = AddSectionItems(line, section);

                if (section.hasFooter)
                    AddFullLine(line++, ItemType.Footer, section.sectionIndex);
            }

            if (hasScrollRectFooter)
                AddFullLine(line, ItemType.RSRFooter, sections.Count - 1);
        }

        private void AddFullLine(int line, ItemType itemType, int sectionIndex)
        {
            AddSlot(line, 0, new Slot(itemType, sectionIndex, -1, _constraintCount, false));
            for (var crossAxisIndex = 1; crossAxisIndex < _constraintCount; crossAxisIndex++)
                AddSlot(line, crossAxisIndex, Slot.Empty(sectionIndex));
        }

        private int AddSectionItems(int firstLine, Section section)
        {
            var lineCount = Mathf.CeilToInt(section.itemCount / (float)_constraintCount);

            for (var lineOffset = 0; lineOffset < lineCount; lineOffset++)
            {
                for (var crossAxisIndex = 0; crossAxisIndex < _constraintCount; crossAxisIndex++)
                {
                    var itemIndex = GetItemIndex(lineOffset, crossAxisIndex, lineCount);
                    var slot = itemIndex < section.itemCount
                        ? new Slot(ItemType.Item, section.sectionIndex, itemIndex, 1, false)
                        : Slot.Empty(section.sectionIndex);
                    AddSlot(firstLine + lineOffset, crossAxisIndex, slot);
                }
            }

            return firstLine + lineCount;
        }

        private int GetItemIndex(int lineOffset, int crossAxisIndex, int lineCount)
        {
            var localWidth = _vertical ? _constraintCount : lineCount;
            var localHeight = _vertical ? lineCount : _constraintCount;
            var x = _vertical ? crossAxisIndex : lineOffset;
            var y = _vertical ? lineOffset : crossAxisIndex;

            var fromRight = _startCorner == GridLayoutGroup.Corner.UpperRight ||
                            _startCorner == GridLayoutGroup.Corner.LowerRight;
            var fromBottom = _startCorner == GridLayoutGroup.Corner.LowerLeft ||
                             _startCorner == GridLayoutGroup.Corner.LowerRight;

            if (_startAxis == GridLayoutGroup.Axis.Vertical)
            {
                var column = fromRight ? localWidth - 1 - x : x;
                var row = fromBottom ? localHeight - 1 - y : y;
                return (column * localHeight) + row;
            }

            var horizontalRow = fromBottom ? localHeight - 1 - y : y;
            var horizontalColumn = fromRight ? localWidth - 1 - x : x;
            return (horizontalRow * localWidth) + horizontalColumn;
        }

        private void AddSlot(int line, int crossAxisIndex, Slot slot)
        {
            var flatIndex = line * _constraintCount + crossAxisIndex;
            _slots[flatIndex] = slot;
            _positions[flatIndex] = GetPosition(line, crossAxisIndex);
            if (!slot.isEmpty)
                LastVisibleFlatIndex = flatIndex;
        }

        private Vector2Int GetPosition(int line, int crossAxisIndex)
        {
            var x = _vertical ? crossAxisIndex : line;
            var y = _vertical ? line : crossAxisIndex;
            return new Vector2Int(x, y);
        }
    }
    
    public enum AnimationState
    {
        Idle,
        Animating,
        Finished,
        Canceled,
    }
    
    public class PinnedHeaderState
    {
        public int index = -1;
        public bool IsPinned => index != -1;
        public ItemPosition position;

        public PinnedHeaderState()
        {
            position = new();
        }
    }
}
