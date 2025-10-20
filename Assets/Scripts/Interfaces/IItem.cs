// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;

namespace RecyclableSR
{
    public interface IItem
    { 
        int ItemIndex { set; }
        RSRBase RSRBase { set; }
        RectTransform[] ItemsNeededForVisualUpdate { get; }
        CanvasGroup CanvasGroup { get; }
    }
}