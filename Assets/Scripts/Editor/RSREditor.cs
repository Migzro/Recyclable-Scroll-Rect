using UnityEditor;

namespace RecyclableSR
{
    [CustomEditor(typeof(RSR), true)]
    public class RSREditor : RSRBaseEditor
    {
        private SerializedProperty _reverseArrangement; 
        private SerializedProperty _extraItemsVisible;

        protected override void OnEnable()
        {
            base.OnEnable();
            _reverseArrangement = serializedObject.FindProperty(nameof(_reverseArrangement));
            _extraItemsVisible = serializedObject.FindProperty(nameof(_extraItemsVisible));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_reverseArrangement);
            EditorGUILayout.PropertyField(_extraItemsVisible);
            serializedObject.ApplyModifiedProperties();
        }
    }
}