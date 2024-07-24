using System;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[Serializable]
public struct ProjectileEntity
{
    public GameObject gameObject;
    public IDType source;
    public int damage;
    public float rangeLeft;
    public Vector2 lastPosition;
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
    // distance to begin attack
    public float attackRange;
    // distance to allow damage at the end of attacks
    public float damageRange;
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
    public int fullMagazineAmmo;

    public int damage;
    public float projectileRange;
}

[Serializable]
public struct AttackSystem
{
    public ProjectileGroup defaultProjectile;

    public ProjectileAttackPreset player;
    public MeleeAttackPreset enemy;
    public ProjectileAttackPreset boss0;

    public void Update(MainScript mainScript)
    {
        // projectile lifetimes first
        foreach (int i in defaultProjectile.pool)
        {
            UpdateProjectile(ref defaultProjectile.pool[i], ref defaultProjectile.pool, i);
        }

        UpdateAttackProjectile(ref mainScript.player, IDType.Player,
            player, defaultProjectile, mainScript);

        foreach (int i in mainScript.enemies)
        {
            UpdateAttackMelee(ref mainScript.enemies[i], enemy, mainScript);
        }

        foreach (int i in mainScript.bosses0)
        {
            UpdateAttackProjectile(ref mainScript.bosses0[i].unit, IDType.Boss0,
                boss0, defaultProjectile, mainScript);
        }
    }

    public void UpdateProjectile(ref ProjectileEntity projectile, ref VersionedPool<ProjectileEntity> pool, int index)
    {
        Vector2 currentPosition = projectile.gameObject.transform.position;
        projectile.rangeLeft -= (currentPosition - projectile.lastPosition).magnitude;
        projectile.lastPosition = currentPosition;

        if (projectile.rangeLeft < 0f)
        {
            projectile.gameObject.SetActive(false);
            pool.TryDespawn(index);
        }
    }

    public void UpdateAttackMelee(
        ref UnitEntity unit,
        in MeleeAttackPreset attackPreset,
        MainScript mainScript)
    {
        if (!unit.transform.gameObject.activeInHierarchy)
            return;

        bool wasAttacking = 0f < unit.attack.meleeAttackTime;

        unit.attack.meleeAttackTime = Mathf.Max(unit.attack.meleeAttackTime - Time.deltaTime, 0f);
        unit.attack.attackCooldown = Mathf.Max(unit.attack.attackCooldown - Time.deltaTime, 0f);

        if (0f < unit.attack.meleeAttackTime)
        {
            return;
        }

        while (wasAttacking)
        {
            // remove freeze pos
            unit.rigidbody.constraints &= ~RigidbodyConstraints2D.FreezePosition;

            ref UnitEntity targetUnit = ref mainScript.GetUnit(unit.attack.singleTarget, out bool isTargetValid);
            if (!isTargetValid)
            {
                break;
            }

            if (attackPreset.damageRange < Vector3.Distance(targetUnit.transform.position, unit.transform.position))
            {
                break;
            }

            // damage when in range
            Damage(unit.attack.singleTarget, attackPreset.damage, mainScript);
            break;
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

        }
    }

    public void UpdateAttackProjectile(
        ref UnitEntity unit,
        IDType unitType,
        in ProjectileAttackPreset attackPreset,
        in ProjectileGroup group,
        MainScript mainScript)
    {
        if (!unit.transform.gameObject.activeInHierarchy)
            return;

        bool wasReloading = 0f < unit.attack.reloadCooldown;

        unit.attack.reloadCooldown = Mathf.Max(unit.attack.reloadCooldown - Time.deltaTime, 0f);
        if (0f < unit.attack.reloadCooldown)
        {
            return;
        }

        if (wasReloading)
        {
            // just reloaded
            unit.attack.ammoShot = 0;
            if (unitType == IDType.Player)
            {
                mainScript.ammoBar.UpdateAmmoBar(unit.attack.ammoShot, attackPreset.fullMagazineAmmo);
            }
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
                ++unit.attack.ammoShot;
                if (unitType == IDType.Player)
                {
                    mainScript.ammoBar.UpdateAmmoBar(unit.attack.ammoShot, attackPreset.fullMagazineAmmo);
                }

                if (attackPreset.fullMagazineAmmo <= unit.attack.ammoShot)
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
            projectile.rangeLeft = attackPreset.projectileRange;
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
            projectile.lastPosition = projectile.gameObject.transform.position;
        }
    }

    // Projectile on trigger enter
    public void ProcessTriggerEnterEvent(MainScript mainScript, IDTriggerEnter source, Collider2D collider)
    {
        if (!defaultProjectile.pool.IsValidID(source.id))
            return;

        ref ProjectileEntity projectile = ref defaultProjectile.pool[source.id.index];
        IDComponent idComp = collider.GetComponent<IDComponent>();

        if (idComp == null ||
            (
                MainScript.IsOppositeTeams(projectile.source, idComp.id.type) &&
                //enemy can only damage player, nothing else, enemy projectiles pass through other objects
                (MainScript.GetTeam(projectile.source) != Team.Enemy || MainScript.GetTeam(idComp.id.type) == Team.Player)
            ) &&
            defaultProjectile.pool.TryDespawn(source.id.index))
        {
            projectile.gameObject.SetActive(false);
            if (idComp != null)
            {
                Damage(idComp.id, projectile.damage, mainScript);
            }
        }
    }

    public static void DamageInPool<T>(ref UnitEntity unit, int damage, in ID id, ref VersionedPool<T> pool)
    {
        if (DamageWithoutDespawn(ref unit, damage))
        {
            pool.TryDespawn(id);
        }
    }

    public static bool DamageWithoutDespawn(ref UnitEntity unit, int damage)
    {
        if (unit.health.current <= 0)
            return false;

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
            crystal.transform.gameObject.SetActive(false);
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
                DamageWithoutDespawn(ref mainScript.player, damage);
                ref HealthComponent health = ref mainScript.player.health;
                mainScript.healthBar.slider.value = health.current / (float)health.max;
                mainScript.healthBar.text.text = $"HP={health.current}/{health.max}";
                return true;

            case IDType.Enemy:
                if (mainScript.enemies.IsValidID(id))
                {
                    DamageInPool(ref mainScript.enemies[id.index], damage, id, ref mainScript.enemies);
                    return true;
                }
                break;

            case IDType.LightCrystal:
                if(mainScript.lightCrystals.IsValidID(id))
                {
                    Vector2 position = mainScript.lightCrystals[id.index].transform.position;
                    if (DamageCrystal(ref mainScript.lightCrystals[id.index], damage, id, ref mainScript.lightCrystals))
                    {
                        mainScript.SpawnLightShards(position, mainScript.lightCrystals[id.index].lightPower);
                        // mainScript.AddLightPower(Crystal.lightPower);
                        return true;
                    }
                }
                break;
                
            case IDType.Boss0:
                if (mainScript.bosses0.IsValidID(id))
                {
                    DamageInPool(ref mainScript.bosses0[id.index].unit, damage, id, ref mainScript.bosses0);
                    return true;
                }
                break;
        }

        return false;
    }
}
