using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace RecyclableSR
{
    public class RecyclableScrollRect : ScrollRect
    {
        // todo: test with dynamic item sizes
        // todo: profile and check which calls are taking the longest time
        // todo: maybe dont set items x in vertical everytime we show a cell or item y in horizontal unless reloaded
        // todo: ExecuteInEditMode?
        // todo: add a scrollto method?
        // todo: Simulate content size (O.o)^(o.O)

        [SerializeField] private IDataSourceContainer _dataSourceContainer;
        [SerializeField] private GameObject _prototypeCell;
        [SerializeField] private int _extraItemsVisible;
        [SerializeField] private bool _initOnStart;

        private VerticalLayoutGroup _verticalLayoutGroup;
        private HorizontalLayoutGroup _horizontalLayoutGroup;
        private ContentSizeFitter _contentSizeFitter;
        private LayoutElement _layoutElement;

        private IDataSource _dataSource;
        private Vector2 _viewPortSize;
        private Vector2 _contentSize;
        private float[] _cellSizes;
        private int _axis;
        private int _minimumItemsInViewPort;
        private int _itemsCount;
        private int _minVisibleItemInViewPort;
        private int _maxVisibleItemInViewPort;
        private bool _hasLayoutGroup;
        private bool _init;

        private Dictionary<int, Item> _visibleItems;
        private List<Item> _invisibleItems;
        private Vector2 _lastScrollPosition;
        private RectOffset _padding;
        private TextAnchor _alignment;
        private float _spacing;

        protected override void Start()
        {
            base.Start();
            if (_initOnStart)
                Initialize();
        }

        public void Initialize()
        {
            _dataSource = _dataSourceContainer.DataSource;

            if (vertical)
            {
                _verticalLayoutGroup = content.gameObject.GetComponent<VerticalLayoutGroup>();
                if (_verticalLayoutGroup != null)
                {
                    _hasLayoutGroup = true;
                    _padding = _verticalLayoutGroup.padding;
                    _spacing = _verticalLayoutGroup.spacing;
                    _alignment = _verticalLayoutGroup.childAlignment;
                }
            }
            else
            {
                _horizontalLayoutGroup = content.gameObject.GetComponent<HorizontalLayoutGroup>();
                if (_horizontalLayoutGroup != null)
                {
                    _hasLayoutGroup = true;
                    _padding = _horizontalLayoutGroup.padding;
                    _spacing = _horizontalLayoutGroup.spacing;
                    _alignment = _horizontalLayoutGroup.childAlignment;
                }
            }

            if (_hasLayoutGroup)
            {
                _contentSizeFitter = content.gameObject.GetComponent<ContentSizeFitter>();
                _layoutElement = content.gameObject.GetComponent<LayoutElement>();
                if (_layoutElement == null)
                    _layoutElement = content.gameObject.AddComponent<LayoutElement>();
            }

            _visibleItems = new Dictionary<int, Item>();
            _invisibleItems = new List<Item>();
            _viewPortSize = viewport.rect.size;
            _init = true;
            _lastScrollPosition = normalizedPosition;
            _axis = vertical ? 1 : 0;
            ReloadData();
        }

        public void ReloadData()
        {
            _itemsCount = _dataSource.GetItemCount();
            DisableContentLayouts();
            CalculateLayoutSize();
            CalculateMinimumItemsInViewPort();
            InitializeCells();
        }

        private void DisableContentLayouts()
        {
            if (_hasLayoutGroup)
            {
                if (_horizontalLayoutGroup != null)
                    _horizontalLayoutGroup.enabled = false;

                if (_verticalLayoutGroup != null)
                    _verticalLayoutGroup.enabled = false;

                _contentSizeFitter.enabled = false;
            }
        }

        private void CalculateLayoutSize()
        {
            var contentSizeDelta = viewport.sizeDelta;
            if (vertical)
                contentSizeDelta.y = 0;
            else
                contentSizeDelta.x = 0;

            _cellSizes = new float[_itemsCount];
            for (var i = 0; i < _itemsCount; i++)
            {
                var cellSize = _dataSource.GetCellSize(i);
                _cellSizes[i] = cellSize;

                if (vertical)
                    contentSizeDelta.y += cellSize;
                else
                    contentSizeDelta.x += cellSize;
            }

            if (_hasLayoutGroup)
            {
                if (vertical && _verticalLayoutGroup != null)
                {
                    contentSizeDelta.y += _spacing * (_itemsCount - 1);
                    contentSizeDelta.y += _padding.top + _padding.bottom;
                }
                else if (!vertical && _horizontalLayoutGroup != null)
                {
                    contentSizeDelta.x += _spacing * (_itemsCount - 1);
                    contentSizeDelta.y += _padding.right + _padding.left;
                }

                if (vertical)
                    _layoutElement.preferredHeight = contentSizeDelta.y;
                else
                    _layoutElement.preferredWidth = contentSizeDelta.x;
            }

            content.sizeDelta = contentSizeDelta;
            _contentSize = content.rect.size;
        }

        private void CalculateMinimumItemsInViewPort()
        {
            _minimumItemsInViewPort = 0;
            var rect = viewport.rect;
            var remainingViewPortSize = vertical ? rect.height : rect.width;

            for (var i = 0; i < _itemsCount; i++)
            {
                var spacing = _spacing;
                if (i == _itemsCount - 1)
                    spacing = 0;

                remainingViewPortSize -= _cellSizes[i] + spacing;
                _minimumItemsInViewPort++;

                if (remainingViewPortSize <= 0)
                    break;
            }

            _minVisibleItemInViewPort = 0;
            _maxVisibleItemInViewPort = _minimumItemsInViewPort - 1;
        }

        private void InitializeCells()
        {
            var itemsToShow = Mathf.Min(_itemsCount, _minimumItemsInViewPort + _extraItemsVisible);
            for (var i = 0; i < itemsToShow; i++)
            {
                InitializeCell(i);
            }
        }

        private void InitializeCell(int index)
        {
            var itemGo = Instantiate(_prototypeCell, content, false);
            itemGo.name = index.ToString();

            var cell = itemGo.GetComponent<ICell>();
            var rect = itemGo.GetComponent<RectTransform>();

            var item = new Item(cell, rect);
            _visibleItems.Add(index, item);
            _dataSource.SetCellData(cell, index);

            SetCellSizePosition(rect, index, index - 1);
        }

        private void SetCellSizePosition(RectTransform rect, int newIndex, int prevIndex)
        {
            // set position
            // all items are anchored to top left
            var anchorVector = new Vector2(0, 1);
            rect.anchorMin = anchorVector;
            rect.anchorMax = anchorVector;

            // set size
            if (_hasLayoutGroup)
            {
                // expand item width if its in a vertical layout group and the conditions are satisfied
                if (vertical && _verticalLayoutGroup.childControlWidth && _verticalLayoutGroup.childForceExpandWidth)
                {
                    var itemSize = rect.sizeDelta;
                    itemSize.x = content.rect.width - _verticalLayoutGroup.padding.right - _verticalLayoutGroup.padding.left;
                    rect.sizeDelta = itemSize;
                }

                // expand item height if its in a horizontal layout group and the conditions are satisfied
                else if (!vertical && _horizontalLayoutGroup.childControlHeight && _horizontalLayoutGroup.childControlHeight)
                {
                    var itemSize = rect.sizeDelta;
                    itemSize.y = content.rect.height - _horizontalLayoutGroup.padding.top - _horizontalLayoutGroup.padding.bottom;
                    rect.sizeDelta = itemSize;
                }
            }

            // get content size without padding
            var contentSizeWithPadding = _contentSize;
            contentSizeWithPadding.x -= _padding.right - _padding.left;
            contentSizeWithPadding.y -= _padding.top - _padding.bottom;

            // figure out where the prev cell position was
            var prevItemPosition = Vector2.zero;
            if (newIndex == 0)
            {
                prevItemPosition.y -= _padding.top;
                prevItemPosition.x += _padding.left;
            }

            if (prevIndex > -1 && prevIndex < _itemsCount - 1)
            {
                var isAfter = newIndex > prevIndex;
                prevItemPosition = _visibleItems[prevIndex].transform.anchoredPosition;
                var sign = isAfter ? -1 : 1;
                if (vertical)
                    prevItemPosition.y += (_visibleItems[prevIndex].transform.rect.height * sign) + (_spacing * sign);
                else
                    prevItemPosition.x -= (_visibleItems[prevIndex].transform.rect.width * sign) + (_spacing * sign);
            }

            // set position of cell based on layout alignment
            // we check for multiple conditions together since the content is made to fit the items, so they only move in one axis in each different scroll direction
            var rectSize = rect.rect.size;
            var itemSizeSmallerThanContent =
                vertical && rectSize.x < contentSizeWithPadding.x || !vertical && rectSize.y < contentSizeWithPadding.y;
            if (_hasLayoutGroup && itemSizeSmallerThanContent)
            {
                if (vertical)
                {
                    if (_alignment == TextAnchor.LowerCenter || _alignment == TextAnchor.MiddleCenter || _alignment == TextAnchor.UpperCenter)
                    {
                        prevItemPosition.x = (_padding.left + (_contentSize.x - rectSize.x) - _padding.right) / 2f;
                    }
                    else if (_alignment == TextAnchor.LowerRight || _alignment == TextAnchor.MiddleRight || _alignment == TextAnchor.UpperRight)
                    {
                        prevItemPosition.x = _contentSize.x - rectSize.x - _padding.right;
                    }
                    else
                    {
                        prevItemPosition.x = _padding.left;
                    }
                }
                else
                {
                    if (_alignment == TextAnchor.MiddleLeft || _alignment == TextAnchor.MiddleCenter || _alignment == TextAnchor.MiddleRight)
                    {
                        prevItemPosition.y = -(_padding.top + (_contentSize.y - rectSize.y) - _padding.bottom) / 2f;
                    }
                    else if (_alignment == TextAnchor.LowerLeft || _alignment == TextAnchor.LowerCenter || _alignment == TextAnchor.LowerRight)
                    {
                        prevItemPosition.y = -(_contentSize.y - rectSize.y - _padding.bottom);
                    }
                    else
                    {
                        prevItemPosition.y = -_padding.top;
                    }
                }
            }

            rect.anchoredPosition = prevItemPosition;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            var timePerUpdateLoop = new Stopwatch();
            timePerUpdateLoop.Start();
            
            if (!_init)
                return;
            if (normalizedPosition == _lastScrollPosition)
                return;
            if (_visibleItems.Count <= 0)
                return;

            // check the direction of the scrolling
            var isDownOrRight = true;
            if (vertical && _lastScrollPosition.y < normalizedPosition.y)
                isDownOrRight = false;
            else if (!vertical && _lastScrollPosition.x > normalizedPosition.x)
                isDownOrRight = false;
            _lastScrollPosition = normalizedPosition;

            var contentTopLeftCorner = content.localPosition.Abs();
            var contentBottomRightCorner = new Vector2(contentTopLeftCorner.x + _viewPortSize.x, contentTopLeftCorner.y + _viewPortSize.y).Abs();

            if (isDownOrRight && _maxVisibleItemInViewPort < _itemsCount - 1)
            {
                // item at top or left is not in viewport
                if (_visibleItems[_minVisibleItemInViewPort].transform.gameObject.activeSelf && !viewport.AnyCornerVisible(_visibleItems[_minVisibleItemInViewPort].transform))
                {
                    var itemToHide = _minVisibleItemInViewPort - _extraItemsVisible;
                    if (itemToHide > -1)
                    {
                        _visibleItems[itemToHide].transform.gameObject.SetActive(false);
                        SetVisibilityInHierarchy(_visibleItems[itemToHide].transform, false);
                        _invisibleItems.Add(_visibleItems[itemToHide]);
                        _visibleItems.Remove(itemToHide);
                    }

                    _minVisibleItemInViewPort++;
                }

                // item at bottom or right needs to appear
                var maxItemBottomRightCorner = _visibleItems[_maxVisibleItemInViewPort].transform.anchoredPosition.Abs() + _visibleItems[_maxVisibleItemInViewPort].transform.rect.size;
                if (vertical && contentBottomRightCorner.y > maxItemBottomRightCorner.y || !vertical && contentBottomRightCorner.x > maxItemBottomRightCorner.x)
                {
                    var newMaxItemToCheck = _maxVisibleItemInViewPort + 1;
                    var itemToShow = newMaxItemToCheck + _extraItemsVisible;
                    if (itemToShow < _itemsCount)
                        ShowCellAtIndex(itemToShow, itemToShow - 1);
                    _maxVisibleItemInViewPort = newMaxItemToCheck;
                }
            }
            else if (!isDownOrRight && _minVisibleItemInViewPort > 0)
            {
                // item at bottom or right not in viewport
                if (_visibleItems[_maxVisibleItemInViewPort].transform.gameObject.activeSelf && !viewport.AnyCornerVisible(_visibleItems[_maxVisibleItemInViewPort].transform))
                {
                    var itemToHide = _maxVisibleItemInViewPort + _extraItemsVisible;
                    if (itemToHide < _itemsCount)
                    {
                        _visibleItems[itemToHide].transform.gameObject.SetActive(false);
                        SetVisibilityInHierarchy(_visibleItems[itemToHide].transform, false);
                        _invisibleItems.Add(_visibleItems[itemToHide]);
                        _visibleItems.Remove(itemToHide);
                    }

                    _maxVisibleItemInViewPort--;
                }

                // item at top or left needs to appear
                var minItemBottomRightCorner = _visibleItems[_minVisibleItemInViewPort].transform.anchoredPosition.Abs();
                if (vertical && contentTopLeftCorner.y < minItemBottomRightCorner.y || !vertical && contentTopLeftCorner.x < minItemBottomRightCorner.x)
                {
                    var newMinItemToCheck = _minVisibleItemInViewPort - 1;
                    var itemToShow = newMinItemToCheck - _extraItemsVisible;
                    if (itemToShow > -1)
                        ShowCellAtIndex(itemToShow, itemToShow + 1);
                    _minVisibleItemInViewPort = newMinItemToCheck;
                }
            }
            timePerUpdateLoop.Stop();
            // Debug.Log($"Time per update loop: {timePerUpdateLoop.ElapsedTicks}");
        }

        private void ShowCellAtIndex(int newIndex, int prevIndex)
        {
            // Get empty cell and adjust its position and size, else just create a new a cell
            var isAfter = newIndex > prevIndex;
            if (_invisibleItems.Count > 0)
            {
                var item = _invisibleItems[0];
                _invisibleItems.RemoveAt(0);

                if (isAfter)
                    item.transform.SetAsLastSibling();
                else
                    item.transform.SetAsFirstSibling();

                item.transform.name = newIndex.ToString();
                item.transform.gameObject.SetActive(true);
                SetVisibilityInHierarchy(item.transform, true);

                _dataSource.SetCellData(item.cell, newIndex);
                _visibleItems.Add(newIndex, item);

                SetCellSizePosition(item.transform, newIndex, prevIndex);
            }
            else
            {
                InitializeCell(newIndex);
            }
        }

        private void SetVisibilityInHierarchy(RectTransform item, bool visible)
        {
#if UNITY_EDITOR
            item.transform.hideFlags = visible ? HideFlags.None : HideFlags.HideInHierarchy;
#endif
        }
    }
}