using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(RecyclableScrollRect), true)]
[CanEditMultipleObjects]
public class RecyclableScrollRectEditor : ScrollRectEditor
{
    SerializedProperty _protoTypeCell;
    SerializedProperty _initOnStart;
    SerializedProperty _dataSourceContainer;
    SerializedProperty _extraItemsVisible;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        _initOnStart = serializedObject.FindProperty("_initOnStart");
        _protoTypeCell = serializedObject.FindProperty("_prototypeCell");
        _dataSourceContainer = serializedObject.FindProperty("_dataSourceContainer");
        _extraItemsVisible = serializedObject.FindProperty("_extraItemsVisible");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_initOnStart);
        EditorGUILayout.PropertyField(_protoTypeCell);
        EditorGUILayout.PropertyField(_dataSourceContainer);
        EditorGUILayout.PropertyField(_extraItemsVisible);
        serializedObject.ApplyModifiedProperties();
    }
}