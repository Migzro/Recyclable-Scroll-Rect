// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEditor;

namespace RecyclableScrollRect.Editor
{
    [CustomEditor(typeof(RSRCarousel), true)]
    public class RSRCarouselEditor : RSRPagesEditor
    {
        private SerializedProperty _loop;

        protected override void OnEnable()
        {
            base.OnEnable();
            _loop = serializedObject.FindProperty(nameof(_loop));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(_loop);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
