using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct AttackPreset
{
    public float attackCooldown;

    public bool usesAmmo;
    public float reloadCooldown;
    public uint fullMagazineAmmo;
}

[Serializable]
public struct AttackSystem
{
    public GameObject[] projectilePrefabs;

    public AttackPreset player;
    public AttackPreset enemy;

#if UNITY_EDITOR
    public bool IsValid()
    {
        return projectilePrefabs != null && Enum.GetNames(typeof(ProjectileType)).Length == projectilePrefabs.Length;
    }

    public void Validate()
    {
        Extensions.Resize(ref projectilePrefabs, Enum.GetNames(typeof(ProjectileType)).Length);
    }
#endif

    public void Update(MainScript mainScript)
    {
        UpdateUnit(ref mainScript.player, player);
    }

    public void UpdateUnit(ref UnitEntity unit, in AttackPreset preset)
    {
        unit.attack.reloadCooldown = Mathf.Max(unit.attack.reloadCooldown - Time.deltaTime, 0f);
        if (0f < unit.attack.reloadCooldown)
        {
            return;
        }

        if (preset.usesAmmo && unit.attack.ammoLeft <= 0)
        {

        }

        unit.attack.attackCooldown = Mathf.Max(unit.attack.attackCooldown - Time.deltaTime, 0f);
        if (0f < unit.attack.attackCooldown)
        {
            return;
        }

        if (unit.attack.isAttacking)
        {
/*            if (unit.attack.isMelee)
            {
                Physics2D.BoxCast()
            }
            else
            {
                
                unit.attack.projectile
            }*/

            unit.attack.attackCooldown = preset.attackCooldown;
            if (preset.usesAmmo)
            {
                --unit.attack.ammoLeft;
                if (unit.attack.ammoLeft <= 0)
                {

                }
            }
        }
    }
}
