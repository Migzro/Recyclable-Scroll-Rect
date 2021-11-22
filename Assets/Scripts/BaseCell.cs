using RecyclableSR;
using UnityEngine;

namespace RecyclableSR
{
    public class BaseCell : MonoBehaviour, ICell
    {
        [SerializeField] private CanvasGroup canvasGroup;
        public int CellIndex { get; set; }
        public RecyclableScrollRect RecyclableScrollRect { get; set; }
        public RectTransform[] CellsNeededForVisualUpdate { get; }

        public CanvasGroup CanvasGroup
        {
            get
            {
                if ( canvasGroup == null )
                {
                    canvasGroup = gameObject.AddComponent< CanvasGroup >();
                }

                return canvasGroup;
            }
        }
    }
}