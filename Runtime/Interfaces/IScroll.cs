using System;
using System.Collections;
using UnityEngine.UI;

namespace RecyclableScrollRect
{
    public interface IScroll
    {
        public void ScrollToNormalizedPosition(RSRBase scrollRect, float targetNormalizedPos, float speed, bool isTime, bool instant, Action onFinished);
        public void ScrollToItem(int itemIndex, bool callEvent = true, bool instant = false, float maxSpeedMultiplier = 1, float offset = 0);
    }
}