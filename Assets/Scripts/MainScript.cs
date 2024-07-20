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
    public EnemyData[] enemiesSpawnRates;

    [Header("Game Settings")]
    public float timeToSurvive = 900f;

    [HideInInspector]
    public float currentTime = 0f;
    public Vector2 spawnArea = Vector2.zero;
    public float centreRadius = 1f;

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

        // Time Update
        {
            currentTime += Time.deltaTime;
        }

        // Enemy Spawning
        {
            for(int i = 0; i < enemiesSpawnRates.Length; i++)
            {
                // Get the number of enemies that should be spawned
                enemiesSpawnRates[i].currentNumberToSpawn += (enemiesSpawnRates[i].spawnRate.Evaluate(currentTime/timeToSurvive)) * Time.deltaTime;
                int NumberOfEnemiesToSpawn = Mathf.FloorToInt(enemiesSpawnRates[i].currentNumberToSpawn);
                enemiesSpawnRates[i].currentNumberToSpawn -= NumberOfEnemiesToSpawn;

                for(int j = 0; j < NumberOfEnemiesToSpawn; j++)
                {
                    // Get spawn position (still working on)
                    Vector2 SpawnPosition = new Vector2(1, 1);

                    enemies.Spawn(new UnitEntity
                        (
                        Instantiate(enemiesSpawnRates[i].enemyPrefab, SpawnPosition, Quaternion.identity),
                        enemiesSpawnRates[i].animationComponent,
                        enemiesSpawnRates[i].attackComponent,
                        enemiesSpawnRates[i].moveSpeed)
                        );
                }
            }
        }
    }

    public void FixedUpdate()
    {
        InputSystem.FixedUpdate(this);

        // Enemy Movement
        {
            Vector3 PlayerTransform = player.transform.position;
            foreach(int EnemyID in enemies)
            {
                print("EnemyID: " + EnemyID);
                Vector2 Velocity = new Vector2(PlayerTransform.x - enemies[EnemyID].transform.position.x, PlayerTransform.y - enemies[EnemyID].transform.position.y).normalized * enemies[EnemyID].MoveSpeed;
                enemies[EnemyID].rigidbody.velocity = Velocity;
                print("EnemyID: " + EnemyID + "MovementVelocity: " + Velocity);
            }
        }
    }
}
