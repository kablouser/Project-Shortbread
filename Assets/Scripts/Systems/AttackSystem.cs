using System;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[Serializable]
public struct ProjectileEntity
{
    public GameObject gameObject;
    public IDType source;
    public int damage;
}

[Serializable]
public struct ProjectileGroup
{
    public GameObject prefab;
    public VersionedPool<ProjectileEntity> pool;
}

[Serializable]
public struct MeleeAttackPreset
{
    public float attackCooldown;
    public int damage;
    public float attackRange;
    // from start attack to damage. Freeze movement during this time
    public float attackTime;
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

    public int damage;
}

[Serializable]
public struct AttackSystem
{
    public ProjectileGroup defaultProjectile;

    public ProjectileAttackPreset player;
    public MeleeAttackPreset enemy;

    public void Update(MainScript mainScript)
    {
        UpdateProjectile(ref mainScript.player, IDType.Player,
            player, defaultProjectile, mainScript);

        foreach (int i in mainScript.enemies)
        {
            UpdateMelee(ref mainScript.enemies[i], enemy, mainScript);
        }
    }

    public void UpdateMelee(
        ref UnitEntity unit,
        in MeleeAttackPreset attackPreset,
        MainScript mainScript)
    {
        bool wasAttacking = 0f < unit.attack.meleeAttackTime;

        unit.attack.meleeAttackTime = Mathf.Max(unit.attack.meleeAttackTime - Time.deltaTime, 0f);
        unit.attack.attackCooldown = Mathf.Max(unit.attack.attackCooldown - Time.deltaTime, 0f);

        if (0f < unit.attack.meleeAttackTime)
        {
            return;
        }

        if (wasAttacking)
        {
            // remove freeze pos
            unit.rigidbody.constraints &= ~RigidbodyConstraints2D.FreezePosition;
        }

        if (0f < unit.attack.attackCooldown)
        {
            return;
        }

        if (unit.attack.isAttacking)
        {
            ref UnitEntity targetUnit = ref mainScript.GetUnit(unit.attack.singleTarget, out bool isTargetValid);
            if (!isTargetValid)
            {
                return;
            }

            if (attackPreset.attackRange < Vector3.Distance(targetUnit.transform.position, unit.transform.position))
            {
                return;
            }

            unit.attack.attackCooldown = attackPreset.attackCooldown;
            unit.attack.meleeAttackTime = attackPreset.attackTime;
            unit.rigidbody.constraints |= RigidbodyConstraints2D.FreezePosition;

            Damage(unit.attack.singleTarget, attackPreset.damage, mainScript);
        }
    }

    public void UpdateProjectile(
        ref UnitEntity unit,
        IDType unitType,
        in ProjectileAttackPreset attackPreset,
        in ProjectileGroup group,
        MainScript mainScript)
    {
        unit.attack.reloadCooldown = Mathf.Max(unit.attack.reloadCooldown - Time.deltaTime, 0f);
        if (0f < unit.attack.reloadCooldown)
        {
            return;
        }

        // state not setup properly (ammo should be full after reload starts)
        if (attackPreset.usesAmmo && unit.attack.ammoLeft <= 0)
        {
            unit.attack.ammoLeft = attackPreset.fullMagazineAmmo;
        }

        unit.attack.attackCooldown = Mathf.Max(unit.attack.attackCooldown - Time.deltaTime, 0f);
        if (0f < unit.attack.attackCooldown)
        {
            return;
        }

        if (unit.attack.isAttacking)
        {
            unit.attack.attackCooldown = attackPreset.attackCooldown;
            if (attackPreset.usesAmmo)
            {
                --unit.attack.ammoLeft;
                if (unit.attack.ammoLeft <= 0)
                {
                    unit.attack.Reload(attackPreset);
                }
            }

            ID projectileID = group.pool.Spawn();
            ref ProjectileEntity projectile = ref group.pool[projectileID.index];

            if (projectile.gameObject == null)
            {
                projectile.gameObject = UnityEngine.Object.Instantiate(group.prefab);
            }
            else
            {
                projectile.gameObject.SetActive(true);
            }
            projectile.damage = attackPreset.damage;
            projectile.source = unitType;
            IDTriggerEnter idTriggerEnter = projectile.gameObject.GetComponent<IDTriggerEnter>();
            idTriggerEnter.id = projectileID;
            idTriggerEnter.mainScript = mainScript;

            // shoot at unit's forward direction
            Quaternion rotation = Quaternion.Euler(0f, 0f, unit.rotationDegrees);
            Vector3 forward = rotation * Vector2.up;
            projectile.gameObject.transform.SetPositionAndRotation(
                unit.transform.position + forward * attackPreset.projectileOffset,
                rotation);
            projectile.gameObject.GetComponent<Rigidbody2D>().velocity = forward * attackPreset.projectileSpeed;
        }
    }

    // Projectile on trigger enter
    public void ProcessTriggerEnterEvent(MainScript mainScript, IDTriggerEnter source, Collider2D collider)
    {
        if (defaultProjectile.pool.TryDespawn(source.id, out ProjectileEntity projectile))
        {
            projectile.gameObject.SetActive(false);
            IDComponent idComp = collider.GetComponent<IDComponent>();
            if (idComp)
            {
                Damage(idComp.id, projectile.damage, mainScript);
            }
        }
    }

    public static void DamageInPool(ref UnitEntity unit, int damage, in ID id, ref VersionedPool<UnitEntity> pool)
    {
        if (DamagePlayer(ref unit, damage))
        {
            pool.TryDespawn(id, out _);
        }
    }

    public static bool DamagePlayer(ref UnitEntity unit, int damage)
    {
        unit.health.current -= damage;
        if (unit.health.current <= 0)
        {
            unit.transform.gameObject.SetActive(false);
            return true;
        }
        return false;
    }

    public static bool DamageCrystal(ref LightCrystal crystal, int damage, in ID id, ref VersionedPool<LightCrystal> pool)
    {
        crystal.health.current -= damage;
        if (crystal.health.current <= 0)
        {
            UnityEngine.Object.Destroy(crystal.transform.gameObject);
            pool.TryDespawn(id, out _);
            return true;
        }
        return false;
    }

    public static bool Damage(in ID id, int damage, MainScript mainScript)
    {
        switch (id.type)
        {
            default: break;

            case IDType.Player:
                DamagePlayer(ref mainScript.player, damage);
                return true;

            case IDType.Enemy:
                if (mainScript.enemies.IsValidID(id))
                {
                    DamageInPool(ref mainScript.enemies[id.index], damage, id, ref mainScript.enemies);
                    return true;
                }
                break;

            case IDType.LightCrystal:
                if(DamageCrystal(ref mainScript.lightCrystals[id.index], damage, id, ref mainScript.lightCrystals))
                {
                    Debug.Log("Destroyed Light Crystal!");
                    mainScript.AddLightPower(mainScript.lightCrystals[id.index].lightPower);
                    return true;
                }
                break;
        }

        return false;
    }
}
