using UnityEditor;
using UnityEditor.UI;

namespace RecyclableSR
{
    [CustomEditor(typeof(RecyclableScrollRect), true)]
    [CanEditMultipleObjects]
    public class RecyclableScrollRectEditor : ScrollRectEditor
    {
        SerializedProperty _paged;
        SerializedProperty _swipeThreshold;
        SerializedProperty _scrollingSpeed;

        protected override void OnEnable()
        {
            base.OnEnable();
            _paged = serializedObject.FindProperty("_paged");
            _swipeThreshold = serializedObject.FindProperty("_swipeThreshold");
            _scrollingSpeed = serializedObject.FindProperty("_scrollingSpeed");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_paged);
            if (_paged.boolValue)
                EditorGUILayout.PropertyField(_swipeThreshold);
            EditorGUILayout.PropertyField(_scrollingSpeed);
            serializedObject.ApplyModifiedProperties();
        }
    }
}