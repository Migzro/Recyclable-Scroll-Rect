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
        SerializedProperty _reverseDirection;

        protected override void OnEnable()
        {
            base.OnEnable();
            _paged = serializedObject.FindProperty(nameof(_paged));
            _swipeThreshold = serializedObject.FindProperty(nameof(_swipeThreshold));
            _reverseDirection = serializedObject.FindProperty(nameof(_reverseDirection));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_reverseDirection);
            EditorGUILayout.PropertyField(_paged);
            if (_paged.boolValue)
                EditorGUILayout.PropertyField(_swipeThreshold);
            serializedObject.ApplyModifiedProperties();
        }
    }
}