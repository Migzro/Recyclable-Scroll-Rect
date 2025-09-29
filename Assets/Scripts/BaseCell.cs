using UnityEngine;

namespace RecyclableSR
{
    public class BaseCell : MonoBehaviour, ICell
    {
        [SerializeField] private CanvasGroup canvasGroup;
        public int CellIndex { get; set; }
        public RSRBase RSRBase { get; set; }
        public RectTransform[] CellsNeededForVisualUpdate { get; }

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