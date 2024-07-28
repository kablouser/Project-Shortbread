using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.FilePathAttribute;

[Serializable]
public struct AudioSystem
{
    public float masterVolume;
    public float vfxVolume;
    public float musicVolume;

    public AudioSource audioSourceVFX;
    public AudioSource audioSourceMusic;
    public AudioSource audioSourceCentreLight;

    public AudioClip music;

    public AudioClip shardCollectVFX;
    public AudioClip elementalShardCollectVFX;

    public AudioClip playerGunShotVFX;
    public AudioClip playerGunHitVFX;

    public AudioClip boss0GunShotVFX;

    public AudioClip playerHitVFX;
    public AudioClip enemyHitVFX;
    public AudioClip boss0HitVFX;
    public AudioClip LightCrystalHitVFX;

    public AudioClip playerDeathVFX;
    public AudioClip enemyDeathVFX;
    public AudioClip boss0DeathVFX;
    public AudioClip LightCrystalDeathVFX;

    public void Start()
    {
        audioSourceCentreLight.volume = masterVolume * vfxVolume;
        PlayMusic(music);
    }

    public void PlayMusic(AudioClip music)
    {
        audioSourceMusic.clip = music;
        audioSourceMusic.volume = masterVolume * musicVolume;
        audioSourceMusic.loop = true;
        audioSourceMusic.Play();
    }

    public void PlayVFX(AudioClip vfx)
    {
        audioSourceVFX.PlayOneShot(vfx, masterVolume * vfxVolume);
    }

    public void PlayVFXAtLocation(AudioClip vfx, Vector2 location)
    {
        AudioSource.PlayClipAtPoint(vfx, location, masterVolume * vfxVolume);
    }

    public void PlayDamagedVFX(IDType type, Vector2 location)
    {
        switch (type)
        {
            case IDType.Player:
                PlayVFXAtLocation(playerHitVFX, location);
                break;
            case IDType.Enemy:
                PlayVFXAtLocation(enemyHitVFX, location);
                break;
            case IDType.Boss0:
                PlayVFXAtLocation(boss0HitVFX, location);
                break;
            case IDType.LightCrystal:
                PlayVFXAtLocation(LightCrystalHitVFX, location);
                break;
            default:
                break;
        }
    }

    public void PlayDeathVFX(IDType type, Vector2 location)
    {
        switch (type)
        {
            case IDType.Player:
                PlayVFXAtLocation(playerDeathVFX, location);
                break;
            case IDType.Enemy:
                PlayVFXAtLocation(enemyDeathVFX, location);
                break;
            case IDType.Boss0:
                PlayVFXAtLocation(boss0DeathVFX, location);
                break;
            case IDType.LightCrystal:
                PlayVFXAtLocation(LightCrystalDeathVFX, location);
                break;
            default:
                break;
        }
    }

    public void PlayAttackSound(IDType type, Vector2 location)
    {
        switch (type)
        {
            case IDType.Player:
                PlayVFXAtLocation(playerGunShotVFX, location);
                break;
            case IDType.Boss0:
                PlayVFXAtLocation(boss0GunShotVFX, location);
                break;
            default:
                break;
        }
    }
}