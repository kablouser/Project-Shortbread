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
            Vector2 velocity = (playerPosition - (Vector2)enemy.transform.position).normalized * enemy.moveSpeed;
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
                boss.unit.rigidbody.velocity = toPlayer / distanceToPlayer * boss.unit.moveSpeed;
                boss.unit.attack.isAttacking = true;
                boss.unit.SetRotationDegrees(boss.unit.rigidbody.velocity);
            }
        }
    }
}
