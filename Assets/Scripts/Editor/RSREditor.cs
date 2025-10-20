// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEditor;

namespace RecyclableScrollRect
{
    [CustomEditor(typeof(RSR), true)]
    public class RSREditor : RSRBaseEditor
    {
        private SerializedProperty _childForceExpand; 
        private SerializedProperty _reverseArrangement; 
        private SerializedProperty _extraItemsVisible;

        protected override void OnEnable()
        {
            base.OnEnable();
            _childForceExpand = serializedObject.FindProperty(nameof(_childForceExpand));
            _reverseArrangement = serializedObject.FindProperty(nameof(_reverseArrangement));
            _extraItemsVisible = serializedObject.FindProperty(nameof(_extraItemsVisible));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_childForceExpand);
            EditorGUILayout.PropertyField(_reverseArrangement);
            EditorGUILayout.PropertyField(_extraItemsVisible);
            serializedObject.ApplyModifiedProperties();
        }
    }
}