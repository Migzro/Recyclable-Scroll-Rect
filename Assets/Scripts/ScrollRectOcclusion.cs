using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollItem
{
    public RectTransform rect;
    public bool visible;

    public ScrollItem(RectTransform rect, bool visible)
    {
        this.rect = rect;
        this.visible = visible;
    }
}

[AddComponentMenu("UI/Extensions/Scrollrect Occlusion")]
public class ScrollRectOcclusion : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    private VerticalLayoutGroup verticalLayoutGroup;
    private HorizontalLayoutGroup horizontalLayoutGroup;
    private LayoutElement layoutElement;

    private List<ScrollItem> items;
    private RectTransform rectTransform;
    private bool isVertical;
    private int minVisibleItem;
    private int maxVisibleItem;
    private Vector2 lastScrollPosition;

    private void Awake()
    {
        items = new List<ScrollItem>();
        rectTransform = gameObject.GetComponent<RectTransform>();

        layoutElement = gameObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();

        for (var i = 0; i < gameObject.transform.childCount; i++)
            items.Add(new ScrollItem(gameObject.transform.GetChild(i).GetComponent<RectTransform>(), false));

        verticalLayoutGroup = gameObject.GetComponent<VerticalLayoutGroup>();
        horizontalLayoutGroup = gameObject.GetComponent<HorizontalLayoutGroup>();
        if (verticalLayoutGroup == null && horizontalLayoutGroup)
        {
            Debug.LogError("No vertical lor horizontal layouts found in ScrollRect Occlusion");
            return;
        }

        isVertical = horizontalLayoutGroup == null;
        if (scrollRect == null)
            scrollRect = gameObject.GetComponent<ScrollRect>();
        scrollRect.onValueChanged.AddListener(OnScroll);
        lastScrollPosition = scrollRect.normalizedPosition;
    }

    private void UpdateLayouts()
    {
        if (isVertical)
            verticalLayoutGroup.enabled = true;
        else
            horizontalLayoutGroup.enabled = true;
        layoutElement.enabled = false;
        
        for (var i = 0; i < items.Count; i++)
            items[i].rect.gameObject.SetActive(true);
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        Canvas.ForceUpdateCanvases();

        layoutElement.enabled = true;
        layoutElement.preferredHeight = rectTransform.rect.height;
        if (isVertical)
            verticalLayoutGroup.enabled = false;
        else
            horizontalLayoutGroup.enabled = false;
        
        for (var i = 0; i < items.Count; i++)
            items[i].rect.gameObject.SetActive(items[i].visible);
    }

    private void OnDestroy()
    {
        scrollRect.onValueChanged.RemoveListener(OnScroll);
    }

    public void AddItems(List<GameObject> newItems, bool updateLayout = false)
    {
        foreach (var item in newItems)
            AddItem(item);

        if (updateLayout)
            UpdateLayouts();
    }

    public void AddItem(GameObject item, bool updateLayout = false)
    {
        scrollRect.vertical = false;
        
        if (updateLayout)
            UpdateLayouts();
        
        var newRect = item.GetComponent<RectTransform>();
        var isVisible = scrollRect.viewport.AnyCornerVisible(newRect);
        newRect.gameObject.SetActive(isVisible);
        items.Add(new ScrollItem(newRect, isVisible));
        if (isVisible)
        {
            var currentItemsCount = items.Count - 1;
            if (minVisibleItem > currentItemsCount)
                minVisibleItem = currentItemsCount;
            if (maxVisibleItem < currentItemsCount)
                maxVisibleItem = currentItemsCount;
        }
        lastScrollPosition = scrollRect.normalizedPosition;
        scrollRect.vertical = true;
    }

    private void OnScroll(Vector2 scrollPosition)
    {
        if (items.Count <= 0)
            return;

        var isDownOrRight = true;
        if (isVertical && lastScrollPosition.y < scrollPosition.y)
            isDownOrRight = false;
        else if (!isVertical && lastScrollPosition.x < scrollPosition.x)
            isDownOrRight = false;
        lastScrollPosition = scrollPosition;

        if (isDownOrRight)
        {
            var newMaxItemToCheck = Mathf.Min(items.Count - 1, maxVisibleItem + 1);
            if (items[minVisibleItem].visible && !scrollRect.viewport.AnyCornerVisible(items[minVisibleItem].rect))
            {
                items[minVisibleItem].rect.gameObject.SetActive(false);
                items[minVisibleItem].visible = false;
                minVisibleItem = Mathf.Min(items.Count - 1, minVisibleItem + 1);
            }
            
            if (!items[newMaxItemToCheck].visible && scrollRect.viewport.AnyCornerVisible(items[newMaxItemToCheck].rect))
            {
                items[newMaxItemToCheck].rect.gameObject.SetActive(true);
                items[newMaxItemToCheck].visible = true;
                maxVisibleItem = newMaxItemToCheck;
            }
        }
        else
        {
            var newMinItemToCheck = Mathf.Max(0, minVisibleItem - 1);
            if (!items[newMinItemToCheck].visible && scrollRect.viewport.AnyCornerVisible(items[newMinItemToCheck].rect))
            {
                items[newMinItemToCheck].rect.gameObject.SetActive(true);
                items[newMinItemToCheck].visible = true;
                minVisibleItem = newMinItemToCheck;
            }
            
            if (items[maxVisibleItem].visible && !scrollRect.viewport.AnyCornerVisible(items[maxVisibleItem].rect))
            {
                items[maxVisibleItem].rect.gameObject.SetActive(false);
                items[maxVisibleItem].visible = false;
                maxVisibleItem = Mathf.Max(0, maxVisibleItem - 1);
            }
        }
    }
}