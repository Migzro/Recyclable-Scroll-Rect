using UnityEditor;
using UnityEditor.UI;

namespace RecyclableSR
{
    // TODO: change type to RSR
    [CustomEditor(typeof(RSRBase), true)]
    [CanEditMultipleObjects]
    public class RSRBaseEditor : ScrollRectEditor
    {
        private SerializedProperty _reverseDirection;
        private SerializedProperty _pullToRefreshThreshold;
        private SerializedProperty _pushToCloseThreshold;
        private SerializedProperty _paged;
        private SerializedProperty _swipeThreshold;
        private SerializedProperty _useConstantScrollingSpeed;
        private SerializedProperty _constantScrollingSpeed;

        protected override void OnEnable()
        {
            base.OnEnable();
            _reverseDirection = serializedObject.FindProperty(nameof(_reverseDirection));
            _pullToRefreshThreshold = serializedObject.FindProperty(nameof(_pullToRefreshThreshold));
            _pushToCloseThreshold = serializedObject.FindProperty(nameof(_pushToCloseThreshold));
            
            _paged = serializedObject.FindProperty(nameof(_paged));
            _swipeThreshold = serializedObject.FindProperty(nameof(_swipeThreshold));
            
            _useConstantScrollingSpeed = serializedObject.FindProperty(nameof(_useConstantScrollingSpeed));
            _constantScrollingSpeed = serializedObject.FindProperty(nameof(_constantScrollingSpeed));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_reverseDirection);
            EditorGUILayout.PropertyField(_pullToRefreshThreshold);
            EditorGUILayout.PropertyField(_pushToCloseThreshold);
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_paged);
            if (_paged.boolValue)
            {
                EditorGUILayout.PropertyField(_swipeThreshold);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_useConstantScrollingSpeed);
            if (_useConstantScrollingSpeed.boolValue)
            {
                EditorGUILayout.PropertyField(_constantScrollingSpeed);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}