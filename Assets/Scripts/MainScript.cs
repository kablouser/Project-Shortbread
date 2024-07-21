using System.Collections.Generic;
using UnityEngine;

public class MainScript : MonoBehaviour
{
    public Camera mainCamera;
    public InputSystem inputSystem;
    public AnimationSystem animationSystem;
    public AttackSystem attackSystem;
    public NavigationGrid navigationGrid;
    public UnitEntity player;
    public VersionedPool<UnitEntity> enemies;
    public EnemyData[] enemiesSpawnRates;
    public CentreLight centreLight;

    [Header("Game Settings")]
    public float timeToSurvive = 900f;

    public float currentTime = 0f;

    public Vector2 mapBoundsMax = Vector2.zero;
    public Vector2 mapBoundsMin = Vector2.zero;
    public float centreRadius = 1f;
    public float spawnOutsideCameraDistance = 0.1f;

    public PlayerControls playerControls;

    public bool isGameOver = false;

    public void Awake()
    {
        playerControls = new PlayerControls();
    }

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

        // Light Bar Setup
        centreLight.currentPower = centreLight.maxPower;
        centreLight.uiPowerBar.maxValue = centreLight.maxPower;
        centreLight.uiPowerBar.value = centreLight.maxPower;
    }

    public void OnValidate()
    {
        enemies.type = IDType.Enemy;
        attackSystem.defaultProjectile.pool.type = IDType.ProjectileDefault;
    }

    public void OnEnable()
    {
        playerControls.Enable();
    }

    public void OnDisable()
    {
        playerControls.Disable();
    }

    // perf markers
    //static readonly ProfilerMarker s_PreparePerfMarker = new ProfilerMarker("MainScript");

    public void Update()
    {
        if(isGameOver)
        {
            return;
        }

        mainCamera.transform.position = player.rigidbody.position;
        inputSystem.Update(this);
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
                    switch (SelectedSide)
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

                    ID id = enemies.Spawn();
                    ref UnitEntity enemy = ref enemies[id.index];

                    if (enemy.IsValid())
                    {
                        enemy = new UnitEntity(enemy.transform.gameObject, enemiesSpawnRates[i].presetUnit, id);
                        enemy.transform.position = spawnPosition;
                        enemy.transform.gameObject.SetActive(true);
                    }
                    else
                    {
                        enemy = new UnitEntity(
                            Instantiate(enemiesSpawnRates[i].enemyPrefab, spawnPosition, Quaternion.identity),
                            enemiesSpawnRates[i].presetUnit,
                            id);
                    }
                }
            }
        }

        // Light Shield
        {
            centreLight.currentPower -= centreLight.powerLossPerSecond * Time.deltaTime;

            float lightPercentLeft = centreLight.currentPower / centreLight.maxPower;
            float lightRadius = centreLight.minLightRadius + ((centreLight.maxLightRadius - centreLight.minLightRadius) * lightPercentLeft);

            centreLight.uiPowerBar.value = centreLight.currentPower;
            centreLight.light.intensity = centreLight.minLightIntensity + ((centreLight.maxLightIntensity - centreLight.minLightIntensity) * lightPercentLeft);
            centreLight.light.pointLightInnerRadius = lightRadius / 2;
            centreLight.light.pointLightOuterRadius = lightRadius;
            centreLight.collider.radius = lightRadius / 2;

            if (centreLight.currentPower <= 0f)
            {
                isGameOver = true;
                print("GAME OVER");
            }

            // Update 
        }

        attackSystem.Update(this);
    }

    public void FixedUpdate()
    {
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, player.transform.position, Time.fixedDeltaTime * 5f);
        
        if(isGameOver)
        {
            return;
        }

        inputSystem.FixedUpdate(this);
        
        // Enemy Movement
        {
            Vector3 playerPosition = player.transform.position;
            foreach(int enemyID in enemies)
            {
                Vector2 Velocity = new Vector2(playerPosition.x - enemies[enemyID].transform.position.x, playerPosition.y - enemies[enemyID].transform.position.y).normalized * enemies[enemyID].moveSpeed;
                enemies[enemyID].rigidbody.velocity = Velocity;
            }
        }
    }

    public void ProcessTriggerEnter(IDTriggerEnter source, Collider2D collider)
    {
        switch (source.id.type)
        {
            default: break;
            case IDType.ProjectileDefault:
                attackSystem.ProcessTriggerEnterEvent(this, source, collider);
                break;
        }
    }

    public ref UnitEntity GetUnit(in ID id, out bool isValid)
    {
        switch (id.type)
        {
            default: break;

            case IDType.Player:
                isValid = true;
                return ref player;

            case IDType.Enemy:
                if (enemies.IsValidID(id))
                {
                    isValid = true;
                    return ref enemies[id.index];
                }
                break;
        }

        isValid = false;
        return ref player;
    }
}
