using RecyclableSR;
using TMPro;
using UnityEngine;

public class DemoCellPrototype : MonoBehaviour, ICell
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private CanvasGroup _canvasGroup; 
    public int CellIndex { get; set; }
    public RSRBase RSRBase { get; set; }
    public RectTransform[] CellsNeededForVisualUpdate => null;
    public CanvasGroup CanvasGroup {
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