using RecyclableSR;
using TMPro;
using UnityEngine;

public class DemoCellPrototype : MonoBehaviour, ICell
{
    [SerializeField] private TextMeshProUGUI _text;
    public int CellIndex { get; set; }
    public RecyclableScrollRect RecyclableScrollRect { get; set; }
    public RectTransform[] CellsNeededForVisualUpdate => null;
    public CanvasGroup CanvasGroup { get; }

    public void Initialize(string text)
    {
        _text.text = text;
    }
}