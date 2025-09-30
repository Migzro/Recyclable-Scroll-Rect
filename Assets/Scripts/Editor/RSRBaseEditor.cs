using UnityEditor;
using UnityEditor.UI;

namespace RecyclableSR
{
    // TODO: change type to RSR
    [CustomEditor(typeof(RSRBase), true)]
    [CanEditMultipleObjects]
    public class RSRBaseEditor : ScrollRectEditor
    {
        private SerializedProperty _padding;
        private SerializedProperty _spacing;
        private SerializedProperty _alignment;
        
        private SerializedProperty _reverseDirection; 
        private SerializedProperty _childForceExpand;
        private SerializedProperty _pullToRefreshThreshold;
        private SerializedProperty _pushToCloseThreshold;
        private SerializedProperty _useConstantScrollingSpeed;
        private SerializedProperty _constantScrollingSpeed;
        
        private SerializedProperty _paged;
        private SerializedProperty _swipeThreshold;
        
        private SerializedProperty _isGridLayout;
        private SerializedProperty _gridCellSize;
        private SerializedProperty _gridStartAxis;
        private SerializedProperty _gridConstraint;
        private SerializedProperty _gridConstraintCount;

        protected override void OnEnable()
        {
            base.OnEnable();
            _padding = serializedObject.FindProperty(nameof(_padding));
            _spacing = serializedObject.FindProperty(nameof(_spacing));
            _alignment = serializedObject.FindProperty(nameof(_alignment));
            
            _reverseDirection = serializedObject.FindProperty(nameof(_reverseDirection));
            _childForceExpand = serializedObject.FindProperty(nameof(_childForceExpand));
            _pullToRefreshThreshold = serializedObject.FindProperty(nameof(_pullToRefreshThreshold));
            _pushToCloseThreshold = serializedObject.FindProperty(nameof(_pushToCloseThreshold));
            _useConstantScrollingSpeed = serializedObject.FindProperty(nameof(_useConstantScrollingSpeed));
            _constantScrollingSpeed = serializedObject.FindProperty(nameof(_constantScrollingSpeed));
            
            _paged = serializedObject.FindProperty(nameof(_paged));
            _swipeThreshold = serializedObject.FindProperty(nameof(_swipeThreshold));

            _isGridLayout = serializedObject.FindProperty(nameof(_isGridLayout));
            _gridCellSize = serializedObject.FindProperty(nameof(_gridCellSize));
            _gridStartAxis = serializedObject.FindProperty(nameof(_gridStartAxis));
            _gridConstraint = serializedObject.FindProperty(nameof(_gridConstraint));
            _gridConstraintCount = serializedObject.FindProperty(nameof(_gridConstraintCount));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_padding);
            EditorGUILayout.PropertyField(_spacing);
            EditorGUILayout.PropertyField(_alignment);
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_reverseDirection);
            EditorGUILayout.PropertyField(_childForceExpand);
            EditorGUILayout.PropertyField(_pullToRefreshThreshold);
            EditorGUILayout.PropertyField(_pushToCloseThreshold);
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_useConstantScrollingSpeed);
            if (_useConstantScrollingSpeed.boolValue)
            {
                EditorGUILayout.PropertyField(_constantScrollingSpeed);
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.PropertyField(_paged);
            if (_paged.boolValue)
            {
                EditorGUILayout.PropertyField(_swipeThreshold);
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.PropertyField(_isGridLayout);
            if (_isGridLayout.boolValue)
            {
                EditorGUILayout.PropertyField(_gridCellSize);
                EditorGUILayout.PropertyField(_gridStartAxis);
                EditorGUILayout.PropertyField(_gridConstraint);
                EditorGUILayout.PropertyField(_gridConstraintCount);
                EditorGUILayout.Space();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}