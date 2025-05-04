using UnityEngine;
using System;

[Serializable]
public struct BossPreset
{
    public float agroRange;
}

[Serializable]
public struct AISystem
{
    public BossPreset boss0;

    public void FixedUpdate(MainScript mainScript)
    {
        Vector2 playerPosition = mainScript.player.transform.position;

        // Enemy Movement
        foreach (int enemyID in mainScript.enemies)
        {
            ref UnitEntity enemy = ref mainScript.enemies[enemyID];
            Vector2 velocity = (playerPosition - (Vector2)enemy.transform.position).normalized * enemy.statSheet.moveSpeed.Calculate();

            enemy.attack.isAttacking = true;

            if (enemy.attack.variant == (int)EnemyVariants.Ranged)
            {
                enemy.SetRotationDegrees(velocity);
                enemy.attack.isAttacking = false;
                // attacked. now reloading
                if (enemy.attack.reloadCooldown > 0f)
                {
                    enemy.rigidbody.velocity = Vector2.zero;
                    continue;
                }
                // ready to attack, delay a little
                else if (enemy.attack.attackCooldown <= 0f && enemy.attack.ammoShot == 0)
                {
                    if (0f < enemy.attack.attackDelay)
                    {
                        enemy.attack.attackDelay -= Time.fixedDeltaTime;
                        if (enemy.attack.attackDelay <= 0f)
                        {
                            enemy.attack.isAttacking = true;
                            enemy.attack.attackDelay = 0f;
                        }
                    }
                    else
                    {
                        enemy.attack.attackDelay = 0.5f;
                    }

                    enemy.rigidbody.velocity = Vector2.zero;
                    continue;
                }
            }
            else if (enemy.attack.variant == (int)EnemyVariants.Charger && 0f < enemy.attack.chargeDistanceLeft)
            {
                continue;
            }

            enemy.rigidbody.velocity = velocity;
        }

        foreach (int i in mainScript.bosses0)
        {
            ref Boss0Entity boss = ref mainScript.bosses0[i];

            boss.unit.rigidbody.velocity = Vector2.zero;
            boss.unit.attack.isAttacking = false;

            Vector2 toPlayer = playerPosition - (Vector2)boss.unit.transform.position;
            float distanceToPlayer = toPlayer.magnitude;
            if (distanceToPlayer <= Mathf.Epsilon)
                continue;

            if (!boss.hasAgro)
            {
                if (distanceToPlayer <= boss0.agroRange)
                {
                    boss.hasAgro = true;
                }
            }

            if (boss.hasAgro)
            {
                // normalise
                if (boss.unit.attack.reloadCooldown == 0f)
                {
                    boss.unit.rigidbody.velocity = Vector2.zero;
                }
                else
                {
                    boss.unit.rigidbody.velocity = toPlayer / distanceToPlayer * boss.unit.statSheet.moveSpeed.Calculate();
                }
                boss.unit.attack.isAttacking = true;

                if (boss.unit.attack.variant == 1)
                {
                    // limbs
                    boss.unit.rotationDegrees += Time.fixedDeltaTime * 30f;
                    for (int limbI = 0; limbI < 3; limbI++)
                    {
                        if (!mainScript.limbs.IsValidID(boss.GetLimb(limbI)))
                            continue;

                        mainScript.limbs[boss.GetLimb(limbI).index].RotateAround(mainScript, limbI);
                    }
                    continue;
                }

                boss.unit.SetRotationDegrees(toPlayer);
            }
        }
    }
}
