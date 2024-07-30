using UnityEngine;

[System.Serializable]
public struct WeightedRandomClip
{
    public AudioClip clip;
    public float weight;
    public float volume;
}

[System.Serializable]
public struct AudioSystem
{
    public float masterVolume;
    public float vfxVolume;
    public float musicVolume;

    public GameObject audioListener;

    public AudioSource audioSourceVFX;
    public AudioSource audioSourceMusic;
    public AudioSource audioSourceGameStartEnd;
    public AudioSource audioSourceCentreLight;

    public AudioClip music0;
    [Range(0f, 1f)]
    public float music0VolumeFactor;
    public AudioClip music1;
    [Range(0f, 1f)]
    public float music1VolumeFactor;
    public AudioClip music2;
    [Range(0f, 1f)]
    public float music2VolumeFactor;

    public AudioClip craftingVFX;
    public AudioClip craftingCompleteVFX;

    public AudioClip shardCollectVFX;
    public AudioClip elementalShardCollectVFX;

    public AudioClip playerGunShotVFX;
    public AudioClip playerGunHitVFX;

    public AudioClip boss0GunShotVFX;

    public WeightedRandomClip[] playerHitVFX;
    public AudioClip enemyHitVFX;
    public AudioClip boss0HitVFX;
    public AudioClip LightCrystalHitVFX;

    public AudioClip playerDeathVFX;
    public AudioClip enemyDeathVFX;
    public float enemyDeathVolume;
    public AudioClip boss0DeathVFX;
    public AudioClip LightCrystalDeathVFX;

    public AudioClip gameStart;
    public AudioClip gameEnd;

    private int lastPlayedMusic;
    private float baseMusicVolume;
    private float currentMusicVolumeFactor;
    private GameState lastGameState;

    public void Start(MainScript mainScript)
    {
        audioListener.transform.SetParent(mainScript.player.transform, false);
        audioSourceCentreLight.volume = masterVolume * vfxVolume * 0.5f;
        lastPlayedMusic = -1;
        currentMusicVolumeFactor = 0f;
        lastGameState = GameState.TutorialBlast;
    }

    public void Update(MainScript mainScript)
    {
        // if game started
        if (GameState.Survive == mainScript.gameState && lastGameState != mainScript.gameState)
        {
            lastGameState = mainScript.gameState;
            audioSourceGameStartEnd.volume = masterVolume * vfxVolume;
            audioSourceGameStartEnd.clip = gameStart;
            audioSourceGameStartEnd.loop = false;
            audioSourceGameStartEnd.Play();
        }

        // if game ended
        if (GameState.Survive < mainScript.gameState && lastGameState != mainScript.gameState)
        {
            lastGameState = mainScript.gameState;
            audioSourceGameStartEnd.volume = masterVolume * vfxVolume;
            audioSourceGameStartEnd.clip = gameEnd;
            audioSourceGameStartEnd.loop = false;
            audioSourceGameStartEnd.Play();
        }

        // duck music when game start/end sfx
        if (audioSourceMusic.isPlaying)
        {
            currentMusicVolumeFactor = Mathf.Lerp(
                currentMusicVolumeFactor,
                audioSourceGameStartEnd.isPlaying ? 0f : 1f,
                Time.deltaTime * 1.0f);
            audioSourceMusic.volume = baseMusicVolume * currentMusicVolumeFactor;
        }

        // time for new music
        if (!audioSourceMusic.isPlaying &&
            !audioSourceGameStartEnd.isPlaying &&
            GameState.Survive <= mainScript.gameState)
        {
            const int NUM_MUSICS = 3;

            if (lastPlayedMusic < 0)
                lastPlayedMusic = Random.Range(0, NUM_MUSICS);
            else
                lastPlayedMusic = (lastPlayedMusic + 1) % NUM_MUSICS;

            float chooseVol;
            AudioClip chooseMusic;
            switch (lastPlayedMusic)
            {
                default:
                case 0:
                    chooseVol = music0VolumeFactor;
                    chooseMusic = music0;
                    break;
                case 1:
                    chooseVol = music1VolumeFactor;
                    chooseMusic = music1;
                    break;
                case 2:
                    chooseVol = music2VolumeFactor;
                    chooseMusic = music2;
                    break;
            }

            audioSourceMusic.clip = chooseMusic;
            audioSourceMusic.loop = false;
            audioSourceMusic.Play();
            baseMusicVolume = masterVolume * musicVolume * chooseVol;
        }


    }

    public void PlayVFX(AudioClip vfx, float volume = 1f)
    {
        audioSourceVFX.PlayOneShot(vfx, masterVolume * vfxVolume * volume);
    }

    public void PlayVFXAtLocation(AudioClip vfx, Vector2 location, float volume = 1f)
    {
        AudioSource.PlayClipAtPoint(vfx, location, masterVolume * vfxVolume * volume);
    }

    public void PlayDamagedVFX(IDType type, Vector2 location)
    {
        switch (type)
        {
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
                PlayVFXAtLocation(enemyDeathVFX, location, enemyDeathVolume);
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

    public void PlayRandomVFX(WeightedRandomClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return;

        float weightSum = 0f;
        foreach (var x in clips)
        {
            weightSum += x.weight;
        }

        float randomValue = Random.Range(0, weightSum);
        weightSum = 0f;
        int i;
        for(i = 0; i < clips.Length; i++)
        {
            weightSum += clips[i].weight;
            if (randomValue < weightSum)
            {
                break;
            }
        }

        PlayVFX(clips[i].clip, clips[i].volume);
    }
}