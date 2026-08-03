// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class VerticalPagingRSRSectionsDemo : MonoBehaviour, IPageDataSource, ISectionsSource
    {
        [SerializeField] private int[] _itemsCount;
        [SerializeField] private RSRPages _scrollRect;
        [SerializeField] private GameObject[] _prototypeItems;
        [SerializeField] private RectTransform _canvas;
        [SerializeField] private int _itemsToReloadTo;
        [SerializeField] private float _headerHeight = 50;

        private List<List<string>> _dataSource;
        private float _canvasHeight;

        public GameObject[] PrototypeItems => _prototypeItems;
        public int SectionsCount => _itemsCount.Length;
        public bool ScrollRectHasHeader => true;
        public bool ScrollRectHasFooter => true;
        public bool ScrollRectHeaderIsPinned => true;
        public bool IsItemSizeKnown => true;

        private void Start()
        {
            _dataSource = new List<List<string>>();
            for (var i = 0; i < _itemsCount.Length; i++)
            {
                _dataSource.Add(new List<string>());
                for (var j = 0; j < _itemsCount[i]; j++)
                    _dataSource[i].Add(j.ToString());
            }
            _canvasHeight = _canvas.rect.height;
            _scrollRect.Initialize(this);
        }
        
        [ContextMenu(nameof(ReloadData))]
        public void ReloadData()
        {
            _dataSource = new List<List<string>>();
            for (var i = 0; i < _itemsCount.Length; i++)
            {
                _dataSource.Add(new List<string>());
                for (var j = 0; j < _itemsCount[i]; j++)
                    _dataSource[i].Add(j.ToString());
            }
            _scrollRect.ReloadData(true);
        }
        
        public int GetItemsCount(int sectionIndex)
        {
            return _itemsCount[sectionIndex];
        }

        public float GetItemSize(ItemData itemData)
        {
            if (itemData.itemType != ItemType.Item)
                return _headerHeight;

            var itemSize = _canvasHeight;
            var sectionIndex = itemData.sectionIndex;
            var itemIndex = itemData.itemIndex;
            var isLastSectionItem =
                itemIndex == _itemsCount[sectionIndex] - 1;

            if (ScrollRectHasHeader && ScrollRectHeaderIsPinned)
            {
                itemSize -= _headerHeight;
            }

            if (SectionHasHeader(sectionIndex) && HeaderIsPinned(sectionIndex))
            {
                itemSize -= _headerHeight;
            }

            if (isLastSectionItem && SectionHasFooter(sectionIndex))
            {
                itemSize -= _headerHeight;
            }

            if (isLastSectionItem && sectionIndex == SectionsCount - 1 && ScrollRectHasFooter)
            {
                itemSize -= _headerHeight;
            }

            return itemSize;
        }

        public void SetItemData(IItem item, ItemData itemData)
        {
            if (itemData.itemType == ItemType.Item)
            {
                (item as DemoItemPrototype)?.Initialize(_dataSource[itemData.sectionIndex][itemData.itemIndex]);
            }
            else if (itemData.itemType == ItemType.RSRHeader)
            {
                (item as DemoItemPrototype)?.Initialize("RSR Header");
            }
            else if (itemData.itemType == ItemType.Header)
            {
                (item as DemoItemPrototype)?.Initialize("Header " + itemData.sectionIndex);
            }
            else if (itemData.itemType == ItemType.RSRFooter)
            {
                (item as DemoItemPrototype)?.Initialize("RSR Footer");
            }
            else if (itemData.itemType == ItemType.Footer)
            {
                (item as DemoItemPrototype)?.Initialize("Footer " + itemData.sectionIndex);
            }
        }

        public void ItemHidden(IItem item, ItemData itemData)
        {
        }

        public GameObject GetItemPrototype(ItemData itemData)
        {
            if (itemData.itemType == ItemType.RSRFooter || itemData.itemType == ItemType.Footer)
                return _prototypeItems[3];
            if (itemData.itemType == ItemType.Header || itemData.itemType == ItemType.RSRHeader)
                return _prototypeItems[2];
            if (itemData.sectionIndex == 0)
                return _prototypeItems[0];
            return _prototypeItems[1];
        }

        public void ItemCreated(IItem item, GameObject itemGo, ItemData itemData)
        {

        }

        public void ScrolledToItem(IItem item, ItemData itemData)
        {
        }

        public bool IgnoreContentPadding(ItemData itemData)
        {
            return true;
        }

        public void PullToRefresh()
        {
        }

        public void PushToClose()
        {
        }

        public void ReachedScrollStart()
        {
        }

        public void ReachedScrollEnd()
        {
        }

        public void LastItemIsVisible()
        {
        }

        public bool SectionHasHeader(int sectionIndex)
        {
            return true;
        }

        public bool SectionHasFooter(int sectionIndex)
        {
            return true;
        }

        public bool HeaderIsPinned(int sectionIndex)
        {
            return false;
        }

        public void PageWillFocus(IItem item, ItemData itemData, bool isNextPage)
        {
        }

        public void PageWillUnFocus(IItem item, ItemData itemData, bool isNextPage)
        {
        }
    }
}