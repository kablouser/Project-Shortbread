using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ProjectileEntity
{
    public GameObject gameObject;
    public IDType source;
    public int damage;
    public float rangeLeft;
    public Vector2 lastPosition;
    public int piercingNumber;
    public List<IDComponent> hitIDs;
}

[System.Serializable]
public struct ProjectileGroup
{
    public GameObject prefab;
    public VersionedPool<ProjectileEntity> pool;
}

[System.Serializable]
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

[System.Serializable]
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

    public int piercingNumber;
    public int extraProjectiles;
    public int spreadAngle;
}

[System.Serializable]
public struct ChargerAttackPreset
{
    public float startChargeDistance;
    public float chargeDistance;
    public float attackCooldown;
    public int damage;
}

[System.Serializable]
public struct AttackSystem
{
    public ProjectileGroup defaultProjectile;

    public ProjectileAttackPreset player;
    public MeleeAttackPreset enemyMelee;
    public ProjectileAttackPreset enemyRanged;
    public ChargerAttackPreset charger;
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
            switch (mainScript.enemies[i].attack.variant)
            {
                case (int)EnemyVariants.Melee:
                    UpdateAttackMelee(ref mainScript.enemies[i], enemyMelee, mainScript);
                    break;
                case (int)EnemyVariants.Ranged:
                    UpdateAttackProjectile(ref mainScript.enemies[i], IDType.Enemy, enemyRanged, defaultProjectile, mainScript);
                    break;
            }
        }

        foreach (int i in mainScript.bosses0)
        {
            UpdateAttackProjectile(ref mainScript.bosses0[i].unit, IDType.Boss0, boss0, defaultProjectile, mainScript);
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

            Damage(unit.attack.singleTarget, attackPreset.damage, mainScript);
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
        unit.attack.attackCooldown = Mathf.Max(unit.attack.attackCooldown - Time.deltaTime, 0f);
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

        if (0f < unit.attack.attackCooldown)
        {
            return;
        }

        if (unit.attack.isAttacking)
        {
            int numberOfShots = 1 + attackPreset.extraProjectiles + unit.statModifiers.extraProjectiles;

            unit.attack.attackCooldown = attackPreset.attackCooldown;
            if (attackPreset.usesAmmo)
            {
                unit.attack.ammoShot += numberOfShots;
                if (unitType == IDType.Player)
                {
                    mainScript.ammoBar.UpdateAmmoBar(unit.attack.ammoShot, attackPreset.fullMagazineAmmo);
                }

                if (attackPreset.fullMagazineAmmo <= unit.attack.ammoShot)
                {
                    unit.attack.Reload(attackPreset, unit.statModifiers);
                }
            }

            float startSpreadAngle = (attackPreset.spreadAngle * (numberOfShots - 1)) / 2;
            if(numberOfShots == 1)
            {
                startSpreadAngle = 0;
            }

            for (int i = 0; i < numberOfShots; i++)
            {
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
                projectile.damage = Mathf.FloorToInt(attackPreset.damage * unit.statModifiers.damageModifier);
                projectile.source = unitType;
                projectile.rangeLeft = attackPreset.projectileRange;
                projectile.piercingNumber = attackPreset.piercingNumber + unit.statModifiers.piercingNumber;
                IDTriggerEnter idTriggerEnter = projectile.gameObject.GetComponent<IDTriggerEnter>();
                idTriggerEnter.id = projectileID;
                idTriggerEnter.mainScript = mainScript;

                // shoot at unit's forward direction
                Quaternion rotation = Quaternion.Euler(0f, 0f, unit.rotationDegrees - startSpreadAngle + attackPreset.spreadAngle * i);
                Vector3 forward = rotation * Vector2.up;
                projectile.gameObject.transform.SetPositionAndRotation(
                    unit.transform.position + forward * attackPreset.projectileOffset,
                    rotation);
                projectile.gameObject.GetComponent<Rigidbody2D>().velocity = forward * attackPreset.projectileSpeed;
                projectile.lastPosition = projectile.gameObject.transform.position;

                mainScript.audioSystem.PlayAttackSound(unitType, unit.transform.position);
            }
        }
    }

    public void UpdateCharger(
        ref UnitEntity unit,
        IDType unitType,
        MainScript mainScript)
    {
        if (!unit.transform.gameObject.activeInHierarchy)
            return;

        bool wasAttacking = 0f < unit.attack.attackCooldown;
        unit.attack.attackCooldown = Mathf.Max(unit.attack.attackCooldown - Time.deltaTime, 0f);

        if (0f < unit.attack.attackCooldown)
        {
            return;
        }

        if (0f < unit.attack.chargeDistanceCurrent)
        {

        }
        else if (unit.attack.isAttacking)
        {
            ref UnitEntity targetUnit = ref mainScript.GetUnit(unit.attack.singleTarget, out bool isTargetValid);
            if (!isTargetValid)
            {
                return;
            }

            Vector2 toTarget = targetUnit.transform.position - unit.transform.position;
            float distance = toTarget.magnitude;
            if (charger.startChargeDistance < distance)
            {
                return;
            }

            unit.attack.chargeDirection = toTarget / distance;
            unit.attack.chargeDistanceCurrent = 0f;
            //unit.attack.attackCooldown = attackPreset.attackTime;
            unit.rigidbody.constraints |= RigidbodyConstraints2D.FreezePosition;

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
            ))
        {
            if(projectile.piercingNumber > 0)
            {
                projectile.piercingNumber--;
            }
            else
            {
                defaultProjectile.pool.TryDespawn(source.id.index);
                projectile.gameObject.SetActive(false);
            }

            if (idComp != null)
            {
                Damage(idComp.id, projectile.damage, mainScript);

                if(projectile.source == IDType.Player)
                {
                    mainScript.audioSystem.PlayVFXAtLocation(mainScript.audioSystem.playerGunHitVFX, collider.transform.position);
                }
            }
        }
    }

    // returns true when unit is dead
    public static bool DamageInPool<T>(ref UnitEntity unit, int damage, in ID id, ref VersionedPool<T> pool)
    {
        if (DamageWithoutDespawn(ref unit, damage))
        {
            pool.TryDespawn(id);
            return true;
        }
        return false;
    }

    // returns true when unit is dead
    public static bool DamageWithoutDespawn(ref UnitEntity unit, int damage)
    {
        if (unit.health.current <= 0)
            return true;

        unit.health.current -= damage;
        if (unit.health.current <= 0)
        {
            unit.transform.gameObject.SetActive(false);
            return true;
        }
        return false;
    }

    // returns true when unit is dead
    public static bool DamageCrystal(ref LightCrystal crystal, int damage, in ID id, ref VersionedPool<LightCrystal> pool)
    {
        crystal.health.current -= damage;
        if (crystal.health.current <= 0)
        {
            crystal.transform.gameObject.SetActive(false);
            pool.TryDespawn(id);
            return true;
        }
        return false;
    }

    // returns true when damaged
    public static bool Damage(in ID id, int damage, MainScript mainScript)
    {
        switch (id.type)
        {
            default: break;

            case IDType.Player:
                if (mainScript.player.health.current <= 0)
                {
                    return true;
                }

                if (DamageWithoutDespawn(ref mainScript.player, damage))
                {
                    // player's listener is disabled.
                    mainScript.audioSystem.audioListener.transform.SetParent(null, true);
                }
                mainScript.healthBar.UpdateHealthBar(mainScript.player.health);

                if (0 < damage)
                {
                    mainScript.shakeSystem.Shake(1.5f);
                    mainScript.audioSystem.PlayRandomVFX(mainScript.audioSystem.playerHitVFX);
                }
                return true;

            case IDType.Enemy:
                if (mainScript.enemies.IsValidID(id))
                {
                    if (DamageInPool(ref mainScript.enemies[id.index], damage, id, ref mainScript.enemies))
                    {
                        mainScript.audioSystem.PlayDeathVFX(IDType.Enemy, mainScript.enemies[id.index].transform.position);
                    }
                    return true;
                }
                break;

            case IDType.LightCrystal:
                if(mainScript.lightCrystals.IsValidID(id))
                {
                    Vector2 position = mainScript.lightCrystals[id.index].transform.position;
                    if (DamageCrystal(ref mainScript.lightCrystals[id.index], damage, id, ref mainScript.lightCrystals))
                    {
                        mainScript.pickupSystem.SpawnLightShards(mainScript, position, mainScript.lightCrystals[id.index].lightPower);

                        if (mainScript.gameState == GameState.TutorialBlast)
                        {
                            mainScript.SetGameState(GameState.TutorialBarrierPower);
                        }

                        mainScript.audioSystem.PlayDeathVFX(IDType.LightCrystal, position);
                        mainScript.shakeSystem.Shake(0.5f);
                        return true;
                    }
                    else
                    {
                        //Still alive
                        mainScript.audioSystem.PlayDamagedVFX(IDType.LightCrystal, position);
                    }
                }
                break;
                
            case IDType.Boss0:
                if (mainScript.bosses0.IsValidID(id))
                {
                    ref Boss0Entity boss = ref mainScript.bosses0[id.index];
                    // attack player if they damaged the boss from afar
                    boss.hasAgro = true;

                    if (DamageInPool(ref boss.unit, damage, id, ref mainScript.bosses0))
                    {
                        // dead, spawn elemental pickups
                        ref Boss0SpawnData spawnData = ref mainScript.boss0SpawnData;

                        float total = spawnData.fire + spawnData.earth + spawnData.air + spawnData.water;
                        if (0f < total && 0 < spawnData.dropResources)
                        {
                            Vector3 bossPos = boss.unit.transform.position;

                            for (int i = 0; i < 4; i++)
                            {
                                // randomise what elements are dropped. not how many. you should be able to craft after defeating a boss.
                                if (Random.value < 0.7f)
                                    // drop boss element
                                    mainScript.pickupSystem.SpawnPickup(boss.pickupType, bossPos.AddRandom(0.05f), 1f);
                                else
                                    // drop other element
                                    mainScript.pickupSystem.SpawnPickup(
                                        (PickupType)Random.Range((int)PickupType.Fire, (int)PickupType.Water + 1), bossPos.AddRandom(0.05f), 1f);
                            }
                            mainScript.audioSystem.PlayDeathVFX(IDType.Boss0, bossPos);

                            //for (int i = 0; i < spawnData.dropResources; i++)
                            //{
                            //    float choose = UnityEngine.Random.Range(0f, total);
                            //    float sum = 0f;
                            //    PickupType pickupType;
                            //    if (choose < sum.Accumulate(spawnData.fire))
                            //        pickupType = PickupType.Fire;
                            //    else if (choose < sum.Accumulate(spawnData.earth))
                            //        pickupType = PickupType.Earth;
                            //    else if (choose < sum.Accumulate(spawnData.air))
                            //        pickupType = PickupType.Air;
                            //    else
                            //        pickupType = PickupType.Water;

                            //    mainScript.pickupSystem.SpawnPickup(pickupType, bossPos.AddRandom(0.05f), 1f);
                            //    mainScript.audioSystem.PlayDeathVFX(IDType.Boss0, bossPos);
                            //}
                        }

                        // need to do this. maybe boss got killed far away
                        boss.bossIndicator.indicatorHolder.gameObject.SetActive(false);

                        // Remove other bosses
                        foreach (int i in mainScript.bosses0)
                        {
                            mainScript.bosses0[i].unit.transform.gameObject.SetActive(false);
                            mainScript.bosses0[i].bossIndicator.indicatorHolder.gameObject.SetActive(false);
                            mainScript.bosses0.TryDespawn(i);
                        }

                        mainScript.shakeSystem.Shake(0.7f);
                        mainScript.numberBossesDefeated++;
                    }
                    return true;
                }
                break;
        }

        return false;
    }
}
