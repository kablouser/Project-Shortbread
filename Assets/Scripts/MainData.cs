using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using Unity.VisualScripting.FullSerializer;
using JetBrains.Annotations;

public enum Team { Neutral, Player, Enemy };

public enum SpriteSheetIndex
{
    Player,
    Enemy,
    LightCrystal,
};

public enum AnimationClipIndex
{
    Stand,
    Walk,
    Attack,
};

[Serializable]
public struct SpriteSheet
{
#if UNITY_EDITOR
    [TextArea(0, 3)]
    public string name;
#endif

    public List<Sprite> spriteSheet;
}

[Serializable]
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

[Serializable]
public struct AnimationComponent
{
    public SpriteSheetIndex spriteSheetIndex;
    public AnimationClipIndex animationClipIndex;
    public int currentIndex;
}

[Flags]
public enum AnimationEventFlags
{
    None,
    ClipEnd,
    Special,
}

[Serializable]
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

[Serializable]
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

    public bool Reload(in ProjectileAttackPreset preset, StatsModifierComponent stats)
    {
        if (0 < ammoShot)
        {
            attackCooldown = 0f;
            reloadCooldown = preset.reloadCooldown * stats.reloadSpeedModifier;
            return true;
        }
        return false;
    }
}

[Serializable]
public struct HealthComponent
{
    public int baseHealth;
    public int current;
    public int max;
}

[Serializable]
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
        health.current += health.max;
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

[Serializable]
public struct EnemyData
{
    public GameObject enemyPrefab;
    public UnitEntity presetUnit;

    // Number of enemies to spawn per second over the game time.
    public AnimationCurve spawnRate;
    public float currentNumberToSpawn;
}

[Serializable]
public struct Boss0SpawnData
{
    public GameObject prefab;
    public Boss0Entity presetUnit;

    // Number required to have spawned over the game time.
    public AnimationCurve spawnRate;
    public int numberSpawned;
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

[Serializable]
public struct PlayerLight
{
    public Light2D light;
    public float baseLightRange;
}

[Serializable]
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

[Serializable]
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

[Serializable]
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

[Serializable]
public struct LightShardData
{
    public float maxPowerPerShard;
}

[Serializable]
public struct TextAndSlider
{
    public TextMeshProUGUI text;
    public Slider slider;

    public void UpdateHealthBar(in HealthComponent health)
    {
        UpdateBar("HP:", health.current, health.max);
    }

    public void UpdateAmmoBar(int ammoShot, int ammoCapacity)
    {
        UpdateBar("Ammo:", ammoCapacity - ammoShot, ammoCapacity);
    }

    public void UpdateBar(string labelPrefix, int currentValue, int maxValue)
    {
        slider.value = currentValue / (float)maxValue;
        text.text = $"{labelPrefix}{currentValue}/{maxValue}";
    }
}

[Serializable]
public struct IndicatorAndLocation
{
    public RectTransform indicatorHolder;
    public Vector2 position;
    public float distance;

    public void Update(MainScript main)
    {
        Vector2 cameraPosition = main.mainCamera.transform.position;
        float verticleExtent = main.mainCamera.orthographicSize;
        float horizontalExtent = verticleExtent * Screen.width / Screen.height;
        float indicatorDistanceFromPlayer = 2f;

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

            Vector2 indicatorPosition = main.mainCamera.WorldToViewportPoint(cameraPosition + (position - cameraPosition).normalized * indicatorDistanceFromPlayer);
            indicatorHolder.anchorMin = indicatorPosition;
            indicatorHolder.anchorMax = indicatorPosition;
        }
    }
}

[Serializable]
public struct Boss0Entity
{
    public UnitEntity unit;
    public bool hasAgro;
    // other components here
    // e.g. limbs system

    public Boss0Entity(
        GameObject go,
        in Boss0Entity template,
        ID id)
    {
        this = template;
        unit = new UnitEntity(go, template.unit, id);
    }
}

[Serializable]
public struct CraftingResource
{
    public int value;
    public GameObject textParent;
    public TextMeshProUGUI text;
    public Sprite pickupSprite;

    public void SetValue(int newValue)
    {
        value = newValue;
        textParent.SetActive(0 < value);
        if (0 < value)
        {
            text.text = value.ToString();
        }
    }
}

[Serializable]
public struct GameOverScreen
{
    public GameObject gameOverScreen;
    public TMP_Text gameOverText;
    public Button restartButton;
    public String winText;
    public String loseText;

    public void Enable(String text)
    {
        gameOverText.SetText(text);
        gameOverScreen.SetActive(true);
    }
}

[Serializable]
public struct GameTimer
{
    public TMP_Text timerText;

    public float timeToSurvive;
    public float currentTime;

    public string GetTimeLeftString()
    {
        float timeLeft = (timeToSurvive - currentTime);
        return Mathf.FloorToInt(timeLeft / 60) + ":" + Mathf.FloorToInt(timeLeft % 60);
    }
}