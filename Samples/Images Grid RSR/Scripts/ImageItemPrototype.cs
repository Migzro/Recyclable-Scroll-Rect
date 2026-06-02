// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;
using UnityEngine.UI;

namespace RecyclableScrollRect
{
    public class ImageItemPrototype : MonoBehaviour, IItem
    {
        [SerializeField] private Image _image;
        [SerializeField] private GameObject _loadingGo;
        [SerializeField] private CanvasGroup _canvasGroup;
        public int ItemIndex { get; set; }
        public RSRBase RSRBase { get; set; }
        public RectTransform[] ItemsNeededForVisualUpdate => null;

        public CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.GetComponent<CanvasGroup>();
                    if (_canvasGroup == null)
                        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }

                return _canvasGroup;
            }
        }
        
        public void Initialize()
        {
            _loadingGo.SetActive(true);
            _image.gameObject.SetActive(false);
        }

        public void SetImage(Sprite sprite)
        {
            _loadingGo.SetActive(false);
            _image.gameObject.SetActive(true);
            _image.sprite = sprite;
        }
    }
}