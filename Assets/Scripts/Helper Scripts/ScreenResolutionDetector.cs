using System;
using System.Collections;
using UnityEngine;

public class ScreenResolutionDetector : MonoBehaviour 
{
    private static ScreenResolutionDetector _Instance;
    public static ScreenResolutionDetector Instance
    {
        get
        {
            if (!_Instance)
            {
                _Instance = new GameObject().AddComponent<ScreenResolutionDetector>();
                _Instance.name = _Instance.GetType().ToString();
                DontDestroyOnLoad(_Instance.gameObject);
            }
            return _Instance;
        }
    }
    
    public event Action OnResolutionChanged;
    private int _lastWidth;
    private int _lastHeight;

    private void Awake()
    {
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
    }

    private void Update()
    {
        if (Screen.width != _lastWidth || Screen.height != _lastHeight)
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            StartCoroutine(WaitForFrame());
        }
    }
    
    private IEnumerator WaitForFrame()
    {
        yield return new WaitForEndOfFrame();
        OnResolutionChanged?.Invoke();
    }


    private void OnDestroy()
    {
        _Instance = null;
    }
}