// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class VerticalRSRSectionsDemo : MonoBehaviour, IRSRDataSource
    {
        [SerializeField] private int[] _itemsCount;
        [SerializeField] private RSR _scrollRect;
        [SerializeField] private GameObject[] _prototypeItems;
        
        private List<List<string>> _dataSource;

        public int SectionsCount => 2;
        public bool IsItemSizeKnown => true;
        public GameObject[] PrototypeItems => _prototypeItems;

        private void Start()
        {
            _dataSource = new List<List<string>>();
            for (var i = 0; i < _itemsCount.Length; i++)
            {
                _dataSource.Add(new List<string>());
                for (var j = 0; j < _itemsCount[i]; j++)
                    _dataSource[i].Add(j.ToString());
            }

            _scrollRect.Initialize(this);
        }
        
        public int GetItemsCountInSection(int sectionIndex)
        {
            return _itemsCount[sectionIndex];
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

        public float GetItemSize(ItemData itemData)
        {
            if (itemData.itemType == ItemType.Item)
                return 40.22f;
            return 80f;
        }

        public void SetItemData(IItem item, ItemData itemData)
        {
            if (itemData.itemType == ItemType.Item)
            {
                (item as DemoItemPrototype)?.Initialize(_dataSource[itemData.sectionIndex][itemData.itemIndex]);
            }
        }

        public void ItemHidden(IItem item, ItemData itemData)
        {
        }

        public GameObject GetItemPrototype(ItemData itemData)
        {
            if (itemData.itemType == ItemType.Footer)
                return _prototypeItems[3];
            if (itemData.itemType == ItemType.Header)
                return _prototypeItems[2];
            if (itemData.sectionIndex == 0)
                return _prototypeItems[0];
            return _prototypeItems[1];
        }

        public void ItemCreated(IItem item, GameObject itemGo, ItemData itemData)
        {
        }

        public bool IsItemStatic(ItemData itemData)
        {
            return false;
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
    }
}