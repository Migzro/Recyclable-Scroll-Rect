using UnityEditor;

namespace RecyclableSR
{
    [CustomEditor(typeof(RSRPages), true)]
    public class RSRPagesEditor : RSREditor
    {
        private SerializedProperty _swipeThreshold;
        private SerializedProperty _cardMode;
        private SerializedProperty _cardZMultiplier;
        private SerializedProperty _manuallyHandleCardAnimations;

        protected override void OnEnable()
        {
            base.OnEnable();
            _swipeThreshold = serializedObject.FindProperty(nameof(_swipeThreshold));
            _cardMode = serializedObject.FindProperty(nameof(_cardMode));
            _cardZMultiplier = serializedObject.FindProperty(nameof(_cardZMultiplier));
            _manuallyHandleCardAnimations = serializedObject.FindProperty(nameof(_manuallyHandleCardAnimations));
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(_swipeThreshold);
            EditorGUILayout.PropertyField(_cardMode);
            if (_cardMode.boolValue)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_cardZMultiplier);
                EditorGUILayout.PropertyField(_manuallyHandleCardAnimations);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}