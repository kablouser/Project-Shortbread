using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainScript : MonoBehaviour
{
    public Camera mainCamera;
    public TextAndSlider
        healthBar,
        ammoBar;

    public InputSystem inputSystem;
    public AnimationSystem animationSystem;
    public UpgradeSystem upgradeSystem;
    public AttackSystem attackSystem;
    public AISystem aiSystem;
    public UnitEntity player;
    public VersionedPool<UnitEntity> enemies;
    public VersionedPool<Boss0Entity> bosses0;
    public EnemyData[] enemiesSpawnRates;
    public Boss0SpawnData boss0SpawnData;
    public PlayerLight playerLight;
    public CentreLight centreLight;
    public LightCrystalSpawning lightCrystalSpawning;
    public VersionedPool<LightCrystal> lightCrystals;
    public LightShardData lightShardData;
    public IndicatorAndLocation lightCrystalIndicator;
    public IndicatorAndLocation centreIndicator;
    public IndicatorUI bossIndicatorUIPrefab;
    public GameObject IndicatorHolder;
    public PickupColor pickupColor;

    public GameTimer gameTimer;
    public GameOverScreen gameOverScreen;

    public PickupSystem pickupSystem;

    public CraftingSystem craftingSystem;

    public AudioSystem audioSystem;
    public ShakeSystem shakeSystem;

    public VersionedPool<LimbEntity> limbs;
    public GameObject limbPrefab;

    [Header("Game Settings")]
    public Vector2 mapBoundsMax = Vector2.zero;
    public Vector2 mapBoundsMin = Vector2.zero;
    public float centreRadius = 1f;

    public PlayerControls playerControls;
    public UnityEngine.EventSystems.EventSystem eventSystem;

    public GameState gameState;
    // controls what types of enemies can spawn
    public int numberBossesDefeated;

    public TMPro.TextMeshProUGUI shootTutorialText;
    public TMPro.TextMeshProUGUI barrierTutorialText;
    public TMPro.TextMeshProUGUI gameStartText;

    public Sprite enemyMeele;
    public Sprite enemyRange;
    public Sprite enemyCharger;

    public ParticleSystem enemyDamagedVFX;
    public ParticleSystem boss0DamagedVFX;
    public ParticleSystem lightCrystalDamagedVFX;
    public ParticleSystem playerDamagedVFX;

    public void Awake()
    {
        playerControls = new PlayerControls();
    }

    public void Start()
    {
        SetGameState(GameState.TutorialBlast, true);

        QualitySettings.vSyncCount = 1;
#if UNITY_EDITOR
        // sync doesn't work in editor
        Application.targetFrameRate = 60;
        // stress test at lowest FPS possible
        //Application.targetFrameRate = Mathf.FloorToInt(1f / Time.maximumDeltaTime);
#endif

        // Light Bar Setup
        centreLight.uiPowerBar.maxValue = centreLight.maxPower;
        centreLight.uiPowerBar.value = centreLight.currentPower;

        healthBar.UpdateHealthBar(player);
        ammoBar.UpdateAmmoBar(player.attack.ammoShot, attackSystem.player.fullMagazineAmmo);

        craftingSystem.Start(this);

        playerLight.light.pointLightOuterRadius = playerLight.baseLightRange;

        audioSystem.Start(this);

        // Assign Buttons
        gameOverScreen.restartButton.onClick.AddListener(() =>
        {
            // Useing this to restart the game for now
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        // tutorial light crystal and elements
        pickupSystem.SpawnPickup(PickupType.Fire, new Vector2(-0.5f, -9), 1f);
        pickupSystem.SpawnPickup(PickupType.Earth, new Vector2(1, -10f), 1f);
        pickupSystem.SpawnPickup(PickupType.Air, new Vector2(-1, -10f), 1f);
        pickupSystem.SpawnPickup(PickupType.Water, new Vector2(0.5f, -9), 1f);
        SpawnLightCrystal(new Vector2(0.7f, -5));

        shakeSystem.Start(this);
    }

    public void OnDestroy()
    {
        shakeSystem.OnDestroy();
    }

    public void OnValidate()
    {
        enemies.type = IDType.Enemy;
        bosses0.type = IDType.Boss0;
        limbs.type = IDType.Limb;
        attackSystem.defaultProjectile.pool.type = IDType.ProjectileDefault;
        if (player.transform)
        {
            var idComp = player.transform.GetComponent<IDComponent>();
            if (idComp)
            {
                idComp.id.type = IDType.Player;
            }
        }
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
        audioSystem.Update(this);

        // Time Update
        if (Time.timeScale == 0f)
        {
            // paused
            gameTimer.timerText.SetText($"{gameTimer.GetTimeLeftString()}\nPAUSED");
            inputSystem.Update(this);
            // crafting relies on inputSystem update first
            craftingSystem.Update(this);
            // rumble and screenshake needs to run
            shakeSystem.UpdateEvenWhenPaused(this, Time.unscaledDeltaTime /*please use unscaled*/);
            return;
        }
        else if (gameState == GameState.Survive)
        {
            // survive countdown
            gameTimer.currentTime += Time.deltaTime;
            if (gameTimer.currentTime >= gameTimer.timeToSurvive)
            {
                SetGameState(GameState.Win);
            }

            gameTimer.timerText.SetText(gameTimer.GetTimeLeftString());
        }

        // is player alive
        if (player.transform.gameObject.activeInHierarchy)
        {
            inputSystem.Update(this);
            // crafting relies on inputSystem update first
            craftingSystem.Update(this);

            ReplenishCrystalsNumber();

            // Health regen
            {
                gameTimer.healthRegenAccum += Time.deltaTime * player.statSheet.healthRegenPerMinute.Calculate() / 60f;

                if (0f < gameTimer.healthRegenAccum)
                {
                    int gain = Mathf.FloorToInt(gameTimer.healthRegenAccum);
                    gameTimer.healthRegenAccum -= gain;

                    player.health.current += gain;
                    if (player.health.current > player.statSheet.maxHealth.Calculate())
                    {
                        player.health.current = player.statSheet.maxHealth.Calculate();
                    }
                    healthBar.UpdateHealthBar(player);
                }
            }
        }
        else if (gameState == GameState.Survive)
        {
            SetGameState(GameState.Death);
        }

        if (gameState == GameState.Survive || gameState == GameState.Death || gameState == GameState.NoPower)
        {
            // Enemy Spawning
            {
                for (int i = 0; i < enemiesSpawnRates.Length; i++)
                {
                    // Get the number of enemies that should be spawned
                    enemiesSpawnRates[i].currentNumberToSpawn += (enemiesSpawnRates[i].spawnRate.Evaluate(gameTimer.currentTime / gameTimer.timeToSurvive)) * Time.deltaTime;
                    int numberOfEnemiesToSpawn = Mathf.FloorToInt(enemiesSpawnRates[i].currentNumberToSpawn);
                    enemiesSpawnRates[i].currentNumberToSpawn -= numberOfEnemiesToSpawn;

                    for (int j = 0; j < numberOfEnemiesToSpawn; j++)
                    {
                        if (!RandomPositionAwayFromPlayer(playerLight.light.pointLightOuterRadius, out Vector2 spawnPosition, 5, playerLight.light.pointLightOuterRadius * 3.0f))
                            continue;

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

                        if (1 < numberBossesDefeated)
                        {
                            float val = Random.value;
                            if (val < 0.6f)
                                enemy.attack.variant = 0;
                            else if (val < 0.8f)
                                enemy.attack.variant = 1;
                            else
                                enemy.attack.variant = 2;
                        }
                        else if (0 < numberBossesDefeated)
                            enemy.attack.variant = Random.value < 0.8f ? 0 : 1;
                        else
                            enemy.attack.variant = 0;

                        CircleCollider2D enemyCollider = enemy.transform.GetComponent<CircleCollider2D>();
                        switch (enemy.attack.variant)
                        {
                            case 0:
                                enemy.spriteRenderer.sprite = enemyMeele;
                                enemyCollider.radius = 0.08365758f;
                                enemyCollider.offset = new Vector2(0, -0.0163253918f);
                                break;
                            case 1:
                                enemy.spriteRenderer.sprite = enemyRange;
                                enemyCollider.radius = 0.1193566f;
                                enemyCollider.offset = new Vector2(0.00247123837f, -0.0283010602f);
                                break;
                            case 2:
                                enemy.spriteRenderer.sprite = enemyCharger;
                                enemyCollider.radius = 0.1990417f;
                                enemyCollider.offset = new Vector2(0.00247123837f, -0.00133603811f);
                                break;
                        }

                    }
                }
            }

            // Boss spawning
            do
            {
                if (0 < bosses0.CountUsing())
                    break;

                boss0SpawnData.spawnTimeLeft -= Time.deltaTime;
                if (0 < boss0SpawnData.spawnTimeLeft)
                {
                    break;
                }

                boss0SpawnData.spawnTimeLeft = boss0SpawnData.timeBetweenSpawns;

                List<int> elementTypeList = new() { 1, 2, 3, 4};
                List<Vector2> spawnedPositions = new();
                for(int i = 0; i < 3; i++)
                {
                    if (!RandomPositionAwayFromPlayer(boss0SpawnData.minDistanceToPlayer, out Vector2 randomPosition, 10, -1, spawnedPositions))
                        continue;
                    spawnedPositions.Add(randomPosition);

                    int randomElementIndex = Random.Range(0, elementTypeList.Count);
                    PickupType elementType = (PickupType)elementTypeList[randomElementIndex];
                    elementTypeList.RemoveAt(randomElementIndex);

                    ID id = bosses0.Spawn();
                    ref Boss0Entity boss = ref bosses0[id.index];

                    if (0 < numberBossesDefeated)
                        // limbed boss
                        boss.unit.attack.variant = Random.value < 0.6f ? 0 : 1;
                    else
                        boss.unit.attack.variant = 0;

                    int saveVariant = boss.unit.attack.variant;
                    Boss0Entity presetUnit = (boss.unit.attack.variant == 0) ? (boss0SpawnData.presetUnit) : (boss0SpawnData.limbsBossPreset);

                    if (boss.unit.IsValid())
                    {
                        boss = new Boss0Entity(boss.unit.transform.gameObject, presetUnit, id, boss.bossIndicator, saveVariant);
                        boss.unit.transform.position = randomPosition;
                        boss.unit.transform.gameObject.SetActive(true);
                    }
                    else
                    {
                        boss = new Boss0Entity(
                            Instantiate(boss0SpawnData.prefab, randomPosition, Quaternion.identity),
                            presetUnit,
                            id,
                            Instantiate(bossIndicatorUIPrefab, IndicatorHolder.transform), saveVariant);
                    }

                    boss.UpdatePickupType(elementType, this);

                    if (boss.unit.attack.variant == 1)
                    {
                        // spawn limbs
                        for (int limbI = 0; limbI < 3; limbI++)
                        {
                            ID limbID = limbs.Spawn();

                            ref LimbEntity limb = ref limbs[limbID.index];
                            if (limb.IsValid())
                            {
                                limb = new LimbEntity(limbID, id, limb.go, 9, pickupColor.GetColor(elementType), this, limbI);
                                limb.go.SetActive(true);
                            }
                            else
                            {
                                limb = new LimbEntity(limbID, id, Instantiate(limbPrefab), 9, pickupColor.GetColor(elementType), this, limbI);
                            }
                            boss.SetLimb(limbI, limbID);
                        }
                        
                    }
                }

                // despawn all crystals
                foreach (int i in lightCrystals)
                {
                    lightCrystals[i].transform.gameObject.SetActive(false);
                    lightCrystals.TryDespawn(i);
                }
            }
            while (false);
        }

        if (gameState == GameState.Survive)
        {
            // Light Shield drain
            centreLight.currentPower -= centreLight.powerLossPerSecond * Time.deltaTime;
        }

        // Light Shield update
        {
            float lightPercentLeft = centreLight.currentPower / centreLight.maxPower;
            float lightRadius = centreLight.minLightRadius + ((centreLight.maxLightRadius - centreLight.minLightRadius) * lightPercentLeft);

            centreLight.uiPowerBar.value = centreLight.currentPower;
            centreLight.light.intensity = centreLight.minLightIntensity + ((centreLight.maxLightIntensity - centreLight.minLightIntensity) * lightPercentLeft);
            centreLight.light.pointLightInnerRadius = lightRadius / 2;
            centreLight.light.pointLightOuterRadius = lightRadius;
            centreLight.collider.radius = lightRadius / 2;

            if (centreLight.currentPower <= 0f)
            {
                SetGameState(GameState.NoPower);
            }
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

            // Update all boss indicators
            foreach (int Index in bosses0)
            {
                bosses0[Index].bossIndicator.distance = Vector2.SqrMagnitude(player.rigidbody.position - (Vector2)bosses0[Index].unit.transform.position);
                bosses0[Index].bossIndicator.position = bosses0[Index].unit.transform.position;
                bosses0[Index].bossIndicator.Update(this);
            }
        }

        attackSystem.Update(this);

        //animationSystem.Update(this);
    }

    public void FixedUpdate()
    {
        shakeSystem.UpdateEvenWhenPaused(this, Time.fixedDeltaTime);

        aiSystem.FixedUpdate(this);

        // is player alive
        if (player.transform.gameObject.activeInHierarchy)
        {
            inputSystem.FixedUpdate(this);
            pickupSystem.FixedUpdate(this);
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
        if (gameState == GameState.NoPower)
            return;

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
            case IDType.Limb:
                return Team.Enemy;
        }
    }

    public static bool IsOppositeTeams(IDType a, IDType b)
    {
        return GetTeam(a) != GetTeam(b);
    }

    public bool RandomPositionAwayFromPlayer(
        float minDistanceToPlayer, out Vector2 randomPosition,
        int maxAttempts = 5, float maxDistanceAway = -1f, List<Vector2> dontInclude = null)
    {
        int attempts = 0;
        Vector2 playerPos = player.transform.position;
        do
        {
            randomPosition = new Vector2(
                Random.Range(mapBoundsMin.x, mapBoundsMax.x),
                Random.Range(mapBoundsMin.y, mapBoundsMax.y));
            float distanceAway = Vector2.Distance(randomPosition, playerPos);
            if (dontInclude != null)
                foreach (ref Vector2 dontIncludeV in dontInclude.AsSpan())
                {
                    distanceAway = Mathf.Min(distanceAway, Vector2.Distance(randomPosition, dontIncludeV));
                }

            if (minDistanceToPlayer < distanceAway)
            {
                if (0f < maxDistanceAway && maxDistanceAway < distanceAway)
                {
                    randomPosition = playerPos + (randomPosition - playerPos) * maxDistanceAway / distanceAway;
                }

                return true;
            }
        } while (++attempts < maxAttempts);
        return false;
    }

    public void ReplenishCrystalsNumber()
    {
        if (0 < bosses0.CountUsing() ||
            gameState != GameState.Survive)
            return;

        int crystalsToSpawn = lightCrystalSpawning.GetCrystalsNumber(mapBoundsMin, mapBoundsMax);
        for (int i = lightCrystals.CountUsing(); i < crystalsToSpawn; i++)
        {
            if (!RandomPositionAwayFromPlayer(lightCrystalSpawning.minDistanceToPlayer, out Vector2 spawnPosition))
                continue;

            SpawnLightCrystal(spawnPosition);
        }
    }

    public void SpawnLightCrystal(Vector2 spawnPosition)
    {
        ID id = lightCrystals.Spawn();
        ref LightCrystal crystal = ref lightCrystals[id.index];

        if (crystal.IsValid())
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

    public void SetGameState(GameState newState, bool isStart = false)
    {
        if (gameState == newState && !isStart)
            return;

        // old state
        switch(gameState)
        {
            case GameState.TutorialBlast:
                shootTutorialText.gameObject.SetActive(false);
                centreLight.uiPowerBar.gameObject.SetActive(true);
                break;

            case GameState.TutorialBarrierPower:
                barrierTutorialText.enabled = false;
                break;

            case GameState.Survive:
                gameStartText.enabled = false;
                break;

            case GameState.Win:
            case GameState.Death:
            case GameState.NoPower:
                craftingSystem.ExitMenu(this);
                gameOverScreen.Enable(this);
                break;
        }

        gameState = newState;

        // new
        switch (newState)
        {
            case GameState.TutorialBlast:
                shootTutorialText.gameObject.SetActive(true);
                centreLight.uiPowerBar.gameObject.SetActive(false);
                break;

            case GameState.TutorialBarrierPower:
                barrierTutorialText.enabled = true;
                barrierTutorialText.alpha = 1.0f;
                barrierTutorialText.CrossFadeAlpha(0f, 5.0f, true);
                break;


            case GameState.Survive:
                gameStartText.enabled = true;
                gameStartText.alpha = 1.0f;
                gameStartText.CrossFadeAlpha(0.0f, 3.0f, true);
                break;

            case GameState.Win:
            case GameState.Death:
            case GameState.NoPower:
                craftingSystem.ExitMenu(this);
                gameOverScreen.Enable(this);
                break;
        }
    }

    public void ExitCraftingMenu()
    {
        craftingSystem.ExitMenu(this);
    }

    public void SpawnHitVFX(IDType idType, Vector3 position)
    {
        switch (idType)
        {
            case IDType.Player:
                Instantiate(playerDamagedVFX, position, Quaternion.identity);
                break;
            case IDType.LightCrystal:
                Instantiate(lightCrystalDamagedVFX, position, Quaternion.identity);
                break;
            case IDType.Enemy:
                Instantiate(enemyDamagedVFX, position, Quaternion.identity);
                break;
            case IDType.Limb:
            case IDType.Boss0:
                Instantiate(boss0DamagedVFX, position, Quaternion.identity);
                break;
        }
    }
}
