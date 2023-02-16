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
        private SerializedProperty _useConstantScrollingSpeed;
        private SerializedProperty _constantScrollingSpeed;
        private SerializedProperty _hiddenCardsOffset;
        private SerializedProperty _hideOffsetCards;

        protected override void OnEnable()
        {
            base.OnEnable();
            _paged = serializedObject.FindProperty(nameof(_paged));
            _swipeThreshold = serializedObject.FindProperty(nameof(_swipeThreshold));
            _reverseDirection = serializedObject.FindProperty(nameof(_reverseDirection));
            
            _useCardsAnimation = serializedObject.FindProperty(nameof(_useCardsAnimation));
            _cardZMultiplier = serializedObject.FindProperty(nameof(_cardZMultiplier));
            
            _useConstantScrollingSpeed = serializedObject.FindProperty(nameof(_useConstantScrollingSpeed));
            _constantScrollingSpeed = serializedObject.FindProperty(nameof(_constantScrollingSpeed));
            
            _hiddenCardsOffset = serializedObject.FindProperty(nameof(_hiddenCardsOffset));
            _hideOffsetCards = serializedObject.FindProperty(nameof(_hideOffsetCards));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_reverseDirection);
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_paged);
            if (_paged.boolValue)
            {
                EditorGUILayout.PropertyField(_swipeThreshold);
                
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_useCardsAnimation);
                if (_useCardsAnimation.boolValue)
                {
                    EditorGUILayout.PropertyField(_cardZMultiplier);
                }
                EditorGUILayout.PropertyField(_hiddenCardsOffset);
                EditorGUILayout.PropertyField(_hideOffsetCards);
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