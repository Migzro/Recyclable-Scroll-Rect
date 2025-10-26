using System;
using UnityEngine;

namespace RecyclableScrollRect
{
    [RequireComponent(typeof(RSRBase))]
    public abstract class BaseScrollAnimationController : MonoBehaviour
    {
        [SerializeField] protected RSRBase _scrollRect;

        public abstract void ScrollToNormalizedPosition(float targetNormalizedPos, float time, bool isSpeed, bool instant, Action onFinished);
        public abstract void ScrollToItem(int itemIndex, bool callEvent = true, bool instant = false, float maxSpeedMultiplier = 1, float offset = 0);
        
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
        public abstract void ScrollToNormalizedPosition(float targetNormalizedPos, float time, bool isSpeed, bool instant, TEase ease, Action onFinished);
    }
}