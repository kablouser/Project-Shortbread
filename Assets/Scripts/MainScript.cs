using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainScript : MonoBehaviour
{
    public Camera mainCamera;
    public TextAndSlider
        healthBar,
        ammoBar;

    public InputSystem inputSystem;
    public AnimationSystem animationSystem;
    public AttackSystem attackSystem;
    public AISystem aiSystem;
    public NavigationGrid navigationGrid;
    public UnitEntity player;
    public VersionedPool<UnitEntity> enemies;
    public VersionedPool<Boss0Entity> bosses0;
    public EnemyData[] enemiesSpawnRates;
    public Boss0SpawnData boss0SpawnData;
    public CentreLight centreLight;
    public LightCrystalSpawning lightCrystalSpawning;
    public VersionedPool<LightCrystal> lightCrystals;
    public IndicatorAndLocation lightCrystalIndicator;
    public IndicatorAndLocation centreIndicator;


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

        // Light Crystal Spawning
        {
            for(int i = 0; i < lightCrystalSpawning.crystalsToSpawn; i++)
            {
                Vector2 spawnPosition = new Vector2(UnityEngine.Random.Range(mapBoundsMin.x, mapBoundsMax.x), UnityEngine.Random.Range(mapBoundsMin.y, mapBoundsMax.y));

                ID id = lightCrystals.Spawn();
                ref LightCrystal crystal = ref lightCrystals[id.index];

                if(crystal.IsValid())
                {
                    crystal = new LightCrystal(crystal.transform.gameObject, lightCrystalSpawning.presetCrystal, id);
                    crystal.transform.position = spawnPosition;
                    crystal.transform.gameObject.SetActive(true);
                }
                else
                {
                    crystal = new LightCrystal(
                            Instantiate(lightCrystalSpawning.crystalPrefab, spawnPosition, Quaternion.identity),
                            lightCrystalSpawning.presetCrystal,
                            id);
                }
            }
        }
        
        healthBar.UpdateHealthBar(player.health);
        ammoBar.UpdateAmmoBar(player.attack.ammoShot, attackSystem.player.fullMagazineAmmo);
    }

    public void OnValidate()
    {
        enemies.type = IDType.Enemy;
        bosses0.type = IDType.Boss0;
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

            if (CameraMin.x <= mapBoundsMin.x)
            {
                SpawnPoints.Remove(0);
            }

            if (CameraMax.x >= mapBoundsMax.x)
            {
                SpawnPoints.Remove(1);
            }

            if (CameraMin.y <= mapBoundsMin.y)
            {
                SpawnPoints.Remove(2);
            }

            if (CameraMax.y >= mapBoundsMax.y)
            {
                SpawnPoints.Remove(3);
            }

            for (int i = 0; i < enemiesSpawnRates.Length; i++)
            {
                // Get the number of enemies that should be spawned
                enemiesSpawnRates[i].currentNumberToSpawn += (enemiesSpawnRates[i].spawnRate.Evaluate(currentTime / timeToSurvive)) * Time.deltaTime;
                int numberOfEnemiesToSpawn = Mathf.FloorToInt(enemiesSpawnRates[i].currentNumberToSpawn);
                enemiesSpawnRates[i].currentNumberToSpawn -= numberOfEnemiesToSpawn;

                // Dont try to spawn if no point avaliable
                if (SpawnPoints.Count <= 0)
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

        // Boss spawning
        while (true)
        {
            // Get the number of enemies that should be spawned
            int spawnRequirement = Mathf.FloorToInt(boss0SpawnData.spawnRate.Evaluate(currentTime / timeToSurvive));
            if (spawnRequirement <= boss0SpawnData.numberSpawned)
                break;

            int numberToSpawn = spawnRequirement - boss0SpawnData.numberSpawned;
            boss0SpawnData.numberSpawned = spawnRequirement;

            for (int j = 0; j < numberToSpawn; j++)
            {
                int attempts = 0;
                Vector2 randomPosition;
                do
                {
                    randomPosition = new Vector2(
                        Random.Range(mapBoundsMin.x, mapBoundsMax.x),
                        Random.Range(mapBoundsMin.y, mapBoundsMax.y));

                    if (boss0SpawnData.minDistanceToPlayer <
                        Vector2.Distance(randomPosition, player.transform.position))
                        break;

                } while (++attempts < 5);

                ID id = bosses0.Spawn();
                ref Boss0Entity boss = ref bosses0[id.index];

                if (boss.unit.IsValid())
                {
                    boss = new Boss0Entity(boss.unit.transform.gameObject, boss0SpawnData.presetUnit, id);
                    boss.unit.transform.position = randomPosition;
                    boss.unit.transform.gameObject.SetActive(true);
                }
                else
                {
                    boss = new Boss0Entity(
                        Instantiate(boss0SpawnData.prefab, randomPosition, Quaternion.identity),
                        boss0SpawnData.presetUnit,
                        id);
                }
            }

            break;
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
            }

            // Update
        }

        // Indicator Updates
        {
            centreIndicator.Update(this);

            // Find closest light crystal
            lightCrystalIndicator.distance = float.PositiveInfinity;
            foreach (int Index in lightCrystals)
            {
                float TempDistance = Vector2.SqrMagnitude(player.rigidbody.position - (Vector2)lightCrystals[Index].transform.position);
                if (TempDistance < lightCrystalIndicator.distance)
                {
                    lightCrystalIndicator.distance = TempDistance;
                    lightCrystalIndicator.position = lightCrystals[Index].transform.position;
                }
            }

            lightCrystalIndicator.Update(this);
        }

        attackSystem.Update(this);
    }

    public void FixedUpdate()
    {
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, player.transform.position, Time.fixedDeltaTime * 5f);

        inputSystem.FixedUpdate(this);
        aiSystem.FixedUpdate(this);
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

            case IDType.Boss0:
                if (bosses0.IsValidID(id))
                {
                    isValid = true;
                    return ref bosses0[id.index].unit;
                }
                break;
        }

        isValid = false;
        return ref player;
    }

    public void AddLightPower(float amount)
    {
        centreLight.currentPower += amount;
        if(centreLight.currentPower > centreLight.maxPower)
        {
            centreLight.currentPower = centreLight.maxPower;
        }
    }

    public static Team GetTeam(IDType type)
    {
        switch (type)
        {
            default:
            case IDType.Invalid:
            case IDType.ProjectileDefault:
                return Team.Neutral;

            case IDType.Player:
                return Team.Player;

            case IDType.Enemy:
            case IDType.Boss0:
                return Team.Enemy;
        }
    }

    public static bool IsOppositeTeams(IDType a, IDType b)
    {
        return GetTeam(a) != GetTeam(b);
    }
}
