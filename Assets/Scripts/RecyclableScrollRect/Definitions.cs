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
        public Vector2 topLeftPosition;
        public Vector2 bottomRightPosition;

        public void SetPositionAndSize (Vector2 position, Vector2 cellSize)
        {
            topLeftPosition = position.Abs();
            bottomRightPosition = topLeftPosition + cellSize;
        }

        public void SetSize(Vector2 cellSize)
        {
            bottomRightPosition = topLeftPosition + cellSize;
        }

        public override string ToString()
        {
            return $"Top Left Position {topLeftPosition}, Bottom Right Position {bottomRightPosition}, Size {bottomRightPosition - topLeftPosition}";
        }
    }
}