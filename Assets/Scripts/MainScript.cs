using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainScript : MonoBehaviour
{
    public Camera mainCamera;
    public AnimationSystem animationSystem;
    public AttackSystem attackSystem;
    public NavigationGrid navigationGrid;
    public UnitEntity player;
    public VersionedPool<UnitEntity> enemies;

    public void Start()
    {
        // not just transparency, all sprites are sorted lowerest y first, then lowest x
        mainCamera.transparencySortMode = TransparencySortMode.CustomAxis;
        mainCamera.transparencySortAxis = new Vector3(0.2f, 1.0f, 0.0f);

        QualitySettings.vSyncCount = 1;
#if UNITY_EDITOR
        // sync doesn't work in editor
        Application.targetFrameRate = 60;
        // stress test at lowest FPS possible
        //Application.targetFrameRate = Mathf.FloorToInt(1f / Time.maximumDeltaTime);
#endif
    }

    public void OnValidate()
    {
        enemies.type = IDType.Enemy;
    }

    // perf markers
    //static readonly ProfilerMarker s_PreparePerfMarker = new ProfilerMarker("MainScript");

    public void Update()
    {
        mainCamera.transform.position = player.rigidbody.position;
        InputSystem.Update(this);
        animationSystem.Update(this);
    }

    public void FixedUpdate()
    {
        InputSystem.FixedUpdate(this);
    }
}
