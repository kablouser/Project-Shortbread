#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class MainEditorWindow : EditorWindow
{
    [MenuItem("Tools/MainEditorWindow")]
    public static void OpenWindow() => GetWindow<MainEditorWindow>("Main Editor");

    public MainScript mainScript;
    public bool alwaysDrawGizmos = true;

    public ID selectedID;

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
        CacheMainScript();
        GameObject go = Selection.activeGameObject;

        selectedID = new ID();
        foreach (int i in mainScript.lightCrystals)
        {
            if (go == mainScript.lightCrystals[i].transform.gameObject)
            {
                selectedID = mainScript.lightCrystals.GetCurrentID(i);
                break;
            }
        }
        if (selectedID.type == IDType.Invalid)
        {
            foreach (int i in mainScript.pickupSystem.pickups)
            {
                if (go == mainScript.pickupSystem.pickups[i].transform.gameObject)
                {
                    selectedID = mainScript.lightCrystals.GetCurrentID(i);
                    break;
                }
            }
        }

        Repaint();
    }

    public bool CacheMainScript()
    {
        if (mainScript == null)
        {
            mainScript = FindObjectOfType<MainScript>();
            if (mainScript == null)
                return false;
        }
        return true;
    }

    public void SceneView_duringSceneGui(SceneView sceneView)
    {
        if (!CacheMainScript())
            return;

        if (sceneView.drawGizmos || alwaysDrawGizmos)
        {
            Handles.DrawLine(mainScript.mapBoundsMin, new Vector3(mainScript.mapBoundsMax.x, mainScript.mapBoundsMin.y));
            Handles.DrawLine(mainScript.mapBoundsMin, new Vector3(mainScript.mapBoundsMin.x, mainScript.mapBoundsMax.y));

            Handles.DrawLine(mainScript.mapBoundsMax, new Vector3(mainScript.mapBoundsMax.x, mainScript.mapBoundsMin.y));
            Handles.DrawLine(mainScript.mapBoundsMax, new Vector3(mainScript.mapBoundsMin.x, mainScript.mapBoundsMax.y));
        }
    }

    public void Update()
    {
        if (!Application.isPlaying)
            return;

        if (!CacheMainScript())
            return;

        if (mainScript.player.transform && !mainScript.player.transform.gameObject.activeSelf)
        {
            Repaint();
        }
    }

    public void OnGUI()
    {
        if (!CacheMainScript())
            return;

        bool bSceneViewDirty = false;

        if (alwaysDrawGizmos != GUILayout.Toggle(alwaysDrawGizmos, "Draw Gizmos"))
        {
            alwaysDrawGizmos = !alwaysDrawGizmos;
            bSceneViewDirty = true;
        }

        //Player GO
        {
            if (!mainScript.player.IsValid())
            {
                EditorGUILayout.HelpBox("Player not setup", MessageType.Error);
            }
            GameObject playerGO = null;
            if (mainScript.player.transform)
            {
                playerGO = mainScript.player.transform.gameObject;
            }
            playerGO = (GameObject)EditorGUILayout.ObjectField("Player", playerGO, typeof(GameObject), true);
            if (playerGO != null)
            {
                Undo.RecordObject(mainScript, "Set Player GameObject");
                mainScript.player = new UnitEntity(playerGO, mainScript.player, new ID { type = IDType.Player });
            }

            if (!playerGO.activeSelf)
            {
                if (GUILayout.Button("Respawn player"))
                {
                    mainScript.player.health.current = mainScript.player.health.max;
                    playerGO.transform.position = Vector3.zero;
                    playerGO.SetActive(true);
                }
            }
        }

        if (!mainScript.animationSystem.IsValid())
        {
            EditorGUILayout.HelpBox("AnimationSystem arrays not correct", MessageType.Error);
            if (GUILayout.Button("Correct AnimationSystem arrays"))
            {
                Undo.RecordObject(mainScript, "Correct AnimationSystem arrays");
                mainScript.animationSystem.Validate();
            }
        }

        if (!mainScript.pickupSystem.IsValid())
        {
            EditorGUILayout.HelpBox("PickupSystem arrays not correct", MessageType.Error);
            if (GUILayout.Button("Correct PickupSystem arrays"))
            {
                Undo.RecordObject(mainScript, "Correct PickupSystem arrays");
                mainScript.pickupSystem.Validate();
            }
        }

        bool isIDValid = true;

        SerializedObject so = new SerializedObject(mainScript);
        EditorGUI.BeginChangeCheck();
        switch (selectedID.type)
        {
            default: isIDValid = false; break;

            case IDType.LightCrystal:
                if (mainScript.lightCrystals.IsValidID(selectedID))
                {
                    SerializedProperty listsp = so.FindProperty("lightCrystals.elements");
                    EditorGUILayout.PropertyField(listsp.GetArrayElementAtIndex(selectedID.index), true);
                }
                break;
            case IDType.Pickup:
                break;
        }
        if (EditorGUI.EndChangeCheck())
        {
            so.ApplyModifiedProperties();
        }

        if (!isIDValid)
        {
            using (EditorGUI.DisabledScope ds = new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(Selection.activeGameObject, typeof(GameObject), true);
            }

            if (GUILayout.Button("Add GO as light crystal entity"))
            {
                Undo.RecordObject(mainScript, "Add GO as light crystal entity");
                selectedID = mainScript.lightCrystals.Spawn();
                mainScript.lightCrystals[selectedID.index] = new LightCrystal(
                    Selection.activeGameObject,
                    mainScript.lightCrystalSpawning.presetCrystal,
                    selectedID);
            }
        }

        if (bSceneViewDirty)
            SceneView.RepaintAll();
    }
}
#endif