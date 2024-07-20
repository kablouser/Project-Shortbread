using System.Collections.Generic;
using UnityEngine;
using System;

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
    // if true melee, false shoots projectile
    public bool isMelee;
    public ProjectileType projectile;
    // can start attack at 0
    public float attackCooldown;

    public uint ammoLeft;
    public float reloadCooldown;
}

[Serializable]
public struct UnitEntity
{
    public Transform transform;
    public Rigidbody2D rigidbody;
    public SpriteRenderer spriteRenderer;
    public AnimationComponent animation;
    public AttackComponent attack;
    public float MoveSpeed;

    public UnitEntity(
        GameObject go,
        in AnimationComponent animation,
        in AttackComponent attack,
        float MoveSpeed)
    {
        transform = go.transform;
        rigidbody = go.GetComponent<Rigidbody2D>();
        spriteRenderer = go.GetComponent<SpriteRenderer>();
        this.animation = animation;
        this.attack = attack;
        this.MoveSpeed = MoveSpeed;
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
    public AnimationComponent animationComponent;
    public AttackComponent attackComponent;
    public float moveSpeed;

    // Number of enemies to spawn per second over the game time.
    public AnimationCurve spawnRate;
    [HideInInspector]
    public float currentNumberToSpawn;
    
}