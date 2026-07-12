// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RecyclableScrollRect
{
    public class ImagesGridRSRDemo : MonoBehaviour, IGridDataSource
    {
        [SerializeField] private int _itemsCount;
        [SerializeField] private RSRGrid _scrollRect;
        [SerializeField] private GameObject[] _prototypeItems;

        private Dictionary<int, Sprite> _sprites;

        public GameObject[] PrototypeItems => _prototypeItems;

        private void Start()
        {
            _sprites = new Dictionary<int, Sprite>();
            _scrollRect.Initialize(this);
        }

        IEnumerator LoadImage(IItem item, int itemIndex)
        {
            if (!_sprites.ContainsKey(itemIndex))
            {
                var wr = new UnityWebRequest("https://picsum.photos/335");
                var texDl = new DownloadHandlerTexture(true);
                wr.downloadHandler = texDl;
                yield return wr.SendWebRequest();
                if (wr.result == UnityWebRequest.Result.Success)
                {
                    var t = texDl.texture;
                    var s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero, 1f);
                    _sprites.Add(itemIndex, s);
                }
            }
            ((ImageItemPrototype)item)?.SetImage(_sprites[itemIndex]);
        }

        public void SetItemData(IItem item, ItemData itemData)
        {
            // download image and cache it using itemIndex as key, then pass the downloaded image to the item to initialize it
            ((ImageItemPrototype)item)?.Initialize();
            StartCoroutine(LoadImage(item, itemData.itemIndex));
        }

        public void ItemHidden(IItem item, ItemData itemData)
        {
        }

        public GameObject GetItemPrototype(ItemData itemData)
        {
            return _prototypeItems[0];
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
        
        public int GetItemsCount(int sectionIndex)
        {
            return _itemsCount;
        }
    }
}