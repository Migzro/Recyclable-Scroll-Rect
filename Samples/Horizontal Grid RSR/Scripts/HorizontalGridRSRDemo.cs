// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class HorizontalGridRSRDemo : MonoBehaviour, IGridDataSource
    {
        [SerializeField] private int _itemsCount;
        [SerializeField] private RSRGrid _scrollRect;
        [SerializeField] private GameObject[] _prototypeItems;
        [SerializeField] private int _itemsToReloadTo;

        private List<string> _dataSource;

        public GameObject[] PrototypeItems => _prototypeItems;

        private void Start()
        {
            _dataSource = new List<string>();
            for (var i = 0; i < _itemsCount; i++)
                _dataSource.Add(i.ToString());
            _scrollRect.Initialize(this);
        }

        [ContextMenu(nameof(ReloadData))]
        public void ReloadData()
        {
            if (_itemsToReloadTo < _itemsCount)
            {
                _dataSource.RemoveRange(_itemsToReloadTo, _itemsCount - _itemsToReloadTo);
            }
            else
            {
                for (int i = _itemsCount; i < _itemsToReloadTo; i++)
                {
                    _dataSource.Add(i.ToString());
                }
            }

            _itemsCount = _itemsToReloadTo;
            _scrollRect.ReloadData(true);
        }
        
        public int GetItemsCount(int sectionIndex)
        {
            return _itemsCount;
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

        public float GetHeaderFooterSize(ItemData itemData)
        {
            return -1;
        }
    }
}