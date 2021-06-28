using UnityEngine;

namespace RecyclableSR
{
    public class Item
    {
        public ICell cell { get; }
        public RectTransform transform { get; }

        public Item(ICell cell, RectTransform transform)
        {
            this.cell = cell;
            this.transform = transform;
        }
    }
}