#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class MainEditorWindow : EditorWindow
{
    [MenuItem("Tools/MainEditorWindow")]
    public static void OpenWindow() => GetWindow<MainEditorWindow>("Main Editor");

    public MainScript mainScript;
    public bool alwaysDrawGizmos = true;

    public void OnEnable()
    {
        Selection.selectionChanged += Selection_selectionChanged;
        SceneView.duringSceneGui += SceneView_duringSceneGui;
    }

    public void OnDisable()
    {
        Selection.selectionChanged -= Selection_selectionChanged;
        SceneView.duringSceneGui -= SceneView_duringSceneGui;
    }

    public void Selection_selectionChanged()
    {
        Repaint();
    }

    public void SceneView_duringSceneGui(SceneView sceneView)
    {
        if (mainScript == null)
        {
            mainScript = FindObjectOfType<MainScript>();
            if (mainScript == null)
                return;
        }

        if (sceneView.drawGizmos || alwaysDrawGizmos)
        {
            mainScript.navigationGrid.OnSceneGUI();
        }
    }

    public void OnGUI()
    {
        bool bSceneViewDirty = false;

        if (alwaysDrawGizmos != GUILayout.Toggle(alwaysDrawGizmos, "Draw Gizmos"))
        {
            alwaysDrawGizmos = !alwaysDrawGizmos;
            bSceneViewDirty = true;
        }

        if (bSceneViewDirty)
            SceneView.RepaintAll();
    }
}
#endif