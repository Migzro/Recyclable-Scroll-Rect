using UnityEngine;

namespace RecyclableSR
{
    public readonly struct Item
    {
        public ICell cell { get; }
        public RectTransform transform { get; }

        public Item(ICell cell, RectTransform transform)
        {
            this.cell = cell;
            this.transform = transform;
        }
    }

    public class ItemPosition
    {
        public Vector2 topLeftPosition{ get; private set; }
        public Vector2 absTopLeftPosition{ get; private set; }
        public Vector2 absBottomRightPosition{ get; private set; }
        public Vector2 cellSize{ get; private set; }
        public bool positionSet { get; private set; }
        public bool sizeSet { get; private set; }

        public ItemPosition()
        {
            absTopLeftPosition = Vector2.zero;
            absBottomRightPosition = Vector2.zero;
            cellSize = Vector2.zero;
            positionSet = false;
            sizeSet = false;
        }

        public void SetPosition(Vector2 position)
        {
            topLeftPosition = position;
            absTopLeftPosition = position.Abs();
            positionSet = true;

            if (sizeSet)
                absBottomRightPosition = absTopLeftPosition + cellSize;
        }
        
        public void SetPositionAndSize (Vector2 position, Vector2 size)
        {
            cellSize = size;
            absTopLeftPosition = position.Abs();
            absBottomRightPosition = absTopLeftPosition + cellSize;
            positionSet = true;
            sizeSet = true;
        }

        public void SetSize(Vector2 size)
        {
            cellSize = size;
            absBottomRightPosition = absTopLeftPosition + cellSize;
            sizeSet = true;
        }

        public override string ToString()
        {
            return $"Top Left Position {absTopLeftPosition}, Bottom Right Position {absBottomRightPosition}, Size {cellSize}";
        }
    }
}