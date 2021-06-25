using RecyclableSR;
using TMPro;
using UnityEngine;

public class DemoCellPrototype : MonoBehaviour, ICell
{
    [SerializeField] private TextMeshProUGUI _text;
    public int index { get; private set; }

    public void Initialize(string text, int cellIndex)
    {
        _text.text = text;
        index = cellIndex;
    }
}