using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

[Serializable]
public struct AnimationSystem
{
    public float frameRate;
    public float animationAccumulateFrames;

    public SpriteSheet[] spriteSheets;
    public AnimationClip[] animationClips;

    public List<AnimationEvent> animationEvents;

    public void Update(MainScript main)
    {
        animationEvents.Clear();

        // Animation update
        animationAccumulateFrames += Time.deltaTime * frameRate;

        int animateFrames = Mathf.FloorToInt(animationAccumulateFrames);
        if (animateFrames < 1)
            return;

        animationAccumulateFrames -= animateFrames;

        Span<SpriteSheet> spriteSheetsSpan = spriteSheets.AsSpan();
        Span<AnimationClip> animationClipsSpan = animationClips.AsSpan();
        AnimationEvent animationEvent = new();

        animationEvent.id.type = IDType.Player;
        if (UpdateUnit(animateFrames, spriteSheetsSpan, animationClipsSpan,
            ref main.player,
            ref animationEvent))
        {
            animationEvents.Add(animationEvent);
        }

        animationEvent.id.type = IDType.Enemy;
        foreach (int index in main.enemies)
        {
            if (UpdateUnit(animateFrames, spriteSheetsSpan, animationClipsSpan,
                ref main.enemies[index],
                ref animationEvent))
            {
                animationEvents.Add(animationEvent);
            }
        }
    }

    // returns true if produced events
    public bool UpdateUnit(
        int animateFrames, in Span<SpriteSheet> spriteSheets, in Span<AnimationClip> animationClips,
        ref UnitEntity unit,
        ref AnimationEvent outAnimationEvent)
    {
        List<Sprite> spriteSheet = spriteSheets[(int)unit.animation.spriteSheetIndex].spriteSheet;
        ref AnimationClip clip = ref animationClips[(int)unit.animation.animationClipIndex];

        // prevent infinite while loop
        if (!clip.IsValid)
            return false;

        int frameCount = clip.GetFrameCount;
        int lastIndex = unit.animation.currentIndex;
        int lastIndexInClip = clip.startIndex + lastIndex;
        unit.animation.currentIndex += animateFrames;
        if (clip.isRepeat)
        {
            unit.animation.currentIndex -= unit.animation.currentIndex / frameCount * frameCount;
#if UNITY_EDITOR
            if (0 <= clip.eventIndex)
            {
                Debug.LogError("Events in repeated clips not implemented");
            }
#endif
        }

        unit.animation.currentIndex = Mathf.Clamp(unit.animation.currentIndex, 0, frameCount);
        int currentIndexInClip = clip.startIndex + unit.animation.currentIndex;
        unit.spriteRenderer.sprite = spriteSheet[currentIndexInClip];

        // events not implemented for repeat
        if (clip.isRepeat)
            return false;

        outAnimationEvent.clip = unit.animation.animationClipIndex;
        outAnimationEvent.flags = AnimationEventFlags.None;

        // Animation end transitions (from non-looping animation back to stand)
        // make sure not to use animationClipIndex anymore to animate during this frame
        if (lastIndexInClip < clip.endIndex && clip.endIndex <= currentIndexInClip)
        {
            outAnimationEvent.flags |= AnimationEventFlags.ClipEnd;
        }

        // Animation special events (on hit / spawn projectiles)
        if (lastIndexInClip < clip.eventIndex && clip.eventIndex <= currentIndexInClip)
        {
            outAnimationEvent.flags |= AnimationEventFlags.Special;
        }

        return outAnimationEvent.flags != 0;
    }
}
