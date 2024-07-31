using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEngine.RuleTile.TilingRuleOutput;

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

    public bool isLimbed;
}

[System.Serializable]
public struct ChargerAttackPreset
{
    public float startChargeDistance;
    public float chargeDistance;
    public float chargeSpeedBuff;
    public float hitRange;
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
    public ProjectileAttackPreset bossLimbAttack;

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
                case (int)EnemyVariants.Charger:
                    UpdateCharger(ref mainScript.enemies[i], mainScript);
                    break;
            }
        }

        foreach (int i in mainScript.bosses0)
        {
            UpdateAttackProjectile(ref mainScript.bosses0[i].unit, IDType.Boss0,
                mainScript.bosses0[i].unit.attack.variant == 0 ? boss0 : bossLimbAttack,
                defaultProjectile, mainScript, mainScript.bosses0.GetCurrentID(i));
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

            mainScript.SpawnHitVFX(unit.attack.singleTarget.type, targetUnit.transform.position);
        }
    }

    public void UpdateAttackProjectile(
        ref UnitEntity unit,
        IDType unitType,
        in ProjectileAttackPreset attackPreset,
        in ProjectileGroup group,
        MainScript mainScript,
        in ID unitIDForLimb = new ID())
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

        if (0f < unit.attack.attackCooldown || unit.statModifiers.fireRateModifier <= 0f)
        {
            return;
        }

        if (unit.attack.isAttacking)
        {
            int numberOfShots = 1 + attackPreset.extraProjectiles + unit.statModifiers.extraProjectiles;

            unit.attack.attackCooldown = attackPreset.attackCooldown / unit.statModifiers.fireRateModifier;
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

            // calc rotation and position first
            Quaternion rotation = Quaternion.identity;
            Vector3 position = new Vector3();
            if (attackPreset.isLimbed)
            {
                // limbs could be all cut off, would need to bail before spawning projectiles
                if (!mainScript.bosses0.IsValidID(unitIDForLimb))
                {
                    return;
                }
                ref Boss0Entity boss = ref mainScript.bosses0[unitIDForLimb.index];

                ID? findLimbID = null;

                // cycle once
                if (unit.attack.limbShotI < 0)
                    unit.attack.limbShotI = 0;
                ID limbID = boss.GetLimb(unit.attack.limbShotI % 3);
                unit.attack.limbShotI++;

                if (mainScript.limbs.IsValidID(limbID))
                {
                    findLimbID = limbID;
                }

                if (findLimbID.HasValue)
                {
                    ref LimbEntity limb = ref mainScript.limbs[findLimbID.Value.index];
                    position = limb.shootOrigin.position;
                    rotation = Quaternion.LookRotation(Vector3.forward, mainScript.player.transform.position - position);
                }
                else
                    return;
            }

            for (int i = 0; i < numberOfShots; i++)
            {
                if (!attackPreset.isLimbed)
                {
                    position = unit.transform.position;
                    rotation = Quaternion.Euler(0f, 0f, unit.rotationDegrees - startSpreadAngle + attackPreset.spreadAngle * i);
                }

                ID projectileID = group.pool.Spawn();
                ref ProjectileEntity projectile = ref group.pool[projectileID.index];

                if (projectile.gameObject == null)
                {
                    projectile.gameObject = Object.Instantiate(group.prefab);
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

                void SwitchColor(GameObject projectileGO, Color color)
                {
                    projectileGO.GetComponent<SpriteRenderer>().color = color;
                    projectileGO.GetComponent<Light2D>().color = color;
                }

                switch (unitType)
                {
                    case IDType.Player:
                        SwitchColor(projectile.gameObject, new Color(1f, 1f, 0f)); break;
                    case IDType.Enemy:
                        SwitchColor(projectile.gameObject, new Color(0.3f, 0.3f, 0.3f)); break;
                    case IDType.Boss0:
                        SwitchColor(projectile.gameObject, unit.spriteRenderer.color); break;
                }

                Vector3 forward = rotation * Vector2.up;
                projectile.gameObject.transform.SetPositionAndRotation(
                    position + forward * attackPreset.projectileOffset,
                    rotation);
                projectile.gameObject.GetComponent<Rigidbody2D>().velocity = forward * attackPreset.projectileSpeed;
                projectile.lastPosition = projectile.gameObject.transform.position;
            }
            //play once per shotgun eff
            mainScript.audioSystem.PlayAttackSound(unitType, position);
        }
    }

    public void UpdateCharger(
        ref UnitEntity unit,
        MainScript mainScript)
    {
        if (!unit.transform.gameObject.activeInHierarchy)
            return;

        if (0f < unit.attack.chargeDistanceLeft)
        {
            while (!unit.attack.hasHit)
            {
                ref UnitEntity targetUnit = ref mainScript.GetUnit(unit.attack.singleTarget, out bool isTargetValid);
                if (!isTargetValid)
                {
                    break;
                }

                if (charger.hitRange < Vector3.Distance(targetUnit.transform.position, unit.transform.position))
                {
                    break;
                }

                Damage(unit.attack.singleTarget, charger.damage, mainScript);
                unit.attack.hasHit = true;
            }

            unit.attack.chargeDistanceLeft -= Time.deltaTime * unit.moveSpeed * charger.chargeSpeedBuff;
            if (unit.attack.chargeDistanceLeft <= 0f)
                unit.rigidbody.velocity = Vector2.zero;
            else
            {
                unit.rigidbody.velocity = charger.chargeSpeedBuff * unit.moveSpeed * unit.attack.chargeDirection;
                unit.attack.attackCooldown = charger.attackCooldown;
                return;
            }
        }

        unit.attack.attackCooldown = Mathf.Max(unit.attack.attackCooldown - Time.deltaTime, 0f);

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

            Vector2 toTarget = targetUnit.transform.position - unit.transform.position;
            float distance = toTarget.magnitude;
            if (charger.startChargeDistance < distance)
            {
                return;
            }

            unit.attack.chargeDirection = toTarget / distance;
            unit.attack.chargeDistanceLeft = charger.chargeDistance;
            unit.attack.hasHit = false;

            unit.rigidbody.velocity = charger.chargeSpeedBuff * unit.moveSpeed * unit.attack.chargeDirection;
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

                mainScript.SpawnHitVFX(idComp.id.type, collider.transform.position);
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

                        // remove limbs
                        for (int limbI = 0; limbI < 3; limbI++)
                        {
                            if (mainScript.limbs.TryDespawn(boss.GetLimb(limbI), out LimbEntity despawned))
                            {
                                despawned.go.transform.SetParent(null, false);
                                despawned.go.SetActive(false);
                                boss.SetLimb(limbI, new ID());
                            }
                        }
                    }
                    return true;
                }
                break;

            case IDType.Limb:
                if (mainScript.limbs.IsValidID(id))
                {
                    ref LimbEntity limb = ref mainScript.limbs[id.index];

                    if (!mainScript.bosses0.IsValidID(limb.parent))
                        break;

                    ref Boss0Entity parent = ref mainScript.bosses0[limb.parent.index];

                    bool isDead = true;
                    do
                    {
                        if (limb.health <= 0)
                            break;

                        limb.health -= damage;
                        if (limb.health <= 0)
                        {
                            limb.go.SetActive(false);
                            limb.go.transform.SetParent(null, false);
                            isDead = true;
                            mainScript.limbs.TryDespawn(id.index);
                        }
                    }
                    while (false);

                    if (isDead)
                    {
                        for (int limbI = 0; limbI < 3; limbI++)
                        {
                            if (!mainScript.limbs.IsValidID(parent.GetLimb(limbI)))
                            {
                                parent.SetLimb(limbI, new ID());
                            }
                        }
                    }

                    Damage(limb.parent, damage, mainScript);

                    return true;
                }

                break;
        }

        return false;
    }
}
