using TMPro;
using UnityEngine;

public class DemoCellPrototype : MonoBehaviour, ICell
{
    [SerializeField] private TextMeshProUGUI _text;

    public void Initialize(string text)
    {
        _text.text = text;
    }
}