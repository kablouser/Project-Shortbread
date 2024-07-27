using UnityEngine;
using System;

public enum PickupType
{
    Light, Fire, Earth, Air, Water
};

[Serializable]
public struct PickupEntity
{
    public Transform transform;
    public Rigidbody2D rigidbody;
    public float value;
    public PickupType type;

    public PickupEntity(
        GameObject go,
        float inValue,
        PickupType inType,
        ID id,
        in PickupSpritePreset spritePreset)
    {
        value = inValue;
        type = inType;
        transform = go.transform;
        rigidbody = go.GetComponent<Rigidbody2D>();
        var spriteRenderer = go.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spritePreset.sprite;
        spriteRenderer.size = spritePreset.spriteSize;
        spriteRenderer.material = spritePreset.material;
        go.GetComponent<IDComponent>().id = id;
    }

    public bool IsValid()
    {
        return transform != null;
    }
}

[Serializable]
public struct PickupSpritePreset
{
#if UNITY_EDITOR
    [TextArea(0, 3)]
    public string name;
#endif
    public Sprite sprite;
    public Vector2 spriteSize;
    public Material material;
}

[Serializable]
public struct PickupSystem
{
    public GameObject prefab;
    public PickupSpritePreset[] spritePresets;

    public float playerDistanceToAttract;
    public AnimationCurve speedAtDistanceFromPlayer;
    public float maxSpeed;
    public float pickUpDistance;
    public VersionedPool<PickupEntity> pickups;

#if UNITY_EDITOR
    public bool IsValid()
    {
        return
            Enum.GetNames(typeof(PickupType)).Length == spritePresets.Length;
    }

    public void Validate()
    {
        Extensions.FillWithEnumNames<PickupType, PickupSpritePreset>(ref spritePresets, (ref PickupSpritePreset psp, string name) => psp.name = name);
    }
#endif

    public void FixedUpdate(MainScript mainScript)
    {
        Vector2 playerPosition = mainScript.player.transform.position;

        foreach (int id in pickups)
        {
            ref PickupEntity pickup = ref pickups[id];
            float DistanceFromPlayer = Vector2.SqrMagnitude((Vector2)pickup.transform.position - playerPosition);

            // Collect shards if close enough
            if ((pickUpDistance * pickUpDistance) >= DistanceFromPlayer)
            {
                switch (pickup.type)
                {
                    case PickupType.Light:
                        mainScript.AddLightPower(pickup.value);
                        break;
                    case PickupType.Fire:
                        mainScript.craftingSystem.resources.fire.AddValue(Mathf.RoundToInt(pickup.value), mainScript);
                        break;
                    case PickupType.Earth:
                        mainScript.craftingSystem.resources.earth.AddValue(Mathf.RoundToInt(pickup.value), mainScript);
                        break;
                    case PickupType.Air:
                        mainScript.craftingSystem.resources.air.AddValue(Mathf.RoundToInt(pickup.value), mainScript);
                        break;
                    case PickupType.Water:
                        mainScript.craftingSystem.resources.water.AddValue(Mathf.RoundToInt(pickup.value), mainScript);
                        break;
                }
                pickup.transform.gameObject.SetActive(false);
                pickups.TryDespawn(pickup.transform.GetComponent<IDComponent>().id, out _);
                continue;
            }

            // Move towards player when in range
            float attrackDistanceSqr = playerDistanceToAttract * playerDistanceToAttract;
            if (DistanceFromPlayer <= attrackDistanceSqr)
            {
                float moveSpeed = speedAtDistanceFromPlayer.Evaluate(DistanceFromPlayer / attrackDistanceSqr) * maxSpeed;
                Vector2 velocity = (playerPosition - (Vector2)pickup.transform.position).normalized * moveSpeed;
                pickup.rigidbody.velocity = velocity;
            }
        }
    }

    public void SpawnPickup(
        PickupType type,
        Vector2 position,
        float value)
    {
        ID id = pickups.Spawn();
        ref PickupEntity shard = ref pickups[id.index];

        ref PickupSpritePreset spritePreset = ref spritePresets[(int)type];

        if (shard.IsValid())
        {
            shard = new PickupEntity(shard.transform.gameObject, value, type, id, spritePreset);
            shard.transform.position = position;
            shard.transform.gameObject.SetActive(true);
        }
        else
        {
            shard = new PickupEntity(
                    UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity),
                    value, type, id, spritePreset);
        }
    }

    public void SpawnLightShards(MainScript mainScript, Vector3 position, float lightPower)
    {
        int NumberToSpawn = Mathf.CeilToInt(lightPower / mainScript.lightShardData.maxPowerPerShard);

        for (int i = 0; i < NumberToSpawn; i++)
        {

            float shardPower = lightPower;
            if (lightPower > mainScript.lightShardData.maxPowerPerShard)
            {
                shardPower = lightPower;
                lightPower -= mainScript.lightShardData.maxPowerPerShard;
            }

            SpawnPickup(PickupType.Light, position.AddRandom(0.05f), shardPower);
        }
    }
}
