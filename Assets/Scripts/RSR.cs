namespace RecyclableSR
{
    public class RSR : RSRBase
    {
        public override void ScrollToTopRight()
        {
            base.ScrollToTopRight();
            StartCoroutine(ScrollToTargetNormalisedPosition((vertical ? 1 : 0) * (_reverseDirection ? 0 : 1)));
        }
    }
}