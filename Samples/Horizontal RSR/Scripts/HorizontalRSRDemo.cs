// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class HorizontalRSRDemo : MonoBehaviour, IRSRDataSource
    {
        [SerializeField] private int _itemsCount;
        [SerializeField] private RSR _scrollRect;
        [SerializeField] private GameObject[] _prototypeItems;

        private List<string> _dataSource;
        private int _itemCount;

        public int SectionsCount => 1;
        public bool IsItemSizeKnown => true;
        public GameObject[] PrototypeItems => _prototypeItems;

        private void Start()
        {
            _dataSource = new List<string>();
            for (var i = 0; i < _itemsCount; i++)
                _dataSource.Add(i.ToString());
            _scrollRect.Initialize(this);
        }
        
        public int GetItemsCountInSection(int sectionIndex)
        {
            return _itemsCount;
        }

        public bool SectionHasHeader(int sectionIndex)
        {
            return false;
        }
        
        public bool SectionHasFooter(int sectionIndex)
        {
            return false;
        }

        public bool HeaderIsPinned(int sectionIndex)
        {
            return false;
        }

        public float GetItemSize(int sectionIndex, int itemIndex)
        {
            return 60.28f;
        }

        public void SetItemData(IItem item, int sectionIndex, int itemIndex)
        {
            (item as DemoItemPrototype)?.Initialize(_dataSource[itemIndex]);
        }

        public void ItemHidden(IItem item, int sectionIndex, int itemIndex)
        {
        }

        public GameObject GetItemPrototype(int sectionIndex, int itemIndex, ItemType itemType)
        {
            if (itemIndex % 2 == 0)
                return _prototypeItems[0];
            return _prototypeItems[1];
        }

        public void ItemCreated(int sectionIndex, int itemIndex, IItem item, GameObject itemGo)
        {

        }

        public bool IsItemStatic(int sectionIndex, int itemIndex)
        {
            return false;
        }

        public void ScrolledToItem(IItem item, int sectionIndex, int itemIndex)
        {
        }

        public bool IgnoreContentPadding(int sectionIndex, int itemIndex)
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
    }
}