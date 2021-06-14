using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(RecyclableScrollRect), true)]
[CanEditMultipleObjects]
public class RecyclableScrollRectEditor : ScrollRectEditor
{
    SerializedProperty _protoTypeCell;
    SerializedProperty _initOnStart;
    SerializedProperty _dataSourceContainer;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        _protoTypeCell = serializedObject.FindProperty("_prototypeCell");
        _initOnStart = serializedObject.FindProperty("_initOnStart");
        _dataSourceContainer = serializedObject.FindProperty("_dataSourceContainer");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_initOnStart);
        EditorGUILayout.PropertyField(_protoTypeCell);
        EditorGUILayout.PropertyField(_dataSourceContainer);
        serializedObject.ApplyModifiedProperties();
    }
}