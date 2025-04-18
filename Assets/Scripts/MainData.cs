using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;

public enum Team { Neutral, Player, Enemy };

public enum SpriteSheetIndex
{
    Player,
    Enemy,
    LightCrystal,
    Boss0
};

public enum AnimationClipIndex
{
    Stand,
    Walk,
    Attack,
};

[System.Serializable]
public struct SpriteSheet
{
#if UNITY_EDITOR
    [TextArea(0, 3)]
    public string name;
#endif

    public List<Sprite> spriteSheet;
}

[System.Serializable]
public struct AnimationClip
{
#if UNITY_EDITOR
    [TextArea(0, 3)]
    public string name;
#endif

    public int startIndex;
    public int endIndex;
    public bool isRepeat;
    // special event at this frame
    public int eventIndex;

    public readonly int GetFrameCount => endIndex - startIndex + 1;
    public readonly bool IsValid => startIndex <= endIndex;
}

[System.Serializable]
public struct AnimationComponent
{
    public SpriteSheetIndex spriteSheetIndex;
    public AnimationClipIndex animationClipIndex;
    public int currentIndex;
}

[System.Flags]
public enum AnimationEventFlags
{
    None,
    ClipEnd,
    Special,
}

[System.Serializable]
public struct AnimationEvent
{
    public ID id;
    public AnimationClipIndex clip;
    public AnimationEventFlags flags;
}

public enum ProjectileType
{
    Default,
}

[System.Serializable]
public struct AttackComponent
{
    public bool isAttacking;
    // can start attack at 0
    public float attackCooldown;
    public int ammoShot;
    public float reloadCooldown;

    // freeze during melee attacks, deal damage at end
    public float meleeAttackTime;
    public ID singleTarget;

    public int variant;
    public float attackDelay;

    // charger
    public Vector2 chargeDirection;
    public float chargeDistanceLeft;
    public bool hasHit;

    // limbs
    public int limbShotI;

    public bool Reload(in ProjectileAttackPreset preset, StatsModifierComponent stats)
    {
        if (0 < ammoShot)
        {
            reloadCooldown = preset.reloadCooldown * stats.reloadSpeedModifier;
            return true;
        }
        return false;
    }
}

public enum EnemyVariants { Melee, Ranged, Charger };
public enum BossVariants { Ranged };

[System.Serializable]
public struct HealthComponent
{
    public int baseHealth;
    public int current;
    public int max;
}

[System.Serializable]
public struct UnitEntity
{
    public Transform transform;
    public Rigidbody2D rigidbody;
    public SpriteRenderer spriteRenderer;
    public float rotationDegrees;
    public AnimationComponent animation;
    public AttackComponent attack;
    public float baseMoveSpeed;
    public float moveSpeed;
    public HealthComponent health;
    public float lightPower;
    public StatsModifierComponent statModifiers;

    public UnitEntity(
        GameObject go,
        in UnitEntity template,
        ID id)
    {
        this = template;
        transform = go.transform;
        rigidbody = go.GetComponent<Rigidbody2D>();
        spriteRenderer = go.GetComponent<SpriteRenderer>();
        go.GetComponent<IDComponent>().id = id;

        rigidbody.constraints &= ~RigidbodyConstraints2D.FreezePosition;

        // Set stats
        health.max = Mathf.FloorToInt(health.baseHealth * statModifiers.healthModifier);
        health.current = health.max;
        moveSpeed = baseMoveSpeed * statModifiers.moveSpeedModifier;
    }

    public bool IsValid()
    {
        return transform != null &&
            rigidbody != null &&
            spriteRenderer != null;
    }

    public void SetRotationDegrees(in Vector3 forward)
    {
        rotationDegrees = Vector3.SignedAngle(Vector3.up, forward, Vector3.forward);
    }
}

[System.Serializable]
public struct EnemyData
{
    public GameObject enemyPrefab;
    public UnitEntity presetUnit;

    // Number of enemies to spawn per second over the game time.
    public AnimationCurve spawnRate;
    public float currentNumberToSpawn;
}

[System.Serializable]
public struct Boss0SpawnData
{
    public GameObject prefab;
    public Boss0Entity presetUnit;
    public Boss0Entity limbsBossPreset;

    public float spawnTimeLeft;
    public float timeBetweenSpawns;
    // min distance gap to player when spawned
    public float minDistanceToPlayer;

    [Header("Drops")]
    public int dropResources;
    [Header("Drop weightings")]
    public float fire;
    public float earth;
    public float air;
    public float water;
}

[System.Serializable]
public struct PlayerLight
{
    public Light2D light;
    public float baseLightRange;
}

[System.Serializable]
public struct CentreLight
{
    public Light2D light;
    public CircleCollider2D collider;

    public Slider uiPowerBar;

    public float maxLightRadius;
    public float minLightRadius;
    public float maxLightIntensity;
    public float minLightIntensity;
    public float powerLossPerSecond;
    public float maxPower;

    public float currentPower;
}

[System.Serializable]
public struct LightCrystalSpawning
{
    public float crystalsToSpawnPerArea;
    public GameObject crystalPrefab;
    public LightCrystal presetCrystal;
    public float minDistanceToPlayer;

    // map area * crystalsToSpawnPerArea
    public int GetCrystalsNumber(in Vector2 mapBoundsMin, in Vector2 mapBoundsMax)
    {
        Vector2 mapArea = mapBoundsMax - mapBoundsMin;
        return Mathf.RoundToInt(crystalsToSpawnPerArea * mapArea.x * mapArea.y);
    }
}

[System.Serializable]
public struct LightCrystal
{
    public Transform transform;
    public HealthComponent health;
    public float lightPower;
    public SpriteRenderer spriteRenderer;
    public AnimationComponent animation;

    public LightCrystal(
        GameObject go,
        in LightCrystal template,
        ID id)
    {
        this = template;
        transform = go.transform;
        spriteRenderer = go.GetComponent<SpriteRenderer>();
        go.GetComponent<IDComponent>().id = id;
    }

    public bool IsValid()
    {
        return transform != null &&
            spriteRenderer != null;
    }
}

[System.Serializable]
public struct LightShardData
{
    public float maxPowerPerShard;
}

[System.Serializable]
public struct TextAndSlider
{
    public TextMeshProUGUI text;
    public Slider slider;

    public void UpdateHealthBar(in HealthComponent health)
    {
        UpdateBar("Health:", health.current, health.max);
    }

    public void UpdateAmmoBar(int ammoShot, int ammoCapacity)
    {
        UpdateBar("Blasts:", ammoCapacity - ammoShot, ammoCapacity);
    }

    public void UpdateBar(string labelPrefix, int currentValue, int maxValue)
    {
        slider.value = currentValue / (float)maxValue;
        text.text = $"{labelPrefix}{currentValue}/{maxValue}";
    }
}

[System.Serializable]
public struct IndicatorAndLocation
{
    public RectTransform indicatorHolder;
    public RectTransform indicatorArrowTransform;
    public Image indicatorImage;
    public Vector2 position;
    public float distance;

    public IndicatorAndLocation(IndicatorUI indicator)
    {
        indicatorHolder = indicator.indicatorHolder;
        indicatorArrowTransform = indicator.indicatorArrowTransform;
        indicatorImage = indicator.indicatorImage;
        position = Vector2.zero;
        distance = 0f;
    }

    public void Update(MainScript main)
    {
        Vector2 cameraPosition = main.mainCamera.transform.position;
        float verticleExtent = main.mainCamera.orthographicSize;
        float horizontalExtent = verticleExtent * Screen.width / Screen.height;
        //float indicatorDistanceFromPlayer = 2f;

        if (!float.IsFinite(distance) ||
            ((cameraPosition.x - horizontalExtent) < position.x
            && (cameraPosition.x + horizontalExtent) > position.x
            && (cameraPosition.y - verticleExtent) < position.y
            && (cameraPosition.y + verticleExtent) > position.y))
        {
            // lightCrystalIndicator.indicatorArrow.enabled = false;
            indicatorHolder.gameObject.SetActive(false);
        }
        else
        {
            // lightCrystalIndicator.indicatorArrow.enabled = true;
            indicatorHolder.gameObject.SetActive(true);

            //Vector2 indicatorPosition = main.mainCamera.WorldToViewportPoint(cameraPosition + (position - cameraPosition).normalized * indicatorDistanceFromPlayer);
            Vector2 indicatorPosition = main.mainCamera.WorldToViewportPoint(position);
            {
                // range from 0 to 1
                const float DIST_FROM_EDGE = 0.1f;
                Vector2 fromMiddle = indicatorPosition - new Vector2(0.5f, 0.5f);
                float maxValue = Mathf.Max(Mathf.Abs(fromMiddle.x), Mathf.Abs(fromMiddle.y));
                if (0.5f - DIST_FROM_EDGE < maxValue)
                {
                    indicatorPosition = fromMiddle / maxValue * (0.5f - DIST_FROM_EDGE) + new Vector2(0.5f, 0.5f);
                }
            }
            indicatorHolder.anchorMin = indicatorPosition;
            indicatorHolder.anchorMax = indicatorPosition;

            Vector3 arrowRotation = indicatorArrowTransform.rotation.eulerAngles;
            arrowRotation.z = Mathf.Atan2(position.y - cameraPosition.y, position.x - cameraPosition.x) * Mathf.Rad2Deg - 90;
            indicatorArrowTransform.rotation = Quaternion.Euler(arrowRotation);
        }
    }
}

[System.Serializable]
public struct Boss0Entity
{
    public UnitEntity unit;
    public bool hasAgro;

    public PickupType pickupType;

    public IndicatorAndLocation bossIndicator;

    public ID limb0, limb1, limb2;

    public Boss0Entity(
        GameObject go,
        in Boss0Entity template,
        ID id,
        IndicatorAndLocation bossIndicator,
        int variant)
    {
        this = template;
        unit = new UnitEntity(go, template.unit, id);
        this.bossIndicator = bossIndicator;
        unit.attack.variant = variant;
    }

    public Boss0Entity(
        GameObject go,
        in Boss0Entity template,
        ID id,
        IndicatorUI Indicator,
        int variant)
    {
        this = template;
        unit = new UnitEntity(go, template.unit, id);
        bossIndicator = new IndicatorAndLocation(Indicator);
        unit.attack.variant = variant;
    }

    public void UpdatePickupType(PickupType pickupType, MainScript mainScript)
    {
        this.pickupType = pickupType;

        Color bossColor = mainScript.pickupColor.GetColor(pickupType);
        bossIndicator.indicatorImage.color = bossColor;
        unit.spriteRenderer.color = bossColor;
    }

    public readonly ID GetLimb(int i)
    {
        switch (i)
        {
            default:
            case 0:
                return limb0;
            case 1:
                return limb1;
            case 2:
                return limb2;
        }
    }

    public void SetLimb(int i, in ID id)
    {
        switch (i)
        {
            default:
            case 0:
                limb0 = id; break;
            case 1:
                limb1 = id; break;
            case 2:
                limb2 = id; break;
        }
    }
}

[System.Serializable]
public struct PickupColor
{
    public Color Light;
    public Color Fire;
    public Color Earth;
    public Color Air;
    public Color Water;

    public Color GetColor(PickupType pickupType)
    {
        switch (pickupType)
        {
            case PickupType.Light:
                return Light;
            case PickupType.Fire:
                return Fire;
            case PickupType.Earth:
                return Earth;
            case PickupType.Air:
                return Air;
            case PickupType.Water:
                return Water;
        }

        return Color.white;
    }
}

[System.Serializable]
public struct GameOverScreen
{
    public GameObject gameOverScreen;
    public TMP_Text gameOverText;
    public Button restartButton;
    public string winText, deathText, noPowerText;

    public void Enable(MainScript mainScript)
    {
        string chooseText;
        switch (mainScript.gameState)
        {
            case GameState.Win:
                chooseText = winText; break;
            case GameState.Death:
                chooseText = deathText; break;
            case GameState.NoPower:
                chooseText = noPowerText; break;
            default:
                Debug.LogError("Not a game over state");
                return;
        }
        gameOverText.SetText(chooseText);
        gameOverScreen.SetActive(true);
        mainScript.eventSystem.SetSelectedGameObject(mainScript.eventSystem.firstSelectedGameObject = restartButton.gameObject);
    }
}

[System.Serializable]
public struct GameTimer
{
    public TMP_Text timerText;

    public float timeToSurvive;
    public float currentTime;

    public float healthRegenAccum;

    public string GetTimeLeftString()
    {
        float timeLeft = (timeToSurvive - currentTime);
        return $"{Mathf.FloorToInt(timeLeft / 60):00}:{Mathf.FloorToInt(timeLeft % 60):00}";
    }
}

public enum GameState { TutorialBlast, TutorialBarrierPower, Survive, Win, Death, NoPower };

[System.Serializable]
public struct LimbEntity
{
    public ID parent;
    public GameObject go;
    public Transform shootOrigin;
    public int health;

    public LimbEntity(ID limbID, ID parent, GameObject go, int maxHealth, Color parentColor, MainScript main, int i)
    {
        this.parent = parent;
        this.go = go;
        go.GetComponent<IDComponent>().id = limbID;
        shootOrigin = go.transform.GetChild(0);
        health = maxHealth;

        go.GetComponent<SpriteRenderer>().color = parentColor;

        if (!main.bosses0.IsValidID(parent))
            return;
        ref Boss0Entity boss = ref main.bosses0[parent.index];

        go.transform.SetParent(boss.unit.transform, false);
        RotateAround(main, i);
    }

    public bool IsValid()
    {
        return go != null;
    }

    public void RotateAround(MainScript main, int i)
    {
        if (!main.bosses0.IsValidID(parent))
            return;

        ref Boss0Entity boss = ref main.bosses0[parent.index];
        float myRotation = boss.unit.rotationDegrees + i * 120f + 60f;
        Quaternion rotation = Quaternion.Euler(0f, 0f, myRotation);
        Vector3 limbPos = rotation * Vector3.up * 3.24f;
        go.transform.SetLocalPositionAndRotation(limbPos, rotation);
    }
}
