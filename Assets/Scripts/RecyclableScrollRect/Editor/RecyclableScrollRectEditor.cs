using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace RecyclableSR.Editor
{
    [CustomEditor(typeof(RecyclableScrollRect), true)]
    [CanEditMultipleObjects]
    public class RecyclableScrollRectEditor : ScrollRectEditor
    {
        SerializedProperty _initOnStart;
        SerializedProperty _useDataSourcePrototypeCells;
        SerializedProperty _protoTypeCell;
        SerializedProperty _dataSourceContainer;
        SerializedProperty _extraItemsVisible;

        protected override void OnEnable()
        {
            base.OnEnable();
            _initOnStart = serializedObject.FindProperty("_initOnStart");
            _useDataSourcePrototypeCells = serializedObject.FindProperty("_useDataSourcePrototypeCells");
            _protoTypeCell = serializedObject.FindProperty("_prototypeCell");
            _dataSourceContainer = serializedObject.FindProperty("_dataSourceContainer");
            _extraItemsVisible = serializedObject.FindProperty("_extraItemsVisible");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_initOnStart);
            EditorGUILayout.PropertyField(_useDataSourcePrototypeCells);
            if (EditorGUILayout.BeginFadeGroup(((RecyclableScrollRect) target).useDataSourcePrototypeCells ? 0 : 1))
            {
                EditorGUILayout.PropertyField(_protoTypeCell);
            }
            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.PropertyField(_dataSourceContainer);
            EditorGUILayout.PropertyField(_extraItemsVisible);
            serializedObject.ApplyModifiedProperties();
        }
    }
}