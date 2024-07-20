#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MainScript))]
public class MainScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Main Editor"))
        {
            MainEditorWindow.OpenWindow();
        }

        base.OnInspectorGUI();
    }
}
#endif
