using UnityEditor;
using UnityEditor.UI;

namespace RecyclableSR
{
    [CustomEditor(typeof(RecyclableScrollRect), true)]
    [CanEditMultipleObjects]
    public class RecyclableScrollRectEditor : ScrollRectEditor
    {
        private SerializedProperty _paged;
        private SerializedProperty _swipeThreshold;
        private SerializedProperty _reverseDirection;
        private SerializedProperty _useCardsAnimation;
        private SerializedProperty _cardZMultiplier;

        protected override void OnEnable()
        {
            base.OnEnable();
            _paged = serializedObject.FindProperty(nameof(_paged));
            _swipeThreshold = serializedObject.FindProperty(nameof(_swipeThreshold));
            _reverseDirection = serializedObject.FindProperty(nameof(_reverseDirection));
            
            _useCardsAnimation = serializedObject.FindProperty(nameof(_useCardsAnimation));
            _cardZMultiplier = serializedObject.FindProperty(nameof(_cardZMultiplier));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_reverseDirection);
            EditorGUILayout.PropertyField(_paged);
            if (_paged.boolValue)
            {
                EditorGUILayout.PropertyField(_swipeThreshold);
                EditorGUILayout.PropertyField(_useCardsAnimation);
                if (_useCardsAnimation.boolValue)
                    EditorGUILayout.PropertyField(_cardZMultiplier);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}