using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ProjectilePrefabPreset
{
    public GameObject prefab;
    public VersionedPool<GameObject> pool;
}

[Serializable]
public struct MeleeAttackPreset
{
    public float attackCooldown;
}

[Serializable]
public struct ProjectileAttackPreset
{
    public float attackCooldown;

    public float projectileOffset;
    public float projectileSpeed;

    public bool usesAmmo;
    public float reloadCooldown;
    public uint fullMagazineAmmo;
}

[Serializable]
public struct AttackSystem
{
    public ProjectilePrefabPreset defaultProjectile;

    public ProjectileAttackPreset player;
    public MeleeAttackPreset enemy;

    public void Update(MainScript mainScript)
    {
        UpdateProjectileUnit(ref mainScript.player, player, defaultProjectile, mainScript);
    }

    public void UpdateMeleeUnit(ref UnitEntity unit, in ProjectileAttackPreset preset)
    {


    }

    public void UpdateProjectileUnit(
        ref UnitEntity unit,
        in ProjectileAttackPreset preset,
        in ProjectilePrefabPreset prefab,
        MainScript mainScript)
    {
        unit.attack.reloadCooldown = Mathf.Max(unit.attack.reloadCooldown - Time.deltaTime, 0f);
        if (0f < unit.attack.reloadCooldown)
        {
            return;
        }

        // state not setup properly (ammo should be full after reload starts)
        if (preset.usesAmmo && unit.attack.ammoLeft <= 0)
        {
            unit.attack.ammoLeft = preset.fullMagazineAmmo;
        }

        unit.attack.attackCooldown = Mathf.Max(unit.attack.attackCooldown - Time.deltaTime, 0f);
        if (0f < unit.attack.attackCooldown)
        {
            return;
        }

        if (unit.attack.isAttacking)
        {
            unit.attack.attackCooldown = preset.attackCooldown;
            if (preset.usesAmmo)
            {
                --unit.attack.ammoLeft;
                if (unit.attack.ammoLeft <= 0)
                {
                    unit.attack.Reload(preset);
                }
            }

            ID newID = prefab.pool.Spawn();
            ref GameObject newProjectile = ref prefab.pool[newID.index];

            if (newProjectile == null)
            {
                newProjectile = UnityEngine.Object.Instantiate(prefab.prefab);
            }
            else
            {
                newProjectile.SetActive(true);
            }
            IDTriggerEnter idTriggerEnter = newProjectile.GetComponent<IDTriggerEnter>();
            idTriggerEnter.id = newID;
            idTriggerEnter.mainScript = mainScript;

            // shoot at unit's forward direction
            Quaternion rotation = Quaternion.Euler(0f, 0f, unit.rotationDegrees);
            Vector3 forward = rotation * Vector2.up;
            newProjectile.transform.SetPositionAndRotation(
                unit.transform.position + forward * preset.projectileOffset,
                rotation);
            newProjectile.GetComponent<Rigidbody2D>().velocity = forward * preset.projectileSpeed;
        }
    }

    // Projectile on trigger enter
    public void ProcessTriggerEnterEvent(in TriggerEvent e)
    {
        if (defaultProjectile.pool.TryDespawn(e.id, out GameObject projectile))
        {
            projectile.SetActive(false);
            //e.collider;
        }
    }
}
