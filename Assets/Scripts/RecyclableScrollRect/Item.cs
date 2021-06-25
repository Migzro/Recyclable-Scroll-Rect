using UnityEngine;

namespace RecyclableSR
{
    public class Item
    {
        public ICell cell { get; private set; }
        public RectTransform transform { get; private set; }

        public Item(ICell cell, RectTransform transform)
        {
            this.cell = cell;
            this.transform = transform;
        }
    }
}