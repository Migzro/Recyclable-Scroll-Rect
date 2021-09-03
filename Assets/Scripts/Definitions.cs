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
        public Vector2 bottomRightPosition{ get; private set; }
        public Vector2 cellSize{ get; private set; }
        public bool positionSet { get; private set; }
        public bool sizeSet { get; private set; }

        public ItemPosition()
        {
            topLeftPosition = Vector2.zero;
            bottomRightPosition = Vector2.zero;
            cellSize = Vector2.zero;
            positionSet = false;
            sizeSet = false;
        }

        public void SetPosition(Vector2 position)
        {
            topLeftPosition = position.Abs();
            positionSet = true;

            if (sizeSet)
                bottomRightPosition = topLeftPosition + cellSize;
        }
        
        public void SetPositionAndSize (Vector2 position, Vector2 size)
        {
            topLeftPosition = position.Abs();
            cellSize = size;
            bottomRightPosition = topLeftPosition + cellSize;
            positionSet = true;
            sizeSet = true;
        }

        public void SetSize(Vector2 size)
        {
            cellSize = size;
            bottomRightPosition = topLeftPosition + cellSize;
            bottomRightPosition = topLeftPosition + size;
            sizeSet = true;
        }

        public override string ToString()
        {
            return $"Top Left Position {topLeftPosition}, Bottom Right Position {bottomRightPosition}, Size {cellSize}";
        }
    }
}