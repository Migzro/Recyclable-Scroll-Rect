// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class VerticalRSRSectionsDemo : MonoBehaviour, IRSRDataSource, ISectionsSource
    {
        [SerializeField] private int[] _itemsCount;
        [SerializeField] private RSR _scrollRect;
        [SerializeField] private GameObject[] _prototypeItems;
        [SerializeField] private int _itemsToReloadTo;
        
        private List<List<string>> _dataSource;

        public int SectionsCount => _itemsCount.Length;
        public bool ScrollRectHasHeader => true;
        public bool ScrollRectHasFooter => true;
        public bool ScrollRectHeaderIsPinned => true;
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
            if (sectionIndex == 0)
                return false;
            return true;
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
            if (itemData.itemType == ItemType.RSRHeader || itemData.itemType == ItemType.Header)
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