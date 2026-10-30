// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Reflection;
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
            DrawSubclassFields();
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the serialized fields added by classes deriving from RSRCarousel, such as custom layouts
        /// </summary>
        protected virtual void DrawSubclassFields()
        {
            DrawFieldsDeclaredBelow(typeof(RSRCarousel));
        }

        /// <summary>
        /// Draws the serialized fields declared by the target's class and its base classes, stopping at baseType
        /// </summary>
        protected void DrawFieldsDeclaredBelow(System.Type baseType)
        {
            for (var type = target.GetType(); type != baseType && type != null; type = type.BaseType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    var property = serializedObject.FindProperty(field.Name);
                    if (property != null)
                    {
                        EditorGUILayout.PropertyField(property, true);
                    }
                }
            }
        }
    }
}
