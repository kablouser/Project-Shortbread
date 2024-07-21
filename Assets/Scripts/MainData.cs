using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public enum SpriteSheetIndex
{
    Player,
    Enemy,
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
    public uint ammoLeft;
    public float reloadCooldown;

    public bool Reload(in ProjectileAttackPreset preset)
    {
        if (ammoLeft < preset.fullMagazineAmmo)
        {
            attackCooldown = 0f;
            reloadCooldown = preset.reloadCooldown;
            ammoLeft = preset.fullMagazineAmmo;
            return true;
        }
        return false;
    }
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
    public float moveSpeed;

    public UnitEntity(
        GameObject go,
        in UnitEntity template)
    {
        this = template;
        transform = go.transform;
        rigidbody = go.GetComponent<Rigidbody2D>();
        spriteRenderer = go.GetComponent<SpriteRenderer>();
    }

    public bool IsValid()
    {
        return transform != null &&
            rigidbody != null &&
            spriteRenderer != null;
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
public struct TriggerEvent
{
    public ID id;
    public Collider2D collider;
}
