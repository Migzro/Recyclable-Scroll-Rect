// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEditor;

namespace RecyclableScrollRect
{
    [CustomEditor(typeof(RSRPages), true)]
    public class RSRPagesEditor : RSREditor
    {
        private SerializedProperty _swipeThreshold;

        protected override void OnEnable()
        {
            base.OnEnable();
            _swipeThreshold = serializedObject.FindProperty(nameof(_swipeThreshold));
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(_swipeThreshold);
            serializedObject.ApplyModifiedProperties();
        }
    }
}