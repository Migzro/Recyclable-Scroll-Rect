namespace RecyclableSR
{
    public class RSR : RSRBase
    {
        /// <summary>
        /// Sets the indices of the items inside the content of the ScrollRect
        /// </summary>
        protected override void SetIndices()
        {
            base.SetIndices();
            
            foreach (var visibleItem in _visibleItems)
            {
                visibleItem.Value.transform.SetSiblingIndex(visibleItem.Key);
            }
        }

        public override void ScrollToTopRight()
        {
            base.ScrollToTopRight();
            StartCoroutine(ScrollToTargetNormalisedPosition((vertical ? 1 : 0) * (_reverseDirection ? 0 : 1)));
        }
    }
}