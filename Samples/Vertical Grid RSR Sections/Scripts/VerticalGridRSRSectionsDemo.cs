// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class VerticalGridRSRSectionsDemo : MonoBehaviour, IGridDataSource, ISectionsSource
    {
        [SerializeField] private int[] _itemsCount;
        [SerializeField] private RSRGrid _scrollRect;
        [SerializeField] private GameObject[] _prototypeItems;
        [SerializeField] private int _itemsToReloadTo;

        private List<List<string>> _dataSource;

        public GameObject[] PrototypeItems => _prototypeItems;
        public int SectionsCount => _itemsCount.Length;
        public bool ScrollRectHasHeader => false;
        public bool ScrollRectHasFooter => false;
        public bool ScrollRectHeaderIsPinned => false;

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

        public void SetItemData(IItem item, ItemData itemData)
        {
            Debug.Log(itemData);
            (item as DemoItemPrototype)?.Initialize(_dataSource[itemData.sectionIndex][itemData.itemIndex]);
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
    }
}