using UnityEditor;
using UnityEngine.UI;

namespace RecyclableSR
{
    [CustomEditor(typeof(RSRGrid), true)]
    [CanEditMultipleObjects]
    public class RSRGridEditor : RSRBaseEditor
    {
        private SerializedProperty _gridCellSize;
        private SerializedProperty _gridStartAxis;
        private SerializedProperty _gridConstraint;
        private SerializedProperty _gridConstraintCount;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _gridCellSize = serializedObject.FindProperty(nameof(_gridCellSize));
            _gridStartAxis = serializedObject.FindProperty(nameof(_gridStartAxis));
            _gridConstraint = serializedObject.FindProperty(nameof(_gridConstraint));
            _gridConstraintCount = serializedObject.FindProperty(nameof(_gridConstraintCount));
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_gridCellSize);
            EditorGUILayout.PropertyField(_gridStartAxis);
            EditorGUILayout.PropertyField(_gridConstraint);

            if (_gridConstraint.intValue != (int)GridLayoutGroup.Constraint.Flexible)
            {
                EditorGUILayout.PropertyField(_gridConstraintCount);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}