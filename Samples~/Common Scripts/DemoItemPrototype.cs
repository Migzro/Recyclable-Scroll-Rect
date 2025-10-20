// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using TMPro;
using UnityEngine;

namespace RecyclableScrollRect
{
    public class DemoItemPrototype : MonoBehaviour, IItem
    {
        [SerializeField] private TextMeshProUGUI _text;
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

        public void Initialize(string text)
        {
            _text.text = text;
        }
    }
}