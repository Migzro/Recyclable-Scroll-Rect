using UnityEngine;

public static class RectTransformExtension
{
    public static CanvasGroup GetCanvasGroup(this GameObject gameObject)
    {
        var canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        return canvasGroup;
    }

    public static Rect GetWorldRect(this RectTransform rectTransform)
    {
        var corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Vector2 min = corners[0];
        Vector2 max = corners[2];
        Vector2 size = max - min;

        return new Rect(min, size);
    }

    public static bool FullyContains(this RectTransform rectTransform, RectTransform other)
    {
        var rect = rectTransform.GetWorldRect();
        var otherRect = other.GetWorldRect();

        return rect.xMin <= otherRect.xMin
               && rect.yMin <= otherRect.yMin
               && rect.xMax >= otherRect.xMax
               && rect.yMax >= otherRect.yMax;
    }

    public static bool AnyCornerVisible(this RectTransform rectTransform, RectTransform other)
    {
        var rect = rectTransform.GetWorldRect();
        var otherRect = other.GetWorldRect();
        return rect.Overlaps(otherRect, true);
    }
}