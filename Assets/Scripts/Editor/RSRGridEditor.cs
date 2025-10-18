using UnityEditor;
using UnityEngine.UI;

namespace RecyclableSR
{
    [CustomEditor(typeof(RSRGrid), true)]
    [CanEditMultipleObjects]
    public class RSRGridEditor : RSREditor
    {
        private SerializedProperty _gridItemSize;
        private SerializedProperty _gridStartAxis;
        private SerializedProperty _gridConstraint;
        private SerializedProperty _gridConstraintCount;
        private SerializedProperty _gridStartCorner;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _gridItemSize = serializedObject.FindProperty(nameof(_gridItemSize));
            _gridStartAxis = serializedObject.FindProperty(nameof(_gridStartAxis));
            _gridConstraint = serializedObject.FindProperty(nameof(_gridConstraint));
            _gridConstraintCount = serializedObject.FindProperty(nameof(_gridConstraintCount));
            _gridStartCorner = serializedObject.FindProperty(nameof(_gridStartCorner));
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Data", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_gridItemSize);
            EditorGUILayout.PropertyField(_gridStartAxis);
            EditorGUILayout.PropertyField(_gridConstraint);

            if (_gridConstraint.intValue != (int)GridLayoutGroup.Constraint.Flexible)
            {
                EditorGUILayout.PropertyField(_gridConstraintCount);
            }
            EditorGUILayout.PropertyField(_gridStartCorner);
            serializedObject.ApplyModifiedProperties();
        }
    }
}