using RecyclableSR;
using TMPro;
using UnityEngine;

public class DemoCellPrototype : MonoBehaviour, ICell
{
    [SerializeField] private TextMeshProUGUI _text;
    public int cellIndex { get; set; }
    public RecyclableScrollRect recyclableScrollRect { get; set; }

    public void Initialize(string text)
    {
        _text.text = text;
    }
}