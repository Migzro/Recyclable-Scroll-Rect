// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEditor;

namespace RecyclableScrollRect.Editor
{
    [CustomEditor(typeof(RSRCards), true)]
    public class RSRCardsEditor : RSRCarouselEditor
    {
        private SerializedProperty _mode;
        private SerializedProperty _depthScaleStep;
        private SerializedProperty _maxDepth;
        private SerializedProperty _depthPeek;
        private SerializedProperty _tuckSlideDistance;
        private SerializedProperty _fanSpacing;
        private SerializedProperty _fanFillViewport;
        private SerializedProperty _fanPileAtEdges;

        protected override void OnEnable()
        {
            base.OnEnable();
            _mode = serializedObject.FindProperty(nameof(_mode));
            _depthScaleStep = serializedObject.FindProperty(nameof(_depthScaleStep));
            _maxDepth = serializedObject.FindProperty(nameof(_maxDepth));
            _depthPeek = serializedObject.FindProperty(nameof(_depthPeek));
            _tuckSlideDistance = serializedObject.FindProperty(nameof(_tuckSlideDistance));
            _fanSpacing = serializedObject.FindProperty(nameof(_fanSpacing));
            _fanFillViewport = serializedObject.FindProperty(nameof(_fanFillViewport));
            _fanPileAtEdges = serializedObject.FindProperty(nameof(_fanPileAtEdges));
        }

        /// <summary>
        /// Shows only the settings the selected mode uses
        /// </summary>
        protected override void DrawSubclassFields()
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_mode);
            EditorGUILayout.PropertyField(_depthScaleStep);

            var mode = (RSRCards.CardsMode)_mode.enumValueIndex;
            if (mode == RSRCards.CardsMode.Fan)
            {
                EditorGUILayout.PropertyField(_fanSpacing);
                EditorGUILayout.PropertyField(_fanFillViewport);
                EditorGUILayout.PropertyField(_fanPileAtEdges);
            }
            else
            {
                EditorGUILayout.PropertyField(_maxDepth);
                EditorGUILayout.PropertyField(_depthPeek);
                if (mode == RSRCards.CardsMode.TuckUnder)
                {
                    EditorGUILayout.PropertyField(_tuckSlideDistance);
                }
            }

            // fields added by classes deriving from RSRCards
            DrawFieldsDeclaredBelow(typeof(RSRCards));
        }
    }
}
