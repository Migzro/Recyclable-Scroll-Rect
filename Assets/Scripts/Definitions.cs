using UnityEngine;

namespace RecyclableSR
{
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

        public ItemPosition()
        {
            absTopLeftPosition = Vector2.zero;
            absBottomRightPosition = Vector2.zero;
            itemSize = Vector2.zero;
            positionSet = false;
            sizeSet = false;
        }

        public void SetPosition(Vector2 position)
        {
            topLeftPosition = position;
            absTopLeftPosition = position.Abs();
            positionSet = true;

            if (sizeSet)
                absBottomRightPosition = absTopLeftPosition + itemSize;
        }
        
        public void SetPositionAndSize (Vector2 position, Vector2 size)
        {
            itemSize = size;
            absTopLeftPosition = position.Abs();
            absBottomRightPosition = absTopLeftPosition + itemSize;
            positionSet = true;
            sizeSet = true;
        }

        public void SetSize(Vector2 size)
        {
            itemSize = size;
            absBottomRightPosition = absTopLeftPosition + itemSize;
            sizeSet = true;
        }

        public override string ToString()
        {
            return $"Top Left Position {absTopLeftPosition}, Bottom Right Position {absBottomRightPosition}, Size {itemSize}";
        }
    }
}