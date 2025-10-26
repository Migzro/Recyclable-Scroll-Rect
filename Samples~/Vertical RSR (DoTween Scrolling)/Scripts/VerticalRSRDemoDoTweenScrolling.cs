// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class VerticalRSRDemoDoTweenScrolling : MonoBehaviour, IDataSource
    {
        [SerializeField] private int _itemsCount;
        [SerializeField] private RSR _scrollRect;
        [SerializeField] private GameObject[] _prototypeItems;
        [SerializeField] private float _timeToScroll;
        [SerializeField] private bool _isSpeed;
        [SerializeField] private bool _isInstant;

        private List<string> _dataSource;
        private int _itemCount;

        public int ItemsCount => _itemsCount;
        public bool IsItemSizeKnown => true;
        public GameObject[] PrototypeItems => _prototypeItems;

        private void Start()
        {
            _dataSource = new List<string>();
            for (var i = 0; i < _itemsCount; i++)
                _dataSource.Add(i.ToString());
            _scrollRect.Initialize(this);
        }

        [ContextMenu(nameof(ScrollToTopRight))]
        public void ScrollToTopRight()
        {
            _scrollRect.ScrollToTopRight(_timeToScroll, _isSpeed, _isInstant, DG.Tweening.Ease.InOutSine);
        }

        public float GetItemSize(int itemIndex)
        {
            return 40.22f;
        }

        public void SetItemData(IItem item, int itemIndex)
        {
            (item as DemoItemPrototype)?.Initialize(_dataSource[itemIndex]);
        }

        public void ItemHidden(IItem item, int itemIndex)
        {
        }

        public GameObject GetItemPrototype(int itemIndex)
        {
            if (itemIndex % 2 == 0)
                return _prototypeItems[0];
            return _prototypeItems[1];
        }

        public void ItemCreated(int itemIndex, IItem item, GameObject itemGo)
        {

        }

        public bool IsItemStatic(int itemIndex)
        {
            return false;
        }

        public void ScrolledToItem(IItem item, int itemIndex)
        {
        }

        public bool IgnoreContentPadding(int itemIndex)
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