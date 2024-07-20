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

[Serializable]
public struct AttackComponent
{
    public bool isFire;
}

[Serializable]
public struct UnitEntity
{
    public Transform transform;
    public Rigidbody2D rigidbody;
    public SpriteRenderer spriteRenderer;
    public AnimationComponent animation;
    public AttackComponent attack;
}
