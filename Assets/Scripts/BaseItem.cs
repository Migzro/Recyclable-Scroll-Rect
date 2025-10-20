// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;

namespace RecyclableSR
{
    public class BaseItem : MonoBehaviour, IItem
    {
        [SerializeField] private CanvasGroup canvasGroup;
        public int ItemIndex { get; set; }
        public RSRBase RSRBase { get; set; }
        public RectTransform[] ItemsNeededForVisualUpdate { get; }

        public CanvasGroup CanvasGroup
        {
            get
            {
                if (canvasGroup == null)
                {
                    if (gameObject.GetComponent<CanvasGroup>() != null)
                        canvasGroup = gameObject.GetComponent<CanvasGroup>();
                    else
                        canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }

                return canvasGroup;
            }
        }
    }
}