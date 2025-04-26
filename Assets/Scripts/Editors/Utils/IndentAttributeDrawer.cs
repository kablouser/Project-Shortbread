#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(IndentAttribute))]
public class IndentAttributeDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return -2f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        IndentAttribute indentAttribute = (IndentAttribute)attribute;
        if (indentAttribute.label != null)
            EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(indentAttribute.label));
        EditorGUI.indentLevel += indentAttribute.indents;
        EditorGUILayout.PropertyField(property);
        EditorGUI.indentLevel -= indentAttribute.indents;
    }
}
#endif