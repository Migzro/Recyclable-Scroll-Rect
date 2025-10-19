using UnityEditor;
using UnityEditor.UI;

namespace RecyclableSR
{
    [CustomEditor(typeof(RSRBase), true)]
    public class RSRBaseEditor : ScrollRectEditor
    {
        private SerializedProperty _showUsingCanvasGroupAlpha;
        private SerializedProperty _pullToRefreshThreshold;
        private SerializedProperty _pushToCloseThreshold;
        private SerializedProperty _useConstantScrollingSpeed;
        private SerializedProperty _constantScrollingSpeed;
        private SerializedProperty _padding;
        private SerializedProperty _spacing;
        private SerializedProperty _childAlignment;

        protected override void OnEnable()
        {
            base.OnEnable();
            _showUsingCanvasGroupAlpha = serializedObject.FindProperty(nameof(_showUsingCanvasGroupAlpha));
            _pullToRefreshThreshold = serializedObject.FindProperty(nameof(_pullToRefreshThreshold));
            _pushToCloseThreshold = serializedObject.FindProperty(nameof(_pushToCloseThreshold));
            _useConstantScrollingSpeed = serializedObject.FindProperty(nameof(_useConstantScrollingSpeed));
            _constantScrollingSpeed = serializedObject.FindProperty(nameof(_constantScrollingSpeed));
            _padding = serializedObject.FindProperty(nameof(_padding));
            _spacing = serializedObject.FindProperty(nameof(_spacing));
            _childAlignment = serializedObject.FindProperty(nameof(_childAlignment));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_showUsingCanvasGroupAlpha);
            EditorGUILayout.PropertyField(_pullToRefreshThreshold);
            EditorGUILayout.PropertyField(_pushToCloseThreshold);
            EditorGUILayout.PropertyField(_useConstantScrollingSpeed);
            if (_useConstantScrollingSpeed.boolValue)
            {
                EditorGUILayout.PropertyField(_constantScrollingSpeed);
                EditorGUILayout.Space();
            }
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_padding);
            EditorGUILayout.PropertyField(_spacing);
            EditorGUILayout.PropertyField(_childAlignment);
            serializedObject.ApplyModifiedProperties();
        }
    }
}