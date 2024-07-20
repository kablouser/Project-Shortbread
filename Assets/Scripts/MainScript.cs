using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderKeywordFilter;

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
    public CentreLight centreLight;

    [Header("Game Settings")]
    public float timeToSurvive = 900f;

    [HideInInspector]
    public float currentTime = 0f;
    public Vector2 mapBoundsMax = Vector2.zero;
    public Vector2 mapBoundsMin = Vector2.zero;
    public float centreRadius = 1f;
    public float spawnOutsideCameraDistance = 0.1f;

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
            List<int> SpawnPoints = new List<int> { 0, 1, 2, 3 };

            // Camera Bounds
            Vector3 cameraPosition = mainCamera.transform.position;
            float verticleExtent = mainCamera.orthographicSize;
            float horizontalExtent = verticleExtent * Screen.width / Screen.height;
            Vector2 CameraMin = new Vector2(cameraPosition.x - horizontalExtent - spawnOutsideCameraDistance, cameraPosition.y - verticleExtent - spawnOutsideCameraDistance);
            Vector2 CameraMax = new Vector2(cameraPosition.x + horizontalExtent + spawnOutsideCameraDistance, cameraPosition.y + verticleExtent + spawnOutsideCameraDistance);

            if(CameraMin.x <= mapBoundsMin.x)
            {
                SpawnPoints.Remove(0);
            }

            if(CameraMax.x >= mapBoundsMax.x)
            {
                SpawnPoints.Remove(1);
            }

            if(CameraMin.y <= mapBoundsMin.y)
            {
                SpawnPoints.Remove(2);
            }

            if(CameraMax.y >= mapBoundsMax.y)
            {
                SpawnPoints.Remove(3);
            }

            for (int i = 0; i < enemiesSpawnRates.Length; i++)
            {
                // Get the number of enemies that should be spawned
                enemiesSpawnRates[i].currentNumberToSpawn += (enemiesSpawnRates[i].spawnRate.Evaluate(currentTime/timeToSurvive)) * Time.deltaTime;
                int numberOfEnemiesToSpawn = Mathf.FloorToInt(enemiesSpawnRates[i].currentNumberToSpawn);
                enemiesSpawnRates[i].currentNumberToSpawn -= numberOfEnemiesToSpawn;

                // Dont try to spawn if no point avaliable
                if(SpawnPoints.Count <= 0)
                {
                    continue;
                }

                for (int j = 0; j < numberOfEnemiesToSpawn; j++)
                {
                    // Get a random point along the selected side
                    Vector2 spawnPosition = new Vector2(0, 0);
                    int SelectedSide = SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Count - 1)];
                    switch(SelectedSide)
                    {
                        case 0: // Left edge
                            spawnPosition.x = CameraMin.x;
                            spawnPosition.y = UnityEngine.Random.Range(CameraMin.y, CameraMax.y);
                            break;
                        case 1: // Right edge
                            spawnPosition.x = CameraMax.x;
                            spawnPosition.y = UnityEngine.Random.Range(CameraMin.y, CameraMax.y);
                            break;
                        case 2: // Bottom edge
                            spawnPosition.x = UnityEngine.Random.Range(CameraMin.x, CameraMax.x);
                            spawnPosition.y = CameraMin.y;
                            break;
                        case 3: // Top edge
                            spawnPosition.x = UnityEngine.Random.Range(CameraMin.x, CameraMax.x);
                            spawnPosition.y = CameraMax.y;
                            break;
                    }

                    enemies.Spawn(new UnitEntity
                        (
                        Instantiate(enemiesSpawnRates[i].enemyPrefab, spawnPosition, Quaternion.identity),
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
            Vector3 playerPosition = player.transform.position;
            foreach(int enemyID in enemies)
            {
                Vector2 Velocity = new Vector2(playerPosition.x - enemies[enemyID].transform.position.x, playerPosition.y - enemies[enemyID].transform.position.y).normalized * enemies[enemyID].MoveSpeed;
                enemies[enemyID].rigidbody.velocity = Velocity;
            }
        }
    }
}
