// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using UnityEngine;

namespace RecyclableScrollRect
{
    [RequireComponent(typeof(RSRBase))]
    public abstract class BaseScrollAnimationController : MonoBehaviour
    {
        [SerializeField] protected RSRBase _scrollRect;

        public abstract void ScrollToContentPosition(float targetContentPosition, float timeOrSpeed, bool isSpeed, Action onFinished);
        public abstract float StopCurrentAnimation();
        
        private void Start()
        {
            if (_scrollRect == null)
            {
                _scrollRect = gameObject.GetComponent<RSRBase>();
            }

            if (_scrollRect == null)
            {  
                Debug.LogError("ScrollAnimationController requires a RSRBase component on the same GameObject or assigned to the _scrollRect field.");
                enabled = false;
            }
        }
        
        private void OnValidate()
        {
            if (_scrollRect == null)
            {
                _scrollRect = gameObject.GetComponent<RSRBase>();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
    }
    
    public abstract class BaseScrollAnimationController<TEase> : BaseScrollAnimationController
    {
        public abstract void ScrollToContentPosition(float targetContentPosition, float timeOrSpeed, bool isSpeed, TEase ease, Action onFinished);
    }
}