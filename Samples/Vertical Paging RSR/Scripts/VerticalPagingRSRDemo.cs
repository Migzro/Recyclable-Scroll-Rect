// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class VerticalPagingRSRDemo : MonoBehaviour, IPageDataSource
    {
        [SerializeField] private int _itemsCount;
        [SerializeField] private RSRPages _scrollRect;
        [SerializeField] private GameObject[] _prototypeItems;
        [SerializeField] private RectTransform _canvas;
        
        private List<string> _dataSource;
        private float _canvasHeight;

        public bool IsItemSizeKnown => true;
        public GameObject[] PrototypeItems => _prototypeItems;

        private void Start()
        {
            _canvasHeight = _canvas.rect.height;
            _dataSource = new List<string>();
            for (var i = 0; i < _itemsCount; i++)
                _dataSource.Add(i.ToString());
            _scrollRect.Initialize(this);
        }
        
        public int GetItemsCount(int sectionIndex)
        {
            return _itemsCount;
        }

        public float GetItemSize(ItemData itemData)
        {
            return _canvasHeight;
        }

        public void SetItemData(IItem item, ItemData itemData)
        {
            (item as DemoItemPrototype)?.Initialize(_dataSource[itemData.itemIndex]);
        }

        public void ItemHidden(IItem item, ItemData itemData)
        {
        }

        public GameObject GetItemPrototype(ItemData itemData)
        {
            if (itemData.itemIndex % 2 == 0)
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
            return false;
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

        public void PageWillFocus(IItem item, ItemData itemData, bool isNextPage)
        {
        }

        public void PageWillUnFocus(IItem item, ItemData itemData, bool isNextPage)
        {
        }
    }
}