using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RecyclableSR.Editor
{
    [CustomPropertyDrawer(typeof(IDataSourceContainer), true)]
    public class DataSourceContainerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            label.text = "Data Source";
 
            var strategyHolder = property.serializedObject.targetObject;
            var fieldInfo = strategyHolder.GetType().GetField(property.propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
 
            var strategyProxy = fieldInfo.GetValue(strategyHolder) as IDataSourceContainer;
            if (strategyProxy == null)
            {
                strategyProxy = new IDataSourceContainer();
                fieldInfo.SetValue(strategyHolder, strategyProxy);
            }
 
            EditorGUI.BeginChangeCheck();
            strategyProxy.DataSource = EditorGUI.ObjectField(position, label, strategyProxy.DataSource as Object, typeof(IDataSource), true) as IDataSource;
            if (EditorGUI.EndChangeCheck())
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                property.serializedObject.ApplyModifiedProperties();
            }
 
            EditorGUI.EndProperty();
        }
    }
}