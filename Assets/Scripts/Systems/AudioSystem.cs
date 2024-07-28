using UnityEngine;

[System.Serializable]
public struct AudioSystem
{
    public float masterVolume;
    public float vfxVolume;
    public float musicVolume;

    public GameObject audioListener;

    public AudioSource audioSourceVFX;
    public AudioSource audioSourceMusic;
    public AudioSource audioSourceCentreLight;

    public AudioClip music0;
    [Range(0f, 1f)]
    public float music0VolumeFactor;
    public AudioClip music1;
    [Range(0f, 1f)]
    public float music1VolumeFactor;

    public AudioClip craftingVFX;
    public AudioClip craftingCompleteVFX;

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

    private int lastPlayedMusic;

    public void Start(MainScript mainScript)
    {
        audioListener.transform.SetParent(mainScript.player.transform, false);
        audioSourceCentreLight.volume = masterVolume * vfxVolume * 0.5f;
        lastPlayedMusic = -1;
    }

    public void Update(MainScript mainScript)
    {
        if (!audioSourceMusic.isPlaying && GameState.Survive <= mainScript.gameState)
        {
            int chooseRandomMusic;
            if (lastPlayedMusic < 0)
            {
                chooseRandomMusic = Random.Range(0, 2);
            }
            else
            {
                chooseRandomMusic = lastPlayedMusic == 1 ? 0 : 1;
            }

            float chooseVol;
            AudioClip chooseMusic;
            if (chooseRandomMusic == 0)
            {
                chooseVol = music0VolumeFactor;
                chooseMusic = music0;
            }
            else
            {
                chooseVol = music1VolumeFactor;
                chooseMusic = music1;
            }

            audioSourceMusic.clip = chooseMusic;
            audioSourceMusic.volume = masterVolume * musicVolume * chooseVol;
            audioSourceMusic.loop = false;
            audioSourceMusic.Play();
        }
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
                // DONT USE LOCATION, location is global position, player can move out of it quickly, so it pans to the one ear when moving quickly
                PlayVFX(playerGunShotVFX);
                break;
            case IDType.Boss0:
                PlayVFXAtLocation(boss0GunShotVFX, location);
                break;
            default:
                break;
        }
    }
}